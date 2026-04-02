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

using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.OpenPgp.IntegrationTests.Helpers;
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
        ///     When <c>true</c>, performs a factory reset of the OpenPGP applet before running the test.
        /// </param>
        /// <param name="action">The async action to execute with the session.</param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task WithOpenPgpSessionAsync(
            bool resetBeforeUse,
            Func<OpenPgpSession, Task> action,
            ProtocolConfiguration? configuration = null,
            ScpKeyParameters? scpKeyParams = null,
            CancellationToken cancellationToken = default) =>
            state.WithConnectionAsync(async connection =>
            {
                var sharedConnection = new SharedSmartCardConnection(connection);

                if (resetBeforeUse)
                {
                    using var resetSession = await state.Device
                        .CreateOpenPgpSessionAsync(
                            sharedConnection,
                            configuration: configuration,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    await resetSession.ResetAsync(cancellationToken).ConfigureAwait(false);
                }

                using var session = await state.Device
                    .CreateOpenPgpSessionAsync(
                        sharedConnection,
                        scpKeyParams,
                        configuration,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                await action(session).ConfigureAwait(false);
            }, cancellationToken);
    }
}
