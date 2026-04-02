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
/// Tests for <see cref="YubiKeyDeviceMonitorService"/>.
/// </summary>
public class YubiKeyDeviceMonitorServiceTests
{

    [Fact]
    public async Task RescanAsync_UpdatesRepository()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([
            new FakeYubiKey("device-1", ConnectionType.SmartCard),
            new FakeYubiKey("device-2", ConnectionType.HidFido)
        ]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);

        // Act
        await service.RescanAsync();

        // Assert
        var devices = repository.GetAll();
        Assert.Equal(2, devices.Count);
        Assert.True(repository.HasData);

        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    public async Task RescanAsync_CalledMultipleTimes_UpdatesCorrectly()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);

        // Act - First scan
        await service.RescanAsync();
        Assert.Single(repository.GetAll());

        // Change what FindYubiKeys returns
        findYubiKeys.SetDevices([
            new FakeYubiKey("device-2", ConnectionType.HidFido),
            new FakeYubiKey("device-3", ConnectionType.HidOtp)
        ]);

        // Act - Second scan
        await service.RescanAsync();

        // Assert
        var devices = repository.GetAll();
        Assert.Equal(2, devices.Count);
        Assert.Contains(devices, d => d.DeviceId == "device-2");
        Assert.Contains(devices, d => d.DeviceId == "device-3");
        Assert.DoesNotContain(devices, d => d.DeviceId == "device-1");

        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    public async Task RescanAsync_EmitsEvents()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);

        var events = new List<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events.Add);

        // Act
        await service.RescanAsync();

        // Assert
        Assert.Single(events);
        Assert.Equal(DeviceAction.Added, events[0].Action);
        Assert.Equal("device-1", events[0].Device.DeviceId);

        await service.DisposeAsync();
        repository.Dispose();
    }



    [Fact]
    public void IsMonitoring_InitiallyFalse()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);

        // Act & Assert
        Assert.False(service.IsMonitoring);

        repository.Dispose();
    }

    [Fact]
    public void StartMonitoring_SetsIsMonitoringTrue()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);

        // Act
        service.StartMonitoring(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(service.IsMonitoring);

        // Cleanup
        service.StopMonitoring();
        repository.Dispose();
    }

    [Fact]
    public void StopMonitoring_SetsIsMonitoringFalse()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);
        service.StartMonitoring(TimeSpan.FromSeconds(5));

        // Act
        service.StopMonitoring();

        // Assert
        Assert.False(service.IsMonitoring);

        repository.Dispose();
    }

    [Fact]
    public void StartMonitoring_Idempotent()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);

        // Act - Call multiple times
        service.StartMonitoring(TimeSpan.FromSeconds(5));
        service.StartMonitoring(TimeSpan.FromSeconds(5));
        service.StartMonitoring(TimeSpan.FromSeconds(5));

        // Assert - Still monitoring, no exception
        Assert.True(service.IsMonitoring);

        // Cleanup
        service.StopMonitoring();
        repository.Dispose();
    }

    [Fact]
    public void StopMonitoring_Idempotent()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);

        // Act - Call multiple times without starting
        service.StopMonitoring();
        service.StopMonitoring();
        service.StopMonitoring();

        // Assert - No exception
        Assert.False(service.IsMonitoring);

        repository.Dispose();
    }

    [Fact]
    public void StartMonitoring_ZeroInterval_Throws()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => service.StartMonitoring(TimeSpan.Zero));

        repository.Dispose();
    }

    [Fact]
    public void StartMonitoring_NegativeInterval_Throws()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => service.StartMonitoring(TimeSpan.FromSeconds(-1)));

        repository.Dispose();
    }



    [Fact]
    public async Task DisposeAsync_StopsMonitoring()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);
        service.StartMonitoring(TimeSpan.FromSeconds(5));

        // Act
        await service.DisposeAsync();

        // Assert
        Assert.False(service.IsMonitoring);

        repository.Dispose();
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);

        // Act & Assert - No exception
        await service.DisposeAsync();
        await service.DisposeAsync();
        await service.DisposeAsync();

        repository.Dispose();
    }

    [Fact]
    public async Task RescanAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);
        await service.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => service.RescanAsync());

        repository.Dispose();
    }

    [Fact]
    public async Task StartMonitoring_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);
        await service.DisposeAsync();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => service.StartMonitoring(TimeSpan.FromSeconds(5)));

        repository.Dispose();
    }



    [Fact]
    public void Constructor_NullRepository_Throws()
    {
        // Arrange
        var findYubiKeys = new FakeFindYubiKeys([]);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new YubiKeyDeviceMonitorService(null!, findYubiKeys));
    }

    [Fact]
    public void Constructor_NullFindYubiKeys_Throws()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new YubiKeyDeviceMonitorService(repository, null!));

        repository.Dispose();
    }



    /// <summary>
    /// Fake IFindYubiKeys for testing.
    /// </summary>
    private sealed class FakeFindYubiKeys(IReadOnlyList<IYubiKey> initialDevices) : IFindYubiKeys
    {
        private IReadOnlyList<IYubiKey> _devices = initialDevices;

        public void SetDevices(IReadOnlyList<IYubiKey> devices) => _devices = devices;

        public Task<IReadOnlyList<IYubiKey>> FindAllAsync(
            ConnectionType type,
            CancellationToken cancellationToken = default)
        {
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
