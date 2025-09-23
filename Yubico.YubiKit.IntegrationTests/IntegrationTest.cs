
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Yubico.YubiKit.Core.Connections;

namespace Yubico.YubiKit.IntegrationTests;

public class IntegrationTest
{
    [Fact]
    public async Task GetPcscDevices()
    {
        var manager = new YubiKeyManager(
            new NullLogger<YubiKeyManager>(), 
            Options.Create(
                new YubiKeyManagerOptions(true, TimeSpan.FromSeconds(1), Transports.All){}));
        
        var pcscDevices = await manager.GetYubiKeys();
        var pcscDevice = pcscDevices.FirstOrDefault();
        Assert.NotNull(pcscDevice);
    }
    
    [Fact]
    public async Task GetDeviceInfo()
    {
        var manager = new YubiKeyManager(
            new NullLogger<YubiKeyManager>(), 
            Options.Create(
                new YubiKeyManagerOptions(true, TimeSpan.FromSeconds(1), Transports.All){}));
        
        var pcscDevices = await manager.GetYubiKeys();
        var pcscDevice = pcscDevices.First();
        var connection = await pcscDevice.ConnectAsync<ISmartCardConnection>();
        
        var mgmtSession = new ManagementSession(new NullLogger<ManagementSession>(), connection);
        var deviceInfo = mgmtSession.GetDeviceInfo();
        Assert.NotNull(deviceInfo);
    }
}