namespace Yubico.YubiKit.Core.YubiKey;

public interface IYubiKeyManager
{
    IObservable<DeviceEvent> DeviceChanges { get; }
    Task<IReadOnlyList<IYubiKey>> FindAllAsync(CancellationToken cancellationToken = default);
}

public class YubiKeyManager(IDeviceRepository? deviceRepository = null) : IYubiKeyManager
{
    #region IYubiKeyManager Members

    public async Task<IReadOnlyList<IYubiKey>> FindAllAsync(CancellationToken cancellationToken = default)
    {
        if (deviceRepository is null)
            return await FindYubiKeys
                .Create()
                .FindAllAsync(cancellationToken)
                .ConfigureAwait(false);

        return await deviceRepository.FindAllAsync(cancellationToken).ConfigureAwait(false);
    }

    public IObservable<DeviceEvent> DeviceChanges
    {
        get
        {
            if (deviceRepository is null)
                throw new InvalidOperationException(
                    $"{nameof(DeviceChanges)} is not available when the {nameof(YubiKeyManager)} " +
                    "is created without a device repository.");

            return deviceRepository.DeviceChanges;
        }
    }

    #endregion
}