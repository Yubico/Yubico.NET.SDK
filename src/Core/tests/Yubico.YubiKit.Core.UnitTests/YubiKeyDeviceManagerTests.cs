// Copyright 2025 Yubico AB
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

using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.UnitTests;

/// <summary>
/// Tests for <see cref="YubiKeyDeviceManager"/> - composition root for device management.
/// </summary>
public class YubiKeyDeviceManagerTests
{

    [Fact]
    public async Task FindAllAsync_FirstCall_PerformsScan()
    {
        // Arrange
        var (manager, findYubiKeys, repository) = CreateManager();
        findYubiKeys.SetDevices([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);

        // Act
        var devices = await manager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(devices);
        Assert.Equal(1, findYubiKeys.ScanCount);

        await manager.DisposeAsync();
    }

    [Fact]
    public async Task FindAllAsync_SubsequentCall_ReturnsCache()
    {
        // Arrange
        var (manager, findYubiKeys, repository) = CreateManager();
        findYubiKeys.SetDevices([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);

        // First call - triggers scan
        await manager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, findYubiKeys.ScanCount);

        // Change available devices (should not be seen)
        findYubiKeys.SetDevices([
            new FakeYubiKey("device-1", ConnectionType.SmartCard),
            new FakeYubiKey("device-2", ConnectionType.HidFido)
        ]);

        // Act - Second call
        var devices = await manager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert - Returns cached result, no new scan
        Assert.Single(devices);
        Assert.Equal(1, findYubiKeys.ScanCount);

        await manager.DisposeAsync();
    }

    [Fact]
    public async Task FindAllAsync_ForceRescan_AlwaysScans()
    {
        // Arrange
        var (manager, findYubiKeys, repository) = CreateManager();
        findYubiKeys.SetDevices([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);

        // First call
        await manager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, findYubiKeys.ScanCount);

        // Change available devices
        findYubiKeys.SetDevices([
            new FakeYubiKey("device-1", ConnectionType.SmartCard),
            new FakeYubiKey("device-2", ConnectionType.HidFido)
        ]);

        // Act - Force rescan
        var devices = await manager.FindAllAsync(forceRescan: true, cancellationToken: TestContext.Current.CancellationToken);

        // Assert - Sees new devices
        Assert.Equal(2, devices.Count);
        Assert.Equal(2, findYubiKeys.ScanCount);

        await manager.DisposeAsync();
    }

    [Fact]
    public async Task FindAllAsync_WhileMonitoring_ReturnsCache()
    {
        // Arrange
        var (manager, findYubiKeys, repository) = CreateManager();
        findYubiKeys.SetDevices([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);

        // Populate cache with an initial scan before monitoring starts
        await manager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Start monitoring (event-driven, does not trigger its own scan)
        manager.StartMonitoring(TimeSpan.FromSeconds(10));

        var scanCountAfterStart = findYubiKeys.ScanCount;

        // Act - Call FindAllAsync while monitoring
        var devices = await manager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert - Returns cache, no additional scan
        Assert.Single(devices);
        Assert.Equal(scanCountAfterStart, findYubiKeys.ScanCount);

        manager.StopMonitoring();
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task FindAllAsync_WithTypeFilter_ReturnsFilteredDevices()
    {
        // Arrange
        var (manager, findYubiKeys, repository) = CreateManager();
        findYubiKeys.SetDevices([
            new FakeYubiKey("device-1", ConnectionType.SmartCard),
            new FakeYubiKey("device-2", ConnectionType.HidFido),
            new FakeYubiKey("device-3", ConnectionType.SmartCard)
        ]);

        // Act
        var smartCardDevices = await manager.FindAllAsync(ConnectionType.SmartCard, cancellationToken: TestContext.Current.CancellationToken);
        var hidFidoDevices = await manager.FindAllAsync(ConnectionType.HidFido, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, smartCardDevices.Count);
        Assert.Single(hidFidoDevices);

        await manager.DisposeAsync();
    }



    [Fact]
    public async Task DeviceChanges_EmitsEventsFromRepository()
    {
        // Arrange
        var (manager, findYubiKeys, repository) = CreateManager();
        var events = new List<DeviceEvent>();
        using var subscription = manager.DeviceChanges.Subscribe(events.Add);

        findYubiKeys.SetDevices([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);

        // Act
        await manager.FindAllAsync(forceRescan: true, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(events);
        Assert.Equal(DeviceAction.Added, events[0].Action);

        await manager.DisposeAsync();
    }



    [Fact]
    public void IsMonitoring_InitiallyFalse()
    {
        // Arrange
        var (manager, _, _) = CreateManager();

        // Assert
        Assert.False(manager.IsMonitoring);

        manager.DisposeAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public void StartMonitoring_SetsIsMonitoringTrue()
    {
        // Arrange
        var (manager, _, _) = CreateManager();

        // Act
        manager.StartMonitoring();

        // Assert
        Assert.True(manager.IsMonitoring);

        manager.StopMonitoring();
        manager.DisposeAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public void StopMonitoring_SetsIsMonitoringFalse()
    {
        // Arrange
        var (manager, _, _) = CreateManager();
        manager.StartMonitoring();

        // Act
        manager.StopMonitoring();

        // Assert
        Assert.False(manager.IsMonitoring);

        manager.DisposeAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public void StartMonitoring_CustomInterval()
    {
        // Arrange
        var (manager, _, _) = CreateManager();

        // Act
        manager.StartMonitoring(TimeSpan.FromSeconds(10));

        // Assert
        Assert.True(manager.IsMonitoring);

        manager.StopMonitoring();
        manager.DisposeAsync().GetAwaiter().GetResult();
    }



    [Fact]
    public async Task DisposeAsync_StopsMonitoring()
    {
        // Arrange
        var (manager, _, _) = CreateManager();
        manager.StartMonitoring();
        Assert.True(manager.IsMonitoring);

        // Act
        await manager.DisposeAsync();

        // Assert
        Assert.False(manager.IsMonitoring);
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var (manager, _, _) = CreateManager();

        // Act & Assert - No exception
        await manager.DisposeAsync();
        await manager.DisposeAsync();
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task FindAllAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var (manager, _, _) = CreateManager();
        await manager.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StartMonitoring_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var (manager, _, _) = CreateManager();
        await manager.DisposeAsync();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => manager.StartMonitoring());
    }

    [Fact]
    public async Task StopMonitoring_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var (manager, _, _) = CreateManager();
        await manager.DisposeAsync();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => manager.StopMonitoring());
    }



    [Fact]
    public void Create_ReturnsValidInstance()
    {
        // Act
        var manager = YubiKeyDeviceManager.Create();

        // Assert
        Assert.NotNull(manager);
        Assert.False(manager.IsMonitoring);

        manager.DisposeAsync().GetAwaiter().GetResult();
    }



    [Fact]
    public async Task FindAllAsync_ConcurrentCalls_AllComplete()
    {
        // Arrange
        var (manager, findYubiKeys, _) = CreateManager();
        findYubiKeys.SetDevices([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);
        const int concurrency = 50;
        var tasks = new List<Task<IReadOnlyList<IYubiKey>>>();

        // Act
        for (int i = 0; i < concurrency; i++)
        {
            tasks.Add(manager.FindAllAsync());
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All should complete successfully
        Assert.All(results, r => Assert.Single(r));

        await manager.DisposeAsync();
    }



    private static (YubiKeyDeviceManager Manager, FakeFindYubiKeys FindYubiKeys, YubiKeyDeviceRepository Repository)
        CreateManager()
    {
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var monitorService = new YubiKeyDeviceMonitorService(repository, findYubiKeys);
        var manager = new YubiKeyDeviceManager(repository, monitorService);

        return (manager, findYubiKeys, repository);
    }

    /// <summary>
    /// Fake IFindYubiKeys for testing with scan counting.
    /// </summary>
    private sealed class FakeFindYubiKeys(IReadOnlyList<IYubiKey> initialDevices) : IFindYubiKeys
    {
        private IReadOnlyList<IYubiKey> _devices = initialDevices;

        public int ScanCount { get; private set; }

        public void SetDevices(IReadOnlyList<IYubiKey> devices) => _devices = devices;

        public Task<IReadOnlyList<IYubiKey>> FindAllAsync(
            ConnectionType type,
            CancellationToken cancellationToken = default)
        {
            ScanCount++;
            var filtered = type == ConnectionType.All
                ? _devices
                : _devices.Where(d => d.ConnectionType == type).ToList();
            return Task.FromResult<IReadOnlyList<IYubiKey>>(filtered);
        }
    }

    /// <summary>
    /// Minimal fake IYubiKey implementation for testing.
    /// </summary>
    private sealed class FakeYubiKey(string deviceId, ConnectionType connectionType) : IYubiKey
    {
        public string DeviceId { get; } = deviceId;
        public ConnectionType ConnectionType { get; } = connectionType;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
            => throw new NotSupportedException("FakeYubiKey does not support connections.");
    }

}
