# Yubico.YubiKit.Piv

The PIV application (NIST SP 800-73) turns a YubiKey into a smart card holding private keys and X.509 certificates
in numbered slots. This package covers PIN, PUK, and management-key authentication, key and certificate management,
signing, decryption, ECDH key agreement, and attestation, on top of `Yubico.YubiKit.Core` and no other module.

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.

## Requirements

- .NET 10 on Windows, macOS, or Linux; Linux also needs PC/SC and udev rules ([Linux setup](../../docs/linux-setup.md)).
- A YubiKey 4 or 5 series device with PIV enabled. SmartCard transport only (USB CCID or NFC), never HID.

| Feature | Minimum firmware |
| --- | --- |
| P-384 curve, PIN and touch policies | 4.0.0 |
| Cached touch policy, key attestation | 4.3.0 |
| Metadata (PIN, PUK, management key, slot) | 5.3.0 |
| AES management key | 5.4.0 |
| Move and delete key, Curve25519, RSA 3072 and 4096 | 5.7.0 |

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Piv --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Getting started

```csharp
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Piv;

IYubiKey device = await YubiKeyManager.FindFirstAsync();
await using var session = await device.CreatePivSessionAsync();

int pinAttempts = await session.GetPinAttemptsAsync();
Console.WriteLine($"PIN attempts remaining: {pinAttempts}");
```

Reading the PIN attempt counter needs no PIN, touch, or management key. On firmware without metadata,
it uses a no-data VERIFY query (which does not consume retries). If the PIN is already verified and
the device reports success without a count, it throws `InvalidOperationException` rather than guessing.
Later snippets assume these directives and a `device` obtained the same way.

## Common operations

### Authenticate and generate a key pair

```csharp
await using var session = await device.CreatePivSessionAsync();

byte[] managementKey = Convert.FromHexString("010203040506070801020304050607080102030405060708");
try
{
    await session.AuthenticateAsync(managementKey);
}
finally
{
    CryptographicOperations.ZeroMemory(managementKey);
}
var keyOptions = new KeyCreationOptions { PinPolicy = PinPolicy.Once, TouchPolicy = TouchPolicy.Never };
IPublicKey publicKey = await session.GenerateKeyAsync(Slot.Authentication, KeyType.P256, keyOptions);
```

`IsManagementKeyAuthenticated` reports PIV authentication, distinct from the inherited `IsAuthenticated` (SCP).

### Store and read a certificate

Storing needs management-key authentication, so continue on the `session` from the previous snippet.

```csharp
using var certificate = X509CertificateLoader.LoadCertificateFromFile("authentication.cer");
await session.StoreCertificateAsync(Slot.Authentication, certificate, CertificateCompression.Automatic);
using X509Certificate2? stored = await session.GetCertificateAsync(Slot.Authentication);
```

### Verify the PIN and sign

```csharp
await using var session = await device.CreatePivSessionAsync();

byte[] pin = Encoding.UTF8.GetBytes("123456");
try
{
    await session.VerifyPinAsync(pin);
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
}
byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes("message to sign"));
Memory<byte> signature = await session.SignOrDecryptAsync(Slot.Authentication, digest);
```

ECDSA signs the digest you pass, so hash first. This overload reads slot metadata to pick the algorithm and
needs firmware 5.3.0; older keys need the overload that takes an explicit `KeyType`.

## User interaction

- Verify the PIN before private-key operations in slots created with `PinPolicy.Once` or `Always`.
- Authenticate the management key before key or certificate writes, retry-limit changes, and key move or delete.
  Retry-limit changes (`SetPinAttemptsAsync`) also need a verified PIN.
- Touch follows the slot's `TouchPolicy`. `VerifyUvAsync` does fingerprint verification on biometric keys.

Supply an `IUserPresencePrompt` to learn when an operation needs a touch.

```csharp
sealed class TouchPrompt : IUserPresencePrompt
{
    public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken) =>
        new(Console.Out.WriteLineAsync($"Touch your YubiKey for {context.Application} {context.Scope}."));
}
```

```csharp
var options = new SessionCreationOptions { UserPresencePrompt = new TouchPrompt() };
await using var session = await device.CreatePivSessionAsync(options);
```

The prompt receives one request immediately before the private-key command and one resolution after the
response is validated. `TouchPolicy.Always` maps to `UserPresenceBasis.PolicyRequires`; `Cached` and
unreadable slot metadata map to `PolicyMayRequire`, because the touch cache is not observable. `Never`,
`Default`, and an empty slot are silent. Pass a `CancellationToken` to abandon a wait.

## Constraints

Core's [cryptographic catalog](../Core/README.md#cryptographic-metadata) exposes immutable `KeyInfo` on
key objects. Internal `KeyTypeEncoding` owns PIV's independent wire identifiers and strict request subsets;
never cast from Core `KeyType` or infer PIV support from catalog representation/applicability.

- Requesting a non-SmartCard transport through `SessionCreationOptions { PreferredConnectionType = ... }` throws.
- One live connection per physical YubiKey, and one session per connection. Whoever creates a connection
  disposes it with `await using`; a session from `CreatePivSessionAsync` owns and disposes the one it opened.
- `ResetAsync`, `DeleteKeyAsync`, `DeleteCertificateAsync`, `MoveKeyAsync`, `SetManagementKeyAsync`,
  `SetPinAttemptsAsync`, and `SetPinOnlyModeAsync` change persistent state. `ResetAsync` destroys every key.
  `SetPinAttemptsAsync` resets the PIN to `123456` and the PUK to `12345678` every time it runs, even with
  unchanged limits; follow it with `ChangePinAsync` and `ChangePukAsync`.
- Operations enforce firmware gates themselves and throw `NotSupportedException` before transmitting an
  unsupported key or policy request. RSA generation is disabled on ROCA-affected 4.2.6–4.3.4; import remains
  independently available. YubiKey 4 FIPS (4.4.x) rejects RSA1024 and PIN policy `Never`.

## Preview API migration

- `PivSlot` → `Slot` (the numeric key references are unchanged; F9 is Yubico's attestation extension).
- `PivFeatures` → standalone public `Features`. `session.IsSupported(Features.Metadata)` is optional;
  operations enforce their own prerequisites. Queries evaluate known firmware compatibility, not authorization
  or a guarantee that a command will succeed. Compatibility overrides affect these gates, not the physical
  factory-default management-key algorithm used after reset.
- `PivAlgorithm` → `Core.Cryptography.KeyType`. `ImportKeyAsync` returns `Task`: the caller already knows
  `privateKey.KeyType`. Never cast a `KeyType` to a PIV byte. The SDK maps identifiers explicitly.
- PIV-local model/policy prefixes are removed: for example `PinPolicy`, `TouchPolicy`, `PinMetadata`,
  `SlotMetadata`, `KeyCreationOptions`, and `DataObjects.AdminData`. Session entrypoints remain `PivSession`
  and `IPivSession`. CLI key selectors use `--key-type` and key-type result fields are named `KeyType`.
- Management-key operations also use Core `KeyType` (`TripleDes`, `Aes128`, `Aes192`, `Aes256`) and canonical
  key sizes. Symmetric management types cannot be generated/imported as asymmetric slot keys. The session's
  nullable `ManagementKeyType` is null for an unmodeled algorithm; raw management metadata remains inspectable.

PIV reads representation-aware catalog fields: RSA/symmetric stored widths from `KeyInfo.KeySizeBytes`, EC
coordinates from `Curve.CoordinateLengthBytes`, and scalar/seed widths from `PrivateValueLengthBytes`.
Shared Core support for secp256k1 or Brainpool does not add PIV generation, import, or agreement support.
- Slot metadata exposes nullable `KeyType`, `RawAlgorithmId`, and complete `RawData`. Unknown identifiers
  remain readable, while using an unrecognized key algorithm throws `NotSupportedException`.
- `EnsureSupports` is protected session implementation machinery, no longer a public interface member.
  Derived sessions retain it; consumers should call operations directly or use `IsSupported` for UI hints.

The support matrix follows the reviewed [Android PIV source](https://github.com/Yubico/yubikit-android/blob/76b89d35142d9a4937b6b72e5c840e057a280620/piv/src/main/java/com/yubico/yubikit/piv/PivSession.java).
The reviewed [Python implementation](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/piv.py)
agrees on P-384/policies (4.0), cached touch/attestation (4.3), metadata (5.3), AES (5.4), and
move/delete/Curve25519/large RSA (5.7). Its conservative serial (5.0.3) and ROCA (4.2.0) boundaries differ;
this SDK retains the existing Android-aligned serial (5.0.0) and ROCA (4.2.6) boundaries. Major-zero
development firmware retains Core's permissive compatibility handling.

## Security notes

- Crypto, raw data-object, and temporary-PIN results transfer writable `Memory<byte>` to the caller.
  Clear owned plaintext, shared secrets, and temporary PINs with `CryptographicOperations.ZeroMemory(result.Span)`
  after use. This replaces read-only result memory from earlier previews. Internal parsers retain mutable
  owned buffers until transfer; raw APDU responses are cleared through their owner's `Clear()` method.

- PINs, PUKs, management keys, and temporary PINs cross the API as `ReadOnlyMemory<byte>`. The session never
  zeroes a buffer you passed in; zero it yourself with `CryptographicOperations.ZeroMemory` in a `finally`.
- Never log PINs, PUKs, management keys, plaintexts, or signatures. Log metadata only.
- Factory defaults, including `PivSession.DefaultManagementKey`, are public; use them only after a reset.

## Example

The interactive PivTool sample lives at `src/Piv/examples/PivTool/`.

```bash
dotnet run --project src/Piv/examples/PivTool/PivTool.csproj
```

Its menus and argument-driven CLI use the [shared selection flow](../../docs/usage/device-discovery.md#selection-in-the-cli-examples)
with SmartCard-only eligibility. The argument-driven CLI never prompts, even on a terminal; use `--serial`
when multiple devices are connected. Picker labels reuse discovery metadata, not authoritative live configuration.
## Maintainer verification

For maintainers, run unit tests with `dotnet toolchain.cs -- test --project Piv`. On an explicitly allowlisted
dedicated test key, run smoke with `dotnet toolchain.cs -- test --integration --project Piv --smoke`, then full
unattended coverage with `dotnet toolchain.cs -- test --integration --project Piv --filter "Category!=RequiresUserPresence"`.
These tests reset and mutate PIV, including a protected-mode enable/reopen/recover/disable round trip. Touch
and hotplug tests need separate human coordination; multi-key contention tests need two attached keys.

## Related

- [Core](../Core/README.md) - device discovery, SmartCard protocol, TLV, and cryptography primitives.
- [Security Domain](../SecurityDomain/README.md) - SCP03 and SCP11 keys that protect the PIV channel.
- [User interaction](../../docs/usage/user-interaction.md) and [device discovery](../../docs/usage/device-discovery.md) - cross-module guides.
- [NIST SP 800-73](https://csrc.nist.gov/pubs/sp/800/73/4/final) - the PIV specification.
- [Developer guide](../../docs/DEV-GUIDE.md): building, testing, and contributing.
