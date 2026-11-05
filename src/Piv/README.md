# Yubico.YubiKit.Piv

The PIV application (NIST SP 800-73) turns a YubiKey into a smart card holding private keys and X.509 certificates
in numbered slots. This package covers PIN, PUK, and management-key authentication, key and certificate management,
signing, decryption, ECDH key agreement, and attestation, on top of `Yubico.YubiKit.Core` and no other module.

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.

## Requirements

- .NET 10 on Windows, macOS, or Linux; Linux also needs PC/SC and udev rules ([Linux setup](../../docs/linux-setup.md)).
- A YubiKey with PIV enabled and SmartCard access (USB CCID or NFC). Current operations target YubiKey 4/5 families;
  older NEO devices have a limited subset. Firmware gates describe SDK compatibility checks; product profiles,
  transport restrictions and device configuration still govern each command. Bio, NFC and operating-system
  combinations need their own qualification.

| Feature | SDK firmware gate |
| --- | --- |
| P-384 curve, PIN and touch policies | 4.0.0 |
| Cached touch policy, key attestation | 4.3.0 |
| Metadata (PIN, PUK, management key, slot) | 5.3.0 |
| AES management key | 5.4.0 |
| Move and delete key, Curve25519, RSA 3072 and 4096 | 5.7.0 |

Yubico dates released-device AES management support to **5.4.2**; the SDK gate is 5.4.0. High-level
`DecryptAsync` also needs metadata (5.3.0), even where raw RSA operations work on older firmware.

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
Later fragments assume these directives and a `device` obtained the same way. Run persistent-state
examples only on a device you are authorized to provision: key generation can replace an existing key.

## Common operations

### Authenticate and generate a key pair

```csharp
await using var session = await device.CreatePivSessionAsync();

// Evaluation only: this public factory value must match the key currently installed.
byte[] managementKey = PivSession.DefaultManagementKey.ToArray();
try
{
    await session.AuthenticateAsync(managementKey);
    var keyOptions = new KeyCreationOptions { PinPolicy = PinPolicy.Once, TouchPolicy = TouchPolicy.Never };
    IPublicKey publicKey = await session.GenerateKeyAsync(Slot.Authentication, KeyType.P256, keyOptions);
}
finally
{
    CryptographicOperations.ZeroMemory(managementKey);
}
```

The session discovers the management-key algorithm at creation. `ManagementKeyType` is nullable for an
unmodeled identifier; inspect `GetManagementKeyMetadataAsync` and its `RawAlgorithmId`/`RawData` before
choosing a compatible workflow. Management types are `TripleDes` (24 bytes), `Aes128` (16), `Aes192` (24)
and `Aes256` (32). Factory reset uses Triple-DES on older released firmware and AES-192 on 5.7+;
development firmware follows the SDK's modern-default convention. Metadata takes precedence when available.

`IsManagementKeyAuthenticated` reports the session's observed PIV management authentication. The inherited
`IsAuthenticated` reports protocol authentication such as SCP. PIN verification is separate card state.
Failed management authentication clears a previous recorded success; successful `SetManagementKeyAsync`
updates the type and preserves authentication without retaining the supplied key bytes. A SET security-status
failure clears the flag; other SET command failures preserve it. External card changes can still invalidate
what a local observation tells you.

To rotate a management key, authenticate the current key, securely back up the replacement, then call
`SetManagementKeyAsync(newType, newKey, requireTouch: false, cancellationToken: token)`. Clear your buffers
after the operation, including failure. Keep the backup until a fresh session authenticates the replacement.
For existing PIN-only configurations, coordinate the recovery objects as described below.

### Choose policies and import an existing key

`KeyCreationOptions` groups PIN and touch policies; omitted options use device defaults. Default PIN policy
is slot-dependent (normally Always for Signature, Never for Card Authentication, Once for the other slots).
`Once` reuses card verification state; `Always` requires verification for each private-key use. MATCH policies
use fingerprint verification on a capable Bio device; enrollment is a separate prerequisite. Touch policies
are Never, Always or a short device-managed Cached window. Set policies when generating or importing the key.

Import accepts an application-owned `IPrivateKey`. Keep it alive through the awaited call and dispose it
afterward to clear its private material. RSA import requires exponent 65537. PIV supports its modeled RSA,
P-256/P-384 and firmware-gated Ed25519/X25519 subsets; the shared Core catalog includes additional types
for other applications. See [Core cryptographic keys](../Core/README.md#cryptographic-metadata) for key creation
and loading. Key generation returns public material; the device retains the private key.

### Store and read a certificate

Storing needs management-key authentication. This fragment assumes an open, authenticated `session` and an
issuer-provided `authentication.cer` for the slot's public key.

```csharp
using var certificate = X509CertificateLoader.LoadCertificateFromFile("authentication.cer");
await session.StoreCertificateAsync(Slot.Authentication, certificate, CertificateCompression.Automatic);
using X509Certificate2? stored = await session.GetCertificateAsync(Slot.Authentication);
if (stored is null)
    throw new InvalidOperationException("The slot has no usable certificate data.");
```

Keys and certificates occupy separate storage. Generating, importing, moving or deleting a key leaves its
certificate alone; deleting a certificate leaves the key alone. `StoreCertificateAsync` stores the supplied
certificate without proving key binding. Compare its public key with slot metadata, or perform a challenge
signature/agreement proof, before enrolling it. A null certificate result says nothing about private-key occupancy.
The caller owns certificates returned by retrieval and attestation and disposes them after use.

Automatic compression uses GZip when DER exceeds 1856 bytes. That is an SDK policy based on a recommended
PIV certificate length, rather than a hard storage limit; device capacity still applies. Select `Always`
to force GZip even for smaller certificates; the current enum has no force-uncompressed mode.
This reader supports GZip and ordinary DER;
other vendors' certificate encodings may require a raw-object workflow.

### Verify the PIN, sign and verify on the host

This fragment uses the P-256 key generated above and firmware 5.3+ metadata. The public-key extractor returns
a disposable host object containing public material only. The host verifies the exact digest and DER signature.

```csharp
await using var session = await device.CreatePivSessionAsync();

byte[] pin = Encoding.UTF8.GetBytes("123456");
byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes("message to sign"));
Memory<byte> signature = Memory<byte>.Empty;
try
{
    await session.VerifyPinAsync(pin);
    var metadata = await session.GetSlotMetadataAsync(Slot.Authentication)
        ?? throw new InvalidOperationException("Generate a P-256 key first.");
    using var verifier = metadata.GetECDsaPublicKey();
    signature = await session.SignOrDecryptAsync(Slot.Authentication, KeyType.P256, digest);
    if (!verifier.VerifyHash(digest, signature.Span, DSASignatureFormat.Rfc3279DerSequence))
        throw new CryptographicException("The slot signature failed host verification.");
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
    CryptographicOperations.ZeroMemory(digest);
    CryptographicOperations.ZeroMemory(signature.Span);
}
```

ECDSA accepts a digest and returns ASN.1 DER `SEQUENCE(r,s)`. Ed25519 accepts the original message and returns
a raw 64-byte signature. RSA signing accepts a complete host-prepared EMSA-PKCS1-v1_5 or EMSA-PSS encoded
representative and returns a modulus-sized result. A hash alone is insufficient RSA signature encoding.
The overload without `KeyType` reads metadata (5.3+); older keys need the explicit type, matched to the slot.

### Decrypt and clear the plaintext

This fragment requires firmware 5.3+, an existing RSA key in Key Management, its matching certificate and
the current PIN. It encrypts known plaintext on the host, decrypts on the device and compares the result.
The test PIN is illustrative; acquire real credentials into a caller-owned mutable buffer.

```csharp
await using var session = await device.CreatePivSessionAsync();
using var certificate = await session.GetCertificateAsync(Slot.KeyManagement)
    ?? throw new InvalidOperationException("Provision a matching RSA certificate first.");
using var rsa = certificate.GetRSAPublicKey()
    ?? throw new InvalidOperationException("The certificate must contain an RSA public key.");
byte[] pin = Encoding.UTF8.GetBytes("123456");
byte[] expected = Encoding.UTF8.GetBytes("known plaintext");
Memory<byte> plaintext = Memory<byte>.Empty;
try
{
    byte[] ciphertext = rsa.Encrypt(expected, RSAEncryptionPadding.OaepSHA256);
    await session.VerifyPinAsync(pin);
    plaintext = await session.DecryptAsync(Slot.KeyManagement, ciphertext, RSAEncryptionPadding.OaepSHA256);
    if (!CryptographicOperations.FixedTimeEquals(expected, plaintext.Span))
        throw new CryptographicException("Decryption returned unexpected plaintext.");
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
    CryptographicOperations.ZeroMemory(expected);
    CryptographicOperations.ZeroMemory(plaintext.Span);
}
```

`DecryptAsync` removes PKCS#1 v1.5 or the selected OAEP padding on the host. Raw RSA
`SignOrDecryptAsync` returns the padded representative. `CalculateSecretAsync` accepts a matching
P-256/P-384 or X25519 peer public key and returns raw agreement bytes. Your protocol authenticates the peer,
selects a KDF and derives application keys; clear the raw shared secret after derivation.

### Manage PIN/PUK retries and recover access

`VerifyPinAsync` and `ChangePinAsync` validate 6–8 encoded bytes. `ChangePukAsync` and `UnblockPinAsync`
enforce an eight-byte maximum in their pair encoder; the device/product profile supplies further policy.
UTF-8 character count can differ from byte count. These SDK checks accept raw bytes without validating
their content. The federal revision-4 PIV profile instead requires a 6–8-byte ASCII decimal PIN and an
eight-byte binary PUK; choose credentials for the profile you actually deploy.

Catch `InvalidPinException` and inspect `RetriesRemaining` before asking for another credential. Let the
application bound retries and obtain fresh user input rather than looping on the same value. A usable PUK
can unblock the PIN with `UnblockPinAsync(puk, newPin)`; when both are blocked, consider an explicitly
authorized destructive reset. `GetPinAttemptsAsync` prefers metadata and falls back only when that
instruction is unavailable. The older `63CX` status carries 0–15 retries; success without a count raises
`InvalidOperationException`. Three is the factory count, not the general maximum.

`SetPinAttemptsAsync` accepts limits 1–255 and needs management authentication plus a verified PIN.
Every call restores PIN `123456` and PUK `12345678`, even for unchanged limits. Change both afterward.
These are separate commands: retain a recovery plan for a failure between them.

### Enable, reopen, recover and disable PIN-protected administration

PIN-protected mode stores the **already active** management key in PIN-gated PRINTED storage and records
the mode in ADMIN DATA. First install and securely back up a random management key using your application's
credential workflow. The following composable method takes that active key and current PIN as borrowed
inputs. Its caller retains and clears both buffers after the method, including failure. Run it only on an
authorized device: enabling deliberately blocks the PUK, and disabling leaves it blocked.

```csharp
static async Task ProtectedAdministrationRoundTripAsync(
    IYubiKey device,
    ReadOnlyMemory<byte> pin,
    ReadOnlyMemory<byte> activeManagementKey,
    CancellationToken cancellationToken)
{
    await using (var setup = await device.CreatePivSessionAsync(cancellationToken: cancellationToken))
    {
        await setup.AuthenticateAsync(activeManagementKey, cancellationToken);
        await setup.SetPinOnlyModeAsync(PinOnlyMode.PinProtected, pin, activeManagementKey, cancellationToken);
    }

    await using (var reopened = await device.CreatePivSessionAsync(cancellationToken: cancellationToken))
    {
        PinOnlyMode detected = await reopened.GetPinOnlyModeAsync(cancellationToken);
        if (!detected.HasFlag(PinOnlyMode.PinProtected))
            throw new InvalidOperationException("Inspect ADMIN DATA before continuing.");
        PinOnlyMode recovered = await reopened.RecoverPinOnlyModeAsync(pin, cancellationToken);
        if (!recovered.HasFlag(PinOnlyMode.PinProtected) || !reopened.IsManagementKeyAuthenticated)
            throw new InvalidOperationException("Use the backed-up active management key to recover access.");

        // Perform authorized administration here, then restore the type-appropriate public default.
        await reopened.SetPinOnlyModeAsync(PinOnlyMode.None, pin, cancellationToken: cancellationToken);
    }
}
```

Enabling reauthenticates the supplied active key, verifies the PIN, stores PRINTED, blocks PUK and writes
ADMIN DATA, preserving a legacy derivation salt if present. Disabling restores the active algorithm's
well-known default key first, then deletes PRINTED and ADMIN DATA. Replace that public default promptly
with a securely backed-up key for deployment. These multi-command workflows provide no rollback: after
failure, inspect ADMIN/PRINTED and credential metadata, then authenticate with the key that is actually
active before deciding how to complete recovery. Keep caller-controlled backups through partial transitions.

Mode detection reads ADMIN DATA only. Recovery tries PRINTED and legacy PIN-derived data; it verifies the
PIN once if access requires it and treats only security-status `0x6982` as candidate rejection. A stale
derived candidate triggers reauthentication of an earlier successful protected key before that success is
returned. A None result with no candidate attempted can preserve preexisting authentication. New derived
setup is excluded; migrate existing derived configurations with a coordinated recovery plan.
Raw PIN/key/retry changes leave these recovery objects unchanged, so coordinate rotations yourself.

### Read provisioning objects and attest keys

Use the typed CHUID, CCC, ADMIN DATA and Key History methods for the SDK's modeled provisioning shapes.
CHUID fixes FASC-N/expiration/signature fields; CCC fixes its surrounding fields. Other valid vendor/profile
encodings may need `GetObjectAsync`/`PutObjectAsync`. Raw methods exchange inner object bytes, with the
outer `53` wrapper handled by the SDK. Use defined `DataObject` identifiers; keep request IDs within the
intended PIV range because arbitrary integers currently reach the encoder without a range guard.

CHUID/CCC constructors borrow supplied identifier memory; decoding copies it and encoding returns a new
nonsecret buffer. Key History counts describe retired keys with on-/off-card certificates. Maintain it
alongside your provisioning workflow; its parser/constructor validate a narrower shape than a complete
federal profile. ADMIN flags describe stored software records. Its timestamp currently uses trimmed
little-endian Unix seconds, differing from Python YubiKey Manager's four-byte big-endian encoding;
coordinate interoperability before writing timestamps across tools.

`AttestKeyAsync` returns a certificate for an on-device-generated key. Retrieve the F9 certificate for the
signer chain, validate against your trust policy, and bind the attested public key to the intended slot.
Retrieval alone supplies neither chain trust nor that binding. Preserve factory F9 material: replacing it
is a separate, potentially irreversible provisioning decision. A host public-key object can prepare a
certificate request, while device signing needs an external `X509SignatureGenerator`, as PivTool demonstrates.

### Reset PIV deliberately

`ResetAsync` consumes PIN and PUK retries until both are blocked, then sends Yubico RESET without requiring
management authentication. Normal private-key slots, their certificates, provisioning objects and
credentials return to factory state. **The F9 attestation key and certificate survive normal PIV reset.**
Failure or cancellation can leave retry counters partly consumed before RESET. After a successful device
reset the session clears management authentication and refreshes the management-key type; refresh can
still throw after the reset has applied. Inspect fresh state before retrying. Bio MPE uses a Management
device-reset route; the PIV implementation's Bio preflight remains unfinished. Consult the product's reset
procedure before invoking this workflow on Bio hardware.

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

The prompt covers signing/raw private operations, decryption and agreement. It receives a request immediately
before the private-key command and a resolution after a successful response is parsed, before output transfer
or local RSA unpadding. Consequently local padding failure can follow a Completed resolution. Management
authentication and Bio verification use their own device ceremonies. `TouchPolicy.Always` maps to `UserPresenceBasis.PolicyRequires`; `Cached` and
unreadable slot metadata map to `PolicyMayRequire`, because the touch cache is not observable. `Never`,
`Default`, and an empty slot are silent. A request callback failure prevents the private-key command.
Cancellation is cooperative: await the operation's terminal result and transport drain before retrying,
disposing or inspecting a possibly changed device. A token alone supplies no native-call deadline.

## Constraints

Core's [cryptographic catalog](../Core/README.md#cryptographic-metadata) exposes immutable `KeyInfo` on
key objects. Internal `KeyTypeEncoding` owns PIV's independent wire identifiers and strict request subsets;
never cast from Core `KeyType` or infer PIV support from catalog representation/applicability.

- Requesting a non-SmartCard transport through `SessionCreationOptions { PreferredConnectionType = ... }` throws.
- One live connection per physical YubiKey, and one session per connection. Whoever creates a connection
  disposes it with `await using`; a session from `CreatePivSessionAsync` owns and disposes the one it opened.
- `ResetAsync`, `DeleteKeyAsync`, `DeleteCertificateAsync`, `MoveKeyAsync`, `SetManagementKeyAsync`,
  `SetPinAttemptsAsync`, and `SetPinOnlyModeAsync` change persistent state. Normal reset retains F9 material.
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

## Maintainer verification

For maintainers, run unit tests with `dotnet toolchain.cs -- test --project Yubico.YubiKit.Piv`. On an explicitly allowlisted
dedicated test key, run smoke with `dotnet toolchain.cs -- test --integration --project Yubico.YubiKit.Piv --smoke`, then full
unattended coverage with `dotnet toolchain.cs -- test --integration --project Yubico.YubiKit.Piv --filter "Category!=RequiresUserPresence"`.
The qualified selector targets the module's test projects in a checkout that also contains archived audit hosts.
These tests reset and mutate PIV, including a protected-mode enable/reopen/recover/disable round trip. Touch
and hotplug tests need separate human coordination; multi-key contention tests need two attached keys.

## Related

- [Core](../Core/README.md) - device discovery, SmartCard protocol, TLV, and cryptography primitives.
- [Security Domain](../SecurityDomain/README.md) - SCP03 and SCP11 keys that protect the PIV channel.
- [User interaction](../../docs/usage/user-interaction.md) and [device discovery](../../docs/usage/device-discovery.md) - cross-module guides.
- [NIST SP 800-73-4, Interfaces for Personal Identity Verification](https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-73-4.pdf),
  May 2015, updated February 8, 2016, especially Part 2 §2.4.3 (credential format) and Part 1 (objects).
- [NIST SP 800-78-4, Cryptographic Algorithms and Key Sizes for Personal Identity Verification](https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-78-4.pdf), May 2015;
  [FIPS 201-2, Personal Identity Verification (PIV) of Federal Employees and Contractors](https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.201-2.pdf), August 2013.
  These fixed referenced revisions are superseded (by SP 800-73/78 revision 5 and FIPS 201-3). They explain
  the referenced profile, rather than asserting adoption of the latest federal profile. Yubico extensions
  and SDK algorithm availability extend beyond that profile; deployment compliance requires separate review.
- [Yubico PIV reset](https://docs.yubico.com/yesdk/yubikey-api/Yubico.YubiKey.Piv.Commands.ResetPivCommand.html) and
  [PIV technical manual](https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-piv.html).
- [Developer guide](../../docs/DEV-GUIDE.md): building, testing, and contributing.
