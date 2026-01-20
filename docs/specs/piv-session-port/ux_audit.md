# UX Audit Report

**PRD:** Port PivSession from Java yubikit-android to C# Yubico.YubiKit.Piv  
**Auditor:** ux-validator  
**Date:** 2026-01-18T00:00:00Z  
**Verdict:** FAIL

---

## Summary

| Severity | Count |
|----------|-------|
| CRITICAL | 7 |
| WARN | 8 |
| INFO | 5 |

**Overall:** The PRD has strong technical coverage but lacks critical UX elements: no cancellation guidance, inconsistent error message specifications, missing empty state behaviors, and insufficient documentation requirements. Seven CRITICAL findings prevent implementation without additional specification.

---

## Findings

### CRITICAL-001: Missing CancellationToken Behavior Specification
**Section:** All User Stories (US-1 through US-16)  
**Issue:** While `CancellationToken` parameters are present in the interface, the PRD does not specify:
- What happens when cancellation is requested during a long operation (key generation, PIN blocking for reset)
- Whether partial state changes are rolled back
- What exception is thrown (`OperationCanceledException`?)
- Whether device state is left in a consistent state after cancellation

**Impact:** Developers cannot implement cancellable operations correctly. Users may experience inconsistent device state if operations are cancelled mid-flight.

**Recommendation:** Add acceptance criteria to relevant user stories:
```
- [ ] Throw OperationCanceledException when cancellationToken is triggered
- [ ] Ensure device state remains consistent (no partial writes)
- [ ] Document which operations can be safely cancelled
- [ ] Document which operations cannot be cancelled (e.g., during APDU response)
```

---

### CRITICAL-002: Inconsistent Error Message Actionability
**Section:** US-3 (Verify PIN), US-6 (Sign or Decrypt), Error Handling section  
**Issue:** Error messages are specified for PIN verification (`InvalidPinException` with retry count), but most other failures lack actionable message requirements:
- "Throw descriptive error if key not present in slot" (US-6) - what does "descriptive" mean?
- `BadResponseException` for challenge-response failure (US-2) - what should the message say?
- Generic `ApduException` wrapping (Error Handling table) - no message content specified

**Impact:** Developers will receive error messages that say "an error occurred" without knowing how to fix it. This violates Heuristic #9 (Help users recognize and recover from errors).

**Recommendation:** For each exception type, specify:
- **WHAT** failed (e.g., "Private key not found in slot 0x9A")
- **WHY** it failed (e.g., "No key has been generated or imported in this slot")
- **HOW** to fix it (e.g., "Use GenerateKeyAsync() or ImportKeyAsync() to provision a key before signing")

Example:
```csharp
throw new InvalidOperationException(
    $"Cannot sign data: No private key exists in slot {slot:X2}. " +
    $"Generate a key with GenerateKeyAsync() or import one with ImportKeyAsync().");
```

---

### CRITICAL-003: Empty State Behavior Undefined
**Section:** US-8 (Certificate Management), US-11 (Metadata Retrieval), US-14 (Data Object Operations)  
**Issue:** The PRD does not specify what happens when:
- `GetCertificateAsync()` is called on a slot with no certificate
- `GetObjectAsync()` is called on a non-existent object ID
- `GetSlotMetadataAsync()` is called on an empty slot

Does it return `null`? Throw? Return an empty certificate?

**Impact:** Developers will not know how to check if a slot has a certificate before using it. API is not discoverable.

**Recommendation:** Add acceptance criteria for empty states:
```
US-8:
- [ ] GetCertificateAsync returns null if slot has no certificate
- [ ] DeleteCertificateAsync is idempotent (no error if already empty)

US-14:
- [ ] GetObjectAsync throws ApduException with "Object not found" if ID doesn't exist
- [ ] PutObjectAsync with null data deletes the object (or throws?)

US-11:
- [ ] GetSlotMetadataAsync throws InvalidOperationException if slot is empty
```

---

### CRITICAL-004: Exception Hierarchy Not Defined
**Section:** Error Handling  
**Issue:** The PRD lists exception types (`InvalidPinException`, `BadResponseException`, `NotSupportedException`) but does not specify:
- Which exceptions derive from which base classes?
- Should `InvalidPinException` derive from `ArgumentException`, `SecurityException`, or a custom base?
- Should `BadResponseException` be SDK-specific or framework `InvalidOperationException`?
- Can callers catch a common base exception for "YubiKey communication errors"?

**Impact:** Developers cannot write effective error handling. Unclear whether to catch specific exceptions or a base type. Violates Heuristic #9 (error recovery).

**Recommendation:** Define exception hierarchy:
```csharp
YubiKeyException (base)
├── PivException (base for all PIV errors)
│   ├── InvalidPinException : PivException
│   ├── PinBlockedException : InvalidPinException
│   ├── ManagementKeyAuthenticationException : PivException
│   └── UnsupportedPivFeatureException : PivException, NotSupportedException
└── ApduException (already exists?)
    └── BadResponseException : ApduException
```

Add to PRD: "All PIV-specific exceptions derive from `PivException : YubiKeyException`."

---

### CRITICAL-005: Default Values Not Fully Specified
**Section:** US-4 (Generate Key Pair), US-5 (Import Private Key)  
**Issue:** The PRD states default parameters (`PivPinPolicy.Default`, `PivTouchPolicy.Default`) but does not define what "Default" MEANS:
- What is the actual PIN policy when `Default` is specified?
- Does it vary by slot (e.g., Signature slot requires PIN each use per spec)?
- Does it vary by firmware version?

**Impact:** Developers will not know the actual behavior of their keys. "Default" is a magic value with undefined semantics. Violates Heuristic #6 (recognition over recall).

**Recommendation:** Document in the enums section:
```csharp
/// <summary>
/// PIN policy for PIV key usage.
/// </summary>
public enum PivPinPolicy : byte
{
    /// <summary>
    /// Use the slot's default PIN policy:
    /// - Slot 0x9C (Signature): Always (per NIST SP 800-73)
    /// - All other slots: Once per session
    /// </summary>
    Default = 0x00,
    
    /// <summary>
    /// Never require PIN verification (not recommended).
    /// </summary>
    Never = 0x01,
    
    // ... etc
}
```

Add to US-4: `- [ ] Document which PIN/Touch policy is applied for each slot when Default is used`

---

### CRITICAL-006: Missing Progress Reporting for Long Operations
**Section:** US-4 (Generate Key Pair), US-15 (Application Reset)  
**Issue:** Two operations are potentially long-running:
1. RSA 4096-bit key generation (can take 30+ seconds)
2. Application reset (blocks PIN/PUK multiple times)

The PRD does not specify how to report progress or indicate the operation is still running. Violates Heuristic #1 (visibility of system status).

**Impact:** Developers cannot show progress UI. Users will think the application has frozen.

**Recommendation:** Add to US-4 and US-15:
```
- [ ] For operations exceeding 3 seconds, support IProgress<T> parameter
- [ ] Report progress for RSA generation: "Generating prime p...", "Generating prime q...", etc.
- [ ] For reset: "Blocking PIN (attempt 1/3)", "Blocking PUK (attempt 1/3)", "Sending reset..."
```

Or specify: `- [ ] Document that GenerateKeyAsync for RSA 4096 may take 30+ seconds with no progress`

---

### CRITICAL-007: Retry Semantics Undefined
**Section:** US-3 (Verify PIN), US-12 (Biometric Authentication)  
**Issue:** When PIN verification or biometric verification fails, the PRD specifies the error (`InvalidPinException` with retry count) but not:
- Is it safe to retry immediately?
- Should there be rate limiting?
- What happens if all retries are exhausted?
- Can the caller distinguish between "wrong PIN" and "other error"?

**Impact:** Developers may write code that accidentally blocks the PIN by retrying in a loop. Violates Heuristic #5 (error prevention).

**Recommendation:** Add to US-3:
```
- [ ] InvalidPinException includes RetriesRemaining property
- [ ] Throw PinBlockedException (derives from InvalidPinException) when retries = 0
- [ ] Document: Callers should NOT automatically retry on InvalidPinException
- [ ] Document: Check RetriesRemaining before prompting user again
```

Example:
```csharp
public class InvalidPinException : PivException
{
    public int RetriesRemaining { get; }
    
    public InvalidPinException(int retriesRemaining)
        : base($"PIN verification failed. {retriesRemaining} attempts remaining.") { }
}
```

---

### WARN-001: No Overloads for Common Scenarios
**Section:** US-2 (Authenticate with Management Key), US-3 (Verify PIN)  
**Issue:** The interface only provides the full signature:
- `AuthenticateAsync(ReadOnlyMemory<byte> managementKey, ...)`
- `VerifyPinAsync(ReadOnlyMemory<char> pin, ...)`

There are no convenience overloads for common cases:
- Authenticate with default key (the 0x010203... value)
- Verify PIN from a string (most common caller scenario)

**Impact:** Every caller must convert `string` to `ReadOnlyMemory<char>`. Violates Heuristic #7 (flexibility and efficiency).

**Recommendation:** Add convenience overloads to interface:
```csharp
// Default management key
Task AuthenticateWithDefaultKeyAsync(CancellationToken ct = default);

// String PIN overload (creates memory internally)
Task VerifyPinAsync(string pin, CancellationToken ct = default);
```

Or document: `- [ ] Session implementation should provide extension methods for common cases`

---

### WARN-002: No Guidance on When to Call Dispose
**Section:** US-1 (Initialize PIV Session)  
**Issue:** The interface derives from `IApplicationSession` (presumably `IDisposable`), but the PRD does not specify:
- What resources are cleaned up on Dispose?
- Is the underlying `ISmartCardConnection` closed?
- Can the session be reused after Dispose?
- What happens if methods are called after Dispose?

**Impact:** Memory leaks or double-dispose errors. Violates Heuristic #9 (error recovery).

**Recommendation:** Add to US-1:
```
- [ ] Dispose releases the ISmartCardConnection (if created by factory)
- [ ] Dispose does NOT release externally-provided connections
- [ ] Methods throw ObjectDisposedException after Dispose
- [ ] Session is NOT reusable after Dispose
```

---

### WARN-003: Unclear Factory vs Extension Method Usage
**Section:** US-1 (Initialize PIV Session)  
**Issue:** Two creation patterns are defined:
1. Extension method: `yubiKey.CreatePivSessionAsync()`
2. Static factory: `PivSession.CreateAsync(connection)`

The PRD does not specify:
- When should a developer use one vs the other?
- Does the extension method create its own connection internally?
- Who owns the connection lifetime?

**Impact:** Developers will misuse the APIs, leading to resource leaks. Violates Heuristic #10 (documentation).

**Recommendation:** Add to US-1:
```
- [ ] Document: Use CreatePivSessionAsync() for typical scenarios (SDK manages connection)
- [ ] Document: Use CreateAsync(connection) for advanced scenarios (caller manages connection)
- [ ] Extension method creates and owns ISmartCardConnection (disposed on session disposal)
- [ ] Factory method does NOT own the connection (caller must dispose separately)
```

---

### WARN-004: No Validation for Invalid Slot/Algorithm Combinations
**Section:** US-4 (Generate Key Pair), US-5 (Import Private Key)  
**Issue:** The PRD lists supported algorithms and slots, but does not specify:
- Can Ed25519 be generated in the Attestation slot (0xF9, read-only)?
- Can any algorithm be used in any slot, or are there restrictions?
- Should the API validate before sending to the device?

**Impact:** Developers will receive cryptic APDU errors instead of clear validation messages. Violates Heuristic #5 (error prevention).

**Recommendation:** Add to US-4:
```
- [ ] Validate slot is writable before generation (throw ArgumentException for 0xF9)
- [ ] Throw ArgumentException with message: "Slot 0xF9 is read-only and cannot be overwritten"
```

---

### WARN-005: Missing XML Documentation Requirement
**Section:** Implementation Plan  
**Issue:** Phase 8 includes "Update module CLAUDE.md" and "Create README.md" but does not specify:
- XML documentation requirements for all public types and members
- Code examples in XML `<example>` tags
- Documentation of exceptions in `<exception>` tags

**Impact:** IntelliSense will not provide useful help. Violates Heuristic #10 (help and documentation).

**Recommendation:** Add to Phase 8:
```
- [ ] All public types have XML summary documentation
- [ ] All public methods have XML summary, param, returns, and exception tags
- [ ] At least one <example> tag per user story with complete code sample
- [ ] Document all possible exceptions each method can throw
```

---

### WARN-006: No Idempotency Specification
**Section:** US-8 (Certificate Management), US-13 (Move and Delete Keys)  
**Issue:** For deletion operations, the PRD does not specify:
- Is `DeleteCertificateAsync()` idempotent (no error if already deleted)?
- Is `DeleteKeyAsync()` idempotent?
- What happens if you delete something that doesn't exist?

**Impact:** Developers must check existence before deletion (extra round-trip) or risk exceptions. Violates Heuristic #7 (efficiency).

**Recommendation:** Add to US-8 and US-13:
```
- [ ] DeleteCertificateAsync is idempotent (succeeds if already empty)
- [ ] DeleteKeyAsync is idempotent (succeeds if slot already empty)
```

---

### WARN-007: No Concurrent Operation Guidance
**Section:** Interface Definition  
**Issue:** The interface is async, but the PRD does not specify:
- Can multiple async operations run concurrently on the same session?
- Is the session thread-safe?
- What happens if two operations are called simultaneously?

**Impact:** Race conditions and corrupted device state. Violates Heuristic #5 (error prevention).

**Recommendation:** Add to "Security Requirements" or create "Concurrency Requirements":
```
## CR-1: Single-Threaded Session
PivSession is NOT thread-safe. All operations must be serialized. Concurrent calls throw InvalidOperationException: "A PIV operation is already in progress."
```

Or:
```
## CR-1: Thread-Safe Session
PivSession uses internal locking to serialize all operations. Concurrent calls block until previous operation completes.
```

---

### WARN-008: Missing Firmware Version Check Examples
**Section:** US-4 (Generate Key Pair), Feature Gates  
**Issue:** The PRD defines feature gates (e.g., `PivFeatures.Cv25519` requires 5.7+) but does not show:
- How developers should check firmware version before calling a method
- Whether the session automatically throws `NotSupportedException` with a helpful message
- Whether developers must manually check `PivFeatures` before each call

**Impact:** Developers will call unsupported methods on old firmware and get cryptic errors. Violates Heuristic #9 (error recovery).

**Recommendation:** Add to US-4:
```
- [ ] Automatically check firmware version before operations
- [ ] Throw UnsupportedPivFeatureException with message:
      "Curve25519 requires YubiKey firmware 5.7.0 or later. Current firmware: 5.4.3."
- [ ] Include PivFeatures property reference in exception message
```

---

### INFO-001: Consider Fluent Builder for Key Generation
**Section:** US-4 (Generate Key Pair)  
**Note:** The current API has 4 parameters for key generation. Consider a fluent builder pattern for discoverability:

```csharp
await session.GenerateKey()
    .InSlot(PivSlot.Authentication)
    .WithAlgorithm(PivAlgorithm.EccP256)
    .RequirePinOnce()
    .RequireTouchAlways()
    .ExecuteAsync();
```

This improves Heuristic #6 (recognition over recall) and #7 (flexibility). Optional - does not block implementation.

---

### INFO-002: Consider Typed Public Key Return
**Section:** US-4 (Generate Key Pair)  
**Note:** The PRD specifies `PivPublicKey` as the return type but does not define this type. Consider making it a discriminated union or base class:

```csharp
public abstract class PivPublicKey { }
public class RsaPublicKey : PivPublicKey { public ReadOnlyMemory<byte> Modulus; public ReadOnlyMemory<byte> Exponent; }
public class EccPublicKey : PivPublicKey { public ReadOnlyMemory<byte> Point; }
```

This improves type safety and API discoverability. Optional enhancement.

---

### INFO-003: Consider ReadOnlySpan<T> for Synchronous Paths
**Section:** US-2, US-3 (Authentication methods)  
**Note:** `ReadOnlyMemory<T>` is typically for async methods. If the underlying APDU send/receive is synchronous (wrapped in Task), consider accepting `ReadOnlySpan<T>` for zero-copy scenarios. This is a performance optimization, not a blocker.

---

### INFO-004: Consider Adding Attestation Certificate Chain Retrieval
**Section:** US-9 (Key Attestation)  
**Note:** The PRD specifies retrieving the attestation certificate from slot 0xF9, but does not mention retrieving the intermediate CA certificate for full chain validation. Consider adding a helper method:

```csharp
Task<X509Certificate2Collection> GetAttestationChainAsync(CancellationToken ct = default);
```

This would improve developer experience when validating attestation. Optional.

---

### INFO-005: Consider Adding Certificate Validation Helpers
**Section:** US-8 (Certificate Management)  
**Note:** Developers will need to validate that a certificate matches the public key in a slot. Consider adding:

```csharp
Task<bool> ValidateCertificateMatchesSlotAsync(PivSlot slot, X509Certificate2 certificate, CancellationToken ct = default);
```

This would prevent common errors. Optional convenience method.

---

## Checklist Results

| Heuristic | Result | Notes |
|-----------|--------|-------|
| 1. Visibility of system status | ❌ | **CRITICAL-006**: No progress reporting for long operations (RSA 4096 gen, reset) |
| 2. Match system and real world | ✅ | All PIV terminology is domain-standard (NIST SP 800-73 references) |
| 3. User control and freedom | ❌ | **CRITICAL-001**: CancellationToken present but behavior undefined |
| 4. Consistency and standards | ✅ | Follows existing `*Session` patterns, async/await, .NET conventions |
| 5. Error prevention | ❌ | **CRITICAL-007**: No retry semantic guidance; **WARN-004**: No input validation specified |
| 6. Recognition over recall | ⚠️ | **CRITICAL-005**: "Default" enum values undefined; **WARN-001**: Missing convenience overloads |
| 7. Flexibility and efficiency | ⚠️ | **WARN-001**: No simple/advanced overload pairs; **WARN-006**: No idempotency spec |
| 8. Minimalist design | ✅ | API surface is minimal; no leaking of TLV/APDU internals |
| 9. Error recovery | ❌ | **CRITICAL-002**: Inconsistent error messages; **CRITICAL-004**: No exception hierarchy |
| 10. Documentation | ❌ | **WARN-005**: No XML doc requirements; **WARN-003**: Unclear factory usage guidance |

**Summary:** 3/10 pass, 3/10 partial, 4/10 fail

---

## Verdict Justification

**FAIL** - The PRD contains **7 CRITICAL findings** that must be resolved before implementation:

1. **CRITICAL-001**: CancellationToken behavior undefined (affects all 16 user stories)
2. **CRITICAL-002**: Error messages lack actionability requirements
3. **CRITICAL-003**: Empty state behaviors not specified
4. **CRITICAL-004**: Exception hierarchy undefined (affects error handling strategy)
5. **CRITICAL-005**: Default enum values have undefined semantics
6. **CRITICAL-006**: No progress reporting for 30+ second operations
7. **CRITICAL-007**: Retry semantics undefined (safety issue)

**Why these block implementation:**
- An implementer cannot write correct cancellation handling without knowing expected behavior
- An implementer cannot write helpful error messages without content requirements
- An implementer cannot decide whether to return null or throw without empty state specification
- An implementer cannot design exception handling without a hierarchy
- An implementer cannot resolve "Default" enum values without knowing their semantics

**Positive aspects:**
- Strong technical coverage of PIV operations
- Good security requirements (zeroing, no logging)
- Comprehensive feature gate definitions
- Clear APDU instruction mapping

**Next steps:**
1. Spec-writer must address all 7 CRITICAL findings
2. Strongly recommend addressing 8 WARN findings (especially WARN-001, 002, 003, 005, 007)
3. Consider 5 INFO suggestions for improved developer experience

**Estimated revision effort:** 4-6 hours to add missing acceptance criteria and documentation requirements.
