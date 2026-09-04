using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Core.IntegrationTests.Devices;

public class CoreTests : IAsyncLifetime
{
    /// <summary>Resets the static manager so each test starts with an empty device cache.</summary>
    public async Task InitializeAsync() => await YubiKeyManager.ShutdownAsync();

    public async Task DisposeAsync() => await YubiKeyManager.ShutdownAsync();

    /// <summary>Verifies delivery of the initial device scan to a watcher against real hardware.</summary>
    /// <remarks>
    /// <see cref="YubiKeyManager.WatchAsync"/> subscribes on the first <c>MoveNextAsync</c>, so the
    /// enumeration is started before monitoring starts; that is how it observes the initial scan.
    /// </remarks>
    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task WatchAsync_ReceivesEventsFromTheInitialScan()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var enumerator = YubiKeyManager.WatchAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        var first = enumerator.MoveNextAsync().AsTask();

        YubiKeyManager.StartMonitoring();

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
    public async Task GetPcscDevices()
    {
        var devices = await TransientScanRetry.ScanAsync(
            () => YubiKeyManager.FindAllAsync(ConnectionType.SmartCard));
        var device = devices.FirstOrDefault();
        Assert.NotNull(device);
    }
}