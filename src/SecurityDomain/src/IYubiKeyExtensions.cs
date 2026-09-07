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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
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
        ///     Always use a <c>using</c> statement or call <see cref="IDisposable.Dispose" /> when finished.
        /// </remarks>
        /// <exception cref="SecureChannelException">
        ///     Secure-channel parameters were supplied and establishing the SCP secure channel failed.
        ///     The original failure is available as <see cref="Exception.InnerException" />.
        /// </exception>
        public async Task<SecurityDomainSession> CreateSecurityDomainSessionAsync(
            SessionCreationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var configuration = options?.ProtocolConfiguration;
            var scpKeyParams = options?.ScpKeyParameters;
            var preferredConnectionType = options?.PreferredConnectionType;
            var firmwareVersionOverride = options?.FirmwareVersionOverride;
            var transport = yubiKey.ResolveSessionTransport(
                preferredConnectionType,
                "Security Domain",
                ConnectionType.SmartCard);

            return await yubiKey.CreateSessionOverTransportAsync(
                    transport,
                    async (connection, ct) =>
                    {
                        var session = await SecurityDomainSession.CreateAsync(
                                (ISmartCardConnection)connection,
                                new SessionCreationOptions
                                {
                                    ProtocolConfiguration = configuration,
                                    ScpKeyParameters = scpKeyParams,
                                    PreferredConnectionType = transport,
                                    FirmwareVersionOverride = firmwareVersionOverride
                                },
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
        public async Task<IReadOnlyList<KeyInfo>> GetSecurityDomainKeyInfoAsync(
            SessionCreationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            await using var session = await yubiKey.CreateSecurityDomainSessionAsync(
                    options,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return await session.GetKeyInfoAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}