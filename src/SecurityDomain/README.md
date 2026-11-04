# Yubico.YubiKit.SecurityDomain

The Security Domain is the GlobalPlatform root security application on a YubiKey. It owns the Secure Channel
Protocol (SCP) keys that every other applet session can use to encrypt and authenticate its APDU traffic. This
package inspects, generates, imports, rotates, and deletes those keys, manages SCP11 certificates and
allowlists, and attempts a destructive Security Domain reset. Other modules consume the keys through
`SessionCreationOptions.ScpKeyParameters`.

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.

## Requirements

- .NET 10 on Windows, macOS, or Linux; Linux also needs PC/SC and udev rules ([Linux setup](../../docs/linux-setup.md)).
- SmartCard transport only, over USB CCID or NFC.

| Feature | Minimum firmware |
| --- | --- |
| Security Domain application, SCP03 | 5.3.0 |
| SCP11a, SCP11b, SCP11c | 5.7.2 |

The API also exposes 5.7.0 feature descriptors for key attestation and allowlists. These descriptors do not
establish device/profile applicability on their own; this module has no public key-attestation operation.

Keys are addressed by a `KeyReference(byte Kid, byte Kvn)`. The KID (key identifier) selects the protocol —
`ScpKid.SCP03` (0x01), `ScpKid.SCP11a` (0x11), `ScpKid.SCP11b` (0x13), `ScpKid.SCP11c` (0x15) — and the KVN
(key version number) distinguishes multiple versions of the same protocol. `KeyReference.Default` is the
factory SCP03 reference, KID 0x01 and KVN 0xFF.

SCP03 is a symmetric channel built on a `StaticKeys` triple (ENC, MAC, DEK). SCP11 is asymmetric and uses
EC P-256 keys. SCP11b authenticates the device to the host using a trusted device public key; it does not
authenticate the host or authorize Security Domain key management. SCP11a and SCP11c additionally use an
Off-Card Entity (OCE, the host) key pair and certificate chain for host authentication. The administrative
examples below use SCP03. SCP11 permissions are command-specific: the August 2023 SCP11 v1.3.0.13 Public
Review draft §4.6 forbids PUT KEY, DELETE [keys], and SET STATUS over SCP11c. This draft restriction does not
establish final-specification or YubiKey-profile behavior. The inherited `IsAuthenticated` flag alone is
not evidence that the host has authority for a particular operation.

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

IYubiKey device = await YubiKeyManager.FindFirstAsync();
await using var session = await device.CreateSecurityDomainSessionAsync();

foreach (var keyInfo in await session.ListKeyInformationAsync())
{
    Console.WriteLine(keyInfo.KeyReference);
}
```

Reading key information needs no secure channel, so this works on an untouched device. For a single read,
`device.ListKeyInformationAsync()` opens and disposes the session for you. If another imported module later
defines an equally applicable extension, qualify this static call with
`Yubico.YubiKit.SecurityDomain.IYubiKeyExtensions` or create an explicit Security Domain session. Later snippets
assume these directives and a `device` obtained the same way.

## Common operations

### Authenticate with the default SCP03 keys

```csharp
using var scpKeyParameters = Scp03KeyParameters.Default;
await using var session = await device.CreateSecurityDomainSessionAsync(
    new SessionCreationOptions { ScpKeyParameters = scpKeyParameters });

IReadOnlyList<Yubico.YubiKit.SecurityDomain.KeyInfo> keys = await session.ListKeyInformationAsync();
```

`Scp03KeyParameters.Default` wraps the publicly documented factory key set. Use it only on a device you are
authorized to administer and whose factory set is still installed. The next two snippets continue on this
administrative `session`; each is a separate task, not a single combined program.

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

Use three AES-128 keys and a custom KVN from 0x01 to 0xFE; 0xFF is reserved for the factory set. Yubico
documents three distinct cases:

- On a factory device, the first import replaces the factory set, even with `replaceKvn: 0`. There is no
  retained factory version to delete afterward.
- With custom sets already installed, `replaceKvn: 0` adds a version alongside them.
- A nonzero `replaceKvn` requests replacement of that existing version. The overwritten secrets cannot be
  recovered from the device.

Before removing any retained old set, dispose the importing session and verify a fresh session with
`Scp03KeyParameters` built from the new reference and escrowed keys. Only then delete the old set, if it is
still present. `DeleteKeyAsync` takes a filter: zero KID or KVN omits that condition, so multiple keys can
match. SCP03 component KIDs 0x01/0x02/0x03 become a **KVN-only filter**; always supply a nonzero KVN and
review all matching keys. The SDK does not reject the empty filter produced by SCP03 with KVN 0; do not
use that combination. `deleteLast: true` permits last-key deletion on the device; it is not a host safety
check. Yubico documents deletion of the last SCP03 set as restoring factory defaults, not leaving the
device protected without keys.

The SDK does not enforce every device range: integer `replaceKvn` values are currently cast to one byte.

### Set up SCP11b

```csharp
var keyReference = new KeyReference(ScpKid.SCP11b, Kvn: 0x01);
ECPublicKey devicePublicKey = await session.GenerateKeyAsync(keyReference, replaceKvn: 0);
```

Persist `devicePublicKey` through this trusted provisioning session. Later sessions establish SCP11b by
passing `new Scp11KeyParameters(keyReference, devicePublicKey)` as `ScpKeyParameters`, with an owned parameter
scope enclosing the session as in the SCP03 example. This provides a channel, not host management authority.

### Understand SCP11a/c provisioning roles

SCP11a/c need a provisioned trust relationship as well as a device key. Keep these roles separate:

| Material | Role |
| --- | --- |
| Device private-key reference and trusted device public key | Identify the Security Domain's SCP11 key; generation returns the public key, not the private key. |
| CA-KLOC public key/reference | Lets the device authenticate the OCE's certificate chain. Import this public key through an authorized administrative channel. |
| CA identifier | `StoreCaIssuerAsync` records a Subject Key Identifier for CA-KLOC or CA-KLCC; it does not import a CA key or certificate. |
| OCE private key and certificate chain | Supplied by the host in `Scp11KeyParameters` for SCP11a/c; this is not the Security Domain certificate store. |
| Security Domain certificate bundle | `StoreCertificatesAsync` stores certificates associated with a device key, in issuer-to-leaf order (leaf last); `GetCertificatesAsync` loads them in received order. |

Provision keys and identifiers using an existing administrative channel, retain a trusted device public
key, then create the SCP11a/c parameters with the host's private key and certificate chain. Certificate
retrieval/loading does not validate trust, signatures, validity, order, or key association. Your application
must establish those properties before relying on the public key.

An allowlist restricts the certificate directly signed by the CA-KLOC: the OCE certificate, or the first
intermediate when a chain is used, rather than every leaf serial. This scope is described in the
[SCP11 v1.3.0.13 Public Review draft](https://globalplatform.org/wp-content/uploads/2023/08/GPC_2.3_F_SCP11_v1.3.0.13_PublicRvw.pdf)
(August 2023, §§4.3, 7.5.1 and 7.9). This draft is not a final specification or a complete YubiKey profile.
`StoreAllowListAsync` replaces the restriction and rejects an empty collection; supply nonempty hexadecimal
serial byte sequences in device order. Malformed hex throws `FormatException`; the SDK currently permits
an empty individual string. `ClearAllowListAsync` deliberately removes the restriction, allowing all
CA-signed certificates subject to other authentication checks. It is not "deny all"; to stop trusting a CA,
delete its key. These writes require host-authenticated administrative authority. The SDK emits no SCP11c
allowlist counter, so do not assume these updates are supported over SCP11c without checking the device profile.

### Factory reset

```csharp
await using var session = await device.CreateSecurityDomainSessionAsync();
await session.ResetAsync();
```

Reset targets Security Domain keys and associated data, not PINs or credentials in other applets. It tries
to exhaust key retry counters using at most 65 raw attempts per processed reference, skipping duplicate
SCP03 components. Custom state can be lost permanently. Yubico documents factory SCP03 restoration when
the last SCP03 set is deleted; that does not prove restoration for every mixed-key/firmware state.

The SDK does not verify factory contents after reset. It retries ordinary transmission failures, does not
throw merely because the attempt limit is reached, and skips reset for an empty key listing. Return alone
therefore does not prove complete restoration. Failure or cancellation can leave partial persistent changes.
Use an unauthenticated reset session as above: retained SCP parameters may no longer work during reselection
and reauthentication. Currently reset reinitialization also omits the original protocol configuration and
firmware override. Dispose the reset session and create a fresh session with your required options afterward.

## User interaction

The Security Domain needs no PIN and no physical touch. The only credential is the SCP key material you
supply through `SessionCreationOptions.ScpKeyParameters`, so this module does not use
`SessionCreationOptions.UserPresencePrompt`. Pass a `CancellationToken` to session calls to request cancellation.
Cancellation is not rollback: a device may already have accepted a persistent change. Reset can take noticeably
longer than other operations because it makes repeated authentication attempts.

## Constraints

Shared Core [cryptographic metadata](../Core/README.md#cryptographic-metadata) uses `KeyType` concrete
identities and `KeyInfo` on key objects. Catalog representation/applicability and COSE mappings do not
expand SCP11's explicit P-256 requirements. `DisplayName` is for human output.

- SmartCard transport only. Requesting another transport through
  `SessionCreationOptions { PreferredConnectionType = ... }` throws.
- One live connection per physical YubiKey, and one session per connection. Whoever creates a connection
  disposes it with `await using`; a session from `CreateSecurityDomainSessionAsync` owns the one it opened.
- The Security Domain cannot detect firmware. `SessionCreationOptions.FirmwareVersionOverride` is its only
  exact version source; without it the session assumes 5.3.0 for protocol configuration. Supply the version
  from a Management session so the SCP11 gate can be evaluated.
- `GenerateKeyAsync` locally checks for firmware 5.7.2 only when you supplied
  `FirmwareVersionOverride` at creation. Without it, the request reaches the device and may fail with an
  `ApduException`. Reset currently clears this explicit-version indicator; use a fresh session afterward.
- Key generation/import/deletion, certificate/CA/allowlist storage and clearing, raw `StoreDataAsync`, and
  reset change persistent state. Review the target reference and recovery plan before sending them.
- With SCP parameters supplied, selected secure-channel configuration failures (`ApduException`,
  `BadResponseException`, `NotSupportedException`) are wrapped in `SecureChannelException` with the original
  `InnerException`. Earlier protocol preparation/application selection failures and cancellation are not
  wrapped by that catch; later operation errors keep their original types.

## Security notes

- `StaticKeys` holds the ENC, MAC, and DEK values and implements `IDisposable`. Wrap it in `using` so the key
  bytes are zeroed; `Scp03KeyParameters` and `Scp11KeyParameters` do the same for what they own.
- Session creation borrows the key parameters you pass. You keep ownership and remain responsible for
  disposing them after the session is gone.
- `GetCertificatesAsync` returns caller-owned certificates. Dispose each certificate when no longer needed;
  disposing the session does not dispose them. If any certificate cannot be loaded, the SDK disposes those
  already loaded and throws instead of returning a partial bundle.
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
- [YubiKey SCP specifics](https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-scp.html) -
  vendor support and SCP03 key-set behavior.
- [GlobalPlatform SCP11 catalogue](https://globalplatform.org/specs-library/secure-channel-protocol-11-amendment-f/) -
  specification versions; the public-review draft cited above is not a final release.
- [Developer guide](../../docs/DEV-GUIDE.md): building, testing, and contributing.
