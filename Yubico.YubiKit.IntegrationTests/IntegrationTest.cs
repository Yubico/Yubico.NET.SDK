
namespace Yubico.YubiKit.IntegrationTests;

public class IntegrationTest
{
    [Fact]
    public async Task GetPcscDevices()
    {
        var manager = new YubiKeyManager();
        var pcscDevices = await manager.GetPcscDevices();
        var pcscDevice = pcscDevices.FirstOrDefault();
        Assert.NotNull(pcscDevice);
    }
}