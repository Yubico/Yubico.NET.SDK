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
using Yubico.YubiKit.Core.Hid;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core;

public sealed class DeviceMonitorService(
    IYubiKeyFactory yubiKeyFactory,
    IFindPcscDevices findPcscService,
    IFindHidDevices findHidService,
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

            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
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

    private async Task PerformDeviceScan(CancellationToken cancellationToken)
    {
        try
        {
            var pcscScanTask = ScanPcscDevices(cancellationToken);
            var hidScanTask = ScanHidDevices(cancellationToken);
            await Task.WhenAll(pcscScanTask, hidScanTask).ConfigureAwait(false);

            var yubiKeys = new List<IYubiKey>();
            yubiKeys.AddRange(pcscScanTask.Result);
            yubiKeys.AddRange(hidScanTask.Result);

            await deviceChannel.PublishAsync(yubiKeys, cancellationToken).ConfigureAwait(false);
            logger.LogDebug("Device scan completed, found {TotalCount} total devices", yubiKeys.Count);
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Device scan was cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Device scanning failed");
            // Continue despite errors - don't crash the background service
        }
    }

    private async Task<List<IYubiKey>> ScanPcscDevices(CancellationToken cancellationToken)
    {
        var devices = await findPcscService.FindAllAsync(cancellationToken).ConfigureAwait(false);
        var yubiKeys = devices.Select(yubiKeyFactory.Create).ToList();
        logger.LogInformation("PCSC scan completed, found {DeviceCount} devices", devices.Count);
        return yubiKeys;
    }

    private async Task<List<IYubiKey>> ScanHidDevices(CancellationToken cancellationToken)
    {
        var devices = await findHidService.FindAllAsync(cancellationToken).ConfigureAwait(false);
        var yubiKeys = devices.Select(yubiKeyFactory.Create).ToList();
        logger.LogInformation("HID scan completed, found {DeviceCount} devices", devices.Count);
        return yubiKeys;
    }

    public override void Dispose()
    {
        if (_disposed) return;

        deviceChannel.Complete();
        base.Dispose();

        _disposed = true;
    }
}