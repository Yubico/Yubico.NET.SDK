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
using Yubico.YubiKit.Core.Protocols;

namespace Yubico.YubiKit;

public static class DependencyInjection
{
    #region Nested type: <extension>

    extension(IServiceCollection services)
    {
        public IServiceCollection AddYubiKeyManager()
        {
            services.AddSingleton<IYubiKeyManager, YubiKeyManager>();
            return services;
        }

        public IServiceCollection AddYubiKeyManager(Action<YubiKeyManagerOptions>? configureOptions)
        {
            if (configureOptions != null) services.Configure(configureOptions);

            // Register Core services (internal to SDK)
            // services.AddSingleton<IDeviceEnumerationService, DeviceEnumerationService>();
            // services.AddSingleton<IPlatformDeviceProvider, PlatformDeviceProvider>();    
            // services.AddHostedService<DeviceMonitorBackgroundService>();                 

            services.AddSingleton<IYubiKeyManager, YubiKeyManager>();
            services.AddTransient<ISmartCardConnectionFactory, SmartCardConnectionFactory>();
            services.AddTransient<IYubiKeyFactory, YubiKeyFactory>();
            services.AddTransient<IProtocolFactory<ISmartCardConnection, IProtocol>,
                SmartCardProtocolFactory<ISmartCardConnection, IProtocol>>();
            services
                .AddSingleton<IManagementSessionFactory<ISmartCardConnection>,
                    ManagementSessionFactory<ISmartCardConnection>>();

            return services;
        }
    }

    #endregion
}