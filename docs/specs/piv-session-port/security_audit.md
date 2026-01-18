# Security Audit Report

**PRD:** Port PivSession from Java yubikit-android to C# Yubico.YubiKit.Piv  
**Auditor:** security-auditor  
**Date:** 2026-01-18T00:00:00Z  
**Verdict:** PASS with WARNINGS

---

## Executive Summary

The PIV Session PRD demonstrates **excellent security awareness** with comprehensive requirements for sensitive data handling. All CRITICAL security requirements are met:
- ✅ Explicit zeroing requirements for all secrets (PINs, PUKs, management keys, private keys)
- ✅ No sensitive data logging (explicitly prohibited)
- ✅ Appropriate memory patterns (Span/Memory for secrets)
- ✅ Safe error messages (no sensitive state leakage)
- ✅ Secure authentication state tracking
- ✅ PIN retry counter handling

**2 WARN findings** require attention but do not block the PRD. No CRITICAL findings.

---

## Summary

| Severity | Count |
|----------|-------|
| CRITICAL | 0 |
| WARN | 2 |
| INFO | 3 |

**Overall:** PRD meets all critical security requirements. Warnings relate to defense-in-depth improvements (timing attack considerations, explicit temporary PIN zeroing).

---

## Sensitive Data Inventory

| Data Type | Identified In PRD | Handling Specified | Zeroing Required |
|-----------|-------------------|-------------------|------------------|
| Management Key | ✅ US-2 | ✅ SR-1, SR-3 | ✅ Explicit |
| PIN | ✅ US-3, US-10 | ✅ SR-1, SR-3 | ✅ Explicit |
| PUK | ✅ US-10 | ✅ SR-1, SR-3 | ✅ Explicit |
| Private Keys (import) | ✅ US-5 | ✅ SR-1 | ✅ Explicit |
| Temporary PIN (bio) | ✅ US-12 | ⚠️ Implicit | ⚠️ Not explicit |
| Challenge/Response | ✅ US-2 | ✅ Implicit in auth flow | ✅ Ephemeral |
| Biometric Templates | ✅ US-12 | ✅ Never leaves YubiKey | N/A |

---

## Findings

### WARN-001: Temporary PIN Zeroing Not Explicit

**Section:** US-12: Biometric Authentication  
**Concern:** Temporary PIN (16 bytes) returned from `VerifyUvAsync` should be explicitly zeroed after use, but this is not stated in the PRD.  
**Impact:** Low - Temporary PIN is time-limited and single-use, but could leak if not properly handled.  
**Recommendation:**

Add to US-12 Acceptance Criteria:
```markdown
- [ ] Zero temporary PIN after verification
- [ ] Document that temporary PIN must be passed directly to VerifyTemporaryPinAsync without intermediate storage
```

Add to SR-1:
```csharp
// Temporary PIN handling
ReadOnlyMemory<byte>? tempPin = await session.VerifyUvAsync(requestTemporaryPin: true);
if (tempPin.HasValue)
{
    try
    {
        await session.VerifyTemporaryPinAsync(tempPin.Value);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(tempPin.Value.Span);
    }
}
```

---

### WARN-002: PIN Verification Timing Attack Consideration

**Section:** US-3: Verify PIN  
**Concern:** No explicit guidance on timing attack resistance for PIN verification. The YubiKey itself is resistant, but SDK error handling could leak timing information through exception construction/logging.  
**Impact:** Low - Attack surface is constrained (physical device access + exception timing analysis).  
**Recommendation:**

Add to Security Requirements section:
```markdown
### SR-5: Constant-Time Error Handling
PIN verification failure paths SHOULD take similar execution time regardless of:
- Whether PIN is correct or incorrect
- Number of retries remaining
- Whether device is about to lock

Implementation notes:
- Avoid conditional logging based on retry count in hot path
- Construct `InvalidPinException` with pre-allocated message strings
- Do NOT perform expensive operations (string formatting, stack traces) differently based on PIN correctness
```

**Note:** This is defense-in-depth. The YubiKey itself handles PIN verification in constant time.

---

### INFO-001: Consider Touch Policy Audit Logging

**Section:** US-4: Generate Key Pair, US-5: Import Private Key  
**Observation:** Touch policies (Always, Cached) are critical for high-security keys but are not surfaced in audit events.  
**Suggestion:** Consider logging touch policy at key generation/import for audit trail purposes:
```csharp
_logger.LogInformation("Generated {Algorithm} key in slot {Slot} with PIN policy {PinPolicy} and Touch policy {TouchPolicy}",
    algorithm, slot, pinPolicy, touchPolicy);
```

**Benefit:** Enables security audits to verify that sensitive keys have appropriate touch requirements.

---

### INFO-002: Attestation Certificate Validation

**Section:** US-9: Key Attestation  
**Observation:** PRD correctly requires attestation but does not specify validation requirements for the attestation certificate chain.  
**Suggestion:** Add to US-9:
```markdown
**Acceptance Criteria:**
- [ ] Attestation certificate includes Yubico device certificate
- [ ] Attestation certificate serial number matches YubiKey serial
- [ ] Certificate signature verified against known Yubico intermediate CA
- [ ] Extension parsing for firmware version, slot, PIN/touch policies
```

**Benefit:** Prevents attestation bypass attacks where an attacker provides a self-signed certificate.

**Note:** This may be handled in a separate attestation validation library, but PRD should reference it.

---

### INFO-003: Reset Operation TOCTOU Risk

**Section:** US-15: Application Reset  
**Observation:** Reset requires blocking PIN and PUK by intentional failure attempts. There's a theoretical TOCTOU (time-of-check-time-of-use) race if another process verifies PIN between blocking attempts.  
**Suggestion:** Document known limitation:
```markdown
**Security Note:** Reset operation has a theoretical race condition if multiple processes access the YubiKey simultaneously. The SDK cannot prevent external processes from verifying PIN/PUK between blocking attempts. Applications requiring guaranteed reset should:
1. Acquire exclusive device access (application-level locking)
2. Perform reset operation
3. Release exclusive access
```

**Benefit:** Manages developer expectations for multi-process scenarios.

---

## Checklist Results

### Memory Safety

| Check | Result | Evidence |
|-------|--------|----------|
| Sensitive data zeroing specified | ✅ PASS | SR-1 explicitly requires `CryptographicOperations.ZeroMemory()` for PIN, PUK, management key, private keys |
| No string conversion of secrets | ✅ PASS | SR-3 uses `ReadOnlyMemory<char>` for PIN/PUK (not `string`), `ReadOnlyMemory<byte>` for keys |
| Span/Memory pattern for secrets | ✅ PASS | SR-3 shows `Span<byte>` with `stackalloc` for small buffers, `ArrayPool<byte>` for larger |
| ArrayPool buffers cleared before return | ✅ PASS | SR-3 example shows `CryptographicOperations.ZeroMemory()` in `finally` block |
| Exception safety (zeroing on error paths) | ✅ PASS | SR-3 explicitly uses `try/finally` pattern |

### YubiKey-Specific Security

| Check | Result | Evidence |
|-------|--------|----------|
| PIN retry behavior defined | ✅ PASS | US-3 maps SW codes to retry counts; US-10 provides `GetPinAttemptsAsync()` |
| PUK handling defined | ✅ PASS | US-10 covers PUK change and unblock operations |
| Touch policy configurable | ✅ PASS | US-4, US-5 include `PivTouchPolicy` parameter with Default/Never/Always/Cached |
| PIN policy configurable | ✅ PASS | US-4, US-5 include `PivPinPolicy` parameter with full enum range |
| Attestation validation | ⚠️ PARTIAL | US-9 requires attestation but doesn't specify chain validation (INFO-002) |
| Biometric data protection | ✅ PASS | US-12 notes fingerprint templates never persist outside YubiKey |
| Management key authentication required | ✅ PASS | US-4, US-5, US-8 explicitly require management key auth for privileged operations |

### OWASP Top 10 (SDK Context)

| Category | Check | Result | Notes |
|----------|-------|--------|-------|
| **Injection** | Input validation before APDU | ✅ PASS | SR-4 requires input length validation; slot/algorithm enums prevent injection |
| **Broken Auth** | Operations require proper auth | ✅ PASS | Management key (US-2), PIN (US-3) required for respective operations |
| **Sensitive Data** | Protected in memory/transit | ✅ PASS | SR-1, SR-3 enforce zeroing; no logging (SR-2) |
| **Broken Access** | Privilege separation | ✅ PASS | Management key vs PIN vs PUK distinct; metadata operations don't require auth |
| **Misconfig** | Secure defaults | ✅ PASS | Default PIN/PUK documented; PinPolicy.Default and TouchPolicy.Default defer to YubiKey firmware defaults |
| **Insecure Deser** | TLV parsing robustness | ⚠️ IMPLICIT | Appendix B documents TLV tags but doesn't specify validation requirements |
| **Logging** | No secret leakage | ✅ PASS | SR-2 explicitly prohibits logging PIN, PUK, keys; shows correct vs incorrect patterns |

### Error Handling

| Check | Result | Evidence |
|-------|--------|----------|
| Safe error messages (no sensitive data) | ✅ PASS | Exception types (`InvalidPinException`, `BadResponseException`) reveal only retry count, not PIN value |
| No internal state leakage | ✅ PASS | Error mapping table (US-3, APDU Status Word Handling) reveals only public information (retries, auth state) |
| Timing attack resistance | ⚠️ WARN | WARN-002 suggests constant-time error handling guidance |

---

## Authentication State Tracking Security

**Assessment:** ✅ SECURE

The PRD's authentication model is sound:

1. **Management Key Authentication:**
   - US-2: "Track authentication state in session"
   - Mutual challenge-response prevents replay attacks
   - Authentication is session-scoped (lost on connection close)

2. **PIN Verification:**
   - US-3: "Track current/max PIN attempts internally"
   - Metadata fallback for older devices (US-11)
   - Empty PIN verification for attempt checking without blocking

3. **Biometric Verification:**
   - US-12: Temporary PIN is time-limited by firmware
   - SDK doesn't need to track expiry (YubiKey handles it)

**Security Properties:**
- Authentication state cannot outlive the physical connection
- No persistent authentication tokens (prevents token theft)
- Retry counter prevents brute force
- Bio temporary PIN is single-use (YubiKey enforces)

---

## Memory Pattern Analysis

### ✅ Correct Patterns in PRD

**SR-3 Example (Management Key):**
```csharp
Span<byte> pinBytes = stackalloc byte[8];
// OR
byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(24);
try { ... }
finally 
{
    CryptographicOperations.ZeroMemory(rentedBuffer.AsSpan(0, 24));
    ArrayPool<byte>.Shared.Return(rentedBuffer);
}
```

**Analysis:**
- ✅ `Span<byte>` with `stackalloc` for small buffers (≤512 bytes)
- ✅ `ArrayPool<byte>` for larger buffers (management key 24/32 bytes)
- ✅ `CryptographicOperations.ZeroMemory()` in `finally` block
- ✅ Exception-safe cleanup

**Interface Design (US-16):**
```csharp
Task AuthenticateAsync(ReadOnlyMemory<byte> managementKey, CancellationToken cancellationToken = default);
Task VerifyPinAsync(ReadOnlyMemory<char> pin, CancellationToken cancellationToken = default);
```

**Analysis:**
- ✅ `ReadOnlyMemory<char>` for PIN (not `string` to avoid GC immutability)
- ✅ `ReadOnlyMemory<byte>` for management key
- ✅ Async-friendly (Memory<T> crosses await boundaries)
- ✅ Caller can use `stackalloc` and pass via `AsMemory()`

### Span<T> vs Memory<T> Decision Matrix

| Scenario | Type | Reason |
|----------|------|--------|
| PIN parameter (async) | `ReadOnlyMemory<char>` | ✅ Async boundary, immutable view |
| Key parameter (async) | `ReadOnlyMemory<byte>` | ✅ Async boundary, immutable view |
| Internal PIN buffer | `Span<byte>` | ✅ Sync encoding, stack allocation |
| APDU response parsing | `ReadOnlySpan<byte>` | ✅ Sync, zero-copy slicing |
| Temp crypto buffer | `ArrayPool<byte>` | ✅ >512 bytes, reusable |

**Verdict:** Memory patterns are optimal for security and performance.

---

## Risk Assessment

### Attack Surface Analysis

**Physical Device Required:** ✅ Mitigates remote attacks
- All operations require physical YubiKey
- Touch policy adds additional physical verification

**Credential Brute Force:** ✅ Mitigated
- PIN: 8 attempts (default), then blocked
- PUK: 3 attempts (default), then blocked
- Management key: Challenge-response (not guessable)

**Sensitive Data Leakage:** ✅ Mitigated
- Memory zeroing required (SR-1)
- No logging (SR-2)
- Immutable strings avoided (SR-3)

**Timing Attacks:** ⚠️ Low Risk (WARN-002)
- YubiKey performs constant-time PIN verification
- SDK error handling could be improved (defense-in-depth)

**Attestation Bypass:** ⚠️ Low Risk (INFO-002)
- Attestation required but chain validation not specified
- Likely handled in separate library

**TOCTOU (Reset):** ⚠️ Low Risk (INFO-003)
- Multi-process scenario only
- Application-level locking advised

### Overall Risk Level: **LOW**

All high-severity risks are mitigated. Remaining warnings are defense-in-depth improvements.

---

## Firmware-Specific Security Considerations

### ROCA Vulnerability (CVE-2017-15361)

**PRD Coverage:** ✅ EXCELLENT

```csharp
// US-4 Acceptance Criteria
- [ ] Block RSA generation on YubiKey 4.2.6-4.3.4 (ROCA vulnerability)

// Feature Gates
public static bool SupportsRsaGeneration(FirmwareVersion version) =>
    version < new FirmwareVersion(4, 2, 6) || version >= new FirmwareVersion(4, 3, 5);
```

**Analysis:** Explicitly prevents vulnerable RSA key generation. Correct version range.

### Feature-Gated Security

| Feature | Min Version | Security Implication |
|---------|-------------|---------------------|
| Metadata | 5.3+ | Enables `IsDefault` checks (detect factory credentials) |
| AES Mgmt Key | 5.4+ | Stronger than 3DES |
| Move/Delete Key | 5.7+ | Enables key lifecycle management |
| Attestation | 4.3+ | Proof of key provenance |

**Verdict:** Feature gates correctly prevent misuse on older firmware.

---

## Compliance Notes

### NIST SP 800-73 (PIV Standard)

**PRD Alignment:** ✅ COMPLIANT

- Signature slot (0x9C): PIN required for each use (US-4 notes this)
- PIN padding to 8 bytes with 0xFF (US-3)
- Certificate storage format (US-8: TLV with TAG 0x70, 0x71, 0xFE)

### FIPS 140 Considerations

**PRD Coverage:** ⚠️ PARTIAL

- US-4: "Generate RSA 1024-bit key (not on FIPS)" - Correct
- No mention of FIPS mode detection or enforcement

**Recommendation:** Add to feature gates or runtime checks:
```csharp
// Check if device is in FIPS mode (affects algorithm availability)
if (deviceInfo.IsFipsSeries && algorithm == PivAlgorithm.Rsa1024)
{
    throw new NotSupportedException("RSA 1024 not available in FIPS mode");
}
```

**Note:** This may be handled in `DeviceInfo` rather than PIV session. Verify in implementation phase.

---

## Verdict Justification

**PASS** - The PRD demonstrates exceptional security rigor:

1. **All CRITICAL requirements met:**
   - Sensitive data zeroing explicitly required (SR-1)
   - No sensitive data logging (SR-2)
   - Appropriate memory types (SR-3)
   - Input validation (SR-4)
   - Safe error messages (no leakage)

2. **YubiKey-specific security handled:**
   - PIN/PUK retry counters
   - Touch policies
   - Management key authentication
   - ROCA vulnerability mitigation

3. **OWASP concerns addressed:**
   - Input validation (enum types)
   - Authentication requirements (explicit per operation)
   - Sensitive data protection (zeroing, no logging)
   - Secure defaults (documented factory credentials)

**The 2 WARN findings are defense-in-depth improvements, not vulnerabilities.** They should be addressed during implementation review but do not block PRD approval.

**The 3 INFO findings are enhancements for audit logging, attestation validation, and edge case documentation.** They improve security posture but are not required for PASS verdict.

---

## Recommendations for Implementation Phase

1. **Priority 1 (Must Address):**
   - Implement temporary PIN zeroing (WARN-001)
   - Add constant-time error handling guidance (WARN-002)

2. **Priority 2 (Should Address):**
   - Add touch policy to audit logs (INFO-001)
   - Reference attestation validation library (INFO-002)
   - Document reset TOCTOU limitation (INFO-003)

3. **Priority 3 (Consider):**
   - Add FIPS mode algorithm enforcement
   - Add TLV parser fuzzing tests
   - Add timing attack regression tests

---

## Sign-Off

**Security Auditor:** security-auditor agent  
**Date:** 2026-01-18  
**Verdict:** ✅ PASS (with 2 warnings to address in implementation)

**Next Steps:**
1. Spec-writer reviews WARN-001, WARN-002 and updates PRD if needed
2. PRD proceeds to technical-validator for implementation feasibility
3. Security auditor reviews implementation PR against this audit report

---

## Appendix: Security Requirements Summary

### SR-1: Sensitive Data Zeroing ✅
All PIN, PUK, and management key data MUST be zeroed after use using `CryptographicOperations.ZeroMemory()`.

### SR-2: No Sensitive Data Logging ✅
NEVER log PIN values, PUK values, management keys, or private key material.

### SR-3: Memory Types for Secrets ✅
Use stack-allocated (`Span<byte>`) or pooled memory (`ArrayPool<byte>`) for sensitive data. Avoid allocating arrays.

### SR-4: Validate Input Lengths ✅
Always validate that provided keys/PINs match expected lengths before use.

### SR-5: Constant-Time Error Handling ⚠️ (WARN-002)
PIN verification failure paths SHOULD take similar execution time regardless of PIN correctness or retry count.

---

## Change Log

| Date | Change | Reason |
|------|--------|--------|
| 2026-01-18 | Initial audit | PRD ready for security review |
