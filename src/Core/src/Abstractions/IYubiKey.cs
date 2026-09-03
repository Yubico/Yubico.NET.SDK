// Copyright 2025 Yubico AB
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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Abstractions;

/// <summary>
///     Represents a physical YubiKey and the set of connections (interfaces) it exposes.
/// </summary>
public interface IYubiKey
{
    /// <summary>
    ///     An identifier for this physical device, suitable for correlating devices <em>within one
    ///     discovery session</em>. It is not a durable identity and must not be persisted, parsed, or
    ///     compared across processes or platforms.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The value is derived from whatever grouping evidence discovery had available, so it takes
    ///         one of several shapes: <c>ykphysical:topology:{key}</c> (Windows Container ID),
    ///         <c>ykphysical:{serial}</c>, or <c>ykphysical:pid:{PID}</c>. When discovery cannot prove that
    ///         multiple interfaces belong together, a one-interface device preserves its transport-shaped
    ///         <c>hid:*</c> or <c>pcsc:*</c> identifier. The shape is an implementation detail, is
    ///         platform-dependent, and may change.
    ///     </para>
    ///     <para>
    ///         <strong>The same physical key can present different values.</strong> Evidence depends on what
    ///         else is attached: a key alone may group by PID, while inserting a second key of the same
    ///         product ID forces serial evidence to tell them apart. Measured on macOS hardware, one key
    ///         reported <c>ykphysical:pid:0407</c> alone and <c>ykphysical:103</c> once a same-PID sibling
    ///         was inserted. The device did not change; the available evidence did.
    ///     </para>
    ///     <para>
    ///         <strong>A live repository and a fresh scan can legitimately disagree.</strong> When an
    ///         evidence-only tier change occurs, the repository keeps publishing the object it already
    ///         handed out rather than churning subscribers, so a concurrent independent scan may report a
    ///         different identifier for the same key. Both are correct.
    ///     </para>
    ///     <para>
    ///         For a durable key — persistence, audit logs, allow lists — use <see cref="SerialNumber" />
    ///         instead, and note that it is <see langword="null" /> until discovery has read it and on
    ///         devices which do not report a serial (for example Security Key series). Firmware version is
    ///         deliberately not part of identity: it can differ per applet on the same key and is not a
    ///         disambiguator.
    ///     </para>
    ///     <para>
    ///         A grouped device identifier is distinct from the per-interface identifiers (<c>hid:*</c>,
    ///         <c>pcsc:*</c>) used by the connection registry. A one-interface device intentionally uses
    ///         that sole interface identifier as its physical-device identifier.
    ///     </para>
    /// </remarks>
    string DeviceId { get; }

    /// <summary>
    ///     The set of concrete connections this physical device exposes (any combination of
    ///     <see cref="ConnectionType.SmartCard"/>, <see cref="ConnectionType.HidFido"/>, and
    ///     <see cref="ConnectionType.HidOtp"/>). Never contains the <see cref="ConnectionType.Hid"/>
    ///     group flag or <see cref="ConnectionType.All"/>.
    /// </summary>
    ConnectionType AvailableConnections { get; }

    /// <summary>
    ///     The hardware serial number read by discovery, or <see langword="null" /> while it is unknown.
    ///     Available without opening a session and without a Management package dependency.
    /// </summary>
    /// <remarks>
    ///     <para>Contract:</para>
    ///     <list type="bullet">
    ///         <item><description>
    ///             <see langword="null" /> until a discovery metadata read has succeeded — and possibly
    ///             forever: reads can fail persistently, the discovery read budget can be exhausted, and
    ///             whole device classes (for example the Security Key series) report no serial at all.
    ///         </description></item>
    ///         <item><description>
    ///             Once non-<see langword="null" />, it never reverts to <see langword="null" />. It may
    ///             transition from <see langword="null" /> to a value after publication, without any
    ///             device event.
    ///         </description></item>
    ///         <item><description>
    ///             A manager-retained object's known value does not change to another known value. If a
    ///             fresh scan reports a different known serial for the same interface set, the manager
    ///             republishes the device as a removal followed by an addition.
    ///         </description></item>
    ///         <item><description>
    ///             A republished device object (see the retention contract in
    ///             <c>docs/architecture/device-identity.md</c>) never inherits identity from its
    ///             predecessor object. It exposes only what discovery established for that fresh object,
    ///             which may already include a serial when the addition is published.
    ///         </description></item>
    ///         <item><description>
    ///             The object delivered with a removal event retains its last-known value.
    ///         </description></item>
    ///     </list>
    ///     <para>
    ///         This is the only durable physical identity the SDK offers. When it is
    ///         <see langword="null" />, physical identity is unknowable without a live session.
    ///     </para>
    /// </remarks>
    int? SerialNumber => null;

    /// <summary>
    ///     Answers whether this reference and <paramref name="other" /> describe the same physical key.
    ///     The answer is tri-state and never guesses: <see cref="DeviceCorrelation.Unknown" /> whenever
    ///     either side's <see cref="SerialNumber" /> is unknown.
    /// </summary>
    /// <remarks>
    ///     The same object is always <see cref="DeviceCorrelation.Same" />. Two references with known,
    ///     equal serials are <see cref="DeviceCorrelation.Same" />; known, unequal serials are
    ///     <see cref="DeviceCorrelation.Different" />. This three-valued correlation is not a total
    ///     equality predicate: <see cref="DeviceCorrelation.Unknown" /> conveys neither equality nor
    ///     inequality and must not be coerced to either result for collection equality or deduplication.
    ///     No equality comparer over this relation is offered: the serial arrives after publication, so
    ///     any serial-derived hash could mutate while the object already keys a collection. Key
    ///     collections by reference instead — instance retention within one manager makes that a
    ///     supported pattern.
    /// </remarks>
    DeviceCorrelation SameDeviceAs(IYubiKey other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (ReferenceEquals(this, other))
            return DeviceCorrelation.Same;

        return SerialNumber is { } ownSerial && other.SerialNumber is { } otherSerial
            ? ownSerial == otherSerial ? DeviceCorrelation.Same : DeviceCorrelation.Different
            : DeviceCorrelation.Unknown;
    }

    /// <summary>
    ///     Whether this device can open the requested connection. Only concrete openable types are valid
    ///     (<see cref="ConnectionType.SmartCard"/>, <see cref="ConnectionType.HidFido"/>,
    ///     <see cref="ConnectionType.HidOtp"/>); <see cref="ConnectionType.Hid"/> means a HID interface is present;
    ///     <see cref="ConnectionType.Unknown"/>, <see cref="ConnectionType.All"/>, and other combinations return <c>false</c>.
    /// </summary>
    bool SupportsConnection(ConnectionType connectionType) =>
        AvailableConnections.SupportsConnection(connectionType);

    /// <summary>
    ///     Opens the requested interface after claiming the physical YubiKey's known member interface IDs.
    /// </summary>
    /// <exception cref="ConnectionInUseException">The physical YubiKey already has a live connection.</exception>
    Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
        where TConnection : class, IConnection;

    /// <summary>
    ///     Opens the device's connection when it exposes exactly one. For a physical device that exposes
    ///     several connections this is ambiguous and throws; callers must use <see cref="ConnectAsync{TConnection}"/>
    ///     or an application-specific extension that selects a transport intentionally.
    /// </summary>
    async Task<IConnection> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var single = AvailableConnections.SingleConcreteConnectionOrUnknown();
        return single switch
        {
            ConnectionType.SmartCard => await ConnectAsync<ISmartCardConnection>(cancellationToken)
                .ConfigureAwait(false),
            ConnectionType.HidFido => await ConnectAsync<IFidoHidConnection>(cancellationToken)
                .ConfigureAwait(false),
            ConnectionType.HidOtp => await ConnectAsync<IOtpHidConnection>(cancellationToken)
                .ConfigureAwait(false),
            _ when (AvailableConnections & ConnectionTypeExtensions.ConcreteConnections) == ConnectionType.Unknown =>
                throw new NotSupportedException(
                    "This YubiKey exposes no openable connection."),
            _ => throw new InvalidOperationException(
                $"This YubiKey exposes multiple connections ({AvailableConnections}); the default connect is ambiguous. " +
                "Use ConnectAsync<TConnection>() or an application-specific session extension to choose a transport.")
        };
    }
}