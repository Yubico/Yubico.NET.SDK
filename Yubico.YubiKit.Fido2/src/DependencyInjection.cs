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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Fido2;

/// <summary>
/// Factory delegate for creating FIDO sessions from any connection type.
/// </summary>
/// <param name="connection">The connection to the YubiKey.</param>
/// <param name="configuration">Optional protocol configuration.</param>
/// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>A configured FidoSession instance.</returns>
public delegate Task<FidoSession> FidoSessionFactoryDelegate(
    IConnection connection,
    ProtocolConfiguration? configuration,
    ScpKeyParameters? scpKeyParams = null,
    CancellationToken cancellationToken = default);

/// <summary>
/// Factory delegate for creating FIDO sessions from SmartCard connections.
/// </summary>
/// <param name="connection">The SmartCard connection to the YubiKey.</param>
/// <param name="configuration">Optional protocol configuration.</param>
/// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>A configured FidoSession instance.</returns>
public delegate Task<FidoSession> SmartCardFidoSessionFactoryDelegate(
    ISmartCardConnection connection,
    ProtocolConfiguration? configuration,
    ScpKeyParameters? scpKeyParams = null,
    CancellationToken cancellationToken = default);

/// <summary>
/// Dependency injection extensions for FIDO2 session support.
/// </summary>
public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds FIDO2/CTAP2 session support to the service collection.
        /// </summary>
        /// <param name="configureOptions">Optional action to configure YubiKeyManager options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers:
        /// <list type="bullet">
        /// <item><description><see cref="FidoSessionFactoryDelegate"/> for creating sessions from any connection</description></item>
        /// <item><description><see cref="SmartCardFidoSessionFactoryDelegate"/> for creating sessions from SmartCard connections</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// It also calls the core <c>AddYubiKeyManagerCore</c> method to set up
        /// the YubiKey manager infrastructure.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var services = new ServiceCollection();
        /// services.AddYubiKeyFido2(options =>
        /// {
        ///     options.EnableAutoDiscovery = true;
        ///     options.ScanInterval = TimeSpan.FromMilliseconds(500);
        /// });
        /// 
        /// var provider = services.BuildServiceProvider();
        /// var factory = provider.GetRequiredService&lt;FidoSessionFactoryDelegate&gt;();
        /// </code>
        /// </example>
        public IServiceCollection AddYubiKeyFido2(Action<YubiKeyManagerOptions>? configureOptions = null)
        {
            services.TryAddSingleton<FidoSessionFactoryDelegate>(
                FidoSession.CreateAsync);

            services.TryAddSingleton<SmartCardFidoSessionFactoryDelegate>(
                FidoSession.CreateAsync);

            services.AddYubiKeyManagerCore(configureOptions);
            return services;
        }
    }
}
