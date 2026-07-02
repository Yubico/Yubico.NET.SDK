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

        public uint SCardCancel(SCardContext context) => ErrorCode.SCARD_S_SUCCESS;

        private void RecordReleasedContext() => ReleasedContextCalls++;

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