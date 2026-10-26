using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.Transports.SmartCard;

/// <summary>Creates raw smart-card connections for an enumerated PC/SC device.</summary>
/// <remarks>
///     If <see cref="CreateAsync" /> fails or is cancelled before returning a connection, the factory must clean up
///     every partial native resource it acquired. If it cannot prove that cleanup completed, it must throw
///     <see cref="UnrecoveredConnectionException" />; Core then retains the managed physical-device claim.
///     Ordinary creation failures mean the factory proved its own partial resources released and permit retry.
///     This creation-failure signal does not apply to disposal: any smart-card disposal failure remains unproven.
/// </remarks>
/// <remarks>
///     A returned connection must not complete disposal until it has relinquished every native resource it owns.
///     If disposal throws, Core cannot infer release from the exception type and retains any managed physical-device
///     claim associated with that connection.
/// </remarks>
public interface ISmartCardConnectionFactory
{
    /// <summary>Creates and initializes a smart-card connection.</summary>
    /// <exception cref="UnrecoveredConnectionException">
    ///     Creation failed after acquiring native resources whose release the factory could not prove.
    /// </exception>
    Task<ISmartCardConnection> CreateAsync(IPcscDevice smartCardDevice, CancellationToken cancellationToken = default);
}

/// <summary>Creates a smart-card connection while owning its physical registration.</summary>
/// <remarks>
///     Implementations accept ownership of the registration on every exit, including failure and cancellation:
///     release it only after native release is proven; otherwise retain it and mark it unrecovered.
/// </remarks>
internal interface IRegisteredSmartCardConnectionFactory
{
    Task<ISmartCardConnection> CreateRegisteredAsync(
        IPcscDevice smartCardDevice,
        IDisposable registration,
        CancellationToken cancellationToken = default);
}

/// <summary>The built-in smart-card connection factory.</summary>
/// <remarks>
///     Custom factories implement <see cref="ISmartCardConnectionFactory" /> rather than deriving from this type.
///     Configure connection logging through <see cref="YubiKitLogging" />.
/// </remarks>
public sealed class SmartCardConnectionFactory : ISmartCardConnectionFactory, IRegisteredSmartCardConnectionFactory
{
    private static readonly Lock s_defaultSharingLock = new();
    private readonly ISCardConnectionApi _api;
    private readonly SmartCardSharing _sharing;

    /// <summary>
    ///     The <see cref="SmartCardSharing" /> used by factories created without an explicit mode, including the
    ///     one behind <see cref="Devices.YubiKeyManager" />. Set it once at start-up, before any device is opened.
    ///     Defaults to <see cref="SmartCardSharing.ExclusiveWithSharedFallback" />.
    /// </summary>
    public static SmartCardSharing DefaultSharing
    {
        get
        {
            lock (s_defaultSharingLock)
                return field;
        }
        set
        {
            lock (s_defaultSharingLock)
                field = value;
        }
    } = SmartCardSharing.ExclusiveWithSharedFallback;

    /// <summary>
    ///     Creates a factory that uses the configured <see cref="YubiKitLogging" /> logger factory and
    ///     <see cref="DefaultSharing" />.
    /// </summary>
    public SmartCardConnectionFactory()
        : this(new NativeSCardConnectionApi(), DefaultSharing)
    {
    }

    /// <summary>Creates a factory that opens card handles with <paramref name="sharing"/>.</summary>
    public SmartCardConnectionFactory(SmartCardSharing sharing)
        : this(new NativeSCardConnectionApi(), sharing)
    {
    }

    internal SmartCardConnectionFactory(ISCardConnectionApi api)
        : this(api, DefaultSharing)
    {
    }

    internal SmartCardConnectionFactory(ISCardConnectionApi api, SmartCardSharing sharing)
    {
        _api = api;
        _sharing = sharing;
    }

    public async Task<ISmartCardConnection> CreateAsync(IPcscDevice smartCardDevice,
        CancellationToken cancellationToken = default)
    {
        var connection = new UsbSmartCardConnection(
            smartCardDevice,
            api: _api,
            sharing: _sharing);

        await connection.InitializeAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    async Task<ISmartCardConnection> IRegisteredSmartCardConnectionFactory.CreateRegisteredAsync(
        IPcscDevice smartCardDevice,
        IDisposable registration,
        CancellationToken cancellationToken)
    {
        UsbSmartCardConnection connection;
        try
        {
            connection = new UsbSmartCardConnection(
                smartCardDevice,
                api: _api,
                sharing: _sharing,
                released: registration.Dispose,
                unrecovered: failure => DeviceConnectionRegistry.MarkUnrecovered(registration, failure));
        }
        catch
        {
            // Construction has not published a native owner, so no native acquisition can have started.
            registration.Dispose();
            throw;
        }

        await connection.InitializeAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    public static SmartCardConnectionFactory CreateDefault() => new();
}