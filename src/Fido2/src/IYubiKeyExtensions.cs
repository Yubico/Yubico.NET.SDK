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

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;

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
        /// <param name="options">Optional cross-cutting session creation settings.</param>
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
        public async Task<AuthenticatorInfo> GetFidoInfoAsync(
            SessionCreationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            await using var fidoSession = await yubiKey.CreateFidoSessionAsync(options, cancellationToken)
                .ConfigureAwait(false);
            return await fidoSession.GetInfoAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a FIDO2 session for interacting with a YubiKey asynchronously.
        /// </summary>
        /// <param name="options">
        /// Optional creation settings. FIDO2 selects HID FIDO, then SmartCard unless
        /// <see cref="SessionCreationOptions.PreferredConnectionType" /> specifies a supported transport.
        /// Secure-channel parameters force SmartCard when no preference is specified.
        /// </param>
        /// <param name="cancellationToken">An optional token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="FidoSession"/> instance configured for the YubiKey device.
        /// The session must be disposed by the caller when no longer needed.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown if the YubiKey does not expose a FIDO-capable connection
        /// (<see cref="ConnectionType.HidFido"/> or <see cref="ConnectionType.SmartCard"/>), or if an
        /// an explicitly preferred connection is valid for FIDO2 but not exposed by this device.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the preferred connection is not a single concrete transport or is a
        /// transport FIDO2 cannot use (for example <see cref="ConnectionType.HidOtp"/>).
        /// </exception>
        /// <exception cref="ConnectionInUseException">The physical YubiKey already has a live connection.</exception>
        /// <remarks>
        /// <para>
        /// FIDO2 sessions can be created over two transport types:
        /// <list type="bullet">
        /// <item><description>FIDO HID: Uses CTAP HID protocol for USB communication (the default first choice)</description></item>
        /// <item><description>SmartCard (CCID): Uses ISO 7816-4 APDUs over the FIDO2 AID (NFC, or USB on firmware 5.8.0+)</description></item>
        /// </list>
        /// When a device exposes both, the default selects HID FIDO; pass
        /// Set <see cref="SessionCreationOptions.PreferredConnectionType" /> to
        /// <see cref="ConnectionType.SmartCard"/> to force SmartCard.
        /// </para>
        /// <para>
        /// SCP (Secure Channel Protocol) is supported only on SmartCard. Supplying
        /// Secure-channel parameters without an explicit override select SmartCard automatically.
        /// Explicitly selecting HID FIDO with SCP parameters causes session initialization to throw
        /// <see cref="NotSupportedException"/> ("SCP is only supported on SmartCard protocols").
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
        ///     new SessionCreationOptions
        ///     {
        ///         ScpKeyParameters = scpKeys,
        ///         PreferredConnectionType = ConnectionType.SmartCard
        ///     });
        /// </code>
        /// </example>
        public async Task<FidoSession> CreateFidoSessionAsync(
            SessionCreationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var scpKeyParams = options?.ScpKeyParameters;
            var preferredConnectionType = options?.PreferredConnectionType;
            var transport = yubiKey.ResolveFidoSessionTransport(
                scpKeyParams is not null && preferredConnectionType is null
                    ? ConnectionType.SmartCard
                    : preferredConnectionType);
            var sessionOptions = (options ?? new SessionCreationOptions())
                .WithPreferredConnectionType(transport);

            return await yubiKey.CreateSessionOverTransportAsync(
                    transport,
                    async (connection, ct) =>
                    {
                        var session = await FidoSession
                            .CreateAsync(
                                connection,
                                sessionOptions,
                                ct)
                            .ConfigureAwait(false);

                        // This entry point opened the connection, so the session it returns is the only thing
                        // that can close it. A caller-created connection is never owned this way.
                        session.OwnConnection();
                        return session;
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Resolves the single transport selected for FIDO2.
        /// </summary>
        /// <param name="preferredConnection">Optional explicit transport override (see CreateFidoSessionAsync).</param>
        /// <returns>The selected FIDO2 transport.</returns>
        private ConnectionType ResolveFidoSessionTransport(ConnectionType? preferredConnection)
        {
            // FIDO2 is dual-transport (HID FIDO or SmartCard FIDO2). The app-specific smart default selects
            // the first exposed transport: HID FIDO, otherwise SmartCard (NFC, or USB on firmware 5.8.0+).
            // Select one transport so a held HID FIDO interface cannot silently create a second FidoSession
            // over SmartCard on the same physical key. Callers can still force either transport explicitly.
            try
            {
                return yubiKey.ResolveSessionTransport(
                    preferredConnection,
                    "FIDO2",
                    FidoTransportOrder);
            }
            catch (NotSupportedException) when (preferredConnection is null)
            {
                // The remap stays scoped to the resolve call ONLY: only the default path (no override) remaps
                // to the FIDO-specific "no FIDO-capable connection" message. An explicit-override failure
                // carries an accurate, override-specific diagnostic from ResolveSessionTransport (e.g. "does
                // not expose the requested SmartCard connection"). Connection failures surface unchanged.
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