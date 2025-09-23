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
        ManagementSession mgmtSession = new(logger, connection);
        var deviceInfo = mgmtSession.GetDeviceInfo();
    }
}