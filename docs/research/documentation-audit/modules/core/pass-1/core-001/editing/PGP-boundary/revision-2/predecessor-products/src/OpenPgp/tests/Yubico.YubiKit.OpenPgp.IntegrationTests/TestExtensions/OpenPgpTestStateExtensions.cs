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

using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.OpenPgp.IntegrationTests.TestExtensions;

/// <summary>
///     Extensions that help integration tests acquire <see cref="OpenPgpSession" />
///     instances while ensuring connections are disposed correctly.
/// </summary>
public static class OpenPgpTestStateExtensions
{
    extension(YubiKeyTestState state)
    {
        /// <summary>
        ///     Executes an action with an <see cref="OpenPgpSession" />, optionally resetting
        ///     the OpenPGP applet before use.
        /// </summary>
        /// <param name="resetBeforeUse">
        ///     When true, calls ResetAsync before the action. Its return supplies no full reset postcondition;
        ///     the helper leaves post-action state in place and performs no final reset/readback. ATT material
        ///     survives a successful YubiKey application reset.
        /// </param>
        /// <param name="action">The async action to execute with the session.</param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="scpKeyParams">
        ///     Borrowed SCP parameters for the action's session only. The optional reset uses a separate plain
        ///     session, disposed before this session is created; this helper does not test reset over SCP.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        ///     WithConnectionAsync owns the connection; both sessions borrow it sequentially. The action must await
        ///     all its session work before returning, because its session is synchronously disposed afterward.
        /// </remarks>
        public Task WithOpenPgpSessionAsync(
            bool resetBeforeUse,
            Func<OpenPgpSession, Task> action,
            ProtocolConfiguration? configuration = null,
            ScpKeyParameters? scpKeyParams = null,
            CancellationToken cancellationToken = default) =>
            state.WithConnectionAsync(async connection =>
            {
                if (resetBeforeUse)
                {
                    using var resetSession = await state.Device
                        .CreateOpenPgpSessionAsync(
                            connection,
                            new SessionCreationOptions { ProtocolConfiguration = configuration },
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    await resetSession.ResetAsync(cancellationToken).ConfigureAwait(false);
                }

                using var session = await state.Device
                    .CreateOpenPgpSessionAsync(
                        connection,
                        new SessionCreationOptions
                        {
                            ScpKeyParameters = scpKeyParams,
                            ProtocolConfiguration = configuration
                        },
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                await action(session).ConfigureAwait(false);
            }, cancellationToken);
    }
}
