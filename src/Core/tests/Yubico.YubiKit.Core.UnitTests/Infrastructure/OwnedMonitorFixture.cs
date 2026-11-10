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

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports;

namespace Yubico.YubiKit.Core.UnitTests.Infrastructure;

/// <summary>Owns a monitor and its scripted discovery, listeners, and clock.</summary>
internal sealed class OwnedMonitorFixture : IAsyncDisposable
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);
    private Task<YubiKeyMonitor>? _creation;
    public ScriptedFinder Finder { get; } = new();
    public FakeDeviceChangeListener Hid { get; } = new();
    public FakeDeviceChangeListener SmartCard { get; } = new();
    public ObservedTimeProvider Clock { get; } = new();
    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(1);

    public Task<YubiKeyMonitor> CreateAsync(CancellationToken cancellationToken) =>
        _creation = YubiKeyMonitor.CreateAsync(Finder,
            [(ConnectionType.Hid, () => Hid), (ConnectionType.SmartCard, () => SmartCard)], Interval, Clock, cancellationToken);

    public async Task WaitForScanAsync(int count, CancellationToken cancellationToken)
    {
        var monitor = await (_creation ?? throw new InvalidOperationException("Create the monitor first."));
        await WaitForRevisionAsync(monitor, count, cancellationToken);
    }

    internal static async Task WaitForRevisionAsync(YubiKeyMonitor monitor, int count, CancellationToken cancellationToken)
    {
        long revision = 0;
        Task read = Task.CompletedTask;
        await AsyncWait.WaitUntilAsync(() =>
        {
            if (read.IsFaulted || read.IsCanceled)
                return true; // Await below to preserve the stream's causal failure.
            if (read.IsCompletedSuccessfully)
                read = ReadRevisionAsync();
            return Volatile.Read(ref revision) >= count;
        }, $"The monitor did not publish scan {count}", Bound, cancellationToken);
        await read;

        async Task ReadRevisionAsync()
        {
            await foreach (var change in monitor.WatchAsync(cancellationToken))
            {
                if (change is SnapshotComplete snapshot)
                {
                    Volatile.Write(ref revision, snapshot.Revision);
                    return;
                }
            }
        }
    }

    public async Task TriggerAsync(CancellationToken cancellationToken)
    {
        var next = Finder.ScanCount + 1;
        SmartCard.Raise();
        await AdvanceHotplugAsync();
        await WaitForScanAsync(next, cancellationToken);
    }

    public async Task AdvanceHotplugAsync()
    {
        await Clock.WaitForTimerAsync(Interval);
        await Clock.WaitForTimerAsync(RescanScheduler.QuietPeriod);
        Clock.Advance(RescanScheduler.QuietPeriod);
    }

    public async ValueTask DisposeAsync()
    {
        if (_creation is not null)
        {
            YubiKeyMonitor? monitor = null;
            // Factory cleanup may still be in flight after an assertion or runner cancellation.
            try { monitor = await _creation.WaitAsync(Bound, CancellationToken.None); }
            catch (Exception) when (_creation.IsFaulted || _creation.IsCanceled)
            {
                // The test observes the causal factory error; the factory already cleaned ownership.
            }
            if (monitor is not null)
                await monitor.DisposeAsync();
        }
    }

    internal sealed class ScriptedFinder : IMonitoredDiscovery
    {
        private int _scanCount;
        private int _activityCount;
        public Func<CancellationToken, Task<IReadOnlyList<IYubiKey>>> Scan { get; set; } =
            ct =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult<IReadOnlyList<IYubiKey>>([]);
            };
        public Action<ConnectionType>? OnActivity { get; set; }
        public Action? OnRetire { get; set; }
        public int ScanCount => Volatile.Read(ref _scanCount);
        public int ActivityCount => Volatile.Read(ref _activityCount);
        public Task<IReadOnlyList<IYubiKey>> FindAllAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _scanCount);
            return Scan(cancellationToken);
        }
        public void InvalidateEvidence(ConnectionType transport)
        {
            Interlocked.Increment(ref _activityCount);
            OnActivity?.Invoke(transport);
        }
        public void Retire() => OnRetire?.Invoke();
    }

    internal sealed class FakeDeviceChangeListener : IDeviceChangeListener
    {
        public Action? Changed { get; set; }
        public DeviceListenerStatus Status { get; private set; }
        public bool Started => Status == DeviceListenerStatus.Started;
        public bool Disposed { get; private set; }
        public void Start() => Status = DeviceListenerStatus.Started;
        public void Stop() => Status = DeviceListenerStatus.Stopped;
        public void Raise() => Changed?.Invoke();
        public void Dispose() => Disposed = true;
    }
}