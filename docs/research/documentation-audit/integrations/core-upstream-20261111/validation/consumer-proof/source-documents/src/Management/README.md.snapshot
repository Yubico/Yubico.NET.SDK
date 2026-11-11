# Yubico.YubiKit.Management

The Management application is the device-level control surface of a YubiKey. This package reads device
information - serial number, firmware, form factor, supported and enabled capabilities - and writes device
configuration: enabled applications per transport, timeouts, device flags, NFC restriction, and the
configuration lock code. Other modules talk to one application; this one controls which supported applications
are enabled on each transport. Configuration cannot add applications the hardware does not support.

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.

## Requirements

- .NET 10. On Linux, install PC/SC and the udev rules described in [linux-setup.md](../../docs/linux-setup.md).
- Device-information reads support firmware 4.1.0 or later; writing configuration needs 5.0.0 or later.
- Transports: SmartCard (CCID, USB or NFC), HID FIDO, or HID OTP - the only module that runs over all three.

SCP is available only over SmartCard. Supplying `SessionCreationOptions.ScpKeyParameters` with a HID connection
throws before any initialization commands are sent.

Creation selects Management over SmartCard, reads status over OTP HID, or uses the prepared FIDO HID channel
version before resolving DeviceInfo. Device reset is available only over SmartCard; HID reset requests are
rejected without sending a reset command.

| Feature | Minimum firmware | Transport |
|---------|------------------|-----------|
| Read device information | 4.1.0 | SmartCard, HID FIDO, HID OTP |
| Write device configuration | 5.0.0 | SmartCard, HID FIDO, HID OTP |
| Bio Multi-protocol Edition combined reset (`ResetDeviceAsync`) | 5.6.0 SDK gate | SmartCard (CCID) only |

The read minimum is a support requirement, not a host-side preflight check: the SDK can send a read to an
older key and propagate a device or parsing error. Writes and reset check the effective session firmware;
passing those generic checks does not establish support for every field or device family. In particular,
NFC restriction needs NFC-capable hardware with firmware 5.7 or later.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Management --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Getting started

```csharp
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Management;

IYubiKey device = await YubiKeyManager.FindFirstAsync();
await using (var session = await device.CreateManagementSessionAsync())
{
    var info = await session.GetDeviceInfoAsync();
    Console.WriteLine($"Serial {info.SerialNumber}, firmware {info.FirmwareVersion}, {info.FormFactor}");
    Console.WriteLine($"USB enabled: {info.UsbEnabled}");
}
```

Later snippets are independent alternatives, each assuming these directives and a `device` obtained the
same way, with no previous connection/session still open. For a single operation, the
one-shot extensions `device.GetDeviceInfoAsync()` and `device.SetDeviceConfigAsync(...)` open a connection, act,
and dispose both.

## Common operations

### Query capabilities

```csharp
var info = await device.GetDeviceInfoAsync();
bool pivOverUsb = (info.UsbSupported & DeviceCapabilities.Piv) != 0;
bool oathEnabledOverNfc = (info.NfcEnabled & DeviceCapabilities.Oath) != 0;
Console.WriteLine($"Configuration locked: {info.IsLocked}");
```

`UsbSupported` and `NfcSupported` describe the hardware; `UsbEnabled` and `NfcEnabled` report enabled
applications. After a configuration write, reported persisted values need not mean the active interfaces
have already changed.

The current Core decoder has two metadata limitations: FIPS capability/approval tags use the wrong bit
mapping, so `FipsCapabilities`, `FipsApproved` and derived filters must not be used to establish application
approval. `IsFips` is a separate manufacturing indicator, not an operating mode. Also, product flag bits
can remain in `FormFactor`, so equality with a named base value can miss a flagged device.

### Enable or disable applications

```csharp
var info = await device.GetDeviceInfoAsync();
var requested = DeviceCapabilities.Piv | DeviceCapabilities.Oath;
if ((info.UsbSupported & requested) != requested)
{
    throw new NotSupportedException("This key does not support both requested USB applications.");
}
var config = new DeviceConfig { UsbEnabled = requested };

await using (var session = await device.CreateManagementSessionAsync())
{
    await session.SetDeviceConfigAsync(config, new SetDeviceConfigOptions { Reboot = true });
}

// The session above is disposed. This pause is illustrative, not an enumeration guarantee.
await Task.Delay(TimeSpan.FromSeconds(3));
var rebooted = await YubiKeyManager.FindAllAsync(); // Every Find performs fresh discovery.
```

`UsbEnabled` and `NfcEnabled` are complete replacement sets, not lists of applications to add. The example
disables every other USB application. Preserve a supported USB application and a recovery route; after
reboot, select the same intended key from fresh discovery results before opening another session. Discovery
order and a fixed delay do not establish device identity or readiness.

`DeviceConfig` is a record with settings corresponding to `DeviceInfo`: `UsbEnabled`, `NfcEnabled`, `AutoEjectTimeout`,
`ChallengeResponseTimeout`, `DeviceFlags` and `NfcRestricted`. Leave a property `null` to keep the device's
current value, so deriving from what the key reports is a one-liner:
`new DeviceConfig { UsbEnabled = info.UsbEnabled & ~DeviceCapabilities.Otp }`. At least one USB capability must
remain enabled. The SDK rejects exactly `DeviceCapabilities.None` for USB, but does not check the requested
mask against this key's supported applications or comprehensively validate arbitrary cast values. NFC
`None` is accepted and clears that transport's enabled application set.

### Timeouts, flags and NFC restriction

Timeouts are in seconds. The SDK accepts auto-eject values 0–3600 and challenge-response values 0–60,
checking those limits when properties are assigned; these are SDK limits, not established firmware maxima.
Auto-eject zero disables automatic ejection. Nonzero auto-eject controls CCID inactivity and is meaningful
with `DeviceFlags.TouchEject` in CCID-only mode. Setting the timeout does not set that flag. Challenge-response
zero requests the device default (normally 15 seconds). A non-null `DeviceFlags` replaces the entire flag set.

On NFC-capable keys with firmware 5.7 or later, `new DeviceConfig { NfcRestricted = true }` requests temporary
NFC restriction. The configured NDEF URL remains readable. To remove the restriction, supply USB power for
three seconds; a charger is sufficient, with no touch or authenticated USB operation required. This differs
from changing `NfcEnabled`. `null` omits the restriction field; `false` encodes a zero-valued request, but
must not be relied on as a verified recovery procedure. Use the documented USB-power procedure. The SDK
does not preflight the field's firmware or NFC-hardware requirements.

### Lock the configuration

```csharp
await using var session = await device.CreateManagementSessionAsync();
byte[] lockCode = RandomNumberGenerator.GetBytes(16);
try
{
    // Persist lockCode in your secret store first. It cannot be recovered.
    await session.SetDeviceConfigAsync(
        new DeviceConfig(),
        new SetDeviceConfigOptions { NewLockCode = lockCode });
}
finally
{
    CryptographicOperations.ZeroMemory(lockCode);
}
```

While locked, each protected write must supply `CurrentLockCode`. It authorizes that command only; it does
not unlock the session or remove the persistent lock. Passing a nonzero `NewLockCode` replaces the code.

To remove the lock, supply the current code and sixteen zero bytes in the same write. This fragment assumes
`byte[] currentLockCode` was loaded from your secret store and is owned by this scope:

```csharp
try
{
    await device.SetDeviceConfigAsync(
        new DeviceConfig(),
        new SetDeviceConfigOptions
        {
            CurrentLockCode = currentLockCode,
            NewLockCode = new byte[16]
        });
}
finally
{
    CryptographicOperations.ZeroMemory(currentLockCode);
}
```

An empty memory value is not a removal request: present lock codes must contain exactly 16 bytes.

### Factory reset

This combined reset is documented for **YubiKey Bio Multi-protocol Edition**, not every key whose firmware
passes the SDK check. It deletes FIDO credentials, fingerprints and the shared PIN, and PIV keys,
certificates and user data, except the F9 attestation slot. It cannot be undone. It is not a documented
recovery procedure for a lost Management configuration lock code. Other families use per-application resets.

After confirming the product and authorizing the destruction, select SmartCard explicitly:

```csharp
await using var session = await device.CreateManagementSessionAsync(
    new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard });

// Destructive: the caller has confirmed Bio MPE applicability and application-data erasure.
await session.ResetDeviceAsync();
```

If SmartCard is not exposed, session creation throws `NotSupportedException`. On an existing session,
`ResetDeviceAsync` throws `NotSupportedException` if the effective session firmware is a release version below
5.6.0 or the session uses either HID transport, without sending a reset command.
`SessionCreationOptions.FirmwareVersionOverride` applies to this check; major-zero development versions bypass it.
Passing the check does not guarantee that the device implements whole-device reset; a device rejection
propagates as `ApduException` rather than `NotSupportedException`.

## User interaction

Management commands do not require touch, a PIN or a PIV management key. Configuration protection uses
the 16-byte configuration lock code. Optional SCP uses separately supplied, caller-owned key parameters;
the session borrows them for initialization, and the caller remains responsible for their disposal.
`SessionCreationOptions.UserPresencePrompt` is accepted by the options object but is not retained,
invoked or disposed by this module.

Pass a `cancellationToken` to request cancellation; an admitted SmartCard
exchange drains before returning, and cancellation does not roll back configuration already sent. HID wait
cancellation has transport-specific behavior. Await operations sequentially: overlapping exchanges are
refused, not queued. A multi-page read contains separate exchanges; it is not one atomic public operation.

Configuration can change how the device behaves afterwards. `DeviceFlags.TouchEject` makes the CCID smart card
absent until the user touches the key, affecting every SmartCard session that follows, but it only takes effect
once every capability that does not depend on CCID - `DeviceCapabilities.Otp`, `U2f`, and `Fido2` - is disabled.

## Constraints

Each application picks one transport: the first exposed entry of its default order, or an explicit
`SessionCreationOptions.PreferredConnectionType`. An unusable override throws `ArgumentException`, one the
device does not expose throws `NotSupportedException`.

| Application | Default transport order | Override accepted |
|-------------|-------------------------|-------------------|
| Management | SmartCard, HID FIDO, HID OTP | yes |
| YubiOTP | SmartCard, HID OTP | yes |
| FIDO2 and WebAuthn | HID FIDO, SmartCard | yes |
| OATH | SmartCard only | `SmartCard` only; no alternative transport |
| PIV, OpenPGP, Security Domain, YubiHSM Auth | SmartCard only | no |

- Supplying `ScpKeyParameters` without an explicit preference selects SmartCard, because SCP does not run over
  HID.
- Within this process, a grouped physical YubiKey admits one live connection across its known member
  interfaces, and one session per connection. Ungrouped records cannot provide cross-interface exclusion.
  A second session throws `ConnectionInUseException` until the first is disposed; connection failures propagate
  rather than falling back to another interface.
- Sessions from `device.CreateManagementSessionAsync()` own the connection they opened and close it on
  disposal. `ManagementSession.CreateAsync(connection, ...)` borrows a connection you opened, and you dispose
  it yourself. Use `await using` for both. Sessions have no finalizer recovery; a built-in connection's
  best-effort native finalizer cleanup is not a lifetime guarantee.
- With a direct factory, `PreferredConnectionType` checks the already-open connection; it does not switch
  transports. A failed direct creation still leaves connection disposal to its creator.
- `SetDeviceConfigAsync` with `Reboot = true` requests a reboot; it does not automatically dispose managed
  session objects. If the requested reboot occurs, do not reuse existing sessions or connections. Dispose
  them, rediscover the intended key and create fresh scopes. Capability changes can disable applications
  other code depends on. Reset has the Bio MPE scope described above.
- Write/reset firmware checks use the effective session version. An override affects protocol configuration
  and feature checks, not the hardware metadata reported by later reads; initialization still probes the key.
- Initialization attempts device metadata before establishing optional SCP. An SCP session does not mean
  every initialization metadata exchange was protected by SCP.

## Security notes

- The lock codes in `SetDeviceConfigOptions` are borrowed `ReadOnlyMemory<byte>` of exactly 16 bytes. The
  options object is not retained. Keep source buffers valid and unchanged until the awaited operation
  completes, then zero owned secrets with `CryptographicOperations.ZeroMemory` in a `finally`.
- The session zeroes the encoded configuration buffer, lock-code copies included, after transmitting it.
- A lost lock code cannot be read back. Do not assume device reset removes it, even on Bio MPE.
- Never log lock codes.

## Example

The [ManagementTool sample](examples/ManagementTool/README.md) provides menus and read-only `info`/`config`
commands. Its active selector currently lists only SmartCard-capable keys, despite SDK support for HID.

```bash
dotnet run --project src/Management/examples/ManagementTool/ManagementTool.csproj
```

This standalone tool accepts SmartCard devices only, including for its reset menu. It uses the
[shared CLI selection flow](../../docs/usage/device-discovery.md#selection-in-the-cli-examples);
multiple devices require an interactive choice, so a noninteractive invocation returns no selection.
The SDK and unified `yk management` also support HID FIDO and HID OTP. Selection labels are last-known
metadata; info/config output and reset prerequisites still use explicit live Management reads.

## Related

For read-only discovery and metadata validation without configuration changes, run only
`ScopedDiscoveryReadOnlyTests` and `OwnedMonitorReadOnlyTests` as described in the
[discovery hardware checks](../../docs/usage/device-discovery.md#read-only-scoped-discovery-hardware-checks).
It compares public `IYubiKey.CachedDeviceInfo` snapshots with sequential live Management reads across the
supported scopes on every attached authorized serial-bearing USB key. The owned-monitor cases require at
least two attached authorized keys and verify initialized snapshots, initial replay, cancellation,
disposal/recreate, and convenience selection. Live comparisons occur only after monitor disposal.
Other Management integration tests
can mutate configuration; a broad project run is not equivalent to this read-only filter.

- [../Core/README.md](../Core/README.md) - device discovery, connections, and the `Core.Devices` metadata types.
- [../Piv/README.md](../Piv/README.md), [../Oath/README.md](../Oath/README.md), [../Ctap2/README.md](../Ctap2/README.md) - applications this module toggles.
- [user-interaction.md](../../docs/usage/user-interaction.md) and
  [device-discovery.md](../../docs/usage/device-discovery.md).
- [YubiKey configuration reference](https://developers.yubico.com/yubikey-manager/Config_Reference.html).
- [Developer guide](../../docs/DEV-GUIDE.md): building, testing, and contributing.
