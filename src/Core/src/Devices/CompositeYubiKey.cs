// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     A single physical YubiKey assembled from several per-interface devices (PC/SC CCID, FIDO HID,
///     OTP HID) that discovery proved belong to the same physical key (shared serial number).
/// </summary>
/// <remarks>
///     The composite owns no long-lived connection; it only holds references to its member interface
///     devices and routes a typed <see cref="ConnectAsync{TConnection}" /> to the member that exposes
///     the requested connection. Member devices are not <see cref="IDisposable" /> and are not owned in a
///     disposable sense, mirroring <see cref="PcscYubiKey" /> and <c>HidYubiKey</c>.
/// </remarks>
internal sealed class CompositeYubiKey : IYubiKey
{
    private readonly IReadOnlyList<IYubiKey> _members;

    public CompositeYubiKey(string deviceId, IReadOnlyList<IYubiKey> members, DeviceInfo? deviceInfo)
    {
        ArgumentException.ThrowIfNullOrEmpty(deviceId);
        ArgumentNullException.ThrowIfNull(members);
        if (members.Count < 2)
            throw new ArgumentException("A composite device requires at least two member interfaces.", nameof(members));

        DeviceId = deviceId;
        _members = members;
        DeviceInfo = deviceInfo;
        MemberDeviceIds = [.. members.Select(m => m.DeviceId).OrderBy(id => id, StringComparer.Ordinal)];

        var combined = ConnectionType.Unknown;
        foreach (var member in members)
            combined |= member.AvailableConnections;
        AvailableConnections = combined & ConnectionTypeExtensions.ConcreteConnections;
    }

    public string DeviceId { get; }

    public ConnectionType AvailableConnections { get; }

    /// <summary>Sorted member interface DeviceIds — a stable key for the physical interface set across rescans.</summary>
    internal IReadOnlyList<string> MemberDeviceIds { get; }

    /// <summary>
    ///     Read-only device metadata read during discovery (used internally; not part of the public contract
    ///     yet). May be populated after construction by the best-effort metadata pass.
    /// </summary>
    public DeviceInfo? DeviceInfo { get; internal set; }

    /// <summary>Firmware version read during discovery, when available.</summary>
    public FirmwareVersion? FirmwareVersion => DeviceInfo?.FirmwareVersion;

    public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
        where TConnection : class, IConnection
    {
        var requested = RequestedConnectionType<TConnection>();
        if (requested == ConnectionType.Unknown)
            throw new NotSupportedException(
                $"Connection type {typeof(TConnection).Name} is not supported by this YubiKey device.");

        foreach (var member in _members)
        {
            if (member.AvailableConnections.SupportsConnection(requested))
                return member.ConnectAsync<TConnection>(cancellationToken);
        }

        throw new NotSupportedException(
            $"Connection type {typeof(TConnection).Name} ({requested}) is not available on this physical YubiKey " +
            $"(available connections: {AvailableConnections}).");
    }

    private static ConnectionType RequestedConnectionType<TConnection>()
        where TConnection : class, IConnection
    {
        if (typeof(TConnection) == typeof(ISmartCardConnection))
            return ConnectionType.SmartCard;
        if (typeof(TConnection) == typeof(IFidoHidConnection))
            return ConnectionType.HidFido;
        if (typeof(TConnection) == typeof(IOtpHidConnection))
            return ConnectionType.HidOtp;
        return ConnectionType.Unknown;
    }
}