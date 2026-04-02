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
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Factory delegate for creating <see cref="YubiOtpSession"/> instances.
/// </summary>
/// <param name="connection">The connection to use (SmartCard or OTP HID).</param>
/// <param name="configuration">Optional protocol configuration.</param>
/// <param name="scpKeyParameters">Optional SCP key parameters for secure channel (SmartCard only).</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>A configured YubiOtpSession.</returns>
public delegate Task<YubiOtpSession> YubiOtpSessionFactory(
    IConnection connection,
    ProtocolConfiguration? configuration = null,
    ScpKeyParameters? scpKeyParameters = null,
    CancellationToken cancellationToken = default);

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers YubiOTP services including the <see cref="YubiOtpSessionFactory"/>.
        /// </summary>
        /// <remarks>
        /// <b>Prerequisite:</b> Call <c>AddYubiKeyManagerCore()</c> before this method
        /// to register core YubiKey services.
        /// </remarks>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddYubiOtp()
        {
            services.TryAddSingleton<YubiOtpSessionFactory>(
                YubiOtpSession.CreateAsync);

            return services;
        }
    }
}
