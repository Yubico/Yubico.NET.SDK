---
name: security-guidelines
description: Use when auditing PRDs for security - OWASP, ZeroMemory, PIN handling, YubiKey-specific constraints (loaded by security-auditor agent)
---

# Security Guidelines for Yubico.NET.SDK

## Overview

This skill provides the rulebook for auditing Product Requirements Documents (PRDs) from a security perspective. It enforces security compliance BEFORE code is written, catching vulnerabilities at the design phase.

**Core principle:** Security-sensitive operations must be explicitly defined—implicit handling is a vulnerability.

## Use when

**Use this skill when:**
- Auditing a PRD as the `security-auditor` agent
- Reviewing sensitive data handling requirements
- Checking YubiKey-specific security constraints

**Don't use when:**
- Writing the PRD (use `spec-writing-standards`)
- Checking API naming (use `api-design-standards`)
- Checking error state completeness (use `ux-heuristics`)

## Sensitive Data Categories

| Category | Examples | Required Handling |
|----------|----------|-------------------|
| **Cryptographic Keys** | Private keys, session keys, KEK | `CryptographicOperations.ZeroMemory()` after use |
| **PINs** | User PIN, PUK, management key | Never log, zero after verification |
| **Biometric Data** | Fingerprint templates | Never persist outside YubiKey |
| **Attestation Data** | Certificates, signatures | Validate chain before trust |
| **Challenge/Response** | Nonces, HMAC secrets | Single use, time-bounded |

## Memory Safety Checklist

| Check | Requirement | CRITICAL if Missing |
|-------|-------------|---------------------|
| **Sensitive data zeroing** | All secrets must be zeroed after use | ✅ Yes |
| **No string conversion** | Secrets must never be converted to `string` (immutable, GC'd) | ✅ Yes |
| **Span/Memory preference** | Use `Span<byte>` over `byte[]` for secrets | ❌ WARN |
| **ArrayPool handling** | Pooled buffers must be cleared before return | ✅ Yes |
| **Exception safety** | Secrets zeroed even on exception path | ✅ Yes |

```csharp
// Correct pattern
Span<byte> pin = stackalloc byte[8];
try
{
    // Use pin
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
}

// NEVER: string pin = Encoding.UTF8.GetString(pinBytes); ❌
```

## YubiKey-Specific Security

### PIN Handling

| Aspect | Requirement |
|--------|-------------|
| **Retry counter** | PRD must define behavior when PIN is blocked |
| **Complexity** | Minimum PIN length/complexity if applicable |
| **Lock state** | Define behavior when device is locked |
| **PUK handling** | If PUK is involved, define reset flow |

### Touch Policy

| Policy | When Required |
|--------|---------------|
| **Touch required** | Key generation, signing operations |
| **Touch cached** | Repeated operations within timeout |
| **Never** | Only for low-sensitivity operations |

**Audit check:** Does PRD specify touch policy for sensitive operations?

### Attestation

| Check | Requirement |
|-------|-------------|
| **Certificate validation** | Attestation certs must be validated to Yubico root |
| **Freshness** | Attestation challenges must be fresh (nonce) |
| **Binding** | Attestation must be bound to the specific operation |

### Firmware Constraints

| Version | Security Implication |
|---------|---------------------|
| < 4.0 | Limited FIDO2 support |
| < 5.0 | No FIPS mode |
| < 5.3 | Limited credential management |

**Audit check:** Does PRD specify minimum firmware version?

## OWASP Top 10 (SDK Adaptation)

| OWASP | SDK Application | Audit Question |
|-------|-----------------|----------------|
| **Injection** | Command/APDU injection | Are all inputs validated before sending to device? |
| **Broken Auth** | PIN/credential bypass | Can operations be performed without proper auth? |
| **Sensitive Data** | Key/PIN exposure | Is sensitive data protected in memory and transit? |
| **XXE** | N/A for SDK | - |
| **Broken Access** | Privilege escalation | Can non-admin operations access admin functions? |
| **Misconfig** | Default insecure settings | Are defaults secure? Is "pit of success" secure? |
| **XSS** | N/A for SDK | - |
| **Insecure Deser** | CBOR/TLV parsing | Are parsers robust against malformed data? |
| **Components** | Vulnerable deps | Any known vulnerable dependencies? |
| **Logging** | Secret leakage in logs | Does PRD prohibit logging sensitive data? |

## Audit Report Template

Create `docs/specs/{feature}/security_audit.md`:

```markdown
# Security Audit Report

**PRD:** [Feature Name]
**Auditor:** security-auditor
**Date:** [ISO 8601 timestamp]
**Verdict:** PASS | FAIL

---

## Summary

| Severity | Count |
|----------|-------|
| CRITICAL | [n] |
| WARN | [n] |
| INFO | [n] |

**Overall:** [One sentence summary of findings]

---

## Sensitive Data Inventory

| Data Type | Identified In PRD | Handling Specified |
|-----------|-------------------|-------------------|
| [Type] | ✅/❌ | ✅/❌ |

---

## Findings

### CRITICAL-001: [Short Title]
**Section:** [PRD section reference]
**Vulnerability:** [What security issue exists]
**Impact:** [Potential attack vector or data exposure]
**Recommendation:** [Specific mitigation]

### WARN-001: [Short Title]
**Section:** [PRD section reference]
**Concern:** [Security improvement opportunity]
**Recommendation:** [Suggested improvement]

---

## Checklist Results

| Category | Check | Result | Notes |
|----------|-------|--------|-------|
| Memory | Sensitive data zeroing | ✅/❌ | |
| Memory | No string conversion | ✅/❌ | |
| YubiKey | PIN retry behavior | ✅/❌ | |
| YubiKey | Touch policy defined | ✅/❌ | |
| YubiKey | Attestation validation | ✅/❌ | |
| OWASP | Input validation | ✅/❌ | |
| OWASP | Auth required | ✅/❌ | |
| OWASP | No secret logging | ✅/❌ | |

---

## Verdict Justification

[Paragraph explaining why PASS or FAIL was chosen. FAIL requires at least one CRITICAL finding.]
```

## Severity Definitions

| Severity | Definition | Effect on Workflow |
|----------|------------|-------------------|
| **CRITICAL** | Security vulnerability or missing protection for sensitive data. | Triggers self-correction. PRD cannot proceed. |
| **WARN** | Suboptimal security or missing defense-in-depth. | Logged for spec-writer. Does not trigger loop. |
| **INFO** | Security hardening suggestion. | Logged for reference. |

## CRITICAL Triggers (Auto-Fail)

- Unhandled sensitive data (keys, PINs, secrets)
- Missing PIN verification for sensitive operation
- No defined behavior for locked/blocked device
- Error messages that leak internal state
- Missing attestation validation when attestation is used

## Verification

Audit is complete when:

- [ ] All sensitive data identified and handling specified
- [ ] PIN/auth requirements verified
- [ ] YubiKey-specific constraints checked
- [ ] OWASP concerns addressed
- [ ] Findings documented with impact
- [ ] Verdict is PASS or FAIL

## Related Skills

- `spec-writing-standards` - Template the PRD should follow
- `ux-heuristics` - Runs before (UX concerns)
- `api-design-standards` - Runs before (DX concerns)
