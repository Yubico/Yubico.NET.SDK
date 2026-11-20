// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
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
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.SecurityDomain;

/// <summary>
///     Extension methods for creating Security Domain sessions from an <see cref="IYubiKey" />.
/// </summary>
public static class IYubiKeyExtensions
{
    extension(IYubiKey yubiKey)
    {
        /// <summary>
        ///     Creates a new Security Domain session for the specified YubiKey.
        /// </summary>
        /// <param name="options">Optional cross-cutting session creation settings.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A new <see cref="SecurityDomainSession" /> instance.</returns>
        /// <remarks>
        ///     The returned session owns the underlying connection and will dispose it when the session is disposed.
        ///     Use an await using scope. Only SmartCard transport (USB CCID or NFC) is supported.
        ///     The caller retains ownership of supplied SCP parameters and disposes them after the session.
        ///     Supply FirmwareVersionOverride when the exact firmware is known: Security Domain cannot
        ///     detect it and otherwise configures with 5.3.0. Reset currently omits the original protocol
        ///     configuration/version; create a fresh session with those options when needed after reset.
        ///     SCP11 setup requires a known supported override (normally 5.7.2+); the default 5.3.0
        ///     fails Core's local gate before authentication. An override is policy, not hardware detection.
        /// </remarks>
        /// <exception cref="SecureChannelException">
        ///     SCP parameters were supplied and secure-channel configuration threw ApduException,
        ///     BadResponseException, or NotSupportedException. The original failure is InnerException.
        ///     Earlier protocol preparation/application selection failures and cancellation are not wrapped here.
        /// </exception>
        public async Task<SecurityDomainSession> CreateSecurityDomainSessionAsync(
            SessionCreationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var preferredConnectionType = options?.PreferredConnectionType;
            var transport = yubiKey.ResolveSessionTransport(
                preferredConnectionType,
                "Security Domain",
                ConnectionType.SmartCard);
            var sessionOptions = (options ?? new SessionCreationOptions())
                .WithPreferredConnectionType(transport);

            return await yubiKey.CreateSessionOverTransportAsync(
                    transport,
                    async (connection, ct) =>
                    {
                        var session = await SecurityDomainSession.CreateAsync(
                                (ISmartCardConnection)connection,
                                sessionOptions,
                                ct)
                            .ConfigureAwait(false);
                        session.OwnConnection();
                        return session;
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        ///     Gets key information from the Security Domain.
        /// </summary>
        /// <param name="options">Optional cross-cutting session creation settings.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of key information from the Security Domain.</returns>
        public async Task<IReadOnlyList<KeyInfo>> ListKeyInformationAsync(
            SessionCreationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            await using var session = await yubiKey.CreateSecurityDomainSessionAsync(
                    options,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return await session.ListKeyInformationAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}