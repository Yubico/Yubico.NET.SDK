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

namespace Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;

/// <summary>
///     Extensions that help integration tests acquire <see cref="SecurityDomainSession" />
///     instances while ensuring connections are disposed correctly.
/// </summary>
public static class TestStateExtensions
{

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
                if (resetBeforeUse)
                {
                    using var resetSession = await SecurityDomainSession.CreateAsync(
                            connection,
                            new SessionCreationOptions
                            {
                                ProtocolConfiguration = configuration,
                                FirmwareVersionOverride = state.FirmwareVersion
                            },
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    await resetSession.ResetAsync(cancellationToken).ConfigureAwait(false);
                }

                using var session = await SecurityDomainSession.CreateAsync(
                        connection,
                        new SessionCreationOptions
                        {
                            ScpKeyParameters = scpKeyParams,
                            ProtocolConfiguration = configuration,
                            FirmwareVersionOverride = state.FirmwareVersion
                        },
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                await action(session).ConfigureAwait(false);
            }, cancellationToken);

        /// <summary>
        ///     Restores the Security Domain to its factory state, reinstating the default SCP03 keys.
        /// </summary>
        /// <remarks>
        ///     Tests that rotate or delete SCP03 keys must call this when they finish. Deleting the last
        ///     key set leaves the device with no key matching <see cref="Scp03KeyParameters.Default" />, so
        ///     every later consumer of the default keys fails secure-channel establishment with
        ///     <c>SW=0x6A88</c> (referenced data not found). That failure surfaces in whichever suite runs
        ///     next rather than in the test that caused it, so resetting here keeps the integration suites
        ///     order-independent.
        /// </remarks>
        public Task ResetSecurityDomainAsync(CancellationToken cancellationToken = default) =>
            state.WithConnectionAsync(async connection =>
            {
                using var session = await SecurityDomainSession.CreateAsync(
                        connection,
                        new SessionCreationOptions { FirmwareVersionOverride = state.FirmwareVersion },
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                await session.ResetAsync(cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
    }

}