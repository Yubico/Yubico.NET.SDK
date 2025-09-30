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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit;

public interface IDeviceListenerService
{
}

public class DeviceListenerService(
    ILogger<DeviceListenerService> logger,
    IDeviceChannel deviceChannel,
    IDeviceRepository deviceRepository,
    DeviceMonitorOptions options)
    : BackgroundService, IDeviceListenerService
{
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        if (options.EnableAutoDiscovery)
            return base.StartAsync(cancellationToken);

        logger.LogInformation("YubiKey device auto-discovery is disabled. Device listener will not start.");
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("YubiKey device listener started");
        try
        {
            await foreach (var devices in deviceChannel.ConsumeAsync(stoppingToken))
                deviceRepository.UpdateDeviceCache(devices);
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Device repository background service was cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error consuming device updates");
        }
    }
}