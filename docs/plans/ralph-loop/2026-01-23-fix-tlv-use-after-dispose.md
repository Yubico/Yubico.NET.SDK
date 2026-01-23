# Fix TLV Use-After-Dispose in PIV Module

**Goal:** Fix use-after-dispose bugs in PIV TLV parsing introduced by refactoring commit 3f06a955.

**Architecture:** The `Tlv.Dispose()` zeros internal `_bytes` buffer, but refactored code returns `inner.Value.Span` after the `using` block ends - resulting in all-zero data. Fix by removing `using` from `Tlv.Create()` calls since parsed response data is not sensitive.

**Completion Promise:** TLV_DISPOSE_FIXED

---

## Problem Summary

Commit `3f06a955633c3ea3d022a6c189fccbc5f42c8966` refactored manual TLV parsing to use `Tlv.Create()` with `using` blocks. However:

1. `Tlv.Create()` copies data to internal `_bytes` array
2. `Tlv.Dispose()` calls `CryptographicOperations.ZeroMemory(_bytes)`
3. Returning `inner.Value.Span` after `using` block returns dangling reference to zeroed memory

**Symptom:** All tests fail with "Management key authentication failed - challenge response: Security status not satisfied (SW=0x6982)" because witness/challenge data becomes all-zeros.

**Bug Pattern:**
```csharp
// ❌ BUG: Returns reference to zeroed memory
using var outer = Tlv.Create(response);
using var inner = Tlv.Create(outer.Value.Span);
return inner.Value.Span;  // ← _bytes already zeroed!
```

**Fix Pattern:**
```csharp
// ✅ FIX: Don't dispose - data is not sensitive
var outer = Tlv.Create(response);
var inner = Tlv.Create(outer.Value.Span);
return inner.Value.Span;  // ← _bytes still valid
```

---

## Phase 1: Fix PivSession.Authentication.cs (P0)

**Goal:** Fix ParseWitnessResponse and ParseChallengeResponse methods.

**Files:**
- Modify: `Yubico.YubiKit.Piv/src/PivSession.Authentication.cs`

### Tasks

- [ ] 1.1: **Fix ParseWitnessResponse** (lines ~189-209)
  
  Remove `using` from both Tlv.Create calls:
  ```csharp
  private static ReadOnlySpan<byte> ParseWitnessResponse(ReadOnlySpan<byte> response, int expectedLength)
  {
      var outer = Tlv.Create(response);
      if (outer.Tag != 0x7C)
      {
          throw new ApduException($"Invalid witness response - expected TAG 0x7C, got 0x{outer.Tag:X2}");
      }

      var inner = Tlv.Create(outer.Value.Span);
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

- [ ] 1.2: **Fix ParseChallengeResponse** (lines ~214-234)
  
  Remove `using` from both Tlv.Create calls:
  ```csharp
  private static ReadOnlySpan<byte> ParseChallengeResponse(ReadOnlySpan<byte> response, int expectedLength)
  {
      var outer = Tlv.Create(response);
      if (outer.Tag != 0x7C)
      {
          throw new ApduException($"Invalid challenge response - expected TAG 0x7C, got 0x{outer.Tag:X2}");
      }

      var inner = Tlv.Create(outer.Value.Span);
      if (inner.Tag != 0x82)
      {
          throw new ApduException($"Invalid challenge response - expected TAG 0x82, got 0x{inner.Tag:X2}");
      }

      if (inner.Length != expectedLength)
      {
          throw new ApduException($"Invalid challenge response length - expected {expectedLength}, got {inner.Length}");
      }

      return inner.Value.Span;
  }
  ```

- [ ] 1.3: **Build verification**
  ```bash
  dotnet build.cs build
  ```

- [ ] 1.4: **Test authentication**
  ```bash
  dotnet build.cs test --filter "FullyQualifiedName~PivAuthenticationTests"
  ```
  Expected: `AuthenticateAsync_WithDefaultKey_Succeeds` should PASS.

---

## Phase 2: Fix PivSession.Crypto.cs (P0)

**Goal:** Fix ParseCryptoResponse method.

**Files:**
- Modify: `Yubico.YubiKit.Piv/src/PivSession.Crypto.cs`

### Tasks

- [ ] 2.1: **Find and fix ParseCryptoResponse**
  
  Search for the method:
  ```bash
  grep -n "ParseCryptoResponse" Yubico.YubiKit.Piv/src/PivSession.Crypto.cs
  ```
  
  Remove `using` from both Tlv.Create calls:
  ```csharp
  private ReadOnlyMemory<byte> ParseCryptoResponse(ReadOnlyMemory<byte> data)
  {
      // Parse outer TLV (0x7C - Dynamic Auth Template)
      var outer = Tlv.Create(data.Span);
      if (outer.Tag != 0x7C)
      {
          throw new ApduException("Invalid crypto response format");
      }

      // Parse inner TLV (0x82 - Response data)
      var inner = Tlv.Create(outer.Value.Span);
      if (inner.Tag != 0x82)
      {
          throw new ApduException("Invalid crypto response - expected TAG 0x82");
      }

      return inner.Value;
  }
  ```

- [ ] 2.2: **Build verification**
  ```bash
  dotnet build.cs build
  ```

---

## Phase 3: Fix PivSession.KeyPairs.cs (P0)

**Goal:** Fix ParsePublicKey method.

**Files:**
- Modify: `Yubico.YubiKit.Piv/src/PivSession.KeyPairs.cs`

### Tasks

- [ ] 3.1: **Find and fix ParsePublicKey**
  
  Search for the method:
  ```bash
  grep -n "ParsePublicKey\|using var.*Tlv" Yubico.YubiKit.Piv/src/PivSession.KeyPairs.cs
  ```
  
  Remove `using` from Tlv.Create call. The method parses 0x7F49 template, then processes inner key data.

- [ ] 3.2: **Build verification**
  ```bash
  dotnet build.cs build
  ```

---

## Phase 4: Fix PivSession.DataObjects.cs (P0)

**Goal:** Fix UnwrapDataObjectResponse or similar TLV parsing.

**Files:**
- Modify: `Yubico.YubiKit.Piv/src/PivSession.DataObjects.cs`

### Tasks

- [ ] 4.1: **Find and fix TLV parsing**
  
  Search for affected patterns:
  ```bash
  grep -n "using var.*Tlv" Yubico.YubiKit.Piv/src/PivSession.DataObjects.cs
  ```
  
  Remove `using` from any Tlv.Create calls that return or use the Value after the block.

- [ ] 4.2: **Build verification**
  ```bash
  dotnet build.cs build
  ```

---

## Phase 5: Fix PivSession.Metadata.cs (P0)

**Goal:** Fix metadata parsing TLV usage.

**Files:**
- Modify: `Yubico.YubiKit.Piv/src/PivSession.Metadata.cs`

### Tasks

- [ ] 5.1: **Find and fix TLV parsing**
  
  Search for affected patterns:
  ```bash
  grep -n "using var.*Tlv\|Tlv.Create" Yubico.YubiKit.Piv/src/PivSession.Metadata.cs
  ```
  
  Note: This file may use `TlvHelper.DecodeDictionary` which is different - only fix `Tlv.Create` with `using`.

- [ ] 5.2: **Build verification**
  ```bash
  dotnet build.cs build
  ```

---

## Phase 6: Fix PivSession.Certificates.cs (P0)

**Goal:** Fix certificate parsing TLV usage.

**Files:**
- Modify: `Yubico.YubiKit.Piv/src/PivSession.Certificates.cs`

### Tasks

- [ ] 6.1: **Find and fix TLV parsing**
  
  Search for affected patterns:
  ```bash
  grep -n "using var.*Tlv\|Tlv.Create" Yubico.YubiKit.Piv/src/PivSession.Certificates.cs
  ```
  
  Note: This file may use `TlvHelper.DecodeDictionary` which is different - only fix `Tlv.Create` with `using`.

- [ ] 6.2: **Build verification**
  ```bash
  dotnet build.cs build
  ```

---

## Phase 7: Final Verification (P0)

**Goal:** Verify all PIV integration tests pass.

### Tasks

- [ ] 7.1: **Run all PIV integration tests**
  ```bash
  dotnet build.cs test --filter "FullyQualifiedName~Piv.IntegrationTests"
  ```
  
  Expected results:
  - `PivAuthenticationTests` - All PASS
  - `PivCryptoTests` - All PASS (or SKIP for unimplemented features)
  - `PivCertificateTests` - All PASS
  - `PivManagementKeyTests` - All PASS
  - `PivKeyOperationsTests` - All PASS (or SKIP for unimplemented features)
  - `PivMetadataTests` - All PASS
  
  **No test should FAIL** (SKIP is acceptable for unimplemented features).

- [ ] 7.2: **Commit fix**
  ```bash
  git add Yubico.YubiKit.Piv/src/PivSession.Authentication.cs \
          Yubico.YubiKit.Piv/src/PivSession.Crypto.cs \
          Yubico.YubiKit.Piv/src/PivSession.KeyPairs.cs \
          Yubico.YubiKit.Piv/src/PivSession.DataObjects.cs \
          Yubico.YubiKit.Piv/src/PivSession.Metadata.cs \
          Yubico.YubiKit.Piv/src/PivSession.Certificates.cs
  git commit -m "fix(piv): remove using from Tlv parsing to prevent use-after-dispose

  Tlv.Dispose() zeros internal buffer, causing returned Value spans
  to become invalid. Since parsed response data is not sensitive
  (encrypted data from device), removing using is safe.

  Fixes authentication failures introduced in 3f06a955."
  ```

---

## Verification Requirements (MUST PASS BEFORE COMPLETION)

1. **Build:** `dotnet build.cs build` (must exit 0)
2. **Auth test:** `AuthenticateAsync_WithDefaultKey_Succeeds` must PASS
3. **PIV integration tests:** No tests should FAIL (SKIP is acceptable)
4. **Commit:** Changes committed with descriptive message

Only after ALL pass, output `<promise>TLV_DISPOSE_FIXED</promise>`.
If any fail, fix and re-verify.

---

## On Failure

- If build fails: Check for typos in edits, fix and re-build
- If auth test still fails: Verify both `using` keywords removed from both methods
- If other tests fail: Check if those files also have `using var` Tlv patterns
- Do NOT output completion promise until all verification passes

---

## Notes

- The root cause is a pattern mismatch: `Tlv.Dispose()` zeros memory for security when *encoding* sensitive data to send, but response parsing doesn't need this
- Only remove `using` from `Tlv.Create()` - do NOT remove `using` from other resources
- `TlvHelper.DecodeDictionary` is a different pattern and may not have this issue
- YubiKey 5.7.4 with SmartCard connection is available for testing
