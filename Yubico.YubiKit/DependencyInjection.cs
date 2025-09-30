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
using Microsoft.Extensions.Options;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Connections;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit;

public static class DependencyInjection
{
    #region Nested type: <extension>

    extension(IServiceCollection services)
    {
        public IServiceCollection AddYubiKeyManager(Action<YubiKeyManagerOptions>? configureOptions = null)
        {
            if (configureOptions != null) services.Configure(configureOptions);

            services.AddTransient<IYubiKeyManager, YubiKeyManager>()
                .AddTransient<IYubiKeyFactory, YubiKeyFactory>()
                .AddTransient<ISmartCardConnectionFactory, SmartCardConnectionFactory>()
                .AddTransient<IPcscDeviceService, PcscDeviceService>()
                .AddTransient<IProtocolFactory<ISmartCardConnection>, SmartCardProtocolFactory<ISmartCardConnection>>()
                .AddTransient<IManagementSessionFactory<ISmartCardConnection>,
                    ManagementSessionFactory<ISmartCardConnection>>()
                .AddSingleton<IDeviceChannel, DeviceChannel>()
                .AddBackgroundServices();

            return services;
        }

        private IServiceCollection AddBackgroundServices() =>
            services
                .AddSingleton<YubiKeyDeviceRepository>()
                .AddSingleton<IYubiKeyDeviceRepository>(sp => sp.GetRequiredService<YubiKeyDeviceRepository>())
                .AddHostedService<YubiKeyDeviceRepository>(sp => sp.GetRequiredService<YubiKeyDeviceRepository>())

                // TODO Make use of IOptions<YubiKeyManagerOptions> in the monitor
                .AddTransient<DeviceMonitorOptions>(sp =>
                    sp.GetRequiredService<IOptions<YubiKeyManagerOptions>>().Value.ToDeviceMonitorOptions())
                .AddSingleton<YubiKeyDeviceMonitor>()
                .AddHostedService<YubiKeyDeviceMonitor>(sp => sp.GetRequiredService<YubiKeyDeviceMonitor>());
    }

    #endregion
}