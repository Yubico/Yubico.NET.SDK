using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit;

public class YubiKeyManager : IYubiKeyManager
{
    private readonly ILogger<YubiKeyManager> _logger;
    private readonly IOptions<YubiKeyManagerOptions> _options;
    private readonly IYubiKeyDeviceRepository _deviceRepository;

    public YubiKeyManager(
        ILogger<YubiKeyManager> logger,
        IOptions<YubiKeyManagerOptions> options,
        IYubiKeyDeviceRepository deviceRepository)
    {
        _logger = logger;
        _options = options;
        _deviceRepository = deviceRepository;
    }

    #region IYubiKeyManager Members

    public async Task<IEnumerable<IYubiKey>> GetYubiKeys()
    {
        var devices = await _deviceRepository.GetAllDevicesAsync();
        return devices.AsEnumerable();
    }

    #endregion

    public IObservable<YubiKeyDeviceEvent> DeviceChanges =>
        _deviceRepository.DeviceChanges;
}