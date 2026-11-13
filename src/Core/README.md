# Yubico.YubiKit.Core

Core is the foundation every other module builds on. It discovers YubiKeys, opens and owns connections
over SmartCard (PC/SC) and HID, runs the ISO 7816-4 APDU pipeline with automatic command chaining,
implements Secure Channel Protocol (SCP03 and SCP11), and supplies the shared device metadata,
cryptography, and TLV types. It has no application of its own; install an application module and Core
comes with it.

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.

## Preview session feature-query migration

`IApplicationSession.IsSupported(feature)` remains public for optional compatibility/UI queries. It evaluates
known firmware support, not runtime authentication, device configuration, or guaranteed command success.
`EnsureSupports(feature)` is now protected on `ApplicationSession` and removed from `IApplicationSession`;
derived applet sessions use it internally. Consumers should invoke operations directly and handle their
reported errors rather than calling an enforcement method first.

## APDU response ownership

`ApduResponse` is a sealed reference-type owner, not a value-type record. Its constructors copy their
input; `RawData` and `Data` are read-only views of the owned copy. Call `Clear()` after consuming sensitive
response material. It zeroes the complete owned response (including status bytes) and all previously
obtained views, without changing constructor input. Read/check status before clearing. Aliasing a response
shares its owner; there is no default empty response or synthesized value equality.

## Requirements

- .NET 10 on Windows, macOS, or Linux; Linux also needs PC/SC and udev rules ([Linux setup](../../docs/linux-setup.md)).
- A YubiKey 4 series, YubiKey 5 series, or Security Key series device.
- SmartCard (USB CCID or NFC), HID FIDO, and HID OTP transports. Which of these a given application uses is
  documented in that application's README.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Core --prerelease
```

You only need this line for device discovery, raw sessions, or SCP key parameters without an application
module. Every application package installs Core transitively.

## Getting started

```csharp
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Piv;

IYubiKey device = await YubiKeyManager.FindFirstAsync();
Console.WriteLine($"Serial {device.SerialNumber}: {device.AvailableConnections}");

foreach (var each in await YubiKeyManager.FindAllAsync())
{
    Console.WriteLine($"  {each.SerialNumber}: {each.AvailableConnections}");
}
```

One `IYubiKey` is one physical YubiKey, even when it exposes several interfaces at once. `SerialNumber` is
the durable identity; it is `null` on devices that do not report one, such as the Security Key series.
Discovery needs no PIN, touch, or open session. Later snippets assume these directives and a `device` obtained the same way;
`Yubico.YubiKit.Piv` is referenced only to show an application session.

## Common operations

### Own continuous tracking

manager watch creates a producer; monitor watch subscribes to an existing one.

`YubiKeyMonitor` owns one continuous tracking lifetime: `WatchAsync` replays `DeviceAdded`, emits
`SnapshotComplete` (including empty discovery), then streams `DeviceAdded`, `DeviceRemoved`, and
`DeviceUpdated` changes. Maintain a consumer projection for cached membership; disposal ends tracking
without disposing application-owned sessions/connections, and later enumerations complete empty.

Create it with `await YubiKeyManager.CreateMonitorAsync(cancellationToken)` and dispose with `await using`.
Initialization registers listeners before the loop's single initial scan and waits for publication,
including successful empty discovery. Startup failure/cancellation cleans up the partial lifetime; the
creation token does not control the monitor after successful creation. The monitor performs all
initial, listener-triggered, and five-second fallback scans sequentially. There is no manual refresh or
stop/restart mode: dispose the monitor and create a new one to recover from terminal failure. There is no
process-global monitoring lifetime or device cache.

Transport listeners share payload-less `IDeviceChangeListener.Changed` hints, not physical-device truth.
The monitor reconciles scans; listener failure falls back to interval discovery. An enumeration timeout is
handled per path: one-shot `Find…Async` logs a warning and returns the other transport's results for that
call; a monitor never publishes a partial scan, so the timeout fails `CreateMonitorAsync` on the first scan
and, after initialization, keeps the previous snapshot until a later scan succeeds.

Share one host-owned monitor between long-lived components rather than letting each component take over
tracking. Short commands should normally use the one-shot Find family. A configuration editor can use a
monitor for selection, but must explicitly read Management for current configuration.

`YubiKeyMonitor.WatchAsync` supplies initial `DeviceAdded` records, `SnapshotComplete`, then ordered changes. On each
enumeration's first `MoveNextAsync`, it captures membership and subscribes at the same boundary as a
completed publication, so there is no snapshot/subscription gap. Initial replay does not occupy the
independent 256-change delta buffer; changes arriving while replay is consumed can still overflow it.
Overflow faults only that watcher: **clear its device projection and re-enumerate the same monitor's
`WatchAsync`** to rebuild it safely. Canceling a watcher does not stop monitoring. Disposal allows already
captured replay and published deltas to drain, but new enumerations complete empty.

Use the replaying stream to initialize a UI projection, or independent `FindAllAsync` for a one-shot lookup.
Disposal sequentially stops listeners, cancels and awaits the loop with no drain timeout, retires discovery,
and completes `DeviceChangeFeed`; native discovery budgets bound waits, while already-open native work
retains its leases until actual teardown. Already-published replay and changes may still drain.
Concurrent watch enumerations are safe, and disposal may overlap them: they end normally. Disposal must
not be called concurrently with itself. See the
[usage guide](../../docs/usage/device-discovery.md#device-monitoring) for picker and generic-host samples.

### Discover selected interfaces once

```csharp
var smartCardYubiKeys = await YubiKeyManager.FindAllAsync(interfaces: ConnectionType.SmartCard, cancellationToken: cancellationToken);
var fidoYubiKeys = await YubiKeyManager.FindAllAsync(interfaces: ConnectionType.HidFido, cancellationToken: cancellationToken);
```

Each returned `IYubiKey` is a device view of one physical YubiKey through the requested interfaces.
Every Find call discovers from scratch without a shared repository or background tracking.
`interfaces` limits enumeration/probes and returned connection slots; it is not a full-device result filter.
To retain composite slots, discover All and filter locally. HID-only skips PC/SC; SmartCard-only skips HID. OTP/FIDO share HID descriptor
enumeration, but excluded interfaces are not probed. Existing monitors run unchanged.

A scoped call can split one physical key into several YubiKeys. Connection exclusion covers overlapping
known interface IDs, not YubiKeys from disjoint scopes, even if serials match; coordinate those rather than
opening them in parallel. Transport enumeration waits run concurrently and are bounded to five seconds each. A timeout logs a warning and omits only that transport's
interfaces for this scan. Cancellation interrupts the wait, not native work; abandoned work completes
and cleans up independently, with late completion/failure observed in Debug logs. Identity read budgets
remain per attempt, and cancellation does not prove native work drained. See
[independent scoped scans](../../docs/usage/device-discovery.md#independent-scoped-scans) for scope,
metadata, identity, and lifetime bounds.

### Read last-known device metadata without I/O

```csharp
if (device.CachedDeviceInfo is { } info)
{
    Console.WriteLine($"Last observed firmware: {info.FirmwareVersion}, form factor: {info.FormFactor}");
}
```

`CachedDeviceInfo` is a nullable Core `DeviceInfo` snapshot. Reading it opens no connection or session;
`null` means metadata is unavailable, including for custom `IYubiKey` implementations that use the default.
Existing discovery/repository updates may replace the snapshot without a device event, but it is not
guaranteed current and can remain historical after configuration changes or disconnection. A failed read
does not clear the last successful snapshot. Use `ManagementSession.GetDeviceInfoAsync` for a live read;
it does not promise to update discovery metadata. Continue using `device.SerialNumber` for durable
identity: its independent contract can retain a known serial even when the latest snapshot omits it.
See [cached device metadata](../../docs/usage/device-discovery.md#cached-device-metadata) for details.

### PC/SC sharing policy and migration

Built-in PC/SC connections default to strict `SmartCardSharing.Exclusive`. If another smart-card client
holds the card, opening fails with `ConnectionInUseException`; it does not retry shared access. Close
the competing client before retrying, or explicitly accept a coexistence policy. This replaces the former
`ExclusiveWithSharedFallback` default and can affect applications running alongside gpg-agent, browsers,
or OS smart-card services.

If discovery ran during contention, a PC/SC-only key may have a `null` `SerialNumber` because its metadata
could not be read. After closing the competing client, repeat the Find operation with fresh evidence:

```csharp
// After closing the competing client; expectedSerialNumber is the serial you want to find.
IYubiKey? matchingDevice = await YubiKeyManager.FindFirstOrDefaultAsync(
    key => key.SerialNumber == expectedSerialNumber, cancellationToken: cancellationToken);
```

A fresh lookup retries metadata reads; it does not guarantee a serial if the key remains busy,
unavailable, or does not report one. With monitoring active, a later successful scan can also refresh
the metadata.

Without topology evidence, contention on multiple same-model keys can leave PC/SC interfaces separate
from serial-bearing HID interfaces. A serial match may then lack SmartCard support; a PIV-opening
`NotSupportedException` can mean incomplete discovery, not that the hardware lacks PIV. After closing
the competing clients, repeat discovery as above and reacquire the device by serial before retrying; check
`SupportsConnection(ConnectionType.SmartCard)`. Previously returned `IYubiKey` objects have immutable
`AvailableConnections` and do not gain the remerged interfaces.

To retain the former fallback behavior for device-manager connections, configure once at startup,
**before discovery or monitoring**:

```csharp
SmartCardConnectionFactory.DefaultSharing = SmartCardSharing.ExclusiveWithSharedFallback;
IYubiKey device = await YubiKeyManager.FindFirstAsync();
```

Use `SmartCardSharing.Shared` instead to open shared without first attempting exclusive access, or select
a policy for one raw factory:

```csharp
// Coordinate discovery/monitoring and all other connections to this key before opening directly.
var factory = new SmartCardConnectionFactory(SmartCardSharing.Shared);
await using ISmartCardConnection connection = await factory.CreateAsync(reader, cancellationToken);
```

Direct `factory.CreateAsync` opens do not acquire the physical-device ownership guard used by
`IYubiKey.ConnectAsync`. Callers must coordinate discovery (including background monitoring) and other
connections for the full direct connection lifetime. With shared access, a discovery metadata read or
another handle can select a different applet and disturb the active session. Prefer
`device.ConnectAsync<ISmartCardConnection>()` for SDK-managed ownership; the selected PC/SC sharing
policy still determines whether other processes can open the card.

Factories snapshot `DefaultSharing` when constructed; changing it does not reconfigure existing factories
or connections. Shared and fallback policies permit other PC/SC handles to use card-wide security state,
including verified PIV PIN state. `connection.IsExclusive` reports the acquired mode. All built-in modes
still request `SCARD_UNPOWER_CARD` on release. PC/SC exclusivity is not an isolation guarantee for HID or
custom transports; no-hardware policy tests do not independently prove physical card isolation.

### Open an application session

Application sessions are the intended path. Each module adds a `Create<Application>SessionAsync`
extension on `IYubiKey` that selects a transport, opens a connection it owns, and selects the applet:

```csharp
await using var piv = await device.CreatePivSessionAsync();
```

Pass a `SessionCreationOptions` to override the transport, establish SCP, or receive touch notifications;
see [User interaction](../../docs/usage/user-interaction.md).
Touch notification contexts carry an `Application` and an optional `Operation` identifier. SDK-created
contexts set `Operation` to a unique constant such as `UserPresenceOperations.Ctap2.GetAssertion`, so a prompt
can switch on it directly. Wrap a call in `UserPresenceIntent.BeginScope("approve the transfer")` to pass the
application's reason to the prompt as `UserPresenceContext.Intent`.

Applet factories share connection binding, protocol preparation, and failure cleanup in `ApplicationSession`.
SCP requires PC/SC SmartCard: requesting it on a HID connection fails before any initialization commands are
sent. FIDO HID channels are prepared before applet discovery; SCP is established after selection and firmware
discovery. A failed direct factory releases its session claim without disposing the caller's connection.
`IsInitialized` becomes true only after all applet setup completes, not merely after protocol configuration.
Security Domain reset uses the same readiness transition when reinitializing the session.
Raw FIDO sessions still defer channel initialization until the first operation.

### Select one device

```csharp
IYubiKey first = await YubiKeyManager.FindFirstAsync();
IYubiKey? smartCard = await YubiKeyManager.FindFirstOrDefaultAsync(
    device => device.SupportsConnection(ConnectionType.SmartCard));
```

`FindFirstAsync` throws `InvalidOperationException` when nothing matches; `FindFirstOrDefaultAsync` returns
`null`. Both perform one fresh discovery operation, like `FindAllAsync`: they do not wait for a
key to be inserted, and "first" is not a stable ordering.

### Watch for devices being added and removed

```csharp
await foreach (DeviceChange change in YubiKeyManager.WatchAsync())
{
    Console.WriteLine(change switch
    {
        DeviceAdded added => $"Added: serial {added.Device.SerialNumber}",
        DeviceRemoved removed => $"Removed: serial {removed.Device.SerialNumber}",
        DeviceUpdated updated => $"Updated: serial {updated.Device.SerialNumber}",
        SnapshotComplete => "Initial snapshot complete",
        _ => "Unknown change"
    });
}
```

`WatchAsync` yields `DeviceAdded`, `DeviceRemoved`, and `DeviceUpdated` records computed from observed scans,
not raw operating-system notifications. Subscription starts on first enumeration, atomically replaying current
membership followed by `SnapshotComplete` (even for an empty snapshot), then ordered changes. `Revision`
increments once per scan; changes sharing a revision belong to that scan. Metadata updates retain the
originally published device object. Maintain a membership projection from the
stream, or use `FindAllAsync` for independent fresh discovery. This enumeration owns its private monitor;
share `CreateMonitorAsync` when several consumers need one producer.

**v2 prerelease migration:** replace `ScanAsync` with `FindAllAsync(interfaces: ...)`; remove `forceRescan`.
Replace static Start/Stop/Watch/Shutdown calls with an owned monitor and `DisposeAsync`. Old `type` filters
selected full-device results; the new `interfaces` restricts discovery, so retain All plus local filtering
where later operations need composite connection slots. No compatibility aliases retain the old cache behavior.

For two-key read-only validation of the public factory, replay, snapshots, lifetime, and metadata, see the
[targeted discovery hardware checks](../../docs/usage/device-discovery.md#read-only-scoped-discovery-hardware-checks).
Those unattended tests do not satisfy the separate attended removal/reinsertion acceptance gate.

### Send raw APDUs

When no application module models what you need, a raw session gives you framing, ownership, and
sequencing without applet checks. You still own the application-specific payload and response semantics.

```csharp
await using ISmartCardConnection connection = await device.ConnectAsync<ISmartCardConnection>();
await using RawSmartCardSession raw = await RawSmartCardSession.CreateAsync(connection);

// firmwareVersion is the device's firmware, read via Management; it sets APDU formatting. Configure first.
raw.Configure(firmwareVersion);
await raw.SelectAsync(ApplicationIds.Piv);
ApduResponse response = await raw.TransmitAndReceiveAsync(
    new ApduCommand(0x00, 0xCB, 0x3F, 0xFF, requestData),
    throwOnError: false);

Console.WriteLine($"SW={response.SW:X4}, {response.Data.Length} bytes");
```

`RawCtapHidSession` and `RawOtpHidSession` are the HID equivalents, created with
`device.CreateRawCtapHidSessionAsync()` and `device.CreateRawOtpHidSessionAsync()`.

`RawCtapHidSession` (formerly `RawFidoHidSession`) exchanges shared CTAPHID commands, not
CTAP2 operations. FIDO HID connection types and `ConnectionType.HidFido` retain their names.
`ApplicationIds.Fido` selects the shared FIDO applet; `ApplicationIds.FidoU2f` is the same-AID
interoperability alias, not another applet. FIDO2 application identities remain
`DeviceCapabilities.Fido2` and `UserPresenceApplications.Fido2`, while operation identifiers use
`UserPresenceOperations.Ctap2` and `Ctap2.Method` values.

An admitted session exchange protects its framing: cancellation is not an immediate abort or a
rollback of a command already sent. FIDO HID can send `CTAPHID_CANCEL` during keep-alives, then
waits for and drains a valid terminal response; OTP HID can send a dummy-report reset after
touch-wait cancellation.
A failed partial SmartCard chain or failed OTP reset requires disposal and reopening the connection,
even if a new session could otherwise borrow it. See [raw access tiers](../../docs/architecture/raw-access-tiers.md).

Direct connection calls are an expert escape hatch. They bypass session and protocol exchange guards;
do not interleave them with a session or retry on the same connection after an interrupted exchange.
Keep caller-owned input unchanged until the returned task finishes, then clear sensitive buffers.
Built-in macOS/Linux/Windows HID sends copy reports before native dispatch; custom connections need not. Async method
names do not guarantee caller-thread responsiveness or an immediate native cancellation.

```csharp
await using ISmartCardConnection connection = await device.ConnectAsync<ISmartCardConnection>();
// Direct I/O takes a preformatted APDU: no applet selection or command chaining is added here.
ReadOnlyMemory<byte> reply = await connection.TransmitAndReceiveAsync(commandBytes, cancellationToken);
```

For direct raw SmartCard transactions, `BeginTransactionAsync` returns `IDisposable`. The built-in
scope also implements `IAsyncDisposable` at runtime; the interface does not promise that for custom
connections. End the transaction before disposing the connection:

```csharp
await using ISmartCardConnection connection = await device.ConnectAsync<ISmartCardConnection>();
IDisposable transaction = await connection.BeginTransactionAsync();
try
{
    // Perform serialized raw operations on connection here.
}
finally
{
    if (transaction is IAsyncDisposable asyncTransaction)
        await asyncTransaction.DisposeAsync();
    else
        transaction.Dispose();
}
```

On built-in SmartCard connections, async begin does not block the caller; synchronous begin and
scope disposal wait for native work and have no fixed completion deadline. A custom connection
using the interface's default `BeginTransactionAsync` executes synchronous `BeginTransaction`
**before returning a task**. For a responsive calling thread, a custom implementation must override
the default; the override's responsiveness and native drain depend on its implementation, and neither
the override nor the returned scope automatically inherits the built-in
connection's native drain guarantees. End the scope before disposing its connection.

`IYubiKey` represents a physical key; `FindHidInterfaces` returns operating-system-exposed
`IHidInterface` report interfaces, not necessarily one per physical USB interface. An opened
`IHidConnection` provides report I/O; built-in Windows connections delegate native lifetime to
separate FIDO/OTP owners. The public direct report access methods `IHidInterface.ConnectToIOReports()` /
`ConnectToFeatureReports()` remain
synchronous. On macOS, the IO connection blocks the caller while the same persistent FIDO input
owner used by typed connections opens, receives, sends and shuts down. A six-second read timeout
detaches only that read; a late report remains available on retry. Native output and shutdown can
still block indefinitely. Feature-report connections retain their synchronous report interface but
open and perform native report calls on the OTP connection's worker, without a GET timeout.
Direct IO `SetReport(byte[])` forwards the supplied report length; typed FIDO sends still require 64-byte packets.
`IHidConnection.GetReport()` / `SetReport(byte[])` offer no cancellation token and wait on the caller's
thread. The caller owns the
direct connection; do not overlap report calls. Direct feature SET forwards any supplied report length;
typed OTP sends require eight bytes. Built-in macOS feature reports drain accepted calls before checked
release; direct report opens do not take a grouped-key registry claim. Synchronous
`ISmartCardConnection.BeginTransaction()` and transaction-scope `Dispose()` can also block on
native work; built-in SmartCard synchronous `Dispose()` waits for an admitted transmit to finish
before disconnect and context release. Do not dispose synchronously inside the operation it must
drain; prefer `DisposeAsync` in asynchronous code. For
method-by-method execution, ownership and evidence gaps, see
[retained synchronous compatibility paths](../../docs/architecture/raw-access-tiers.md#retained-synchronous-compatibility-paths).

The built-in macOS FIDO connection opens asynchronously, initializes its channel with an
awaited operation, and receives input through a persistent native owner. Blocking output
and checked shutdown run on connection-owned execution; an unconfirmed native close retains
the physical claim. This does not make the public lower-level `IHidConnection` interface
asynchronous or establish the same execution and drain behavior for custom HID. Windows and Linux use
their separate owners described below. See
[retained synchronous compatibility paths](../../docs/architecture/raw-access-tiers.md#retained-synchronous-compatibility-paths)
before using direct report access.

For source navigation, the macOS facades forward to `MacOSFidoHidNativeOwner` and
`MacOSOtpHidNativeOwner`; `MacOSHidInputBridge` supplies native callback delivery and
`MacOSHidReportApi` supplies the mechanical IOKit report calls. Portable USB/NFC
smart-card connections use `PcscConnection` and `PcscConnectionNativeOwner`.
These are internal organization changes. **The macOS extraction still requires native
and hardware testing on macOS before merge.**
The PC/SC connection logger category follows the new internal `PcscConnection` name;
its disposed-object diagnostic retains the legacy `UsbSmartCardConnection` string.

### Windows HID connections

Built-in Windows `ConnectAsync<IFidoHidConnection>` / `ConnectAsync<IOtpHidConnection>` and raw-session
factories now use separate asynchronous native owners. FIDO uses overlapped report completion through
`ThreadPoolBoundHandle`; OTP uses a connection-owned worker for zero-access open and feature GET/SET.
Open/metadata and checked shutdown run on the respective worker. Each owner refuses overlapping
ordinary calls rather than queueing them. Custom HID interfaces keep their compatibility adapters.

Cancellation before dispatch submits no report. After dispatch, FIDO cancellation requests targeted
native cancellation and waits for completion/retirement; native success may win. OTP waits for the
blocking call to return and may succeed after cancellation. Neither resets the device protocol.
Prefer `DisposeAsync`: it drains accepted work and checked cleanup without a fixed completion deadline.
The grouped-key connection/discovery claim transfers before open and releases only with proven cleanup,
including failed opens. Unproven close or BCL request preparation retains native ownership and the claim
while reporting a public fault.

Public direct `ConnectToIOReports()` / `ConnectToFeatureReports()` remain synchronous facades over these
same owners, with no implicit grouped-key claim. Report calls have no cancellation token. Direct IO and
feature payload sizes follow the descriptor (excluding the native report-ID byte), and SET must match
that descriptor exactly; typed FIDO requires 64 bytes and typed OTP eight bytes. Returned data belongs
to the caller. Public interfaces and raw-access tiers are unchanged.

The Windows implementation checkpoint at `bdac2f4f4` passed native Windows units,
59 selected device cases, redirected FIDO-over-CCID GetInfo, and an unsealed JIT
synthetic-pipe diagnostic. These results retain their original source identity;
they are not native verification of later consistency edits. AOT/sealed publication
and physical HID-driver race coverage remain outstanding. See the
[bounded acceptance record](../../docs/research/windows-async-native-acceptance.md)
and [raw access tiers](../../docs/architecture/raw-access-tiers.md).

### Linux HID connections

Public direct Linux HID access remains synchronous: reads and feature ioctls can block their caller.
The internal report-access boundary preserves caller-length IO and feature SET payloads within
native bounds; IO metadata/read capacity follows supported bounded unnumbered descriptors.
The separate internal FIDO and OTP owners enforce typed64 and typed8 at Send/Receive respectively.
Feature GET retains its 8-byte payload.
Native report-ID framing is hidden, and short output/feature
transfers fail instead of returning padded data or retrying a suffix. The report seam itself is
synchronous; migrated internal FIDO and OTP lifetimes supply separate asynchronous ownership.
No-hardware Linux ABI/framing tests can be run with:

```bash
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~Linux"
```

Fake-backed contract tests run on every host; libc-specific cases run only on Linux and exercise
glibc (`libc.so.6`), not physical YubiKeys. See [source evidence and limits](../../docs/research/linux-native-report-access.md).

An independent internal read reactor now supplies generation-aware readiness tokens with separate
activation outcome and actual quiescence. The existing native owner worker explicitly releases
the retained reference after quiescence; no cleanup jobs are scheduled. Private eventfd retirement
stays serialized on the poller, including admissions arriving during cleanup. The direct Linux IO
facade now uses this reactor and one dedicated native owner worker. Its public constructor/Get/Set
still wait synchronously; input waits indefinitely until data or terminal failure, with EOF now
reported as terminal rather than an empty successful report. Descriptor sizes and arbitrary supported
SET lengths remain available. Prefer asynchronous disposal, which awaits native output, real reactor
drain and checked close without a per-disposal Task.Run.
Its no-hardware tests are included by the Linux filter; see [L2 contract and limitations](../../docs/research/linux-read-reactor.md).

The internal `LinuxFidoHidConnection` supplies the built-in typed Linux FIDO path, providing
asynchronous open/send/receive/disposal against native/report and reactor seams. Public
`ConnectAsync<IFidoHidConnection>` and discovery now select it directly, not the synchronous report adapter.
The owner retains any supplied physical claim until native release is proven, and failed release
retains/quarantines that original claim. Pending read cancellation keeps native input running;
output cancellation after dispatch does not complete before the native call returns. See the
[L3 handoff](../../docs/research/linux-fido-async-lifetime.md) for precise lifetime bounds and tests.
An unexpected FIDO owner-worker failure terminalizes pending operations and stops new admissions;
the original worker still attempts safe checked cleanup. Disposal preserves the initial failure
even when native release succeeds, and unproven release still retains/quarantines ownership.

The internal `LinuxOtpHidConnection` now supplies asynchronous open/send/receive/disposal over the
feature-report seam, without using the read reactor or FIDO owner. Its one native worker drains an
admitted GET/SET before close; cancellation after submission cannot detach the call, impose a timeout,
or promise native abort. Output is copied and cleared before task completion; failed input is cleared.
The synchronous `LinuxHidFeatureReportConnection` facade uses this same owner, preserving GET8,
metadata8 and arbitrary supported direct SET lengths. Checked-close claim handling, worker-failure
cleanup and nonblocking finalization have the same release-proof discipline as FIDO, not shared code.
Public `ConnectAsync<IOtpHidConnection>` and discovery now select this owner directly too.
For no-hardware owner and protocol recovery tests, run:

```bash
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~LinuxOtp"
```

See [L4 lifetime and L5 handoff](../../docs/research/linux-otp-async-lifetime.md) for precise guarantees
and remaining hardware/native limits. These internal seams are not new public creation APIs.

Both typed Linux paths claim all known interfaces of the physical key **before** native open.
Discovery transfers its selected interface lease into the same native owner, including through a
published composite. A discovery timeout ends only the caller's wait: public reconnect is refused
until actual native cleanup proves release. Disposal can report a Linux EINTR/EIO close diagnostic
after safely releasing the claim; unknown release instead retains/quarantines it and subsequent opens
throw `UnrecoveredConnectionException`. A pending open/close can remain pending indefinitely if native
work does not return. No timeout or transport fallback manufactures release.

Use the existing typed API and dispose connections explicitly; raw sessions borrow and may be reused
sequentially on the same connection:

```csharp
await using var fidoHidConnection = await device.ConnectAsync<IFidoHidConnection>(cancellationToken);
await using (var raw = await RawCtapHidSession.CreateAsync(fidoHidConnection, cancellationToken))
{
    // CTAP HID initialization is lazy, on the first raw exchange.
}
await using var next = await RawCtapHidSession.CreateAsync(fidoHidConnection, cancellationToken);
```

Run the public factory/discovery/raw-session seam tests with:

```bash
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~LinuxHidRoutingTests"
```

These are fake-native tests, not hardware, native-AOT, or Ctap2/applet acceptance.
See [L5 routing contract and L6 handoff](../../docs/research/linux-hid-routing.md).

L6a adds bounded **managed** acceptance: actual Linux slots/owners under fake native seams support public
CTAP2 creation/GetInfo, keepalive cancellation/reuse, owned-session terminal drain/claim retention, and
YubiOTP creation/status/serial, touch cancellation/recovery and CRC-owned presence outcomes. Direct Linux
synchronous facades are now included in the independent public reachability and adapter-link registries.
Run the applet journeys without hardware:

```bash
dotnet toolchain.cs -- test --project Ctap2 --filter "FullyQualifiedName~Ctap2SessionLinuxCrossLayerTests"
dotnet toolchain.cs -- test --project YubiOtp --filter "FullyQualifiedName~YubiOtpSessionLinuxCrossLayerTests"
```

PC/SC portable managed tests additionally hold disconnect and context release before allowing the public
claim to reopen, and check live release failures remain quarantined. No production PC/SC code changed.
These tests do not attest Linux drivers, native AOT, hardware timing or whole-platform readiness; see
[L6a evidence and limits](../../docs/research/linux-managed-acceptance.md).

### Use a secure channel

Core owns the SCP key-parameter types; session factories establish the channel from them.

```csharp
using var keys = new StaticKeys(encKey, macKey, dekKey);
using var scp = new Scp03KeyParameters(KeyReference.Default, keys);

await using var raw = await device.CreateRawSmartCardSessionAsync(scp, firmwareVersion);
```

`Scp03KeyParameters.Default` carries the well-known factory keys for test devices. Manage the keys on the
device with `Yubico.YubiKit.SecurityDomain`.

## Cryptographic metadata

`CryptographicDefinitions` is an immediately usable, thread-safe catalog with three routes. `KeyType`
identifies concrete keys, curves have stable string identities, and algorithms describe operations:

```csharp
using Yubico.YubiKit.Core.Cryptography;
using static Yubico.YubiKit.Core.Cryptography.CryptographicDefinitions;

KeyInfo keyInfo = Keys.Get(KeyType.P256); // same instance as Keys.P256
Console.WriteLine(keyInfo.DisplayName); // P-256
Console.WriteLine(keyInfo.Curve?.FieldSizeBits); // 256
Console.WriteLine(keyInfo.Asn1?.AlgorithmOid); // 1.2.840.10045.2.1 (id-ecPublicKey)
Console.WriteLine(keyInfo.Asn1?.NamedCurveOid); // 1.2.840.10045.3.1.7

CurveInfo curveInfo = Curves.GetByOid("1.2.840.10045.3.1.7"); // named curves only
foreach (KeyInfo construction in Keys.FindByCurve(curveInfo))
    Console.WriteLine(construction.DisplayName); // a curve can have multiple concrete constructions
foreach (AlgorithmInfo algorithmInfo in Algorithms.FindForKey(keyInfo))
    Console.WriteLine(algorithmInfo.DisplayName); // multiple ECDSA choices, ECDH, ...

if (Keys.TryGet((KeyType)999, out KeyInfo? unknown))
    Console.WriteLine(unknown.DisplayName); // non-null on success
bool found = Curves.TryGetByOid("1.2.3.4.5", out _); // false
var matches = Algorithms.FindByOid(Oids.RsaPss); // generic + parameterized RSA-PSS
CurveInfo? rsaCurve = Keys.Rsa2048.Curve; // null
CurveInfo? aesCurve = Keys.Aes256.Curve; // null
Asn1KeyInfo? aesAsn1 = Keys.Aes256.Asn1; // null; CBC is an operation, not a bare key
```

`All` on each route is immutable and deterministically ordered. `Get`/`GetBy` return one result or throw
with the missing identity; `TryGet`/`TryGetBy` return false for expected misses, and `Find` returns an empty
collection if nothing matches. There is no registration step and no default algorithm on a key.
`Keys.GetByCurve` is an exactly-one convenience: it throws if multiple constructions share the curve;
use `Keys.FindByCurve` to select explicitly instead of assuming that curve identity determines a key.
`IKeyBase.KeyInfo` gives the same metadata on public/private keys. Use `DisplayName` for human output;
enum `ToString()` is not a persistence format.

**Do not conflate identifier domains.** A named-curve OID selects a mathematical curve; id-ecPublicKey
requires that curve as a parameter. RSA's rsaEncryption OID alone does not select a modulus size.
X25519/Ed25519 have mathematical curves but RFC 8410 ASN.1 identifiers with **absent** parameters. Operation
OIDs (including `Oids.Aes256Cbc` and `Oids.TripleDesCbc`) belong to `Algorithms`, not bare key metadata.
COSE `keyInfo.Cose` keeps kty/crv separate from the operation's numeric alg.
Decoders reject extra AlgorithmIdentifier fields: EC requires exactly one named-curve OID and Ed/X
requires none. RSA accepts absent or single NULL parameters on input and continues to encode NULL.

**Applicability is not capability or recommendation.** `FindForKey` reports structural compatibility,
not hardware/provider support, applet/slot/profile support, security guidance, or an exclusive choice.
ES256 itself is not restricted to P-256; protocol profiles can impose that restriction. The catalog includes
historical algorithms such as SHA-1 and Triple DES to describe existing SDK behavior, not recommend them.
Each applicability alternative combines its fields with AND; alternatives are combined with OR.
Fully specified Ed25519/X25519 operations constrain the concrete construction, not just its curve.
`HashAlgorithm == null` means no separately selected hash: it can describe an unspecified hash (generic
ECDSA), an intrinsic hash construction (Ed25519), or an operation without a hash parameter (CBC).
Numeric COSE mappings are not complete codec/profile contracts: COSE AES-CBC still requires RFC 9459's
PKCS#7 padding, IV handling and companion integrity protection. A generic CBC mode entry selects none of
those operation inputs for the caller.

Sizes have explicit units: `KeySizeBytes` is the RSA modulus width, ordinary EC coordinate width,
or stored symmetric/25519 width. `KeySizeBits` preserves the corresponding nominal size (25519 storage =
256 bits; Triple DES includes parity), not security strength. Use `Curve.FieldSizeBits`/`OrderSizeBits`
for mathematics and `PrivateValueLengthBytes`/`PublicValueLengthBytes` for scalar/seed and public encodings.
The 25519 field has 255 bits despite its 32-byte storage. Ordinary EC points are uncompressed `04 || X || Y`.

The modeled ordinary curves include NIST P-256/P-384/P-521, secp256k1, and Brainpool P256r1/P384r1/P512r1.
Host providers need not implement every curve; ASN metadata is not a promise of cryptographic provider support.
COSE mappings exist only for verified registered representations (Brainpool P256/P384/P512 = 256/258/259).
Protocol-owned OpenPGP
metadata is available through `keyInfo.GetOpenPgpInfo()` when the OpenPgp package is referenced.

## Constraints

- Choose one interface per connection with the typed `ConnectAsync<TConnection>()`. The parameterless
  overload is only for single-interface devices and throws on a composite key.
- A physical YubiKey admits one live connection across the interfaces discovery grouped together. A
  second attempt in the same process throws `ConnectionInUseException` until the first is disposed. The
  guard is process-local.
- One live session per connection. Dispose it before creating another over the same connection.
- Whoever creates a connection disposes it; use `await using`. A session from a `device.Create...` factory
  owns the hidden connection it opened. Built-in SmartCard connections request safe shutdown from their
  finalizer, but finalization is nondeterministic and is not a substitute for disposal.
- Sessions refuse overlapping operations. Caller cancellation does not interrupt constituent I/O
  after admission; FIDO keep-alive or OTP touch-wait cancellation can still end an exchange after
  protocol-specific cancel or reset handling. A failed partial exchange may require reopening the
  connection. Native work may not have a bounded completion time.
- Raw `IConnection` I/O bypasses session and exchange guards. The built-in SmartCard connection additionally
  refuses overlapping raw native operations instead of queuing them. Never interleave raw I/O with a live
  session, and dispose and reopen after an interrupted exchange.
- Built-in SmartCard cancellation prevents dispatch when observed first. Once a native PC/SC call starts, its
  task remains pending until the call returns so caller-owned input remains borrowed safely. Zero sensitive
  caller-owned input only after the task is terminal. Async disposal waits for accepted work, transaction end,
  disconnect, and context release without blocking the caller. Custom implementations must provide their own
  drain and borrowed-memory lifetime guarantees; these are not automatic for every SDK connection.
- On built-in macOS FIDO, canceling a pending raw read detaches that reader and frees its overlap slot;
  a later report remains queued for the next read. Cancellation between reads
  does not immediately abort the native device protocol. Built-in macOS OTP calls already active at cancellation
  may finish successfully. Do not assume cancellation releases native resources early; await the operation and
  connection disposal. Other HID implementations may have different cancellation behavior.
- Built-in SmartCard and macOS/Linux/Windows HID owners retain their physical-interface claim if native release cannot
  be proven; a later managed open throws `UnrecoveredConnectionException` rather than treating the key
  as ordinarily busy. Custom connections do not inherit those native-release guarantees.
- Discovery serializes USB metadata/identity reads that may target sibling interfaces, including native
  connection disposal. Without reliable physical identity yet, distinct same-PID keys share this admission;
  known different PIDs remain independent. An admitted unknown USB PID conservatively excludes all USB
  discovery reads, but a pending unknown-PID request reserves nothing and cannot block an available known
  PID. Caller timeout/cancellation does not release an active native owner's admission or extend discovery
  budgets. This is discovery scheduling, not a new public connection or cross-process ownership guarantee.
- `ProtocolFactory` and the `IProtocol` family are internal. Use `Raw*Session.CreateAsync(connection)`.

## Security notes

- SCP key material is caller-owned. `StaticKeys` and the `Scp*KeyParameters` types are `IDisposable` and
  zero their contents on disposal; zero any source buffers you supplied.
- Never embed or log real SCP keys, and disable trace logging in production; APDU traces can contain
  sensitive payloads.
- `Tlv` and the collection from `TlvHelper.DecodeList` own buffers and must be disposed. The encode helpers
  do not clear your source buffers or their own returned encodings; for sensitive data, zero both in a
  `finally`. Full examples: [TLV processing](../../docs/usage/tlv-processing.md).
- `ApduCommand` stores a reference to your data, not a copy. Zero the source after transmission.

## Related

For repeatable `FindAllAsync`/`CachedDeviceInfo` hardware validation, use the
[targeted read-only discovery checks](../../docs/usage/device-discovery.md#read-only-scoped-discovery-hardware-checks).
They cover every attached authorized serial-bearing USB key, use sequential connections only, and keep the
live-read Management dependency in its integration project. Scope exclusion and disjoint ownership limits
are checked deterministically rather than by racing native hardware connections.

- Application modules: [Management](../Management/README.md), [PIV](../Piv/README.md),
  [CTAP2](../Ctap2/README.md), [WebAuthn](../WebAuthn/README.md), [OATH](../Oath/README.md),
  [YubiOTP](../YubiOtp/README.md), [OpenPGP](../OpenPgp/README.md),
  [Security Domain](../SecurityDomain/README.md), [YubiHSM Auth](../YubiHsm/README.md).
- [Physical device model](../../docs/architecture/physical-device-model.md): one `IYubiKey` per key, transport selection, ownership.
- [Device discovery guarantees](../../docs/architecture/device-discovery-guarantees.md): grouping guarantees per platform.
- [Raw access tiers](../../docs/architecture/raw-access-tiers.md): applet sessions, raw sessions, raw connections.
- [Device discovery](../../docs/usage/device-discovery.md), [User interaction](../../docs/usage/user-interaction.md),
  and [TLV processing](../../docs/usage/tlv-processing.md).
- [Logging](../../docs/LOGGING.md): configure `YubiKitLogging.LoggerFactory` once at startup.
- [Developer guide](../../docs/DEV-GUIDE.md): building, testing, and contributing.
