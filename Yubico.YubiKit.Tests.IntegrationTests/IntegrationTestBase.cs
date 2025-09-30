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
using System.Reflection;
using Yubico.YubiKit.Core;

namespace Yubico.YubiKit.IntegrationTests;

public abstract class IntegrationTestBase : IDisposable
{
    private bool _disposed;

    protected IntegrationTestBase(Action<YubiKeyManagerOptions>? overrideOptions = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddYubiKeyManager(overrideOptions ?? DefaultOptions);

        ServiceProvider = services.BuildServiceProvider();
        ServiceLocator.SetLocatorProvider(ServiceProvider);

        YubiKeyManager = ServiceProvider.GetRequiredService<IYubiKeyManager>();
        DeviceRepository = ServiceProvider.GetRequiredService<IDeviceRepository>();
        DeviceMonitorService = ServiceProvider.GetRequiredService<DeviceMonitorService>();
        DeviceListenerService = ServiceProvider.GetRequiredService<DeviceListenerService>();

        DeviceMonitorService.StartAsync(CancellationToken.None).Wait();
        DeviceListenerService.StartAsync(CancellationToken.None).Wait();
    }

    private static Action<YubiKeyManagerOptions> DefaultOptions =>
        options =>
        {
            options.EnableAutoDiscovery = true;
            options.ScanInterval = TimeSpan.FromMilliseconds(100);
            options.EnabledTransports = YubiKeyManagerOptions.Transports.All;
        };

    private DeviceListenerService DeviceListenerService { get; }

    protected ServiceProvider ServiceProvider { get; }
    protected IYubiKeyManager YubiKeyManager { get; }
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

    protected void SetSkipManualScan(bool value)
    {
        var type = typeof(DeviceRepository);
        var field = type.GetField("TEST_MONITORSERVICE_SKIP_MANUALSCAN",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(DeviceRepository, value);
    }
}