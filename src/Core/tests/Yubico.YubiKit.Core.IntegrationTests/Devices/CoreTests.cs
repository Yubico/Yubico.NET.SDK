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
        var first = enumerator.MoveNextAsync();

        YubiKeyManager.StartMonitoring();

        bool observed;
        try
        {
            observed = await first.AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (TimeoutException)
        {
            observed = false;
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