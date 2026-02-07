using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.YubiKey;

/// <summary>
/// Manager for discovering and interacting with YubiKey devices.
/// </summary>
public interface IYubiKeyManager
{
    /// <summary>
    /// Observable stream of device events (added, removed, updated).
    /// </summary>
    /// <remarks>
    /// Requires background services to be running. Call <c>host.StartAsync()</c> before accessing.
    /// </remarks>
    IObservable<DeviceEvent> DeviceChanges { get; }

    /// <summary>
    /// Finds all connected YubiKey devices, returning composite devices that aggregate all transports.
    /// </summary>
    /// <param name="type">Connection type filter. Use <see cref="ConnectionType.All"/> for all devices.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of composite YubiKey devices.</returns>
    Task<IReadOnlyList<IYubiKey>> FindAllAsync(ConnectionType type = ConnectionType.All,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of <see cref="IYubiKeyManager"/>.
/// </summary>
public class YubiKeyManager(IDeviceRepository? deviceRepository) : IYubiKeyManager
{
    /// <inheritdoc />
    public Task<IReadOnlyList<IYubiKey>> FindAllAsync(
        ConnectionType type = ConnectionType.All,
        CancellationToken cancellationToken = default)
    {
        if (deviceRepository is null)
        {
            throw new InvalidOperationException(
                $"{nameof(FindAllAsync)} requires a device repository. " +
                "Use dependency injection with AddYubiKeyManagerCore() to register services.");
        }

        return deviceRepository.FindAllAsync(type, cancellationToken);
    }

    /// <inheritdoc />
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
}