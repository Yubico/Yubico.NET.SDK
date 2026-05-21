# Plan: Integration Tests for PIV and OATH Modules

## Summary
Write new integration test files for PIV (3 files) and OATH (1 file) modules, then verify compilation with `dotnet toolchain.cs build`.

## Research Complete
All source APIs and existing test patterns have been read and understood:
- **PIV**: `ImportKeyAsync(slot, IPrivateKey, pinPolicy, touchPolicy)` returns `Task<PivAlgorithm>`
- **PIV**: `GenerateKeyAsync(slot, algorithm, pinPolicy, touchPolicy)` returns `Task<IPublicKey>`
- **PIV**: `StoreCertificateAsync(slot, cert, compress)` - compression supported via `bool compress` param
- **PIV**: `DecryptAsync(slot, cipherText, padding)` - high-level decrypt with auto padding removal
- **PIV**: Key types: `RSAPrivateKey.CreateFromPkcs8()`, `ECPrivateKey.CreateFromPkcs8()`, `Curve25519PrivateKey.CreateFromPkcs8()`
- **PIV**: Pin policies: `PivPinPolicy.Never`, `.Once`, `.Always`
- **OATH**: `CredentialData` with `Name`, `OathType`, `HashAlgorithm`, `Secret`, `Issuer`, `Period`, `Digits`, `Counter`
- **OATH**: `OathHashAlgorithm.Sha1/.Sha256/.Sha512`
- **OATH**: `WithOathSessionAsync` extension for test state
- **OATH**: `CalculateCodeAsync(credential, timestamp?, ct)` returns `Code` with `.Value` string, `.ValidFrom`, `.ValidTo`
- **OATH**: `SetKeyAsync(key, ct)`, `ValidateAsync(key, ct)`, `ListCredentialsAsync(ct)`, `IsLocked`

## Files to Create

### 1. `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivImportTests.cs`
Tests:
- `ImportKeyAsync_Rsa2048_CanSignAndDecrypt` - Import RSA 2048, sign with PKCS#1 v1.5 padding, verify with software. Also encrypt with public key, use `DecryptAsync` to decrypt.
- `ImportKeyAsync_Rsa4096_CanSign` - Same for RSA 4096, gated to MinFirmware="5.7.0", marked Slow
- `ImportKeyAsync_EccP384_CanSign` - Import P-384 key, sign SHA-384 hash, verify
- `ImportKeyAsync_Ed25519_CanSign` - Import Ed25519 key (MinFirmware="5.7.0"), sign, verify format only (no .NET Ed25519 verify)

Pattern: Follow existing `PivKeyOperationsTests.ImportKeyAsync_EccP256_CanSign` exactly. Use `RSAPrivateKey.CreateFromPkcs8()`, `ECPrivateKey.CreateFromPkcs8()`, `Curve25519PrivateKey.CreateFromPkcs8()`.

### 2. `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivPolicyTests.cs`
Tests:
- `GenerateKeyAsync_WithPinPolicyAlways_RequiresPinForEachSign` - Generate ECC P-256 with `PivPinPolicy.Always`, verify PIN, sign once, verify PIN again, sign again. Both signatures should succeed.
- `GenerateKeyAsync_WithPinPolicyNever_DoesNotRequirePin` - Generate with `PivPinPolicy.Never`, sign without calling VerifyPinAsync.

### 3. `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCompressedCertTests.cs`
Tests:
- `StoreCertificateAsync_Compressed_RoundTrips` - Create self-signed cert (RSA 2048 for larger size), store with `compress: true`, retrieve, verify thumbprint matches.

### 4. `src/Oath/tests/Yubico.YubiKit.Oath.IntegrationTests/OathHashAlgorithmTests.cs`
Tests:
- `PutCredential_Sha256Totp_CalculateReturnsCode` - TOTP with SHA-256, calculate, verify 6-digit code
- `PutCredential_Sha512Totp_CalculateReturnsCode` - TOTP with SHA-512
- `PutCredential_Totp60SecondPeriod_CalculateReturnsCode` - Period = 60
- `PutCredential_Totp8Digits_CalculateReturns8DigitCode` - Digits = 8, verify code.Value.Length == 8
- `LockedSession_ListBlocked_ThrowsOrReturnsError` - Set key, create new session (locked), try ListCredentialsAsync, expect exception
- `LockedSession_CalculateBlocked_ThrowsOrReturnsError` - Same but with CalculateCodeAsync

## Compilation
After writing all 4 files, run `dotnet toolchain.cs build` and fix any errors until clean.

## Key Decisions
- For RSA import test decrypt: Use `DecryptAsync(slot, cipherText, RSAEncryptionPadding.Pkcs1)` instead of manual PKCS#1 padding parsing (it's the higher-level API)
- For Ed25519 import: Use `Curve25519PrivateKey.CreateFromPkcs8()`. Since .NET 10 doesn't support Ed25519 verification, just verify signature length
- For PinPolicy.Always: Need to verify PIN before EACH sign operation. The YubiKey requires a new VerifyPinAsync call for each sign with Always policy
- For locked OATH session: Use `state.Device.CreateOathSessionAsync()` directly (not WithOathSessionAsync which resets)
- For compressed cert: Use RSA 2048 key for software cert creation to get larger cert bytes
