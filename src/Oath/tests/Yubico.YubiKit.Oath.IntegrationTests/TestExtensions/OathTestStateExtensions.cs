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

namespace Yubico.YubiKit.Oath.IntegrationTests.TestExtensions;

/// <summary>
///     Extensions that help integration tests acquire <see cref="OathSession" />
///     instances while ensuring connections are disposed correctly.
/// </summary>
public static class OathTestStateExtensions
{
    extension(YubiKeyTestState state)
    {
        /// <summary>
        ///     Executes an action with an OATH session.
        ///     Optionally resets the OATH application before running the action.
        /// </summary>
        /// <param name="action">The async action to execute with the session.</param>
        /// <param name="resetBeforeUse">
        ///     When <c>true</c>, resets the OATH application before running the test action.
        ///     Defaults to <c>true</c>.
        /// </param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task WithOathSessionAsync(
            Func<OathSession, Task> action,
            bool resetBeforeUse = true,
            ProtocolConfiguration? configuration = null,
            ScpKeyParameters? scpKeyParams = null,
            CancellationToken cancellationToken = default)
        {
            await using var session = await state.Device
                .CreateOathSessionAsync(scpKeyParams, configuration, cancellationToken)
                .ConfigureAwait(false);

            if (resetBeforeUse)
            {
                await session.ResetAsync(cancellationToken).ConfigureAwait(false);
            }

            await action(session).ConfigureAwait(false);
        }
    }
}
