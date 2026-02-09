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
    
    // Event-driven listeners
    private DesktopSmartCardDeviceListener? _smartCardListener;
    private HidDeviceListener? _hidListener;
    private readonly SemaphoreSlim _eventSemaphore = new(0);

    /// <summary>
    /// Indicates whether the monitor service has been started.
    /// Used by DeviceRepositoryCached to validate DeviceChanges access.
    /// </summary>
    internal static bool IsStarted { get; private set; }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.EnableAutoDiscovery)
        {
            logger.LogInformation("YubiKey device auto-discovery is disabled. Device monitor will not start.");
            return Task.CompletedTask;
        }
        
        IsStarted = true;
        return base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("YubiKey device monitor stopping...");

        // Signal the event semaphore to wake up ExecuteAsync
        try { _eventSemaphore.Release(); } catch (SemaphoreFullException) { }

        // Complete the channel BEFORE stopping ExecuteAsync
        // This allows DeviceListenerService to exit its await foreach loop
        deviceChannel.Complete();

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        
        TeardownListeners();
        IsStarted = false;
        logger.LogInformation("YubiKey device monitor stopped");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("YubiKey device monitor started");
            
            SetupListeners();
            
            logger.LogInformation("Performing initial device scan...");
            await PerformDeviceScan(stoppingToken).ConfigureAwait(false);
            
            // Event-driven loop: wait for signals, coalesce, then scan
            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait for an event signal from a listener
                await _eventSemaphore.WaitAsync(stoppingToken).ConfigureAwait(false);
                
                // Coalescing delay: wait to allow multiple rapid events to accumulate
                await Task.Delay(_options.EventCoalescingDelay, stoppingToken).ConfigureAwait(false);
                
                // Drain any additional events that accumulated during the delay
                while (_eventSemaphore.CurrentCount > 0)
                {
                    await _eventSemaphore.WaitAsync(TimeSpan.Zero, stoppingToken).ConfigureAwait(false);
                }
                
                await PerformDeviceScan(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Device monitoring was cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Device monitoring failed");
        }
    }
    
    private void SetupListeners()
    {
        // SmartCard listener
        try
        {
            _smartCardListener = new DesktopSmartCardDeviceListener();
            _smartCardListener.Arrived += (_, _) => SignalEvent();
            _smartCardListener.Removed += (_, _) => SignalEvent();
            logger.LogDebug("SmartCard device listener started");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to start SmartCard device listener, falling back to polling");
            _smartCardListener = null;
        }
        
        // HID listener (platform-specific via factory)
        try
        {
            _hidListener = HidDeviceListener.Create();
            _hidListener.Arrived += (_, _) => SignalEvent();
            _hidListener.Removed += (_, _) => SignalEvent();
            logger.LogDebug("HID device listener started ({Platform})", _hidListener.GetType().Name);
        }
        catch (PlatformNotSupportedException)
        {
            logger.LogDebug("HID device listener not supported on this platform");
            _hidListener = null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to start HID device listener");
            _hidListener = null;
        }
    }
    
    private void TeardownListeners()
    {
        try
        {
            _smartCardListener?.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error disposing SmartCard listener");
        }
        finally
        {
            _smartCardListener = null;
        }
        
        try
        {
            _hidListener?.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error disposing HID listener");
        }
        finally
        {
            _hidListener = null;
        }
    }
    
    private void SignalEvent()
    {
        try
        {
            _eventSemaphore.Release();
        }
        catch (SemaphoreFullException)
        {
            // Semaphore is already at max; event is already pending
        }
        catch (ObjectDisposedException)
        {
            // Semaphore was disposed; service is shutting down
        }
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
        logger.LogDebug("PCSC scan completed, found {DeviceCount} devices", devices.Count);
        return yubiKeys;
    }

    private async Task<List<IYubiKey>> ScanHidDevices(CancellationToken cancellationToken)
    {
        var devices = await findHidService.FindAllAsync(cancellationToken).ConfigureAwait(false);
        var yubiKeys = devices.Select(yubiKeyFactory.Create).ToList();
        logger.LogDebug("HID scan completed, found {DeviceCount} devices", devices.Count);
        return yubiKeys;
    }

    public override void Dispose()
    {
        if (_disposed) return;

        TeardownListeners();
        _eventSemaphore.Dispose();
        base.Dispose();

        _disposed = true;
    }
}