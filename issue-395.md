# Issue 395: YubiKeySignatureGenerator.DigestData Regression Fix

## Issue Summary

**GitHub Issue:** https://github.com/Yubico/Yubico.NET.SDK/issues/395

**Problem:** Commit `01d2a667e382e814ce323ed993423d44519c2808` introduced a regression in `YubiKeySignatureGenerator.DigestData` where `_algorithm.GetKeySizeBytes()` was incorrectly used instead of the hash digest size.

**Symptoms:**
- Running PivSampleCode with RSA2048 key + SHA256 throws `ArgumentException` from `Array.Copy`
- The code tried to copy 256 bytes (RSA2048 key size) from a 32-byte array (SHA256 digest)

## Root Cause Analysis

In the buggy code:
```csharp
int bufferSize = _algorithm.GetKeySizeBytes();  // Returns 256 for RSA2048
byte[] digest = new byte[bufferSize];           // Creates 256-byte buffer
int offset = bufferSize - (digester.HashSize / 8);  // 256 - 32 = 224
// ...
Array.Copy(digester.Hash, 0, digest, offset, digest.Length);  // Tries to copy 256 bytes from 32-byte Hash!
```

The `GetKeySizeBytes()` returns the **cryptographic key size** (256 bytes for RSA2048), not the **hash digest size** (32 bytes for SHA256).

## Changes Made

### 1. Fixed DigestData method
**File:** `Yubico.YubiKey/examples/PivSampleCode/CertificateOperations/YubiKeySignatureGenerator.cs`

- Changed visibility from `private` to `public` (for testing)
- Replaced buggy implementation with call to `MessageDigestOperations.ComputeMessageDigest()`
- **For RSA keys:** Returns the raw digest directly (32/48/64 bytes for SHA256/384/512)
- **For ECC keys:** Pads digest to key size with leading zeros if needed, throws if digest is larger than key size

### 2. Added Unit Tests
**File:** `Yubico.YubiKey/tests/unit/Yubico/YubiKey/Sample/YubiKeySignatureGeneratorTests.cs`

Tests cover:
- RSA keys return correct digest sizes (SHA256→32, SHA384→48, SHA512→64 bytes)
- RSA2048+SHA256 fixed version does not throw
- RSA2048+SHA256 buggy version throws (regression test)
- ECC keys pad correctly (P-384+SHA256 → 48 bytes with 16 leading zeros)
- ECC keys throw when digest > key size (P-256+SHA384/512)

### 3. Updated Dev Container
**File:** `.devcontainer/devcontainer.json`

Added .NET 8.0 and .NET 10.0 as additional SDK versions to support running tests.

## Handoff Notes for Next Agent

### Testing Required
1. **Rebuild the dev container** to get .NET 8.0 runtime:
   - Run "Dev Containers: Rebuild Container" from VS Code command palette
   
2. **Run unit tests** after container rebuild:
   ```bash
   dotnet test Yubico.YubiKey/tests/unit/Yubico.YubiKey.UnitTests.csproj
   ```

3. **Manual integration test** (requires YubiKey):
   - Run PivSampleCode.exe
   - Option 12: GenerateKeyPair → Slot 9A → RSA 2048 → defaults
   - Option 18: GetCertRequest → Slot 9A
   - Should complete without exception

### Files Modified
- `.devcontainer/devcontainer.json` - Added .NET 8.0 and 10.0
- `Yubico.YubiKey/examples/PivSampleCode/CertificateOperations/YubiKeySignatureGenerator.cs` - Fixed DigestData
- `Yubico.YubiKey/tests/unit/Yubico/YubiKey/Sample/YubiKeySignatureGeneratorTests.cs` - New test file

### Branch
`fix/issue-395-digest-data-regression`

### Cleanup Notes
- The `DigestData` method was made `public` for testing purposes
- Consider reverting to `private` after tests pass, or keep public if useful for users
- The unit tests duplicate the digest logic since PivSampleCode can't be referenced (strong naming)

### Build Status
- ✅ Main SDK builds successfully
- ✅ PivSampleCode builds successfully  
- ⏳ Unit tests not yet run (need .NET 8.0 runtime in container)

## Related Files

- Bug report: https://github.com/Yubico/Yubico.NET.SDK/issues/395
- Breaking commit: `01d2a667e382e814ce323ed993423d44519c2808`
- MessageDigestOperations utility: `Yubico.YubiKey/examples/PivSampleCode/DotNetOperations/MessageDigestOperations.cs`
- KeyDefinitions (key sizes): `Yubico.YubiKey/src/Yubico/YubiKey/Cryptography/KeyDefinitions.cs`
