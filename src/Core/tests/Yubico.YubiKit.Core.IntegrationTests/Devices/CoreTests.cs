using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.IntegrationTests.Devices;

public class CoreTests
{
    /// <summary>Verifies delivery of the initial device scan to a watcher against real hardware.</summary>
    /// <remarks>
    /// <see cref="YubiKeyMonitor.WatchAsync"/> replays the initialized snapshot on first enumeration.
    /// </remarks>
    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task WatchAsync_ReceivesEventsFromTheInitialScan()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var monitor = await YubiKeyManager.CreateMonitorAsync(cts.Token);
        await using var enumerator = monitor.WatchAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        var first = enumerator.MoveNextAsync().AsTask();

        bool observed;
        try
        {
            observed = await first.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (TimeoutException)
        {
            observed = false;

            // `first` is still in flight, and disposing an async iterator with a MoveNextAsync
            // outstanding throws NotSupportedException — which would surface instead of the assertion
            // below and hide what actually went wrong. Cancel and let the pending move retire first.
            await cts.CancelAsync();
            try
            {
                _ = await first;
            }
            catch (OperationCanceledException)
            {
                // Expected: cancellation is how the in-flight MoveNextAsync is retired.
            }
        }

        Assert.True(observed, "Expected at least one device event to reach the watcher.");
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task FindFirstOrDefaultAsync_FindsSmartCardDevice()
    {
        var device = await TransientScanRetry.ScanAsync(
            () => YubiKeyManager.FindFirstOrDefaultAsync(
                candidate => candidate.SupportsConnection(ConnectionType.SmartCard)));
        Assert.NotNull(device);
    }
}