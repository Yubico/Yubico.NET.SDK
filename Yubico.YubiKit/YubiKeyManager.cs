using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Core.Devices.SmartCard;

namespace Yubico.YubiKit;

public class YubiKeyManager(
    ILogger<YubiKeyManager> logger, 
    IOptions<YubiKeyManagerOptions> options) : IYubiKeyManager
{
    public async Task<IEnumerable<IYubiKey>> GetYubiKeys()
    {
        var pcscDevices = await ReadPcscDevices();
        return pcscDevices;
    }

    private async Task<IEnumerable<IYubiKey>> ReadPcscDevices()
    {
        var devices = await PcscYubiKey.GetAllAsync();
        return devices;
    }
}