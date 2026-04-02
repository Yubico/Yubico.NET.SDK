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

using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.YubiHsm;

/// <summary>
///     Extension methods for creating YubiHSM Auth sessions from an <see cref="IYubiKey" />.
/// </summary>
public static class IYubiKeyExtensions
{
    extension(IYubiKey yubiKey)
    {
        /// <summary>
        ///     Creates a new YubiHSM Auth session for the specified YubiKey.
        /// </summary>
        /// <param name="scpKeyParams">Optional SCP key parameters for secure channel authentication.</param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="firmwareVersion">Optional firmware version override.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A new <see cref="HsmAuthSession" /> instance.</returns>
        /// <remarks>
        ///     The returned session owns the underlying connection and will dispose it when the session is disposed.
        ///     Always use a <c>using</c> statement or call <c>Dispose</c> when finished.
        /// </remarks>
        public async Task<HsmAuthSession> CreateHsmAuthSessionAsync(
            ScpKeyParameters? scpKeyParams = null,
            ProtocolConfiguration? configuration = null,
            FirmwareVersion? firmwareVersion = null,
            CancellationToken cancellationToken = default)
        {
            var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken)
                .ConfigureAwait(false);
            return await HsmAuthSession.CreateAsync(
                    connection,
                    configuration,
                    scpKeyParams,
                    firmwareVersion,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        ///     Lists all YubiHSM Auth credentials stored on the YubiKey.
        ///     Creates and disposes a session automatically.
        /// </summary>
        /// <param name="scpKeyParams">Optional SCP key parameters for secure channel authentication.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A read-only list of stored credentials.</returns>
        public async Task<IReadOnlyList<HsmAuthCredential>> ListHsmAuthCredentialsAsync(
            ScpKeyParameters? scpKeyParams = null,
            CancellationToken cancellationToken = default)
        {
            await using var session = await yubiKey.CreateHsmAuthSessionAsync(
                    scpKeyParams,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return await session.ListCredentialsAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
