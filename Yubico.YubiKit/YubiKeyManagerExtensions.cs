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
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit;

public static class ServiceCollectionExtensions
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
            // Configure options
            if (configureOptions != null) services.Configure(configureOptions);

            // Register Core services (internal to SDK)
            // services.AddSingleton<IDeviceEnumerationService, DeviceEnumerationService>();
            // services.AddSingleton<IPlatformDeviceProvider, PlatformDeviceProvider>();    
            // services.AddHostedService<DeviceMonitorBackgroundService>();                 

            // Register user-facing services
            services.AddSingleton<IYubiKeyManager, YubiKeyManager>();

            // Registration
            services.AddTransient<ISmartCardConnectionFactory, SmartCardConnectionFactory>();
            services.AddTransient<IYubiKeyFactory, YubiKeyFactory>();

            return services;
        }
    }

    #endregion
}

public class YubiKeyManagerOptions
{
    public bool EnableAutoDiscovery { get; set; }
    public TimeSpan ScanInterval { get; set; }
    public Transports EnabledTransports { get; set; }
}

[Flags]
public enum Transports
{
    None = 0,
    Usb = 1,
    Nfc = 2,
    All = Usb | Nfc
}