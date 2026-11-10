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

public class YubiKeyManagerWatchTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task WatchAsync_IsLazy_AndYieldsReplayCompletionThenHotplugChanges()
    {
        await using var fixture = new OwnedMonitorFixture();
        var first = new FakeYubiKey("first");
        var added = new FakeYubiKey("added");
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([first]);
        var stream = YubiKeyManager.WatchAsync(fixture.CreateAsync, TestToken);
        await using var watcher = stream.GetAsyncEnumerator(TestToken);
        Assert.False(fixture.Hid.Started || fixture.SmartCard.Started);
        Assert.Equal(0, fixture.Finder.ScanCount);

        Assert.True(await watcher.MoveNextAsync().AsTask().WaitAsync(Bound, TestToken));
        Assert.Same(first, Assert.IsType<DeviceAdded>(watcher.Current).Device);
        Assert.True(await watcher.MoveNextAsync());
        Assert.IsType<SnapshotComplete>(watcher.Current);
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([first, added]);
        await fixture.TriggerAsync(TestToken);
        Assert.True(await watcher.MoveNextAsync().AsTask().WaitAsync(Bound, TestToken));
        Assert.Same(added, Assert.IsType<DeviceAdded>(watcher.Current).Device);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WatchAsync_Cancellation_DisposesPrivateMonitor(bool tokenOnEnumerator)
    {
        await using var fixture = new OwnedMonitorFixture();
        var retired = false;
        fixture.Finder.OnRetire = () => retired = true;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var stream = YubiKeyManager.WatchAsync(fixture.CreateAsync, tokenOnEnumerator ? TestToken : cancellation.Token);
        await using var watcher = stream.GetAsyncEnumerator(tokenOnEnumerator ? cancellation.Token : TestToken);
        Assert.True(await watcher.MoveNextAsync().AsTask().WaitAsync(Bound, TestToken));
        Assert.IsType<SnapshotComplete>(watcher.Current);
        var pending = watcher.MoveNextAsync().AsTask();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(Bound, TestToken));
        Assert.True(fixture.Hid.Disposed && fixture.SmartCard.Disposed);
        Assert.True(retired);
        Assert.Null(fixture.Hid.Changed);
        Assert.Null(fixture.SmartCard.Changed);
    }

    [Fact]
    public async Task WatchAsync_BreakAfterSnapshotComplete_DisposesPrivateMonitor()
    {
        await using var fixture = new OwnedMonitorFixture();
        var retired = false;
        fixture.Finder.OnRetire = () => retired = true;
        await foreach (var change in YubiKeyManager.WatchAsync(fixture.CreateAsync, TestToken))
        {
            Assert.IsType<SnapshotComplete>(change);
            break;
        }
        Assert.True(fixture.Hid.Disposed && fixture.SmartCard.Disposed);
        Assert.True(retired);
    }

    [Fact]
    public async Task WatchAsync_FirstScanFailure_PropagatesFromFirstMoveAndCleansOwnership()
    {
        await using var fixture = new OwnedMonitorFixture();
        var failure = new IOException("first scan failed");
        var retired = false;
        fixture.Finder.OnRetire = () => retired = true;
        fixture.Finder.Scan = _ => throw failure;
        await using var watcher = YubiKeyManager.WatchAsync(fixture.CreateAsync, TestToken).GetAsyncEnumerator(TestToken);
        Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => watcher.MoveNextAsync().AsTask().WaitAsync(Bound, TestToken)));
        Assert.True(fixture.Hid.Disposed && fixture.SmartCard.Disposed);
        Assert.True(retired);
        Assert.Equal(1, fixture.Finder.ScanCount);
        fixture.SmartCard.Raise();
        fixture.Clock.Advance(fixture.Interval * 2);
        Assert.Equal(1, fixture.Finder.ScanCount);
    }

    [Fact]
    public async Task WatchAsync_ConsumerBodyThrows_DisposesPrivateMonitor()
    {
        await using var fixture = new OwnedMonitorFixture();
        var retired = false;
        fixture.Finder.OnRetire = () => retired = true;
        var failure = new InvalidOperationException("consumer failed");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var change in YubiKeyManager.WatchAsync(fixture.CreateAsync, TestToken))
            {
                Assert.IsType<SnapshotComplete>(change);
                throw failure;
            }
        });

        Assert.Same(failure, thrown);
        Assert.True(fixture.Hid.Disposed && fixture.SmartCard.Disposed);
        Assert.True(retired);
        var scans = fixture.Finder.ScanCount;
        fixture.SmartCard.Raise();
        fixture.Clock.Advance(fixture.Interval * 2);
        Assert.Equal(scans, fixture.Finder.ScanCount);
    }
}
