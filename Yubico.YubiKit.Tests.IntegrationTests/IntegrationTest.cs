using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Connections;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.IntegrationTests;

public class IntegrationTest : IntegrationTestBase
{
    [Fact]
    public async Task DeviceEvents_ArePublished()
    {
        var events = new List<YubiKeyDeviceEvent>();
        using var subscription = Manager.DeviceChanges.Subscribe(events.Add);

        // Plug in or remove a YubiKey to trigger events
        // You should see events appear in the 'events' list
        // You have 10 seconds to do this
        await Task.Delay(10000);

        Assert.True(events.Count > 0, $"Expected at least one device event to be published, but got {events.Count}.");
    }

    [Fact]
    public async Task GetPcscDevices()
    {
        var pcscDevices = await Manager.GetYubiKeysAsync();
        var pcscDevice = pcscDevices.FirstOrDefault();
        Assert.NotNull(pcscDevice);
    }

    [Fact]
    public async Task GetDeviceInfoAsync()
    {
        var pcscDevices = await Manager.GetYubiKeysAsync();
        var pcscDevice = pcscDevices.First();
        using var connection = await pcscDevice.ConnectAsync<ISmartCardConnection>();

        var logger = ServiceProvider.GetRequiredService<ILogger<ManagementSession<ISmartCardConnection>>>();
        var scpFactory = ServiceProvider.GetRequiredService<IProtocolFactory<ISmartCardConnection>>();
        using var mgmtSession = new ManagementSession<ISmartCardConnection>(logger, connection, scpFactory);

        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
    }

    [Fact]
    public async Task GetDeviceInfoAsync_WithDISession()
    {
        var pcscDevices = await Manager.GetYubiKeysAsync();
        var pcscDevice = pcscDevices.First();
        using var connection = await pcscDevice.ConnectAsync<ISmartCardConnection>();

        var managementSessionFactory =
            ServiceProvider.GetRequiredService<IManagementSessionFactory<ISmartCardConnection>>();
        using var mgmtSession = managementSessionFactory.Create(connection);

        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
    }
}