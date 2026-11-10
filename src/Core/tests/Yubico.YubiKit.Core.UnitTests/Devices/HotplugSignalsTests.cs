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

using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public class HotplugSignalsTests
{
    [Fact]
    public async Task StartedListeners_EmitTheirTransport()
    {
        var hid = new Listener("hid", []);
        var smartCard = new Listener("card", []);
        using var signals = new HotplugSignals(
            [(ConnectionType.Hid, () => hid), (ConnectionType.SmartCard, () => smartCard)], NullLogger.Instance);

        hid.Changed?.Invoke();
        Assert.Equal(ConnectionType.Hid, await signals.Signals.ReadAsync(TestContext.Current.CancellationToken));
        smartCard.Changed?.Invoke();
        Assert.Equal(ConnectionType.SmartCard, await signals.Signals.ReadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void FactoryFailure_DoesNotPreventOtherListener()
    {
        var listener = new Listener("card", []);
        using var signals = new HotplugSignals(
            [(ConnectionType.Hid, () => throw new InvalidOperationException("unavailable")),
                (ConnectionType.SmartCard, () => listener)], NullLogger.Instance);

        listener.Changed?.Invoke();
        Assert.True(signals.Signals.TryRead(out var transport));
        Assert.Equal(ConnectionType.SmartCard, transport);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedStart_CleansListenerAndContinues(bool throws)
    {
        var calls = new List<string>();
        var failed = new Listener("failed", calls) { StartStatus = DeviceListenerStatus.Stopped, ThrowOnStart = throws };
        var healthy = new Listener("healthy", calls);
        using var signals = new HotplugSignals(
            [(ConnectionType.Hid, () => failed), (ConnectionType.SmartCard, () => healthy)], NullLogger.Instance);

        Assert.Equal(["failed.start", "failed.clear", "failed.stop", "failed.dispose", "healthy.start"], calls);
        Assert.Null(failed.Changed);
        healthy.Changed?.Invoke();
        Assert.True(signals.Signals.TryRead(out var transport));
        Assert.Equal(ConnectionType.SmartCard, transport);
    }

    [Fact]
    public void RapidSignals_DropTheSecondWrite()
    {
        var listener = new Listener("hid", []);
        using var signals = new HotplugSignals([(ConnectionType.Hid, () => listener)], NullLogger.Instance);
        listener.Changed?.Invoke();
        listener.Changed?.Invoke();

        Assert.True(signals.Signals.TryRead(out _));
        Assert.False(signals.Signals.TryRead(out _));
    }

    [Fact]
    public async Task Dispose_ClearsThenStopsThenDisposesAndRejectsLateCallback()
    {
        var calls = new List<string>();
        var hid = new Listener("hid", calls);
        var card = new Listener("card", calls);
        var signals = new HotplugSignals(
            [(ConnectionType.Hid, () => hid), (ConnectionType.SmartCard, () => card)], NullLogger.Instance);
        var lateCallback = Assert.IsType<Action>(hid.Changed);
        calls.Clear();

        signals.Dispose();
        lateCallback();
        signals.Dispose();

        Assert.Equal(["hid.clear", "card.clear", "hid.stop", "card.stop", "hid.dispose", "card.dispose"], calls);
        Assert.Null(hid.Changed);
        Assert.Null(card.Changed);
        Assert.False(signals.Signals.TryRead(out _));
        Assert.False(await signals.Signals.WaitToReadAsync(TestContext.Current.CancellationToken));
        await signals.Signals.Completion.WaitAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CleanupFailures_DoNotPreventRemainingStepsOrCompletion()
    {
        var calls = new List<string>();
        var listener = new Listener("hid", calls) { ThrowOnCleanup = true };
        var signals = new HotplugSignals([(ConnectionType.Hid, () => listener)], NullLogger.Instance);
        calls.Clear();

        signals.Dispose();

        Assert.Equal(["hid.clear", "hid.stop", "hid.dispose"], calls);
        Assert.False(await signals.Signals.WaitToReadAsync(TestContext.Current.CancellationToken));
    }

    private sealed class Listener(string name, List<string> calls) : IDeviceChangeListener
    {
        private Action? _changed;
        public DeviceListenerStatus StartStatus { get; init; } = DeviceListenerStatus.Started;
        public bool ThrowOnStart { get; init; }
        public bool ThrowOnCleanup { get; init; }
        public Action? Changed
        {
            get => _changed;
            set
            {
                if (value is null)
                    Cleanup("clear");
                _changed = value;
            }
        }
        public DeviceListenerStatus Status { get; private set; }
        public void Start()
        {
            Assert.NotNull(Changed);
            calls.Add($"{name}.start");
            if (ThrowOnStart)
                throw new InvalidOperationException("start failed");
            Status = StartStatus;
        }
        public void Stop() => Cleanup("stop");
        public void Dispose() => Cleanup("dispose");
        private void Cleanup(string operation)
        {
            calls.Add($"{name}.{operation}");
            if (ThrowOnCleanup)
                throw new InvalidOperationException("cleanup failed");
        }
    }
}