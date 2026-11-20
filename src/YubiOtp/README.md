# Yubico.YubiKit.YubiOtp

The YubiOTP application owns the two programmable slots on a YubiKey. This package programs those slots with a Yubico
OTP, static password, OATH-HOTP, or HMAC-SHA1 challenge-response configuration, reads slot state, runs
challenge-response, and sets the NFC NDEF record. Stored TOTP and HOTP credentials belong to [Oath](../Oath/README.md).

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.

## Requirements

- .NET 10. On Linux, install PC/SC and the udev rules described in [docs/linux-setup.md](../../docs/linux-setup.md).
- YubiKey firmware 2.0 or later. The session enforces no minimum; operations and status fields are gated:

  | Feature | SDK-enforced minimum firmware |
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

These checks do not validate every flag, password length, hardware capability, or command/transport combination.
For example, the legacy *YubiKey Manual 3.3* documents static-password limits of 16 characters on 2.1 and
38 on 2.2, and HOTP counter preset support from 2.2; the configuration-type checks do not enforce those distinctions.
An NFC operation also needs an NFC-capable key and a suitable programmed slot. Session support for SmartCard
does not establish challenge-response support over every device's USB CCID or NFC interface. The challenge
examples below explicitly select OTP HID; this is the supported USB recipe, not a claim of transport parity.

Session creation discovers firmware and slot status before constructing its operational backend. SmartCard
programming-sequence validation starts from the value returned by OTP selection, including when SCP is enabled.
On OTP HID, Core reads a first report for protocol firmware, performs the NEO refresh when needed, then
reads a second report for cached status. SmartCard probes Management on both USB CCID and NFC, then
selects OTP; when OTP's major version is three it uses the higher Management/OTP version. A caller override
is applied afterward to session policy. Cached `ConfigState` still uses raw OTP status, so its gates and
the SmartCard touch heuristic can differ from effective session feature checks. Major-zero acceptance
is optimistic SDK policy, not proof of device/command support.

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
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid.Keyboard;
using Yubico.YubiKit.YubiOtp;

IYubiKey device = await YubiKeyManager.FindFirstAsync();
{
    await using var session = await device.CreateYubiOtpSessionAsync();
    ConfigState state = session.GetConfigState();
    Console.WriteLine($"OTP applet firmware {state.FirmwareVersion}");
}
```

The session above is closed before another example opens one. Later blocks are alternative task fragments:
each needs these directives and an `IYubiKey device` obtained the same way, with no other connection open.
Programming examples require a slot you are authorized to overwrite; they cannot restore its unreadable secrets.
`state.IsConfigured(Slot.One)` throws below firmware 2.1.0; the one-shot `device.GetConfigStateAsync()`
extension opens and disposes its own session.

`GetConfigState()` reads a cached snapshot from session creation or the last successful write, not fresh device
I/O. Its firmware is the raw OTP-status version and can differ from the session's effective version. Trigger
bits describe keyboard activation; they do not reveal the slot mode, `RequireTouch`, short-challenge mode,
access code, or secret material. Keep your intended configuration in your application.

## Common operations

Choose a configuration for the output you need:

| Configuration | Purpose and input |
|---|---|
| `YubiOtpSlotConfiguration` | Keyboard Yubico OTP: raw public-ID bytes (up to 16), 6-byte private ID and 16-byte AES key. The key emits two modhex characters per public-ID byte; do not pass modhex text bytes. Your service verifies the OTP. |
| `HotpSlotConfiguration` | Keyboard HOTP: exact 20-byte key; synchronize the verifier's counter. Use the default `imf: 0`: nonzero IMF encoding is currently incorrect (see Constraints). |
| `StaticPasswordSlotConfiguration` | A string plus keyboard layout, or already translated raw scan codes; not UTF-8 bytes. |
| `StaticTicketSlotConfiguration` | A fixed Yubico-format ticket, not a changing one-time password. |
| `HmacSha1SlotConfiguration` | Host-supplied challenge, 20-byte HMAC-SHA1 result. |
| `YubicoOtpChallengeResponseSlotConfiguration` | Host-supplied 6-byte challenge, 16-byte AES-encrypted Yubico ticket. |

### Replace, update, and protect a slot

`PutConfigurationAsync` replaces a slot's configuration, including its key material. Use a mode-specific
configuration for replacement. `UpdateConfigurationAsync` preserves key material but sends **complete values
within the update masks**, not a patch of just the setters you called. Set every allowed flag you want to
retain; the SDK cannot read back the old values. An update starts with `SerialApiVisible` and `AllowUpdate`
enabled, and all ticket/configuration flags clear. Keyboard-mode replacement configurations additionally
default to `AppendCr` and `FastTrigger`; HMAC additionally defaults to short-challenge mode, not touch.

The two access-code arguments have different jobs. `currentAccessCode` authenticates a write to an already
protected slot. `accessCode` sets the new six-byte code. Pass the same code in both to retain protection;
omitting the new code sends zeros and removes protection where the firmware permits. Access codes protect
writes, not challenge calculations, and do not encrypt the transport. Validate both code lengths before
calling: the current implementation can leave internal secret copies uncleared on invalid-code preflight failures.

This update fragment assumes `byte[] accessCode` contains the slot's current six-byte code, owned by this
task, and a keyboard slot previously programmed with `AllowUpdate`. It deliberately chooses all retained
masked flags rather than preserving unknown settings:

```csharp
try
{
    await using var session = await device.CreateYubiOtpSessionAsync();
    using var update = new UpdateConfiguration();
    update.AppendCr();
    update.FastTrigger();
    update.PacingChar10();
    await session.UpdateConfigurationAsync(
        Slot.Two, update, accessCode: accessCode, currentAccessCode: accessCode);
}
finally
{
    CryptographicOperations.ZeroMemory(accessCode);
}
```

Clearing `AllowUpdate` can prevent later updates. Legacy Manual 3.3 also describes dormant activation as
one-way within updates: once cleared, setting it again requires replacement. Tagged vendor tools reject
access-code changes on firmware 4.3.2–4.3.5; this SDK does not enforce that restriction. Check applicability
to your firmware rather than treating legacy flag rules as universal 5.x behavior.

### Program a slot for HMAC-SHA1 challenge-response

The key must be exactly 20 bytes. Short-challenge mode is enabled by default;
`UseShortChallenge(false)` selects fixed 64-byte mode.

This application-dependent provisioning fragment generates a new key. Securely enroll that same key with
your verifier before it is cleared; a random key known only to this process is not a deployed verifier.

```csharp
byte[] hmacKey = RandomNumberGenerator.GetBytes(20);
try
{
    // Securely enroll hmacKey with your verifier here as part of your application's provisioning.
    await using var session = await device.CreateYubiOtpSessionAsync(
        new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp });
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

The response is 20 bytes. This fragment needs a slot already provisioned for HMAC-SHA1 with the verifier's
key and short-challenge convention. Touch the key if that slot requires it; use the prompt recipe below
to display a notification. This one-shot extension opens and disposes its own session.

```csharp
byte[] challenge = Encoding.UTF8.GetBytes("some-challenge");
ReadOnlyMemory<byte> response = default;
try
{
    response = await device.CalculateHmacSha1Async(
        Slot.Two, challenge,
        new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp });
    // Consume the response with your verifier here; do not log it.
}
finally
{
    CryptographicOperations.ZeroMemory(challenge);
    CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(response).Span);
}
```

The SDK always sends 64 bytes: shorter inputs are padded with `0x01` after a trailing
`0x00`, otherwise with `0x00` (including empty input). Exactly 64 bytes are sent unchanged.

- **Short-challenge slot (default):** use 1–63 bytes to hash the input as given. The device
  treats the final transmitted byte as a marker and removes the trailing run of that
  value, retaining at least one byte. A 64-byte input loses at least its last byte;
  empty input hashes as one `0x00`, not as an empty message.
- **Fixed-challenge slot:** pass exactly 64 bytes, padded according to your verifier's
  convention; the device hashes all 64 bytes as sent.

The session does not know or enforce the slot mode. Use one challenge-length convention
per slot and agree with your verifier: `[0x41]` and `[0x41, 0x00 × 63]` send the same
request, as do `[0x41, 0x00]` and `[0x41, 0x00, 0x01 × 62]`. The device cannot distinguish
these input lengths, in either mode.

The minimum-one-byte interpretation above is the behavior expected by the supplied known-answer vectors;
confirm the empty and 64-byte boundaries on your target firmware before relying on them. Request-encoding
tests alone do not establish device hashing. Prefer 1–63 bytes in short mode, avoiding the ambiguous boundaries.

### Calculate a Yubico OTP AES ticket

This alternative fragment replaces slot 2 and calculates a ticket using OTP HID. Provision the same AES key
securely to your verifier before clearing it; a random key known only to this process is not a deployed verifier.

```csharp
byte[] aesKey = [];
byte[] challenge = [];
ReadOnlyMemory<byte> ticket = default;
try
{
    aesKey = RandomNumberGenerator.GetBytes(16);
    challenge = RandomNumberGenerator.GetBytes(6);
    // Securely provision aesKey to the verifier here as part of your application's enrollment.
    await using var session = await device.CreateYubiOtpSessionAsync(
        new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp });
    using var config = new YubicoOtpChallengeResponseSlotConfiguration(aesKey);
    await session.PutConfigurationAsync(Slot.Two, config);
    ticket = await session.CalculateYubicoOtpAsync(Slot.Two, challenge);
    // Send the ticket to your verifier through your application's secure channel.
}
finally
{
    CryptographicOperations.ZeroMemory(aesKey);
    CryptographicOperations.ZeroMemory(challenge);
    CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(ticket).Span);
}
```

The device encrypts a Yubico ticket with changing counter/random fields, not just the six challenge bytes.
Repeated challenges can therefore return different tickets. In the legacy protocol the private ID is XORed
with the challenge; this configuration leaves the private ID zero, so a verifier expects the first six
decrypted bytes to equal the challenge. The verifier must also validate the ticket CRC and its freshness/
replay policy. The SDK returns bytes; it does not decrypt, verify, format an OTP, or enforce replay protection.

### Program a static password

The YubiKey emits HID scan codes interpreted by the host's active layout. Select that layout;
`KeyboardLayout.ModHex` uses characters shared by supported layouts, not a universal layout guarantee.
Verify actual output under the intended host layout and input state.

```csharp
{
    await using var session = await device.CreateYubiOtpSessionAsync();
    using var config = new StaticPasswordSlotConfiguration("Correct-Horse-1", KeyboardLayout.en_US);
    config.AppendCr();
    await session.PutConfigurationAsync(Slot.Two, config);
}
```

The string overload is convenient but cannot erase the immutable password string. For deterministic cleanup
of your source, supply a mutable buffer of **translated scan codes** to the raw overload and clear it yourself.
The configuration copies either input and clears its own fields on disposal.

### Swap or erase slot configurations

Both are destructive: `SwapSlotsAsync` exchanges slot 1 and slot 2, and `DeleteSlotAsync` zeroes a slot.

```csharp
{
    await using var session = await device.CreateYubiOtpSessionAsync();
    await session.SwapSlotsAsync();
    await session.DeleteSlotAsync(Slot.Two);
}
```

These calls assume unprotected slots. Supply `currentAccessCode` for a protected deletion. Swap has no
access-code parameter; the device enforces permissions (the legacy vendor contract requires both slots
to allow updates). `ProtectSlot2` is a separate protection flag, not an access code.

### Configure NFC output

Call `SetNdefConfigurationAsync` for an existing compatible slot on an NFC-capable key. URI mode chooses
the longest case-sensitive ordinal prefix in the NFC URI table and allows **53 UTF-8 bytes after that
prefix**. Text mode allows **51 UTF-8 text bytes**, with a fixed English (`en`) UTF-8 header. These are byte
limits, not character counts. The 56-byte programming payload is not a complete on-tap NDEF message;
legacy YubiKey documentation describes a URI prefix followed by a generated OTP.

Pass only `NdefType.Uri` or `NdefType.Text`; the SDK does not validate undefined enum values or URI syntax.
`null` sends an all-zero disable request, while `""` sends an empty URI/text configuration with its header.
There is no NFC read-back API here: verify the resulting on-tap behavior on your target key, including disable.

## User interaction

Challenge-response slots can require physical touch when programmed with `RequireTouch`. Writes,
deletion, swap, NDEF setup, and status reads do not request user-presence notifications. Pass an
`IUserPresencePrompt` to receive challenge-response touch notifications; an absent callback is not proof
that the slot needs no touch.

```csharp
sealed class TouchPrompt : IUserPresencePrompt
{
    public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken) =>
        new(Console.Out.WriteLineAsync($"Touch your YubiKey for {context.Application} {context.Scope}."));
}
```

```csharp
{
    await using var session = await device.CreateYubiOtpSessionAsync(
        new SessionCreationOptions
        {
            PreferredConnectionType = ConnectionType.HidOtp,
            UserPresencePrompt = new TouchPrompt()
        });
    // Calculate a challenge-response on this session using the cleanup pattern above.
}
```

The context carries application `YubiOTP` and the slot name as its scope. Over OTP HID it fires once the device reports
it is waiting. SmartCard emits `PolicyRequires` up front only when cached status marks the slot touch-triggered.
That keyboard-trigger indicator is not a reliable read-back of challenge-response `RequireTouch`; this is
the current notification heuristic, not verified policy certainty. Unknown/non-triggered state stays silent. Cancelling the
operation's token during an OTP HID touch wait requests a dummy-report reset before the
notification resolves as cancelled. The reset is not a rollback of commands already sent; if it
fails, dispose the session and connection rather than reusing them. SmartCard operations already
admitted run to completion even if the caller cancels their token.

OTP HID's local checks use a one-second write-ready window with a 5 ms busy delay, a three-second
readiness window without an explicit busy delay, and 250 ms touch polling with a local 14-second
threshold. These checks occur between awaited calls; a held native GET/SET can exceed them. Only the
classified OTP HID touch-wait timeout resolves as `TimedOut`; other timeouts are failures.

## Constraints

- Within this process, grouped views admit one live connection across known member IDs, and each connection
  admits one session. Disjoint scoped views remain uncoordinated. Whoever opens a connection disposes it;
  see [connection ownership](../../docs/architecture/connection-ownership-and-contention.md).
- A session from `device.CreateYubiOtpSessionAsync(...)` owns the connection it opened;
  `YubiOtpSession.CreateAsync(connection, ...)` borrows yours. Use `await using` for both.
- Override the transport with `new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp }`; one
  transport is selected and a connection failure propagates instead of falling back.
- Await the entire public task before another operation or disposal, keeping configurations, borrowed
  memory and prompt resources alive. Write/status fallback, CRC validation and notification callbacks
  extend beyond Core's individual guarded exchanges.
- `PutConfigurationAsync`, `UpdateConfigurationAsync`, `SwapSlotsAsync`, `DeleteSlotAsync`, `SetScanMapAsync`, and
  `SetNdefConfigurationAsync` overwrite device state irreversibly. Access codes are exactly 6 bytes.
- Firmware-gated operations throw `NotSupportedException`; `ConfigState` accessors throw `InvalidOperationException`.
- Nonzero HOTP `imf` is currently encoded with a divisor of 65,536 instead of the vendor protocol's 16.
  Do not use it to provision a desired nonzero initial counter. Keep `imf: 0` and synchronize your verifier.
- `SetScanMapAsync` currently requires 38 bytes, whereas the tagged vendor personalization tool specifies
  a 45-entry map for firmware 3+. This is an implementation limitation, not a supported full-map recipe;
  do not truncate a vendor map to make it fit.
- Keyboard pacing flags select 10 ms and 20 ms (30 ms combined). Mode-specific aliases share flag bits;
  the update mask's pacing bits do not grant permission to change challenge touch/short mode. Legacy
  delay, fast-trigger, numeric-keypad, and serial-visibility flags have firmware-specific applicability.
  Serial visibility merges across slots: clearing one slot's flag need not hide the device serial globally.
- SmartCard write sequence checks are bypassed for effective firmware 5.0.0 ≤ version < 5.5.0 and accept
  zero after any prior sequence. A short fallback write status can currently escape rejection; success
  alone is not proof that malformed status was validated. OTP HID data reads check CRC over the expected
  response prefix; its write-status check is a minimum-length check, not that data-response CRC check.
  CRC detects transmission errors, not cryptographic authenticity.

## Security notes

- Configurations copy key, identity, and scan-code inputs into private fields. Dispose them with `using`;
  disposal clears those fields, not your original buffers or independent arrays returned by `GetConfig`.
- You own your source arrays and each `GetConfig` result. Clear them in `finally`. Strings cannot be
  deterministically erased; choose the raw scan-code overload when mutable-source cleanup is required.
- Session memory arguments are borrowed: keep them stable until the awaited operation finishes, including
  failure. HMAC clears its padded copy; AES sends the borrowed six-byte challenge without padding.
- Successful calculation output belongs to you even though it is exposed as `ReadOnlyMemory<byte>`.
  The examples use `MemoryMarshal.AsMemory` only to clear these documented, caller-owned SDK outputs.
  Do not use that technique on arbitrary borrowed read-only memory. Consume first, then clear in `finally`.
  The one-shot HMAC helper transfers only after hidden-session teardown succeeds. If teardown throws
  after calculation, its task faults without exposing or clearing that result. An explicit session lets
  you retain the calculation result before teardown and clear it in an enclosing `finally`; this does
  not erase separate incoming-report, APDU, decrypted-response or transport staging copies.
- Normal write cleanup clears assembled configuration/payload copies, but invalid access-code preflight
  currently has cleanup gaps. No-I/O validation does not imply erasure of every temporary allocation.
  None of these measures promises erasure of all heap copies. Never log keys, passwords, codes, or responses.

## Example

The interactive OtpTool sample lives at src/YubiOtp/examples/OtpTool/. It demonstrates commands, not
drop-in `ykman` compatibility. Use the SDK recipes above for production-sensitive provisioning:

- The static command currently passes UTF-8 bytes to the raw scan-code constructor and ignores its layout
  option. It does not correctly translate text; use `StaticPasswordSlotConfiguration(string, KeyboardLayout)`.
- Calculate requires an explicit hex challenge despite the help's stdin suggestion. `--totp` does not
  compute a time counter, period, truncation, or code; use your application's agreed HMAC convention instead.
- Settings sends full masked values, not a patch, and program/settings commands omit a new access code.
  A supplied current code authenticates the write but does not retain protection. Swap ignores the CLI code option.
- Sessions use the SmartCard-first default, with no transport selector or presence prompt. A calculation
  failure does not establish that the same key cannot calculate over explicitly selected OTP HID.
- JSON mode bypasses confirmation even without force. Generated secrets/passwords are intentionally
  displayed in human mode; JSON output is not equivalent and omits generated secret material. Calculate
  prints the response. Parser errors can echo input and immutable CLI strings cannot be zeroed: protect
  terminal history, logs and captured output, and do not treat the sample as a hardened secret-input tool.

```bash
dotnet run --project src/YubiOtp/examples/OtpTool/OtpTool.csproj
```

## Related

Use Management's `GetDeviceInfoAsync` for device metadata and `SetDeviceConfigAsync` for modern TLV
configuration (effective 5.0.0 write gate). These are not a typed OTP FIPS-mode query or the legacy
mode/timeout/eject mutator: v2 exposes no typed `QueryFipsMode`, `SetLegacyDeviceConfig` or `SetMode` API.
FIPS metadata/approval fields do not answer that legacy operating-mode question. Raw command access is
an expert tier, not a relocated typed implementation; see [raw access tiers](../../docs/architecture/raw-access-tiers.md).

- [../Core/README.md](../Core/README.md) - device discovery, connections, and session plumbing.
- [../Oath/README.md](../Oath/README.md) - TOTP and HOTP credential storage.
- [../Management/README.md](../Management/README.md) - enabling the OTP application per transport.
- [../../docs/usage/user-interaction.md](../../docs/usage/user-interaction.md) - user-presence notifications.
- [../../docs/usage/device-discovery.md](../../docs/usage/device-discovery.md) - finding and watching YubiKeys.
- [Yubico OTP documentation](https://developers.yubico.com/OTP/) - protocol and slot reference.
- [The YubiKey Manual 3.3 (2014)](https://downloads.yubico.com/support/yubico_YubiKey-Technical-Manual-v3.3_2014.pdf) - legacy device behavior, especially §§5.5–5.7; not universal 5.x rules.
- [Personalization v1.20.0 manual](https://github.com/Yubico/yubikey-personalization/blob/v1.20.0/ykpersonalize.1.adoc) - tagged vendor flag, IMF and 45-entry scan-map reference.
- [Manager 5.8.0 YubiOTP implementation](https://github.com/Yubico/yubikey-manager/blob/5.8.0/yubikit/yubiotp.py) - tagged padding, status and access-code-update behavior; not a protocol standard.
- [Developer guide](../../docs/DEV-GUIDE.md): building, testing, and contributing.
