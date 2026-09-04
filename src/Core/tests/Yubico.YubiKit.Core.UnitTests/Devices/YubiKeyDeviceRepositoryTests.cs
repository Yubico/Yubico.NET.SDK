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

using System.Diagnostics;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.UnitTests.Infrastructure;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>
/// Tests for <see cref="YubiKeyDeviceRepository"/> - pure cache with diff-based events.
/// </summary>
public class YubiKeyDeviceRepositoryTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task UpdateCache_EmptyToDevices_EmitsAddedEvents()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        using var cts = new CancellationTokenSource(Bound);
        await using var watcher = await DeviceEventWatcher.StartAsync(repository, cts.Token);

        var device1 = new FakeYubiKey("device-1", ConnectionType.SmartCard);
        var device2 = new FakeYubiKey("device-2", ConnectionType.HidFido);

        // Act
        repository.UpdateCache([device1, device2]);

        // Assert
        await watcher.WaitForCountAsync(2, "both arrivals did not reach the watcher", cts.Token);
        var events = watcher.Events;
        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Equal(DeviceAction.Added, e.Action));
        Assert.Contains(events, e => e.Device.DeviceId == "device-1");
        Assert.Contains(events, e => e.Device.DeviceId == "device-2");
    }

    [Fact]
    public async Task UpdateCache_DevicesToEmpty_EmitsRemovedEvents()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        using var cts = new CancellationTokenSource(Bound);
        var device1 = new FakeYubiKey("device-1", ConnectionType.SmartCard);
        var device2 = new FakeYubiKey("device-2", ConnectionType.HidFido);
        repository.UpdateCache([device1, device2]);

        await using var watcher = await DeviceEventWatcher.StartAsync(repository, cts.Token);

        // Act
        repository.UpdateCache([]);

        // Assert
        await watcher.WaitForCountAsync(2, "both removals did not reach the watcher", cts.Token);
        var events = watcher.Events;
        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Equal(DeviceAction.Removed, e.Action));
        Assert.Contains(events, e => e.Device.DeviceId == "device-1");
        Assert.Contains(events, e => e.Device.DeviceId == "device-2");
    }

    [Fact]
    public async Task UpdateCache_DifferentDevices_EmitsCorrectAddedAndRemoved()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        using var cts = new CancellationTokenSource(Bound);
        var deviceA = new FakeYubiKey("device-A", ConnectionType.SmartCard);
        var deviceB = new FakeYubiKey("device-B", ConnectionType.HidFido);
        repository.UpdateCache([deviceA, deviceB]);

        await using var watcher = await DeviceEventWatcher.StartAsync(repository, cts.Token);

        var deviceC = new FakeYubiKey("device-C", ConnectionType.SmartCard);
        var deviceD = new FakeYubiKey("device-D", ConnectionType.HidOtp);

        // Act: Replace A,B with C,D
        repository.UpdateCache([deviceC, deviceD]);

        // Assert
        await watcher.WaitForCountAsync(4, "the full swap did not reach the watcher", cts.Token);
        var events = watcher.Events;
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
    public async Task UpdateCache_SameDevices_NoEvents()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        using var cts = new CancellationTokenSource(Bound);
        var device1 = new FakeYubiKey("device-1", ConnectionType.SmartCard);
        repository.UpdateCache([device1]);

        await using var watcher = await DeviceEventWatcher.StartAsync(repository, cts.Token);

        // Act: Update with same device ID
        var device1Updated = new FakeYubiKey("device-1", ConnectionType.SmartCard);
        repository.UpdateCache([device1Updated]);

        // Assert: No events since device ID hasn't changed
        Assert.Empty(await watcher.DrainAsync(repository, cts.Token));
    }

    [Fact]
    public async Task UpdateCache_PartialOverlap_EmitsOnlyChanges()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        using var cts = new CancellationTokenSource(Bound);
        var deviceA = new FakeYubiKey("device-A", ConnectionType.SmartCard);
        var deviceB = new FakeYubiKey("device-B", ConnectionType.HidFido);
        repository.UpdateCache([deviceA, deviceB]);

        await using var watcher = await DeviceEventWatcher.StartAsync(repository, cts.Token);

        var deviceC = new FakeYubiKey("device-C", ConnectionType.SmartCard);

        // Act: Keep B, remove A, add C
        repository.UpdateCache([deviceB, deviceC]);

        // Assert
        await watcher.WaitForCountAsync(2, "the partial swap did not reach the watcher", cts.Token);
        var events = watcher.Events;
        Assert.Equal(2, events.Count);
        Assert.Single(events, e => e.Action == DeviceAction.Removed && e.Device.DeviceId == "device-A");
        Assert.Single(events, e => e.Action == DeviceAction.Added && e.Device.DeviceId == "device-C");
    }

    // ---------- WatchAsync delivery contract ----------

    /// <summary>ISC-5: concurrent watchers are independent and see the same ordered sequence.</summary>
    [Fact]
    public async Task WatchAsync_TwoConcurrentWatchers_ReceiveTheSameOrderedSequence()
    {
        using var repository = new YubiKeyDeviceRepository();
        using var cts = new CancellationTokenSource(Bound);
        await using var first = await DeviceEventWatcher.StartAsync(repository, cts.Token);
        await using var second = await DeviceEventWatcher.StartAsync(repository, cts.Token);

        Assert.Equal(2, repository.WatcherCount);

        var deviceA = new FakeYubiKey("device-a", ConnectionType.SmartCard);
        repository.UpdateCache([deviceA]);
        repository.UpdateCache([]);
        repository.UpdateCache([new FakeYubiKey("device-b", ConnectionType.SmartCard)]);

        await first.WaitForCountAsync(3, "first watcher did not receive every event", cts.Token);
        await second.WaitForCountAsync(3, "second watcher did not receive every event", cts.Token);

        var expected = new[]
        {
            (DeviceAction.Added, "device-a"),
            (DeviceAction.Removed, "device-a"),
            (DeviceAction.Added, "device-b")
        };

        Assert.Equal(expected, first.Events.Select(e => (e.Action, e.Device.DeviceId)));
        Assert.Equal(expected, second.Events.Select(e => (e.Action, e.Device.DeviceId)));
    }

    /// <summary>ISC-6: cancellation is per-watcher; it neither completes nor faults the others.</summary>
    [Fact]
    public async Task WatchAsync_CancellingOneWatcher_DoesNotDisturbAnother()
    {
        using var repository = new YubiKeyDeviceRepository();
        using var survivorCts = new CancellationTokenSource(Bound);
        using var doomedCts = new CancellationTokenSource();

        await using var survivor = await DeviceEventWatcher.StartAsync(repository, survivorCts.Token);
        var doomed = await DeviceEventWatcher.StartAsync(repository, doomedCts.Token);

        await doomedCts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => doomed.Completion);
        await doomed.DisposeAsync();

        repository.UpdateCache([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);

        await survivor.WaitForCountAsync(1, "the surviving watcher stopped receiving events", survivorCts.Token);
        Assert.False(survivor.Completion.IsCompleted);
    }

    /// <summary>ISC-7: overflow terminates only the watcher whose own buffer filled.</summary>
    [Fact]
    public async Task WatchAsync_WhenOneWatcherOverflows_OnlyThatWatcherFaults()
    {
        using var repository = new YubiKeyDeviceRepository();
        using var cts = new CancellationTokenSource(Bound);
        using var stall = new SemaphoreSlim(0, 1);

        var overflowing = Task.Run(
            async () =>
            {
                var stalled = false;
                await foreach (var _ in repository.WatchAsync(cts.Token))
                {
                    if (!stalled)
                    {
                        stalled = true;
                        await stall.WaitAsync(cts.Token);
                    }
                }
            },
            cts.Token);

        await AsyncWait.WaitUntilAsync(
            () => repository.WatcherCount == 1,
            "the stalling watcher did not subscribe",
            Bound,
            cts.Token);

        // Each cycle emits Removed + Added, so this overruns the 256-event buffer several times over.
        for (var i = 0; i < DeviceEventHub.WatcherBufferCapacity; i++)
        {
            repository.UpdateCache([new FakeYubiKey($"device-{i}", ConnectionType.SmartCard)]);
        }

        stall.Release();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => overflowing);
        Assert.Contains("FindAllAsync", ex.Message, StringComparison.Ordinal);

        // The publisher is unharmed and a fresh watcher still receives everything after the fault.
        await using var healthy = await DeviceEventWatcher.StartAsync(repository, cts.Token);
        repository.UpdateCache([new FakeYubiKey("after-overflow", ConnectionType.SmartCard)]);

        await healthy.WaitForCountAsync(2, "publication did not continue after an overflow", cts.Token);
        Assert.Contains(healthy.Events, e => e.Action == DeviceAction.Added && e.Device.DeviceId == "after-overflow");
    }

    /// <summary>ISC-9: <c>WatchAsync</c> subscribes on first enumeration, not when it is called.</summary>
    [Fact]
    public void WatchAsync_WithoutEnumerating_CreatesNoSubscription()
    {
        using var repository = new YubiKeyDeviceRepository();

        _ = repository.WatchAsync(CancellationToken.None);
        _ = repository.WatchAsync(CancellationToken.None);

        Assert.Equal(0, repository.WatcherCount);
    }

    /// <summary>
    /// ISC-10: the publication path is never handed to a consumer, so a stalled, abandoned, or
    /// throwing watcher can neither block <see cref="YubiKeyDeviceRepository.UpdateCache"/> nor
    /// interrupt it.
    /// </summary>
    [Fact]
    public async Task UpdateCache_WithStalledAbandonedAndThrowingWatchers_IsNeitherBlockedNorInterrupted()
    {
        using var repository = new YubiKeyDeviceRepository();
        using var cts = new CancellationTokenSource(Bound);

        // Abandoned: enumerated once to subscribe, then never pumped again.
        var abandoned = repository.WatchAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        var abandonedFirstMove = abandoned.MoveNextAsync();

        // Throwing: the consumer's own loop body throws on its first event.
        var throwing = Task.Run(
            async () =>
            {
                await foreach (var _ in repository.WatchAsync(cts.Token))
                {
                    throw new InvalidOperationException("consumer bug");
                }
            },
            cts.Token);

        await AsyncWait.WaitUntilAsync(
            () => repository.WatcherCount == 2,
            "the misbehaving watchers did not subscribe",
            Bound,
            cts.Token);

        // Far more events than either watcher's 256-event buffer can hold.
        var elapsed = Stopwatch.StartNew();
        for (var i = 0; i < DeviceEventHub.WatcherBufferCapacity + 50; i++)
        {
            repository.UpdateCache([new FakeYubiKey($"device-{i}", ConnectionType.SmartCard)]);
        }

        elapsed.Stop();

        Assert.True(
            elapsed.Elapsed < TimeSpan.FromSeconds(10),
            $"UpdateCache was stalled by a misbehaving watcher for {elapsed.Elapsed}");

        // The consumer's exception stayed inside the consumer.
        var consumerFault = await Assert.ThrowsAsync<InvalidOperationException>(() => throwing);
        Assert.Equal("consumer bug", consumerFault.Message);

        // Publication is still healthy afterwards.
        await using var healthy = await DeviceEventWatcher.StartAsync(repository, cts.Token);
        repository.UpdateCache([new FakeYubiKey("still-publishing", ConnectionType.SmartCard)]);
        await healthy.WaitForCountAsync(2, "publication stopped after misbehaving watchers", cts.Token);

        Assert.True(await abandonedFirstMove);
        await abandoned.DisposeAsync();
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
        Assert.All(result, d => Assert.Equal(ConnectionType.SmartCard, d.AvailableConnections));
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
    public void GetAll_WithHid_ReturnsHidFidoAndHidOtpDevices()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        var device1 = new FakeYubiKey("device-1", ConnectionType.SmartCard);
        var device2 = new FakeYubiKey("device-2", ConnectionType.HidFido);
        var device3 = new FakeYubiKey("device-3", ConnectionType.HidOtp);
        repository.UpdateCache([device1, device2, device3]);

        // Act
        var result = repository.GetAll(ConnectionType.Hid);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.DeviceId == "device-2");
        Assert.Contains(result, d => d.DeviceId == "device-3");
    }

    [Fact]
    public void GetAll_WithCombinedFilter_ReturnsMatchingDevices()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        repository.UpdateCache([
            new FakeYubiKey("smartcard", ConnectionType.SmartCard),
            new FakeYubiKey("fido", ConnectionType.HidFido),
            new FakeYubiKey("otp", ConnectionType.HidOtp)
        ]);

        // Act
        var result = repository.GetAll(ConnectionType.SmartCard | ConnectionType.HidFido);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.DeviceId == "smartcard");
        Assert.Contains(result, d => d.DeviceId == "fido");
    }

    [Fact]
    public void GetAll_WithSpecificHidFilter_DoesNotReturnGenericHidDevice()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        repository.UpdateCache([
            new FakeYubiKey("generic-hid", ConnectionType.Hid),
            new FakeYubiKey("fido", ConnectionType.HidFido)
        ]);

        // Act
        var result = repository.GetAll(ConnectionType.HidFido);

        // Assert
        Assert.Single(result);
        Assert.Equal("fido", result[0].DeviceId);
    }

    [Fact]
    public void GetAll_WithUnknown_ReturnsEmptyList()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        repository.UpdateCache([
            new FakeYubiKey("device-1", ConnectionType.SmartCard),
            new FakeYubiKey("device-2", ConnectionType.HidFido)
        ]);

        // Act
        var result = repository.GetAll(ConnectionType.Unknown);

        // Assert
        Assert.Empty(result);
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



    /// <summary>ISC-8: disposal ends active watchers normally — not faulted, not cancelled.</summary>
    [Fact]
    public async Task Dispose_CompletesActiveWatchersNormally()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        using var cts = new CancellationTokenSource(Bound);
        await using var first = await DeviceEventWatcher.StartAsync(repository, cts.Token);
        await using var second = await DeviceEventWatcher.StartAsync(repository, cts.Token);

        // Act
        repository.Dispose();

        // Assert
        await first.Completion.WaitAsync(Bound, cts.Token);
        await second.Completion.WaitAsync(Bound, cts.Token);
        Assert.True(first.EndedNormally);
        Assert.True(second.EndedNormally);
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
    public async Task Clear_DoesNotEmitEvents()
    {
        // Arrange
        using var repository = new YubiKeyDeviceRepository();
        using var cts = new CancellationTokenSource(Bound);
        repository.UpdateCache([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);

        await using var watcher = await DeviceEventWatcher.StartAsync(repository, cts.Token);

        // Act
        repository.Clear();

        // Assert: Clear is silent (no events)
        Assert.Empty(await watcher.DrainAsync(repository, cts.Token));
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
}