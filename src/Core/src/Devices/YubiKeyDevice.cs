// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     The single production device shape published by discovery, with at most one slot for each concrete
///     connection type.
/// </summary>
internal sealed class YubiKeyDevice : IYubiKey, IDiscoveryConnectionProvider
{
    private readonly IYubiKeyConnectionSlot? _smartCard;
    private readonly IYubiKeyConnectionSlot? _hidFido;
    private readonly IYubiKeyConnectionSlot? _hidOtp;
    private DeviceInfoSnapshot? _deviceInfo;
    private object? _serialNumber; // boxed int, published atomically as a reference

    internal YubiKeyDevice(
        string deviceId,
        IYubiKeyConnectionSlot? smartCard,
        IYubiKeyConnectionSlot? hidFido,
        IYubiKeyConnectionSlot? hidOtp,
        DeviceInfo? deviceInfo,
        bool identityReadBudgetConsumedThisScan = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(deviceId);
        if (smartCard is null && hidFido is null && hidOtp is null)
            throw new ArgumentException("A published device requires at least one connection slot.");

        ValidateSlot(smartCard, ConnectionType.SmartCard, nameof(smartCard));
        ValidateSlot(hidFido, ConnectionType.HidFido, nameof(hidFido));
        ValidateSlot(hidOtp, ConnectionType.HidOtp, nameof(hidOtp));

        DeviceId = deviceId;
        _smartCard = smartCard;
        _hidFido = hidFido;
        _hidOtp = hidOtp;
        DeviceInfo = deviceInfo;
        IdentityReadBudgetConsumedThisScan = identityReadBudgetConsumedThisScan;
        InterfaceIds = Slots()
            .Select(slot => slot.InterfaceId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        PhysicalIdentityKey = EncodeInterfaceIds(InterfaceIds);

        var connections = ConnectionType.Unknown;
        if (smartCard is not null)
            connections |= ConnectionType.SmartCard;
        if (hidFido is not null)
            connections |= ConnectionType.HidFido;
        if (hidOtp is not null)
            connections |= ConnectionType.HidOtp;
        AvailableConnections = connections;
    }
    public string DeviceId { get; }

    public ConnectionType AvailableConnections { get; }

    internal IReadOnlyList<string> InterfaceIds { get; }

    internal string PhysicalIdentityKey { get; }

    /// <summary>The most recent device metadata read successfully for this stable interface set.</summary>
    /// <remarks>
    ///     Null updates are ignored deliberately: a transient read failure must not erase the last successful
    ///     metadata snapshot while the published device retains the same interface identity.
    /// </remarks>
    public DeviceInfo? DeviceInfo
    {
        get => Volatile.Read(ref _deviceInfo)?.Value;
        internal set
        {
            if (value is { } metadata)
            {
                Volatile.Write(ref _deviceInfo, new DeviceInfoSnapshot(metadata));

                // The serial is latched independently of DeviceInfo churn: a later successful read
                // whose metadata carries no serial (serial-less report, mid-reconfiguration read) must
                // not regress an already-known serial to null. A different non-null serial does update
                // the latch, mirroring the metadata healing behavior after a same-slot key swap.
                if (metadata.SerialNumber is { } serial)
                    Volatile.Write(ref _serialNumber, serial);
            }
        }
    }

    /// <summary>
    ///     The latched hardware serial number per the <see cref="IYubiKey.SerialNumber" /> contract:
    ///     null until a metadata read delivering a serial succeeds, monotonically non-null afterward.
    /// </summary>
    public int? SerialNumber => (int?)Volatile.Read(ref _serialNumber);

    internal bool IdentityReadBudgetConsumedThisScan { get; }

    /// <summary>
    ///     Returns the internal interface-set identity for production devices and a one-DeviceId encoding
    ///     for transparent third-party or test implementations.
    /// </summary>
    /// <remarks>
    ///     <see cref="DeviceId" /> names the evidence tier used by discovery, so it can change while the
    ///     physical key remains unchanged. The interface set does not change merely because merge evidence
    ///     changes; broader identity-policy comparisons belong in the architecture documentation.
    /// </remarks>
    internal static string PhysicalIdentityKeyFor(IYubiKey device) =>
        device is YubiKeyDevice published
            ? published.PhysicalIdentityKey
            : EncodeInterfaceIds([device.DeviceId]);

    public async Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
        where TConnection : class, IConnection
    {
        var requested = RequestedConnectionType<TConnection>();
        if (requested == ConnectionType.Unknown)
        {
            throw new NotSupportedException(
                $"Connection type {typeof(TConnection).Name} is not supported by this YubiKey device.");
        }

        if (!TryResolveSlot(requested, out var slot))
        {
            throw new NotSupportedException(
                $"Connection type {typeof(TConnection).Name} ({requested}) is not available on this physical " +
                $"YubiKey (available connections: {AvailableConnections}).");
        }

        var ownership = await DeviceConnectionRegistry
            .AcquireConnectionAsync(InterfaceIds, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            var raw = await slot.OpenRawConnectionAsync(requested, cancellationToken).ConfigureAwait(false);
            try
            {
                IConnection registered = requested switch
                {
                    ConnectionType.SmartCard when raw is ISmartCardConnection smartCard =>
                        new RegisteredSmartCardConnection(smartCard, ownership),
                    ConnectionType.HidFido when raw is IFidoHidConnection fido =>
                        new RegisteredFidoHidConnection(fido, ownership),
                    ConnectionType.HidOtp when raw is IOtpHidConnection otp =>
                        new RegisteredOtpHidConnection(otp, ownership),
                    _ => throw new InvalidOperationException(
                        $"The {slot.GetType().Name} slot returned an unexpected connection for {requested}.")
                };

                return (TConnection)(object)registered;
            }
            catch
            {
                await raw.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }
        catch
        {
            ownership.Dispose();
            throw;
        }
    }

    async Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
        ConnectionType connection,
        CancellationToken cancellationToken)
    {
        if (!TryResolveSlot(connection, out var slot))
        {
            throw new NotSupportedException(
                $"Connection type {connection} is not available on this physical YubiKey " +
                $"(available connections: {AvailableConnections}).");
        }

        try
        {
            return await slot.OpenRawConnectionAsync(connection, cancellationToken).ConfigureAwait(false);
        }
        catch (NonOpenableConnectionSlotException)
        {
            throw new DiscoveryReadSkippedException(slot.InterfaceId, DiscoveryReadSkipCause.NoDiscoveryProvider);
        }
    }

    /// <summary>
    ///     The one slot-selection rule shared by normal connect, discovery connect, and registry identity
    ///     resolution.
    /// </summary>
    internal bool TryResolveSlot(
        ConnectionType connection,
        [NotNullWhen(true)] out IYubiKeyConnectionSlot? slot)
    {
        slot = connection switch
        {
            ConnectionType.SmartCard => _smartCard,
            ConnectionType.HidFido => _hidFido,
            ConnectionType.HidOtp => _hidOtp,
            ConnectionType.Hid => _hidFido ?? _hidOtp,
            _ => null
        };
        return slot is not null;
    }

    private IEnumerable<IYubiKeyConnectionSlot> Slots()
    {
        if (_smartCard is not null)
            yield return _smartCard;
        if (_hidFido is not null)
            yield return _hidFido;
        if (_hidOtp is not null)
            yield return _hidOtp;
    }

    private static void ValidateSlot(
        IYubiKeyConnectionSlot? slot,
        ConnectionType expected,
        string parameterName)
    {
        if (slot is not null && slot.ConnectionType != expected)
        {
            throw new ArgumentException(
                $"The {expected} slot must expose exactly that connection.",
                parameterName);
        }
    }

    private static string EncodeInterfaceIds(IReadOnlyList<string> sortedInterfaceIds)
    {
        var builder = new StringBuilder();
        foreach (var id in sortedInterfaceIds)
            builder.Append(id.Length).Append(':').Append(id);
        return builder.ToString();
    }

    private static ConnectionType RequestedConnectionType<TConnection>()
        where TConnection : class, IConnection =>
        typeof(TConnection) == typeof(ISmartCardConnection)
            ? ConnectionType.SmartCard
            : typeof(TConnection) == typeof(IFidoHidConnection)
                ? ConnectionType.HidFido
                : typeof(TConnection) == typeof(IOtpHidConnection)
                     ? ConnectionType.HidOtp
                     : ConnectionType.Unknown;

    private sealed class DeviceInfoSnapshot(DeviceInfo value)
    {
        public DeviceInfo Value { get; } = value;
    }
}