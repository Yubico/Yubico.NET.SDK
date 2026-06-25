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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;

namespace Yubico.YubiKit.YubiHsm;

/// <summary>
///     Factory delegate for creating <see cref="HsmAuthSession" /> instances.
/// </summary>
/// <param name="connection">The SmartCard connection to use.</param>
/// <param name="configuration">Optional protocol configuration.</param>
/// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>A configured HsmAuthSession.</returns>
public delegate Task<HsmAuthSession> HsmAuthSessionFactory(
    ISmartCardConnection connection,
    ProtocolConfiguration? configuration,
    ScpKeyParameters? scpKeyParams,
    CancellationToken cancellationToken);

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     Registers YubiHSM Auth services including the <see cref="HsmAuthSessionFactory" />.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This method registers the <see cref="HsmAuthSessionFactory" /> delegate
        ///         for creating YubiHSM Auth sessions via dependency injection.
        ///     </para>
        ///     <para>
        ///         <b>Prerequisite:</b> Call <c>AddYubiKeyManagerCore()</c> before this method
        ///         to register core YubiKey services.
        ///     </para>
        /// </remarks>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddHsmAuth()
        {
            services.TryAddSingleton<HsmAuthSessionFactory>(
                (conn, cfg, scp, ct) => HsmAuthSession.CreateAsync(conn, cfg, scp, cancellationToken: ct));

            return services;
        }
    }
}
