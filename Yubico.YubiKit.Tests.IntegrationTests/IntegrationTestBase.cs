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
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.IntegrationTests;

public abstract class IntegrationTestBase : IDisposable
{
    private bool _disposed;

    protected IntegrationTestBase()
    {
        var services = new ServiceCollection();
        services.AddYubiKeyManager(options =>
        {
            options.EnableAutoDiscovery = true;
            options.ScanInterval = TimeSpan.FromSeconds(1);
            options.EnabledTransports = Options.Transports.All;
        });

        ServiceProvider = services.BuildServiceProvider();
        ServiceLocator.SetLocatorProvider(ServiceProvider);

        Manager = ServiceProvider.GetRequiredService<IYubiKeyManager>();
        Repository = ServiceProvider.GetRequiredService<DeviceRepository>();
        MonitorService = ServiceProvider.GetRequiredService<MonitorService>();

        MonitorService.StartAsync(CancellationToken.None).Wait();
        Repository.StartAsync(CancellationToken.None).Wait();
    }

    protected ServiceProvider ServiceProvider { get; }
    protected IYubiKeyManager Manager { get; }
    private DeviceRepository Repository { get; }
    private MonitorService MonitorService { get; }

    #region IDisposable Members

    public void Dispose()
    {
        if (_disposed)
            return;

        MonitorService?.Dispose();
        Repository?.Dispose();
        ServiceProvider?.Dispose();
        GC.SuppressFinalize(this);

        _disposed = true;
    }

    #endregion
}