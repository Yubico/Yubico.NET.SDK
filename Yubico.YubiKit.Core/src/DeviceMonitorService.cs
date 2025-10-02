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
using Microsoft.Extensions.Options;
using Yubico.YubiKit.Core.Core.Devices.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core;

public sealed class DeviceMonitorService(
    IYubiKeyFactory yubiKeyFactory,
    IFindPcscDevices findPcscService,
    IDeviceChannel deviceChannel,
    ILogger<DeviceMonitorService> logger,
    IOptions<YubiKeyManagerOptions> options)
    : BackgroundService
{
    private readonly YubiKeyManagerOptions _options = options.Value;
    private bool _disposed;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        if (_options.EnableAutoDiscovery)
            return base.StartAsync(cancellationToken);

        logger.LogInformation("YubiKey device auto-discovery is disabled. Device monitor will not start.");
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("YubiKey device monitor started");
            logger.LogInformation("Performing initial device scan...");

            await PerformDeviceScan(stoppingToken).ConfigureAwait(false);
            using var timer = new PeriodicTimer(_options.ScanInterval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
                await PerformDeviceScan(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Device monitoring was cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Device monitoring failed");
        }

        logger.LogInformation("YubiKey device monitor stopped");
    }

    private Task PerformDeviceScan(CancellationToken cancellationToken)
    {
        try
        {
            return ScanPcscDevices(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PCSC device scanning failed");
            // Continue despite errors - don't crash the background service
            return Task.CompletedTask;
        }
    }

    private async Task ScanPcscDevices(CancellationToken cancellationToken)
    {
        var devices = await findPcscService.FindAllAsync(cancellationToken).ConfigureAwait(false);
        var yubiKeys = devices.Select(yubiKeyFactory.Create).ToList();

        await deviceChannel.PublishAsync(yubiKeys, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("PCSC scan completed, found {DeviceCount} devices", devices.Count);
    }

    public override void Dispose()
    {
        if (_disposed) return;

        deviceChannel.Complete();
        base.Dispose();

        _disposed = true;
    }
}