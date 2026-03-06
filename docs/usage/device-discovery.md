# Device Discovery with YubiKeyManager

This guide covers how to discover and monitor YubiKey devices using the static `YubiKeyManager` API.

## Quick Start

```csharp
using Yubico.YubiKit.Core.YubiKey;

// Find all connected YubiKeys
var devices = await YubiKeyManager.FindAllAsync();
foreach (var device in devices)
{
    Console.WriteLine($"Found YubiKey: {device.SerialNumber}");
}
```

## API Overview

The `YubiKeyManager` class is a **static-only API** - no dependency injection or configuration is required.

### Discovery Methods

| Method | Description |
|--------|-------------|
| `FindAllAsync()` | Find all connected YubiKeys |
| `FindAllAsync(ConnectionType)` | Find YubiKeys by connection type (SmartCard, HID, or All) |

### Monitoring Methods

| Method | Description |
|--------|-------------|
| `StartMonitoring()` | Start monitoring with default 5-second interval |
| `StartMonitoring(TimeSpan)` | Start monitoring with custom interval |
| `StopMonitoring()` | Stop monitoring |
| `IsMonitoring` | Check if monitoring is active |
| `DeviceChanges` | Observable sequence of device events |

### Lifecycle Methods

| Method | Description |
|--------|-------------|
| `ShutdownAsync()` | Clean up all resources (async) |
| `Shutdown()` | Clean up all resources (sync) |

## Simple Discovery

```csharp
// Find all devices
var allDevices = await YubiKeyManager.FindAllAsync();

// Find only SmartCard-connected devices
var smartCardDevices = await YubiKeyManager.FindAllAsync(ConnectionType.SmartCard);

// Find only HID-connected devices (FIDO2, OTP)
var hidDevices = await YubiKeyManager.FindAllAsync(ConnectionType.Hid);
```

## Device Monitoring

For applications that need to react to device connections/disconnections:

```csharp
using System.Reactive.Linq;
using Yubico.YubiKit.Core.YubiKey;

// Subscribe to device events
using var subscription = YubiKeyManager.DeviceChanges.Subscribe(e =>
{
    switch (e.Action)
    {
        case DeviceAction.Arrived:
            Console.WriteLine($"Device connected: {e.Device.SerialNumber}");
            break;
        case DeviceAction.Removed:
            Console.WriteLine($"Device removed: {e.Device.SerialNumber}");
            break;
    }
});

// Start monitoring (events won't flow until this is called)
YubiKeyManager.StartMonitoring();

// ... application runs ...

// Clean up
await YubiKeyManager.ShutdownAsync();
```

### Custom Monitoring Interval

```csharp
// Monitor with 10-second interval
YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(10));
```

### UI Thread Marshaling

Events are raised on background threads. For UI applications, marshal to the UI thread:

```csharp
// WPF example
YubiKeyManager.DeviceChanges
    .ObserveOn(SynchronizationContext.Current!)
    .Subscribe(e =>
    {
        // Safe to update UI here
        DevicesList.Add(e.Device);
    });
```

## Error Handling

```csharp
try
{
    var devices = await YubiKeyManager.FindAllAsync(cancellationToken);
}
catch (OperationCanceledException)
{
    // Scan was cancelled
}
catch (PlatformInteropException ex)
{
    // Platform API error (e.g., SmartCard service not running)
    Console.WriteLine($"Platform error: {ex.Message}");
}
```

## Testing Pattern

In xUnit tests, clean up static state between tests:

```csharp
public class YubiKeyTests : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Reset static state for test isolation
        await YubiKeyManager.ShutdownAsync();
    }

    [Fact]
    public async Task CanDiscoverDevices()
    {
        var devices = await YubiKeyManager.FindAllAsync();
        Assert.NotNull(devices);
    }
}
```

## Race Conditions

Device discovery is inherently subject to race conditions. If a device connects or disconnects during a scan:

- The returned list may not include a device that just connected
- The returned list may include a device that just disconnected

For real-time accuracy, use `DeviceChanges` with `StartMonitoring()` to track changes as they occur.

## Migration from DI-based API

If you were using the previous `IYubiKeyManager` interface with dependency injection:

**Before (DI-based):**
```csharp
public class MyService(IYubiKeyManager manager)
{
    public async Task DoWork()
    {
        var devices = await manager.FindAllAsync();
    }
}

// In Program.cs
builder.Services.AddYubiKeyManagerCore();
```

**After (Static API):**
```csharp
public class MyService
{
    public async Task DoWork()
    {
        var devices = await YubiKeyManager.FindAllAsync();
    }
}

// No DI registration needed - just use the static API directly
```

Key changes:
- Remove `IYubiKeyManager` constructor parameter
- Remove `AddYubiKeyManagerCore()` service registration
- Call static `YubiKeyManager` methods directly
- Use `ShutdownAsync()` for cleanup instead of `IDisposable`

## Thread Safety

All `YubiKeyManager` methods are thread-safe:
- `FindAllAsync()` can be called from multiple threads concurrently
- `StartMonitoring()` and `StopMonitoring()` are idempotent
- `DeviceChanges` events may be delivered on any thread
