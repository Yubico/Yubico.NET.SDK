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

using Microsoft.Extensions.DependencyInjection;
using Yubico.YubiKit.Core;
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

        /// <summary>
        ///     Executes an action with a <see cref="SecurityDomainSession" /> created via the
        ///     DI-registered <see cref="SecurityDomainSessionFactory" />.
        /// </summary>
        /// <remarks>
        ///     This method builds a <see cref="ServiceProvider" /> internally with
        ///     <see cref="DependencyInjection.AddYubiKeySecurityDomain" /> registered,
        ///     then resolves and invokes the factory. Use this for integration tests
        ///     that verify the standard DI registration works end-to-end.
        /// </remarks>
        /// <param name="resetBeforeUse">
        ///     When <c>true</c>, resets the Security Domain before running the test action.
        /// </param>
        /// <param name="action">The async action to execute with the session.</param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="scpKeyParams">Optional SCP key parameters for authentication.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task WithSecurityDomainSessionFromDIAsync(
            bool resetBeforeUse,
            Func<SecurityDomainSession, Task> action,
            ProtocolConfiguration? configuration = null,
            ScpKeyParameters? scpKeyParams = null,
            CancellationToken cancellationToken = default)
        {
            var services = new ServiceCollection();
            services.AddYubiKeyManagerCore();
            services.AddYubiKeySecurityDomain();
            await using var provider = services.BuildServiceProvider();

            await state.WithSecurityDomainSessionFromDIAsync(
                resetBeforeUse,
                action,
                provider,
                configuration,
                scpKeyParams,
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///     Executes an action with a <see cref="SecurityDomainSession" /> created via a
        ///     custom <see cref="IServiceProvider" />.
        /// </summary>
        /// <remarks>
        ///     Use this overload when you need to test with additional services registered
        ///     or a custom DI configuration.
        /// </remarks>
        /// <param name="resetBeforeUse">
        ///     When <c>true</c>, resets the Security Domain before running the test action.
        /// </param>
        /// <param name="action">The async action to execute with the session.</param>
        /// <param name="serviceProvider">
        ///     The service provider containing the registered <see cref="SecurityDomainSessionFactory" />.
        /// </param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="scpKeyParams">Optional SCP key parameters for authentication.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task WithSecurityDomainSessionFromDIAsync(
            bool resetBeforeUse,
            Func<SecurityDomainSession, Task> action,
            IServiceProvider serviceProvider,
            ProtocolConfiguration? configuration = null,
            ScpKeyParameters? scpKeyParams = null,
            CancellationToken cancellationToken = default)
        {
            var factory = serviceProvider.GetRequiredService<SecurityDomainSessionFactory>();

            return state.WithConnectionAsync(async connection =>
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

                using var session = await factory(
                    sharedConnection,
                    configuration,
                    scpKeyParams,
                    cancellationToken).ConfigureAwait(false);

                await action(session).ConfigureAwait(false);
            }, cancellationToken);
        }
    }

    #endregion
}