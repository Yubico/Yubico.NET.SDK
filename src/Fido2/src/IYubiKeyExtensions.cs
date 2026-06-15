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

using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Fido2;

/// <summary>
/// Extension methods for creating FIDO2 sessions from YubiKey devices.
/// </summary>
/// <remarks>
/// These extension methods provide a convenient API for working with FIDO2/CTAP2
/// functionality on YubiKey devices. They automatically handle connection management
/// and session creation.
/// </remarks>
public static class IYubiKeyExtensions
{
    extension(IYubiKey yubiKey)
    {
        /// <summary>
        /// Gets FIDO2 authenticator information from a YubiKey asynchronously.
        /// </summary>
        /// <param name="cancellationToken">An optional token to cancel the operation.</param>
        /// <returns>
        /// An <see cref="AuthenticatorInfo"/> containing detailed information about the 
        /// authenticator's capabilities, supported extensions, and options.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This is a convenience method that creates a temporary FIDO session, retrieves
        /// the authenticator info, and disposes of the session. For multiple operations,
        /// use <c>CreateFidoSessionAsync</c> instead to reuse the session.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var info = await yubiKey.GetFidoInfoAsync();
        /// Console.WriteLine($"AAGUID: {Convert.ToHexString(info.Aaguid.Span)}");
        /// Console.WriteLine($"Supports CTAP2.1: {info.Versions.Contains("FIDO_2_1")}");
        /// </code>
        /// </example>
        public async Task<AuthenticatorInfo> GetFidoInfoAsync(CancellationToken cancellationToken = default)
        {
            await using var fidoSession = await yubiKey.CreateFidoSessionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return await fidoSession.GetInfoAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a FIDO2 session for interacting with a YubiKey asynchronously.
        /// </summary>
        /// <param name="scpKeyParams">
        /// Optional SCP (Secure Channel Protocol) key parameters for establishing
        /// a secure session with the YubiKey device. Only applicable for SmartCard connections.
        /// </param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="preferredConnection">
        /// Optional explicit transport override. When <see langword="null"/> (the default), FIDO2 selects a
        /// transport in its documented default order: <see cref="ConnectionType.HidFido"/>, then
        /// <see cref="ConnectionType.SmartCard"/>. When set, it must be one of those two transports and
        /// supported by the device; otherwise an <see cref="ArgumentException"/> (invalid transport) or
        /// <see cref="NotSupportedException"/> (transport not available on this device) is thrown.
        /// </param>
        /// <param name="cancellationToken">An optional token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="FidoSession"/> instance configured for the YubiKey device.
        /// The session must be disposed by the caller when no longer needed.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown if the YubiKey does not expose a FIDO-capable connection
        /// (<see cref="ConnectionType.HidFido"/> or <see cref="ConnectionType.SmartCard"/>), or if an
        /// explicit <paramref name="preferredConnection"/> is valid for FIDO2 but not exposed by this device.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="preferredConnection"/> is not a single concrete transport or is a
        /// transport FIDO2 cannot use (for example <see cref="ConnectionType.HidOtp"/>).
        /// </exception>
        /// <remarks>
        /// <para>
        /// FIDO2 sessions can be created over two transport types:
        /// <list type="bullet">
        /// <item><description>FIDO HID: Uses CTAP HID protocol for USB communication (the default first choice)</description></item>
        /// <item><description>SmartCard (CCID): Uses ISO 7816-4 APDUs over the FIDO2 AID (NFC, or USB on firmware 5.8.0+)</description></item>
        /// </list>
        /// When a device exposes both, the default selects HID FIDO; pass
        /// <paramref name="preferredConnection"/> = <see cref="ConnectionType.SmartCard"/> to force SmartCard.
        /// </para>
        /// <para>
        /// SCP (Secure Channel Protocol) is only supported on the SmartCard transport. Supplying
        /// <paramref name="scpKeyParams"/> while a non-SmartCard transport is selected — including the default
        /// HID FIDO first choice — causes session initialization to throw <see cref="NotSupportedException"/>
        /// ("SCP is only supported on SmartCard protocols"). To use SCP on a device that also exposes HID FIDO,
        /// explicitly select SmartCard via <paramref name="preferredConnection"/>:
        /// <c>ConnectionType.SmartCard</c>. (This phase does not change SCP semantics; transport selection is
        /// independent of <paramref name="scpKeyParams"/>.)
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Create a FIDO session and get authenticator info
        /// await using var fidoSession = await yubiKey.CreateFidoSessionAsync();
        /// var info = await fidoSession.GetInfoAsync();
        /// 
        /// // Create a session with SCP03 over SmartCard (force the SmartCard transport)
        /// using var scpKeys = Scp03KeyParameters.Default;
        /// await using var secureSession = await yubiKey.CreateFidoSessionAsync(
        ///     scpKeyParams: scpKeys, preferredConnection: ConnectionType.SmartCard);
        /// </code>
        /// </example>
        public async Task<FidoSession> CreateFidoSessionAsync(
            ScpKeyParameters? scpKeyParams = null,
            ProtocolConfiguration? configuration = null,
            ConnectionType? preferredConnection = null,
            CancellationToken cancellationToken = default)
        {
            var candidates = yubiKey.ResolveFidoSessionTransports(
                scpKeyParams is not null && preferredConnection is null ? ConnectionType.SmartCard : preferredConnection);

            return await yubiKey.ConnectSessionTransportAsync(
                    candidates,
                    "FIDO2",
                    async (connection, _, ct) => await FidoSession.CreateAsync(
                            connection,
                            configuration,
                            scpKeyParams,
                            cancellationToken: ct)
                        .ConfigureAwait(false),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Source-compatibility overload preserving the pre-Phase-38 positional shape
        /// (<c>scpKeyParams, configuration, cancellationToken</c>); forwards using the default transport order.
        /// </summary>
        /// <param name="scpKeyParams">Optional SCP key parameters (SmartCard only).</param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="cancellationToken">An optional token to cancel the operation.</param>
        public Task<FidoSession> CreateFidoSessionAsync(
            ScpKeyParameters? scpKeyParams,
            ProtocolConfiguration? configuration,
            CancellationToken cancellationToken) =>
            yubiKey.CreateFidoSessionAsync(scpKeyParams, configuration, null, cancellationToken);

        /// <summary>
        /// Resolves the candidate transports for FIDO2.
        /// </summary>
        /// <param name="preferredConnection">Optional explicit transport override (see CreateFidoSessionAsync).</param>
        /// <returns>Candidate transports suitable for FIDO2 operations.</returns>
        private IReadOnlyList<ConnectionType> ResolveFidoSessionTransports(ConnectionType? preferredConnection)
        {
            // FIDO2 is dual-transport (HID FIDO or SmartCard FIDO2). The app-specific smart default prefers
            // HID FIDO, then SmartCard (NFC, or USB on firmware 5.8.0+); an explicit override can force either.
            // The ordered candidate list drives ConnectSessionTransportAsync, which opens the most-preferred
            // candidate and falls back when a held SmartCard transport is detected (Phase 38.5).
            try
            {
                return yubiKey.ResolveSessionTransports(preferredConnection, "FIDO2", FidoTransportOrder);
            }
            catch (NotSupportedException) when (preferredConnection is null)
            {
                // The remap stays scoped to the resolve call ONLY: only the default path (no override) remaps
                // to the FIDO-specific "no FIDO-capable connection" message. An explicit-override failure
                // carries an accurate, override-specific diagnostic from ResolveSessionTransports (e.g. "does
                // not expose the requested SmartCard connection"), and a connect/held/fallback error from
                // ConnectSessionTransportAsync must surface unchanged — neither is masked by this message.
                throw new NotSupportedException(
                    $"This YubiKey does not expose a FIDO-capable connection (available: {yubiKey.AvailableConnections}). " +
                    "FIDO2 requires HID FIDO or SmartCard.");
            }
        }
    }

    // FIDO2 default transport order: HID FIDO first (primary USB FIDO2 interface), then SmartCard FIDO2.
    private static readonly ConnectionType[] FidoTransportOrder =
        [ConnectionType.HidFido, ConnectionType.SmartCard];
}