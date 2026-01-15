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
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;

/// <summary>
///     Extensions that help integration tests acquire <see cref="SecurityDomainSession" />
///     instances while ensuring connections are disposed correctly.
/// </summary>
public static class SecurityDomainTestStateExtensions
{
    #region Nested type: $extension

    extension(YubiKeyTestState state)
    {
        public Task WithSecurityDomainSessionAsync(
            bool resetBeforeUse,
            Func<SecurityDomainSession, Task> action,
            ProtocolConfiguration? configuration = null,
            ScpKeyParameters? scpKeyParams = null,
            CancellationToken cancellationToken = default) =>
            state.WithConnectionAsync(async connection =>
            {
                var sharedConnection = new SharedSmartCardConnection(connection);

                if (resetBeforeUse)
                {
                    using var resetSession = await state.Device
                        .CreateSecurityDomainSessionAsync(
                            sharedConnection,
                            configuration: configuration,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    await resetSession.ResetAsync(cancellationToken).ConfigureAwait(false);
                }

                using var session = await state.Device
                    .CreateSecurityDomainSessionAsync(
                        sharedConnection,
                        scpKeyParams,
                        configuration,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                await action(session).ConfigureAwait(false);
            }, cancellationToken);
    }

    #endregion
}