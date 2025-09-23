using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Core.Devices.SmartCard;

namespace Yubico.YubiKit;

public class YubiKeyManager : IYubiKeyManager
{
    private readonly ILogger<YubiKeyManager> _logger;

    public YubiKeyManager(
        ILogger<YubiKeyManager> logger,
        IOptions<YubiKeyManagerOptions> options) =>
        _logger = logger;

    public YubiKeyManager() : this(new NullLogger<YubiKeyManager>(), Options.Create(new YubiKeyManagerOptions()))
    {
    }

    public async Task<IEnumerable<IYubiKey>> GetYubiKeys()
    {
        IEnumerable<IYubiKey> pcscDevices = await ReadPcscDevices();
        return pcscDevices;
    }

    private async Task<IEnumerable<IYubiKey>> ReadPcscDevices()
    {
        IReadOnlyList<PcscYubiKey> devices = await PcscYubiKey.GetAllAsync();
        return devices;
    }
}