# Plan: Integration Tests for OpenPGP and YubiHSM Modules

## Summary
Write new integration test files for the OpenPGP (3 files) and YubiHSM (1 file) modules, following existing patterns exactly. Compile-only -- no hardware execution.

## Research Complete
- Read all source APIs: `IOpenPgpSession`, `OpenPgpSession.Keys.cs`, `.Crypto.cs`, `.Config.cs`
- Read all model types: `PrivateKeyTemplate`, `RsaKeyTemplate`, `RsaCrtKeyTemplate`, `EcKeyTemplate`, `CurveOid`, `AlgorithmAttributes`, `RsaAttributes`, `EcAttributes`, `Kdf`, `KdfIterSaltedS2k`, `RsaSize`
- Read existing tests: `OpenPgpSessionTests.cs`, `HsmAuthSessionTests.cs`
- Read test extensions: `OpenPgpTestStateExtensions.cs`, `HsmAuthTestStateExtensions.cs`
- Read project files for both integration test projects
- Verified `TestCategories` constants: `Slow`, `RequiresUserPresence`, `Category`

## Key API Signatures Discovered

### OpenPGP Key Import
- `PutKeyAsync(KeyRef keyRef, PrivateKeyTemplate template, AlgorithmAttributes? attributes = null, ct)`
- `RsaCrtKeyTemplate(KeyRef, e, p, q, iqmp, dmp1, dmq1, n)` -- full CRT format
- `RsaKeyTemplate(KeyRef, e, p, q)` -- standard format
- `EcKeyTemplate(KeyRef, privateKey, publicKey?)` -- EC format
- Must set `AlgorithmAttributes` before import (or pass via `attributes` param)

### OpenPGP Crypto
- `SignAsync(ReadOnlyMemory<byte> message, HashAlgorithmName hashAlgorithm, ct)` -> `ReadOnlyMemory<byte>`
- `DecryptAsync(ReadOnlyMemory<byte> ciphertext, ct)` -> `ReadOnlyMemory<byte>`
- `AuthenticateAsync(ReadOnlyMemory<byte> data, HashAlgorithmName hashAlgorithm, ct)` -> `ReadOnlyMemory<byte>`

### OpenPGP Config
- `SetKdfAsync(Kdf kdf, ct)`
- `GetKdfAsync(ct)` -> `Kdf`
- `KdfIterSaltedS2k` with `{ HashAlgorithm, IterationCount, SaltUser, SaltAdmin, InitialHashUser, InitialHashAdmin }`

### YubiHSM Auth
- `PutCredentialAsymmetricAsync(managementKey, label, privateKey, credentialPassword, touchRequired, ct)`
- `CalculateSessionKeysAsymmetricAsync(label, context, credentialPassword, cardCryptogram?, ct)` -> `SessionKeys`
- `GetChallengeAsync(label, credentialPassword?, ct)` -> `ReadOnlyMemory<byte>`
- `GenerateCredentialAsymmetricAsync(managementKey, label, credentialPassword, touchRequired, ct)`

## Files to Create

### 1. `src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests/OpenPgpKeyImportTests.cs`

Tests:
1. **ImportRsaKey_2048_CanSign** -- Generate RSA 2048 in software via `RSA.Create(2048)`, extract CRT params, create `RsaCrtKeyTemplate`, import via `PutKeyAsync`, sign, verify signature is 256 bytes. Skip FW 4.2-4.3.5.
2. **ImportRsaKey_4096_CanSign** -- Same for RSA 4096. `[Trait(TestCategories.Category, TestCategories.Slow)]`, MinFirmware="5.2.0". Skip FW 4.2-4.3.5.
3. **ImportEcKey_P256_CanSign** -- Generate ECDSA P-256, extract private scalar + public point, create `EcKeyTemplate`, set `EcAttributes`, import, sign, verify signature length > 0. MinFirmware="5.2.0".
4. **ImportEcKey_P384_CanSign** -- Same for P-384. MinFirmware="5.2.0".
5. **ImportEd25519Key_CanSign** -- Ed25519 import. MinFirmware="5.2.0". NOTE: .NET doesn't have native Ed25519 key gen before .NET 9/10 -- will need to check availability. May use fixed test vector or conditional.
6. **ImportX25519Key_ForDecryption** -- X25519 to KeyRef.Dec. MinFirmware="5.2.0". Same consideration as Ed25519.

### 2. `src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests/OpenPgpDecryptTests.cs`

Tests:
1. **Decrypt_Rsa2048_ReturnsPlaintext** -- Generate RSA key on card (Dec slot), get public key, encrypt externally with RSA, decrypt on card. Skip FW 4.2-4.3.5.
2. **Decrypt_EcdhP256_DeriveSharedSecret** -- Generate EC P-256 on Dec slot, get public key, create ephemeral ECDH key pair, send ephemeral public to card via DecryptAsync, verify result. MinFirmware="5.2.0".

### 3. `src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests/OpenPgpAdvancedTests.cs`

Tests:
1. **GenerateX25519Key_Succeeds** -- X25519 for Dec. MinFirmware="5.2.0".
2. **GenerateRsaKey_3072_Succeeds** -- RSA 3072. `Slow` trait.
3. **GenerateRsaKey_4096_Succeeds** -- RSA 4096. `Slow` trait.
4. **SetupKdf_IterSaltedS2k_ThenVerifyPin** -- Setup KDF with random salts and iteration count, verify PIN works.

### 4. `src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.IntegrationTests/HsmAuthAsymmetricTests.cs`

Tests:
1. **PutAsymmetric_ImportEcKey_ListShowsCredential** -- Generate EC P-256 locally, import private key via `PutCredentialAsymmetricAsync`, list shows EcP256 algorithm. MinFirmware="5.6.0".
2. **CalculateSessionKeysAsymmetric_Returns48Bytes** -- Generate asymmetric cred on device, get EPK-OCE via `GetChallengeAsync`, use as context for `CalculateSessionKeysAsymmetricAsync`. MinFirmware="5.6.0".
3. **GetChallenge_Symmetric_ReturnsUniqueBytes** -- Store symmetric cred, call `GetChallengeAsync` twice, verify different results. MinFirmware="5.6.0".
4. **GetChallenge_Asymmetric_ReturnsUniqueBytes** -- Generate asymmetric cred, call `GetChallengeAsync` twice. MinFirmware="5.6.0".

## Implementation Notes

### RSA Key Import Pattern
```csharp
using var rsa = RSA.Create(2048);
var p = rsa.ExportParameters(includePrivateParameters: true);
// p.Exponent, p.P, p.Q, p.InverseQ, p.DP, p.DQ, p.Modulus
var template = new RsaCrtKeyTemplate(KeyRef.Sig, p.Exponent, p.P, p.Q, p.InverseQ, p.DP, p.DQ, p.Modulus);
var attributes = RsaAttributes.Create(RsaSize.Rsa2048, RsaImportFormat.CrtWithModulus);
await session.PutKeyAsync(KeyRef.Sig, template, attributes);
```

### EC Key Import Pattern
```csharp
using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
var p = ecdsa.ExportParameters(includePrivateParameters: true);
// p.D is private scalar, p.Q.X + p.Q.Y is public point
var pubPoint = new byte[1 + p.Q.X.Length + p.Q.Y.Length];
pubPoint[0] = 0x04; // uncompressed
p.Q.X.CopyTo(pubPoint, 1);
p.Q.Y.CopyTo(pubPoint, 1 + p.Q.X.Length);
var template = new EcKeyTemplate(KeyRef.Sig, p.D, pubPoint);
var attributes = EcAttributes.Create(KeyRef.Sig, CurveOid.Secp256R1);
await session.PutKeyAsync(KeyRef.Sig, template, attributes);
```

### Ed25519/X25519 Consideration
.NET 9+ has `EdDSA` support but it's limited. For .NET 10 target, check if we can use `EdDSA.Create()`. If not available, use a fixed 32-byte test vector for the private key since we only need to verify import succeeds, not verify signatures externally.

### RSA Decrypt Pattern
RSA public key from card is in OpenPGP TLV format. We need to parse it to get modulus+exponent, then use `RSA.Create()` to encrypt externally. The card returns PKCS#1 padded plaintext or raw plaintext depending on card capabilities.

### KDF Setup Pattern
```csharp
var salt = RandomNumberGenerator.GetBytes(8);
var kdf = new KdfIterSaltedS2k
{
    HashAlgorithm = KdfHashAlgorithm.Sha256,
    IterationCount = 100000,
    SaltUser = salt,
    SaltAdmin = RandomNumberGenerator.GetBytes(8),
};
// Need to also set InitialHashUser/InitialHashAdmin with pre-computed hashes
// of default PINs so existing PIN still works after KDF setup.
```

### RsaImportFormat Check
Need to verify `RsaImportFormat` enum values to pick the right one for CRT.

## Execution Steps

1. Check `RsaImportFormat` enum values
2. Write `OpenPgpKeyImportTests.cs`
3. Write `OpenPgpDecryptTests.cs`
4. Write `OpenPgpAdvancedTests.cs`
5. Write `HsmAuthAsymmetricTests.cs`
6. Run `dotnet build.cs build` to verify compilation
7. Fix any compilation errors
8. Re-verify build is clean
