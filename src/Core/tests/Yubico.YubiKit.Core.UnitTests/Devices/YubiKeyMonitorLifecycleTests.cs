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

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.UnitTests.Infrastructure;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>Disposal-window regressions migrated from the removed global manager adapter.</summary>
public class YubiKeyMonitorLifecycleTests
{
    [Fact]
    public async Task DisposeAsync_ResumingAParkedPublicationAtRepositoryTeardown_EmitsNothing()
    {
        var repository = new YubiKeyDeviceRepository();
        var device = new FakeYubiKey("device-1", ConnectionType.SmartCard);
        var service = CreateService(repository, new FixedFinder(device));
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        // Change-only repository observation isolates deltas from the monitor's initial replay.
        await using var watcher = await DeviceEventWatcher.StartAsync(repository, TestContext.Current.CancellationToken);
        var admitted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        service.PublishAdmittedForTest = async () =>
        {
            admitted.SetResult();
            await release.Task;
        };
        var parked = service.RescanAsync(TestContext.Current.CancellationToken);
        try
        {
            await admitted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            monitor.RepositoryTeardownReachedForTest = async () =>
            {
                release.TrySetResult();
                await parked.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            };
            // The bounded service drain expires. The resumed diff must still see the intact cache,
            // not a live-but-emptied repository that would synthesize another Added event.
            await monitor.DisposeAsync();
            await watcher.Completion.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.True(watcher.EndedNormally);
            Assert.Empty(watcher.Events);
        }
        finally
        {
            release.TrySetResult();
            // Drain the publication before disposing its fake repository, even on runner cancellation.
            await parked.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
    }

    [Fact]
    public async Task DisposeAsync_WithAPublicationResumingAfterDisposeReturned_EmitsNothing()
    {
        var repository = new YubiKeyDeviceRepository();
        var device = new FakeYubiKey("device-1", ConnectionType.SmartCard);
        var finder = new FixedFinder(device) { Devices = [] };
        var service = CreateService(repository, finder);
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        // The abandoned publication would add a new device if it could reach a live repository.
        finder.Devices = [device];
        await using var watcher = await DeviceEventWatcher.StartAsync(repository, TestContext.Current.CancellationToken);
        var admitted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        service.PublishAdmittedForTest = async () =>
        {
            admitted.SetResult();
            await release.Task;
        };
        var parked = service.RescanAsync(TestContext.Current.CancellationToken);
        try
        {
            await admitted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            await monitor.DisposeAsync();
            release.TrySetResult();
            await parked.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            await watcher.Completion.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.True(watcher.EndedNormally);
            Assert.Empty(watcher.Events);
            Assert.False(repository.HasData);
            Assert.Throws<ObjectDisposedException>(() => repository.GetAll());
            Assert.Throws<ObjectDisposedException>(() => monitor.CurrentDevices);
        }
        finally
        {
            release.TrySetResult();
            // Drain the admitted publication even when the runner has canceled.
            await parked.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
    }

    private static YubiKeyDeviceMonitorService CreateService(YubiKeyDeviceRepository repository, IFindYubiKeys finder) =>
        new(repository, finder,
            () => throw new InvalidOperationException("No native HID listener"),
            () => throw new InvalidOperationException("No native PC/SC listener"),
            shutdownTimeout: TimeSpan.FromMilliseconds(250));

    private sealed class FixedFinder(IYubiKey device) : IFindYubiKeys
    {
        internal IReadOnlyList<IYubiKey> Devices { get; set; } = [device];
        public Task<IReadOnlyList<IYubiKey>> FindAllAsync(ConnectionType type, CancellationToken cancellationToken = default) =>
            Task.FromResult(Devices);
    }
}