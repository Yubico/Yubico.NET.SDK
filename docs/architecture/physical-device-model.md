# Physical Device Model

In v2 of the SDK, an `IYubiKey` represents **one physical YubiKey**, not a single transport interface.
A composite USB YubiKey exposes several interfaces at once — PC/SC CCID (smart card), HID FIDO, and HID OTP
— and discovery returns **one** `IYubiKey` for that physical key, with the interfaces it exposes described
by `AvailableConnections`. This document explains the model, how to discover and connect, where read-only
metadata lives, how each applet picks a transport, and how to migrate code written against the old
per-interface-handle model.

> **Platform note:** HID interface enumeration is implemented on macOS, Linux, and Windows. On Windows,
> opening HID report handles can fail with access denied if another process holds the interface or if the
> environment requires an elevated process. See
> [Platform Support For HID Discovery](#platform-support-for-hid-discovery).

See also: [event-driven device discovery](./event-driven-device-discovery.md) and the
[Core module README](../../src/Core/README.md).

## One IYubiKey Per Physical Device

`IYubiKey` (defined in `src/Core/src/Abstractions/IYubiKey.cs`) is intentionally small:

- `string DeviceId` — a human-readable correlation identifier. Once a device object is published through
  `YubiKeyManager`, its `DeviceId` remains stable for that uninterrupted physical presence while its
  physical interface identity and `AvailableConnections` remain unchanged. The repository retains the
  originally published object across evidence-tier flips so its eventual `Removed` event correlates with
  the earlier `Added` event. A fresh direct scan object can still have an evidence-tier-derived ID
  (`ykphysical:topology:*`, `ykphysical:{serial}`, or `ykphysical:pid:*`) different from an earlier scan;
  do not treat independently created scan objects as durable identity records. The repository correlates
  physical presence by interface set (`CompositeYubiKey.PhysicalIdentityKeyFor`).
- `ConnectionType AvailableConnections` — the concrete interfaces this device exposes, any combination of
  `SmartCard`, `HidFido`, and `HidOtp`. It never contains the `Hid` group flag or `All`.
  This is the union of observed transport interfaces, not proof that every applet is enabled on every
  interface or that every combination is safe to use concurrently.
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
var devices = await YubiKeyManager.FindAllAsync(ConnectionType.All, forceRescan: true);

foreach (var device in devices)
{
    Console.WriteLine($"{device.DeviceId}: {device.AvailableConnections}");

    if (device.SupportsConnection(ConnectionType.SmartCard))
    {
        // This physical key exposes a smart card interface.
    }
}

// Filters return physical devices capable of the requested connection, not per-interface rows.
var fidoCapable = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
```

In the common case, discovery merges the interfaces of a single physical key by USB Product ID parsed from
the PC/SC reader name (serial number is consulted only to disambiguate multiple same-model keys), so a
physical key is returned as one device even when no connection is opened and even when another process holds
the CCID exclusively. NFC PC/SC devices are never merged with USB interfaces.

The one-device-per-physical-key result is the common merge case, not an absolute guarantee. Discovery
intentionally degrades to conservative **no-merge** in ambiguous cases — for example when a USB CCID reader
name cannot be parsed for its Product ID, or when a serial number needed to disambiguate same-Product-ID
keys cannot be read. In those cases interfaces are left unmerged rather than risk wrongly collapsing two
distinct keys, so one physical key can surface as more than one row.

`FindAllAsync(forceRescan: false)` returns the repository cache after its first populated scan;
`forceRescan: true` performs discovery and reconciles the result into that cache. Successful identity and
metadata reads are cached while their member interfaces remain present. Retaining the originally published
object preserves `DeviceId` event correlation, but also means a newly constructed equivalent object's
refreshed cached metadata/member instances are not substituted when the physical interface set and
`AvailableConnections` are unchanged. Request fresh Management data explicitly when current device
configuration matters. `DeviceChanges` is emitted from repository diffs after a full rescan, not directly
from native listener hints. These APIs inherit the conservative grouping bounds in
[Device Discovery Guarantees](device-discovery-guarantees.md); they do not strengthen them.

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

await using var smartCard = await device.ConnectAsync<ISmartCardConnection>();
await using var fido = await device.ConnectAsync<IFidoHidConnection>();
await using var otp = await device.ConnectAsync<IOtpHidConnection>();
```

The parameterless `ConnectAsync()` is only for single-interface devices; on a composite device it throws
rather than silently choosing a surprising transport. To select a transport intentionally on a multi-
interface device, use the typed overload above or an applet session extension (below).

## Read-Only Metadata Ownership

Read-only physical-device metadata lives in **Core** (`Yubico.YubiKit.Core.Devices`): `DeviceInfo`,
`FormFactor`, `DeviceCapabilities`, `DeviceFlags`, `VersionQualifier`, and `VersionQualifierType`. This lets
Core describe a physical device without depending on the Management module. Reading the metadata from a
device uses the Management extension, which opens a transient session:

```csharp
using Yubico.YubiKit.Management;

DeviceInfo info = await device.GetDeviceInfoAsync();
int? serial = info.SerialNumber;
FirmwareVersion firmware = info.FirmwareVersion;
```

**Mutating** operations — device configuration, reset, lock, reboot, and mode changes — remain owned by the
Management module (`ManagementSession`). Core owns only read-only metadata and the connection/discovery
machinery.

## Applet Transport Selection: Smart Defaults, Overrides, Fallback

Applet session-entry extensions keep their ergonomic one-call shape while selecting a transport
intentionally on a composite device. Each multi-transport applet documents a default order and accepts an
optional explicit `preferredConnection` override:

| Applet | Default order | Override (`preferredConnection`) |
| --- | --- | --- |
| Management (`CreateManagementSessionAsync`) | `SmartCard → HidFido → HidOtp` | any of those three |
| YubiOTP (`CreateYubiOtpSessionAsync`) | `SmartCard → HidOtp` | `SmartCard` or `HidOtp` |
| FIDO2 (`CreateFidoSessionAsync`) | `HidFido → SmartCard` | `HidFido` or `SmartCard` |
| WebAuthn (`CreateWebAuthnClientAsync`) | `HidFido → SmartCard` (forwards to FIDO2) | `HidFido` or `SmartCard` |

Single-transport applets (PIV, OATH, OpenPGP, Security Domain, YubiHSM) are SmartCard-only and take no
override.

```csharp
// Default order (no override): Management prefers SmartCard.
await using var mgmt = await device.CreateManagementSessionAsync();

// Explicit override: force HID OTP for this session.
await using var otpMgmt = await device.CreateManagementSessionAsync(
    preferredConnection: ConnectionType.HidOtp);
```

Override semantics, validated before any connect:

- `preferredConnection == null` → use the applet's documented default order.
- A concrete, applet-valid, device-supported value → used exactly.
- Not exactly one concrete transport (a group/combined/`Unknown` value) → `ArgumentException`.
- A concrete transport that is not valid for the applet (even if the device exposes it) → `ArgumentException`.
- A valid transport the device does not expose → `NotSupportedException`.

**Held-transport fallback** (default path only): if no override is given and the SmartCard transport cannot
be opened because the CCID interface is already held, the session falls back to the next supported transport
in the default order. Two things count as held, and they mean the same thing to the caller:

- **Another process** — PC/SC `SCARD_E_SHARING_VIOLATION` / `SCARD_E_SERVER_TOO_BUSY`, e.g. GnuPG
  `scdaemon` holding the CCID.
- **This process** — `ConnectionInUseException`, because a CCID interface admits one live connection and
  something in this process (a PIV or OATH session, say) already has it.

The in-process case is the common one and it is why the fallback exists: an internal `GetDeviceInfoAsync`
must not step on a session the caller opened three lines earlier. Both HID transports answer Management
correctly while a PIV session holds CCID, and the PIV session survives — hardware-measured.

An explicit override never falls back: `preferredConnection` is an instruction, and quietly opening a
different transport would be a lie, so the held error surfaces. The SDK never kills another process, and
never revokes an existing in-process holder, to free a transport.

```csharp
// If the CCID is held — by another process or by a session in this one — this transparently
// falls back to HID FIDO/OTP.
await using var resilient = await device.CreateManagementSessionAsync();
```

## One Connection Per CCID Interface, One Session Per Connection

A YubiKey's CCID interface holds exactly one selected application. Selecting another deselects the first and
destroys its security state — measured: after a second applet is selected, the first session's next command
returns `SW=0x6D00` (*instruction not supported*), and nothing at the intervening call site reports a problem.

The SDK refuses that at acquisition, before any command reaches the card, so the error lands on the call that
would have caused the damage rather than on the victim's next operation:

| Acquisition | Rule | On violation |
| --- | --- | --- |
| Second connection to a live CCID interface | Refused | `ConnectionInUseException` naming the interface |
| Second session on a live connection | Refused | `ConnectionInUseException` naming the current session |
| HID FIDO interface | Shared | — (CTAPHID channels provide protocol separation) |
| HID OTP interface | Exclusive | `ConnectionInUseException` naming the interface |

Both refusals are per *live* holder, not per lifetime. Successive use is the supported pattern, and it does
not require reconnecting — a session never disposes a connection it did not create:

```csharp
await using var connection = await device.ConnectAsync<ISmartCardConnection>();

await using (var piv = await PivSession.CreateAsync(connection))
    await piv.VerifyPinAsync(pin);

// Same connection, next application. The PIV session's disposal released it, not closed it.
await using var oath = await OathSession.CreateAsync(connection);
```

This mirrors canonical yubikit: Rust's applet sessions take the connection by value and hand it back with
`into_connection`, making a second concurrent session a compile error; Python's base `Session` binds itself
to the connection at construction. Neither inspects the wire.

**Who disposes what:** whoever created the connection. The `device.Create<App>SessionAsync()` convenience
methods open a connection the caller never sees, so the session they return owns and disposes it through the
internal `ApplicationSession.OwnConnection()` path. A direct `Session.CreateAsync(connection)` borrows the
caller-created connection and does not dispose it. Use `await using` for both connections and sessions.
Missing connection disposal can retain an exclusive CCID or OTP HID lease for the connection lifetime
(potentially the process lifetime), blocking later opens; there is intentionally no finalizer backstop,
because deterministic disposal is the only point at which native-handle teardown and lease release can be
ordered reliably.

OTP HID is exclusive because one OTP protocol exchange spans multiple feature reports; two independent
protocol instances on the same interface could interleave one logical frame. FIDO HID remains shared.
Management's default order may still fall through `SmartCard -> HidFido -> HidOtp`, but if another
connection already holds HID OTP, that final acquisition is refused rather than shared.

## SCP Note

Secure Channel Protocol is only valid on the SmartCard transport. Supplying `scpKeyParams` while a
non-SmartCard transport is selected (including the FIDO2/WebAuthn `HidFido`-first default) throws
`NotSupportedException` during session initialization. To use SCP, select the SmartCard transport explicitly
with `preferredConnection: ConnectionType.SmartCard`.

## Migration From The Per-Interface Handle Model

In v1, an `IYubiKey` was effectively one transport interface, and code commonly inspected a scalar
connection type and enumerated one row per interface. In v2:

| v1 pattern | v2 replacement |
| --- | --- |
| One `IYubiKey` per interface; multiple rows for one physical key | One `IYubiKey` per physical key; interfaces in `AvailableConnections` |
| Scalar `yubiKey.ConnectionType` to decide routing | `yubiKey.AvailableConnections` + `yubiKey.SupportsConnection(...)` |
| Parameterless `ConnectAsync()` picks "the" transport | `ConnectAsync<TConnection>()` for a specific interface; parameterless throws on multi-interface devices |
| Reaching for Management types to read metadata | Read-only metadata types now in `Yubico.YubiKit.Core.Devices`; read via `GetDeviceInfoAsync()` |
| Applet extension assumed a single transport | Applet extensions select via documented default order + optional `preferredConnection` |

Practical steps:

1. Stop enumerating per-interface; treat each `FindAllAsync` result as a physical device and branch on
   `AvailableConnections` / `SupportsConnection(...)`.
2. Replace any scalar connection-type routing with typed `ConnectAsync<TConnection>()` or an applet session
   extension.
3. Where you need a specific transport, pass `preferredConnection`; otherwise rely on the documented default
   order (and held-transport fallback).
4. Update metadata type references to `Yubico.YubiKit.Core.Devices`.
5. Dispose every connection at the scope that created it. If you create a connection and pass it to
   `Session.CreateAsync(connection)`, keep the connection in its own `await using`; the session will not
   close it. Dispose one session before constructing the next on that connection.
