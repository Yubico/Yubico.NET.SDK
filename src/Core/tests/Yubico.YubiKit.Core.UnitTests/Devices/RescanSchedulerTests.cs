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

using System.Threading.Channels;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.UnitTests.Infrastructure;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public class RescanSchedulerTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RunAsync_YieldsInitialWithoutAdvancingTime()
    {
        var clock = new ObservedTimeProvider();
        var started = clock.GetTimestamp();
        var signals = Channel.CreateUnbounded<ConnectionType>();
        var scheduler = new RescanScheduler(signals.Reader, Interval, clock);
        await using var iterator = scheduler.RunAsync(TestToken).GetAsyncEnumerator(TestToken);

        Assert.True(await iterator.MoveNextAsync().AsTask().WaitAsync(Bound, TestToken));
        Assert.Equal(RescanTrigger.Initial, iterator.Current);
        Assert.Equal(TimeSpan.Zero, clock.GetElapsedTime(started));
    }

    [Fact]
    public async Task RunAsync_NoSignals_YieldsIntervalAtDeadline()
    {
        var clock = new ObservedTimeProvider();
        var signals = Channel.CreateUnbounded<ConnectionType>();
        var scheduler = new RescanScheduler(signals.Reader, Interval, clock);
        await using var iterator = scheduler.RunAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await iterator.MoveNextAsync().AsTask().WaitAsync(Bound, TestToken));

        var next = iterator.MoveNextAsync().AsTask();
        await clock.WaitForTimerAsync(Interval);
        clock.Advance(Interval - TimeSpan.FromMilliseconds(1));
        Assert.False(next.IsCompleted);
        clock.Advance(TimeSpan.FromMilliseconds(1));

        Assert.True(await next.WaitAsync(Bound, TestToken));
        Assert.Equal(RescanTrigger.Interval, iterator.Current);
    }

    [Fact]
    public async Task RunAsync_OneSignal_WaitsForQuietPeriod()
    {
        var clock = new ObservedTimeProvider();
        var signals = Channel.CreateUnbounded<ConnectionType>();
        var scheduler = new RescanScheduler(signals.Reader, Interval, clock);
        await using var iterator = scheduler.RunAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await iterator.MoveNextAsync().AsTask().WaitAsync(Bound, TestToken));
        Assert.True(signals.Writer.TryWrite(ConnectionType.Hid));

        var next = iterator.MoveNextAsync().AsTask();
        await clock.WaitForTimerAsync(Interval);
        await clock.WaitForTimerAsync(RescanScheduler.QuietPeriod);
        clock.Advance(RescanScheduler.QuietPeriod - TimeSpan.FromMilliseconds(1));
        Assert.False(next.IsCompleted);
        clock.Advance(TimeSpan.FromMilliseconds(1));

        Assert.True(await next.WaitAsync(Bound, TestToken));
        Assert.Equal(RescanTrigger.Hotplug(ConnectionType.Hid), iterator.Current);
    }

    [Fact]
    public async Task RunAsync_TwoSignals_RearmsQuietPeriodAndCombinesTransports()
    {
        var clock = new ObservedTimeProvider();
        var signals = Channel.CreateUnbounded<ConnectionType>();
        var scheduler = new RescanScheduler(signals.Reader, Interval, clock);
        await using var iterator = scheduler.RunAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await iterator.MoveNextAsync().AsTask().WaitAsync(Bound, TestToken));
        Assert.True(signals.Writer.TryWrite(ConnectionType.Hid));
        var next = iterator.MoveNextAsync().AsTask();
        await clock.WaitForTimerAsync(Interval);
        await clock.WaitForTimerAsync(RescanScheduler.QuietPeriod);

        clock.Advance(TimeSpan.FromMilliseconds(100));
        Assert.True(signals.Writer.TryWrite(ConnectionType.SmartCard));
        await clock.WaitForTimerAsync(RescanScheduler.QuietPeriod);
        clock.Advance(RescanScheduler.QuietPeriod - TimeSpan.FromMilliseconds(1));
        Assert.False(next.IsCompleted);
        clock.Advance(TimeSpan.FromMilliseconds(1));

        Assert.True(await next.WaitAsync(Bound, TestToken));
        Assert.Equal(RescanTrigger.Hotplug(ConnectionType.Hid | ConnectionType.SmartCard), iterator.Current);
    }

    [Fact]
    public async Task RunAsync_ContinuousSignals_YieldsAtCoalesceCap()
    {
        var clock = new ObservedTimeProvider();
        var started = clock.GetTimestamp();
        var signals = Channel.CreateUnbounded<ConnectionType>();
        var scheduler = new RescanScheduler(signals.Reader, Interval, clock);
        await using var iterator = scheduler.RunAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await iterator.MoveNextAsync().AsTask().WaitAsync(Bound, TestToken));
        Assert.True(signals.Writer.TryWrite(ConnectionType.Hid));
        var next = iterator.MoveNextAsync().AsTask();
        await clock.WaitForTimerAsync(Interval);
        await clock.WaitForTimerAsync(RescanScheduler.QuietPeriod);

        for (var elapsed = 100; elapsed < 1000; elapsed += 100)
        {
            clock.Advance(TimeSpan.FromMilliseconds(100));
            Assert.False(next.IsCompleted);
            Assert.True(signals.Writer.TryWrite(ConnectionType.Hid));
            await clock.WaitForTimerAsync(TimeSpan.FromMilliseconds(Math.Min(200, 1000 - elapsed)));
        }
        clock.Advance(TimeSpan.FromMilliseconds(99));
        Assert.False(next.IsCompleted);
        clock.Advance(TimeSpan.FromMilliseconds(1));

        Assert.True(await next.WaitAsync(Bound, TestToken));
        Assert.Equal(RescanScheduler.MaxCoalesce, clock.GetElapsedTime(started));
        Assert.Equal(RescanTrigger.Hotplug(ConnectionType.Hid), iterator.Current);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunAsync_ChannelCompletion_EndsNormally(bool duringCoalescing)
    {
        var clock = new ObservedTimeProvider();
        var signals = Channel.CreateUnbounded<ConnectionType>();
        var scheduler = new RescanScheduler(signals.Reader, Interval, clock);
        await using var iterator = scheduler.RunAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await iterator.MoveNextAsync().AsTask().WaitAsync(Bound, TestToken));
        if (duringCoalescing)
            Assert.True(signals.Writer.TryWrite(ConnectionType.Hid));
        var next = iterator.MoveNextAsync().AsTask();
        await clock.WaitForTimerAsync(Interval);
        if (duringCoalescing)
            await clock.WaitForTimerAsync(RescanScheduler.QuietPeriod);

        signals.Writer.Complete();

        Assert.False(await next.WaitAsync(Bound, TestToken));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunAsync_Cancellation_Throws(bool duringCoalescing)
    {
        var clock = new ObservedTimeProvider();
        var signals = Channel.CreateUnbounded<ConnectionType>();
        var scheduler = new RescanScheduler(signals.Reader, Interval, clock);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        await using var iterator = scheduler.RunAsync(cancellation.Token).GetAsyncEnumerator(cancellation.Token);
        Assert.True(await iterator.MoveNextAsync().AsTask().WaitAsync(Bound, TestToken));
        if (duringCoalescing)
            Assert.True(signals.Writer.TryWrite(ConnectionType.Hid));
        var next = iterator.MoveNextAsync().AsTask();
        await clock.WaitForTimerAsync(Interval);
        if (duringCoalescing)
            await clock.WaitForTimerAsync(RescanScheduler.QuietPeriod);

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => next.WaitAsync(Bound, TestToken));
    }

    [Fact]
    public async Task RunAsync_HotplugDuringInterval_StartsFreshIntervalAfterHotplug()
    {
        var clock = new ObservedTimeProvider();
        var signals = Channel.CreateUnbounded<ConnectionType>();
        var scheduler = new RescanScheduler(signals.Reader, Interval, clock);
        await using var iterator = scheduler.RunAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await iterator.MoveNextAsync().AsTask().WaitAsync(Bound, TestToken));
        var hotplug = iterator.MoveNextAsync().AsTask();
        await clock.WaitForTimerAsync(Interval);
        clock.Advance(TimeSpan.FromSeconds(4));
        Assert.True(signals.Writer.TryWrite(ConnectionType.Hid));
        await clock.WaitForTimerAsync(RescanScheduler.QuietPeriod);
        clock.Advance(RescanScheduler.QuietPeriod);
        Assert.True(await hotplug.WaitAsync(Bound, TestToken));
        Assert.Equal(RescanTrigger.Hotplug(ConnectionType.Hid), iterator.Current);

        var next = iterator.MoveNextAsync().AsTask();
        await clock.WaitForTimerAsync(Interval);
        clock.Advance(Interval - TimeSpan.FromMilliseconds(1));
        Assert.False(next.IsCompleted);
        clock.Advance(TimeSpan.FromMilliseconds(1));

        Assert.True(await next.WaitAsync(Bound, TestToken));
        Assert.Equal(RescanTrigger.Interval, iterator.Current);
    }

}