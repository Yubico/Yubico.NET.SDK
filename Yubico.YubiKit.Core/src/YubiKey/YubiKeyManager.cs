namespace Yubico.YubiKit.Core.YubiKey;

public interface IYubiKeyManager
{
    IObservable<DeviceEvent> DeviceChanges { get; }

    Task<IReadOnlyList<IYubiKey>> FindAllAsync(ConnectionType type = ConnectionType.All,
        CancellationToken cancellationToken = default);
}

public class YubiKeyManager(IDeviceRepository? deviceRepository = null) : IYubiKeyManager
{
    #region IYubiKeyManager Members

    public Task<IReadOnlyList<IYubiKey>> FindAllAsync(
        ConnectionType type = ConnectionType.All,
        CancellationToken cancellationToken = default)
    {
        if (deviceRepository is null)
            return FindYubiKeys
                .Create()
                .FindAllAsync(type, cancellationToken);

        return deviceRepository.FindAllAsync(type, cancellationToken);
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