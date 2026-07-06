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

using System.Collections.Concurrent;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.Hid.Linux;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

/// <summary>
/// No-hardware fault injection for <see cref="LinuxHidDeviceListener"/> via a fake
/// <see cref="ILinuxHidEventSource"/>. Verifies the red-team failure modes: persistent
/// poll/fd errors must transition to Error and exit without spinning, shutdown must unblock
/// a waiting thread promptly, receive failures and unclassifiable events must emit
/// unknown-change rescan hints instead of being suppressed, and every session must dispose
/// its event source exactly once. Runs on all platforms because the fake replaces udev.
/// </summary>
[Trait("Category", "RuntimeResilience")]
public class LinuxHidDeviceListenerFaultInjectionTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public void PersistentPollFailure_TransitionsToErrorWithoutSpinningAndDisposesSource() =>
        AssertFailureOutcomeStopsListener(LinuxHidPollOutcome.PollFailed);

    [Fact]
    public void MonitorFdError_TransitionsToErrorWithoutSpinningAndDisposesSource() =>
        AssertFailureOutcomeStopsListener(LinuxHidPollOutcome.MonitorFdError);

    [Fact]
    public void ShutdownFdError_TransitionsToErrorWithoutSpinningAndDisposesSource() =>
        AssertFailureOutcomeStopsListener(LinuxHidPollOutcome.ShutdownFdError);

    private static void AssertFailureOutcomeStopsListener(LinuxHidPollOutcome failureOutcome)
    {
        // Arrange
        var source = new FakeLinuxHidEventSource();
        source.ScriptOutcomes(failureOutcome);
        using var listener = new LinuxHidDeviceListener(() => source);

        // Act
        listener.Start();

        // Assert - the loop must exit on the first failure, not hot-spin retrying a broken fd.
        Assert.True(
            SpinWait.SpinUntil(() => listener.Status == DeviceListenerStatus.Error, DefaultTimeout),
            $"Listener did not reach Error after {failureOutcome}");
        Assert.True(
            SpinWait.SpinUntil(() => source.Disposed, DefaultTimeout),
            "Listener thread did not dispose its event source on the error path");
        Assert.Equal(1, source.WaitCalls);

        // Stop after Error must still land in Stopped without double-disposing the source.
        listener.Stop();
        Assert.Equal(DeviceListenerStatus.Stopped, listener.Status);
        Assert.Equal(1, source.DisposeCalls);
    }

    [Fact]
    public void Stop_WhileThreadBlockedInWait_UnblocksPromptlyAndDisposesSource()
    {
        // Arrange - no scripted outcomes: the fake blocks in WaitForEvent until shutdown,
        // modeling a poll() blocked with no udev traffic.
        var source = new FakeLinuxHidEventSource();
        using var listener = new LinuxHidDeviceListener(() => source);
        listener.Start();
        Assert.True(source.WaitUntilBlocked(DefaultTimeout), "Listener thread never entered WaitForEvent");

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        listener.Stop();
        stopwatch.Stop();

        // Assert - shutdown signal must wake the blocked thread well before the join timeout.
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(4),
            $"Stop took {stopwatch.Elapsed}; shutdown signal failed to wake the blocked thread");
        Assert.Equal(DeviceListenerStatus.Stopped, listener.Status);
        Assert.True(source.Disposed, "Joined listener thread must have disposed its event source");
        Assert.Equal(1, source.WaitCalls);
    }

    [Fact]
    public void ReceiveFailure_EmitsUnknownRescanHintAndKeepsListening()
    {
        // Arrange - poll reports an event but the receive fails (e.g. ENOBUFS overrun),
        // which is exactly when a removal may have been lost.
        var source = new FakeLinuxHidEventSource();
        source.ScriptOutcomes(LinuxHidPollOutcome.Event);
        source.ScriptReceives((LinuxHidUdevEvent?)null);
        using var listener = new LinuxHidDeviceListener(() => source);
        var hints = new ConcurrentQueue<HidDeviceRescanHint>();
        listener.DeviceEvent = hints.Enqueue;

        // Act
        listener.Start();

        // Assert - never suppress: an unknown-change hint must trigger a discovery rescan.
        Assert.True(
            SpinWait.SpinUntil(() => !hints.IsEmpty, DefaultTimeout),
            "Receive failure did not produce a rescan hint");
        Assert.True(hints.TryDequeue(out var hint));
        Assert.Equal(HidDeviceRescanHint.Unknown, hint);
        Assert.Equal(DeviceListenerStatus.Started, listener.Status);

        listener.Stop();
        Assert.Equal(DeviceListenerStatus.Stopped, listener.Status);
    }

    [Fact]
    public void EventWithoutAction_EmitsUnknownRescanHint()
    {
        // Arrange
        var source = new FakeLinuxHidEventSource();
        source.ScriptOutcomes(LinuxHidPollOutcome.Event);
        source.ScriptReceives(new LinuxHidUdevEvent(Action: null, "/sys/devices/hid-1", "/dev/hidraw0"));
        using var listener = new LinuxHidDeviceListener(() => source);
        var hints = new ConcurrentQueue<HidDeviceRescanHint>();
        listener.DeviceEvent = hints.Enqueue;

        // Act
        listener.Start();

        // Assert
        Assert.True(
            SpinWait.SpinUntil(() => !hints.IsEmpty, DefaultTimeout),
            "Action-less udev event did not produce a rescan hint");
        Assert.True(hints.TryDequeue(out var hint));
        Assert.Equal(HidDeviceRescanHint.Unknown, hint);

        listener.Stop();
    }

    [Fact]
    public void NonTopologyAction_DoesNotEmitHintAndLoopContinues()
    {
        // Arrange - a "change" event must be ignored, and the loop must stay healthy enough
        // to deliver the "add" that follows it.
        var source = new FakeLinuxHidEventSource();
        source.ScriptOutcomes(LinuxHidPollOutcome.Event, LinuxHidPollOutcome.Event);
        source.ScriptReceives(
            new LinuxHidUdevEvent("change", "/sys/devices/hid-1", "/dev/hidraw0"),
            new LinuxHidUdevEvent("add", "/sys/devices/hid-2", "/dev/hidraw1"));
        using var listener = new LinuxHidDeviceListener(() => source);
        var hints = new ConcurrentQueue<HidDeviceRescanHint>();
        listener.DeviceEvent = hints.Enqueue;

        // Act
        listener.Start();

        // Assert - exactly one hint: the add. The preceding change produced nothing.
        Assert.True(
            SpinWait.SpinUntil(() => !hints.IsEmpty, DefaultTimeout),
            "Add event following an ignored change event did not produce a hint");
        Assert.True(hints.TryDequeue(out var hint));
        Assert.Equal(HidDeviceChangeKind.Added, hint!.ChangeKind);
        Assert.Equal("/sys/devices/hid-2", hint.PlatformDeviceId);
        Assert.Equal("/dev/hidraw1", hint.DevicePath);
        Assert.True(hints.IsEmpty);

        listener.Stop();
    }

    [Fact]
    public void RemoveEvent_EmitsRemovedHintWithStableIdentity()
    {
        // Arrange
        var source = new FakeLinuxHidEventSource();
        source.ScriptOutcomes(LinuxHidPollOutcome.Event);
        source.ScriptReceives(new LinuxHidUdevEvent("remove", "/sys/devices/hid-1", "/dev/hidraw0"));
        using var listener = new LinuxHidDeviceListener(() => source);
        var hints = new ConcurrentQueue<HidDeviceRescanHint>();
        listener.DeviceEvent = hints.Enqueue;

        // Act
        listener.Start();

        // Assert
        Assert.True(
            SpinWait.SpinUntil(() => !hints.IsEmpty, DefaultTimeout),
            "Remove event did not produce a rescan hint");
        Assert.True(hints.TryDequeue(out var hint));
        Assert.Equal(HidDeviceChangeKind.Removed, hint!.ChangeKind);
        Assert.Equal("/sys/devices/hid-1", hint.PlatformDeviceId);

        listener.Stop();
    }

    [Fact]
    public void AddEventWithUnreadyHidrawNode_EmitsDelayedFallbackHint()
    {
        // Arrange - the hidraw node is not ready at notification time; the listener must
        // re-hint after the readiness delay so discovery retries once permissions settle.
        var source = new FakeLinuxHidEventSource { HidrawReady = false };
        source.ScriptOutcomes(LinuxHidPollOutcome.Event);
        source.ScriptReceives(new LinuxHidUdevEvent("add", "/sys/devices/hid-1", "/dev/hidraw0"));
        using var listener = new LinuxHidDeviceListener(() => source);
        var hints = new ConcurrentQueue<HidDeviceRescanHint>();
        listener.DeviceEvent = hints.Enqueue;

        // Act
        listener.Start();

        // Assert
        Assert.True(
            SpinWait.SpinUntil(() => hints.Count >= 2, DefaultTimeout),
            "Unready hidraw node did not produce a delayed fallback hint");
        Assert.True(hints.TryDequeue(out var first));
        Assert.True(hints.TryDequeue(out var fallback));
        Assert.Equal(HidDeviceChangeKind.Added, first!.ChangeKind);
        Assert.Equal(first, fallback);

        listener.Stop();
    }

    [Fact]
    public void InitializeFailure_SetsErrorAndDisposesSourceWithoutStartingThread()
    {
        // Arrange
        var source = new FakeLinuxHidEventSource { InitializeResult = false };
        using var listener = new LinuxHidDeviceListener(() => source);

        // Act
        listener.Start();

        // Assert
        Assert.Equal(DeviceListenerStatus.Error, listener.Status);
        Assert.True(source.Disposed, "Failed initialization must dispose the event source");
        Assert.Equal(0, source.WaitCalls);
    }

    [Fact]
    public void StartAfterError_CreatesFreshSourceAndListens()
    {
        // Arrange - first session fails to initialize; a later Start must recover with a
        // brand-new source instead of staying wedged in Error.
        var broken = new FakeLinuxHidEventSource { InitializeResult = false };
        var healthy = new FakeLinuxHidEventSource();
        var sources = new Queue<FakeLinuxHidEventSource>([broken, healthy]);
        var factoryCalls = 0;
        using var listener = new LinuxHidDeviceListener(() =>
        {
            factoryCalls++;
            return sources.Dequeue();
        });

        // Act
        listener.Start();
        Assert.Equal(DeviceListenerStatus.Error, listener.Status);
        listener.Start();

        // Assert
        Assert.Equal(2, factoryCalls);
        Assert.Equal(DeviceListenerStatus.Started, listener.Status);
        Assert.True(healthy.WaitUntilBlocked(DefaultTimeout), "Recovered session never started listening");

        listener.Stop();
        Assert.Equal(DeviceListenerStatus.Stopped, listener.Status);
        Assert.True(healthy.Disposed);
    }

    [Theory]
    [InlineData("/sys/devices/pci0/usb1/1-1/1-1:1.0/0003:1050:0407.0001/hidraw/hidraw3",
        "/sys/devices/pci0/usb1/1-1/1-1:1.0/0003:1050:0407.0001")]
    [InlineData("/sys/devices/pci0/usb1/1-1/1-1:1.0/0003:1050:0407.0001/hidraw",
        "/sys/devices/pci0/usb1/1-1/1-1:1.0/0003:1050:0407.0001")]
    [InlineData("/sys/devices/pci0/usb1/1-1/1-1:1.0/0003:1050:0407.0001",
        "/sys/devices/pci0/usb1/1-1/1-1:1.0/0003:1050:0407.0001")]
    public void TrimHidrawSyspath_MapsHidrawNodesToStableParentIdentity(string syspath, string expected)
    {
        // add and remove notifications arrive on the hidraw child; identity must resolve to
        // the parent HID device path so both sides of a plug/unplug pair correlate.
        Assert.Equal(expected, LinuxUdevHidEventSource.TrimHidrawSyspath(syspath));
    }

    /// <summary>
    /// Scriptable fake event source. Scripted outcomes are returned in order; once exhausted,
    /// <see cref="WaitForEvent"/> blocks (like a quiet poll()) until <see cref="SignalShutdown"/>.
    /// </summary>
    private sealed class FakeLinuxHidEventSource : ILinuxHidEventSource
    {
        private readonly ConcurrentQueue<LinuxHidPollOutcome> _outcomes = new();
        private readonly ConcurrentQueue<LinuxHidUdevEvent?> _receives = new();
        private readonly ManualResetEventSlim _shutdownWake = new(false);
        private readonly ManualResetEventSlim _blockedInWait = new(false);
        private volatile bool _shutdownSignaled;
        private int _waitCalls;
        private int _disposeCalls;

        public bool InitializeResult { get; set; } = true;

        public bool HidrawReady { get; set; } = true;

        public int WaitCalls => Volatile.Read(ref _waitCalls);

        public int DisposeCalls => Volatile.Read(ref _disposeCalls);

        public bool Disposed => DisposeCalls > 0;

        public void ScriptOutcomes(params LinuxHidPollOutcome[] outcomes)
        {
            foreach (var outcome in outcomes)
            {
                _outcomes.Enqueue(outcome);
            }
        }

        public void ScriptReceives(params LinuxHidUdevEvent?[] events)
        {
            foreach (var udevEvent in events)
            {
                _receives.Enqueue(udevEvent);
            }
        }

        public bool WaitUntilBlocked(TimeSpan timeout) => _blockedInWait.Wait(timeout);

        public bool Initialize() => InitializeResult;

        public LinuxHidPollOutcome WaitForEvent()
        {
            Interlocked.Increment(ref _waitCalls);

            if (_shutdownSignaled)
            {
                return LinuxHidPollOutcome.ShutdownSignaled;
            }

            if (_outcomes.TryDequeue(out var outcome))
            {
                return outcome;
            }

            _blockedInWait.Set();
            _shutdownWake.Wait();
            return LinuxHidPollOutcome.ShutdownSignaled;
        }

        public LinuxHidUdevEvent? ReceiveEvent() =>
            _receives.TryDequeue(out var udevEvent) ? udevEvent : null;

        public void SignalShutdown()
        {
            _shutdownSignaled = true;
            _shutdownWake.Set();
        }

        public bool IsHidrawReady(string? devNode) => HidrawReady;

        public void Dispose()
        {
            Interlocked.Increment(ref _disposeCalls);
            _shutdownWake.Set();
        }
    }
}