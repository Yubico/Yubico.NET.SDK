using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Devices.SmartCard;

namespace Yubico.YubiKit;

public class YubiKeyManager : IYubiKeyManager
{
    private readonly ILogger<YubiKeyManager> _logger;
    private readonly IOptions<YubiKeyManagerOptions> _options;
    private readonly IYubiKeyFactory _yubiKeyFactory;

    public YubiKeyManager(
        ILogger<YubiKeyManager> logger,
        IOptions<YubiKeyManagerOptions> options,
        IYubiKeyFactory yubiKeyFactory)
    {
        _logger = logger;
        _options = options;
        _yubiKeyFactory = yubiKeyFactory;
    }

    #region IYubiKeyManager Members

    public async Task<IEnumerable<IYubiKey>> GetYubiKeys()
    {
        var pcscDevices = await ReadPcscDevices();
        return pcscDevices;
    }

    #endregion

    private async Task<IEnumerable<IYubiKey>> ReadPcscDevices()
    {
        var devices = await PcscYubiKey.GetAllAsync(_yubiKeyFactory);
        return devices;
    }
}