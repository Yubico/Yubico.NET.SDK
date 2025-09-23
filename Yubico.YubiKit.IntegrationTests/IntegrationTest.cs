using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Connections;

namespace Yubico.YubiKit.IntegrationTests;

public class IntegrationTest
{
    [Fact]
    public async Task GetPcscDevices()
    {
        YubiKeyManager manager = new(
            new NullLogger<YubiKeyManager>(),
            Options.Create(
                new YubiKeyManagerOptions(true, TimeSpan.FromSeconds(1))));

        IEnumerable<IYubiKey> pcscDevices = await manager.GetYubiKeys();
        IYubiKey? pcscDevice = pcscDevices.FirstOrDefault();
        Assert.NotNull(pcscDevice);
    }

    [Fact]
    public async Task GetDeviceInfo()
    {
        YubiKeyManager manager = new(
            new NullLogger<YubiKeyManager>(),
            Options.Create(
                new YubiKeyManagerOptions(true, TimeSpan.FromSeconds(1))));

        IEnumerable<IYubiKey> pcscDevices = await manager.GetYubiKeys();
        IYubiKey pcscDevice = pcscDevices.First();
        ISmartCardConnection connection = await pcscDevice.ConnectAsync<ISmartCardConnection>();

        ManagementSession mgmtSession = new(new NullLogger<ManagementSession>(), connection);
        DeviceInfo deviceInfo = mgmtSession.GetDeviceInfo();
        Assert.NotNull(deviceInfo);
    }
}