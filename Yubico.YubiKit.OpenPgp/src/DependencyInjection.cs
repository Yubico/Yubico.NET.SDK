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
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Factory delegate for creating OpenPGP sessions.
/// </summary>
/// <param name="connection">An open SmartCard connection to a YubiKey.</param>
/// <param name="configuration">Optional protocol configuration overrides.</param>
/// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
/// <param name="cancellationToken">Token used to cancel the operation.</param>
/// <returns>An initialized <see cref="OpenPgpSession" />.</returns>
public delegate Task<OpenPgpSession> OpenPgpSessionFactory(
    ISmartCardConnection connection,
    ProtocolConfiguration? configuration,
    ScpKeyParameters? scpKeyParams,
    CancellationToken cancellationToken);

/// <summary>
///     Dependency injection extensions for the OpenPGP module.
/// </summary>
public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     Registers the <see cref="OpenPgpSessionFactory" /> delegate in the DI container.
        ///     Requires <c>AddYubiKeyManagerCore()</c> to have been called first.
        /// </summary>
        public IServiceCollection AddOpenPgp()
        {
            services.TryAddSingleton<OpenPgpSessionFactory>(
                (conn, cfg, scp, ct) => OpenPgpSession.CreateAsync(conn, cfg, scp, ct));

            return services;
        }
    }
}
