# Device discovery with YubiKeyManager

Choose fresh one-shot discovery through `YubiKeyManager.Find…Async`, or explicitly own a ready
`YubiKeyMonitor` for a shared snapshot-and-change stream. No registration or global tracking state is needed.

## Quick start

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

## API overview

The `YubiKeyManager` class is a **static-only API** - no dependency injection or configuration is required.

### Manager methods

| Method | Description |
|--------|-------------|
| `FindAllAsync(interfaces: All, cancellationToken)` | Fresh independent discovery, limited to selected interfaces |
| `FindFirstAsync(predicate, interfaces: All, cancellationToken)` | Discover once and select the first match, or throw `InvalidOperationException` |
| `FindFirstOrDefaultAsync(predicate, interfaces: All, cancellationToken)` | Discover once and select the first match, or return `null` |
| `WatchAsync(cancellationToken)` | Create a private monitor per enumeration; replay, completion marker, then live changes |
| `CreateMonitorAsync(cancellationToken)` | Create a shared monitor; await initial publication, including empty success |

### Monitor methods

| Method | Description |
|--------|-------------|
| `monitor.WatchAsync(cancellationToken)` | Subscribe to the existing producer: `DeviceAdded` replay, `SnapshotComplete`, then live changes |
| `monitor.DisposeAsync()` | Stop and dispose this owned lifetime; normally use `await using` |

### Choose the lifetime for your application

| Application | Recommended path |
|---|---|
| Short CLI command or script | `FindFirstAsync` / `FindAllAsync`; no background cleanup |
| Optional hardware feature | `FindFirstOrDefaultAsync`; null means no matching key now |
| CCID-only utility | `Find…Async(interfaces: ConnectionType.SmartCard)` to avoid HID probing |
| One device-change consumer | `YubiKeyManager.WatchAsync(ct)`; enumeration owns the monitor |
| Desktop device picker | Build a projection from replay; use `SnapshotComplete` to distinguish loading from empty |
| Multi-view application or service | Share one host-owned monitor; each consumer calls `monitor.WatchAsync(ct)` |
| Embedded component | Receive an `IYubiKey` or host-owned monitor; do not take over process-wide lifetime |
| Independent diagnostic | One-shot Find with its own scope/evidence, even if a monitor exists |
| Configuration editor | Monitor for selection and presence; Management for live configuration |

## Simple discovery

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

## Independent scoped scans

All Find methods use independent evidence, without a shared cache or monitoring lifecycle:

```csharp
// Every YubiKey reachable as a smart card (CCID); HID is never enumerated.
var smartCardYubiKeys = await YubiKeyManager.FindAllAsync(interfaces: ConnectionType.SmartCard, cancellationToken: cancellationToken);

// Every YubiKey reachable over FIDO HID. HID descriptors are enumerated, but only FIDO interfaces are probed.
var fidoYubiKeys = await YubiKeyManager.FindAllAsync(interfaces: ConnectionType.HidFido, cancellationToken: cancellationToken);

// Every YubiKey on every interface, still without publishing into a monitor feed.
var allYubiKeys = await YubiKeyManager.FindAllAsync(cancellationToken: cancellationToken);

// Either family can discover the selected physical key. No OTP slots/protocol probes are admitted.
var selected = await YubiKeyManager.FindFirstAsync(
    predicate: key => key.SerialNumber == serial,
    interfaces: ConnectionType.SmartCard | ConnectionType.HidFido,
    cancellationToken: cancellationToken);
```

Each returned `IYubiKey` is a device view: one physical YubiKey as seen through the requested interfaces.
A SmartCard-only call returns YubiKeys that expose only their smart card connection, even when the same
keys also have FIDO or OTP interfaces.

Each call discovers from scratch: identity and metadata are read again rather than reused from an earlier
call. It does **not** initialize tracking, return cached objects, update a feed, emit changes, or start
monitoring.
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
- Returned `AvailableConnections` contains only observed in-scope interfaces. A YubiKey from a scoped call
  does not gain sibling slots from an earlier scan or a monitor. PID completeness still means the PID's **full**
  expected interface set, not the requested subset. Ambiguous partial observations can split one key
  into several results.
- Metadata is best-effort, and each returned object can retain historical `CachedDeviceInfo`. Separate
  scans need not produce equal `DeviceId` values or the same grouping. Use known serials for durable
  identity, not scan objects or their identifiers.

**Ownership bound:** YubiKeys from repeat scans that share any known member interface ID exclude
overlapping connections in-process. YubiKeys from disjoint scopes, such as a SmartCard-only and a
FIDO-only call, do not share claims, even when their serials match. Coordinate them explicitly; do not treat them as safe parallel access to
one key. A scan can also probe unclaimed sibling interfaces while a scoped connection is live. See
[connection ownership](../architecture/connection-ownership-and-contention.md#grouping-bound).

Cancellation is checked before discovery starts. Enumeration waits run concurrently with a provisional
five-second budget per transport; for one-shot `Find…Async` a timeout logs a warning and omits that transport
for this call, using the other transport's results. Cancellation abandons the enumeration wait, not already-dispatched native work.
Identity reads have a two-second budget per attempt (up to three attempts); metadata reads have a three-second
budget. These are discovery budgets, not a hard end-to-end wall-clock bound. On scan exit, queued identity reads are retired so they cannot open later; already-open reads keep
their registry claim and worker admission until background teardown. Cancellation is not proof of native drain.

## Cached device metadata

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

This is **last-known data, not guaranteed-current state**. Monitoring can
replace the snapshot on a retained object and emit `DeviceUpdated`; a failed metadata read does
not erase the last successful value. A snapshot already returned to your code does not change. Metadata
can be stale after configuration changes, and a retained object can still expose historical data after
disconnection. Reading the property does not refresh it, and there is no freshness or presence guarantee.

When current configuration matters, use the existing Management live-read API,
`ManagementSession.GetDeviceInfoAsync` (or the `device.GetDeviceInfoAsync()` convenience extension from
`Yubico.YubiKit.Management`). A live read does **not** promise to update discovery metadata.

Keep using `device.SerialNumber` for durable physical identity. Its independent identity contract is
unchanged: once known, it remains known on that object even if the latest cached metadata omits the serial.
Do not substitute `device.CachedDeviceInfo?.SerialNumber` for it.

## Selection in the CLI examples

The seven applet tools and unified `yk` commands use one
[shared selector](../../src/Cli.Shared/src/Device/DeviceSelector.cs). Each endpoint supplies ordered
concrete transports and an explicit policy for multiple devices without an interactive terminal:

| Endpoint | Eligible transports, in preference order | Multiple devices, noninteractive |
|---|---|---|
| OathTool, OpenPgpTool, HsmAuthTool | SmartCard | First eligible device |
| FidoTool | HID FIDO, SmartCard | Prefer a device exposing HID FIDO, otherwise first eligible |
| OtpTool | SmartCard, HID OTP | Prefer a device exposing SmartCard, otherwise first eligible |
| PivTool menus | SmartCard | No selection; an explicit choice is required |
| ManagementTool | SmartCard | No selection; an explicit choice is required |
| Unified `yk` | Command's `AppletTransports`, restricted by `--transport` when supplied | Prefer a device exposing the first effective transport, otherwise first eligible |

The standalone ManagementTool deliberately retains SmartCard-only eligibility, including its reset menu.
That is a tool policy, not an SDK limitation: unified `yk management` supports SmartCard, HID FIDO, and
HID OTP. FIDO transport presence alone does not establish applet/firmware support; session creation enforces it.

Selection always discovers **All** interfaces once per attempt, then applies one transport eligibility
filter without reconstructing or trimming devices. Unified `yk --transport` also reaches command session
options; it does not narrow backend discovery. `--serial` uses the independently latched `device.SerialNumber`
first, rejecting a known nonmatch without a live read. If identity is unknown, selection can use available
metadata; a successful serial-less snapshot is not treated as a promise that another read will reveal a serial.

A single eligible device is selected automatically. An interactive multi-device picker sorts labels and maps
choices by index, so identical labels remain separate choices; Cancel returns no selection. When no eligible
device is found, only an interactive caller is offered a retry. Discovery order is not stable, so scripts
requiring a particular physical device should use `--serial` where supported. PivTool's argument-driven CLI
always disables prompts, even on a terminal, and requires `--serial` when more than one device remains eligible.

Labels and returned `DeviceSelection.Info` reuse `CachedDeviceInfo`. Missing metadata gets at most one
best-effort live fallback per device reference per selection invocation, including retries. Reads are sequential
because two listed YubiKeys may be the same physical key seen through different interfaces. Neither successful nor failed fallback results
are retained by selector singletons across invocations, and cancellation propagates rather than becoming a
missing device. These bounds describe selection enrichment, not discovery's own metadata probes.

Unified `YkCommandBase` carries this metadata into the context and banner without opening Management again.
PIV info uses it only for firmware/identity display. Management info and config explicitly request live
information; Management reset also requires a successful live prerequisite read before its existing firmware
and confirmation checks. A failed live read never falls back to the selection snapshot. These live reads and
subsequent command sessions honor the requested transport.

## Device monitoring

manager watch creates a producer; monitor watch subscribes to an existing one.

Both streams replay `DeviceAdded` records, emit one `SnapshotComplete` even for an empty replay, then
deliver live `DeviceAdded`, `DeviceRemoved`, and `DeviceUpdated` records. Each carries `long Revision`:
the revision increments per applied scan, including scans with no changes. Changes sharing a revision
come from the same scan; a revision is not a frozen metadata snapshot or an end-of-scan notification.
The SDK has no reactive or hosting dependency.

### One consumer: an enumeration-owned monitor

```csharp
using Yubico.YubiKit.Core.Devices;

using var stop = new CancellationTokenSource();
var ct = stop.Token; // Cancel from the application's shutdown path.
await foreach (var change in YubiKeyManager.WatchAsync(ct))
{
    Console.WriteLine(change switch
    {
        DeviceAdded added => $"Connected: {added.Device.DeviceId}",
        DeviceRemoved removed => $"Removed: {removed.Device.DeviceId}",
        DeviceUpdated updated => $"Metadata changed: {updated.Device.DeviceId}",
        SnapshotComplete complete => $"Initial snapshot ready at revision {complete.Revision}",
        _ => "Unknown change"
    });
}
```

Creation is lazy, on the first `MoveNextAsync`. Each enumeration starts independent listeners and scans.
Completion, cancellation, an exception, or `break` disposes its private monitor. Cancellation throws
`OperationCanceledException`. Use the shared factory below when several consumers need one producer.

### Device picker: loading is not empty

Build a projection from replay, rather than prepending a separate snapshot or Find call (which introduces
a subscription gap). This example uses reference identity because removals carry the published object:

```csharp
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;

using var stop = new CancellationTokenSource();
var ct = stop.Token;
var devices = new HashSet<IYubiKey>(ReferenceEqualityComparer.Instance);
var loaded = false;
Console.WriteLine("Loading…");
await foreach (var change in YubiKeyManager.WatchAsync(ct))
{
    if (change is DeviceAdded added)
        devices.Add(added.Device);
    else if (change is DeviceRemoved removed)
        devices.Remove(removed.Device);
    else if (change is SnapshotComplete)
        loaded = true;
    // DeviceUpdated retains the object; render its updated CachedDeviceInfo below.
    Console.WriteLine(!loaded ? "Loading…" : devices.Count == 0 ? "No YubiKeys connected" : $"{devices.Count} YubiKey(s)");
}
```

For a one-off answer, call `FindAllAsync`. For continuous membership, keep this projection and filter it
locally. Monitoring scans all interfaces; there is no public interval or force-refresh option.

### Several consumers: a host-owned singleton

Applications using `Microsoft.Extensions.Hosting` can register one initialized monitor. The SDK itself
does not depend on hosting. The three-line adapter below stops monitoring at graceful host shutdown;
`await using` also cleans up if host startup or execution fails. Register consumers after this adapter.
Their watch loops may still be enumerating when `StopAsync` disposes the monitor; those enumerations end
normally, so consumers need no extra coordination beyond handling the end of the stream.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yubico.YubiKit.Core.Devices;

await using var monitor = await YubiKeyManager.CreateMonitorAsync();
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton(monitor); // Externally created instance: container does not own disposal.
builder.Services.AddHostedService<MonitorLifetime>();
// Register services that receive YubiKeyMonitor and call monitor.WatchAsync(theirStoppingToken).
using var host = builder.Build();
await host.RunAsync();

sealed class MonitorLifetime(YubiKeyMonitor monitor) : IHostedService
{
    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct) => monitor.DisposeAsync().AsTask();
}
```

The factory starts listeners and awaits the loop's first publication. Empty success is ready; first-scan
failure fails fast and cleans up partial ownership. Its token cancels initialization only. After return,
the monitor runs even without subscribers until disposed. There is no pause/restart: create a new instance.

On first enumeration, capture and subscription registration are atomic. Replay does not occupy the
256-change subscriber buffer, but changes arriving during replay do. Overflow ends only that subscriber
with `InvalidOperationException`: clear its projection and re-enumerate the same monitor. Cancellation
or disposing a subscription does not stop a shared monitor. Consumer code never runs inline on publication.

Disposal is sequential: stop/dispose listeners, cancel and await the scan loop, retire discovery, complete
the feed. There is no disposal timeout or abandoned managed loop. In-flight scan waits are bounded by the
discovery budgets, not a hard total wall-clock limit; already-open native work keeps its lease until actual
teardown. Healthy streams can drain committed history and end normally; new enumerations after disposal
end empty. Disposal is idempotent and does not dispose caller-owned sessions or connections.

### Technical troubleshooting

- Logger category: `Yubico.YubiKit.Core.Devices.YubiKeyMonitor`; see [logging setup](../LOGGING.md).
- Listener startup failure is logged and cleaned up independently. The other listener and five-second
  interval fallback continue; listeners are hints, not physical-device truth.
- A monitor never publishes a partial scan. If a transport's enumeration exceeds its budget during creation,
  `CreateMonitorAsync` fails with `TimeoutException`; after initialization the scan is logged and the previous
  snapshot stands until a later scan succeeds. Successful initialization does not prove that every interface
  or metadata field was accessible.
- First-scan exceptions fail creation. Later scan exceptions retain the previous publication, log, and retry
  on the next trigger. Unexpected terminal loop failure faults subscriptions; dispose and create a new monitor.
- Hotplug triggers globally invalidate identity/metadata evidence immediately before scanning, after
  coalescing. Without hints, same-interface swaps can remain stale until a scan observes absence.
- Disposal waits for the in-flight scan within its discovery budgets; cancellation is not proof of native
  release. See [monitor lifecycle](../architecture/event-driven-device-discovery.md#monitor-lifecycle-and-disposal).

### UI thread marshaling

Changes may be consumed on background threads. Capture the UI `SynchronizationContext` before awaiting
and marshal before touching controls. Here `Apply` represents the application's UI projection handler:

```csharp
static async Task WatchOnUiAsync(Action<DeviceChange> apply, CancellationToken ct)
{
    var ui = SynchronizationContext.Current
        ?? throw new InvalidOperationException("Start watching from the UI thread.");
    await foreach (var change in YubiKeyManager.WatchAsync(ct))
        ui.Post(_ => apply(change), null);
}
```

The application's posted-work queue needs its own lifetime/backpressure policy; the SDK buffer cannot
bound work already posted to the UI thread.

## Error handling

```csharp
try
{
    var devices = await YubiKeyManager.FindAllAsync(cancellationToken: cancellationToken);
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

## Testing pattern

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
in-scope connection sets, serial-based `SameDeviceAs` correlation across YubiKeys from independent scoped calls, retained snapshots,
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

Connections to YubiKeys from disjoint scopes are **never** opened simultaneously in hardware tests. Overlapping
and disjoint ownership bounds, cancellation, and queued-read retirement are tested with fakes in
`FindYubiKeysScopeTests`; successful read-only hardware checks do not imply universal physical-key locking.

`OwnedMonitorReadOnlyTests` adds four public-API cases requiring **at least two attached authorized keys**;
fewer usable fixtures fails that hardware prerequisite rather than counting a one-key run as two-key coverage.
They read initial membership through `WatchAsync` replay up to `SnapshotComplete` after awaited creation
(no polling into readiness), checking both fixtures, retained membership/object identity, subscriber-local cancellation, factory-token
independence, disposal/stream completion, recreate, independent convenience selection, and predicate-based
null/throw no-match behavior without unplugging anything. They log fixture ordinals/interface support,
not machine serials. Snapshot metadata is compared with sequential live reads only **after disposing the
monitor**. Incomplete initial metadata/interfaces remain failures, not successful two-key observations.

`ScopedDiscoveryReadinessTests` also pins that a complete serial-less bystander does not prevent readiness,
whereas any YubiKey lacking metadata exhausts the bounded retries. Run that hardware-free regression explicitly
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

## Race conditions

Device discovery is inherently subject to race conditions. If a device connects or disconnects during a scan:

- The returned list may not include a device that just connected
- The returned list may include a device that just disconnected

For ongoing tracking, own a monitor and consume its `WatchAsync`. Neither discovery nor tracking promises
an instantaneous global hardware snapshot.

## Migration from dependency-injection-based API

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
- Replace static Start/Stop/Watch/Shutdown/IsMonitoring with manager `WatchAsync`, or `CreateMonitorAsync`
  plus monitor `WatchAsync` and disposal. There are no aliases preserving the old global cache semantics.
- `DeviceEvent`/`DeviceAction` → `DeviceChange` records (`DeviceAdded`, `DeviceRemoved`, `DeviceUpdated`, `SnapshotComplete`).
- `CurrentDevices` removed: build a projection from replay, or call `FindAllAsync` for a one-off.
- `CancellationToken`-only `Find*` overloads removed: use the `cancellationToken:` named argument.
- Old `type` filtered a full scan; new `interfaces` narrows discovery. Preserve full connection slots with
  All plus local filtering when that is the caller's intent.

## Thread safety

- `Find*` operations are fully thread-safe and use fresh evidence per call.
- Concurrent `WatchAsync` enumerations are safe; manager enumerations own independent producers,
  monitor enumerations share one producer with independent buffers.
- `DisposeAsync` may overlap active `WatchAsync` enumerations; they end normally. It must not be called
  concurrently with itself, and a monitor is not reusable after it.
- Changes may be consumed on any thread; marshal application state changes as needed.
