# Physical Device Model

In v2 of the SDK, an `IYubiKey` represents **one physical YubiKey**, not a single transport interface.
A composite USB YubiKey exposes several interfaces at once — PC/SC CCID (smart card), HID FIDO, and HID OTP
— and discovery groups observed interfaces into an `IYubiKey` when its evidence permits, with those interfaces described
by `AvailableConnections`. This document explains the model, how to discover and connect, where read-only
metadata lives, how each applet picks a transport, and how to migrate code written against the old
per-interface-handle model.

> **Platform note:** HID interface enumeration is implemented on macOS, Linux, and Windows. On Windows,
> opening HID report handles can fail with access denied if another process holds the interface or if the
> environment requires an elevated process. See
> [Platform Support For HID Discovery](#platform-support-for-hid-discovery).

See also: [device identity and physical-device correlation](./device-identity.md),
[event-driven device discovery](./event-driven-device-discovery.md), and the
[Core module README](../../src/Core/README.md).

## One IYubiKey Per Physical Device

`IYubiKey` (defined in `src/Core/src/Abstractions/IYubiKey.cs`) is intentionally small:

- `string DeviceId` — a human-readable correlation identifier. Once a device object is published through
  an owned `YubiKeyMonitor` repository, its `DeviceId` remains stable for that uninterrupted physical presence while its
  physical interface identity and `AvailableConnections` remain unchanged. The repository retains the
  object only while fresh known serial evidence does not contradict it. A known-serial substitution
  produces `DeviceRemoved` followed by `DeviceAdded` for a new object. The repository retains the
  originally published object across evidence-tier flips so its eventual `DeviceRemoved` event correlates with
  the earlier `DeviceAdded` event. A fresh direct scan object can still have an evidence-tier-derived ID
  (`ykphysical:topology:*`, `ykphysical:{serial}`, or `ykphysical:pid:*`) different from an earlier scan;
  do not treat independently created scan objects as durable identity records. The repository correlates
  physical presence by interface set (`YubiKeyDevice.PhysicalIdentityKeyFor`, internal).
- `ConnectionType AvailableConnections` — the concrete interfaces this device exposes, any combination of
  `SmartCard`, `HidFido`, and `HidOtp`. It never contains the `Hid` group flag or `All`.
  This is the union of observed transport interfaces, not proof that every applet is enabled on every
  interface or that every combination is safe to use concurrently.
- `int? SerialNumber` — discovery's independently latched durable hardware identity, when known; see
  [device identity](device-identity.md). It is not derived from the latest metadata snapshot.
- `DeviceInfo? CachedDeviceInfo` — last-known read-only metadata, available without I/O. It can be null or
  stale; changed metadata on a retained monitor object produces `DeviceUpdated`. The default interface
  implementation returns null for custom implementations.
- `bool SupportsConnection(ConnectionType)` — whether a given interface is present on this device. The
  concrete values (`SmartCard`, `HidFido`, `HidOtp`) test a specific openable interface; the `Hid` group
  flag returns true when either HID interface is present; `Unknown`, `All`, and mixed/combined values
  return false.
- `Task<TConnection> ConnectAsync<TConnection>(CancellationToken)` — open a specific typed interface.
- `Task<IConnection> ConnectAsync(CancellationToken)` — open the device's connection **only when it exposes
  exactly one**; on a multi-interface device this is ambiguous and throws.

`ConnectionType` is a `[Flags]` enum. `SmartCard`, `HidFido`, and `HidOtp` are concrete, openable interfaces;
`Hid` is a group filter (HID FIDO + HID OTP) used by discovery; `All` is every interface; `Unknown` matches
none.

## Discovery

`YubiKeyManager` is the static entry point. `FindAllAsync` returns one `IYubiKey` per physical key:

```csharp
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;

// One IYubiKey per physical device, even when CCID + HID FIDO + HID OTP are all present.
var devices = await YubiKeyManager.FindAllAsync(interfaces: ConnectionType.All);

foreach (var device in devices)
{
    Console.WriteLine($"{device.DeviceId}: {device.AvailableConnections}");

    if (device.SupportsConnection(ConnectionType.SmartCard))
    {
        // This physical key exposes a smart card interface.
    }
}

// Keep full composite slots, then select physical keys capable of FIDO.
var fidoCapable = devices.Where(key => key.SupportsConnection(ConnectionType.HidFido));

// Or intentionally discover only FIDO interfaces; these YubiKeys have no CCID/OTP slots.
var fidoYubiKeys = await YubiKeyManager.FindAllAsync(interfaces: ConnectionType.HidFido);
```

In the common case, discovery merges the interfaces of a single physical key by USB Product ID parsed from
the PC/SC reader name (serial number is consulted only to disambiguate multiple same-model keys), so a
physical key is returned as one device even when no connection is opened and even when another process holds
the CCID exclusively. NFC PC/SC devices are never merged with USB interfaces.

The one-device-per-physical-key result is the common merge case, not an absolute guarantee. Ambiguous
observations can leave a key split across rows. Conversely, complementary partial observations from
two same-PID keys can satisfy the PID-completeness heuristic and form a cross-key composite. A slot
still opens its enumerated path, but selecting the composite by serial does not independently verify
every slot against that serial. See the [G2 evidence bound](device-discovery-guarantees.md#g2-the-epistemic-bound).

Every `FindAllAsync`, `FindFirstAsync`, and `FindFirstOrDefaultAsync` call performs independent one-shot
discovery with fresh per-call evidence, including after an earlier empty result. First-result methods
select after discovery; they do not wait for insertion. There is no global repository or tracking lifetime.
Discovery populates best-effort metadata for every device shape, including one-interface USB and NFC
records. Grouping does not depend on those results, but bounded reads are awaited and may delay completion.

For tracking, explicitly own a ready monitor:

```csharp
await using var monitor = await YubiKeyManager.CreateMonitorAsync(cancellationToken);
await foreach (var change in monitor.WatchAsync(cancellationToken))
{
    // DeviceAdded replay, SnapshotComplete (even if empty), then live DeviceChange records.
    Apply(change);
}
```

manager watch creates a producer; monitor watch subscribes to an existing one.

The monitor's finder retains successful evidence until invalidation. Observed absence and listener
activity evict identity and metadata; PID changes are checked by the identity cache, while the metadata
cache currently lacks an equivalent PID check. Successful serial-less metadata can also remain cached.
When a later scan obtains metadata for an unchanged
physical device, the diff updates its retained object and emits `DeviceUpdated` if metadata changed.
This is last-known data, not continuously refreshed configuration. Request live Management data when
current configuration matters. `WatchAsync` changes come from snapshot diffs, not native hints.
These APIs inherit [Device Discovery Guarantees](device-discovery-guarantees.md); they do not strengthen them.

One-shot `interfaces` limits enumeration/probes and returned connection slots, not just result selection.
SmartCard-only skips HID, HID-only skips PC/SC, and OTP/FIDO share descriptor enumeration but not excluded
interface probes. A view contains only observed in-scope interfaces; partial scope does not relax the
PID-completeness rule. Existing monitors run unchanged. See
[independent scoped scans](../usage/device-discovery.md#independent-scoped-scans) for examples and lifetime
bounds. Connections exclude other device views only through overlapping known member interface IDs:
YubiKeys from disjoint scopes cannot promise physical-key exclusion even with matching serials. Coordinate them explicitly.
Monitor disposal releases its tracking resources, not caller-owned sessions/connections or returned
descriptors. Build membership from stream replay, or call `FindAllAsync` for a one-off; new subscriptions
after disposal end empty. Disposal sequentially stops listeners, cancels/awaits the loop, retires discovery,
and completes the feed, without a separate timeout. Discovery waits are bounded by the discovery budgets.

### Platform Support For HID Discovery

HID interface enumeration (HID FIDO, HID OTP) is implemented on **macOS, Linux, and Windows**. On Windows,
discovery uses ConfigMgr interface metadata and does not need to open report handles just to identify YubiKey
HID interfaces. Opening a HID report connection can still fail with `UnauthorizedAccessException` when Windows
denies access to the interface, for example because another process holds it exclusively or because the
current environment requires the process to run elevated as Administrator. The PC/SC SmartCard path works on
all platforms and is unaffected by HID report-handle access.

## Opening A Connection

Open a specific interface with the typed overload:

```csharp
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

await using (var smartCard = await device.ConnectAsync<ISmartCardConnection>())
{
    // Use SmartCard, then dispose it before opening another interface.
}

await using (var fido = await device.ConnectAsync<IFidoHidConnection>())
{
    // Use FIDO HID, then dispose it before opening another interface.
}

await using (var otp = await device.ConnectAsync<IOtpHidConnection>())
{
    // Use OTP HID.
}
```

The parameterless `ConnectAsync()` is only for single-interface devices; on a composite device it throws
rather than silently choosing a surprising transport. To select a transport intentionally on a multi-
interface device, use the typed overload above or an applet session extension (below).

## Read-Only Metadata Ownership

Read-only physical-device metadata lives in **Core** (`Yubico.YubiKit.Core.Devices`): `DeviceInfo`,
`FormFactor`, `DeviceCapabilities`, `DeviceFlags`, `VersionQualifier`, and `VersionQualifierType`. This lets
Core describe a physical device without depending on the Management module. `IYubiKey.CachedDeviceInfo`
exposes `YubiKeyDevice`'s existing atomically published snapshot, not a second cache:

```csharp
DeviceInfo? lastKnown = device.CachedDeviceInfo; // No I/O or presence check.
```

Null means unavailable. Failed metadata reads preserve a successful snapshot, and replacing that snapshot
preserves the scalar fields in a previously returned value. `ChallengeResponseTimeout` currently aliases its input
TLV memory, so read-only access does not establish independent storage or stability after owner cleanup.
The data is not guaranteed current and
can remain historical on retained objects after configuration changes or disconnection. A replacement
object does not inherit its predecessor's snapshot. `SerialNumber` keeps its independent identity contract
even if the latest snapshot has no serial. See [cached device metadata](../usage/device-discovery.md#cached-device-metadata).

A live read uses the Management extension, which opens a transient session:

```csharp
using Yubico.YubiKit.Management;

DeviceInfo info = await device.GetDeviceInfoAsync();
int? serial = info.SerialNumber;
FirmwareVersion firmware = info.FirmwareVersion;
```

`ManagementSession.GetDeviceInfoAsync` and the convenience extension above do not promise to update
discovery metadata.

**Mutating** operations — device configuration, reset, lock, reboot, and mode changes — remain owned by the
Management module (`ManagementSession`). Core owns only read-only metadata and the connection/discovery
machinery.

## Applet Transport Selection: Smart Defaults And Overrides

Applet session-entry extensions keep their ergonomic one-call shape while selecting a transport
intentionally on a composite device. Each multi-transport applet documents a default order and accepts an
optional explicit `SessionCreationOptions.PreferredConnectionType` override. When SCP parameters are supplied
without an override, the SCP-capable SmartCard transport is selected instead of the normal default:

| Applet | Default order | `PreferredConnectionType` values |
| --- | --- | --- |
| Management (`CreateManagementSessionAsync`) | `SmartCard → HidFido → HidOtp` | any of those three |
| YubiOTP (`CreateYubiOtpSessionAsync`) | `SmartCard → HidOtp` | `SmartCard` or `HidOtp` |
| CTAP2 (`CreateCtap2SessionAsync`) | `HidFido → SmartCard` | `HidFido` or `SmartCard` |
| WebAuthn (`CreateWebAuthnClientAsync`) | `HidFido → SmartCard` (forwards to CTAP2) | `HidFido` or `SmartCard` |
| OATH (`CreateOathSessionAsync`) | `SmartCard` only | `SmartCard` |

Single-transport applets (PIV, OATH, OpenPGP, Security Domain, YubiHSM) are SmartCard-only. OATH accepts an
explicit `PreferredConnectionType` of `SmartCard`; that is an accepted setting, not an alternative transport.

```csharp
// Default order (no override): Management prefers SmartCard.
await using (var mgmt = await device.CreateManagementSessionAsync())
{
    var info = await mgmt.GetDeviceInfoAsync();
    Console.WriteLine(info.FirmwareVersion);
}

// Explicit override: force HID OTP for this session.
await using (var otpMgmt = await device.CreateManagementSessionAsync(
    new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp }))
{
    var info = await otpMgmt.GetDeviceInfoAsync();
    Console.WriteLine(info.FirmwareVersion);
}
```

Override semantics, validated before any connect:

- Explicit valid `options.PreferredConnectionType` → use it exactly.
- Otherwise, non-null `options.ScpKeyParameters` → select SmartCard.
- Otherwise → use the applet's documented default order.
- Not exactly one concrete transport (a group/combined/`Unknown` value) → `ArgumentException`.
- A concrete transport that is not valid for the applet (even if the device exposes it) → `ArgumentException`.
- A valid transport the device does not expose → `NotSupportedException`.

The resolver selects exactly one transport. The session entry point opens it once; it does not retry another
interface after `ConnectionInUseException`, PC/SC sharing errors, cancellation, or initialization failure.

## One Connection Per Physical Device, One Session Per Connection

A YubiKey's basic CCID channel has one selected application. Selecting another applet changes that
selection and can disturb the first session. In the recorded PIV/Management hardware sequence, the
next PIV command returned `SW=0x6D00` (*instruction not supported*). That observation does not establish
that every applet's security state is cleared by selection; PIV PIN state and power/reset effects have
their own conditions.

The SDK refuses that at acquisition, before any command reaches the card, so the error lands on the call that
would have caused the damage rather than on the victim's next operation:

| Acquisition | Rule | On violation |
| --- | --- | --- |
| Second connection to any known interface of a grouped physical key | Refused | `ConnectionInUseException` naming a held member interface |
| Second session on a live connection | Refused | `ConnectionInUseException` naming the current session |

Both refusals are per *live* holder, not per lifetime. Successive use is the supported pattern, and it does
not require reconnecting — a session never disposes a connection it did not create:

```csharp
await using var connection = await device.ConnectAsync<ISmartCardConnection>();

await using (var piv = await PivSession.CreateAsync(connection))
    await piv.VerifyPinAsync(pin);

// Same connection, next application. The PIV session's disposal released it, not closed it.
await using var oath = await OathSession.CreateAsync(connection);
```

Core enforces this lifetime through its connection/session guards before applet initialization. This
managed ownership rule is separate from the operating system's sharing mode and the card's security state.

**Who disposes what:** whoever created the connection. The `device.Create<App>SessionAsync()` convenience
methods open a connection the caller never sees, so the session they return owns and disposes it through the
internal `ApplicationSession.OwnConnection()` path. A direct `Session.CreateAsync(connection)` borrows the
caller-created connection and does not dispose it. Use `await using` for both connections and sessions.
Missing connection disposal can retain the physical-device lease for the connection lifetime
(potentially the process lifetime), blocking later opens. Sessions have no finalizer recovery. Some
built-in connections request best-effort native cleanup from a finalizer, but that is not a guarantee of
timely teardown or lease release. Use deterministic disposal to observe completion and failures.

The registry is process-local; it does not exclude other processes' handles. Opening any grouped member
claims all known stable member interface IDs. When conservative discovery cannot
establish grouping, a standalone record retains a one-element scope. The PID-completeness exception
described above remains a bound on what a grouped object proves.

macOS FIDO HID must be opened with `kIOHIDOptionsTypeNone`, never `kIOHIDOptionsTypeSeizeDevice`. SDK
exclusivity is enforced before native open; seizing would add a separate platform-wide exclusion policy
and lock other processes out of the key. Both canonical
yubikit implementations open non-seizing on macOS: Rust enables hidapi's `macos-shared-device`
(`hid_darwin_set_open_exclusive(0)`), and python-fido2's macOS backend calls `IOHIDDeviceOpen(handle, 0)`.

## SCP Note

Secure Channel Protocol is valid only on SmartCard. Supplying `SessionCreationOptions.ScpKeyParameters`
without an explicit override selects SmartCard automatically. Explicitly selecting a non-SmartCard transport
with SCP parameters throws `NotSupportedException` during session initialization.

## Migration From The Per-Interface Handle Model

In v1, an `IYubiKey` was effectively one transport interface, and code commonly inspected a scalar
connection type and enumerated one row per interface. In v2:

| v1 pattern | v2 replacement |
| --- | --- |
| One `IYubiKey` per interface; multiple rows for one physical key | One `IYubiKey` per physical key; interfaces in `AvailableConnections` |
| Scalar `yubiKey.ConnectionType` to decide routing | `yubiKey.AvailableConnections` + `yubiKey.SupportsConnection(...)` |
| Parameterless `ConnectAsync()` picks "the" transport | `ConnectAsync<TConnection>()` for a specific interface; parameterless throws on multi-interface devices |
| Reaching for Management types to read metadata | Read-only metadata types now in `Yubico.YubiKit.Core.Devices`; read via `GetDeviceInfoAsync()` |
| Applet extension assumed a single transport | Applet extensions select via documented default order + optional `SessionCreationOptions.PreferredConnectionType` |

Practical steps:

1. Stop enumerating per-interface; treat each `FindAllAsync` result as a physical device and branch on
   `AvailableConnections` / `SupportsConnection(...)`.
2. Replace any scalar connection-type routing with typed `ConnectAsync<TConnection>()` or an applet session
   extension.
3. Where you need a specific transport, set `SessionCreationOptions.PreferredConnectionType`; otherwise rely
   on the documented default order. Connection failures do not select another interface.
4. Update metadata type references to `Yubico.YubiKit.Core.Devices`.
5. Dispose every connection at the scope that created it. If you create a connection and pass it to
   `Session.CreateAsync(connection)`, keep the connection in its own `await using`; the session will not
   close it. Dispose one session before constructing the next on that connection.
