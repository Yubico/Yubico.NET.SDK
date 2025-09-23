using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Connections;

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
        var connection = await pcscDevice.ConnectAsync<ISmartCardConnection>();

        var logger = ServiceProvider.GetRequiredService<ILogger<ManagementSession>>();
        var mgmtSession = new ManagementSession(logger, connection);
        var deviceInfo = mgmtSession.GetDeviceInfo();
    }

    [Fact]
    public async Task GetDeviceInfoWithDISession()
    {
        var pcscDevices = await YubiKeyManager.GetYubiKeys();
        var pcscDevice = pcscDevices.First();
        var connection = await pcscDevice.ConnectAsync<ISmartCardConnection>();

        var managementSessionFactory = ServiceProvider.GetRequiredService<IManagementSessionFactory>();
        var mgmtSession = managementSessionFactory.Create(connection);
        var deviceInfo = mgmtSession.GetDeviceInfo();
    }
}