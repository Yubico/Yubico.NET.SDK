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
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(5);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task DisposeAsync_ResumingAParkedPublicationAtRepositoryTeardown_EmitsNothing()
    {
        await using var fixture = new OwnedMonitorFixture();
        var device = new FakeYubiKey("device-1", ConnectionType.SmartCard);
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([device]);
        var monitor = await fixture.CreateAsync(TestToken);
        // Change-only observation isolates deltas from the monitor's initial replay.
        await using var watcher = await DeviceEventWatcher.StartAsync(fixture.Repository, TestToken);
        var admitted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Worker.PublishAdmittedForTest = () =>
        {
            admitted.TrySetResult();
            return release.Task;
        };
        fixture.SmartCard.Raise();
        try
        {
            await admitted.Task.WaitAsync(Bound, TestToken);
            monitor.RepositoryTeardownReachedForTest = async () =>
            {
                release.TrySetResult();
                await fixture.Loop.WaitAsync(Bound, TestToken);
            };
            // After the bounded drain expires, the resumed diff still sees intact membership,
            // not a live-but-emptied repository that would synthesize another Added event.
            await monitor.DisposeAsync();
            await watcher.Completion.WaitAsync(Bound, TestToken);
            Assert.True(watcher.EndedNormally);
            Assert.Empty(watcher.Events);
        }
        finally
        {
            release.TrySetResult();
            // The actual loop owns the publication until it finishes, even on runner cancellation.
            await monitor.DisposeAsync();
            await fixture.Loop.WaitAsync(Bound, CancellationToken.None);
        }
    }

    [Fact]
    public async Task DisposeAsync_WithAPublicationResumingAfterDisposeReturned_EmitsNothing()
    {
        await using var fixture = new OwnedMonitorFixture();
        var monitor = await fixture.CreateAsync(TestToken);
        var device = new FakeYubiKey("device-1", ConnectionType.SmartCard);
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([device]);
        await using var watcher = await DeviceEventWatcher.StartAsync(fixture.Repository, TestToken);
        var admitted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Worker.PublishAdmittedForTest = () =>
        {
            admitted.TrySetResult();
            return release.Task;
        };
        fixture.SmartCard.Raise();
        try
        {
            await admitted.Task.WaitAsync(Bound, TestToken);
            await monitor.DisposeAsync();
            release.TrySetResult();
            await fixture.Loop.WaitAsync(Bound, TestToken);
            await watcher.Completion.WaitAsync(Bound, TestToken);
            Assert.True(watcher.EndedNormally);
            Assert.Empty(watcher.Events);
            Assert.False(fixture.Repository.HasData);
            Assert.Throws<ObjectDisposedException>(() => fixture.Repository.GetAll());
            Assert.Throws<ObjectDisposedException>(() => monitor.CurrentDevices);
        }
        finally
        {
            release.TrySetResult();
            // Drain the real loop before the fake lifetime ends, even when the runner has canceled.
            await monitor.DisposeAsync();
            await fixture.Loop.WaitAsync(Bound, CancellationToken.None);
        }
    }
}