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
using Yubico.YubiKit.Core.Hid;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     Registers core YubiKey services for dependency injection.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This method is idempotent - calling it multiple times is safe.
        ///         Services are only registered if not already present.
        ///     </para>
        ///     <para>
        ///         Registers: <see cref="IYubiKeyManager" />, <see cref="IYubiKeyFactory" />,
        ///         <see cref="ISmartCardConnectionFactory" />, <see cref="IDeviceRepository" />,
        ///         and background device monitoring services.
        ///     </para>
        /// </remarks>
        /// <param name="configureOptions">Optional configuration for YubiKey manager options.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddYubiKeyManagerCore(Action<YubiKeyManagerOptions>? configureOptions = null)
        {
            if (configureOptions is not null)
            {
                services.Configure(configureOptions);
            }

            // Use TryAdd* for idempotency - safe to call multiple times
            services.TryAddSingleton<YubiKitLoggingInitializer>();
            services.TryAddTransient<IYubiKeyManager, YubiKeyManager>();
            services.TryAddTransient<IYubiKeyFactory, YubiKeyFactory>();
            services.TryAddTransient<IFindYubiKeys, FindYubiKeys>();
            services.TryAddTransient<ISmartCardConnectionFactory, SmartCardConnectionFactory>();
            services.TryAddTransient<IFindPcscDevices, FindPcscDevices>();
            services.TryAddTransient<IFindHidDevices, FindHidDevices>();
            services.TryAddTransient<IProtocolFactory<ISmartCardConnection>, PcscProtocolFactory<ISmartCardConnection>>();
            services.TryAddSingleton<IDeviceChannel, DeviceChannel>();
            services.TryAddSingleton<IDeviceRepository, DeviceRepositoryCached>();

            services.AddBackgroundServices();

            return services;
        }

        private IServiceCollection AddBackgroundServices()
        {
            // Background services use TryAdd pattern via marker check
            if (services.Any(s => s.ServiceType == typeof(DeviceListenerService)))
            {
                return services;
            }

            services.AddSingleton<DeviceListenerService>();
            services.AddHostedService<DeviceListenerService>(sp => sp.GetRequiredService<DeviceListenerService>());
            services.AddSingleton<DeviceMonitorService>();
            services.AddHostedService<DeviceMonitorService>(sp => sp.GetRequiredService<DeviceMonitorService>());

            return services;
        }
    }
}