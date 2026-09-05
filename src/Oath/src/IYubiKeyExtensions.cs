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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Oath;

/// <summary>
///     Provides convenience extension methods for OATH operations on <see cref="IYubiKey" />.
/// </summary>
public static class IYubiKeyExtensions
{
    extension(IYubiKey yubiKey)
    {
        /// <summary>
        ///     Creates a new OATH session for the specified YubiKey.
        /// </summary>
        /// <param name="options">Optional cross-cutting session creation settings.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        ///     A new <see cref="OathSession" /> instance. The caller owns the session and must dispose it.
        /// </returns>
        public async Task<OathSession> CreateOathSessionAsync(
            SessionCreationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var preferredConnectionType = options?.PreferredConnectionType;
            var transport = yubiKey.ResolveSessionTransport(
                preferredConnectionType,
                "OATH",
                ConnectionType.SmartCard);
            var sessionOptions = (options ?? new SessionCreationOptions())
                .WithPreferredConnectionType(transport);

            return await yubiKey.CreateSessionOverTransportAsync(
                    transport,
                    async (connection, ct) =>
                    {
                        var session = await OathSession.CreateAsync(
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
        ///     Lists all OATH credentials stored on the YubiKey.
        /// </summary>
        /// <param name="options">Optional cross-cutting session creation settings.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of credentials stored on the device.</returns>
        public async Task<IReadOnlyList<Credential>> ListOathCredentialsAsync(
            SessionCreationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            await using var session = await yubiKey.CreateOathSessionAsync(options, cancellationToken)
                .ConfigureAwait(false);
            return await session.ListCredentialsAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///     Calculates OTP codes for all OATH credentials stored on the YubiKey.
        ///     HOTP and touch-required credentials return <c>null</c> codes.
        /// </summary>
        /// <param name="timestamp">Optional Unix timestamp. Defaults to current time.</param>
        /// <param name="options">Optional cross-cutting session creation settings.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A dictionary mapping each credential to its calculated code (or null).</returns>
        public async Task<IReadOnlyDictionary<Credential, Code?>> CalculateAllOathCodesAsync(
            long? timestamp = null,
            SessionCreationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            await using var session = await yubiKey.CreateOathSessionAsync(options, cancellationToken)
                .ConfigureAwait(false);
            return await session.CalculateAllAsync(timestamp, cancellationToken).ConfigureAwait(false);
        }
    }
}
