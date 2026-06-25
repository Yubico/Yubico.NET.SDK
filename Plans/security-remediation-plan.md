# Security Remediation Plan: Sensitive Data Handling

**Date:** 2026-04-02
**Scope:** All modules under `src/`
**Reference:** [Yubico Sensitive Data Best Practices](https://docs.yubico.com/yesdk/users-manual/sdk-programming-guide/sensitive-data.html)
**Audit Methodology:** 3 parallel security review agents (buffer clearing, logging leaks, memory patterns)

---

## Executive Summary

A comprehensive security audit of the Yubico.NET.SDK codebase identified **31 findings** across 4 severity levels. The most critical class of vulnerability is **38 `Console.WriteLine()` statements in the SCP implementation** that unconditionally dump session encryption keys, cryptograms, MAC chains, and plaintext APDU payloads to stdout. These cannot be disabled via logging configuration and represent a complete compromise of SCP03 secure channel confidentiality.

Secondary findings include unzeroed PIN buffers in FIDO2, `.ToArray()` creating untracked copies of sensitive key material, missing `IDisposable` on `ScpState`, and ILogger trace calls that hex-dump plaintext payloads.

| Severity | Count | Category |
|----------|-------|----------|
| CRITICAL | 6 | Console.WriteLine key dumps, plaintext logging |
| HIGH | 10 | Unzeroed PIN buffers, .ToArray() key copies, missing IDisposable |
| MEDIUM | 9 | Logger hex dumps, PIN length leaks, pinning gaps |
| LOW | 6 | Minor improvements, defense-in-depth |

---

## Phase 1: CRITICAL - Remove Debug Key Logging (Sprint 1)

**Estimated effort:** 1-2 hours
**Risk if unaddressed:** Complete SCP03 session compromise via log capture

### Task 1.1: Remove all Console.WriteLine from SCP implementation

All `Console.WriteLine` statements MUST be removed (not commented out, not wrapped in `#if DEBUG`). These dump cryptographic material that defeats the entire purpose of SCP03.

**Files and line ranges:**

| File | Lines | What's leaked |
|------|-------|---------------|
| `src/Core/src/SmartCard/Scp/ScpState.Scp03.cs` | 53-78 | S-ENC, S-MAC, S-RMAC session keys, host/card challenges, cryptograms |
| `src/Core/src/SmartCard/Scp/ScpState.cs` | 137-147 | MAC chain (full 16 bytes), C-MAC, MAC input data |
| `src/Core/src/SmartCard/Scp/ScpProcessor.cs` | 102-147 | Original APDU plaintext, encrypted data, response data, MAC values |
| `src/Core/src/SmartCard/Scp/StaticKeys.cs` | 166-176 | Key derivation inputs (key + derivation data), derived MAC |

**Action:** Delete every `Console.WriteLine` line in these 4 files. Replace with metadata-only logger calls where operationally needed:

```csharp
// BEFORE (CRITICAL vulnerability):
Console.WriteLine($"[DEBUG] S-ENC: {Convert.ToHexString(sessionKeys.Senc)}");

// AFTER (safe):
logger?.LogDebug("SCP03 session keys derived for KVN 0x{Kvn:X2}", keyRef.Kvn);
```

### Task 1.2: Remove plaintext hex dumps from ILogger trace calls

**Files:**

| File | Lines | What's leaked |
|------|-------|---------------|
| `src/Core/src/SmartCard/Scp/ScpState.cs` | 42 | Plaintext command data via `LogTrace` |
| `src/Core/src/SmartCard/Scp/ScpState.cs` | 113 | Plaintext decrypted response via `LogTrace` |

**Action:** Replace with length-only logging:

```csharp
// BEFORE:
logger?.LogTrace("Plaintext data: {Data}", Convert.ToHexString(data));

// AFTER:
logger?.LogTrace("Encrypting {ByteCount} bytes of command data", data.Length);
```

---

## Phase 2: HIGH - Fix Unzeroed Sensitive Buffers (Sprint 1-2)

**Estimated effort:** 3-4 hours
**Risk if unaddressed:** PIN/key plaintext persists in managed heap, recoverable via memory dump

### Task 2.1: Zero PIN bytes in FIDO2 ClientPin

**File:** `src/Fido2/src/Pin/ClientPin.cs`

**Issue:** `PadPin()` (line 460) and `ComputePinHash()` (line 476) both call `Encoding.UTF8.GetBytes(pin)` creating a `pinBytes` array that is NEVER zeroed.

**Fix for `PadPin()`** (lines 457-471):
```csharp
private static byte[] PadPin(string pin)
{
    var pinBytes = Encoding.UTF8.GetBytes(pin);
    try
    {
        var padded = new byte[PinBlockSize];
        if (pinBytes.Length > PinBlockSize)
            throw new ArgumentException($"PIN UTF-8 encoding exceeds {PinBlockSize} bytes.", nameof(pin));
        pinBytes.CopyTo(padded.AsSpan());
        return padded;
    }
    finally
    {
        CryptographicOperations.ZeroMemory(pinBytes);
    }
}
```

**Fix for `ComputePinHash()`** (lines 473-479):
```csharp
private static byte[] ComputePinHash(string pin)
{
    var pinBytes = Encoding.UTF8.GetBytes(pin);
    try
    {
        var hash = SHA256.HashData(pinBytes);
        return hash.AsSpan(0, 16).ToArray();
    }
    finally
    {
        CryptographicOperations.ZeroMemory(pinBytes);
    }
}
```

### Task 2.2: Zero intermediate buffers in ClientPin async methods

**File:** `src/Fido2/src/Pin/ClientPin.cs`

In `ChangePinAsync()` (~line 207), `SetPinAsync()` (~line 149), `GetPinTokenAsync()`, and `GetPinUvAuthTokenUsingPinAsync()`:
- `pinHashEnc`, `newPinEnc`, and `message` buffers are never zeroed
- Add these to the existing `finally` blocks alongside `sharedSecret`

**Pattern:**
```csharp
finally
{
    CryptographicOperations.ZeroMemory(sharedSecret);
    CryptographicOperations.ZeroMemory(pinHashEnc);    // ADD
    CryptographicOperations.ZeroMemory(newPinEnc);      // ADD
    CryptographicOperations.ZeroMemory(message);        // ADD
}
```

### Task 2.3: Zero .ToArray() key copies in PinUvAuthProtocol V1/V2

**Files:**
- `src/Fido2/src/Pin/PinUvAuthProtocolV1.cs` (lines 173, 185, 214, 225)
- `src/Fido2/src/Pin/PinUvAuthProtocolV2.cs` (lines 207, 220, 258, 259, 267)

**Issue:** `aes.Key = key.ToArray()` creates an untracked copy of the shared secret. The `.ToArray()` result is assigned directly to `aes.Key` and never separately zeroed.

**Fix:** Store the copy, zero in finally:
```csharp
byte[]? aesKeyArray = null;
try
{
    aesKeyArray = key.ToArray();
    aes.Key = aesKeyArray;
    // ... encryption logic
}
finally
{
    if (aesKeyArray is not null)
        CryptographicOperations.ZeroMemory(aesKeyArray);
}
```

### Task 2.4: Zero .ToArray() key copy in PivSession.Authentication

**File:** `src/Piv/src/PivSession.Authentication.cs` (lines 321, 373)

**Issue:** `aes.Key = keyBuffer.AsSpan(0, key.Length).ToArray()` creates a heap copy. The source `keyBuffer` is zeroed in the outer finally, but the `.ToArray()` copy assigned to `aes.Key` is separate memory.

**Fix:** Same pattern as Task 2.3 — capture the `.ToArray()` result, zero it in finally.

### Task 2.5: Fix KdfNone returning unzeroed PIN bytes

**File:** `src/OpenPgp/src/Kdf.cs` (lines 84-85)

**Issue:** `KdfNone.Process()` returns `Encoding.UTF8.GetBytes(pin)` directly. Unlike `KdfIterSaltedS2k.Process()` which zeroes in finally, the None variant has no cleanup path.

**Fix:** Callers of `Kdf.Process()` must zero the returned bytes. Verify all call sites in `OpenPgpSession.Pin.cs` have try/finally with ZeroMemory on the result. If not, add them.

---

## Phase 3: HIGH - Implement IDisposable on ScpState (Sprint 2)

**Estimated effort:** 1-2 hours

### Task 3.1: Make ScpState implement IDisposable

**File:** `src/Core/src/SmartCard/Scp/ScpState.cs`

**Issue:** `ScpState` holds:
- `SessionKeys keys` (which IS `IDisposable` and contains S-ENC, S-MAC, S-RMAC)
- `byte[] _macChain` (sensitive MAC accumulator)
- Neither is disposed/zeroed when `ScpState` goes out of scope

**Fix:**
```csharp
internal partial class ScpState(SessionKeys keys, byte[] macChain, ILogger<ScpState>? logger = null) : IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        keys.Dispose();
        CryptographicOperations.ZeroMemory(_macChain);
    }
}
```

**Also:** Verify that all code paths that create `ScpState` instances use `using` or dispose properly.

---

## Phase 4: MEDIUM - Reduce Logger Hex Dumps (Sprint 2)

**Estimated effort:** 1-2 hours

### Task 4.1: Remove payload hex dumps from OTP HID logging

**File:** `src/Core/src/Hid/Otp/OtpHidProtocol.cs`

| Line | Current | Fix |
|------|---------|-----|
| 139 | `LogDebug("Status-only response: {Status}", Convert.ToHexString(status))` | Log status byte only, not full hex |
| 313, 323 | `LogTrace("Read feature report: {Report}", Convert.ToHexString(report.Span))` | Log length only |
| 332 | `LogTrace("Write feature report: {Report}", Convert.ToHexString(buffer.Span))` | Log length only |
| 364 | `LogTrace("Sending payload to slot 0x{Slot:X2}: {Payload}", slot, Convert.ToHexString(payload))` | Remove payload hex, keep slot |
| 376 | `LogTrace("Frame (70 bytes): {Frame}", Convert.ToHexString(frame))` | Remove frame hex |

### Task 4.2: Remove plaintext APDU logging from PcscProtocol

**File:** `src/Core/src/SmartCard/PcscProtocol.cs`

| Line | Current | Fix |
|------|---------|-----|
| 89 | `LogTrace("Selecting application ID: {ApplicationId}", Convert.ToHexString(applicationId.Span))` | OK for AID (public), but review if any sensitive select data |

### Task 4.3: Remove PIN length from PIV Bio logging

**File:** `src/Piv/src/PivSession.Bio.cs` (line 142)

```csharp
// BEFORE:
Logger.LogDebug("PIV: Biometric verification succeeded, temporary PIN retrieved (length={Length})", tempPin.Length);

// AFTER:
Logger.LogDebug("PIV: Biometric verification succeeded");
```

---

## Phase 5: MEDIUM - Memory Pinning for Sensitive Allocations (Sprint 3)

**Estimated effort:** 2-3 hours

### Task 5.1: Use pinned allocation for sensitive crypto buffers

Follow the excellent pattern already in `AesCmac.cs`:
```csharp
// GOOD EXAMPLE (already in codebase):
private readonly byte[] _keyBuffer = GC.AllocateUninitializedArray<byte>(Aes128KeyLength, pinned: true);
```

Apply to:
- `ScpState.cs` — `CbcEncrypt()` intermediate buffers
- `PinUvAuthProtocolV1/V2` — AES key and plaintext buffers
- Any new sensitive buffer allocations

### Task 5.2: Audit OATH credential secret handling

**File:** `src/Oath/src/OathSession.cs` (lines 207-258)

The `data` buffer in `PutCredentialAsync()` contains the credential secret in plaintext and is NOT zeroed after APDU transmission. Add `CryptographicOperations.ZeroMemory(data)` to the finally block.

**File:** `src/Oath/src/CredentialData.cs` (lines 225-228)

`GetProcessedSecret()` creates an intermediate `shortened` key array that is never zeroed. Add try/finally with ZeroMemory.

---

## Phase 6: LOW - Defense-in-Depth Improvements (Sprint 3+)

### Task 6.1: Override ToString() on AnswerToReset

**File:** `src/Core/src/SmartCard/AnswerToReset.cs` (line 36)

Current `ToString()` returns full ATR hex via `BitConverter.ToString()`, which exposes device identity if logged. Change to `$"ATR({_bytes.Length} bytes)"`.

### Task 6.2: Add static analysis rule for Console.WriteLine

Add a `.editorconfig` or Roslyn analyzer rule to flag `Console.WriteLine` in `src/` directories. This prevents future debug statements from leaking into production code.

### Task 6.3: Verify HsmAuthSession sensitive buffer handling

**File:** `src/YubiHsm/src/HsmAuthSession.cs` (line 189)

`Encoding.UTF8.GetBytes(password, buffer)` — verify the output buffer is zeroed after use.

---

## Verification Checklist

After all phases, run these verification commands:

```bash
# 1. No Console.WriteLine in production code
grep -rn "Console\.WriteLine" src/ --include="*.cs" | grep -v "test" | grep -v "Test"
# Expected: 0 matches

# 2. No sensitive hex dumps in logs
grep -rn "Convert\.ToHexString\|BitConverter\.ToString" src/ --include="*.cs" | grep -i "key\|pin\|secret\|mac\|encrypt\|decrypt\|password"
# Expected: 0 matches in non-test code

# 3. ZeroMemory usage count (should increase)
grep -rn "ZeroMemory" src/ --include="*.cs" | wc -l
# Expected: Higher than current baseline

# 4. IDisposable on ScpState
grep -n "IDisposable" src/Core/src/SmartCard/Scp/ScpState.cs
# Expected: 1 match
```

---

## Implementation Order for DevTeam Ship Loop

| Priority | Task | Files | Est. Hours |
|----------|------|-------|------------|
| P0 | 1.1 Remove Console.WriteLine from SCP | 4 files in Scp/ | 0.5 |
| P0 | 1.2 Remove LogTrace hex dumps in SCP | ScpState.cs | 0.25 |
| P1 | 2.1 Zero PIN bytes in ClientPin | ClientPin.cs | 0.5 |
| P1 | 2.2 Zero intermediate buffers in ClientPin | ClientPin.cs | 0.5 |
| P1 | 2.3 Zero .ToArray() copies in PinUvAuth V1/V2 | 2 files | 1.0 |
| P1 | 2.4 Zero .ToArray() copy in PivSession.Auth | PivSession.Authentication.cs | 0.5 |
| P1 | 2.5 Fix KdfNone unzeroed return | Kdf.cs + callers | 0.5 |
| P1 | 3.1 IDisposable on ScpState | ScpState.cs + callers | 1.0 |
| P2 | 4.1-4.3 Reduce logger hex dumps | OtpHidProtocol.cs, PivSession.Bio.cs | 1.0 |
| P3 | 5.1-5.2 Pinned allocations, OATH zeroing | Multiple | 2.0 |
| P4 | 6.1-6.3 Defense-in-depth | Multiple | 1.0 |
| **Total** | | | **~8.75 hours** |
