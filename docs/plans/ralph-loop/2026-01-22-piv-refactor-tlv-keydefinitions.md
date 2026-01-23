---
type: progress
status: pending
created: 2026-01-22
feature: piv-refactor-tlv-keydefinitions
completion_promise: PIV_REFACTOR_COMPLETE
---

# PIV Module Refactor: Use Core Utilities (TLV & KeyDefinitions)

**Goal:** Refactor PIV module to use existing `Tlv`/`TlvHelper` utilities and `KeyDefinitions` instead of manual parsing and hardcoded magic numbers.

**Background:** Code review identified ~240 lines of duplicated TLV parsing code and 16+ hardcoded key size constants that should use existing Core utilities.

**Architecture:**
- Replace manual BER-TLV length parsing with `Tlv.Create()` and `TlvHelper.DecodeDictionary()`
- Replace hardcoded RSA/EC sizes (256, 128, 384, 512, 32, 64) with `KeyDefinitions.*`
- Maintain identical external behavior - this is a pure internal refactor

**Tech Stack:** C# 14, .NET 10, xUnit v3

---

## Phase 1: Test Refactoring - KeyDefinitions (Priority: P0)

**Goal:** Replace all hardcoded key sizes in PIV integration tests with `KeyDefinitions`.

**Files:**
- Modify: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs`

### Tasks

- [ ] 1.1: **Add using directive**
  Add at top of file:
  ```csharp
  using Yubico.YubiKit.Core.Cryptography;
  ```

- [ ] 1.2: **Replace RSA modulus sizes in CreatePkcs1v15SigningPadding calls**
  Replace hardcoded values:
  ```csharp
  // Line ~243: 256 → KeyDefinitions.RSA2048.LengthInBytes
  // Line ~276: 128 → KeyDefinitions.RSA1024.LengthInBytes
  // Line ~309: 384 → KeyDefinitions.RSA3072.LengthInBytes
  // Line ~342: 512 → KeyDefinitions.RSA4096.LengthInBytes
  ```

- [ ] 1.3: **Replace signature length assertions**
  Replace hardcoded values:
  ```csharp
  // Line ~250: Assert.Equal(256, ...) → Assert.Equal(KeyDefinitions.RSA2048.LengthInBytes, ...)
  // Line ~283: Assert.Equal(128, ...) → Assert.Equal(KeyDefinitions.RSA1024.LengthInBytes, ...)
  // Line ~316: Assert.Equal(384, ...) → Assert.Equal(KeyDefinitions.RSA3072.LengthInBytes, ...)
  // Line ~349: Assert.Equal(512, ...) → Assert.Equal(KeyDefinitions.RSA4096.LengthInBytes, ...)
  ```

- [ ] 1.4: **Replace EC curve size assertions**
  Replace hardcoded values:
  ```csharp
  // Line ~94: Assert.Equal(32, sharedSecret.Length) → Assert.Equal(KeyDefinitions.P256.LengthInBytes, ...)
  // Line ~124: Assert.Equal(64, signature.Length) → Assert.Equal(KeyDefinitions.Ed25519.LengthInBytes * 2, ...)
  // Line ~129, ~178: Assert.Equal(32, ...) → Assert.Equal(KeyDefinitions.Ed25519.LengthInBytes, ...)
  ```

- [ ] 1.5: **Remove redundant comments**
  Remove comments like `// 1024-bit RSA = 128 byte modulus` - the code now self-documents.

- [ ] 1.6: **Build verification**
  ```bash
  dotnet build.cs build
  ```
  Must exit 0.

- [ ] 1.7: **Test verification**
  ```bash
  dotnet build.cs test --filter "FullyQualifiedName~Piv"
  ```
  All tests must pass (or skip cleanly).

- [ ] 1.8: **Commit**
  ```bash
  git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs
  git commit -m "refactor(piv): use KeyDefinitions instead of hardcoded sizes in tests

  - Replace magic numbers (256, 128, 384, 512, 32, 64) with KeyDefinitions.*
  - Improves maintainability and consistency with SDK patterns
  - No behavioral changes"
  ```

---

## Phase 2: Refactor ParseCryptoResponse (Priority: P0)

**Goal:** Replace 65 lines of manual TLV parsing with `Tlv.Create()`.

**Files:**
- Modify: `Yubico.YubiKit.Piv/src/PivSession.Crypto.cs`

### Tasks

- [ ] 2.1: **Audit current implementation**
  ```bash
  grep -n "ParseCryptoResponse" Yubico.YubiKit.Piv/src/PivSession.Crypto.cs
  ```
  Identify lines 277-341 (manual 0x7C/0x82 parsing).

- [ ] 2.2: **Refactor ParseCryptoResponse**
  Replace manual parsing with:
  ```csharp
  private ReadOnlyMemory<byte> ParseCryptoResponse(ReadOnlyMemory<byte> data)
  {
      // Parse outer TLV (0x7C - Dynamic Auth Template)
      using var outer = Tlv.Create(data.Span);
      if (outer.Tag != 0x7C)
      {
          throw new ApduException("Invalid crypto response format");
      }

      // Parse inner TLV (0x82 - Response data)
      using var inner = Tlv.Create(outer.Value.Span);
      if (inner.Tag != 0x82)
      {
          throw new ApduException("Invalid crypto response - expected TAG 0x82");
      }

      return inner.Value;
  }
  ```

- [ ] 2.3: **Build verification**
  ```bash
  dotnet build.cs build
  ```
  Must exit 0.

- [ ] 2.4: **Test verification**
  ```bash
  dotnet build.cs test --filter "FullyQualifiedName~PivCrypto"
  ```
  All crypto tests must pass.

- [ ] 2.5: **Commit**
  ```bash
  git add Yubico.YubiKit.Piv/src/PivSession.Crypto.cs
  git commit -m "refactor(piv): use Tlv.Create in ParseCryptoResponse

  - Replace 65 lines of manual TLV parsing with Tlv utility
  - Cleaner, more maintainable code
  - No behavioral changes"
  ```

---

## Phase 3: Refactor ParsePublicKey (Priority: P0)

**Goal:** Replace manual 0x7F49 template parsing with `Tlv.Create()`.

**Files:**
- Modify: `Yubico.YubiKit.Piv/src/PivSession.KeyPairs.cs`

### Tasks

- [ ] 3.1: **Audit current implementation**
  ```bash
  grep -n "ParsePublicKey\|0x7F\|0x49" Yubico.YubiKit.Piv/src/PivSession.KeyPairs.cs
  ```
  Identify lines 411-455.

- [ ] 3.2: **Refactor ParsePublicKey**
  Replace manual parsing with:
  ```csharp
  private IPublicKey ParsePublicKey(ReadOnlyMemory<byte> data, PivAlgorithm algorithm)
  {
      // Parse 0x7F49 (Public key template) - Tlv handles 2-byte tags
      using var template = Tlv.Create(data.Span);
      if (template.Tag != 0x7F49)
      {
          throw new ApduException("Invalid public key response format");
      }

      var keyData = template.Value.Span;

      // Parse based on algorithm
      return algorithm switch
      {
          PivAlgorithm.EccP256 or PivAlgorithm.EccP384 => ParseEccPublicKey(keyData, algorithm),
          PivAlgorithm.Ed25519 or PivAlgorithm.X25519 => ParseCurve25519PublicKey(keyData, algorithm),
          PivAlgorithm.Rsa1024 or PivAlgorithm.Rsa2048 or PivAlgorithm.Rsa3072 or PivAlgorithm.Rsa4096 
              => ParseRsaPublicKey(keyData),
          _ => throw new NotSupportedException($"Unsupported algorithm: {algorithm}")
      };
  }
  ```

- [ ] 3.3: **Build verification**
  ```bash
  dotnet build.cs build
  ```
  Must exit 0.

- [ ] 3.4: **Test verification**
  ```bash
  dotnet build.cs test --filter "FullyQualifiedName~PivKeyOperations"
  ```
  All key operations tests must pass.

- [ ] 3.5: **Commit**
  ```bash
  git add Yubico.YubiKit.Piv/src/PivSession.KeyPairs.cs
  git commit -m "refactor(piv): use Tlv.Create in ParsePublicKey

  - Replace manual 0x7F49 template parsing with Tlv utility
  - Handles 2-byte tags correctly via Tlv.ParseData
  - No behavioral changes"
  ```

---

## Phase 4: Refactor Data Object Parsing (Priority: P0)

**Goal:** Replace manual 0x53 unwrapping with `Tlv.Create()`.

**Files:**
- Modify: `Yubico.YubiKit.Piv/src/PivSession.DataObjects.cs`

### Tasks

- [ ] 4.1: **Audit current implementation**
  ```bash
  grep -n "0x53\|ParseTlv" Yubico.YubiKit.Piv/src/PivSession.DataObjects.cs
  ```
  Identify lines 88-130.

- [ ] 4.2: **Refactor UnwrapDataObject**
  Replace manual parsing:
  ```csharp
  private ReadOnlyMemory<byte> UnwrapDataObject(ReadOnlyMemory<byte> data)
  {
      if (data.IsEmpty)
          return data;

      var span = data.Span;

      // Check for TAG 0x53 (Discretionary data)
      if (span[0] != 0x53)
      {
          // Not wrapped, return as-is
          return data;
      }

      // Use Tlv to parse the wrapper
      using var wrapper = Tlv.Create(span);
      return wrapper.Value;
  }
  ```

- [ ] 4.3: **Build verification**
  ```bash
  dotnet build.cs build
  ```
  Must exit 0.

- [ ] 4.4: **Test verification**
  ```bash
  dotnet build.cs test --filter "FullyQualifiedName~PivKeyOperations"
  ```
  Tests using data objects must pass.

- [ ] 4.5: **Commit**
  ```bash
  git add Yubico.YubiKit.Piv/src/PivSession.DataObjects.cs
  git commit -m "refactor(piv): use Tlv.Create in data object unwrapping

  - Replace manual 0x53 length parsing with Tlv utility
  - Consistent with other TLV parsing in SDK
  - No behavioral changes"
  ```

---

## Phase 5: Refactor Metadata Parsing (Priority: P1)

**Goal:** Replace manual while-loop TLV iteration with `TlvHelper.DecodeDictionary()`.

**Files:**
- Modify: `Yubico.YubiKit.Piv/src/PivSession.Metadata.cs`

### Tasks

- [ ] 5.1: **Audit current implementations**
  ```bash
  grep -n "while.*offset\|span\[offset" Yubico.YubiKit.Piv/src/PivSession.Metadata.cs
  ```
  Identify:
  - GetSlotMetadataAsync (lines ~65-122)
  - GetManagementKeyMetadataAsync (lines ~260-300)

- [ ] 5.2: **Refactor GetSlotMetadataAsync parsing**
  Replace manual while-loop with:
  ```csharp
  var tlvDict = TlvHelper.DecodeDictionary(span);

  var algorithm = tlvDict.TryGetValue(0x01, out var alg) && alg.Length > 0
      ? (PivAlgorithm)alg.Span[0]
      : PivAlgorithm.None;

  var (pinPolicy, touchPolicy) = tlvDict.TryGetValue(0x02, out var policy) && policy.Length >= 2
      ? ((PivPinPolicy)policy.Span[0], (PivTouchPolicy)policy.Span[1])
      : (PivPinPolicy.Default, PivTouchPolicy.Default);

  var isGenerated = tlvDict.TryGetValue(0x03, out var origin) && origin.Length > 0
      && origin.Span[0] == 0x01;

  ReadOnlyMemory<byte>? publicKey = tlvDict.TryGetValue(0x04, out var pk) ? pk : null;

  var isDefault = tlvDict.TryGetValue(0x05, out var def) && def.Length > 0
      && def.Span[0] == 0x01;
  ```

- [ ] 5.3: **Refactor GetManagementKeyMetadataAsync parsing**
  Apply same pattern for management key metadata TLV parsing.

- [ ] 5.4: **Build verification**
  ```bash
  dotnet build.cs build
  ```
  Must exit 0.

- [ ] 5.5: **Test verification**
  ```bash
  dotnet build.cs test --filter "FullyQualifiedName~PivMetadata"
  ```
  All metadata tests must pass.

- [ ] 5.6: **Commit**
  ```bash
  git add Yubico.YubiKit.Piv/src/PivSession.Metadata.cs
  git commit -m "refactor(piv): use TlvHelper.DecodeDictionary in metadata parsing

  - Replace manual while-loop TLV iteration with TlvHelper
  - Consistent with GetPinMetadataAsync which already uses TlvHelper
  - No behavioral changes"
  ```

---

## Phase 6: Refactor Authentication Response Parsing (Priority: P1)

**Goal:** Replace manual witness/challenge TLV parsing with `Tlv.Create()`.

**Files:**
- Modify: `Yubico.YubiKit.Piv/src/PivSession.Authentication.cs`

### Tasks

- [ ] 6.1: **Audit current implementations**
  ```bash
  grep -n "ParseWitnessResponse\|ParseChallengeResponse" Yubico.YubiKit.Piv/src/PivSession.Authentication.cs
  ```
  Identify lines ~188-220, ~225-260.

- [ ] 6.2: **Refactor ParseWitnessResponse**
  Replace manual parsing:
  ```csharp
  private static ReadOnlySpan<byte> ParseWitnessResponse(ReadOnlySpan<byte> response, int expectedLength)
  {
      using var outer = Tlv.Create(response);
      if (outer.Tag != 0x7C)
      {
          throw new ApduException($"Invalid witness response - expected TAG 0x7C, got 0x{outer.Tag:X2}");
      }

      using var inner = Tlv.Create(outer.Value.Span);
      if (inner.Tag != 0x80)
      {
          throw new ApduException($"Invalid witness response - expected TAG 0x80, got 0x{inner.Tag:X2}");
      }

      if (inner.Length != expectedLength)
      {
          throw new ApduException($"Invalid witness length - expected {expectedLength}, got {inner.Length}");
      }

      return inner.Value.Span;
  }
  ```

- [ ] 6.3: **Refactor ParseChallengeResponse**
  Apply same pattern (0x7C outer, 0x82 inner).

- [ ] 6.4: **Build verification**
  ```bash
  dotnet build.cs build
  ```
  Must exit 0.

- [ ] 6.5: **Test verification**
  ```bash
  dotnet build.cs test --filter "FullyQualifiedName~PivManagementKey"
  ```
  All management key tests must pass.

- [ ] 6.6: **Commit**
  ```bash
  git add Yubico.YubiKit.Piv/src/PivSession.Authentication.cs
  git commit -m "refactor(piv): use Tlv.Create in authentication response parsing

  - Replace manual 0x7C/0x80/0x82 parsing with Tlv utility
  - Cleaner error handling with Tlv
  - No behavioral changes"
  ```

---

## Phase 7: Refactor Certificate Parsing (Priority: P1)

**Goal:** Replace manual certificate TLV iteration with `TlvHelper.DecodeDictionary()`.

**Files:**
- Modify: `Yubico.YubiKit.Piv/src/PivSession.Certificates.cs`

### Tasks

- [ ] 7.1: **Audit current implementation**
  ```bash
  grep -n "while.*offset\|tag == 0x70\|tag == 0x71" Yubico.YubiKit.Piv/src/PivSession.Certificates.cs
  ```
  Identify certificate parsing loop (lines ~55-92).

- [ ] 7.2: **Refactor certificate parsing**
  Replace manual while-loop with:
  ```csharp
  var tlvDict = TlvHelper.DecodeDictionary(certData.Span);

  byte[]? certBytes = tlvDict.TryGetValue(0x70, out var cert) 
      ? cert.ToArray() 
      : null;

  bool isCompressed = tlvDict.TryGetValue(0x71, out var info) 
      && info.Length > 0 
      && info.Span[0] == 0x01;

  if (certBytes == null || certBytes.Length == 0)
  {
      return null;
  }
  ```

- [ ] 7.3: **Remove ParseTlvLength helper** (if now unused)
  Check if `ParseTlvLength` is used elsewhere; if not, remove it.

- [ ] 7.4: **Build verification**
  ```bash
  dotnet build.cs build
  ```
  Must exit 0.

- [ ] 7.5: **Test verification**
  ```bash
  dotnet build.cs test --filter "FullyQualifiedName~Piv"
  ```
  All PIV tests must pass.

- [ ] 7.6: **Commit**
  ```bash
  git add Yubico.YubiKit.Piv/src/PivSession.Certificates.cs
  git commit -m "refactor(piv): use TlvHelper.DecodeDictionary in certificate parsing

  - Replace manual TLV iteration with TlvHelper
  - Remove redundant ParseTlvLength helper if unused
  - No behavioral changes"
  ```

---

## Phase 8: Final Verification (Priority: P0)

**Goal:** Ensure all refactoring is complete and no regressions introduced.

### Tasks

- [ ] 8.1: **Full build verification**
  ```bash
  dotnet build.cs build
  ```
  Must exit 0 with no new warnings.

- [ ] 8.2: **Full test suite**
  ```bash
  dotnet build.cs test
  ```
  All tests must pass (or skip cleanly with documented reasons).

- [ ] 8.3: **Verify no remaining manual TLV parsing**
  ```bash
  # Should return minimal/no results in PIV src (only encoding, not parsing)
  grep -n "span\[offset\+\+\]\|if.*0x81\|if.*0x82.*==" Yubico.YubiKit.Piv/src/*.cs | grep -v "encoding\|build"
  ```
  Document any remaining manual parsing with justification.

- [ ] 8.4: **Verify no remaining hardcoded sizes in tests**
  ```bash
  grep -n "Assert.Equal(256\|Assert.Equal(128\|Assert.Equal(384\|Assert.Equal(512" Yubico.YubiKit.Piv/tests/**/*.cs
  ```
  Should return 0 matches.

- [ ] 8.5: **Commit history review**
  ```bash
  git log --oneline -10
  ```
  Verify one commit per phase with conventional format.

---

## Verification Requirements (MUST PASS BEFORE COMPLETION)

1. **Build:** `dotnet build.cs build` (must exit 0)
2. **Test:** `dotnet build.cs test` (all tests must pass or skip cleanly)
3. **No regressions:** Existing tests pass, behavior unchanged
4. **Grep verification:** No remaining manual TLV parsing patterns in PIV src

Only after ALL pass, output `<promise>PIV_REFACTOR_COMPLETE</promise>`.
If any fail, fix and re-verify.

---

## Security Verification (Crypto Code)

Since this touches cryptographic response parsing, verify:

```bash
# Ensure Tlv disposal (ZeroMemory on dispose)
grep -c "using var.*Tlv" Yubico.YubiKit.Piv/src/*.cs
# Expected: >= 6 (one per refactored method)

# Verify no plaintext secrets logged
grep -rn "Log.*response\|Log.*data" Yubico.YubiKit.Piv/src/*.cs | grep -v "length\|Length\|format"
# Expected: 0 matches with actual data values
```

---

## On Failure

- If build fails: Fix errors, re-run build
- If tests fail: Investigate, fix, re-run ALL tests
- If Tlv parsing differs from manual: Check edge cases (empty data, malformed TLV)
- Do NOT output completion until all green
