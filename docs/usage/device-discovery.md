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
| `FindAllAsync(ConnectionType)` | Filter full-device cached results by connection type |
| `ScanAsync(scope, cancellationToken)` | Independent fresh scan limited to the requested interfaces; no repository updates |
| `FindFirstAsync(predicate, cancellationToken)` | Find the first match, or throw `InvalidOperationException` |
| `FindFirstOrDefaultAsync(predicate, cancellationToken)` | Find the first match, or return `null` |

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

// Find one device. The predicate is optional.
var firstDevice = await YubiKeyManager.FindFirstAsync();
var firstSmartCardDevice = await YubiKeyManager.FindFirstOrDefaultAsync(
    device => device.SupportsConnection(ConnectionType.SmartCard));
```

The first-device methods are one-shot queries over the same cached discovery as `FindAllAsync`.
They do not wait for a YubiKey to be inserted, and discovery ordering is not stable. Use
`WatchAsync` when the application needs to wait for device changes. They inherit `FindAllAsync`
cancellation behavior: the token is passed to discovery, but a cached result may complete without
observing cancellation.

## Independent Scoped Scans

Use `ScanAsync` when you need a one-shot view without the manager's cache or monitoring lifecycle:

```csharp
// Enumerate and probe only SmartCard interfaces; never enumerate HID.
var smartCardViews = await YubiKeyManager.ScanAsync(ConnectionType.SmartCard, cancellationToken);

// Enumerate HID descriptors, but create slots and probe metadata only for FIDO interfaces.
var fidoViews = await YubiKeyManager.ScanAsync(ConnectionType.HidFido, cancellationToken);

// Fresh evidence across all interfaces, still without publishing into the manager repository.
var allViews = await YubiKeyManager.ScanAsync(cancellationToken: cancellationToken);
```

Each call creates a new finder with independent identity/metadata evidence. It does **not** initialize
the manager, return its cached objects, update its repository, emit `WatchAsync` events, or start
monitoring. A monitor already running is neither stopped nor restricted by the scope. `FindAllAsync`,
including `forceRescan: true`, retains its existing full-scan/result-filter behavior and repository updates.

`scope` supports concrete combinations of `SmartCard`, `HidFido`, and `HidOtp`. `Hid` expands to
`HidFido | HidOtp`; `All` includes all three; `Unknown` scans nothing. Undefined bits are ignored, as
with discovery result filtering; a value containing only undefined bits scans nothing.

- SmartCard-only never enumerates HID, and HID-only never enumerates PC/SC. Excluded backends are not
  constructed.
- OTP and FIDO share OS HID descriptor enumeration. The OS may therefore expose excluded descriptors,
  but they receive no connection slots, topology queries, or identity/metadata protocol probes.
- Returned `AvailableConnections` contains only observed in-scope interfaces. A scoped view does not gain
  sibling slots from an earlier scan or the repository. PID completeness still means the PID's **full**
  expected interface set, not the requested subset. Ambiguous partial observations can split one key
  into several results.
- Metadata is best-effort, and each returned object can retain historical `CachedDeviceInfo`. Separate
  scans need not produce equal `DeviceId` values or the same grouping. Use known serials for durable
  identity, not scan objects or their identifiers.

**Ownership bound:** repeat scans and views sharing any known member interface ID exclude overlapping
connections in-process. Disjoint scoped views, such as SmartCard-only and FIDO-only, do not share claims,
even when their serials match. Coordinate them explicitly; do not treat them as safe parallel access to
one key. A scan can also probe unclaimed sibling interfaces while a scoped connection is live. See
[connection ownership](../architecture/connection-ownership-and-contention.md#grouping-bound).

Cancellation is checked before constructing the finder. Read budgets and cancellation bound identity/metadata
waits, not already-dispatched native calls. Dispatched native enumeration is still awaited: the public scan
task can remain pending after cancellation until enumeration returns. There is no end-to-end scan timeout
guarantee. On scan exit, queued identity reads are retired so they cannot open later; already-open reads keep
their registry claim and worker admission until background teardown. Cancellation is not proof of native drain.

## Cached Device Metadata

`IYubiKey.CachedDeviceInfo` returns the last successfully observed Core `DeviceInfo` snapshot without
opening a connection or session, performing I/O, or checking whether the device is still present:

```csharp
var device = await YubiKeyManager.FindFirstAsync();
if (device.CachedDeviceInfo is { } info)
{
    Console.WriteLine($"Last observed firmware: {info.FirmwareVersion}");
    Console.WriteLine($"Last observed USB capabilities: {info.UsbEnabled}");
}
else
{
    Console.WriteLine("Device metadata is unavailable.");
}
```

No Management package is needed to read this property. `null` means metadata is unavailable, not that
the device lacks a capability or has disconnected. Custom `IYubiKey` implementations can omit this member;
the default implementation returns `null`.

This is **last-known data, not guaranteed-current state**. Existing discovery/repository updates can
replace the snapshot on a retained object without emitting a device event; a failed metadata read does
not erase the last successful value. A snapshot already returned to your code does not change. Metadata
can be stale after configuration changes, and a retained object can still expose historical data after
disconnection. Reading the property does not refresh it, and there is no freshness or presence guarantee.

When current configuration matters, use the existing Management live-read API,
`ManagementSession.GetDeviceInfoAsync` (or the `device.GetDeviceInfoAsync()` convenience extension from
`Yubico.YubiKit.Management`). A live read does **not** promise to update discovery metadata.

Keep using `device.SerialNumber` for durable physical identity. Its independent identity contract is
unchanged: once known, it remains known on that object even if the latest cached metadata omits the serial.
Do not substitute `device.CachedDeviceInfo?.SerialNumber` for it.

## Device Monitoring

For applications that need to react to device connections and disconnections. The SDK has **no
reactive dependency** — both surfaces below use BCL types only.

> **Owned-monitor migration:** the examples here still use the legacy static, change-only API.
> `YubiKeyMonitor` now provides initial `Added` replay plus ordered deltas, but its initialized factory
> remains internal until the public API/consumer migration. Each enumeration's first `MoveNextAsync`
> captures membership and registers for changes at one complete-publication boundary. Initial membership
> does not occupy its independent 256-event delta buffer; incoming changes during replay can overflow it.
> To recover, **clear the consumer's projection and re-enumerate `WatchAsync` on the same monitor**.
> Do not use a separate snapshot/discovery call followed by subscription as a race-free substitute.
> Cancellation/overflow are subscriber-local; disposal drains captured replay and published deltas, while
> new enumerations after disposal complete empty. The static examples below do not acquire replay semantics.

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

Each enumeration of the legacy static `WatchAsync` gets its own independent buffer, so several watchers can run
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

### Read-only scoped discovery hardware checks

From the repository root, explicitly select the read-only cases:

```bash
dotnet toolchain.cs -- test --integration --project Management --filter "FullyQualifiedName~ScopedDiscoveryReadOnlyTests"
```

These tests live in the Management integration project only because they compare discovery metadata with
independent live `ManagementSession.GetDeviceInfoAsync` reads. Core itself needs no Management dependency.
Configure the existing `YubiKeyTests:AllowedSerialNumbers` allow list before running them. They exercise
**every attached authorized serial-bearing USB key**, not just the first `[WithYubiKey]` match, and do not
hard-code a key count or serial in the source.

The seven cases cover SmartCard, FIDO, OTP, HID, SmartCard+OTP, explicit All, and the default scope. They check
in-scope connection sets, serial-based `SameDeviceAs` correlation across independent views, retained snapshots,
and cached/live serial, firmware, form factor, and USB/NFC capability agreement. Each exposed concrete
connection is opened, read, and asynchronously disposed before the next connection. The tests never reset,
write configuration or credentials, request touch, or require unplugging. Do **not** substitute a broad
Management integration run: other cases in that project change configuration or reset the key.

Run on an idle serial-bearing USB fixture, without another monitor/client probing the same keys. The tests
do not start monitoring or use its cache; a full reference scan precedes each scoped test. Consequently this
hardware run does not prove that excluded backends were never called: the deterministic `FindYubiKeysScopeTests`
pin those boundaries, including the distinction between shared HID descriptor enumeration and excluded
interface protocol probes.

Metadata readiness is retried up to three scans with 250 ms backoff; live connection acquisition retries only
`ConnectionInUseException`, up to three attempts. The existing finite worker-saturation scan retry also applies.
Each case requests cancellation after 60 seconds; this is neither a hard completion bound nor a native-drain
guarantee. Missing expected interfaces or metadata remain failures, with diagnostic output; no fixture or
no supported authorized interface can skip a case. Check executed/skipped totals and resolve external
contention before treating a failure as an SDK bug.
For detailed firmware/count/retry output, temporarily enable xUnit's `showLiveOutput` in that project's runner
configuration. Do not commit machine-specific serials or hardware logs.

Connections through disjoint scoped views are **never** opened simultaneously in hardware tests. Overlapping
and disjoint ownership bounds, cancellation, and queued-read retirement are tested with fakes in
`FindYubiKeysScopeTests`; successful read-only hardware checks do not imply universal physical-key locking.

### Static manager cleanup

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
