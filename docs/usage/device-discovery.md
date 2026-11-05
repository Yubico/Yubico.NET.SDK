# Device Discovery with YubiKeyManager

Choose fresh one-shot discovery through `YubiKeyManager.Find…Async`, or explicitly own a ready
`YubiKeyMonitor` for cached membership and changes. No DI registration or global tracking state is needed.

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
| `FindAllAsync(interfaces: All, cancellationToken)` | Fresh independent discovery, limited to selected interfaces |
| `FindFirstAsync(predicate, interfaces: All, cancellationToken)` | Discover once and select the first match, or throw `InvalidOperationException` |
| `FindFirstOrDefaultAsync(predicate, interfaces: All, cancellationToken)` | Discover once and select the first match, or return `null` |

### Monitoring Methods

| Method | Description |
|--------|-------------|
| `CreateMonitorAsync(cancellationToken)` | Create an independent active monitor; await initial publication, including empty success |
| `monitor.CurrentDevices` | Read the latest completed, read-only membership snapshot without I/O |
| `monitor.WatchAsync(cancellationToken)` | Replay initial Added events, then stream ordered changes |
| `monitor.DisposeAsync()` | Stop and dispose this owned lifetime; normally use `await using` |

### Choose the lifetime for your application

| Application | Recommended path |
|---|---|
| Short CLI command or script | `FindFirstAsync` / `FindAllAsync`; no background cleanup |
| Optional hardware feature | `FindFirstOrDefaultAsync`; null means no matching key now |
| CCID-only utility | `Find…Async(interfaces: ConnectionType.SmartCard)` to avoid HID probing |
| Desktop device picker | Owned monitor + replaying `WatchAsync` to build the UI without a subscription gap |
| Long-running service | Share one host-owned monitor; filter `CurrentDevices` in memory |
| Embedded component | Receive an `IYubiKey` or host-owned monitor; do not take over process-wide lifetime |
| Independent diagnostic | One-shot Find with its own scope/evidence, even if a monitor exists |
| Configuration editor | Monitor for selection and presence; Management for live configuration |

## Simple Discovery

```csharp
// Find all devices
var allDevices = await YubiKeyManager.FindAllAsync();

// Enumerate/probe only SmartCard interfaces (no HID slots are returned).
var smartCardDevices = await YubiKeyManager.FindAllAsync(ConnectionType.SmartCard);

// Find only HID-connected devices (FIDO2, OTP)
var hidDevices = await YubiKeyManager.FindAllAsync(ConnectionType.Hid);

// Find one device. The predicate is optional.
var firstDevice = await YubiKeyManager.FindFirstAsync();
var firstSmartCardDevice = await YubiKeyManager.FindFirstOrDefaultAsync(
    device => device.SupportsConnection(ConnectionType.SmartCard));
```

The first-device methods perform one fresh discovery operation, like `FindAllAsync`.
They do not wait for a YubiKey to be inserted, and discovery ordering is not stable. Use
an owned monitor's `WatchAsync` when the application needs to wait for device changes.

## Independent Scoped Scans

All Find methods use independent evidence, without a shared cache or monitoring lifecycle:

```csharp
// Enumerate and probe only SmartCard interfaces; never enumerate HID.
var smartCardViews = await YubiKeyManager.FindAllAsync(interfaces: ConnectionType.SmartCard, cancellationToken: cancellationToken);

// Enumerate HID descriptors, but create slots and probe metadata only for FIDO interfaces.
var fidoViews = await YubiKeyManager.FindAllAsync(interfaces: ConnectionType.HidFido, cancellationToken: cancellationToken);

// Fresh evidence across all interfaces, still without publishing into a monitor repository.
var allViews = await YubiKeyManager.FindAllAsync(cancellationToken: cancellationToken);

// Either family can discover the selected physical key. No OTP slots/protocol probes are admitted.
var selected = await YubiKeyManager.FindFirstAsync(
    predicate: key => key.SerialNumber == serial,
    interfaces: ConnectionType.SmartCard | ConnectionType.HidFido,
    cancellationToken: cancellationToken);
```

Each call creates a new finder with independent identity/metadata evidence. It does **not** initialize
tracking, return cached objects, update a repository, emit events, or start monitoring.
A monitor already running is neither stopped nor restricted by the interfaces scope.

`interfaces` supports concrete combinations of `SmartCard`, `HidFido`, and `HidOtp`. `Hid` expands to
`HidFido | HidOtp`; `All` includes all three; `Unknown` scans nothing. Undefined bits are ignored, as
with discovery result filtering; a value containing only undefined bits scans nothing.

Flags select places to look, not a requirement that every key expose all selected interfaces. When later
operations need a full composite key, discover All and filter locally using `SupportsConnection`, rather
than narrowing discovery and accidentally discarding sibling connection slots.

- SmartCard-only never enumerates HID, and HID-only never enumerates PC/SC. Excluded backends are not
  constructed.
- OTP and FIDO share OS HID descriptor enumeration. The OS may therefore expose excluded descriptors,
  but they receive no connection slots, topology queries, or identity/metadata protocol probes.
- Returned `AvailableConnections` contains only observed in-scope interfaces. A scoped view does not gain
  sibling slots from an earlier scan or a monitor. PID completeness still means the PID's **full**
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

For continuous tracking, explicitly own and share a monitor. The factory registers listeners before the
loop's single initial scan and returns only after successful publication. An empty snapshot is ready;
initial failure/cancellation cleans up partial ownership. Creation cancellation applies only until return.
The SDK has **no reactive dependency** — the stream uses BCL types only.

Each monitor has one initialize-run-dispose lifetime. A single internal worker awaits every initial,
listener-triggered, and fallback scan and its publication before starting another. There is no manual
rescan or stop/restart mode. After terminal failure, dispose it and create a new monitor; a new watcher
on the failed monitor does not restart discovery.

`CurrentDevices` is a coherent, read-only membership snapshot without hardware I/O. Older lists keep their
membership; contained devices can receive last-known metadata updates. A later failed scan retains the
previous snapshot and retries; unexpected terminal failure faults watchers and rejects cached access.

### `await foreach` (recommended)

```csharp
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;

await using var monitor = await YubiKeyManager.CreateMonitorAsync();

await foreach (var e in monitor.WatchAsync())
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

On the first `MoveNextAsync`, `WatchAsync` captures membership and registers for subsequent deltas at one
publication boundary. It yields initial Added events outside the boundary, followed by ordered changes.
Do not prepend a separate snapshot or one-shot Find call: that would reintroduce a subscription gap.

#### Waiting for a specific device event

To accept an already-attached key or wait for an insertion with a timeout, handle cancellation explicitly:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
try
{
    await using var monitor = await YubiKeyManager.CreateMonitorAsync(cts.Token);
    await foreach (var e in monitor.WatchAsync(cts.Token))
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

Each enumeration of `monitor.WatchAsync` gets its own independent buffer, so several watchers can run
concurrently without interfering. If a new event arrives while a consumer's 256-event buffer is
full, its stream ends with an `InvalidOperationException` rather than silently dropping events —
**clear the consumer's projection and re-enumerate the same monitor**. Initial replay does not occupy the
delta buffer, but changes during replay can overflow it. Because `DeviceEvent` is a delta rather than a
snapshot, dropping one would permanently desynchronise your device list, so the SDK reports it instead.
Concurrent watchers are state-safe: cancelling or losing one watcher neither completes nor faults
another. Disposal ends healthy watchers normally; buffered history can drain, and new enumerations after
disposal end empty. `CurrentDevices` after disposal throws. Disposing one monitor does not affect another
or dispose application-owned sessions/connections. Concurrent disposal callers share completion through
repository teardown. Disposal closes public access, retires discovery ingress, and waits once for the
sole worker loop with a **ten-second managed-drain budget**, then disposes the repository even if the loop
was abandoned. This budget does **not** bound native listener cleanup or total wall-clock disposal.
Already-open native discovery reads keep their admission/lease until actual teardown. Late uncommitted
results cannot repopulate disposed membership or emit new deltas; already-committed history may still drain.

`WatchAsync` is the only device-change stream. Device events are never delivered inline on the
monitor's publishing thread, so a slow or abandoned watcher cannot delay device monitoring for
anyone else.

Native SmartCard and HID listener notifications are
treated as rescan triggers only. Public device events are emitted from repository diffs after discovery,
so a raw HID listener notification is not itself authoritative evidence that a YubiKey physical device was added
or removed.

The monitor scans all interfaces, uses listeners as latency hints, and falls back to a five-second interval.
There is no public interval or force-refresh option in this version. Filter `CurrentDevices` locally.

### Technical troubleshooting

- Listeners are optional latency accelerators, not device truth. If one fails to start, its cleanup is
  attempted and the other listener/five-second fallback still runs. Ordinary later scan failures retain
  the last committed snapshot and retry on subsequent hints/fallback; initial failure instead fails creation.
- When diagnosing listener/coalescing/scan/drain messages, the internal logger category is now
  `Yubico.YubiKit.Core.Devices.YubiKeyMonitorWorker`, renamed from
  `Yubico.YubiKit.Core.Devices.YubiKeyDeviceMonitorService`. Update category-specific filters; the public
  `YubiKeyManager` / `YubiKeyMonitor` API names are unchanged. See [logging setup](../LOGGING.md).
- Removed monitor generations do not mean discovery evidence is timeless. Listener activity globally
  invalidates finder identity/metadata evidence and retires its read scope before signaling a scan.
  Without hints, same-interface swaps may stay stale until a scan observes absence. Finder evidence epochs,
  queued-read retirement, and actual native ownership remain; cancellation or a drain timeout does not
  prove native release. See [monitor concurrency and shutdown](../architecture/event-driven-device-discovery.md#single-lifetime-worker-and-shutdown).

### UI Thread Marshaling

Events are raised on background threads, so UI applications must marshal before touching controls.

With `await foreach`, capture the UI `SynchronizationContext` and post to it — no extra dependency:

```csharp
var ui = SynchronizationContext.Current
    ?? throw new InvalidOperationException("Start watching from the UI thread.");

await using var monitor = await YubiKeyManager.CreateMonitorAsync();
await foreach (var e in monitor.WatchAsync(CancellationToken.None))
{
    ui.Post(_ =>
    {
        if (e.Action == DeviceAction.Added)
            DevicesList.Add(e.Device);
        else if (e.Action == DeviceAction.Removed)
            DevicesList.Remove(e.Device);
    }, null);
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
dotnet toolchain.cs -- test --integration --project Management.IntegrationTests --filter "(FullyQualifiedName~OwnedMonitorReadOnlyTests|FullyQualifiedName~ScopedDiscoveryReadOnlyTests)&Category!=RequiresUserPresence"
```

These tests live in the Management integration project only because they compare discovery metadata with
independent live `ManagementSession.GetDeviceInfoAsync` reads. Core itself needs no Management dependency.
Configure the existing `YubiKeyTests:AllowedSerialNumbers` allow list before running them. They exercise
**every attached authorized serial-bearing USB key**, not just the first `[WithYubiKey]` match, and do not
hard-code a key count or serial in the source.

The eight scoped cases cover SmartCard, FIDO, OTP, HID, SmartCard+OTP, SmartCard+FIDO, explicit All, and the default scope. They check
in-scope connection sets, serial-based `SameDeviceAs` correlation across independent views, retained snapshots,
and cached/live serial, firmware, form factor, and USB/NFC capability agreement. Each exposed concrete
connection is opened, read, and asynchronously disposed before the next connection. The tests never reset,
write configuration or credentials, request touch, or require unplugging. Do **not** substitute a broad
Management integration run: other cases in that project change configuration or reset the key.

Run on idle serial-bearing USB fixtures, without another monitor/client probing the same keys. The scoped
tests do not start monitoring or use its cache; a full reference scan precedes each scoped test. Consequently this
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

`OwnedMonitorReadOnlyTests` adds four public-API cases requiring **at least two attached authorized keys**;
fewer usable fixtures fails that hardware prerequisite rather than counting a one-key run as two-key coverage.
They verify both fixtures in `CurrentDevices` immediately after awaited creation (no polling into readiness),
initial Added replay, retained membership/object identity, subscriber-local cancellation, factory-token
independence, disposal/stream completion, recreate, independent convenience selection, and predicate-based
null/throw no-match behavior without unplugging anything. They log fixture ordinals/interface support,
not machine serials. Snapshot metadata is compared with sequential live reads only **after disposing the
monitor**. Incomplete initial metadata/interfaces remain failures, not successful two-key observations.

Repeated hardware cache reads do not prove absence of I/O by speed alone:
`YubiKeyMonitorTests.CurrentDevices_PopulatedSnapshot_PerformsNoBackendEnumerationProbesOrOpens` uses
instrumented PC/SC and HID backends, slot/topology/open counters, and real finder-produced descriptors.
`ScopedDiscoveryReadinessTests` also pins that a complete serial-less bystander does not prevent readiness,
whereas any view lacking metadata exhausts the bounded retries. Run that hardware-free regression explicitly
with `--integration --project Management.IntegrationTests --filter "FullyQualifiedName~ScopedDiscoveryReadinessTests"`.

**Attended acceptance remains a separate required pass gate for this lifecycle migration.** With a human
present, run the narrowly selected `DeviceEventHotPlugTests` and `HotPlugIdentityContractTests` protocols
for real removal/reinsertion and event/identity lifetime validation. Do not execute those cases unattended
or count unexecuted/skipped cases as acceptance. No UV/UP, touch, PIN/biometric prompt, reset, write, or manual
insertion/removal is part of the read-only filter above.

### Owned lifetime cleanup

One-shot tests need no manager initialization or shutdown. Tests that actually track changes own a monitor:

Keep the monitor in an `await using` scope and dispose each watcher/connection/session that the test
creates. The two-fixture suite above asserts actual initial membership and replay rather than merely
checking a non-null list. Use deterministic fixtures for valid empty initialization and blocked-native
teardown; do not manufacture absence through unattended physical removal.

## Race Conditions

Device discovery is inherently subject to race conditions. If a device connects or disconnects during a scan:

- The returned list may not include a device that just connected
- The returned list may include a device that just disconnected

For ongoing tracking, own a monitor and consume its `WatchAsync`. Neither discovery nor tracking promises
an instantaneous global hardware snapshot.

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
- One-shot discovery needs no cleanup; tracking components receive a host-owned monitor or own one with `await using`

### Earlier v2 prerelease migration

- `ScanAsync(scope, ct)` becomes `FindAllAsync(interfaces: scope, cancellationToken: ct)`.
- Remove `forceRescan`; every Find uses fresh per-call evidence, including after an earlier empty result.
- Replace static Start/Stop/Watch/Shutdown/IsMonitoring with `CreateMonitorAsync`, owned `WatchAsync`,
  `CurrentDevices`, and disposal. There are no aliases preserving the old global cache semantics.
- Old `type` filtered a full scan; new `interfaces` narrows discovery. Preserve full connection slots with
  All plus local filtering when that is the caller's intent.

## Thread Safety

All `YubiKeyManager` methods are thread-safe:
- `FindAllAsync()` can be called from multiple threads concurrently
- Monitors are independent owners; repeated disposal shares completion
- Listener ingress orders evidence invalidation against retirement; a private capacity-one occurrence
  signal coalesces hints for one sequential discovery/publication loop, not a notification payload queue
- `WatchAsync` events may be delivered on any thread
