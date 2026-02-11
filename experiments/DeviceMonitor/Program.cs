// Device Monitor - Experiment to debug device discovery and filtering
// Run with: dotnet run --project experiments/DeviceMonitor
//
// This application monitors YubiKey device events from both:
// 1. Device listeners (HID and SmartCard)
// 2. Device repository cache updates
//
// Insert/remove your YubiKey to see events in real-time.

using System.Reactive.Linq;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;

Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║            YubiKey Device Monitor - Debug Tool               ║");
Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
Console.WriteLine("║  Insert/remove your YubiKey to see device events             ║");
Console.WriteLine("║  Press Ctrl+C to exit                                        ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Set up Ctrl+C handler early so we can use the token throughout
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\n[Shutdown] Ctrl+C received, shutting down...");
    cts.Cancel();
};

// Subscribe to device changes BEFORE starting monitoring
Console.WriteLine("[Setup] Subscribing to YubiKeyManager.DeviceChanges...");
using var subscription = YubiKeyManager.DeviceChanges.Subscribe(
    onNext: e =>
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var actionColor = e.Action == DeviceAction.Added ? ConsoleColor.Green : ConsoleColor.Red;
        var actionSymbol = e.Action == DeviceAction.Added ? "+" : "-";

        Console.ForegroundColor = actionColor;
        Console.WriteLine($"[{timestamp}] [{actionSymbol}] {e.Action}: {e.Device.DeviceId}");
        Console.ResetColor();

        // Only fetch device details on Added - device is gone on Removed
        if (e.Action == DeviceAction.Added)
        {
            // Fire-and-forget but log errors (avoid sync-over-async deadlocks)
            _ = PrintDeviceDetailsAsync(e.Device, "  ");
        }
        else
        {
            // For removals, just show the connection type we had cached
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"  ConnectionType: {e.Device.ConnectionType}");
            Console.ResetColor();
        }
    },
    onError: ex =>
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] DeviceChanges error: {ex.Message}");
        Console.ResetColor();
    },
    onCompleted: () =>
    {
        Console.WriteLine("[INFO] DeviceChanges stream completed");
    });

// Start monitoring BEFORE initial scan to avoid race condition
Console.WriteLine("[Setup] Starting device monitoring...");
YubiKeyManager.StartMonitoring();
Console.WriteLine($"[Setup] Monitoring started: {YubiKeyManager.IsMonitoring}");

// Perform initial scan
Console.WriteLine("[Setup] Performing initial device scan...");
var initialDevices = await YubiKeyManager.FindAllAsync(cts.Token);
Console.WriteLine($"[Setup] Found {initialDevices.Count} device(s) initially:");
foreach (var device in initialDevices)
{
    Console.WriteLine($"  - {device.DeviceId} ({device.ConnectionType})");
    await PrintDeviceDetailsAsync(device, "    ");
}
Console.WriteLine();

Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine("  Waiting for device events... (Insert/remove your YubiKey)");
Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine();

// Main loop - periodically show current state
try
{
    while (!cts.Token.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);

        // Periodic status update
        var devices = await YubiKeyManager.FindAllAsync(cts.Token);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [STATUS] {devices.Count} device(s) in repository:");
        foreach (var device in devices)
        {
            Console.WriteLine($"  - {device.DeviceId} ({device.ConnectionType})");
        }
        Console.ResetColor();
    }
}
catch (OperationCanceledException)
{
    // Expected on Ctrl+C
}

// Shutdown
Console.WriteLine("[Shutdown] Stopping monitoring...");
YubiKeyManager.StopMonitoring();
Console.WriteLine("[Shutdown] Shutting down YubiKeyManager...");
await YubiKeyManager.ShutdownAsync();
Console.WriteLine("[Shutdown] Done.");

// Async helper method to print device details without blocking
static async Task PrintDeviceDetailsAsync(IYubiKey device, string indent)
{
    try
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"{indent}ConnectionType: {device.ConnectionType}");

        // Use proper async with timeout via CancellationToken
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var info = await device.GetDeviceInfoAsync(timeoutCts.Token);
        
        Console.WriteLine($"{indent}SerialNumber: {info.SerialNumber}");
        Console.WriteLine($"{indent}FirmwareVersion: {info.FirmwareVersion}");
        Console.WriteLine($"{indent}FormFactor: {info.FormFactor}");
        Console.WriteLine($"{indent}UsbEnabled: {info.UsbEnabled}");
        Console.WriteLine($"{indent}NfcEnabled: {info.NfcEnabled}");
        Console.ResetColor();
    }
    catch (OperationCanceledException)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{indent}(DeviceInfo timeout - device may be busy)");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{indent}(Could not get DeviceInfo: {ex.Message})");
        Console.ResetColor();
    }
}

