# Physical Device Model

In v2 of the SDK, an `IYubiKey` represents **one physical YubiKey**, not a single transport interface.
A composite USB YubiKey exposes several interfaces at once — PC/SC CCID (smart card), HID FIDO, and HID OTP
— and discovery returns **one** `IYubiKey` for that physical key, with the interfaces it exposes described
by `AvailableConnections`. This document explains the model, how to discover and connect, where read-only
metadata lives, how each applet picks a transport, and how to migrate code written against the old
per-interface-handle model.

> **Platform note:** HID interface enumeration is implemented on macOS and Linux. On Windows, HID discovery
> is not yet implemented, so a YubiKey currently surfaces only its PC/SC (CCID) interface there. See
> [Platform Support For HID Discovery](#platform-support-for-hid-discovery).

See also: [event-driven device discovery](./event-driven-device-discovery.md) and the
[Core module README](../../src/Core/README.md).

## One IYubiKey Per Physical Device

`IYubiKey` (defined in `src/Core/src/Interfaces/IYubiKey.cs`) is intentionally small:

- `string DeviceId` — a stable identifier for the physical device.
- `ConnectionType AvailableConnections` — the concrete interfaces this device exposes, any combination of
  `SmartCard`, `HidFido`, and `HidOtp`. It never contains the `Hid` group flag or `All`.
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
using Yubico.YubiKit.Core.YubiKey;

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

### Platform Support For HID Discovery

HID interface enumeration (HID FIDO, HID OTP) is implemented on **macOS and Linux**. On **Windows** HID
enumeration is not yet implemented, so today a YubiKey is discovered through its PC/SC (CCID) interface only:
`AvailableConnections` will not include `HidFido`/`HidOtp`, HID connection filters return no devices, and a
composite USB key cannot merge HID interfaces it cannot see. The PC/SC SmartCard path works on all
platforms. This is a known platform residual tracked outside the composite-device model itself.

## Opening A Connection

Open a specific interface with the typed overload:

```csharp
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Hid.Interfaces;

await using var smartCard = await device.ConnectAsync<ISmartCardConnection>();
await using var fido = await device.ConnectAsync<IFidoHidConnection>();
await using var otp = await device.ConnectAsync<IOtpHidConnection>();
```

The parameterless `ConnectAsync()` is only for single-interface devices; on a composite device it throws
rather than silently choosing a surprising transport. To select a transport intentionally on a multi-
interface device, use the typed overload above or an applet session extension (below).

## Read-Only Metadata Ownership

Read-only physical-device metadata lives in **Core** (`Yubico.YubiKit.Core.YubiKey`): `DeviceInfo`,
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

**Held-transport fallback** (default path only): if no override is given and the SmartCard transport fails
to connect because another process holds the card (PC/SC `SCARD_E_SHARING_VIOLATION` /
`SCARD_E_SERVER_TOO_BUSY` — e.g. GnuPG `scdaemon` holding the CCID), the session falls back to the next
supported transport in the default order. An explicit override never falls back. The SDK never kills another
process to free a transport.

```csharp
// If the CCID is held by another process, this transparently falls back to HID FIDO/OTP.
await using var resilient = await device.CreateManagementSessionAsync();
```

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
| Reaching for Management types to read metadata | Read-only metadata types now in `Yubico.YubiKit.Core.YubiKey`; read via `GetDeviceInfoAsync()` |
| Applet extension assumed a single transport | Applet extensions select via documented default order + optional `preferredConnection` |

Practical steps:

1. Stop enumerating per-interface; treat each `FindAllAsync` result as a physical device and branch on
   `AvailableConnections` / `SupportsConnection(...)`.
2. Replace any scalar connection-type routing with typed `ConnectAsync<TConnection>()` or an applet session
   extension.
3. Where you need a specific transport, pass `preferredConnection`; otherwise rely on the documented default
   order (and held-transport fallback).
4. Update metadata type references to `Yubico.YubiKit.Core.YubiKey`.
