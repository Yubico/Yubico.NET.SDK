# Yubico.YubiKit.Management

The Management application is the device-level control surface of a YubiKey. This package reads device
information - serial number, firmware, form factor, supported and enabled capabilities - and writes device
configuration: enabled applications per transport, timeouts, device flags, NFC restriction, and the
configuration lock code. Other modules talk to one application; this one decides which applications exist.

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.

## Requirements

- .NET 10. On Linux, install PC/SC and the udev rules described in [linux-setup.md](../../docs/linux-setup.md).
- Any YubiKey with firmware 4.1.0 or later; writing configuration needs 5.0.0 or later.
- Transports: SmartCard (CCID, USB or NFC), HID FIDO, or HID OTP - the only module that runs over all three.

| Feature | Minimum firmware |
|---------|------------------|
| Read device information | 4.1.0 |
| Write device configuration | 5.0.0 |
| Factory reset (`ResetDeviceAsync`) | 5.6.0 |

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

var devices = await YubiKeyManager.FindAllAsync();
IYubiKey device = devices[0];
await using var session = await device.CreateManagementSessionAsync();

var info = await session.GetDeviceInfoAsync();
Console.WriteLine($"Serial {info.SerialNumber}, firmware {info.FirmwareVersion}, {info.FormFactor}");
Console.WriteLine($"USB enabled: {info.UsbEnabled}");
```

Later snippets assume these directives and a `device` obtained the same way. For a single operation, the
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

`UsbSupported` and `NfcSupported` describe the hardware; `UsbEnabled` and `NfcEnabled` describe what is on now.

### Enable or disable applications

```csharp
var config = DeviceConfig.CreateBuilder()
    .WithCapabilities(Transport.Usb, (int)(DeviceCapabilities.Piv | DeviceCapabilities.Oath))
    .Build();

await using (var session = await device.CreateManagementSessionAsync())
{
    await session.SetDeviceConfigAsync(config, new SetDeviceConfigOptions { Reboot = true });
}

// The key disconnects and re-enumerates. The session above is disposed; rescan before using the device again.
await Task.Delay(TimeSpan.FromSeconds(3));
var rebooted = await YubiKeyManager.FindAllAsync(forceRescan: true);
```

The builder also carries `WithAutoEjectTimeout`, `WithChallengeResponseTimeout`, `WithNfcRestricted`, and
`WithDeviceFlags`, which takes a byte such as `(byte)DeviceFlags.TouchEject`. At least one USB capability must
remain enabled; `Build()` rejects zero.

### Lock the configuration

```csharp
await using var session = await device.CreateManagementSessionAsync();
byte[] lockCode = RandomNumberGenerator.GetBytes(16);
try
{
    // Persist lockCode in your secret store first. It cannot be recovered.
    await session.SetDeviceConfigAsync(
        DeviceConfig.CreateBuilder().Build(),
        new SetDeviceConfigOptions { NewLockCode = lockCode });
}
finally
{
    CryptographicOperations.ZeroMemory(lockCode);
}
```

While locked, every later `SetDeviceConfigAsync` must supply `CurrentLockCode`. Passing `NewLockCode` again
replaces the code; there is no unlock without the current one.

## User interaction

Management operations need no touch, no PIN, and no management key. `SessionCreationOptions.UserPresencePrompt`
is accepted by the options object but never invoked by this module. The only secret it handles is the 16-byte
configuration lock code. Cancel a pending operation with the `cancellationToken` you pass to the call.

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
| PIV, OATH, OpenPGP, Security Domain, YubiHSM Auth | SmartCard only | no |

- Supplying `ScpKeyParameters` without an explicit preference selects SmartCard, because SCP does not run over
  HID.
- A physical YubiKey admits one live connection across all of its interfaces, and one session per connection.
  A second session throws `ConnectionInUseException` until the first is disposed; connection failures propagate
  rather than falling back to another interface.
- Sessions from `device.CreateManagementSessionAsync()` own the connection they opened and close it on
  disposal. `ManagementSession.CreateAsync(connection, ...)` borrows a connection you opened, and you dispose
  it yourself. Use `await using` for both; there is no finalizer backstop.
- `SetDeviceConfigAsync` with `Reboot = true` terminates every session on the device, and capability changes
  can disable applications other code depends on. `ResetDeviceAsync` resets the whole device and cannot be undone.
- Firmware-gated operations throw `NotSupportedException` when the connected key is too old.

## Security notes

- The lock codes in `SetDeviceConfigOptions` are borrowed `ReadOnlyMemory<byte>` of exactly 16 bytes. The
  options object is not retained; zero your buffer with `CryptographicOperations.ZeroMemory` in a `finally`.
- The session zeroes the encoded configuration buffer, lock-code copies included, after transmitting it.
- A lost lock code cannot be recovered, and firmware below 5.6.0 has no device reset to fall back on.
- Never log lock codes.

## Example

The interactive ManagementTool sample lives at src/Management/examples/ManagementTool/.

```bash
dotnet run --project src/Management/examples/ManagementTool/ManagementTool.csproj
```

## Related

- [../Core/README.md](../Core/README.md) - device discovery, connections, and the `Core.Devices` metadata types.
- [../Piv/README.md](../Piv/README.md), [../Oath/README.md](../Oath/README.md), [../Fido2/README.md](../Fido2/README.md) - applications this module toggles.
- [user-interaction.md](../../docs/usage/user-interaction.md) and
  [device-discovery.md](../../docs/usage/device-discovery.md).
- [YubiKey configuration reference](https://developers.yubico.com/yubikey-manager/Config_Reference.html).
- [CLAUDE.md](CLAUDE.md) - contributor guidance, internals, and test infrastructure.
