using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Connections;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Management;

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
        var devices = await Manager.GetYubiKeysAsync();
        var device = devices.FirstOrDefault();
        Assert.NotNull(device);
    }

    [Fact]
    public async Task GetDeviceInfoAsync_with_Constructor()
    {
        var devices = await Manager.GetYubiKeysAsync();
        var device = devices.First();
        using var connection = await device.ConnectAsync<ISmartCardConnection>();

        // Collect dependencies yourself!
        var logger = ServiceProvider.GetRequiredService<ILogger<ManagementSession<ISmartCardConnection>>>();
        var scpFactory = ServiceProvider.GetRequiredService<IProtocolFactory<ISmartCardConnection>>();
        using var mgmtSession = new ManagementSession<ISmartCardConnection>(logger, connection, scpFactory);
        await mgmtSession.InitializeAsync();

        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
    }

    [Fact]
    public async Task GetDeviceInfoAsync_with_FactoryMethod()
    {
        var devices = await Manager.GetYubiKeysAsync();
        var device = devices.First();
        using var connection = await device.ConnectAsync<ISmartCardConnection>();

        // Collect dependencies yourself!
        var logger = ServiceProvider.GetRequiredService<ILogger<ManagementSession<ISmartCardConnection>>>();
        var protocolFactory = ServiceProvider.GetRequiredService<IProtocolFactory<ISmartCardConnection>>();
        using var mgmtSession =
            await ManagementSession<ISmartCardConnection>.CreateAsync(logger, connection, protocolFactory);

        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
    }

    [Fact]
    public async Task GetDeviceInfoAsync_with_FactoryClass()
    {
        var devices = await Manager.GetYubiKeysAsync();
        var device = devices.First();
        using var connection = await device.ConnectAsync<ISmartCardConnection>();

        // Just get the correct factory
        var managementSessionFactory =
            ServiceProvider.GetRequiredService<IManagementSessionFactory<ISmartCardConnection>>();
        using var mgmtSession = await managementSessionFactory.CreateAsync(connection);

        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
    }
}