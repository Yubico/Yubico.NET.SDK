using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yubico.YubiKit.Device;

namespace Yubico.YubiKit;

public interface IYubiKeyManager
{
    IObservable<YubiKeyDeviceEvent> DeviceChanges { get; }
    Task<IEnumerable<IYubiKey>> GetYubiKeysAsync(CancellationToken cancellationToken = default);
}

public class YubiKeyManager(
    ILogger<YubiKeyManager> logger,
    IOptions<YubiKeyManagerOptions> options,
    IDeviceRepository deviceRepository)
    : IYubiKeyManager
{
    private readonly ILogger<YubiKeyManager> _logger = logger;
    private readonly IOptions<YubiKeyManagerOptions> _options = options;

    #region IYubiKeyManager Members

    public async Task<IEnumerable<IYubiKey>> GetYubiKeysAsync(CancellationToken cancellationToken = default) =>
        await deviceRepository.GetAllDevicesAsync(cancellationToken).ConfigureAwait(false);

    public IObservable<YubiKeyDeviceEvent> DeviceChanges =>
        deviceRepository.DeviceChanges;

    #endregion
}