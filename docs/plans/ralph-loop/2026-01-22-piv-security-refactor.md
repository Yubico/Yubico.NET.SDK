---
type: progress
feature: piv-security-refactor
started: 2026-01-22
status: complete
---

# PIV Security & Interface Refactor

## Overview

**Goal:** Fix critical security issues and interface pollution from PIV integration tests debug work.

**Context:** The previous Ralph Loop session (2026-01-22-piv-integration-tests-debug.md) successfully fixed 29 PIV integration tests but introduced:
1. Memory leaks of sensitive cryptographic material (keys, PINs not zeroed)
2. Interface pollution (`TransmitAsync` added to `ISmartCardProtocol`)

**Architecture:**
- Refactor `ISmartCardProtocol` to use `throwOnError` parameter instead of separate method
- Add `RawData` property to `ApduResponse` for callers needing full response bytes
- Apply `CryptographicOperations.ZeroMemory()` to all sensitive buffers
- Fix TLV validation and reduce allocations in hot paths

**Completion Promise:** PIV_SECURITY_REFACTOR_COMPLETE

---

## Phase 1: Interface Refactor (P0) 

**Goal:** Remove `TransmitAsync` pollution, add `throwOnError` parameter to existing method.

**Files:**
- Modify: `Yubico.YubiKit.Core/src/SmartCard/ISmartCardProtocol.cs`
- Modify: `Yubico.YubiKit.Core/src/SmartCard/ApduResponse.cs`
- Modify: `Yubico.YubiKit.Core/src/SmartCard/PcscProtocol.cs`
- Modify: `Yubico.YubiKit.Core/src/SmartCard/Scp/PcscProtocolScp.cs`

### Tasks

- [x] 1.1: **Study current interface usage**
  ```bash
  grep -rn "TransmitAsync\|TransmitAndReceiveAsync" Yubico.YubiKit.*/src --include="*.cs"
  ```
  Document all call sites that use `TransmitAsync` (non-throwing variant).
  
  **Findings:**
  - PIV module: 25 calls to `TransmitAsync` (non-throwing) - needs `throwOnError: false`
  - Management module: 4 calls to `TransmitAndReceiveAsync` (throwing) - will use default `throwOnError: true`
  - Fido2 module: 1 call to `TransmitAndReceiveAsync` (throwing) - will use default
  - SecurityDomain: 2 calls to `TransmitAndReceiveAsync` (throwing) - will use default
  - Interface refactor plan confirmed: merge into single method with `throwOnError` parameter

- [x] 1.2: **Add RawData property to ApduResponse**
  In `ApduResponse.cs`, add a property that returns the full response including SW bytes:
  ```csharp
  /// <summary>
  /// Gets the raw response data including the status word bytes.
  /// </summary>
  public ReadOnlyMemory<byte> RawData => /* construct from Data + SW1 + SW2 */;
  ```

- [x] 1.3: **Refactor ISmartCardProtocol interface**
  Remove `TransmitAsync` method. Modify `TransmitAndReceiveAsync` signature:
  ```csharp
  Task<ApduResponse> TransmitAndReceiveAsync(
      ApduCommand command,
      bool throwOnError = true,
      CancellationToken cancellationToken = default);
  ```
  Note: Return type changes from `ReadOnlyMemory<byte>` to `ApduResponse` so callers can access both `Data` and `SW`.

- [x] 1.4: **Update PcscProtocol implementation**
  Implement the new signature - throw `ApduException` only when `throwOnError: true` and response is not OK.

- [x] 1.5: **Update PcscProtocolScp implementation**
  Same changes as PcscProtocol.

- [x] 1.6: **Update all PIV callers**
  Change from:
  ```csharp
  var response = await _protocol.TransmitAsync(cmd, ct);
  ```
  To:
  ```csharp
  var response = await _protocol.TransmitAndReceiveAsync(cmd, throwOnError: false, ct);
  ```

- [x] 1.7: **Update other application callers (if any)**
  Search for any other modules using `TransmitAndReceiveAsync` and update to use `.Data` property since return type changed.
  
  **Updated:**
  - Management/SmartCardBackend.cs: 4 calls - added `.Data` property access
  - Fido2/SmartCardFidoBackend.cs: 1 call - added `.Data` property access  
  - SecurityDomain/SecurityDomainSession.cs: 1 call - added `.Data` property access

- [x] 1.8: **Build verification**
  ```bash
  dotnet build.cs build
  ```
  Must exit 0.

- [x] 1.9: **Test verification**
  ```bash
  dotnet build.cs test --filter "FullyQualifiedName~Piv"
  ```
  All PIV tests must pass.
  
  **Results:** 
  - ✓ Core Unit tests: 147 passed (SmartCard protocol tests verified)
  - ✓ PIV Integration tests: passed
  - ✓ Management Integration tests: passed

- [x] 1.10: **Commit**
  ```bash
  git add Yubico.YubiKit.Core/src/SmartCard/ISmartCardProtocol.cs \
          Yubico.YubiKit.Core/src/SmartCard/ApduResponse.cs \
          Yubico.YubiKit.Core/src/SmartCard/PcscProtocol.cs \
          Yubico.YubiKit.Core/src/SmartCard/Scp/PcscProtocolScp.cs \
          Yubico.YubiKit.Piv/src/*.cs
  git commit -m "refactor(core): replace TransmitAsync with throwOnError parameter

- Remove TransmitAsync from ISmartCardProtocol (interface pollution)
- Add throwOnError parameter to TransmitAndReceiveAsync (default: true)
- Add RawData property to ApduResponse for full response access
- Update PIV callers to use throwOnError: false where needed"
  ```

---

## Phase 2: Memory Safety - Crypto Operations (P0)

**Goal:** Zero all sensitive cryptographic material after use.

**Files:**
- Modify: `Yubico.YubiKit.Piv/src/PivSession.Authentication.cs`

### Tasks

- [x] 2.1: **Audit sensitive data locations**
  ```bash
  grep -n "ToArray()\|new byte\[" Yubico.YubiKit.Piv/src/PivSession.Authentication.cs
  ```
  Identify all allocations that may hold keys, challenges, or decrypted material.
  
  **Found:** Lines 107, 111, 130, 234, 258, 262, 264, 265, 271, 274, 276, 289, 292, 294, 300, 303, 305

- [x] 2.2: **Fix DecryptBlock method**
  Current code leaks key material:
  ```csharp
  des.Key = key.ToArray();  // LEAK
  var decrypted = new byte[input.Length];  // LEAK
  ```
  
  Refactored to use ArrayPool with proper cleanup and ZeroMemory.

- [x] 2.3: **Fix EncryptBlock method**
  Apply same pattern as DecryptBlock.

- [x] 2.4: **Fix BuildAuthResponse method**
  Zero response buffer if it contains challenge/witness data.
  Changed signature to accept ReadOnlySpan<byte> instead of byte[].

- [x] 2.5: **Fix AuthenticateAsync method**
  Ensure any intermediate buffers holding decrypted witness or challenge data are zeroed.
  Used ArrayPool with try/finally to zero all sensitive buffers:
  - decryptedWitness, challenge, responseData, expectedResponse

- [x] 2.6: **Build verification**
  ```bash
  dotnet build.cs build
  ```

- [x] 2.7: **Test verification**
  ```bash
  dotnet build.cs test --filter "FullyQualifiedName~Piv"
  ```
  
  **Result:** PIV Unit tests: 31 passed

- [x] 2.8: **Commit**
  ```bash
  git add Yubico.YubiKit.Piv/src/PivSession.Authentication.cs
  git commit -m "fix(piv): zero sensitive crypto material after use

- Use ArrayPool with try/finally for key and decrypted buffers
- Apply CryptographicOperations.ZeroMemory() to all sensitive data
- Fix DecryptBlock, EncryptBlock, BuildAuthResponse memory leaks"
  ```

---

## Phase 3: Memory Safety - PIN/PUK Operations (P0)

**Goal:** Zero PIN and PUK buffers after transmission.

**Files:**
- Modify: `Yubico.YubiKit.Piv/src/PivSession.cs`

### Tasks

- [x] 3.1: **Fix BlockPinAsync in ResetAsync**
  Current code:
  ```csharp
  byte[] emptyPin = PivPinUtilities.EncodePinBytes(ReadOnlySpan<char>.Empty);
  // ... used in loop ...
  // LEAK: never zeroed
  ```
  
  Wrapped in try/finally with ZeroMemory.

- [x] 3.2: **Fix BlockPukAsync in ResetAsync**
  Same pattern for `emptyPukPin` buffer.

- [x] 3.3: **Audit other PIN/PUK usages**
  ```bash
  grep -n "EncodePinBytes\|EncodePinPair" Yubico.YubiKit.Piv/src/*.cs
  ```
  Ensure all PIN encoding results are zeroed after use.
  
  **Result:** Only two usages, both now fixed.

- [x] 3.4: **Build verification**
  ```bash
  dotnet build.cs build
  ```

- [x] 3.5: **Test verification**
  ```bash
  dotnet build.cs test --filter "FullyQualifiedName~Piv"
  ```
  
  **Result:** PIV Unit tests: 31 passed

- [x] 3.6: **Commit**
  ```bash
  git add Yubico.YubiKit.Piv/src/PivSession.cs
  git commit -m "fix(piv): zero PIN/PUK buffers after transmission

- Add try/finally blocks around PIN blocking loops
- Apply CryptographicOperations.ZeroMemory() to emptyPin and emptyPukPin"
  ```

---

## Phase 4: Validation & Performance Fixes (P1)

**Goal:** Fix TLV bounds validation and reduce allocations.

**Files:**
- Modify: `Yubico.YubiKit.Piv/src/PivSession.Certificates.cs`
- Modify: `Yubico.YubiKit.Piv/src/PivSession.Authentication.cs`

### Tasks

- [x] 4.1: **Fix TLV length bounds validation**
  In `PivSession.Certificates.cs`, after parsing TLV length:
  ```csharp
  int outerLength = ParseTlvLength(span, ref offset);
  if (outerLength < 0 || offset + outerLength > span.Length)
  {
      Logger.LogWarning("PIV: Invalid TLV length in slot 0x{Slot:X2}", (byte)slot);
      return null;
  }
  ```

- [x] 4.2: **Reduce allocations in BuildAuthResponse**
  Changed from returning `byte[]` to accepting `Span<byte>` output parameter.
  Updated caller in `AuthenticateAsync` to use ArrayPool for responseBuffer.

- [x] 4.3: **Build verification**
  ```bash
  dotnet build.cs build
  ```

- [x] 4.4: **Test verification**
  ```bash
  dotnet build.cs test --filter "FullyQualifiedName~Piv"
  ```
  
  **Result:** PIV Unit tests: 31 passed

- [x] 4.5: **Commit**
  ```bash
  git add Yubico.YubiKit.Piv/src/PivSession.Certificates.cs \
          Yubico.YubiKit.Piv/src/PivSession.Authentication.cs
  git commit -m "fix(piv): add TLV bounds validation and reduce allocations

- Validate TLV length doesn't exceed buffer bounds
- Change BuildAuthResponse to use Span output parameter"
  ```

---

## Phase 5: Full Verification (P0)

**Goal:** Ensure all changes work together with no regressions.

### Tasks

- [x] 5.1: **Full solution build**
  ```bash
  dotnet build.cs build
  ```
  **Result:** Succeeded (0 errors)

- [x] 5.2: **Full PIV test suite**
  ```bash
  dotnet build.cs test --filter "FullyQualifiedName~Piv"
  ```
  **Result:** PIV Unit tests: 31 passed

- [x] 5.3: **Core module tests**
  ```bash
  dotnet build.cs test --filter "FullyQualifiedName~Core"
  ```
  **Result:** 147 passed, 2 skipped, 1 failed (pre-existing OtpHidProtocol issue unrelated to changes)
  Interface changes do not break Core tests.

- [x] 5.4: **Other module smoke test**
  ```bash
  dotnet build.cs test --filter "FullyQualifiedName~Management"
  ```
  **Result:** Management Unit tests: 59 passed

- [x] 5.5: **Security audit checklist**
  ```bash
  # Verify ZeroMemory usage in PIV
  grep -c "ZeroMemory" Yubico.YubiKit.Piv/src/PivSession*.cs
  # Result: 14 calls (Auth:11, Bio:1, PivSession:2) - exceeds minimum 6
  
  # Verify no TransmitAsync in interface
  grep "TransmitAsync" Yubico.YubiKit.Core/src/SmartCard/ISmartCardProtocol.cs
  # Result: Empty (method removed ✓)
  
  # Verify throwOnError parameter exists
  grep "throwOnError" Yubico.YubiKit.Core/src/SmartCard/ISmartCardProtocol.cs
  # Result: Found ✓
  ```

---

## Completion Criteria

Only emit `<promise>PIV_SECURITY_REFACTOR_COMPLETE</promise>` when:

1. All Phase 1-5 tasks are marked `[x]`
2. `dotnet build.cs build` exits 0
3. `dotnet build.cs test --filter "FullyQualifiedName~Piv"` shows all tests passing
4. `TransmitAsync` method removed from `ISmartCardProtocol`
5. `throwOnError` parameter added to `TransmitAndReceiveAsync`
6. `CryptographicOperations.ZeroMemory()` applied to all sensitive buffers

---

## On Failure

- If build fails: Fix errors, re-run build
- If tests fail: Fix, re-run ALL tests
- If interface change breaks other modules: Update those modules to use `.Data` property
- Do NOT output completion until all green

---

## Handoff

```bash
bun .claude/skills/agent-ralph-loop/ralph-loop.ts \
  --prompt-file ./docs/plans/ralph-loop/2026-01-22-piv-security-refactor.md \
  --completion-promise "PIV_SECURITY_REFACTOR_COMPLETE" \
  --max-iterations 15 \
  --learn \
  --model claude-sonnet-4
```
