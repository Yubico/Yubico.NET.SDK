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

// Event counter for tracking
var eventCounter = 0;

// Subscribe to device changes BEFORE starting monitoring
Console.WriteLine("[Setup] Subscribing to YubiKeyManager.DeviceChanges...");
using var subscription = YubiKeyManager.DeviceChanges.Subscribe(
    onNext: e =>
    {
        // Increment counter here (before async call)
        var currentEventNum = ++eventCounter;
        // Handle event synchronously to ensure proper output ordering
        HandleDeviceEventAsync(e, currentEventNum).GetAwaiter().GetResult();
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

// Async handler for device events - ensures proper output ordering
static async Task HandleDeviceEventAsync(DeviceEvent e, int eventNum)
{
    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
    var isAdded = e.Action == DeviceAction.Added;
    var actionColor = isAdded ? ConsoleColor.Green : ConsoleColor.Red;
    var actionSymbol = isAdded ? "▲ ADDED" : "▼ REMOVED";
    var borderChar = isAdded ? '═' : '─';

    Console.WriteLine();
    Console.ForegroundColor = actionColor;
    Console.WriteLine($"╔{new string(borderChar, 62)}╗");
    Console.WriteLine($"║  [{timestamp}] EVENT #{eventNum}: {actionSymbol,-42} ║");
    Console.WriteLine($"╠{new string(borderChar, 62)}╣");
    Console.ResetColor();

    // Print device info
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"║  DeviceId:       {e.Device.DeviceId,-43} ║");
    Console.WriteLine($"║  ConnectionType: {e.Device.ConnectionType,-43} ║");
    Console.ResetColor();

    // Fetch device details for Added events (await to ensure proper ordering)
    if (isAdded)
    {
        await PrintDeviceDetailsAsync(e.Device, "║  ", actionColor);
    }

    Console.ForegroundColor = actionColor;
    Console.WriteLine($"╚{new string(borderChar, 62)}╝");
    Console.ResetColor();
}

// Start monitoring BEFORE initial scan to avoid race condition
Console.WriteLine("[Setup] Starting device monitoring...");
YubiKeyManager.StartMonitoring();
Console.WriteLine($"[Setup] Monitoring started: {YubiKeyManager.IsMonitoring}");

// Perform initial scan
Console.WriteLine();
Console.WriteLine("┌──────────────────────────────────────────────────────────────┐");
Console.WriteLine("│                    INITIAL DEVICE SCAN                       │");
Console.WriteLine("└──────────────────────────────────────────────────────────────┘");
var initialDevices = await YubiKeyManager.FindAllAsync(cts.Token);

// Sort by ConnectionType for easier reading
var sortedInitialDevices = initialDevices
    .OrderBy(d => d.ConnectionType)
    .ThenBy(d => d.DeviceId)
    .ToList();

Console.WriteLine($"  Found {sortedInitialDevices.Count} device(s):");
Console.WriteLine();

var deviceNum = 0;
foreach (var device in sortedInitialDevices)
{
    deviceNum++;
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"  ┌─── Device #{deviceNum} ───────────────────────────────────────────┐");
    Console.ResetColor();
    
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"  │  DeviceId:       {device.DeviceId}");
    Console.WriteLine($"  │  ConnectionType: {device.ConnectionType}");
    Console.ResetColor();
    
    await PrintDeviceDetailsAsync(device, "  │  ", ConsoleColor.Cyan);
    
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"  └────────────────────────────────────────────────────────────┘");
    Console.ResetColor();
    Console.WriteLine();
}

if (initialDevices.Count == 0)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  (No devices found - insert a YubiKey)");
    Console.ResetColor();
}

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine("  MONITORING ACTIVE - Insert/remove your YubiKey to see events");
Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.ResetColor();
Console.WriteLine();

// Main loop - periodically show current state
try
{
    while (!cts.Token.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), cts.Token);

        // Periodic status update
        var devices = await YubiKeyManager.FindAllAsync(cts.Token);
        
        // Sort by ConnectionType for easier reading
        var sortedDevices = devices
            .OrderBy(d => d.ConnectionType)
            .ThenBy(d => d.DeviceId)
            .ToList();
        
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine();
        Console.WriteLine($"┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄");
        Console.WriteLine($"  [{DateTime.Now:HH:mm:ss}] STATUS: {sortedDevices.Count} device(s) in cache");
        
        // Group by connection type for clearer display
        var grouped = sortedDevices.GroupBy(d => d.ConnectionType);
        foreach (var group in grouped)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"  [{group.Key}]");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            foreach (var device in group)
            {
                Console.WriteLine($"    • {device.DeviceId}");
            }
        }
        Console.WriteLine($"┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄");
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
static async Task PrintDeviceDetailsAsync(IYubiKey device, string indent, ConsoleColor borderColor)
{
    try
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"{indent}--- Querying DeviceInfo... ---");

        // Use proper async with timeout via CancellationToken
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var info = await device.GetDeviceInfoAsync(timeoutCts.Token);
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{indent}SerialNumber:    {info.SerialNumber}");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"{indent}FirmwareVersion: {info.FirmwareVersion}");
        Console.WriteLine($"{indent}FormFactor:      {info.FormFactor}");
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"{indent}UsbSupported:    {info.UsbSupported}");
        Console.WriteLine($"{indent}UsbEnabled:      {info.UsbEnabled}");
        Console.WriteLine($"{indent}NfcSupported:    {info.NfcSupported}");
        Console.WriteLine($"{indent}NfcEnabled:      {info.NfcEnabled}");
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
