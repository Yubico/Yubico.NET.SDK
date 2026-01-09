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
using Yubico.YubiKit.Core.Hid;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core;

public static class DependencyInjection
{
    #region Nested type: <extension>

    extension(IServiceCollection services)
    {
        public IServiceCollection AddYubiKeyManagerCore(Action<YubiKeyManagerOptions>? configureOptions = null)
        {
            if (configureOptions != null) services.Configure(configureOptions);

            services.AddTransient<IYubiKeyManager, YubiKeyManager>()
                .AddTransient<IYubiKeyFactory, YubiKeyFactory>()
                .AddTransient<IFindYubiKeys, FindYubiKeys>()
                .AddTransient<ISmartCardConnectionFactory, SmartCardConnectionFactory>()
                .AddTransient<IFindPcscDevices, FindPcscDevices>()
                .AddTransient<IFindHidDevices, FindHidDevices>()
                .AddTransient<IProtocolFactory<ISmartCardConnection>, PcscProtocolFactory<ISmartCardConnection>>()
                .AddSingleton<IDeviceChannel, DeviceChannel>()
                .AddSingleton<IDeviceRepository, DeviceRepositoryCached>()
                .AddBackgroundServices();

            return services;
        }

        private IServiceCollection AddBackgroundServices() =>
            services
                .AddSingleton<DeviceListenerService>()
                .AddHostedService<DeviceListenerService>(sp => sp.GetRequiredService<DeviceListenerService>())
                .AddSingleton<DeviceMonitorService>()
                .AddHostedService<DeviceMonitorService>(sp => sp.GetRequiredService<DeviceMonitorService>());
    }

    #endregion
}