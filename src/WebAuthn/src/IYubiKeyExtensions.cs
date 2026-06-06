// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.WebAuthn.Client;

namespace Yubico.YubiKit.WebAuthn;

/// <summary>
/// Extension methods for creating WebAuthn clients from YubiKey devices.
/// </summary>
public static class IYubiKeyExtensions
{
    extension(IYubiKey yubiKey)
    {
        /// <summary>
        /// Creates a WebAuthn client for the YubiKey asynchronously.
        /// </summary>
        /// <param name="origin">The WebAuthn origin for client data JSON.</param>
        /// <param name="isPublicSuffix">Checker used to reject public-suffix RP IDs.</param>
        /// <param name="enterpriseRpIds">Optional set of enterprise-allowed RP IDs.</param>
        /// <param name="scpKeyParams">Optional SCP key parameters for SmartCard FIDO2 sessions.</param>
        /// <param name="configuration">Optional FIDO2 protocol configuration.</param>
        /// <param name="cancellationToken">An optional token to cancel the operation.</param>
        /// <returns>A <see cref="WebAuthnClient"/> that owns the underlying FIDO2 session.</returns>
        /// <remarks>
        /// The public suffix checker should be backed by Public Suffix List data. RP ID validation
        /// rejects public suffixes such as <c>com</c> and <c>co.uk</c> before any CTAP operation runs.
        /// </remarks>
        public async Task<WebAuthnClient> CreateWebAuthnClientAsync(
            WebAuthnOrigin origin,
            PublicSuffixChecker isPublicSuffix,
            IReadOnlySet<string>? enterpriseRpIds = null,
            ScpKeyParameters? scpKeyParams = null,
            ProtocolConfiguration? configuration = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(origin);
            ArgumentNullException.ThrowIfNull(isPublicSuffix);

            var fidoSession = await yubiKey.CreateFidoSessionAsync(
                    scpKeyParams,
                    configuration,
                    cancellationToken)
                .ConfigureAwait(false);

            try
            {
                return new WebAuthnClient(fidoSession, origin, isPublicSuffix, enterpriseRpIds);
            }
            catch
            {
                await fidoSession.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }
    }
}