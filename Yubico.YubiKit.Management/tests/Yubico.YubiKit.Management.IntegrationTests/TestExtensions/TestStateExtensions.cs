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

using Microsoft.Extensions.DependencyInjection;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Management.IntegrationTests.TestExtensions;

public static class TestStateExtensions
{
    /// <summary>
    ///     Executes an action with a <see cref="ManagementSession" /> created via
    ///     the DI-registered <see cref="ManagementSessionFactoryDelegate" />.
    /// </summary>
    /// <remarks>
    ///     This method builds a <see cref="ServiceProvider" /> internally with
    ///     <see cref="DependencyInjection.AddYubiKeyManager" /> registered,
    ///     then resolves and invokes the factory. Use this for integration tests
    ///     that verify the standard DI registration works end-to-end.
    /// </remarks>
    /// <param name="state">The YubiKey test state.</param>
    /// <param name="action">The async action to execute with the session.</param>
    /// <param name="configuration">Optional protocol configuration.</param>
    /// <param name="scpKeyParams">Optional SCP key parameters for authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task WithManagementSessionFromDIAsync(
        this YubiKeyTestState state,
        Func<ManagementSession, Task> action,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
    {
        var services = new ServiceCollection();
        services.AddYubiKeyManager();
        await using var provider = services.BuildServiceProvider();

        await state.WithManagementSessionFromDIAsync(
            action,
            provider,
            configuration,
            scpKeyParams,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Executes an action with a <see cref="ManagementSession" /> created via a
    ///     custom <see cref="IServiceProvider" />.
    /// </summary>
    /// <remarks>
    ///     Use this overload when you need to test with additional services registered
    ///     or a custom DI configuration.
    /// </remarks>
    /// <param name="state">The YubiKey test state.</param>
    /// <param name="action">The async action to execute with the session.</param>
    /// <param name="serviceProvider">
    ///     The service provider containing the registered <see cref="ManagementSessionFactoryDelegate" />.
    /// </param>
    /// <param name="configuration">Optional protocol configuration.</param>
    /// <param name="scpKeyParams">Optional SCP key parameters for authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static Task WithManagementSessionFromDIAsync(
        this YubiKeyTestState state,
        Func<ManagementSession, Task> action,
        IServiceProvider serviceProvider,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
    {
        var factory = serviceProvider.GetRequiredService<ManagementSessionFactoryDelegate>();

        return state.WithConnectionAsync(async connection =>
        {
            using var session = await factory(
                connection,
                configuration,
                scpKeyParams,
                cancellationToken).ConfigureAwait(false);

            await action(session).ConfigureAwait(false);
        }, cancellationToken);
    }
}
