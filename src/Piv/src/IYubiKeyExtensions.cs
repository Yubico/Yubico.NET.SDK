// Copyright 2024 Yubico AB
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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Piv;

/// <summary>
/// Extension methods for <see cref="IYubiKey"/> to create PIV sessions.
/// </summary>
public static class IYubiKeyExtensions
{
    extension(IYubiKey yubiKey)
    {
        /// <summary>
        /// Creates a PIV session with the YubiKey.
        /// </summary>
        /// <param name="options">Optional cross-cutting session creation settings.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An initialized PIV session.</returns>
        /// <exception cref="NotSupportedException">If the YubiKey does not support PIV or SmartCard connections.</exception>
        public async Task<PivSession> CreatePivSessionAsync(
            SessionCreationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var configuration = options?.ProtocolConfiguration;
            var scpKeyParams = options?.ScpKeyParameters;
            var preferredConnectionType = options?.PreferredConnectionType;
            var firmwareVersionOverride = options?.FirmwareVersionOverride;
            var transport = yubiKey.ResolveSessionTransport(
                preferredConnectionType,
                "PIV",
                ConnectionType.SmartCard);

            return await yubiKey.CreateSessionOverTransportAsync(
                    transport,
                    async (connection, ct) =>
                    {
                        var session = await PivSession.CreateAsync(
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
    }
}