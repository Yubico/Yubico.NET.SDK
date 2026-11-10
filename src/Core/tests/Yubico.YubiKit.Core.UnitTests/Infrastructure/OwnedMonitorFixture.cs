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

/// <summary>Owns one monitor, including factory-failure cleanup and the actual loop's final drain.</summary>
internal sealed class OwnedMonitorFixture : IAsyncDisposable
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);
    private Task<YubiKeyMonitor>? _creation;
    private int _completedScans;

    public YubiKeyDeviceRepository Repository { get; } = new();
    public ScriptedFinder Finder { get; } = new();
    public FakeDeviceChangeListener Hid { get; } = new();
    public FakeDeviceChangeListener SmartCard { get; } = new();
    public YubiKeyMonitorWorker Worker { get; }
    public Task Loop => Worker.MonitoringCompletionForTest;
    public Action? WaitReached { get; set; }

    public OwnedMonitorFixture()
    {
        Worker = new(Repository, Finder,
            [(ConnectionType.Hid, () => Hid), (ConnectionType.SmartCard, () => SmartCard)], TimeSpan.FromMilliseconds(50));
        Worker.MonitoringWaitReachedForTest = () =>
        {
            Volatile.Write(ref _completedScans, Finder.ScanCount);
            WaitReached?.Invoke();
        };
    }

    public Task<YubiKeyMonitor> CreateAsync(CancellationToken cancellationToken) =>
        _creation = YubiKeyMonitor.CreateAsync(Repository, Worker, cancellationToken, TimeSpan.FromHours(1));

    public Task WaitForScanAsync(int count, CancellationToken cancellationToken) => AsyncWait.WaitUntilAsync(
        () => Volatile.Read(ref _completedScans) >= count,
        $"The loop did not finish scan/publication {count}", Bound, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (_creation is null)
        {
            await Worker.DisposeAsync();
            Repository.Dispose();
        }
        else
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

        // Released test gates may still be owned by an abandoned loop after runner cancellation.
        await Loop.WaitAsync(Bound, CancellationToken.None);
    }

    internal sealed class ScriptedFinder : IMonitoredDiscovery
    {
        private int _scanCount;
        private int _activityCount;
        public Func<CancellationToken, Task<IReadOnlyList<IYubiKey>>> Scan { get; set; } =
            _ => Task.FromResult<IReadOnlyList<IYubiKey>>([]);
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