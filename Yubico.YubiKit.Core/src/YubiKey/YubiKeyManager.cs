using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.YubiKey;

public interface IYubiKeyManager
{
    IObservable<DeviceEvent> DeviceChanges { get; }

    Task<IReadOnlyList<IYubiKey>> FindAllAsync(ConnectionType type = ConnectionType.All,
        CancellationToken cancellationToken = default);
}

public class YubiKeyManager(IDeviceRepository? deviceRepository) : IYubiKeyManager
{
    // Static API - Lazy singleton for DI-free usage
    private static readonly Lazy<DeviceRepository> _repository = new(
        DeviceRepository.Create,
        LazyThreadSafetyMode.ExecutionAndPublication);

    // Monitoring lifecycle fields
#pragma warning disable CS0169 // Field is never used - used in upcoming monitoring implementation (tasks 3.2-3.6)
    private static CancellationTokenSource? _monitoringCts;
    private static Task? _monitoringTask;
#pragma warning restore CS0169
    private static readonly object _monitorLock = new();

    /// <summary>
    /// Finds all connected YubiKey devices using the static API (no DI required).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of discovered YubiKey devices.</returns>
    public static Task<IReadOnlyList<IYubiKey>> FindAllAsync(CancellationToken cancellationToken = default)
        => FindAllAsync(ConnectionType.All, cancellationToken);

    /// <summary>
    /// Finds all connected YubiKey devices of the specified connection type using the static API.
    /// </summary>
    /// <param name="type">The connection type to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of discovered YubiKey devices.</returns>
    public static Task<IReadOnlyList<IYubiKey>> FindAllAsync(
        ConnectionType type,
        CancellationToken cancellationToken)
        => _repository.Value.FindAllAsync(type, cancellationToken);

    // Instance API - for DI-based usage
    Task<IReadOnlyList<IYubiKey>> IYubiKeyManager.FindAllAsync(
        ConnectionType type,
        CancellationToken cancellationToken)
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
}