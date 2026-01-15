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

using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;

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
        /// <param name="scpKeyParams">Optional SCP key parameters for secure channel authentication.</param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="firmwareVersion">Optional firmware version override.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A new <see cref="SecurityDomainSession" /> instance.</returns>
        /// <remarks>
        ///     The returned session owns the underlying connection and will dispose it when the session is disposed.
        ///     Always use a <c>using</c> statement or call <see cref="SecurityDomainSession.Dispose" /> when finished.
        /// </remarks>
        public async Task<SecurityDomainSession> CreateSecurityDomainSessionAsync(
            ScpKeyParameters? scpKeyParams = null,
            ProtocolConfiguration? configuration = null,
            FirmwareVersion? firmwareVersion = null,
            CancellationToken cancellationToken = default)
        {
            var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken)
                .ConfigureAwait(false);
            return await SecurityDomainSession.CreateAsync(
                    connection,
                    configuration,
                    scpKeyParams,
                    firmwareVersion,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        ///     Creates a new Security Domain session using an existing connection.
        /// </summary>
        /// <param name="existingConnection">An existing SmartCard connection to use.</param>
        /// <param name="scpKeyParams">Optional SCP key parameters for secure channel authentication.</param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="firmwareVersion">Optional firmware version override.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A new <see cref="SecurityDomainSession" /> instance.</returns>
        /// <remarks>
        ///     The session does NOT own the provided connection. The caller is responsible for
        ///     managing the connection lifecycle.
        /// </remarks>
        internal async Task<SecurityDomainSession> CreateSecurityDomainSessionAsync(
            ISmartCardConnection existingConnection,
            ScpKeyParameters? scpKeyParams = null,
            ProtocolConfiguration? configuration = null,
            FirmwareVersion? firmwareVersion = null,
            CancellationToken cancellationToken = default) =>
            await SecurityDomainSession.CreateAsync(
                    existingConnection,
                    configuration,
                    scpKeyParams,
                    firmwareVersion,
                    cancellationToken)
                .ConfigureAwait(false);

        /// <summary>
        ///     Gets key information from the Security Domain.
        /// </summary>
        /// <param name="scpKeyParams">Optional SCP key parameters for secure channel authentication.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of key information from the Security Domain.</returns>
        public async Task<IReadOnlyList<SecurityDomainKeyInfo>> GetSecurityDomainKeyInfoAsync(
            ScpKeyParameters? scpKeyParams = null,
            CancellationToken cancellationToken = default)
        {
            using var session = await yubiKey.CreateSecurityDomainSessionAsync(
                    scpKeyParams,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return await session.GetKeyInfoAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}