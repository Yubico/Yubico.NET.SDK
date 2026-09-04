# Device Discovery with YubiKeyManager

This guide covers how to discover and monitor YubiKey devices using the static `YubiKeyManager` API.

## Quick Start

```csharp
using System;
using Yubico.YubiKit.Core.Devices;

// Find all connected YubiKeys
var devices = await YubiKeyManager.FindAllAsync();
foreach (var device in devices)
{
    Console.WriteLine($"Found YubiKey: {device.DeviceId} ({device.AvailableConnections})");
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
| `WatchAsync(CancellationToken)` | Async sequence of repository-diffed device events (`await foreach`) |

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

For applications that need to react to device connections and disconnections. The SDK has **no
reactive dependency** — both surfaces below use BCL types only.

### `await foreach` (recommended)

```csharp
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;

// Start monitoring (events won't flow until this is called)
YubiKeyManager.StartMonitoring();

await foreach (var e in YubiKeyManager.WatchAsync())
{
    var message = e.Action switch
    {
        DeviceAction.Added => "connected",
        DeviceAction.Removed => "removed",
        _ => "changed"
    };

    Console.WriteLine($"Device {message}: {e.Device.DeviceId} ({e.Device.AvailableConnections})");
}
```

Pass a cancellation token and cancel it to stop watching. Enumeration throws
`OperationCanceledException`, and the subscription is released automatically.

> **Start enumerating before the action you expect to trigger an event.** `WatchAsync` subscribes on
> the first iteration, not when it is called, so events raised in between are not observed.

#### Waiting for a specific device event

To wait for an insertion with a timeout, handle cancellation explicitly:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
YubiKeyManager.StartMonitoring();

try
{
    await foreach (var e in YubiKeyManager.WatchAsync(cts.Token))
    {
        if (e.Action == DeviceAction.Added)
        {
            Console.WriteLine($"Got it: {e.Device.DeviceId}");
            break;
        }
    }
}
catch (OperationCanceledException) when (cts.IsCancellationRequested)
{
    Console.WriteLine("Timed out waiting for a YubiKey.");
}
```

Each enumeration of `WatchAsync` gets its own independent buffer, so several watchers can run
concurrently without interfering. If a new event arrives while a consumer's 256-event buffer is
full, its stream ends with an `InvalidOperationException` rather than silently dropping events —
resynchronise with `FindAllAsync` and re-enumerate. Because `DeviceEvent` is a delta rather than a
snapshot, dropping one would permanently desynchronise your device list, so the SDK reports it instead.
Concurrent watchers are state-safe: cancelling or losing one watcher neither completes nor faults
another, and shutting the SDK down ends every active watcher normally rather than by throwing.

`WatchAsync` is the only device-change stream. Device events are never delivered inline on the
monitor's publishing thread, so a slow or abandoned watcher cannot delay device monitoring for
anyone else.

`StartMonitoring()` performs an initial repository rescan. Native SmartCard and HID listener notifications are
treated as rescan triggers only. Public device events are emitted from repository diffs after discovery,
so a raw HID listener notification is not itself authoritative evidence that a YubiKey physical device was added
or removed.

### Custom Monitoring Interval

```csharp
// Monitor with 10-second interval
YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(10));
```

### UI Thread Marshaling

Events are raised on background threads, so UI applications must marshal before touching controls.

With `await foreach`, capture the UI `SynchronizationContext` and post to it — no extra dependency:

```csharp
var ui = SynchronizationContext.Current!;

await foreach (var e in YubiKeyManager.WatchAsync(CancellationToken.None))
{
    ui.Post(_ => DevicesList.Add(e.Device), null);
}
```

Watchers are decoupled from the publisher by their own buffer, so a loop body that blocks delays
only that watcher — never device monitoring — until its 256-event buffer fills.

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

For real-time accuracy, use `WatchAsync` with `StartMonitoring()` to track changes as they occur.

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
- Listener notifications are serialized through a single-reader debounce queue before rescans
- `WatchAsync` events may be delivered on any thread
