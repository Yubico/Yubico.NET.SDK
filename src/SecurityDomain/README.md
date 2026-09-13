# Yubico.YubiKit.SecurityDomain

The Security Domain is the GlobalPlatform root security application on a YubiKey. It owns the Secure Channel
Protocol (SCP) keys that every other applet session can use to encrypt and authenticate its APDU traffic. This
package inspects, generates, imports, rotates, and deletes those keys, manages SCP11 certificates and allow
lists, and factory-resets it. Other modules consume the keys through `SessionCreationOptions.ScpKeyParameters`.

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.

## Requirements

- .NET 10 on Windows, macOS, or Linux; Linux also needs PC/SC and udev rules ([Linux setup](../../docs/linux-setup.md)).
- SmartCard transport only, over USB CCID or NFC.

| Feature | Minimum firmware |
| --- | --- |
| Security Domain application, SCP03 | 5.3.0 |
| Key attestation, allow list | 5.7.0 |
| SCP11a, SCP11b, SCP11c | 5.7.2 |

Keys are addressed by a `KeyReference(byte Kid, byte Kvn)`. The KID (key identifier) selects the protocol —
`ScpKid.SCP03` (0x01), `ScpKid.SCP11a` (0x11), `ScpKid.SCP11b` (0x13), `ScpKid.SCP11c` (0x15) — and the KVN
(key version number) distinguishes multiple versions of the same protocol. `KeyReference.Default` is the
factory SCP03 reference, KID 0x01 and KVN 0xFF.

SCP03 is a symmetric channel built on a `StaticKeys` triple (ENC, MAC, DEK). SCP11 is asymmetric and uses
EC P-256 keys: SCP11b needs only the device public key you read out after generation, while SCP11a and SCP11c
additionally require an Off-Card Entity key pair and certificate chain, and can be restricted with a
certificate serial-number allow list.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.SecurityDomain --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Getting started

```csharp
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.SecurityDomain;

var devices = await YubiKeyManager.FindAllAsync();
IYubiKey device = devices[0];
await using var session = await device.CreateSecurityDomainSessionAsync();

foreach (var keyInfo in await session.GetKeyInfoAsync())
{
    Console.WriteLine(keyInfo.KeyReference);
}
```

Reading key information needs no secure channel, so this works on an untouched device. For a single read,
`device.GetSecurityDomainKeyInfoAsync()` opens and disposes the session for you. Later snippets assume these
directives and a `device` obtained the same way.

## Common operations

### Authenticate with the default SCP03 keys

```csharp
using var scpKeyParameters = Scp03KeyParameters.Default;
await using var session = await device.CreateSecurityDomainSessionAsync(
    new SessionCreationOptions { ScpKeyParameters = scpKeyParameters });

IReadOnlyList<KeyInfo> keys = await session.GetKeyInfoAsync();
```

`Scp03KeyParameters.Default` wraps the publicly documented factory key set. Every operation that writes key
material requires an authenticated channel, so the next two snippets continue on this `session`.

### Rotate the SCP03 keys

```csharp
byte[] keyMaterial = RandomNumberGenerator.GetBytes(48);
try
{
    // Escrow keyMaterial before you send it: the device will not give it back.
    using var newKeys = new StaticKeys(keyMaterial.AsSpan(0, 16), keyMaterial.AsSpan(16, 16), keyMaterial.AsSpan(32, 16));
    await session.PutKeyAsync(new KeyReference(ScpKid.SCP03, Kvn: 0x02), newKeys, replaceKvn: 0);
}
finally
{
    CryptographicOperations.ZeroMemory(keyMaterial);
}
```

Pass the KVN you are replacing as `replaceKvn` to overwrite an existing version, or `0` to add a new one. Once
the new version is in place, delete the old one with `DeleteKeyAsync(oldKeyReference, deleteLast: false)` and
authenticate future sessions with a `Scp03KeyParameters` built from the new reference and keys.

### Set up SCP11b

```csharp
var keyReference = new KeyReference(ScpKid.SCP11b, Kvn: 0x01);
ECPublicKey devicePublicKey = await session.GenerateKeyAsync(keyReference, replaceKvn: 0);
```

Persist `devicePublicKey`. Later sessions authenticate by passing `new Scp11KeyParameters(keyReference,
devicePublicKey)` as `ScpKeyParameters`, exactly as in the SCP03 flow above. SCP11a and SCP11c also need the
Off-Card Entity side configured: `StoreCertificatesAsync` loads the certificate bundle (leaf last),
`StoreCaIssuerAsync` records the CA subject key identifier, and `StoreAllowListAsync` restricts serial numbers.

### Factory reset

```csharp
await using var session = await device.CreateSecurityDomainSessionAsync();
await session.ResetAsync();
```

## User interaction

The Security Domain needs no PIN and no physical touch. The only credential is the SCP key material you
supply through `SessionCreationOptions.ScpKeyParameters`, so this module does not use
`SessionCreationOptions.UserPresencePrompt`. Pass a `CancellationToken` to any session call to abandon it;
`ResetAsync` in particular sends up to 65 failed authentication attempts per key and takes noticeably longer
than other operations.

## Constraints

- SmartCard transport only. Requesting another transport through
  `SessionCreationOptions { PreferredConnectionType = ... }` throws.
- One live connection per physical YubiKey, and one session per connection. Whoever creates a connection
  disposes it with `await using`; a session from `CreateSecurityDomainSessionAsync` owns the one it opened.
- The Security Domain cannot detect firmware. `SessionCreationOptions.FirmwareVersionOverride` is its only
  exact version source; without it the session assumes 5.3.0 for protocol configuration. Supply the version
  from a Management session so the SCP11 gate can be evaluated.
- `GenerateKeyAsync` throws `NotSupportedException` below 5.7.2 only when you supplied
  `FirmwareVersionOverride`; without it an unsupported device fails as an `ApduException` instead.
- `ResetAsync`, `PutKeyAsync`, `DeleteKeyAsync`, `StoreAllowListAsync`, and `ClearAllowListAsync` change
  persistent state. `ResetAsync` blocks every registered key by exhausting its retry counter; there is no undo,
  and any key you loaded yourself is gone.
- Establishing a secure channel that fails throws `SecureChannelException` with the underlying failure as
  `InnerException`.

## Security notes

- `StaticKeys` holds the ENC, MAC, and DEK values and implements `IDisposable`. Wrap it in `using` so the key
  bytes are zeroed; `Scp03KeyParameters` and `Scp11KeyParameters` do the same for what they own.
- Session creation borrows the key parameters you pass. You keep ownership and remain responsible for
  disposing them after the session is gone.
- Zero any caller-owned key buffer with `CryptographicOperations.ZeroMemory` in a `finally`. The SDK does not
  zero buffers it did not allocate.
- The factory SCP03 key set is a public value. Treat a device that still has it as unprotected, and rotate
  before relying on the channel.
- Never log key bytes, key check values, or challenge and response payloads. Log key references only.

## Related

- [Core](../Core/README.md) - SCP key parameter types, SmartCard protocol, TLV, and logging configuration.
- [Management](../Management/README.md) - the firmware version to pass as `FirmwareVersionOverride`.
- [PIV](../Piv/README.md) - an applet session that can run over a channel established with these keys.
- [Device discovery](../../docs/usage/device-discovery.md) - finding and monitoring YubiKeys.
- [GlobalPlatform Card Specification](https://globalplatform.org/specs-library/card-specification-v2-3-1/) -
  the SCP03 and SCP11 source specifications.
- [CLAUDE.md](CLAUDE.md) - contributor guidance, internals, and test infrastructure.
