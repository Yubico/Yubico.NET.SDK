using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Connections;
using Yubico.YubiKit.Core.Protocols;

namespace Yubico.YubiKit.IntegrationTests;

public class IntegrationTest : IntegrationTestBase
{
    [Fact]
    public async Task GetPcscDevices()
    {
        var pcscDevices = await YubiKeyManager.GetYubiKeys();
        var pcscDevice = pcscDevices.FirstOrDefault();
        Assert.NotNull(pcscDevice);
    }

    [Fact]
    public async Task GetDeviceInfo()
    {
        var pcscDevices = await YubiKeyManager.GetYubiKeys();
        var pcscDevice = pcscDevices.First();
        using var connection = await pcscDevice.ConnectAsync<ISmartCardConnection>();

        var logger = ServiceProvider.GetRequiredService<ILogger<ManagementSession<ISmartCardConnection>>>();
        var scpFactory = ServiceProvider.GetRequiredService<IProtocolFactory<ISmartCardConnection, IProtocol>>();

        using var mgmtSession = new ManagementSession<ISmartCardConnection>(logger, connection, scpFactory);
        var deviceInfo = mgmtSession.GetDeviceInfo();
    }

    [Fact]
    public async Task GetDeviceInfoWithDISession()
    {
        var pcscDevices = await YubiKeyManager.GetYubiKeys();
        var pcscDevice = pcscDevices.First();
        using var connection = await pcscDevice.ConnectAsync<ISmartCardConnection>();

        var managementSessionFactory =
            ServiceProvider.GetRequiredService<IManagementSessionFactory<ISmartCardConnection>>();
        using var mgmtSession = managementSessionFactory.Create(connection);
        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
    }
}