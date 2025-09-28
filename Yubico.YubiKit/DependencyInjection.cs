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
using Yubico.YubiKit.Core.Connections;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit;

public static class DependencyInjection
{
    #region Nested type: <extension>

    extension(IServiceCollection services)
    {
        public IServiceCollection AddYubiKeyManager()
        {
            return services.AddYubiKeyManager(configureOptions: null);
        }

        public IServiceCollection AddYubiKeyManager(Action<YubiKeyManagerOptions>? configureOptions)
        {
            if (configureOptions != null) services.Configure(configureOptions);

            // Core factory services
            services.AddSingleton<IYubiKeyFactory, YubiKeyFactory>();
            services.AddTransient<ISmartCardConnectionFactory, SmartCardConnectionFactory>();

            // Device communication channel
            services.AddSingleton<DeviceChannel>();
            services.AddSingleton<IDeviceChannel>(sp => sp.GetRequiredService<DeviceChannel>());

            // Device repository (state management + BackgroundService)
            services.AddSingleton<YubiKeyDeviceRepository>();
            services.AddSingleton<IYubiKeyDeviceRepository>(sp =>
                sp.GetRequiredService<YubiKeyDeviceRepository>());
            services.AddHostedService<YubiKeyDeviceRepository>(sp =>
                sp.GetRequiredService<YubiKeyDeviceRepository>());

            // Background monitoring service
            services.AddHostedService<YubiKeyDeviceMonitor>();

            // YubiKeyManager now uses repository
            services.AddSingleton<YubiKeyManager>();
            services.AddSingleton<IYubiKeyManager>(sp => sp.GetRequiredService<YubiKeyManager>());

            // Protocol and session factories
            services
                .AddTransient<IProtocolFactory<ISmartCardConnection>, SmartCardProtocolFactory<ISmartCardConnection>>();
            services
                .AddSingleton<IManagementSessionFactory<ISmartCardConnection>,
                    ManagementSessionFactory<ISmartCardConnection>>();

            return services;
        }
    }

    #endregion
}