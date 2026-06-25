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
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;

namespace Yubico.YubiKit.Management;

/// <summary>
///     Factory delegate for creating <see cref="ManagementSession" /> instances.
/// </summary>
/// <param name="connection">The connection to use (SmartCard, FIDO HID, or OTP HID).</param>
/// <param name="configuration">Optional protocol configuration.</param>
/// <param name="scpKeyParameters">Optional SCP key parameters for secure channel (SmartCard only).</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>A configured ManagementSession.</returns>
public delegate Task<ManagementSession> ManagementSessionFactory(
    IConnection connection,
    ProtocolConfiguration? configuration,
    ScpKeyParameters? scpKeyParameters = null,
    CancellationToken cancellationToken = default);

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     Registers Management services including the <see cref="ManagementSessionFactory" />.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This method registers the <see cref="ManagementSessionFactory" /> delegate
        ///         for creating Management sessions via dependency injection.
        ///     </para>
        ///     <para>
        ///         <b>Prerequisite:</b> Call <c>AddYubiKeyManagerCore()</c> before this method
        ///         to register core YubiKey services.
        ///     </para>
        /// </remarks>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddYubiKeyManager()
        {
            services.TryAddSingleton<ManagementSessionFactory>(
                ManagementSession.CreateAsync);

            return services;
        }
    }
}