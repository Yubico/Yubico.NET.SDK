---
name: api-design-standards
description: Use when auditing PRDs for API quality - .NET conventions, naming, Span/Memory patterns, consistency checks (loaded by dx-validator agent)
---

# API Design Standards for Yubico.NET.SDK

## Overview

This skill provides the rulebook for auditing Product Requirements Documents (PRDs) from a Developer Experience (DX) perspective. It ensures proposed APIs follow .NET conventions and maintain consistency with existing SDK patterns.

**Core principle:** New APIs must look and feel like they belong in the existing SDK.

## Use when

**Use this skill when:**
- Auditing a PRD as the `dx-validator` agent
- Reviewing proposed API naming
- Checking consistency with existing patterns

**Don't use when:**
- Writing the PRD (use `spec-writing-standards`)
- Checking error state completeness (use `ux-heuristics`)
- Reviewing security (use `security-guidelines`)

## .NET Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Classes/Structs | PascalCase | `PivSession`, `FidoCredential` |
| Interfaces | IPascalCase | `IYubiKeyDevice`, `IConnection` |
| Methods | PascalCase | `GetCertificate()`, `Sign()` |
| Properties | PascalCase | `SerialNumber`, `Version` |
| Parameters | camelCase | `pinVerificationRequired`, `slotNumber` |
| Private fields | _camelCase | `_connection`, `_session` |
| Constants | PascalCase | `DefaultTimeout`, `MaxRetries` |
| Enums | PascalCase (singular) | `PivAlgorithm.EccP256` |

## Existing SDK Patterns

### Session Pattern

All YubiKey applications use the `*Session` pattern:

```csharp
// Correct pattern
using var session = new PivSession(yubiKeyDevice);
session.VerifyPin(pin);
var cert = session.GetCertificate(slot);

// NOT: yubiKeyDevice.Piv.GetCertificate(slot) ❌
```

**Audit check:** Does proposed API fit the `*Session` pattern?

### Memory Management

From `CLAUDE.md`:

```csharp
// Prefer Span<T> for synchronous byte operations
public void Process(ReadOnlySpan<byte> input)

// Use Memory<T> for async operations
public async Task ProcessAsync(ReadOnlyMemory<byte> input)

// Pool large buffers
byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
try { /* use buffer */ }
finally { ArrayPool<byte>.Shared.Return(buffer); }

// Zero sensitive data
CryptographicOperations.ZeroMemory(sensitiveSpan);
```

**Audit check:** Does PRD specify memory handling for byte data?

### Async Pattern

```csharp
// Async methods end in "Async"
public async Task<T> OperationAsync(CancellationToken ct = default)

// Return ValueTask for frequently-completed operations
public ValueTask<T> GetCachedAsync()

// Always support cancellation for I/O
public async Task<T> SendCommandAsync(CancellationToken ct)
```

**Audit check:** Are I/O operations marked as potentially async?

### Error Handling

```csharp
// Specific exceptions, not generic
throw new PivPinException("PIN blocked after 3 failed attempts");

// Include actionable guidance
throw new InvalidOperationException(
    $"Session already has an active transaction. " +
    $"Call EndTransaction() before starting a new one.");

// Error codes for programmatic handling
public class YubiKeyException : Exception
{
    public StatusWord StatusWord { get; }
    public string Message { get; } // Human-readable
}
```

**Audit check:** Are error types and messages specified?

## API Design Checklist

| Check | Question | Severity if Violated |
|-------|----------|---------------------|
| **Naming** | Does naming follow .NET conventions? | CRITICAL |
| **Consistency** | Does it match existing `*Session` patterns? | CRITICAL |
| **Memory** | Is `Span<T>`/`Memory<T>` used for byte data? | WARN |
| **Async** | Do I/O operations have async variants? | WARN |
| **Errors** | Are specific exception types defined? | CRITICAL |
| **Overloads** | Are there simple defaults AND power-user options? | INFO |
| **Nullability** | Are nullable parameters marked with `?`? | WARN |

## Codebase Reference Points

When auditing, check these files for existing patterns:

| Pattern | Reference Location |
|---------|-------------------|
| PIV Session | `Yubico.YubiKit.Piv/src/PivSession.cs` |
| FIDO2 Session | `Yubico.YubiKit.Fido2/src/Fido2Session.cs` |
| OATH Session | `Yubico.YubiKit.Oath/src/OathSession.cs` |
| Management | `Yubico.YubiKit.Management/src/ManagementSession.cs` |
| Error handling | `Yubico.YubiKit.Core/src/Exceptions/` |
| Memory patterns | `Yubico.YubiKit.Core/src/Buffers/` |

## Audit Report Template

Create `docs/specs/{feature}/dx_audit.md`:

```markdown
# DX Audit Report

**PRD:** [Feature Name]
**Auditor:** dx-validator
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

## Findings

### CRITICAL-001: [Short Title]
**Section:** [PRD section reference]
**Issue:** [What violates standards]
**Existing Pattern:** [Reference to existing code]
**Recommendation:** [Specific fix]

### WARN-001: [Short Title]
**Section:** [PRD section reference]
**Issue:** [What could be improved]
**Recommendation:** [Suggested improvement]

---

## Checklist Results

| Check | Result | Notes |
|-------|--------|-------|
| Naming conventions | ✅/❌ | [Details] |
| Session pattern consistency | ✅/❌ | [Details] |
| Memory management | ✅/❌ | [Details] |
| Async patterns | ✅/❌ | [Details] |
| Error handling | ✅/❌ | [Details] |
| API surface minimalism | ✅/❌ | [Details] |

---

## Codebase References Checked

- [ ] Checked `Yubico.YubiKit.{relevant}/` for existing patterns
- [ ] Verified no naming conflicts with existing public API
- [ ] Confirmed consistency with related functionality

---

## Verdict Justification

[Paragraph explaining why PASS or FAIL was chosen.]
```

## Severity Definitions

| Severity | Definition | Effect on Workflow |
|----------|------------|-------------------|
| **CRITICAL** | Breaks consistency with existing API or violates .NET conventions. | Triggers self-correction loop. PRD cannot proceed. |
| **WARN** | Suboptimal but doesn't break consistency. | Logged for spec-writer. Does not trigger loop. |
| **INFO** | Suggestion for improvement. | Logged for reference. |

## CRITICAL Triggers (Auto-Fail)

- Naming conflicts with existing public API
- Breaking change to existing API signature
- Proposed pattern contradicts established `*Session` pattern
- Missing error handling for any operation

## Verification

Audit is complete when:

- [ ] All naming checked against conventions
- [ ] Existing patterns verified in codebase
- [ ] Memory handling specified or flagged
- [ ] Async requirements identified
- [ ] Findings documented with codebase references
- [ ] Verdict is PASS or FAIL

## Related Skills

- `spec-writing-standards` - Template the PRD should follow
- `ux-heuristics` - Runs in parallel (UX concerns)
- `security-guidelines` - Runs after (security concerns)
