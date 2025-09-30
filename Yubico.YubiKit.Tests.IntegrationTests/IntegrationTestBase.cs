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
using Yubico.YubiKit.Core;

namespace Yubico.YubiKit.IntegrationTests;

public abstract class IntegrationTestBase : IDisposable
{
    private bool _disposed;

    protected IntegrationTestBase()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddYubiKeyManager(options =>
        {
            options.EnableAutoDiscovery = true;
            options.ScanInterval = TimeSpan.FromSeconds(1);
            options.EnabledTransports = YubiKeyManagerOptions.Transports.All;
        });

        ServiceProvider = services.BuildServiceProvider();
        ServiceLocator.SetLocatorProvider(ServiceProvider);

        Manager = ServiceProvider.GetRequiredService<IYubiKeyManager>();
        DeviceRepository = ServiceProvider.GetRequiredService<IDeviceRepository>();
        DeviceMonitorService = ServiceProvider.GetRequiredService<DeviceMonitorService>();
        DeviceListenerService = ServiceProvider.GetRequiredService<DeviceListenerService>();

        DeviceMonitorService.StartAsync(CancellationToken.None).Wait();
        // DeviceRepository.StartAsync(CancellationToken.None).Wait();
        DeviceListenerService.StartAsync(CancellationToken.None).Wait();
    }

    private DeviceListenerService DeviceListenerService { get; }

    protected ServiceProvider ServiceProvider { get; }
    protected IYubiKeyManager Manager { get; }
    private IDeviceRepository DeviceRepository { get; }
    private DeviceMonitorService DeviceMonitorService { get; }

    #region IDisposable Members

    public void Dispose()
    {
        if (_disposed)
            return;

        DeviceMonitorService?.Dispose();
        DeviceRepository?.Dispose();
        ServiceProvider?.Dispose();
        GC.SuppressFinalize(this);

        _disposed = true;
    }

    #endregion
}