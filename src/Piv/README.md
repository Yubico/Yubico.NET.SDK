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

Reading the PIN attempt counter needs no PIN, touch, or management key, and works on every supported
firmware. Later snippets assume these directives and a `device` obtained the same way.

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
var keyOptions = new PivKeyCreationOptions { PinPolicy = PivPinPolicy.Once, TouchPolicy = PivTouchPolicy.Never };
IPublicKey publicKey = await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256, keyOptions);
```

`IsManagementKeyAuthenticated` reports PIV authentication, distinct from the inherited `IsAuthenticated` (SCP).

### Store and read a certificate

Storing needs management-key authentication, so continue on the `session` from the previous snippet.

```csharp
using var certificate = X509CertificateLoader.LoadCertificateFromFile("authentication.cer");
await session.StoreCertificateAsync(PivSlot.Authentication, certificate, PivCertificateCompression.Automatic);
using X509Certificate2? stored = await session.GetCertificateAsync(PivSlot.Authentication);
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
ReadOnlyMemory<byte> signature = await session.SignOrDecryptAsync(PivSlot.Authentication, digest);
```

ECDSA signs the digest you pass, so hash first. This overload reads slot metadata to pick the algorithm and
needs firmware 5.3.0; older keys need the overload that takes an explicit `PivAlgorithm`.

## User interaction

- Verify the PIN before private-key operations in slots created with `PivPinPolicy.Once` or `Always`.
- Authenticate the management key before key or certificate writes, retry-limit changes, and key move or delete.
- Touch follows the slot's `PivTouchPolicy`. `VerifyUvAsync` does fingerprint verification on biometric keys.

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
response is validated. `PivTouchPolicy.Always` maps to `UserPresenceBasis.PolicyRequires`; `Cached` and
unreadable slot metadata map to `PolicyMayRequire`, because the touch cache is not observable. `Never`,
`Default`, and an empty slot are silent. Pass a `CancellationToken` to abandon a wait.

## Constraints

- Requesting a non-SmartCard transport through `SessionCreationOptions { PreferredConnectionType = ... }` throws.
- One live connection per physical YubiKey, and one session per connection. Whoever creates a connection
  disposes it with `await using`; a session from `CreatePivSessionAsync` owns and disposes the one it opened.
- `ResetAsync`, `DeleteKeyAsync`, `DeleteCertificateAsync`, `MoveKeyAsync`, `SetManagementKeyAsync`,
  `SetPinAttemptsAsync`, and `SetPinOnlyModeAsync` change persistent state. `ResetAsync` destroys every key.
- Firmware gates throw `NotSupportedException`; `PivFeatures.SupportsRsaGeneration` screens ROCA-affected 4.2.6-4.3.4.

## Security notes

- PINs, PUKs, management keys, and temporary PINs cross the API as `ReadOnlyMemory<byte>`. The session never
  zeroes a buffer you passed in; zero it yourself with `CryptographicOperations.ZeroMemory` in a `finally`.
- Never log PINs, PUKs, management keys, plaintexts, or signatures. Log metadata only.
- Factory defaults, including `PivSession.DefaultManagementKey`, are public; use them only after a reset.

## Example

The interactive PivTool sample lives at `src/Piv/examples/PivTool/`.

```bash
dotnet run --project src/Piv/examples/PivTool/PivTool.csproj
```

## Related

- [Core](../Core/README.md) - device discovery, SmartCard protocol, TLV, and cryptography primitives.
- [Security Domain](../SecurityDomain/README.md) - SCP03 and SCP11 keys that protect the PIV channel.
- [User interaction](../../docs/usage/user-interaction.md) and [device discovery](../../docs/usage/device-discovery.md) - cross-module guides.
- [NIST SP 800-73](https://csrc.nist.gov/pubs/sp/800/73/4/final) - the PIV specification.
- [Developer guide](../../docs/DEV-GUIDE.md): building, testing, and contributing.
