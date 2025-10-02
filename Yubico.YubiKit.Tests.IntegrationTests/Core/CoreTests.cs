using Yubico.YubiKit.Core;

namespace Yubico.YubiKit.IntegrationTests.Core;

public class CoreTests : IntegrationTestBase
{
    [Fact]
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
    public async Task GetPcscDevices()
    {
        var devices = await YubiKeyManager.FindAllAsync();
        var device = devices.FirstOrDefault();
        Assert.NotNull(device);
    }
}