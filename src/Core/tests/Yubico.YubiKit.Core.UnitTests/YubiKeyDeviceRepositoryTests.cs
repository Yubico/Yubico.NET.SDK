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

using System.Reactive.Linq;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.UnitTests;

/// <summary>
/// Tests for <see cref="YubiKeyDeviceRepository"/> - pure cache with diff-based events.
/// </summary>
public class YubiKeyDeviceRepositoryTests
{

    [Fact]
    public void UpdateCache_EmptyToDevices_EmitsAddedEvents()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        var events = new List<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events.Add);

        var device1 = new FakeYubiKey("device-1", ConnectionType.SmartCard);
        var device2 = new FakeYubiKey("device-2", ConnectionType.HidFido);

        // Act
        repository.UpdateCache([device1, device2]);

        // Assert
        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Equal(DeviceAction.Added, e.Action));
        Assert.Contains(events, e => e.Device.DeviceId == "device-1");
        Assert.Contains(events, e => e.Device.DeviceId == "device-2");
    }

    [Fact]
    public void UpdateCache_DevicesToEmpty_EmitsRemovedEvents()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        var device1 = new FakeYubiKey("device-1", ConnectionType.SmartCard);
        var device2 = new FakeYubiKey("device-2", ConnectionType.HidFido);
        repository.UpdateCache([device1, device2]);

        var events = new List<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events.Add);

        // Act
        repository.UpdateCache([]);

        // Assert
        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Equal(DeviceAction.Removed, e.Action));
        Assert.Contains(events, e => e.Device.DeviceId == "device-1");
        Assert.Contains(events, e => e.Device.DeviceId == "device-2");
    }

    [Fact]
    public void UpdateCache_DifferentDevices_EmitsCorrectAddedAndRemoved()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        var deviceA = new FakeYubiKey("device-A", ConnectionType.SmartCard);
        var deviceB = new FakeYubiKey("device-B", ConnectionType.HidFido);
        repository.UpdateCache([deviceA, deviceB]);

        var events = new List<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events.Add);

        var deviceC = new FakeYubiKey("device-C", ConnectionType.SmartCard);
        var deviceD = new FakeYubiKey("device-D", ConnectionType.HidOtp);

        // Act: Replace A,B with C,D
        repository.UpdateCache([deviceC, deviceD]);

        // Assert
        Assert.Equal(4, events.Count);

        var removed = events.Where(e => e.Action == DeviceAction.Removed).ToList();
        var added = events.Where(e => e.Action == DeviceAction.Added).ToList();

        Assert.Equal(2, removed.Count);
        Assert.Equal(2, added.Count);

        Assert.Contains(removed, e => e.Device.DeviceId == "device-A");
        Assert.Contains(removed, e => e.Device.DeviceId == "device-B");
        Assert.Contains(added, e => e.Device.DeviceId == "device-C");
        Assert.Contains(added, e => e.Device.DeviceId == "device-D");
    }

    [Fact]
    public void UpdateCache_SameDevices_NoEvents()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        var device1 = new FakeYubiKey("device-1", ConnectionType.SmartCard);
        repository.UpdateCache([device1]);

        var events = new List<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events.Add);

        // Act: Update with same device ID
        var device1Updated = new FakeYubiKey("device-1", ConnectionType.SmartCard);
        repository.UpdateCache([device1Updated]);

        // Assert: No events since device ID hasn't changed
        Assert.Empty(events);
    }

    [Fact]
    public void UpdateCache_PartialOverlap_EmitsOnlyChanges()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        var deviceA = new FakeYubiKey("device-A", ConnectionType.SmartCard);
        var deviceB = new FakeYubiKey("device-B", ConnectionType.HidFido);
        repository.UpdateCache([deviceA, deviceB]);

        var events = new List<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events.Add);

        var deviceC = new FakeYubiKey("device-C", ConnectionType.SmartCard);

        // Act: Keep B, remove A, add C
        repository.UpdateCache([deviceB, deviceC]);

        // Assert
        Assert.Equal(2, events.Count);
        Assert.Single(events.Where(e => e.Action == DeviceAction.Removed && e.Device.DeviceId == "device-A"));
        Assert.Single(events.Where(e => e.Action == DeviceAction.Added && e.Device.DeviceId == "device-C"));
    }



    [Fact]
    public void GetAll_WithConnectionTypeAll_ReturnsAllDevices()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        var device1 = new FakeYubiKey("device-1", ConnectionType.SmartCard);
        var device2 = new FakeYubiKey("device-2", ConnectionType.HidFido);
        var device3 = new FakeYubiKey("device-3", ConnectionType.HidOtp);
        repository.UpdateCache([device1, device2, device3]);

        // Act
        var result = repository.GetAll(ConnectionType.All);

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void GetAll_WithSmartCard_ReturnsOnlySmartCardDevices()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        var device1 = new FakeYubiKey("device-1", ConnectionType.SmartCard);
        var device2 = new FakeYubiKey("device-2", ConnectionType.HidFido);
        var device3 = new FakeYubiKey("device-3", ConnectionType.SmartCard);
        repository.UpdateCache([device1, device2, device3]);

        // Act
        var result = repository.GetAll(ConnectionType.SmartCard);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, d => Assert.Equal(ConnectionType.SmartCard, d.ConnectionType));
    }

    [Fact]
    public void GetAll_WithHidFido_ReturnsOnlyHidFidoDevices()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        var device1 = new FakeYubiKey("device-1", ConnectionType.SmartCard);
        var device2 = new FakeYubiKey("device-2", ConnectionType.HidFido);
        repository.UpdateCache([device1, device2]);

        // Act
        var result = repository.GetAll(ConnectionType.HidFido);

        // Assert
        Assert.Single(result);
        Assert.Equal("device-2", result[0].DeviceId);
    }

    [Fact]
    public void GetAll_EmptyCache_ReturnsEmptyList()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();

        // Act
        var result = repository.GetAll();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetAll_NoMatchingType_ReturnsEmptyList()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        var device = new FakeYubiKey("device-1", ConnectionType.SmartCard);
        repository.UpdateCache([device]);

        // Act
        var result = repository.GetAll(ConnectionType.HidOtp);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetAll_DefaultParameter_ReturnsAll()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        var device1 = new FakeYubiKey("device-1", ConnectionType.SmartCard);
        var device2 = new FakeYubiKey("device-2", ConnectionType.HidFido);
        repository.UpdateCache([device1, device2]);

        // Act: Call without parameter (defaults to All)
        var result = repository.GetAll();

        // Assert
        Assert.Equal(2, result.Count);
    }



    [Fact]
    public void Dispose_CompletesSubject()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var completed = false;
        repository.DeviceChanges.Subscribe(
            onNext: _ => { },
            onCompleted: () => completed = true);

        // Act
        repository.Dispose();

        // Assert
        Assert.True(completed);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();

        // Act & Assert: Should not throw
        repository.Dispose();
        repository.Dispose();
        repository.Dispose();
    }

    [Fact]
    public void GetAll_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        repository.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => repository.GetAll());
    }

    [Fact]
    public void UpdateCache_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        repository.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => repository.UpdateCache([]));
    }

    [Fact]
    public void Clear_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        repository.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => repository.Clear());
    }



    [Fact]
    public void HasData_InitiallyFalse()
    {
        // Arrange & Act
        using var repository = new YubiKeyDeviceRepository();

        // Assert
        Assert.False(repository.HasData);
    }

    [Fact]
    public void HasData_TrueAfterUpdateCache()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();

        // Act
        repository.UpdateCache([]);

        // Assert
        Assert.True(repository.HasData);
    }

    [Fact]
    public void HasData_FalseAfterClear()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        repository.UpdateCache([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);

        // Act
        repository.Clear();

        // Assert
        Assert.False(repository.HasData);
    }



    [Fact]
    public void Clear_RemovesAllDevices()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        repository.UpdateCache([
            new FakeYubiKey("device-1", ConnectionType.SmartCard),
            new FakeYubiKey("device-2", ConnectionType.HidFido)
        ]);

        // Act
        repository.Clear();

        // Assert
        Assert.Empty(repository.GetAll());
    }

    [Fact]
    public void Clear_DoesNotEmitEvents()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        repository.UpdateCache([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);

        var events = new List<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events.Add);

        // Act
        repository.Clear();

        // Assert: Clear is silent (no events)
        Assert.Empty(events);
    }



    [Fact]
    public void UpdateCache_ConcurrentCalls_NoCorruption()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        const int iterations = 100;

        // Act
        Parallel.For(0, iterations, i =>
        {
            var devices = Enumerable.Range(0, i % 5)
                .Select(j => new FakeYubiKey($"device-{i}-{j}", ConnectionType.SmartCard))
                .ToList();
            repository.UpdateCache(devices);
        });

        // Assert: Should not throw and final state is consistent
        var finalDevices = repository.GetAll();
        Assert.NotNull(finalDevices);
    }

    [Fact]
    public void GetAll_ConcurrentWithUpdateCache_NoException()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        const int iterations = 100;
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        // Act
        Parallel.For(0, iterations, i =>
        {
            try
            {
                if (i % 2 == 0)
                {
                    repository.UpdateCache([new FakeYubiKey($"device-{i}", ConnectionType.SmartCard)]);
                }
                else
                {
                    _ = repository.GetAll();
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        Assert.Empty(exceptions);
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
