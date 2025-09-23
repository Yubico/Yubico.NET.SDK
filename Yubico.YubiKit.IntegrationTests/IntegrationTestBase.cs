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
using Microsoft.Extensions.Logging;

namespace Yubico.YubiKit.IntegrationTests;

public abstract class IntegrationTestBase : IDisposable
{
    protected IntegrationTestBase()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
        YubiKeyManager = ServiceProvider.GetRequiredService<IYubiKeyManager>();
    }

    protected ServiceProvider ServiceProvider { get; }
    protected IYubiKeyManager YubiKeyManager { get; }

    #region IDisposable Members

    public void Dispose()
    {
        ServiceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion

    protected void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        services.AddYubiKeyManager(options =>
        {
            options.EnableAutoDiscovery = true;
            options.ScanInterval = TimeSpan.FromSeconds(1);
            options.EnabledTransports = Transports.All;
        });
    }
}