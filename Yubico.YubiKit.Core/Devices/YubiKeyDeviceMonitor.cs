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

namespace Yubico.YubiKit.Core.Devices;

public class YubiKeyDeviceMonitor : BackgroundService
{
    private readonly IYubiKeyFactory yubiKeyFactory;
    private readonly IPcscService _pcscService;
    private readonly IDeviceChannel _deviceChannel;
    private readonly ILogger<YubiKeyDeviceMonitor> _logger;
    private readonly TimeSpan _scanInterval;

    public YubiKeyDeviceMonitor(
        IYubiKeyFactory yubiKeyFactory,
        IPcscService pcscService,
        IDeviceChannel deviceChannel,
        ILogger<YubiKeyDeviceMonitor> logger)
    {
        this.yubiKeyFactory = yubiKeyFactory;
        _pcscService = pcscService;
        _deviceChannel = deviceChannel;
        _logger = logger;
        _scanInterval = TimeSpan.FromSeconds(200);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("YubiKey device monitor started");

        try
        {
            // IMMEDIATE scan on startup (before any API calls)
            _logger.LogInformation("Performing initial device scan...");
            await PerformDeviceScan(stoppingToken);

            // Then start periodic scanning using PeriodicTimer
            using var timer = new PeriodicTimer(_scanInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await PerformDeviceScan(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Device monitoring was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Device monitoring failed");
        }

        _logger.LogInformation("YubiKey device monitor stopped");
    }

    private async Task PerformDeviceScan(CancellationToken cancellationToken)
    {
        try
        {
            var pcscDevices = await _pcscService.GetAllAsync();
            var yubiKeys = pcscDevices.Select(device => yubiKeyFactory.Create(device));
            
            await _deviceChannel.PublishAsync(yubiKeys, cancellationToken);
            _logger.LogDebug("Found {DeviceCount} PCSC devices", pcscDevices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PCSC device scanning failed");
            // Continue despite errors - don't crash the background service
        }
    }

    public override void Dispose()
    {
        _deviceChannel.Complete();
        base.Dispose();
    }
}