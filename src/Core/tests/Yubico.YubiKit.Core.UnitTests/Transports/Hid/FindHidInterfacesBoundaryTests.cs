using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Transports.Hid;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

public class FindHidInterfacesBoundaryTests
{
    [Fact]
    public async Task PreCancelledScanDoesNotSubmitEnumeration()
    {
        var calls = 0;
        var finder = new FindHidInterfaces(NullLogger<FindHidInterfaces>.Instance, () =>
        {
            Interlocked.Increment(ref calls);
            return [];
        });
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => finder.FindAllAsync(cancellation.Token));
        Assert.Equal(0, Volatile.Read(ref calls));
    }

    [Fact]
    public async Task WithheldScanReturnsPendingWithoutBlockingInvocationAndCancellationWaitsForEnumeration()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var calls = 0;
        var resourceHeld = 0;
        var finder = new FindHidInterfaces(NullLogger<FindHidInterfaces>.Instance, () =>
        {
            Interlocked.Increment(ref calls);
            Interlocked.Exchange(ref resourceHeld, 1);
            entered.Set();
            try
            {
                if (!release.Wait(TimeSpan.FromSeconds(10)))
                    throw new TimeoutException("Enumeration gate was not released.");
                return [];
            }
            finally
            {
                Interlocked.Exchange(ref resourceHeld, 0);
            }
        });
        using var cancellation = new CancellationTokenSource();
        // Use a separate caller thread so a regression that blocks before returning a task is bounded.
        var invocation = Task.Factory.StartNew(() => finder.FindAllAsync(cancellation.Token),
            CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            await ((Task)invocation).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            var scan = invocation.Result;
            Assert.False(scan.IsCompleted);

            cancellation.Cancel();
            Assert.False(scan.IsCompleted);
            Assert.Equal(1, Volatile.Read(ref calls));
            Assert.Equal(1, Volatile.Read(ref resourceHeld));

            release.Set();
            Assert.Empty(await scan.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.Equal(1, Volatile.Read(ref calls));
            Assert.Equal(0, Volatile.Read(ref resourceHeld));
        }
        finally
        {
            release.Set();
            await ((Task)invocation).WaitAsync(TimeSpan.FromSeconds(15));
            await invocation.Result.WaitAsync(TimeSpan.FromSeconds(15));
        }
    }

    [Fact]
    public async Task EnumerationFailureIsPreservedWithoutReplay()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var calls = 0;
        var failure = new InvalidOperationException("enumeration failed");
        var finder = new FindHidInterfaces(NullLogger<FindHidInterfaces>.Instance, () =>
        {
            Interlocked.Increment(ref calls);
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Enumeration gate was not released.");
            throw failure;
        });
        var scan = finder.FindAllAsync(TestContext.Current.CancellationToken);

        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.False(scan.IsCompleted);
            release.Set();
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(
                () => scan.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)));
            Assert.Equal(1, Volatile.Read(ref calls));
        }
        finally
        {
            release.Set();
            try
            {
                await scan.WaitAsync(TimeSpan.FromSeconds(15));
            }
            catch (InvalidOperationException ex) when (ReferenceEquals(ex, failure))
            {
                // The asserted enumeration failure has already been observed.
            }
        }
    }
}
