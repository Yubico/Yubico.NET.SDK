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
using Yubico.YubiKit.Core.UnitTests.Infrastructure;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public class YubiKeyMonitorAcceptanceTests
{
    [Fact]
    public async Task DevicePicker_LoadsEmpty_TracksHotplugAndMetadata_AndSharesOneProducerWithIndependentSubscribers()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = new OwnedMonitorFixture();
        var monitor = await fixture.CreateAsync(ct);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        await using var first = new DevicePicker(monitor.WatchAsync(cancellation.Token), cancellation.Token);
        Assert.Equal(PickerState.Loading, first.State);
        Assert.IsType<SnapshotComplete>(await first.ReadAsync(ct));
        Assert.Equal(PickerState.LoadedEmpty, first.State);
        Assert.Equal(1, fixture.Finder.ScanCount);

        var original = Key(default(DeviceInfo) with { SerialNumber = 123, IsLocked = false });
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([original]);
        await fixture.TriggerAsync(ct);
        Assert.Same(original, Assert.IsType<DeviceAdded>(await first.ReadAsync(ct)).Device);
        Assert.Equal(PickerState.LoadedWithDevices, first.State);
        Assert.Same(original, Assert.Single(first.Devices));
        Assert.Equal(2, fixture.Finder.ScanCount);

        await using var late = new DevicePicker(monitor.WatchAsync(ct), ct);
        Assert.Equal(PickerState.Loading, late.State);
        var replay = Assert.IsType<DeviceAdded>(await late.ReadAsync(ct));
        Assert.Same(original, replay.Device);
        Assert.Equal(PickerState.Loading, late.State);
        var complete = Assert.IsType<SnapshotComplete>(await late.ReadAsync(ct));
        Assert.Equal(replay.Revision, complete.Revision);
        Assert.Equal(2, complete.Revision);
        Assert.Equal(PickerState.LoadedWithDevices, late.State);
        Assert.Equal(2, fixture.Finder.ScanCount); // Subscribing never starts discovery.

        var metadata = default(DeviceInfo) with { SerialNumber = 123, IsLocked = true };
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([Key(metadata)]);
        await fixture.TriggerAsync(ct);
        var firstUpdate = Assert.IsType<DeviceUpdated>(await first.ReadAsync(ct));
        var lateUpdate = Assert.IsType<DeviceUpdated>(await late.ReadAsync(ct));
        Assert.Same(original, firstUpdate.Device);
        Assert.Same(original, lateUpdate.Device);
        Assert.Equal(firstUpdate.Revision, lateUpdate.Revision);
        Assert.Equal(metadata, original.CachedDeviceInfo);
        Assert.Equal(3, fixture.Finder.ScanCount);

        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([]);
        await fixture.TriggerAsync(ct);
        Assert.Same(original, Assert.IsType<DeviceRemoved>(await first.ReadAsync(ct)).Device);
        Assert.Equal(PickerState.LoadedEmpty, first.State);
        Assert.Same(original, Assert.IsType<DeviceRemoved>(await late.ReadAsync(ct)).Device);
        Assert.Equal(PickerState.LoadedEmpty, late.State);
        Assert.Empty(late.Devices);
        Assert.Equal(4, fixture.Finder.ScanCount);

        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first.ReadAsync(ct));

        // The surviving view stays live after the other view has been canceled.
        var reinserted = Key(metadata);
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([reinserted]);
        await fixture.TriggerAsync(ct);
        Assert.Same(reinserted, Assert.IsType<DeviceAdded>(await late.ReadAsync(ct)).Device);
        Assert.Equal(PickerState.LoadedWithDevices, late.State);
        Assert.Equal(5, fixture.Finder.ScanCount);
    }

    private static YubiKeyDevice Key(DeviceInfo metadata) => new("picker", new Slot(), null, null, metadata);

    private sealed class Slot : IYubiKeyConnectionSlot
    {
        public string InterfaceId => "pcsc:picker";
        public ConnectionType ConnectionType => ConnectionType.SmartCard;
    }

    private enum PickerState { Loading, LoadedEmpty, LoadedWithDevices }

    // This consumer deliberately knows only the public stream, never the fixture or discovery.
    private sealed class DevicePicker(IAsyncEnumerable<DeviceChange> changes, CancellationToken cancellationToken) : IAsyncDisposable
    {
        private readonly IAsyncEnumerator<DeviceChange> _changes = changes.GetAsyncEnumerator(cancellationToken);
        private bool _loaded;
        public HashSet<IYubiKey> Devices { get; } = [];
        public PickerState State => !_loaded ? PickerState.Loading :
            Devices.Count == 0 ? PickerState.LoadedEmpty : PickerState.LoadedWithDevices;

        public async Task<DeviceChange> ReadAsync(CancellationToken ct)
        {
            Assert.True(await _changes.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), ct));
            var change = _changes.Current;
            _ = change switch
            {
                DeviceAdded added => Devices.Add(added.Device),
                DeviceRemoved removed => Devices.Remove(removed.Device),
                SnapshotComplete => _loaded = true,
                _ => true
            };
            return change;
        }

        public ValueTask DisposeAsync() => _changes.DisposeAsync();
    }
}