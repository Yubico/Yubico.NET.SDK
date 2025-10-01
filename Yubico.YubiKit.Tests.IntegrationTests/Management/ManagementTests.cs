using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Core.Connections;
using Yubico.YubiKit.Core.Core.Protocols;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.IntegrationTests.Management;

public class ManagementTests : IntegrationTestBase
{
    [Fact]
    public async Task GetDeviceInfoAsync_with_Constructor()
    {
        var devices = await YubiKeyManager.GetYubiKeysAsync();
        var device = devices.FirstOrDefault();
        Assert.NotNull(device);
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
        var devices = await YubiKeyManager.GetYubiKeysAsync();
        var device = devices.FirstOrDefault();
        Assert.NotNull(device);
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
        var devices = await YubiKeyManager.GetYubiKeysAsync();
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