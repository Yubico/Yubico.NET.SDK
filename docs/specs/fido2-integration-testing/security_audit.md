# Security Audit Report

**PRD:** FIDO2 FidoSession Integration Testing Enhancement  
**Auditor:** security-auditor  
**Date:** 2026-01-18T03:00:00Z  
**Verdict:** PASS with WARNINGS

---

## Summary

| Severity | Count |
|----------|-------|
| CRITICAL | 0 |
| WARN | 5 |
| INFO | 3 |

**Overall:** The PRD proposes a well-structured integration testing framework with appropriate security considerations for test PIN handling and credential cleanup. However, several areas require explicit security specifications to prevent test-induced device lockouts, sensitive data leakage in test output, and incomplete cleanup operations. No CRITICAL vulnerabilities were identified, but the WARNINGS highlight gaps in security requirements that should be addressed before implementation.

---

## Sensitive Data Inventory

| Data Type | Identified In PRD | Handling Specified |
|-----------|-------------------|-------------------|
| Test PIN | ✅ Section 8.1 | ⚠️ Partial (value specified, but no zeroing requirement) |
| User handle (UserId) | ✅ Section 8.1 | ❌ No handling specified |
| Challenge bytes | ✅ Section 8.1 | ❌ No handling specified |
| Credential private keys | ✅ Implicit (MakeCredential) | ⚠️ Partial (cleanup required, but no explicit "never exposed" statement) |
| Authenticator data | ✅ Section 3.2 | ⚠️ Test parsing without security constraints |
| Assertion signatures | ✅ Section 3.2 | ❌ No handling for test output |

---

## Findings

### WARN-001: PIN Memory Management Not Specified
**Section:** 8.1 Shared Test Constants (FidoTestData class)  
**Concern:** The PRD specifies test PIN values (`"Abc12345"` and `"123456"`) as string constants but does not require zero-after-use for test PINs. While these are test PINs (not production secrets), setting a precedent of not zeroing PINs in test code could lead to developers forgetting to zero PINs in production code.

**Test-Specific Context:**  
Test PINs are known values and are already present in source code as string literals. However, when test code converts these to `byte[]` or `Span<byte>` for API calls, those byte representations should be zeroed after use to establish good security hygiene patterns.

**Impact:** Low-Medium. Test PIN exposure is low risk (publicly known values), but inconsistent security patterns in test code can lead to vulnerabilities in production code when developers copy-paste patterns.

**Recommendation:**  
Add to Section 4.2 (Security):
```markdown
### 4.2 Security

- Test PIN values must be zeroed after use when converted to byte arrays
  - `CryptographicOperations.ZeroMemory()` must be called on PIN byte representations
  - String constants are acceptable for test configuration
  - Byte conversions must follow production security patterns
```

Add to Section 8.2 (Test Utilities):
```csharp
// Example secure PIN handling pattern for tests
public static async Task SetOrVerifyPinAsync(
    this FidoSession session, 
    string pinString,
    CancellationToken ct = default)
{
    Span<byte> pinBytes = stackalloc byte[Encoding.UTF8.GetByteCount(pinString)];
    try
    {
        Encoding.UTF8.GetBytes(pinString, pinBytes);
        await session.VerifyPinAsync(pinBytes, ct);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(pinBytes);
    }
}
```

---

### WARN-002: PIN Retry Counter State Not Fully Specified
**Section:** 3.3 Error States (Unhappy Paths), 4.2 Security  
**Concern:** While Section 3.3 acknowledges PIN retry behavior and Section 4.2 states "Tests must not leave device in locked/blocked state," the PRD does not specify:
- How tests will monitor PIN retry counter
- What happens if a test fails mid-execution with retries remaining
- Recovery procedures for tests that consume retry attempts
- CI safety mechanisms to prevent device lockout

**Attack Vector:** While this is testing, repeated test execution against a physical device could exhaust PIN retries, blocking the device for legitimate use. This is especially concerning in CI environments with shared test devices.

**Impact:** Medium. A locked device requires manual intervention (PUK reset or factory reset), disrupting CI and development workflows.

**Recommendation:**  
Add to Section 3.3 (Error States):
```markdown
| Condition | System Behavior | Error Type |
|-----------|-----------------|------------|
| PIN retry counter = 2 | Test fails immediately without retry; logs warning | `SkipException` with retry counter message |
| PIN retry counter = 1 | Test skips with CRITICAL warning | `SkipException` |
| PIN blocked (counter = 0) | All tests skip; device requires reset | `SkipException` |
```

Add to Section 8.2 (Test Utilities):
```markdown
| Utility | Purpose |
|---------|---------|
| `GetPinRetriesAsync(session)` | Checks remaining PIN attempts before test |
| `SafeSkipIfLowRetries(session, minRetries)` | Skips test if retries ≤ threshold |
```

Add to Section 3.1 (Test Infrastructure):
```markdown
### Pre-Test PIN Safety Check
Before any test that requires PIN verification:
1. Check `GetPinRetriesRemaining()` 
2. If retries ≤ 2, skip test with warning message
3. Log retry counter at start and end of each test
4. Alert CI if retry counter decreases unexpectedly
```

---

### WARN-003: Credential Cleanup Failure Handling Missing
**Section:** 4.2 Security ("Tests must clean up all created credentials after completion")  
**Concern:** While the requirement exists, the PRD does not specify:
- What happens if credential deletion fails (device disconnect, cancellation, exception)
- Whether tests should verify cleanup succeeded (enumerate after delete)
- How to handle orphaned credentials from previous failed test runs
- Cleanup priority order (temp credentials vs test RP credentials)

**Impact:** Medium. Failed cleanup could:
- Exhaust credential storage on device (YubiKeys have limited slots)
- Leave test credentials that interfere with subsequent tests
- Prevent tests from running on devices with maxed-out credential storage

**Recommendation:**  
Add to Section 3.2 (Happy Path):
```markdown
| Step | User Action | System Response |
|------|-------------|-----------------|
| 7 | Test verifies credential deletion | EnumerateCredentials confirms credential removed |
| 8 | Test fixture cleanup (even on failure) | Best-effort cleanup of all test RP credentials |
```

Add to Section 8.2 (Test Utilities):
```markdown
| Utility | Purpose |
|---------|---------|
| `DeleteAllCredentialsAsync(session, rpId)` | Removes all credentials for test RP; logs failures but does not throw |
| `VerifyNoTestCredentialsAsync(session, rpId)` | Asserts cleanup succeeded; fails test if credentials remain |
| `CleanupOrphanedCredentialsAsync(session)` | One-time cleanup on test fixture setup; removes leftover test credentials |
```

Add new section:
```markdown
### 3.6 Cleanup Guarantees

**Best-Effort Cleanup:**
- Tests must attempt credential cleanup in `finally` block
- Cleanup failures must be logged but not throw exceptions
- Test fixture teardown performs additional cleanup sweep

**Cleanup Verification:**
- After each test, enumerate credentials for test RP
- If credentials remain, log ERROR but allow test to complete
- CI job fails if >5 orphaned credentials accumulate

**Pre-Test Cleanup:**
- Test fixture setup enumerates and removes all existing test RP credentials
- This handles orphaned credentials from previous failed runs
```

---

### WARN-004: Sensitive Data in Test Output Not Addressed
**Section:** None (missing)  
**Concern:** The PRD does not specify constraints on test output (assertions, logs, error messages) for sensitive data. Tests may inadvertently:
- Log full authenticator data containing credential IDs
- Print assertion signatures in failure messages
- Include user handles in test output
- Dump CBOR payloads containing sensitive structures

**Test Context:**  
While test data is ephemeral and local to dev machines, test logs are often captured in CI systems and may persist. Additionally, developers copy-pasting test output for debugging could inadvertently share sensitive structures.

**Impact:** Low-Medium. Test data is non-production, but establishing good logging hygiene prevents accidental exposure of production patterns.

**Recommendation:**  
Add new section 4.2.1:
```markdown
### 4.2.1 Sensitive Data in Test Output

**Prohibited in Logs/Assertions:**
- PIN values (even test PINs) - use `"[REDACTED]"` in logs
- Credential private keys (should never be extractable)
- Full authenticator data - log only parsed structure, not raw bytes
- User handles - log hash or `"[LENGTH:16]"` instead

**Allowed in Test Output:**
- Credential IDs (public, necessary for test validation)
- Public key data (public by definition)
- Attestation certificates (public, verifiable)
- AAGUID (public device identifier)
- RP ID (public, necessary for test context)

**Pattern:**
```csharp
// ❌ BAD
_logger.LogDebug("PIN verified: {pin}", pinBytes);

// ✅ GOOD
_logger.LogDebug("PIN verified: [REDACTED {length} bytes]", pinBytes.Length);

// ❌ BAD
Assert.True(signature.SequenceEqual(expected), 
    $"Signature mismatch: got {Convert.ToBase64String(signature)}");

// ✅ GOOD
Assert.True(signature.SequenceEqual(expected), 
    $"Signature mismatch: length={signature.Length}, hash={Convert.ToBase64String(SHA256.HashData(signature))}");
```
```

---

### WARN-005: Attestation Validation Not Required for Tests
**Section:** 3.2 Happy Path, 4.2 Security  
**Concern:** The PRD states tests should "verify attestation response contains valid CBOR, AAGUID, credentialId" but does not require cryptographic validation of attestation signatures or certificate chain verification. While this is acceptable for integration tests (they test SDK behavior, not cryptographic validity), the PRD should explicitly acknowledge this choice to prevent confusion.

**Security Implication:**  
If production code uses similar patterns without validation, attestation could be spoofed. Tests should document that they verify *structure*, not *authenticity*.

**Impact:** Low. This is a documentation/clarity issue, not a vulnerability in test code itself.

**Recommendation:**  
Add to Section 6 (Out of Scope):
```markdown
- **Attestation cryptographic validation**: Tests verify attestation structure, format, and field presence, but do not validate:
  - Certificate chain trust to Yubico root CA
  - Signature cryptographic correctness
  - Certificate revocation status
  - (Rationale: Integration tests focus on SDK protocol correctness, not cryptographic verification logic)
```

Add to Section 3.2 (Happy Path):
```markdown
| Step | User Action | System Response |
|------|-------------|-----------------|
| 3 | Test verifies attestation response | Response contains valid CBOR structure, AAGUID, credentialId; **Note:** Does not cryptographically validate signature |
```

---

### INFO-001: Touch Policy Not Specified for Test Credentials
**Section:** None (missing)  
**Concern:** The PRD does not specify the `userVerification` (UV) and `residentKey` (RK) options for test credentials, which affect touch requirements during testing.

**Implication:**  
- If UV is required, tests will block on physical touch
- If RK is set, credentials persist and require cleanup
- CI environments may not support interactive touch

**Recommendation:**  
Add to Section 8.1 (Shared Test Constants):
```markdown
| Constant | Value | Purpose |
|----------|-------|---------|
| `UserVerification` | `UserVerificationPreference.Discouraged` | Avoid blocking on touch in CI |
| `ResidentKeyRequirement` | `ResidentKeyRequirement.Preferred` | Create RK if supported, non-RK fallback |
| `TouchPolicyForInteractive` | `TouchPolicy.Cached` | Used in manual/interactive test runs |
```

Add to Section 3.4 (Edge Cases):
```markdown
| Scenario | Expected Behavior |
|----------|-------------------|
| CI non-interactive mode | Tests use `uv=discouraged` to avoid blocking on touch |
| Interactive test run | Tests may use `uv=preferred` with `--interactive` flag |
| Touch-required credential | Test documents user presence requirement in test name |
```

---

### INFO-002: No Explicit FIPS PIN Handling
**Section:** 3.3 Error States, 10.1 YubiKey 5.8.0+ with Enhanced PIN  
**Concern:** The PRD mentions FIPS mode testing (Story 5) and enhanced PIN complexity (Story 7, Section 10.1), but does not specify security constraints for FIPS PIN handling:
- FIPS requires PIN on every operation (alwaysUv)
- FIPS requires PinUvAuthProtocol v2
- Enhanced PIN complexity enforcement

**Implication:**  
Tests on FIPS devices may behave differently than standard devices. The PRD should clarify PIN handling differences.

**Recommendation:**  
Add to Section 10.1 (Device-Specific Notes):
```markdown
### FIPS PIN Security Constraints

When testing FIPS-capable devices:
- PIN must be provided for every MakeCredential/GetAssertion
- PinUvAuthProtocol version 2 is required (v1 not supported)
- PIN complexity validated on set/change operations
- alwaysUv config flag is enforced (no UP-only operations)

**Test Behavior:**
- FIPS tests must set PIN before any credential operations
- FIPS tests must use PinUvAuthToken for all protected commands
- FIPS tests must verify PIN UV AuthProtocol v2 is selected
```

---

### INFO-003: Random Test Data Generation Not Specified
**Section:** 8.1 Shared Test Constants (FidoTestData class)  
**Concern:** The PRD states:
- `UserId`: "16 random bytes"
- `Challenge`: "32 random bytes"

But does not specify:
- Whether these are generated per-test or shared across tests
- Source of randomness (CSPRNG vs non-cryptographic)
- Collision handling for concurrent tests

**Implication:**  
While test randomness is not security-critical, using CSPRNG establishes good patterns and prevents test collisions in concurrent runs.

**Recommendation:**  
Add to Section 8.1:
```csharp
public static class FidoTestData
{
    // Regenerated per test to avoid collisions
    public static byte[] GenerateUserId() => RandomNumberGenerator.GetBytes(16);
    public static byte[] GenerateChallenge() => RandomNumberGenerator.GetBytes(32);
    
    // Or: Shared constants with sufficient entropy
    // (Less preferred due to potential test interactions)
    public static readonly byte[] UserId = RandomNumberGenerator.GetBytes(16);
    public static readonly byte[] Challenge = RandomNumberGenerator.GetBytes(32);
    
    // Recommendation: Use GenerateX() pattern for isolation
}
```

---

## Checklist Results

| Category | Check | Result | Notes |
|----------|-------|--------|-------|
| **Memory** | Sensitive data zeroing | ⚠️ | PIN zeroing not explicitly required; recommend adding pattern (WARN-001) |
| **Memory** | No string conversion for secrets | ✅ | PIN stored as string constant (acceptable for tests); byte conversion should zero |
| **Memory** | Span/Memory preference | ✅ | PRD implies modern memory patterns (not explicit) |
| **YubiKey** | PIN retry behavior | ⚠️ | Acknowledged but not fully specified (WARN-002) |
| **YubiKey** | Touch policy defined | ⚠️ | Not specified; may block in CI (INFO-001) |
| **YubiKey** | Attestation validation | ⚠️ | Structural only, not cryptographic (WARN-005 - acceptable for tests) |
| **YubiKey** | FIPS constraints | ⚠️ | Mentioned but not fully specified (INFO-002) |
| **OWASP** | Input validation | ✅ | Implied by use of SDK APIs (not direct APDU construction) |
| **OWASP** | Auth required | ✅ | PIN required for credential operations (Section 3.2) |
| **OWASP** | No secret logging | ⚠️ | Not explicitly prohibited (WARN-004) |
| **OWASP** | Secure defaults | ✅ | Test PIN meets enhanced complexity by default |
| **Cleanup** | Credential deletion | ⚠️ | Required but failure handling missing (WARN-003) |
| **Cleanup** | Device state reset | ✅ | Required in Section 4.2; destructive tests opt-in only |

---

## Verdict Justification

**PASS with WARNINGS**

This PRD demonstrates strong security awareness for an integration testing framework. Key strengths include:
- ✅ Test PIN meets enhanced complexity requirements (8+ chars, mixed types)
- ✅ Credential cleanup required after test completion
- ✅ Explicit requirement not to leave device in locked state
- ✅ Destructive operations (Reset) require explicit opt-in
- ✅ Firmware version detection uses reliable source (ManagementSession)
- ✅ Edge cases documented (alpha firmware, enhanced PIN, FIPS mode)

However, **five WARNINGS** were identified that should be addressed before implementation:

1. **WARN-001:** PIN memory management pattern not specified (establish test security hygiene)
2. **WARN-002:** PIN retry counter monitoring missing (prevent device lockout in CI)
3. **WARN-003:** Credential cleanup failure handling incomplete (prevent orphaned credentials)
4. **WARN-004:** Sensitive data in test output not addressed (prevent accidental exposure)
5. **WARN-005:** Attestation validation scope unclear (document structural-only validation)

**No CRITICAL findings were identified** because:
- Test PINs are intentionally known values (not production secrets)
- Credentials are test-only and explicitly require cleanup
- Integration tests run against controlled test devices (not production)
- The framework integrates with existing secure SDK infrastructure

**Recommendation:** Address the five WARNINGS by adding explicit security specifications to the PRD sections noted above. These additions will:
- Prevent test-induced device lockouts (high impact on CI)
- Establish secure coding patterns that propagate to production code
- Prevent credential storage exhaustion on test devices
- Clarify test validation scope (structural vs cryptographic)

The PRD can proceed to implementation once these clarifications are added, as no security vulnerabilities that would block development were identified.

---

## Additional Security Observations

### Positive Security Patterns

1. **Enhanced PIN as Default:**  
   Using `"Abc12345"` (8 chars, mixed case + numbers) as the default test PIN ensures tests work on devices with strictest PIN policies, establishing secure-by-default pattern.

2. **Firmware Version from Management:**  
   Requiring ManagementSession for version detection (not unreliable FIDO AuthenticatorInfo) prevents version-based security checks from being bypassed on alpha/beta firmware.

3. **Destructive Test Isolation:**  
   Section 7 (Open Questions) proposes separate test class for Reset operations with explicit opt-in, preventing accidental device wipes.

4. **Feature Detection over Version Checks:**  
   Tests skip gracefully on unsupported features rather than assuming capabilities based on version, reducing risk of false positives.

5. **Transport-Specific Behavior:**  
   Section 3.4 acknowledges USB vs NFC differences, preventing transport-confusion attacks in tests.

### Recommendations for Implementation Phase

When implementing this PRD, ensure:

1. **Test Base Class Security:**
   - Implement PIN retry monitoring in test fixture setup
   - Add automatic credential cleanup in teardown (even on test failure)
   - Log security-relevant events (PIN attempts, credential count)

2. **CI Configuration:**
   - Set retry counter threshold alerts (warn at ≤3 retries)
   - Monitor credential storage usage across test runs
   - Implement device pool rotation if lockout occurs

3. **Developer Documentation:**
   - Document why test PINs are zeroed (establish pattern)
   - Provide examples of secure vs insecure test logging
   - Explain attestation validation scope (structural only)

4. **Code Review Checklist:**
   - Verify `CryptographicOperations.ZeroMemory()` in PIN utility methods
   - Check `finally` blocks for credential cleanup
   - Review test assertions for sensitive data exposure
   - Confirm PIN retry checks before PIN operations

---

## References

- Security Guidelines: `.claude/skills/domain-security-guidelines/SKILL.md`
- DX Audit: `docs/specs/fido2-integration-testing/dx_audit.md`
- PRD: `docs/specs/fido2-integration-testing/draft.md`
- CLAUDE.md Security Patterns: Lines 26-29 (Zero sensitive data, dispose crypto, no logging)

---

**Audit Complete.** PRD may proceed to implementation with WARNINGS addressed during detailed design phase.
