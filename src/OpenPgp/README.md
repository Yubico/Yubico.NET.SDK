# Yubico.YubiKit.OpenPgp

This package implements the OpenPGP card application (specification 3.4) on the YubiKey. It manages the signature,
decryption, and authentication key slots and their certificates, signs, decrypts, and authenticates on the device, and
administers the PINs, reset code, and touch policy. It is a smart-card applet beside PIV; the two share no credentials.

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.

## Requirements

- .NET 10. Linux additionally needs PC/SC and udev rules; see [Linux setup](../../docs/linux-setup.md).
- SmartCard transport only, over USB CCID or NFC. The applet has no HID interface.
- Basic OpenPGP operation is available from firmware 1.0.0; individual features are gated as follows.

| Feature | Minimum firmware |
|---|---|
| Factory reset | 1.0.6 |
| Touch policy (UIF) | 4.2.0 |
| Elliptic-curve keys | 5.2.0 |
| Per-slot certificates | 5.2.0 |
| Key attestation | 5.2.0 |
| Supported-algorithm query | 5.2.0 |
| PIN unverify | 5.6.0 |

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.OpenPgp --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

`GetSupportedAlgorithmsAsync` returns an ordered `IReadOnlyList<SupportedAlgorithm>`. Read each entry's
`KeyRef` and `Attributes`; do not collapse the list into a dictionary because one slot can report multiple
supported algorithms.

## Getting started

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.OpenPgp;

IYubiKey device = await YubiKeyManager.FindFirstAsync();
await using var session = await device.CreateOpenPgpSessionAsync();
ApplicationRelatedData appData = await session.GetApplicationRelatedDataAsync();
Console.WriteLine($"OpenPGP card {appData.Aid.Version.Major}.{appData.Aid.Version.Minor}, serial {appData.Aid.Serial}");
```

Later snippets assume these directives and a `device` obtained the same way. `GetApplicationRelatedDataAsync` needs no
PIN and no touch. There is no one-shot extension: every operation runs on a session you open.

## Common operations

PINs cross the API as UTF-8 `ReadOnlyMemory<byte>`. The factory defaults are `123456` and `12345678`; change both before the key is used for anything real.

### Inspect key metadata

```csharp
KeyInformation keyInformation = await session.ListKeyInformationAsync();
KeyFingerprints keyFingerprints = await session.GetKeyFingerprintsAsync();

if (keyFingerprints.TryGetValue(KeyRef.Sig, out ReadOnlyMemory<byte> fingerprint))
{
    Console.WriteLine(Convert.ToHexString(fingerprint.Span));
}
```

`KeyInformation`, `KeyFingerprints`, and `GenerationTimes` are named read-only dictionaries: callers can index,
look up, and enumerate entries but cannot mutate collection storage. `ApplicationRelatedData.Discretionary` exposes
the same values through `KeyInformation`, `KeyFingerprints`, and `CaKeyFingerprints`.

### Verify the user PIN

`extended: false` authorizes signing and `extended: true` authorizes decryption and internal authentication. They are
separate states on the card, so a session that does both verifies twice.

```csharp
await using var session = await device.CreateOpenPgpSessionAsync();
byte[] pin = Encoding.UTF8.GetBytes("123456");
try
{
    await session.VerifyPinAsync(pin, extended: false);
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
}
```

A rejected PIN throws `OpenPgpInvalidPinException`, whose `RetriesRemaining` reports the attempts left, or `0` when the
PIN is now blocked. `ChangePinAsync` and `ChangeAdminAsync` take a current and a new PIN under the same borrow-and-zero
contract; `ResetPinUsingResetCodeAsync` and `ResetPinUsingAdminAuthenticationAsync` recover a blocked user PIN.

### Generate a key

Key generation needs admin authentication first. Use `RsaAttributes.Create` for RSA and `EcAttributes.Create` for
elliptic curves, where `CurveOid.Secp256R1` is P-256.

```csharp
await using var session = await device.CreateOpenPgpSessionAsync();
byte[] adminPin = Encoding.UTF8.GetBytes("12345678");
try
{
    await session.VerifyAdminAsync(adminPin);
    await session.GenerateKeyAsync(KeyRef.Sig, RsaAttributes.Create(RsaSize.Rsa2048));
    await session.GenerateKeyAsync(KeyRef.Dec, EcAttributes.Create(KeyRef.Dec, CurveOid.Secp256R1));
}
finally
{
    CryptographicOperations.ZeroMemory(adminPin);
}
```

### Sign and decrypt

`SignAsync` takes the message, not a digest: it hashes with the named algorithm and, for RSA, wraps the hash in a
PKCS#1 v1.5 `DigestInfo` before the card signs. ECDSA signatures come back DER-encoded.

```csharp
await using var session = await device.CreateOpenPgpSessionAsync();
byte[] pin = Encoding.UTF8.GetBytes("123456");
try
{
    await session.VerifyPinAsync(pin, extended: false);
    ReadOnlyMemory<byte> message = Encoding.UTF8.GetBytes("hello");
    ReadOnlyMemory<byte> signature = await session.SignAsync(message, HashAlgorithmName.SHA256);

    await session.VerifyPinAsync(pin, extended: true);
    ReadOnlyMemory<byte> ciphertext = await File.ReadAllBytesAsync("message.gpg");
    ReadOnlyMemory<byte> plaintext = await session.DecryptAsync(ciphertext);
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
}
```

For an RSA key, `ciphertext` is the raw encrypted block; for an EC key, it is the sender's ephemeral public key and the
result is the ECDH shared secret.

## User interaction

This module never asks the application for a credential; pass PINs to the methods that need them. Touch is a card
policy, set per slot with `SetUifAsync` (admin PIN, firmware 4.2.0 or later) and reported to your
`IUserPresencePrompt` implementation:

```csharp
sealed class TouchPrompt : IUserPresencePrompt
{
    public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken) =>
        new(Console.Out.WriteLineAsync($"Touch your YubiKey for {context.Application} {context.Scope}."));
}
```

```csharp
await using var session = await device.CreateOpenPgpSessionAsync(
    new SessionCreationOptions { UserPresencePrompt = new TouchPrompt() });
```

Signing, decryption, internal authentication, and attestation notify before their APDU, with the `KeyRef` name as scope; attestation reports `KeyRef.Att` because the attestation key performs that operation. `Uif.On` and `Uif.Fixed` report `PolicyRequires`, `Uif.Cached` and `Uif.CachedFixed` report `PolicyMayRequire`, and `Uif.Off` is silent, as is firmware that cannot configure UIF at all. A UIF read that fails for any reason other than cancellation degrades to `PolicyMayRequire` rather than failing the operation. Outcomes are `Completed`, `Cancelled`, or `Failed`; no OpenPGP status word is treated as a touch timeout. Cancel the operation's token to give up on it.

## Constraints

- SmartCard only. Asking for `ConnectionType.HidFido` or `ConnectionType.HidOtp` through `SessionCreationOptions.PreferredConnectionType` throws `ArgumentException`.
- A physical YubiKey admits one live connection and one session over it. A session from `CreateOpenPgpSessionAsync` owns the connection it opened; `OpenPgpSession.CreateAsync` borrows yours and you dispose it. Use `await using` for both.
- `ResetAsync` is destructive: it deliberately blocks both PINs, terminates the applet, and reactivates it, erasing every key, certificate, and setting and restoring the default PINs.
- `GenerateKeyAsync`, `PutKeyAsync`, and `DeleteKeyAsync` overwrite slot key material irrecoverably, and `SetAlgorithmAttributesAsync`, `SetKdfAsync`, `SetPinAttemptsAsync`, and `SetUifAsync` change persistent configuration.
- PIN blocking is persistent: once the user PIN is exhausted, only the reset code or an admin-authenticated reset brings it back. Firmware-gated operations throw `NotSupportedException` before any APDU is sent.

## Security notes

- PINs, reset codes, and imported key material are borrowed `ReadOnlyMemory<byte>`; zero your buffers with `CryptographicOperations.ZeroMemory` in a `finally`.
- The SDK zeroes the APDU payloads it builds, the KDF-derived bytes it computes, and the raw card responses behind signatures and plaintexts. The values returned to you are yours to clear.
- Treat the default PINs as public knowledge. Change both before provisioning keys.
- Never log PINs, reset codes, plaintexts, or key material; slot names, algorithm identifiers, and lengths are safe.

## Example

The interactive OpenPgpTool sample lives at `src/OpenPgp/examples/OpenPgpTool/`.

```bash
dotnet run --project src/OpenPgp/examples/OpenPgpTool/OpenPgpTool.csproj
```

## Related

- [../Core/README.md](../Core/README.md) - device discovery, smart-card connections, APDU, and TLV.
- [../Piv/README.md](../Piv/README.md) - the other smart-card key applet, with its own PIN and slots.
- [../Management/README.md](../Management/README.md) - enabling or disabling the OpenPGP application.
- [user-interaction.md](../../docs/usage/user-interaction.md) and [device-discovery.md](../../docs/usage/device-discovery.md).
- The [OpenPGP smart card application 3.4](https://gnupg.org/ftp/specs/OpenPGP-smart-card-application-3.4.pdf) specification.
- [Developer guide](../../docs/DEV-GUIDE.md): building, testing, and contributing.
