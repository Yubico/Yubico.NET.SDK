// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Diagnostics;
using Yubico.YubiKit.Core.Native.Desktop.SCard;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Transports.SmartCard;

/// <summary>
/// No-hardware regression tests for listener retry storms. Persistent native failures must be
/// backoff-bounded before retry or recovery, so these tests intentionally use fakes and must not
/// skip when PC/SC or YubiKey hardware is unavailable.
/// </summary>
[Trait("Category", "RuntimeResilience")]
public sealed class DesktopSmartCardDeviceListenerSCardErrorTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan StatusInterval = TimeSpan.FromSeconds(1);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SuccessfulStatus_WaitsBeforePollingAgain_AndStopWakesTheWait(bool pnpOnly)
    {
        var ct = TestContext.Current.CancellationToken;
        var waiting = new TaskCompletionSource<TimeSpan>(TaskCreationOptions.RunContinuationsAsynchronously);
        var nextNativeCall = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseUnexpectedCall = new ManualResetEventSlim();
        var interop = new FakeSCardApi
        {
            ListReaders = _ => pnpOnly
                ? (ErrorCode.SCARD_E_NO_READERS_AVAILABLE, [])
                : (ErrorCode.SCARD_S_SUCCESS, ["Reader A"]),
            Cancel = releaseUnexpectedCall.Set,
        };
        interop.GetStatusChange = _ =>
        {
            if (interop.GetStatusChangeCalls > 1)
            {
                nextNativeCall.TrySetResult();
                releaseUnexpectedCall.Wait(TestTimeout, ct);
            }

            return ErrorCode.SCARD_S_SUCCESS;
        };
        bool pulsed = false;
        int hints = 0;
        TimeSpan? firstWait = null;
        using var listener = new DesktopSmartCardDeviceListener(interop, _ => { }, (gate, remaining) =>
        {
            if (firstWait is null)
            {
                firstWait = remaining;
                return true; // Simulate an early pulse without advancing the native loop.
            }

            waiting.TrySetResult(remaining);
            // Hold the real monitor wait until Stop pulses it. The timeout is only a test guard.
            pulsed = Monitor.Wait(gate, TestTimeout);
            return pulsed;
        });
        listener.Changed = () => hints++;

        listener.Start();
        var firstBoundary = await Task.WhenAny(waiting.Task, nextNativeCall.Task).WaitAsync(TestTimeout, ct);
        Assert.Same(waiting.Task, firstBoundary);
        var requested = await waiting.Task.WaitAsync(TestTimeout, ct);
        Assert.True(requested > TimeSpan.Zero && requested <= StatusInterval);
        Assert.True(requested <= firstWait.GetValueOrDefault());
        Assert.Equal(1, interop.GetStatusChangeCalls);
        Assert.Equal(0, hints);
        Assert.Equal(0, interop.ReleasedContextCalls);

        listener.Stop();
        Assert.True(pulsed);
        Assert.False(nextNativeCall.Task.IsCompleted);
        Assert.Equal(0, hints);
        Assert.Equal(1, interop.ReleasedContextCalls);
    }

    [Fact]
    public async Task SuccessfulStatus_AlreadySpentTheInterval_DoesNotAddAManagedWait()
    {
        var ct = TestContext.Current.CancellationToken;
        var nativeStarted = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var nextNativeCall = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseNative = new ManualResetEventSlim();
        using var releaseNextNative = new ManualResetEventSlim();
        var interop = new FakeSCardApi
        {
            ListReaders = _ => (ErrorCode.SCARD_S_SUCCESS, ["Reader A"]),
            Cancel = () =>
            {
                releaseNative.Set();
                releaseNextNative.Set();
            },
        };
        interop.GetStatusChange = _ =>
        {
            if (interop.GetStatusChangeCalls == 1)
            {
                nativeStarted.SetResult(Stopwatch.GetTimestamp());
                releaseNative.Wait(TestTimeout, ct);
            }
            else
            {
                nextNativeCall.TrySetResult();
                releaseNextNative.Wait(TestTimeout, ct);
            }

            return ErrorCode.SCARD_S_SUCCESS;
        };
        using var listener = new DesktopSmartCardDeviceListener(interop, _ => { }, (gate, remaining) =>
        {
            waiting.TrySetResult();
            return Monitor.Wait(gate, remaining);
        });

        listener.Start();
        var started = await nativeStarted.Task.WaitAsync(TestTimeout, ct);
        var remaining = StatusInterval - Stopwatch.GetElapsedTime(started);
        while (remaining > TimeSpan.Zero)
        {
            await Task.Delay(remaining, ct);
            remaining = StatusInterval - Stopwatch.GetElapsedTime(started);
        }

        releaseNative.Set();
        var firstBoundary = await Task.WhenAny(waiting.Task, nextNativeCall.Task).WaitAsync(TestTimeout, ct);
        Assert.Same(nextNativeCall.Task, firstBoundary);
    }

    [Fact]
    public async Task SuccessfulStatus_ReaderChangeStillNotifies_AndStopDrainsHeldNativeCall()
    {
        var ct = TestContext.Current.CancellationToken;
        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var heldNativeCall = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseNative = new ManualResetEventSlim();
        var interop = new FakeSCardApi();
        bool nativeReturned = false;
        bool releasedBeforeNativeReturn = false;
        interop.ListReaders = _ => (ErrorCode.SCARD_S_SUCCESS,
            interop.GetStatusChangeCalls == 0 ? ["Reader A"] : ["Reader A", "Reader B"]);
        interop.GetStatusChange = _ =>
        {
            if (interop.GetStatusChangeCalls > 1)
            {
                heldNativeCall.SetResult();
                if (!releaseNative.Wait(TestTimeout, ct))
                {
                    throw new TimeoutException("The test did not release the native status call.");
                }

                Volatile.Write(ref nativeReturned, true);
            }

            return ErrorCode.SCARD_S_SUCCESS;
        };
        interop.Cancel = () => cancelRequested.TrySetResult();
        interop.ContextReleased = () => releasedBeforeNativeReturn = !Volatile.Read(ref nativeReturned);
        int hints = 0;
        using var listener = new DesktopSmartCardDeviceListener(interop, _ => { }, (gate, remaining) =>
        {
            waiting.TrySetResult();
            return Monitor.Wait(gate, remaining);
        });
        listener.Changed = () => hints++;

        Task? stop = null;
        try
        {
            listener.Start();
            var firstBoundary = await Task.WhenAny(waiting.Task, heldNativeCall.Task).WaitAsync(TestTimeout, ct);
            Assert.Same(waiting.Task, firstBoundary);
            await heldNativeCall.Task.WaitAsync(TestTimeout, ct);
            Assert.Equal(1, hints);

            stop = Task.Run(listener.Stop, ct);
            await cancelRequested.Task.WaitAsync(TestTimeout, ct);
            Assert.False(stop.IsCompleted);
            Assert.Equal(0, interop.ReleasedContextCalls);
        }
        finally
        {
            releaseNative.Set();
            if (stop is not null)
            {
                // Drain before disposing the fake gate, even if the test's token was canceled.
                await stop.WaitAsync(TestTimeout, CancellationToken.None);
            }
        }

        Assert.Equal(2, interop.GetStatusChangeCalls);
        Assert.Equal(1, interop.ReleasedContextCalls);
        Assert.False(releasedBeforeNativeReturn);
    }

    [Fact]
    public void WhenGetStatusChangeReturnsInvalidHandle_ListenerBacksOffBeforeRetrying()
    {
        var interop = new FakeSCardApi
        {
            ListReaders = _ => (ErrorCode.SCARD_S_SUCCESS, ["Reader A"]),
            GetStatusChange = _ => ErrorCode.SCARD_E_INVALID_HANDLE
        };
        using var sleeper = new BlockingSleeper();
        using var listener = new DesktopSmartCardDeviceListener(interop, sleeper.Sleep);

        listener.Start();

        Assert.True(sleeper.WaitUntilSleeping(), "Listener should back off after an invalid-handle status change.");
        Assert.Equal(1, interop.GetStatusChangeCalls);
        Assert.Equal(1, sleeper.SleepCalls);
        sleeper.Release();
    }

    [Fact]
    public void WhenListReadersReturnsInvalidHandle_ListenerBacksOffBeforeRetrying()
    {
        var firstListReaders = true;
        var interop = new FakeSCardApi
        {
            ListReaders = _ =>
            {
                if (firstListReaders)
                {
                    firstListReaders = false;
                    return (ErrorCode.SCARD_S_SUCCESS, ["Reader A"]);
                }

                return (ErrorCode.SCARD_E_INVALID_HANDLE, []);
            },
            GetStatusChange = _ => ErrorCode.SCARD_S_SUCCESS
        };
        using var sleeper = new BlockingSleeper();
        using var listener = new DesktopSmartCardDeviceListener(interop, sleeper.Sleep);

        listener.Start();

        Assert.True(sleeper.WaitUntilSleeping(), "Listener should back off after an invalid-handle reader list.");
        Assert.Equal(2, interop.ListReadersCalls);
        Assert.Equal(1, sleeper.SleepCalls);
        sleeper.Release();
    }

    [Fact]
    public void WhenContextReestablishmentFails_ListenerStopsAfterSingleBackoff()
    {
        var interop = new FakeSCardApi
        {
            EstablishContext = call => call == 1 ? ErrorCode.SCARD_S_SUCCESS : ErrorCode.SCARD_E_NO_SERVICE,
            ListReaders = _ => (ErrorCode.SCARD_S_SUCCESS, ["Reader A"]),
            GetStatusChange = _ => ErrorCode.SCARD_E_INVALID_HANDLE
        };
        using var sleeper = new BlockingSleeper();
        using var listener = new DesktopSmartCardDeviceListener(interop, sleeper.Sleep);

        listener.Start();

        Assert.True(sleeper.WaitUntilSleeping(), "Listener should back off before trying to re-establish context.");
        Assert.Equal(1, interop.EstablishContextCalls);
        sleeper.Release();

        Assert.True(
            SpinWait.SpinUntil(() => listener.Status == DeviceListenerStatus.Error, TimeSpan.FromSeconds(5)),
            "Listener should enter Error status when context re-establishment fails.");
        Assert.Equal(2, interop.EstablishContextCalls);
        Assert.Equal(1, sleeper.SleepCalls);
    }

    [Fact]
    public void WhenContextReestablishmentSucceeds_ListenerRebuildsBaselineAndResumes()
    {
        var resumedStatusWait = new ManualResetEventSlim();
        var releaseResumedStatusWait = new ManualResetEventSlim();
        var getStatusResults = 0;
        var interop = new FakeSCardApi
        {
            ListReaders = _ => (ErrorCode.SCARD_S_SUCCESS, ["Reader A"]),
            GetStatusChange = _ =>
            {
                if (Interlocked.Increment(ref getStatusResults) == 1)
                {
                    return ErrorCode.SCARD_E_INVALID_HANDLE;
                }

                resumedStatusWait.Set();
                releaseResumedStatusWait.Wait(TimeSpan.FromSeconds(5));
                return ErrorCode.SCARD_S_SUCCESS;
            }
        };
        using var sleeper = new BlockingSleeper();
        using var listener = new DesktopSmartCardDeviceListener(interop, sleeper.Sleep);

        try
        {
            listener.Start();

            Assert.True(sleeper.WaitUntilSleeping(), "Listener should back off before re-establishing context.");
            Assert.Equal(1, interop.EstablishContextCalls);

            sleeper.Release();

            Assert.True(
                resumedStatusWait.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken),
                "Listener should resume status monitoring after context recovery.");
            Assert.Equal(DeviceListenerStatus.Started, listener.Status);
            Assert.Equal(2, interop.EstablishContextCalls);
            Assert.True(interop.ListReadersCalls >= 4, "Recovery should rebuild the baseline before resuming the listener loop.");
            Assert.Equal(1, sleeper.SleepCalls);
        }
        finally
        {
            releaseResumedStatusWait.Set();
            resumedStatusWait.Dispose();
            releaseResumedStatusWait.Dispose();
        }
    }

    [Fact]
    public void WhenListenerRestarts_PreviousContextsAreDisposed()
    {
        var interop = new FakeSCardApi
        {
            ListReaders = _ => (ErrorCode.SCARD_E_NO_READERS_AVAILABLE, [])
        };
        using var listener = new DesktopSmartCardDeviceListener(interop, _ => { });

        listener.Start();
        listener.Stop();
        listener.Start();
        listener.Stop();
        listener.Dispose();

        Assert.Equal(2, interop.EstablishContextCalls);
        Assert.Equal(interop.EstablishContextCalls, interop.ReleasedContextCalls);
    }

    private sealed class BlockingSleeper : IDisposable
    {
        private readonly ManualResetEventSlim _sleeping = new();
        private readonly ManualResetEventSlim _release = new();

        public int SleepCalls { get; private set; }

        public void Sleep(TimeSpan _)
        {
            SleepCalls++;
            _sleeping.Set();
            _release.Wait(TimeSpan.FromSeconds(5));
        }

        public bool WaitUntilSleeping() => _sleeping.Wait(TimeSpan.FromSeconds(5));

        public void Release() => _release.Set();

        public void Dispose()
        {
            _release.Set();
            _sleeping.Dispose();
            _release.Dispose();
        }
    }

    private sealed class FakeSCardApi : ISCardApi
    {
        public Func<int, uint> EstablishContext { get; set; } = _ => ErrorCode.SCARD_S_SUCCESS;

        public Func<SCardContext, (uint Result, string[] Readers)> ListReaders { get; set; } =
            _ => (ErrorCode.SCARD_S_SUCCESS, []);

        public Func<SCARD_READER_STATE[], uint> GetStatusChange { get; set; } =
            _ => ErrorCode.SCARD_S_SUCCESS;

        public Action? Cancel { get; set; }
        public Action? ContextReleased { get; set; }

        public int EstablishContextCalls { get; private set; }
        public int GetStatusChangeCalls { get; private set; }
        public int ListReadersCalls { get; private set; }
        public int ReleasedContextCalls { get; private set; }

        public uint SCardEstablishContext(SCARD_SCOPE scope, out SCardContext context)
        {
            EstablishContextCalls++;
            context = new TestSCardContext(new IntPtr(EstablishContextCalls), this);
            return EstablishContext(EstablishContextCalls);
        }

        public uint SCardListReaders(SCardContext context, string[]? groups, out string[] readerNames)
        {
            ListReadersCalls++;
            var result = ListReaders(context);
            readerNames = result.Readers;
            return result.Result;
        }

        public uint SCardGetStatusChange(
            SCardContext context,
            int timeout,
            SCARD_READER_STATE[] readerStates,
            int readerStatesCount)
        {
            GetStatusChangeCalls++;
            return GetStatusChange(readerStates);
        }

        public uint SCardCancel(SCardContext context)
        {
            Cancel?.Invoke();
            return ErrorCode.SCARD_S_SUCCESS;
        }

        private void RecordReleasedContext()
        {
            ReleasedContextCalls++;
            ContextReleased?.Invoke();
        }

        private sealed class TestSCardContext(IntPtr handle, FakeSCardApi owner) : SCardContext(handle)
        {
            protected override bool ReleaseHandle()
            {
                owner.RecordReleasedContext();
                return true;
            }
        }
    }
}