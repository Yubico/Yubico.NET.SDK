using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Yubico.YubiKit.Core.YubiKey;

public interface IYubiKeyManager
{
    IObservable<DeviceEvent> DeviceChanges { get; }
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

    public IObservable<DeviceEvent> DeviceChanges =>
        deviceRepository.DeviceChanges;

    #endregion
}