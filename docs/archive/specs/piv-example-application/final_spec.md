# PRD: PIV Example Application - FINAL

**Status:** APPROVED  
**Author:** spec-writer agent  
**Approved:** 2026-01-23T13:55:00Z  
**Feature Slug:** piv-example-application

---

# PRD: PIV Example Application

**Status:** DRAFT  
**Author:** spec-writer agent  
**Created:** 2026-01-23  
**Feature Slug:** piv-example-application

---

## 1. Problem Statement

### Current State
The existing PIV example application in the reference repository contains ~4,000+ lines of code with significant maintainability issues:

- **Scattered Architecture:** Code split across 6+ directories (KeyCollector, YubiKeyOperations, CertificateOperations, Converters, SlotContents, Run)
- **Custom UI Boilerplate:** ~2,000 lines dedicated to menu systems and prompts
- **Poor Discoverability:** Developers must navigate multiple files to understand a single operation
- **High Coupling:** Changes to one feature often require modifications across multiple directories

### Evidence
- Total example code: ~11,800 lines across all examples (PIV, FIDO2, OATH, U2F, Shared)
- PIV example alone: ~4,000 lines
- Time to understand a single operation: 30+ minutes for new developers

### Desired State
A modern, maintainable PIV example application that:
- Demonstrates all PIV operations in under 2,000 lines
- Uses vertical slicing (one operation = one file)
- Leverages Spectre.Console to eliminate custom UI code
- Serves as a template for FIDO2, OATH, and other example applications

### Impact
- **Developers:** Faster SDK adoption, clearer usage patterns
- **End-users:** Reliable tool to test YubiKey PIV functionality
- **Yubico:** Reduced support burden, better SDK feedback loop

---

## 2. User Stories

### US-1: Device Discovery
**As a** developer learning the SDK  
**I want to** list all connected YubiKeys with their PIV capabilities  
**So that** I can select the correct device for my operations

**Acceptance Criteria:**
- [ ] Display device serial number, firmware version, and form factor
- [ ] Show PIV application version and supported features
- [ ] Handle multiple connected devices gracefully
- [ ] Clear error message when no YubiKey is connected
- [ ] Implementation ≤ 150 lines

### US-2: PIN Management
**As a** security administrator  
**I want to** manage PIV PIN, PUK, and management key  
**So that** I can configure device security policies

**Acceptance Criteria:**
- [ ] Verify current PIN with retry count display
- [ ] Change PIN with old/new confirmation
- [ ] Change PUK with old/new confirmation
- [ ] Unblock PIN using PUK
- [ ] Set PIN/PUK retry limits
- [ ] View PIN/PUK metadata (default status, retries remaining)
- [ ] Display metadata showing if default PIN/PUK/management key is in use
- [ ] Visual warning indicator (🔓) when defaults are detected
- [ ] Change management key with algorithm selection (3DES, AES-128/192/256)
- [ ] All PIN/PUK/management key inputs are zeroed after use
- [ ] No credential strings remain in memory after operations
- [ ] Clear error messages for wrong PIN with retries remaining
- [ ] Implementation ≤ 400 lines

### US-3: Key Generation
**As a** developer  
**I want to** generate key pairs in PIV slots  
**So that** I can set up cryptographic operations

**Acceptance Criteria:**
- [ ] Select slot from list (Authentication, Signature, KeyManagement, CardAuthentication, Retired1-20)
- [ ] Select algorithm (RSA-1024/2048/3072/4096, ECC P-256/P-384, Ed25519, X25519)
- [ ] Configure PIN policy (Default, Never, Once, Always, MatchOnce, MatchAlways)
- [ ] Configure touch policy (Default, Never, Always, Cached)
- [ ] Display generated public key
- [ ] Warn about overwriting existing key
- [ ] Implementation ≤ 300 lines

### US-4: Certificate Operations
**As a** PKI administrator  
**I want to** manage certificates in PIV slots  
**So that** I can deploy and rotate credentials

**Acceptance Criteria:**
- [ ] View certificate details (subject, issuer, validity, algorithm)
- [ ] Import certificate from PEM/DER file
- [ ] Export certificate to PEM/DER file
- [ ] Delete certificate from slot
- [ ] Generate self-signed certificate for testing
- [ ] Generate CSR for CA signing
- [ ] Store certificate with optional compression
- [ ] Implementation ≤ 400 lines

### US-5: Cryptographic Operations
**As a** developer testing PIV integration  
**I want to** perform signing and decryption operations  
**So that** I can verify my application's cryptographic workflows

**Acceptance Criteria:**
- [ ] Sign data with private key (SHA-256, SHA-384, SHA-512)
- [ ] Decrypt data with RSA private key
- [ ] Verify signature with stored certificate
- [ ] Display operation timing for performance testing
- [ ] Handle PIN/touch prompts appropriately
- [ ] Implementation ≤ 300 lines

### US-6: Key Attestation
**As a** security auditor  
**I want to** verify that keys were generated on-device  
**So that** I can ensure cryptographic key provenance

**Acceptance Criteria:**
- [ ] Generate attestation certificate for any slot
- [ ] Display attestation chain (slot cert → attestation cert → Yubico root)
- [ ] Verify attestation signature
- [ ] Show key generation metadata from attestation
- [ ] Implementation ≤ 200 lines

### US-7: Slot Overview
**As a** user testing my YubiKey  
**I want to** see a summary of all PIV slots  
**So that** I can understand what's configured on my device

**Acceptance Criteria:**
- [ ] Table view of all slots (standard + retired)
- [ ] Show for each slot: algorithm, PIN policy, touch policy, has certificate
- [ ] Indicate empty vs. populated slots
- [ ] Quick access to detailed slot view
- [ ] Implementation ≤ 200 lines

### US-8: PIV Reset
**As a** user with a locked device  
**I want to** reset PIV to factory defaults  
**So that** I can recover from blocked PIN/PUK

**Acceptance Criteria:**
- [ ] Multiple confirmation prompts before reset
- [ ] Clear warning about data loss
- [ ] Reset requires both PIN and PUK to be blocked, or management key
- [ ] Display warning about default credentials after successful reset
- [ ] Prompt user to change PIN, PUK, and management key immediately
- [ ] Success/failure feedback
- [ ] Implementation ≤ 100 lines

---

## 3. Functional Requirements

### FR-1: Device Connection
- **FR-1.1:** Application MUST connect to YubiKey via SmartCard interface
- **FR-1.2:** Application MUST support device hot-plug (connect/disconnect during runtime)
- **FR-1.3:** Application MUST handle device selection when multiple YubiKeys are connected
- **FR-1.4:** Application MUST display clear error when no compatible device is found

### FR-2: Authentication
- **FR-2.1:** Application MUST support PIN verification before protected operations
- **FR-2.2:** Application MUST support management key authentication for admin operations
- **FR-2.3:** Application MUST display remaining retry count on authentication failure
- **FR-2.4:** Application MUST NOT store PINs or keys in memory longer than necessary

### FR-3: Key Management
- **FR-3.1:** Application MUST support all PIV slots (9a, 9c, 9d, 9e, 82-95, f9)
- **FR-3.2:** Application MUST support all algorithms: RSA (1024-4096), ECC (P-256, P-384), Curve25519 (Ed25519, X25519)
- **FR-3.3:** Application MUST allow configuration of PIN and touch policies
- **FR-3.4:** Application MUST support key import from PEM/PKCS#8 format
- **FR-3.5:** Application MUST support key move between slots
- **FR-3.6:** Application MUST support key deletion with confirmation

### FR-4: Certificate Management
- **FR-4.1:** Application MUST support X.509 certificate import (PEM/DER)
- **FR-4.2:** Application MUST support certificate export (PEM/DER)
- **FR-4.3:** Application MUST support certificate deletion
- **FR-4.4:** Application MUST display certificate details in human-readable format
- **FR-4.5:** Application MUST support certificate compression for storage

### FR-5: Cryptographic Operations
- **FR-5.1:** Application MUST support signing with RSA (PKCS#1 v1.5, PSS) and ECDSA
- **FR-5.2:** Application MUST support RSA decryption (PKCS#1 v1.5, OAEP)
- **FR-5.3:** Application MUST handle touch policy prompts with user feedback
- **FR-5.4:** Application MUST time operations and display duration

### FR-6: Metadata and Status
- **FR-6.1:** Application MUST display PIN/PUK/management key metadata
- **FR-6.2:** Application MUST display slot metadata (algorithm, policies, key origin)
- **FR-6.3:** Application MUST display device serial number and firmware version
- **FR-6.4:** Application MUST indicate feature support based on firmware version

---

## 4. Error States and Handling

### Error Message Security Guidelines

Error messages MUST NOT reveal:
- Internal exception details or stack traces
- Exact cryptographic algorithm details to unauthenticated users
- Key material sizes or properties before authentication
- Slot occupancy status before PIN verification (except for certificate operations)

**Safe Error Patterns:**
✅ "Authentication required to perform this operation."
✅ "Incorrect PIN. 2 attempts remaining."
✅ "Operation failed. Verify key and certificate match."

**Unsafe Error Patterns:**
❌ "InvalidOperationException: Key in slot 9a is ECCP256 but certificate is RSA2048"
❌ "CryptographicException: Padding mode PKCS1 failed with status 0x6A80"
❌ "Slot 9a contains private key but no certificate"

**Exception Handling Pattern:**
```csharp
catch (Exception ex)
{
    // Log technical details
    _logger.LogError(ex, "Certificate import failed for slot {Slot}", slot);
    
    // Show user-friendly message (no internal details)
    AnsiConsole.MarkupLine("[red]Certificate import failed. Verify format and slot compatibility.[/]");
}
```

### ES-1: Device Errors
| Error | User Message | Recovery Action |
|-------|--------------|-----------------|
| No device connected | "No YubiKey detected. Please insert a YubiKey and try again." | Prompt to retry |
| Device disconnected | "YubiKey was removed. Please reconnect to continue." | Return to device selection |
| Multiple devices | "Multiple YubiKeys detected. Please select one:" | Show selection menu |
| Unsupported device | "This YubiKey does not support PIV. Minimum firmware: 4.0" | Exit gracefully |

### ES-2: Authentication Errors
| Error | User Message | Recovery Action |
|-------|--------------|-----------------|
| Wrong PIN | "Incorrect PIN. {n} attempts remaining." | Prompt to retry |
| PIN blocked | "PIN is blocked. Use PUK to unblock or reset PIV." | Offer unblock/reset options |
| Wrong PUK | "Incorrect PUK. {n} attempts remaining." | Prompt to retry |
| PUK blocked | "PUK is blocked. PIV reset required." | Offer reset option |
| Wrong mgmt key | "Incorrect management key." | Prompt to retry |
| Auth required | "This operation requires PIN verification." | Prompt for PIN |

### ES-3: Operation Errors
| Error | User Message | Recovery Action |
|-------|--------------|-----------------|
| Unsupported algorithm | "Algorithm {alg} not supported on firmware {ver}." | Show supported algorithms |
| Slot occupied | "Slot {slot} contains a key. Overwrite? [y/N]" | Require confirmation |
| Touch timeout | "Touch timeout. Please touch the YubiKey when prompted." | Retry operation |
| Invalid certificate | "Certificate format not recognized. Expected PEM or DER." | Show format help |
| Key type mismatch | "Certificate does not match key in slot {slot}." | Show details |

### ES-4: Edge Cases
- **Empty slot access:** "Slot {slot} is empty. Generate or import a key first."
- **Attestation on imported key:** "Attestation only available for keys generated on-device."
- **Reset without blocked PIN/PUK:** "Reset requires blocked PIN and PUK, or management key authentication."
- **Default credentials detected:** "⚠️  WARNING: This YubiKey is using factory default credentials (PIN: 123456, PUK: 12345678). Change them immediately before using in production."
- **Retired slot on old firmware:** "Retired slots require firmware 5.3+. Your firmware: {ver}"

---

## 5. Non-Functional Requirements

### NFR-1: Performance
- Application startup: < 2 seconds
- Device enumeration: < 1 second
- Key generation (RSA-2048): < 30 seconds (YubiKey hardware limited)
- Signing operation: < 500ms

### NFR-2: Code Quality
- Total lines of code: ≤ 2,000 (excluding generated code)
- Cyclomatic complexity per method: ≤ 10
- Maximum file size: ≤ 400 lines
- Maximum folder depth: 2 levels from project root

### NFR-3: Maintainability
- Each PIV operation in single file (vertical slicing)
- No shared state between features except device connection
- Self-documenting code with minimal comments
- Follow CLAUDE.md coding standards

### NFR-4: Security
- Zero sensitive data in logs (PINs, keys, PUKs)
- Use `CryptographicOperations.ZeroMemory()` for sensitive buffers
- No hardcoded default credentials
- Clear sensitive data from UI after use
- Error messages MUST NOT leak internal exception details
- Technical details logged separately, not shown to user
- Timing consistency for success/failure paths where applicable

### NFR-5: Compatibility
- .NET 10.0 target framework
- C# 14 language features
- Cross-platform: Windows, macOS, Linux
- Spectre.Console 0.49+ for CLI

---

## 6. Technical Design

### Project Structure
```
Yubico.YubiKit.Piv/
├── examples/
│   └── PivTool/
│       ├── PivTool.csproj
│       ├── Program.cs              # Entry point, main menu
│       ├── README.md               # Usage documentation
│       ├── Features/
│       │   ├── DeviceInfo.cs       # US-1: Device discovery
│       │   ├── PinManagement.cs    # US-2: PIN/PUK/mgmt key
│       │   ├── KeyGeneration.cs    # US-3: Generate keys
│       │   ├── Certificates.cs     # US-4: Certificate ops
│       │   ├── Crypto.cs           # US-5: Sign/decrypt
│       │   ├── Attestation.cs      # US-6: Key attestation
│       │   ├── SlotOverview.cs     # US-7: Slot summary
│       │   └── Reset.cs            # US-8: PIV reset
│       └── Shared/
│           ├── DeviceSelector.cs   # Multi-device handling
│           ├── OutputHelpers.cs    # Spectre.Console utilities
│           └── PinPrompt.cs        # Secure PIN entry
```

### Dependencies
```xml
<ItemGroup>
  <PackageReference Include="Spectre.Console" Version="0.49.1" />
  <ProjectReference Include="../../src/Yubico.YubiKit.Piv.csproj" />
  <ProjectReference Include="../../../Yubico.YubiKit.Core/src/Yubico.YubiKit.Core.csproj" />
</ItemGroup>
```

### Key Architectural Decisions

**AD-1: Vertical Slicing**
Each feature file contains:
- Spectre.Console command registration
- User prompts and input validation
- SDK API calls
- Output formatting
- Error handling

Rationale: Maximizes cohesion, minimizes coupling, enables easy feature addition/removal.

**AD-2: No Abstraction Over SDK**
Call `IPivSession` methods directly without wrapper classes.

Rationale: Example should demonstrate actual SDK usage, not hide it behind abstractions.

**AD-3: Spectre.Console for All UI**
Use built-in prompts, tables, trees, and status indicators.

Rationale: Eliminates custom menu code (~2,000 lines in existing example).

**AD-4: Shared Code Minimal**
Only share:
- Device selection (multi-device handling)
- Output formatting helpers
- Secure PIN prompt

Rationale: Keeps features independent while avoiding duplication of truly common patterns.

### Sensitive Credential Handling

All credential prompts SHALL follow this pattern to protect PIN, PUK, and management key data in memory:

**Memory Zeroing Pattern:**
```csharp
// CORRECT: Use char[] + zero after use
char[] pinChars = promptResult.ToCharArray();
try
{
    byte[] pinBytes = Encoding.UTF8.GetBytes(pinChars);
    try
    {
        await session.VerifyPinAsync(new ReadOnlyMemory<byte>(pinBytes), ct);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(pinBytes);
    }
}
finally
{
    Array.Clear(pinChars);
}

// NEVER: string pin = AnsiConsole.Prompt(...); ❌
// Strings are immutable and cannot be securely zeroed
```

**Spectre.Console Integration:**
- Use `TextPrompt<string>` with `.Secret()` for input masking
- IMMEDIATELY convert to `char[]` after prompt
- Zero all credential arrays in `finally` blocks
- Document in `Shared/PinPrompt.cs` as reference implementation

**Example: Secure PIN Prompt Helper**
```csharp
internal static class SecurePinPrompt
{
    public static char[] PromptForPin(string message)
    {
        // Spectre.Console returns string - convert immediately
        string pinString = AnsiConsole.Prompt(
            new TextPrompt<string>(message).Secret()
        );
        
        char[] pinChars = pinString.ToCharArray();
        // Note: pinString still in memory (GC-managed)
        // Best effort to minimize exposure window
        return pinChars; // Caller MUST zero this in finally block
    }
}
```

**Usage in Features:**
```csharp
char[] pin = SecurePinPrompt.PromptForPin("Enter PIN:");
try
{
    // Use pin for authentication
    byte[] pinBytes = Encoding.UTF8.GetBytes(pin);
    try
    {
        await pivSession.VerifyPinAsync(new ReadOnlyMemory<byte>(pinBytes), ct);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(pinBytes);
    }
}
finally
{
    Array.Clear(pin); // Always zero credentials
}
```

**Rationale:** PINs, PUKs, and management keys are high-value credentials that could be extracted from memory dumps or debugger inspection. This pattern minimizes the window of exposure and follows CLAUDE.md security guidelines (line 69: "ALWAYS zero sensitive data").

---

## 7. UI Design

### Main Menu
```
╭─────────────────────────────────────────╮
│          PIV Tool - YubiKey             │
│         Serial: 12345678                │
│         Firmware: 5.4.3                 │
╰─────────────────────────────────────────╯

What would you like to do?

  ❯ 📋 Slot Overview
    🔐 PIN Management
    🔑 Key Generation
    📜 Certificate Operations
    ✍️  Sign / Decrypt
    🛡️  Key Attestation
    ⚙️  Device Info
    ⚠️  Reset PIV
    ─────────────────
    ❌ Exit
```

### PIN Entry
```
Enter PIN: ******
✓ PIN verified. 3 attempts remaining.
```

### Key Generation Flow
```
Select slot:
  ❯ 9a - Authentication
    9c - Digital Signature
    9d - Key Management
    9e - Card Authentication
    82-95 - Retired Slots

Select algorithm:
  ❯ ECC P-256 (Recommended)
    ECC P-384
    RSA 2048
    RSA 3072
    RSA 4096
    Ed25519
    X25519

Select PIN policy:
  ❯ Default
    Never
    Once
    Always

Select touch policy:
  ❯ Default
    Never
    Always
    Cached

⠋ Generating key pair in slot 9a...

✓ Key generated successfully!

┌────────────────────────────────────────┐
│ Public Key (ECC P-256)                 │
├────────────────────────────────────────┤
│ -----BEGIN PUBLIC KEY-----             │
│ MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE │
│ ...                                    │
│ -----END PUBLIC KEY-----               │
└────────────────────────────────────────┘
```

### Slot Overview
```
┌───────┬────────────┬──────────┬─────────┬─────────────┐
│ Slot  │ Algorithm  │ PIN      │ Touch   │ Certificate │
├───────┼────────────┼──────────┼─────────┼─────────────┤
│ 9a    │ ECC P-256  │ Once     │ Never   │ ✓           │
│ 9c    │ RSA 2048   │ Always   │ Always  │ ✓           │
│ 9d    │ -          │ -        │ -       │ -           │
│ 9e    │ ECC P-384  │ Never    │ Never   │ ✗           │
│ 82-95 │ (expand)   │          │         │             │
│ f9    │ ECC P-256  │ -        │ -       │ ✓ (Attest)  │
└───────┴────────────┴──────────┴─────────┴─────────────┘
```

---

## 8. API Coverage Checklist

| API Method | User Story | Feature File |
|------------|------------|--------------|
| `GetSerialNumberAsync` | US-1 | DeviceInfo.cs |
| `AuthenticateAsync` | US-2, US-3 | PinManagement.cs, KeyGeneration.cs |
| `VerifyPinAsync` | US-2, US-5 | PinManagement.cs, Crypto.cs |
| `ChangePinAsync` | US-2 | PinManagement.cs |
| `ChangePukAsync` | US-2 | PinManagement.cs |
| `UnblockPinAsync` | US-2 | PinManagement.cs |
| `SetPinAttemptsAsync` | US-2 | PinManagement.cs |
| `GetPinAttemptsAsync` | US-2 | PinManagement.cs |
| `GetPinMetadataAsync` | US-2 | PinManagement.cs |
| `GetPukMetadataAsync` | US-2 | PinManagement.cs |
| `GetManagementKeyMetadataAsync` | US-2 | PinManagement.cs |
| `SetManagementKeyAsync` | US-2 | PinManagement.cs |
| `GenerateKeyAsync` | US-3 | KeyGeneration.cs |
| `ImportKeyAsync` | US-3 | KeyGeneration.cs |
| `MoveKeyAsync` | US-3 | KeyGeneration.cs |
| `DeleteKeyAsync` | US-3 | KeyGeneration.cs |
| `GetCertificateAsync` | US-4 | Certificates.cs |
| `StoreCertificateAsync` | US-4 | Certificates.cs |
| `DeleteCertificateAsync` | US-4 | Certificates.cs |
| `SignOrDecryptAsync` | US-5 | Crypto.cs |
| `CalculateSecretAsync` | US-5 | Crypto.cs |
| `AttestKeyAsync` | US-6 | Attestation.cs |
| `GetSlotMetadataAsync` | US-7 | SlotOverview.cs |
| `GetBioMetadataAsync` | US-1 | DeviceInfo.cs |
| `ResetAsync` | US-8 | Reset.cs |

---

## 9. Out of Scope

- **Biometric operations:** `VerifyUvAsync`, `VerifyTemporaryPinAsync` (requires YubiKey Bio)
- **ECDH key agreement:** `CalculateSecretAsync` (advanced use case)
- **PIV data objects:** `GetObjectAsync`, `PutObjectAsync` (rarely used)
- **Multi-device orchestration:** Operating on multiple YubiKeys simultaneously
- **Automated testing:** Unit/integration tests for the example itself
- **Localization:** English only for this version
- **Configuration persistence:** No saving settings between runs

---

## 10. SDK Pain Points Section

During implementation, document any SDK usability issues in `./Yubico.YubiKit.Piv/examples/PivTool/SDK_PAIN_POINTS.md`:

### Template
```markdown
# SDK Pain Points - PIV Example

## Issue 1: [Title]
**Severity:** High | Medium | Low
**Category:** API Design | Documentation | Error Handling | Performance
**Description:** [What was difficult or unexpected]
**Workaround:** [How the example handles it]
**Suggestion:** [Proposed SDK improvement]

## Issue 2: ...
```

### Expected Categories
- Missing convenience methods
- Unclear error messages from SDK
- Missing documentation
- Inconsistent naming conventions
- Missing async overloads
- Difficult-to-use APIs

---

## 11. FIDO2 Template Notes

This PIV example serves as a template for the subsequent FIDO2 example PRD. Document learnings in `./docs/specs/piv-example-application/fido2_template_notes.md`:

### Structure Patterns
- Vertical slicing worked well for PIV, recommend for FIDO2
- `Shared/` directory pattern for common utilities
- Spectre.Console menu structure scales well

### Differences to Consider
- FIDO2 has credential management (enumerate, delete)
- FIDO2 has resident vs. non-resident credentials
- FIDO2 PIN semantics differ from PIV
- FIDO2 has user presence vs. user verification distinction

### LOC Baseline
- PIV example target: 2,000 lines
- FIDO2 estimate: 1,500 lines (fewer operations)

---

## 12. Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Total LOC | ≤ 2,000 | `wc -l **/*.cs` |
| Files per feature | 1 | Code review |
| Max file size | ≤ 400 lines | `wc -l` per file |
| SDK methods covered | 100% of core | API checklist |
| Time to understand | ≤ 15 min | User testing |
| Pain points documented | ≥ 5 | SDK_PAIN_POINTS.md |

---

## 13. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 0.1 | 2026-01-23 | spec-writer | Initial draft |
| 0.2 | 2026-01-23 | spec-writer | Security audit CRITICAL fixes |

---

## Revision Notes (v0.2)

**Security Audit Corrections - CRITICAL Issues Fixed:**

This revision addresses 3 CRITICAL security findings from the security audit (see `./docs/specs/piv-example-application/security_audit.md`):

### CRITICAL-001: PIN/PUK/Management Key Memory Zeroing
**Fixed in:**
- **Section 6 - Technical Design:** Added "Sensitive Credential Handling" section with explicit memory zeroing patterns for PIN, PUK, and management key data
- **US-2 - Acceptance Criteria:** Added requirements for zeroing all PIN/PUK/management key inputs after use
- **Code Examples:** Provided complete pattern showing `char[]` conversion, `CryptographicOperations.ZeroMemory()`, and `finally` block cleanup

**Impact:** Prevents credential extraction from memory dumps by explicitly zeroing sensitive buffers after use.

### CRITICAL-002: Default Credential Warning
**Fixed in:**
- **US-2 - Acceptance Criteria:** Added requirement to display metadata showing default PIN/PUK/management key status with visual warning indicator (🔓)
- **US-8 - Acceptance Criteria:** Added requirement to warn about default credentials after PIV reset and prompt immediate credential change
- **ES-4 - Edge Cases:** Added default credential detection error message with explicit warning

**Impact:** Ensures users are aware when YubiKey is in factory-default state (PIN: 123456, PUK: 12345678), preventing trivial physical access attacks.

### CRITICAL-003: Error Message Information Leakage
**Fixed in:**
- **Section 4 - Error States and Handling:** Added "Error Message Security Guidelines" section with safe vs. unsafe error pattern examples
- **NFR-4 - Security:** Added requirements preventing error messages from leaking internal exception details
- **Code Examples:** Provided exception handling pattern that logs technical details separately while showing user-friendly messages

**Impact:** Prevents information disclosure through error messages that could reveal cryptographic algorithm details, key types, or internal state to unauthenticated users.

**Sections Modified:**
- Section 2: User Stories (US-2, US-8 acceptance criteria)
- Section 4: Error States and Handling (new security guidelines, ES-4 edge cases)
- Section 5: Non-Functional Requirements (NFR-4 security enhancements)
- Section 6: Technical Design (new sensitive credential handling subsection)

**Sections Unchanged:**
- UX Design (Section 7) - passed UX audit
- DX patterns (API coverage, project structure) - passed DX audit
- All other functional requirements (FR-1 through FR-6)

**Audit Compliance:**
This revision brings the PRD into compliance with:
- OWASP Top 10 (SDK adaptation): Sensitive data exposure, security misconfiguration
- NIST SP 800-73-4: PIV credential management requirements
- CLAUDE.md Security Guidelines: Memory zeroing (line 69), crypto disposal
- SDK Memory Safety Patterns: `CryptographicOperations.ZeroMemory()`, secure buffer handling


---

## Audit Summary

All validators passed. This specification is approved for implementation.

| Audit | Result | Findings | Report |
|-------|--------|----------|--------|
| UX | PASS | 0 CRITICAL, 6 WARN | [ux_audit.md](./ux_audit.md) |
| DX | PASS | 0 CRITICAL, 4 WARN | [dx_audit.md](./dx_audit.md) |
| Technical | PASS | 0 CRITICAL, 5 WARN | [feasibility_report.md](./feasibility_report.md) |
| Security | PASS | 0 CRITICAL (3 fixed), 5 WARN | [security_audit.md](./security_audit.md) |

### Key Warnings to Address During Implementation

**Technical Warnings:**
- WARN-001: 15 SDK APIs need implementation (see feasibility report)
- WARN-002: LOC target may need adjustment to 2,500

**UX Warnings:**
- WARN-001: Add elapsed time updates for long operations
- WARN-006: Specify touch timeout duration (recommend 15s)

**DX Warnings:**
- WARN-002: Document memory management patterns explicitly
- WARN-004: Exception handling → user message translation

**Security Warnings:**
- WARN-001: Explicitly prohibit private key display
- WARN-002: Detail attestation certificate chain validation

---

## Next Steps

1. **Complete SDK Implementation** - Implement the 15 missing `IPivSession` methods (see feasibility_report.md WARN-001)
2. **Run `write-plan` skill** with this spec to create implementation plan
3. **Execute plan** using TDD workflow
4. **Request code review** before merge
5. **Document SDK pain points** in `SDK_PAIN_POINTS.md` during implementation
6. **Create FIDO2 PRD** using this as template (see Section 11: FIDO2 Template Notes)

---

## PRD Workflow Summary

- **Phase 1 (Define):** Draft created by spec-writer agent
- **Phase 2 (Validate):** UX + DX validators passed (parallel)
- **Phase 3 (Refine):** Skipped (no CRITICAL in Phase 2)
- **Phase 4 (Audit):** Technical validator passed, Security audit required self-correction (3 CRITICAL → 0)
- **Phase 5 (Finalize):** This document

**Total Iterations:** 2 (initial + 1 security correction)
**Final PRD Size:** ~700 lines

---

**End of Final Specification**
