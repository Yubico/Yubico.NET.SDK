using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Core.IntegrationTests.Core;

public class CoreTests : IAsyncLifetime
{
    public Task InitializeAsync()
    {
        YubiKeyManager.StartMonitoring();
        return Task.CompletedTask;
    }
    
    public async Task DisposeAsync() => await YubiKeyManager.ShutdownAsync();
    
    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task DeviceEvents_ArePublished()
    {
        var events = new List<DeviceEvent>();
        using var subscription = YubiKeyManager.DeviceChanges.Subscribe(events.Add);

        // Plug in or remove a YubiKey to trigger events
        // You should see events appear in the 'events' list
        // You have 10 seconds to do this
        await Task.Delay(10000);

        Assert.True(events.Count > 0, $"Expected at least one device event to be published, but got {events.Count}.");
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task GetPcscDevices()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.SmartCard);
        var device = devices.FirstOrDefault();
        Assert.NotNull(device);
    }
}