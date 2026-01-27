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
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;

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
        /// use <see cref="CreateFidoSessionAsync"/> instead to reuse the session.
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
        /// <param name="cancellationToken">An optional token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="FidoSession"/> instance configured for the YubiKey device.
        /// The session must be disposed by the caller when no longer needed.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown if the YubiKey's connection type is not supported for FIDO2.
        /// Supported types are <see cref="ConnectionType.SmartCard"/> (SmartCard) and 
        /// <see cref="ConnectionType.HidFido"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// FIDO2 sessions can be created over two transport types:
        /// <list type="bullet">
        /// <item><description>SmartCard (CCID): Uses ISO 7816-4 APDUs over the FIDO2 AID</description></item>
        /// <item><description>FIDO HID: Uses CTAP HID protocol for USB communication</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// SCP (Secure Channel Protocol) is only supported for SmartCard connections.
        /// If SCP parameters are provided for a HID connection, they will be ignored.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Create a FIDO session and get authenticator info
        /// await using var fidoSession = await yubiKey.CreateFidoSessionAsync();
        /// var info = await fidoSession.GetInfoAsync();
        /// 
        /// // Create a session with SCP03 for SmartCard
        /// using var scpKeys = Scp03KeyParameters.Default;
        /// await using var secureSession = await yubiKey.CreateFidoSessionAsync(scpKeyParams: scpKeys);
        /// </code>
        /// </example>
        public async Task<FidoSession> CreateFidoSessionAsync(
            ScpKeyParameters? scpKeyParams = null,
            ProtocolConfiguration? configuration = null,
            CancellationToken cancellationToken = default)
        {
            var connection = await yubiKey.ConnectAsync(cancellationToken).ConfigureAwait(false);
            return await FidoSession.CreateAsync(
                    connection,
                    configuration,
                    scpKeyParams,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Connects to a YubiKey using the appropriate connection type for FIDO2.
        /// </summary>
        /// <param name="cancellationToken">An optional token to cancel the operation.</param>
        /// <returns>A connection suitable for FIDO2 operations.</returns>
        /// <exception cref="NotSupportedException">
        /// Thrown if the YubiKey's connection type is not supported for FIDO2.
        /// </exception>
        private async Task<IConnection> ConnectAsync(CancellationToken cancellationToken)
            =>
                yubiKey.ConnectionType switch
                {
                    ConnectionType.SmartCard => await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken)
                        .ConfigureAwait(false),
                    ConnectionType.HidFido => await yubiKey.ConnectAsync<IFidoHidConnection>(cancellationToken)
                        .ConfigureAwait(false),
                    _ => throw new NotSupportedException(
                        $"Connection type {yubiKey.ConnectionType} is not supported for FIDO2 session creation. " +
                        "Use Ccid or HidFido connection types."),
                };
    }
}
