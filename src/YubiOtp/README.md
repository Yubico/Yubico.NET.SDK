# Yubico.YubiKit.YubiOtp

The YubiOTP application owns the two programmable slots on a YubiKey. This package programs those slots with a Yubico
OTP, static password, OATH-HOTP, or HMAC-SHA1 challenge-response configuration, reads slot state, runs
challenge-response, and sets the NFC NDEF record. Stored TOTP and HOTP credentials belong to [Oath](../Oath/README.md).

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.

## Requirements

- .NET 10. On Linux, install PC/SC and the udev rules described in [docs/linux-setup.md](../../docs/linux-setup.md).
- YubiKey firmware 2.0 or later. The session enforces no minimum; operations and status fields are gated:

  | Feature | Minimum firmware |
  |---------|------------------|
  | `ConfigState.IsConfigured` | 2.1.0 |
  | Serial number read | 2.2.0 |
  | HMAC-SHA1 challenge-response | 2.2.0 |
  | Yubico OTP challenge-response | 2.2.0 |
  | Slot update | 2.3.0 |
  | Slot swap | 2.3.0 |
  | NDEF configuration | 3.0.0 |
  | `ConfigState.IsTouchTriggered` | 3.0.0 |

- Transports: SmartCard (USB CCID and NFC) and OTP HID. `CreateYubiOtpSessionAsync` picks the first supported transport
  in the order SmartCard, then OTP HID; `ScpKeyParameters` without an explicit preference forces SmartCard.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.YubiOtp --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Getting started

```csharp
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid.Keyboard;
using Yubico.YubiKit.YubiOtp;

IReadOnlyList<IYubiKey> devices = await YubiKeyManager.FindAllAsync();
IYubiKey device = devices[0];
await using var session = await device.CreateYubiOtpSessionAsync();
ConfigState state = session.GetConfigState();
Console.WriteLine($"OTP applet firmware {state.FirmwareVersion}");
```

Later snippets assume these directives and a `device` obtained the same way. `state.IsConfigured(Slot.One)` throws
below firmware 2.1.0; the one-shot `device.GetConfigStateAsync()` extension owns its own session.

## Common operations

### Program a slot for HMAC-SHA1 challenge-response

The key must be exactly 20 bytes. `UseShortChallenge` permits challenges below 64 bytes.

```csharp
byte[] hmacKey = RandomNumberGenerator.GetBytes(20);
try
{
    await using var session = await device.CreateYubiOtpSessionAsync();
    using var config = new HmacSha1SlotConfiguration(hmacKey);
    config.RequireTouch();
    config.UseShortChallenge();
    await session.PutConfigurationAsync(Slot.Two, config);
}
finally
{
    CryptographicOperations.ZeroMemory(hmacKey);
}
```

### Calculate an HMAC-SHA1 response

The response is 20 bytes. This one-shot extension opens and disposes its own session.

```csharp
byte[] challenge = Encoding.UTF8.GetBytes("some-challenge");
ReadOnlyMemory<byte> response = await device.CalculateHmacSha1Async(Slot.Two, challenge);
```

### Program a static password

The YubiKey emits HID scan codes and types correctly only under the same layout; `KeyboardLayout.ModHex` is neutral.

```csharp
await using var session = await device.CreateYubiOtpSessionAsync();
using var config = new StaticPasswordSlotConfiguration("Correct-Horse-1", KeyboardLayout.en_US);
config.AppendCr();
await session.PutConfigurationAsync(Slot.Two, config);
```

### Swap or erase slot configurations

Both are destructive: `SwapSlotsAsync` exchanges slot 1 and slot 2, and `DeleteSlotAsync` zeroes a slot.

```csharp
await using var session = await device.CreateYubiOtpSessionAsync();
await session.SwapSlotsAsync();
await session.DeleteSlotAsync(Slot.Two);
```

## User interaction

Only challenge-response can require physical touch, and only when the slot was programmed with `RequireTouch`. Writes,
deletion, swap, NDEF setup, and status reads never wait. Pass your `IUserPresencePrompt` implementation to be told
when the key is waiting.

```csharp
sealed class TouchPrompt : IUserPresencePrompt
{
    public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken) =>
        new(Console.Out.WriteLineAsync($"Touch your YubiKey for {context.Application} {context.Scope}."));
}
```

```csharp
await using var session = await device.CreateYubiOtpSessionAsync(
    new SessionCreationOptions { UserPresencePrompt = new TouchPrompt() });
```

The context carries application `YubiOTP` and the slot name as its scope. Over OTP HID it fires once the device reports
it is waiting; SmartCard fires up front, and only when cached status marks the slot touch-triggered. Cancelling the
operation's token resolves the notification as cancelled.

## Constraints

- A physical YubiKey admits one live connection and one session per connection; a second attempt throws
  `ConnectionInUseException`. Whoever opens a connection disposes it.
- A session from `device.CreateYubiOtpSessionAsync(...)` owns the connection it opened;
  `YubiOtpSession.CreateAsync(connection, ...)` borrows yours. Use `await using` for both.
- Override the transport with `new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp }`; one
  transport is selected and a connection failure propagates instead of falling back.
- `PutConfigurationAsync`, `UpdateConfigurationAsync`, `SwapSlotsAsync`, `DeleteSlotAsync`, `SetScanMapAsync`, and
  `SetNdefConfigurationAsync` overwrite device state irreversibly. Access codes are exactly 6 bytes.
- Firmware-gated operations throw `NotSupportedException`; `ConfigState` accessors throw `InvalidOperationException`.

## Security notes

- Keys, passwords, and access codes are borrowed `ReadOnlySpan<byte>`, `ReadOnlyMemory<byte>`, or `string`. You own the
  input buffers: zero them with `CryptographicOperations.ZeroMemory` in a `finally`.
- `SlotConfiguration` is `IDisposable` and zeroes its assembled key material on disposal. Always `using` it.
- The session zeroes the encoded configuration block, the padded challenge, and the response buffer when an operation
  fails. Never log keys, passwords, access codes, or responses.

## Example

The interactive OtpTool sample lives at src/YubiOtp/examples/OtpTool/.

```bash
dotnet run --project src/YubiOtp/examples/OtpTool/OtpTool.csproj
```

## Related

- [../Core/README.md](../Core/README.md) - device discovery, connections, and session plumbing.
- [../Oath/README.md](../Oath/README.md) - TOTP and HOTP credential storage.
- [../Management/README.md](../Management/README.md) - enabling the OTP application per transport.
- [../../docs/usage/user-interaction.md](../../docs/usage/user-interaction.md) - user-presence notifications.
- [../../docs/usage/device-discovery.md](../../docs/usage/device-discovery.md) - finding and watching YubiKeys.
- [Yubico OTP documentation](https://developers.yubico.com/OTP/) - protocol and slot reference.
- [CLAUDE.md](CLAUDE.md) - contributor guidance, internals, and test infrastructure.
