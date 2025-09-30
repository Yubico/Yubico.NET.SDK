using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Device;

namespace Yubico.YubiKit;

public interface IYubiKeyManager
{
    IObservable<YubiKeyDeviceEvent> DeviceChanges { get; }
    Task<IEnumerable<IYubiKey>> GetYubiKeysAsync(CancellationToken cancellationToken = default);
}

public class YubiKeyManager : IYubiKeyManager
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly ILogger<YubiKeyManager> _logger;
    private readonly IOptions<YubiKeyManagerOptions> _options;

    public YubiKeyManager(
        ILogger<YubiKeyManager> logger,
        IOptions<YubiKeyManagerOptions> options,
        IDeviceRepository deviceRepository)
    {
        _logger = logger;
        _options = options;
        _deviceRepository = deviceRepository;
    }

    #region IYubiKeyManager Members

    public async Task<IEnumerable<IYubiKey>> GetYubiKeysAsync(CancellationToken cancellationToken = default) =>
        await _deviceRepository.GetAllDevicesAsync(cancellationToken).ConfigureAwait(false);

    public IObservable<YubiKeyDeviceEvent> DeviceChanges =>
        _deviceRepository.DeviceChanges;

    #endregion
}