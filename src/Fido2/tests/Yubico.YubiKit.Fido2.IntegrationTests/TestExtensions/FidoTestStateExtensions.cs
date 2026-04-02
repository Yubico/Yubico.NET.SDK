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

using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Fido2.IntegrationTests.TestExtensions;

/// <summary>
///     Extensions that help integration tests acquire <see cref="FidoSession" />
///     instances while ensuring connections are disposed correctly.
/// </summary>
public static class FidoTestStateExtensions
{
    extension(YubiKeyTestState state)
    {
        /// <summary>
        ///     Executes an action with a FIDO2 session.
        ///     Automatically handles connection and session lifecycle.
        /// </summary>
        /// <param name="action">The async action to execute with the session.</param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task WithFidoSessionAsync(
            Func<FidoSession, Task> action,
            ProtocolConfiguration? configuration = null,
            ScpKeyParameters? scpKeyParams = null,
            CancellationToken cancellationToken = default)
        {
            await using var session = await state.Device
                .CreateFidoSessionAsync(scpKeyParams, configuration, cancellationToken)
                .ConfigureAwait(false);

            await action(session).ConfigureAwait(false);
        }

        /// <summary>
        ///     Executes an action with a FIDO2 session and returns the authenticator info.
        ///     Automatically handles connection and session lifecycle.
        /// </summary>
        /// <param name="action">The async action to execute with the session and info.</param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task WithFidoSessionAsync(
            Func<FidoSession, AuthenticatorInfo, Task> action,
            ProtocolConfiguration? configuration = null,
            ScpKeyParameters? scpKeyParams = null,
            CancellationToken cancellationToken = default)
        {
            await using var session = await state.Device
                .CreateFidoSessionAsync(scpKeyParams, configuration, cancellationToken)
                .ConfigureAwait(false);

            var info = await session.GetInfoAsync(cancellationToken).ConfigureAwait(false);
            await action(session, info).ConfigureAwait(false);
        }
    }
}
