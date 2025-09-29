using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit;

public class YubiKeyManager : IYubiKeyManager
{
    private readonly ILogger<YubiKeyManager> _logger;
    private readonly IOptions<YubiKeyManagerOptions> _options;
    private readonly YubiKeyDeviceRepository _deviceRepository;

    public YubiKeyManager(
        ILogger<YubiKeyManager> logger,
        IOptions<YubiKeyManagerOptions> options,
        YubiKeyDeviceRepository deviceRepository)
    {
        _logger = logger;
        _options = options;
        _deviceRepository = deviceRepository;
    }

    #region IYubiKeyManager Members

    public IEnumerable<IYubiKey> GetYubiKeys()
    {
       return _deviceRepository.GetAllDevices();
    }

    public async Task<IEnumerable<IYubiKey>> GetYubiKeysAsync(CancellationToken cancellationToken = default)
    {
       return await _deviceRepository.GetAllDevicesAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    public IObservable<YubiKeyDeviceEvent> DeviceChanges =>
        _deviceRepository.DeviceChanges;
}