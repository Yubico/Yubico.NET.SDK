# Yubico.YubiKit.OpenPgp

Use this package to provision OpenPGP card keys, sign messages, perform raw decipher/agreement operations,
store certificates, and administer PINs and touch policy on a YubiKey. The host application handles OpenPGP
packet/file processing, key fingerprints and certificate trust decisions. OpenPGP and PIV have separate credentials.

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for its evaluation status.

## Requirements and installation

Use .NET 10 and SmartCard transport over USB CCID or NFC. Linux needs PC/SC and udev rules; see
[Linux setup](../../docs/linux-setup.md). This applet has no HID interface.

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.OpenPgp --prerelease
```

Core is installed transitively. The current SDK checks these version thresholds for the indicated paths:

| SDK check | Version |
|---|---|
| Application reset | 1.0.6 |
| UIF read/write | 4.2.0 |
| EC generation/import, ordinary per-slot certificates, attestation, algorithm-list query | 5.2.0 |
| Corrected SELECT DATA framing; earlier supported versions use a length-prefix workaround | 5.4.4 |
| User PIN unverification | 5.6.0 |

Match these checks with the card's advertised capabilities, supported algorithms, slot and policy support.
They describe this SDK's checks rather than a blanket availability guarantee. In particular, cached/ATT touch
policies need additional device support, and retry/challenge commands leave some capability checks to the card.
The 1.0.0 version-query fallback is a compatibility fallback, not a qualified minimum device version. NEO applet
versions have a different history from physical firmware; NEO import, generation and deletion paths still need
separate qualification. Development-device applet versions and device metadata can differ: retain both when
diagnosing a feature check, and use an override only with a known device profile.

For RSA generation, follow [YSA-2017-01](https://www.yubico.com/support/security-advisories/ysa-2017-01/):
the advisory identifies on-device OpenPGP RSA generation on 4.2.6–4.3.4 as affected by ROCA and 4.3.5 as corrected.
Externally generated/imported keys are a mitigation. This SDK currently has no shipping ROCA generation guard;
the Python SDK uses the broader conservative interval `[4.2.0, 4.3.5)`.

## Open and inspect a session

This complete read-only example selects the first discoverable key. Applications managing several keys should
select and confirm the intended device before any write. Every later C# block is a **composable method**: place
it in a class with the directives below and supply its typed parameters from your application. Each method opens
and disposes its own session. Parameters containing credentials, private-key input or message data are borrowed;
the caller keeps them valid through completion and clears sensitive source buffers afterward.

```csharp
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.OpenPgp;

IYubiKey device = await YubiKeyManager.FindFirstAsync();
await using var session = await device.CreateOpenPgpSessionAsync();
ApplicationRelatedData data = await session.GetApplicationRelatedDataAsync();
Console.WriteLine($"Card {data.Aid.Version}, applet {session.FirmwareVersion}");
foreach (var entry in await session.ListKeyInformationAsync())
{
    Console.WriteLine($"{entry.Key}: {entry.Value}");
}
```

The device extension owns the connection it opens. `OpenPgpSession.CreateAsync(connection, ...)` uses an existing
SmartCard connection; keep that connection alive through session disposal and dispose it afterward. Await operations
sequentially on a session. `GetApplicationRelatedDataAsync` refreshes cached algorithms and UIF, while KDF has a
separate lazy cache. Typed initialization requires recognized mandatory algorithm attributes; use `GetDataAsync`
for raw response inspection when a model omits a field.

`KeyInformation`, `KeyFingerprints`, and `GenerationTimes` are read-only dictionaries. Their slots reflect the
card's reported data. Use key status to inspect presence: an unset fingerprint can still occupy a 20-byte zero-filled
field. `GetSupportedAlgorithmsAsync` returns an ordered list with repeated slots; preserve that multiplicity.

## Change credentials and prepare recovery

Pass PINs as UTF-8 bytes from your credential reader. The public factory defaults are User `123456` and Admin
`12345678`; change both before real use. Apply the card's byte-length/profile rules as well as reader limits.
Avoid creating immutable strings for production secrets, and clear owned credential buffers in `finally`.

The same User PIN (PW1) authorizes two separate card states: `extended: false` selects signature mode (VERIFY P2=81),
and `extended: true` selects decryption/authentication mode (P2=82). Both share the User retry counter. The Reset Code
is a separate recovery credential, even though its `Pw.Reset` enum value also happens to be 82.

**Current KDF limitation:** extended verification uses the Reset Code salt and, on status 6982, its retry counter.
Distinct User/Reset salts can therefore reject a correct User PIN and consume a User retry. Use a qualified
configuration for extended operations and treat that fallback count as uncertain until the implementation is corrected.

Change a known PIN without prior verification; the command checks the current value. The following method also
installs a recovery code under the new Admin PIN. All five credential inputs are borrowed buffers that the caller clears.

```csharp
static async Task ChangeCredentialsAsync(
    IYubiKey device, ReadOnlyMemory<byte> currentUserPin, ReadOnlyMemory<byte> newUserPin,
    ReadOnlyMemory<byte> currentAdminPin, ReadOnlyMemory<byte> newAdminPin,
    ReadOnlyMemory<byte> resetCode, CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.ChangePinAsync(currentUserPin, newUserPin, cancellationToken);
    await session.ChangeAdminAsync(currentAdminPin, newAdminPin, cancellationToken);
    await session.VerifyAdminAsync(newAdminPin, cancellationToken);
    await session.SetResetCodeAsync(resetCode, cancellationToken);
}
```

These are separate persistent commands: retain the new User PIN if a later Admin change fails. With KDF active,
change methods derive **both current and new** inputs using the cached KDF. Plan KDF transitions separately below.

Recover a blocked User PIN through either the configured Reset Code or prior Admin authentication. The two methods
below are alternatives; choose the credential your recovery process holds. Each restores the User retry counter.

```csharp
static async Task RecoverWithResetCodeAsync(
    IYubiKey device, ReadOnlyMemory<byte> resetCode, ReadOnlyMemory<byte> newUserPin,
    CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.ResetPinUsingResetCodeAsync(resetCode, newUserPin, cancellationToken);
}

static async Task RecoverWithAdminAsync(
    IYubiKey device, ReadOnlyMemory<byte> adminPin, ReadOnlyMemory<byte> newUserPin,
    CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.VerifyAdminAsync(adminPin, cancellationToken);
    await session.ResetPinUsingAdminAuthenticationAsync(newUserPin, cancellationToken);
}
```

`VerifyPinAsync` and `VerifyAdminAsync` currently map every unsuccessful VERIFY response to
`OpenPgpInvalidPinException`. Inspect its `SW` as well as `RetriesRemaining`: 63Cx supplies a count, 6983 supplies
zero, and 6982 triggers a status query. Other failures or a failed query can leave the count `null`. A 6982 response
can have causes beyond a wrong PIN. Stop automatic retries and diagnose uncertain status before spending another
attempt. For an extended-mode failure, read fresh `GetPinStatusAsync().AttemptsUser`: the typed count can reflect
Reset Code attempts, so even typed zero alone fails to establish that the User PIN is blocked. Change/reset-code
commands propagate general APDU exceptions rather than this retry-aware exception.
A blocked Admin PIN requires destructive application reset; a blocked User PIN has the two recovery routes above.

## Configure KDF before provisioning keys

Check `ExtendedCapabilityFlags.Kdf` in the card's Extended Capabilities before planning a KDF configuration.
The KDF derives credential bytes on the host using Iterated-and-Salted S2K (SHA-256 or SHA-512).
`IterationCount` counts input bytes fed into a hash, with at least one full salt-plus-PIN copy. Card F9 stores the
expanded count in four bytes; RFC 9580 §3.7.1.3 describes the primitive, not a newer packet profile for this API.

Treat enable/change/disable as provisioning operations with a recovery plan, before key generation/import.
`SetKdfAsync` writes F9; the card can refuse the configuration with conditions-not-satisfied (6985) depending on
its state. The generic Card specification assigns credential/F9 alignment to terminal software. YubiKey's vendor
transition restores initial User/Admin values and invalidates the Reset Code, so establish the new credentials
and recovery code under the selected KDF afterward. Qualify same- and fresh-session behavior for your device/profile
before deployment; a successful same-PIN change alone establishes little about the transition.

This provisioning fragment configures distinct salts and initial default-PIN hashes on an authorized, empty,
KDF-capable YubiKey. Supply the currently valid Admin PIN and the UTF-8 default User/Admin buffers (`123456` and
`12345678`). It closes the provisioning session before fresh-session verification. Change default credentials and
install a new Reset Code afterward, then provision keys. **Extended verification remains affected by the distinct-salt
defect**, so this sequence establishes ordinary User/Admin access only; choose a qualified configuration before
using decryption/authentication. A state-dependent refusal requires inspection rather than an automatic reset/retry.

```csharp
static async Task ConfigureInitialKdfAsync(
    IYubiKey device, ReadOnlyMemory<byte> currentAdminPin,
    ReadOnlyMemory<byte> defaultUserPin, ReadOnlyMemory<byte> defaultAdminPin,
    CancellationToken cancellationToken)
{
    byte[] saltUser = RandomNumberGenerator.GetBytes(8);
    byte[] saltReset = RandomNumberGenerator.GetBytes(8);
    byte[] saltAdmin = RandomNumberGenerator.GetBytes(8);
    byte[] hashUser = [];
    byte[] hashAdmin = [];
    try
    {
        using var derivation = new KdfIterSaltedS2k
        {
            HashAlgorithm = KdfHashAlgorithm.Sha256, IterationCount = 100000,
            SaltUser = saltUser, SaltReset = saltReset, SaltAdmin = saltAdmin
        };
        hashUser = derivation.Process(Pw.User, defaultUserPin.Span);
        hashAdmin = derivation.Process(Pw.Admin, defaultAdminPin.Span);
        using var configured = new KdfIterSaltedS2k
        {
            HashAlgorithm = KdfHashAlgorithm.Sha256, IterationCount = 100000,
            SaltUser = saltUser, SaltReset = saltReset, SaltAdmin = saltAdmin,
            InitialHashUser = hashUser, InitialHashAdmin = hashAdmin
        };
        await using (var provisioning = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken))
        {
            await provisioning.VerifyAdminAsync(currentAdminPin, cancellationToken);
            await provisioning.SetKdfAsync(configured, cancellationToken);
        }
        await using var verification = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
        await verification.VerifyAdminAsync(defaultAdminPin, cancellationToken);
        await verification.VerifyPinAsync(defaultUserPin, cancellationToken: cancellationToken);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(hashUser);
        CryptographicOperations.ZeroMemory(hashAdmin);
        CryptographicOperations.ZeroMemory(saltUser);
        CryptographicOperations.ZeroMemory(saltReset);
        CryptographicOperations.ZeroMemory(saltAdmin);
    }
}
```

The current cache retains the exact object passed to `SetKdfAsync`, and `GetKdfAsync` returns that same instance.
Keep it undisposed while the session uses it. Session disposal clears its current KDF; replacement/reset currently
drops the previous reference without disposing it. Keep an enclosing cleanup scope for each supplied object through
session teardown, and clear separately owned initial hashes and serialized KDF bytes after use. Raw F9 writes require
a fresh session to reload KDF; refreshing application data leaves the KDF cache unchanged. These lifetime constraints
describe the current implementation rather than a general cloning/ownership policy.

## Generate or import an ordinary-slot key

Verify the Admin PIN, then choose a supported algorithm for `Sig`, `Dec` or `Aut`. Generation and import write
attributes before the private-key operation. Attribute changes can erase or disable the previous key even if a later
command fails. The sequence has no rollback; after failure/cancellation, inspect fresh application data, key status
and public key before retrying. A cache-refresh error can occur after successful provisioning.

```csharp
static async Task GenerateSigningKeyAsync(
    IYubiKey device, ReadOnlyMemory<byte> adminPin, CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.VerifyAdminAsync(adminPin, cancellationToken);
    await session.GenerateKeyAsync(KeyRef.Sig,
        EcAttributes.Create(KeyRef.Sig, CurveOid.Secp256R1), cancellationToken);
    Console.WriteLine((await session.ListKeyInformationAsync(cancellationToken))[KeyRef.Sig]);
}
```

Use `RsaAttributes.Create(RsaSize.Rsa2048)` for RSA, subject to the ROCA guidance above. EC uses the SDK's 5.2.0 check
and the card's advertised curve support. ATT provisioning currently uses malformed CRT encoding; ordinary target-key
attestation below is a separate operation. Replacing the factory ATT private key would irreversibly lose manufacturer
attestation provenance, even if its public certificate had been saved.

For import, supply a supported SDK `IPrivateKey`: RSA 2048/3072/4096 with exponent 65537, NIST P-256/P-384/P-521,
Ed25519 or X25519 as appropriate for the slot. This method accepts **borrowed PKCS#8 DER containing an RSA key**,
creates its own disposable private-key object, and leaves the source DER for its caller to clear.

```csharp
static async Task ImportRsaSigningKeyAsync(
    IYubiKey device, ReadOnlyMemory<byte> adminPin, byte[] pkcs8Der,
    CancellationToken cancellationToken)
{
    using var privateKey = RSAPrivateKey.CreateFromPkcs8(pkcs8Der);
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.VerifyAdminAsync(adminPin, cancellationToken);
    await session.PutKeyAsync(KeyRef.Sig, privateKey, cancellationToken);
}
```

Generation/import manages the key, while certificates, fingerprints and timestamps are separate tasks.
The host computes the card-profile fingerprint and writes exactly 20 bytes with `SetKeyFingerprintAsync`; use
`SetGenerationTimeAsync` for unsigned four-byte seconds since 1970-01-01 UTC. Zero marks an unspecified time;
pass zero to clear the date metadata. The current API stores/returns those four bytes through a signed `int`,
so values above `int.MaxValue` appear negative. Preserve the original bytes or interpret the same 32 bits as
unsigned seconds, rather than dates before 1970. The setter writes the supplied representation without date-range
validation or special handling of zero. These host-written fields alone establish no provenance.
`DeleteKeyAsync` destructively switches attributes to RSA 4096 then RSA 2048; NEO's vendor deletion strategy differs
and remains separately unqualified. Delete certificates separately when that is your intent.

## Sign and verify on the host

`SignAsync` and `AuthenticateAsync` accept messages/challenges with algorithm-specific handling:

| Slot algorithm | SDK input processing | Returned signature |
|---|---|---|
| RSA | Hash with the named algorithm; supply DigestInfo; card applies PKCS#1 v1.5 padding | Raw modulus-sized bytes |
| ECDSA | Hash with the named algorithm | DER sequence of integers `(r, s)` |
| Ed25519/EdDSA | Send the raw message; `hashAlgorithm` is unused | Raw 64 bytes |

RSA/ECDSA accept SHA-1, SHA-256, SHA-384 and SHA-512; choose a current algorithm suitable for your protocol.
Passing SHA-256 with Ed25519 still performs ordinary Ed25519, rather than selecting a prehashed variant.

This P-256 method reads the card's 7F49 public-key template, decodes field 86, and verifies the returned DER signature
on the host. The helper is included; the input message and PIN remain borrowed. Use a separately trusted public key
when verifying identity—the card read establishes which key verifies the signature, not who owns the device.

```csharp
static ECParameters DecodeP256PublicKey(ReadOnlyMemory<byte> template)
{
    using var outer = Tlv.Create(template.Span);
    if (outer.Tag != 0x7F49)
        throw new CryptographicException("Expected a card public-key template.");
    var point = TlvHelper.DecodeDictionary(outer.Value.Span)[0x86];
    if (point.Length != 65 || point.Span[0] != 0x04)
        throw new CryptographicException("Expected an uncompressed P-256 point.");
    return new ECParameters
    {
        Curve = ECCurve.NamedCurves.nistP256,
        Q = new ECPoint { X = point.Slice(1, 32).ToArray(), Y = point.Slice(33, 32).ToArray() }
    };
}

static async Task<bool> SignAndVerifyP256Async(
    IYubiKey device, ReadOnlyMemory<byte> userPin, ReadOnlyMemory<byte> message,
    CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    var attributes = await session.GetAlgorithmAttributesAsync(KeyRef.Sig, cancellationToken);
    if (attributes is not EcAttributes { Oid: CurveOid.Secp256R1 })
        throw new InvalidOperationException("Provision a P-256 signing key first.");
    using var verifier = ECDsa.Create(DecodeP256PublicKey(
        await session.GetPublicKeyAsync(KeyRef.Sig, cancellationToken)));
    await session.VerifyPinAsync(userPin, cancellationToken: cancellationToken);
    ReadOnlyMemory<byte> signature = await session.SignAsync(message, HashAlgorithmName.SHA256, cancellationToken);
    try
    {
        return verifier.VerifyData(message.Span, signature.Span, HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(signature).Span);
    }
}
```

`PinPolicy.Always` requires User PIN verification for each signature; `Once` retains authorization until the card
access state ends. `UnverifyPinAsync` clears both User modes using the SDK's 5.6.0 check. For `AuthenticateAsync`,
verify with `extended: true`, then pass your challenge and verify the result with the Authentication public key.
Apply the current distinct-salt KDF limitation described above to that route.

## Decipher an RSA block or derive an ECDH secret

`DecryptAsync` operates at the raw private-key layer. For RSA, supply a single modulus-sized RSAES-PKCS1-v1_5
ciphertext block; the card checks/removes padding and returns plaintext. For NIST-curve ECDH, supply the sender's
uncompressed point `04 || X || Y`; the SDK adds `A6 { 7F49 { 86 point } }` and returns raw shared-secret material.
The host performs shared-secret KDF, packet processing and content decryption. A complete `.gpg` file and an RSA OAEP
block are outside this contract. The current X25519 branch omits the required EC wrapper; use a qualified RSA/NIST
path until that implementation is corrected.

This composable raw-operation method accepts an application callback that consumes the result before cleanup.
The callback must finish using the memory before returning; any retained secret copy needs its own clearing scope.

```csharp
static async Task DecipherAndConsumeAsync(
    IYubiKey device, ReadOnlyMemory<byte> userPin, ReadOnlyMemory<byte> rawCiphertextOrPoint,
    Func<ReadOnlyMemory<byte>, CancellationToken, Task> consume,
    CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.VerifyPinAsync(userPin, extended: true, cancellationToken: cancellationToken);
    ReadOnlyMemory<byte> result = await session.DecryptAsync(rawCiphertextOrPoint, cancellationToken);
    try
    {
        await consume(result, cancellationToken);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(result).Span);
    }
}
```

The method owns this returned memory, so `MemoryMarshal.AsMemory` clears the actual backing storage.
Use that technique only for SDK outputs whose storage you own, rather than arbitrary borrowed read-only inputs.
Clearing a `ToArray()` copy would leave the original secret intact. The SDK clears its raw decipher response after
copying the successful result. RSA/EdDSA signatures instead transfer response storage to the caller; ECDSA returns
new DER storage and clears the raw response. Keep caller-owned result cleanup in both cases.

## Store certificates and attest a generated key

Certificates are independent public objects: storing one leaves the private key unchanged, and the host decides
whether its public key matches. Ordinary slot access uses the SDK's 5.2.0 certificate check. `GetCertificateAsync`
returns `null` for a successful empty read or ordinary-slot GET DATA status 6A88; selection errors and other failures
propagate. ATT uses its separate FC object and propagates every APDU failure.

This method borrows DER, owns both loaded/read-back certificate objects, and returns exported public bytes.
To remove a certificate instead, authenticate Admin and call `DeleteCertificateAsync(slot)`.

```csharp
static async Task<byte[]> StoreAndReadCertificateAsync(
    IYubiKey device, KeyRef slot, ReadOnlyMemory<byte> adminPin, ReadOnlyMemory<byte> certificateDer,
    CancellationToken cancellationToken)
{
    using var certificate = X509CertificateLoader.LoadCertificate(certificateDer.Span);
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.VerifyAdminAsync(adminPin, cancellationToken);
    await session.PutCertificateAsync(slot, certificate, cancellationToken);
    using var readBack = await session.GetCertificateAsync(slot, cancellationToken);
    return readBack?.RawData ?? throw new InvalidOperationException("Certificate was absent after writing.");
}
```

`AttestKeyAsync` asks ATT to certify a **generated** Sig/Dec/Aut key and overwrites that target's cardholder
certificate. Save any existing certificate first; restore it with Admin authentication if required. The following
method leaves the new attestation stored and returns its DER for an application-specific verifier.

```csharp
static async Task<byte[]> AttestGeneratedKeyAsync(
    IYubiKey device, KeyRef slot, ReadOnlyMemory<byte> userPin, CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.VerifyPinAsync(userPin, extended: false, cancellationToken: cancellationToken);
    using var attestation = await session.AttestKeyAsync(slot, cancellationToken);
    return attestation.RawData;
}
```

Ordinary User PIN verification is the vendor CLI/test route; ATT's UIF controls touch. Retrieval can fail after
the certificate overwrite. The SDK decodes X.509 but leaves public-key binding and trusted CA-chain validation to
the verifier. Mutable names, fingerprints and dates alone prove no provenance. See the vendor
[attestation extension](https://developers.yubico.com/PGP/Attestation.html) for verification and trust-store guidance.

## Configure touch and signature policy

Verify the Admin PIN before policy writes. This method chooses changeable `On` and per-signature PIN verification:

```csharp
static async Task ConfigureSigningPolicyAsync(
    IYubiKey device, ReadOnlyMemory<byte> adminPin, CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.VerifyAdminAsync(adminPin, cancellationToken);
    await session.SetUifAsync(KeyRef.Sig, Uif.On, cancellationToken);
    await session.SetSignaturePinPolicyAsync(PinPolicy.Always, cancellationToken);
    Console.WriteLine(await session.GetUifAsync(KeyRef.Sig, cancellationToken));
}
```

Choose fixed policy only with an explicit replacement/recovery plan. Ordinary fixed UIF clears on key
replacement/deletion or application reset. **Fixed ATT UIF survives reset and needs ATT key replacement**,
which loses factory private-key provenance; the current malformed ATT CRT also limits SDK provisioning.
The SDK's general UIF gate is 4.2.0; cached/ATT policies need additional support (the Python vendor implementation
uses 5.2.1 checks). `SetPinAttemptsAsync` likewise needs a device-specific plan: NEO can restore default PINs.
`SetAlgorithmAttributesAsync` can invalidate a key and leaves cached algorithms stale; explicitly refresh
application data before crypto use. Prefer generation/import methods for complete provisioning tasks.

Supply `SessionCreationOptions.UserPresencePrompt` when creating the session to display your application's touch
notification. Implement `IUserPresencePrompt` using your UI; the session retains the prompt but leaves its lifetime
with the caller. Sign/decrypt/authenticate use their slot's UIF; attestation uses ATT. `On`/`Fixed` report
`PolicyRequires`, cached policies report `PolicyMayRequire`, and `Off` stays silent. The session uses cached UIF
first, reads directly if absent, and falls back to `PolicyMayRequire` on a failed read other than cancellation.
Firmware without UIF support stays silent. See [user interaction](../../docs/usage/user-interaction.md).

Terminal notifications are `Completed`, `Cancelled` or `Failed`. Device APDU errors resolve as `Failed`; the SDK
currently leaves OpenPGP statuses, including 6600, outside its `TimedOut` classification. Cancellation can leave
an admitted SmartCard exchange or persistent command in progress. Await
completion/drain and inspect state through a fresh session before retrying a mutation; a cancelled token is no
rollback guarantee.

## Reset an application deliberately

Authorize reset only when loss of ordinary keys/certificates and configuration is acceptable. A successful device
reset restores the application's ordinary defaults. **ATT key/certificate and fixed ATT policy survive.** Saving a
public ATT certificate preserves that certificate, not a recoverable factory private key or manufacturer trust.

`ResetAsync` attempts PIN blocking, TERMINATE, ACTIVATE and state refresh, using the SDK's 1.0.6 check. Its current
implementation ignores TERMINATE/ACTIVATE statuses and checks no complete erasure postcondition. A normal return
alone proves too little: independently inspect key status, certificates, PIN acceptance/retries, KDF and UIF afterward.
Cancellation/failure can leave blocked PINs or partial state. This method performs the attempted reset and returns
fresh application metadata for part of that inspection; your application supplies authorization and full verification.

```csharp
static async Task<ApplicationRelatedData> ResetAndInspectAsync(
    IYubiKey device, CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.ResetAsync(cancellationToken);
    return await session.GetApplicationRelatedDataAsync(cancellationToken);
}
```

## Example tool and further reading

Run the interactive sample from the repository root:

```bash
dotnet run --project src/OpenPgp/examples/OpenPgpTool/OpenPgpTool.csproj
```

Treat OpenPgpTool as a sample with current limitations. Its attestation `--pin` option is declared but unused;
use the direct SDK attestation method above to verify the User PIN explicitly. `info` infers key presence from
fingerprint length, so use `ListKeyInformationAsync` for state inspection. Command-line PINs and PEM text live in
immutable strings, even when temporary byte buffers are cleared. Noninteractive device selection takes the first
SmartCard device, and the tool installs no touch-notification prompt. Certificate command results also need improved
disposal. Use application-owned input buffers, explicit device selection, complete resource scopes and your UI with
the direct SDK for those responsibilities. The sample's reset/fixed-policy help strings overstate reset scope;
apply the ATT exception described above.

- [Core guide](../Core/README.md): discovery, connections, APDU and TLV.
- [PIV guide](../Piv/README.md) and [Management guide](../Management/README.md).
- [OpenPGP Card specification](https://gnupg.org/ftp/specs/OpenPGP-smart-card-application-3.4.pdf):
  *Functional Specification of the OpenPGP application on ISO Smart Card Operating Systems*, Version **3.4.1,
  March 18, 2020** (the URL retains “3.4”). Relevant clauses: §4.3.2 KDF, §4.4.3.9 attributes, §§7.2.2–4 PINs,
  §§7.2.10–11 crypto, §7.2.14 key generation and §§7.2.16–17 lifecycle.
- [RFC 9580 §3.7.1.3](https://www.rfc-editor.org/rfc/rfc9580.html#section-3.7.1.3): S2K primitive;
  [RFC 8017](https://www.rfc-editor.org/rfc/rfc8017.html): RSA encodings;
  [RFC 8032](https://www.rfc-editor.org/rfc/rfc8032.html): Ed25519.
- [Vendor touch/reset guidance](https://docs.yubico.com/software/yubikey/tools/ykman/OpenPGP_Commands.html).
- [Developer guide](../../docs/DEV-GUIDE.md): build, test and contribution workflow.
