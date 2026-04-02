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

using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Convenience extensions on <see cref="IYubiKey" /> for OpenPGP operations.
/// </summary>
public static class IYubiKeyExtensions
{
    extension(IYubiKey yubiKey)
    {
        /// <summary>
        ///     Creates and initializes an OpenPGP session, acquiring a SmartCard connection automatically.
        /// </summary>
        /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
        /// <param name="configuration">Optional protocol configuration overrides.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>An initialized <see cref="OpenPgpSession" />. The caller owns its lifetime.</returns>
        public async Task<OpenPgpSession> CreateOpenPgpSessionAsync(
            ScpKeyParameters? scpKeyParams = null,
            ProtocolConfiguration? configuration = null,
            CancellationToken cancellationToken = default)
        {
            var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken)
                .ConfigureAwait(false);
            return await OpenPgpSession.CreateAsync(
                    connection,
                    configuration,
                    scpKeyParams,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        ///     Creates an OpenPGP session using an existing SmartCard connection.
        /// </summary>
        internal async Task<OpenPgpSession> CreateOpenPgpSessionAsync(
            ISmartCardConnection existingConnection,
            ScpKeyParameters? scpKeyParams = null,
            ProtocolConfiguration? configuration = null,
            CancellationToken cancellationToken = default) =>
            await OpenPgpSession.CreateAsync(
                    existingConnection,
                    configuration,
                    scpKeyParams,
                    cancellationToken)
                .ConfigureAwait(false);
    }
}