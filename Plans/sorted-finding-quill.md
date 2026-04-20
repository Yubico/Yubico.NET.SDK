# Codebase Consistency & Hygiene Assessment

**Branch:** `yubikit-applets`
**Date:** 2026-04-02

---

## Context

The SDK is functionally complete — all YubiKey applets implemented, 9/9 unit test projects passing. Before moving toward PR/merge, we need the codebase to look like it was written by one team following one set of principles. This assessment cross-references three sources: (1) the CLAUDE.md rules, (2) the canonical Python yubikey-manager patterns, and (3) an internal consistency audit.

---

## Assessment Summary

| Category | Status | Count | Priority |
|----------|--------|-------|----------|
| `== null` / `!= null` (should be `is null`) | Violation | ~101 | HIGH |
| `.ToArray()` in hot/crypto paths | Violation | 400+ total, ~60 critical | HIGH |
| `#region` blocks | Violation | ~153 | MEDIUM |
| Collection expressions (`new byte[]` → `[..]`) | Violation | 400+ | MEDIUM |
| Old-style `switch (` statements | Violation | ~48 | MEDIUM |
| Constructor-injected loggers (should be static) | Violation | ~22 | MEDIUM |
| `SequenceEqual` in security contexts | Security risk | ~5 | CRITICAL |
| File-scoped namespaces | Compliant | 0 violations | -- |
| Factory pattern consistency | Compliant | All modules | -- |
| Partial class organization | Compliant | PIV, OpenPGP, YubiOtp | -- |
| ZeroMemory usage | Mostly compliant | 79 files | -- |
| Feature gating pattern | Compliant | All modules | -- |

---

## Critical Findings

### 1. SECURITY: `SequenceEqual` in timing-sensitive contexts

5 production files use `.SequenceEqual()` where `CryptographicOperations.FixedTimeEquals()` is required:

- `src/Fido2/src/Credentials/AuthenticatorData.cs:194` — RP ID hash comparison
- `src/Fido2/src/LargeBlobs/LargeBlobData.cs:219` — hash comparison
- `src/Oath/src/Credential.cs:174` — credential ID comparison
- `src/Core/src/SmartCard/AnswerToReset.cs:42` — ATR equality
- `src/OpenPgp/src/CurveOid.cs:98` — OID byte comparison

**Risk:** Timing side-channels in hash/credential comparisons. The Fido2 and Oath ones are the most concerning.

### 2. Null-check style: `== null` → `is null` (~101 violations)

Scattered across all modules. Heaviest in:
- `src/Fido2/src/Extensions/ExtensionBuilder.cs` (multiple)
- `src/Core/src/SmartCard/Scp/ScpState.cs` (4 instances)
- `src/Core/src/Cryptography/RSAPublicKey.cs` (2 instances)
- `src/Piv/src/PivSession.Certificates.cs`

This is a mechanical find-and-replace but must be done carefully to avoid breaking `==` operator overloads on custom types.

### 3. Unnecessary `.ToArray()` in crypto/protocol paths

The CLAUDE.md rule: "NEVER use `.ToArray()` unless data must escape scope." Key violations in:
- `src/Piv/src/PivSession.Crypto.cs` — 7 instances in sign/decrypt/key-agreement
- `src/Piv/src/PivSession.KeyPairs.cs` — 8 instances in key generation
- `src/Piv/src/PivSession.Authentication.cs` — 4 instances in management key auth
- `src/OpenPgp/src/Kdf.cs` — 5 instances in KDF computation
- `src/OpenPgp/src/OpenPgpSession.Crypto.cs` — 3 instances

Many of these are in crypto hot paths where Span-based alternatives exist. Each needs case-by-case evaluation — some `.ToArray()` calls are necessary when data must be stored in fields.

### 4. `#region` blocks (~153 instances)

CLAUDE.md: "NEVER use `#region` (split large classes instead)." Heaviest in:
- `src/Core/src/SmartCard/SWConstants.cs` — 12 regions organizing SW categories
- `src/Piv/src/PivSession.cs` — 3 regions
- `src/Management/tests/.../FirmwareVersionTests.cs` — 10+ regions
- `src/YubiOtp/tests/.../SlotConfigurationTests.cs` — extensive
- Various test files across Fido2, Core

Test files are the worst offenders. Production code has fewer but they still exist.

### 5. Old-style switch statements (~48 instances)

Heaviest in:
- `src/Fido2/src/` — CBOR key parsing (AuthenticatorInfo, MakeCredentialResponse, GetAssertionResponse, ClientPin)
- `src/Oath/src/OathSession.cs` — TLV tag parsing
- `src/Piv/src/PivSession.cs` — TLV tag parsing
- `src/YubiOtp/examples/OtpTool/` — CLI argument parsing

Many of these are TLV/CBOR parsing switches with side effects (setting fields), which don't convert cleanly to switch expressions. Need case-by-case evaluation.

### 6. Constructor-injected loggers (~22 instances)

CLAUDE.md: "Use Static LoggingFactory - NEVER inject ILogger." Violations in:
- `src/SecurityDomain/src/SecurityDomainSession.cs`
- `src/Fido2/src/FidoSession.cs` and backends
- `src/YubiOtp/src/YubiOtpSession.cs`
- `src/Oath/src/OathSession.cs`
- `src/Core/src/SmartCard/PcscProtocol.cs`
- `src/Core/src/Hid/Fido/FidoHidProtocol.cs`

### 7. Collection expressions (~400+ opportunities)

`new byte[] { ... }` → `[...]`, `new List<T>()` → `[]`. Most common in:
- APDU/TLV construction (`new byte[] { subCommand }` → `[subCommand]`)
- List initialization (`new List<Credential>()` → `List<Credential> list = []`)
- Scattered across all modules

---

## Python yubikey-manager Alignment Check

The canonical Python SDK was analyzed for structural patterns. Key alignment findings:

| Pattern | Python | C# SDK | Aligned? |
|---------|--------|--------|----------|
| Per-app session classes | Yes (PivSession, OathSession, etc.) | Yes | Aligned |
| Private ctor + static factory | N/A (Python `__init__`) | Yes (CreateAsync) | Good (C# idiom) |
| APDU processor chain (decorator) | Yes (composable processors) | Yes (same architecture) | Aligned |
| TLV as encoding primitive | Yes (Tlv class with parse_dict/parse_list) | Yes (TlvHelper) | Aligned |
| Version-based feature gating | Yes (require_version) | Yes (Feature constants) | Aligned |
| Backend pattern (multi-transport) | Yes (ManagementSession: SmartCard vs OTP) | Yes (IManagementBackend, IYubiOtpBackend) | Aligned |
| Error hierarchy (typed exceptions) | Yes (InvalidPinError, ApduError, etc.) | Yes (ApduException, etc.) | Aligned |
| Immutable credential models | Yes (frozen dataclass) | Yes (records, readonly structs) | Aligned |
| SCP processor integration | Yes (ScpProcessor in chain) | Yes (same approach) | Aligned |
| Connection abstraction markers | Yes (SmartCard/Otp/FidoConnection) | Yes (ISmartCardConnection, etc.) | Aligned |

**Verdict:** The C# SDK is well-aligned architecturally with the Python canonical source. The structural patterns match. Differences are appropriate C#/.NET idioms (async factories, DI, generics over connection types). No major architectural gaps.

### One structural difference worth noting:

The Python SDK has `SecurityDomainSession` as a relatively flat class. The C# version is also monolithic (~984 lines in one file). Given the PIV/OpenPGP modules already use partial classes effectively, `SecurityDomainSession` should follow the same pattern.

---

## Execution Plan — Parallelized

These fixes are largely independent — they touch different aspects of each file and don't create merge conflicts when run simultaneously in worktrees. We group them into parallel tracks.

### Setup

All work happens in a single worktree branched from `yubikit-applets`:
```bash
git worktree add ../Yubico.NET.SDK-hygiene yubikit-applets -b codebase-hygiene
```
Agents work sequentially within this worktree (or in sub-worktrees off the hygiene branch). Final result merges back to `yubikit-applets` when ready.

### Wave 1: Six parallel agents (all independent, worktree-isolated)

Each agent works in its own sub-worktree off `codebase-hygiene`, verified independently, then merged back.

| Agent | Task | Scope | Est. Files |
|-------|------|-------|-----------|
| **A: security-timing** | Replace `SequenceEqual` → `FixedTimeEquals` in security contexts | 5 production files | 5 |
| **B: null-style** | Replace `== null`/`!= null` → `is null`/`is not null` | All modules | ~40 |
| **C: remove-regions** | Remove all `#region`/`#endregion` blocks | All modules + tests | ~30 |
| **D: collection-exprs** | Replace `new byte[]{}`, `new List<T>()` → collection expressions `[..]` | All modules | ~80 |
| **E: static-loggers** | Convert constructor-injected `ILogger` to static `LoggingFactory.CreateLogger<T>()` | Core, Fido2, Oath, YubiOtp, SecurityDomain | ~12 |
| **F: switch-exprs** | Convert old-style `switch(` to switch expressions where clean | Fido2, Oath, Piv, OpenPgp, YubiOtp | ~15 |

**Why these parallelize safely:**
- A touches comparison operators, B touches null checks, C touches region markers, D touches constructor expressions, E touches logger declarations, F touches switch blocks — all different syntactic elements, no overlap.
- Each agent runs `dotnet toolchain.cs build && dotnet toolchain.cs test` before committing.

### Wave 2: Sequential (depends on Wave 1 merge)

| Task | Description |
|------|-------------|
| **G: SecurityDomainSession split** | Split monolithic 984-line file into partial classes (Keys, Scp, Crypto, Reset) — same pattern as PIV/OpenPGP |
| **H: ToArray audit** | Case-by-case review of `.ToArray()` in crypto paths (Piv.Crypto, Piv.KeyPairs, Piv.Authentication, OpenPgp.Kdf, OpenPgp.Crypto). Replace with Span-based alternatives where data doesn't escape scope. |
| **I: dotnet format** | Run `dotnet format` across entire solution to catch any remaining style drift |

**Why sequential:** G is a structural refactor that moves code between files. H requires careful judgment about each call site. Both benefit from having Wave 1's mechanical cleanups already applied.

### Verification Strategy

**Per-agent (Wave 1):**
```bash
dotnet toolchain.cs build   # 0 errors
dotnet toolchain.cs test    # 9/9 passing
```

**After Wave 1 merge:**
```bash
dotnet toolchain.cs build   # 0 errors after merge
dotnet toolchain.cs test    # 9/9 passing after merge
dotnet format --verify-no-changes  # clean
```

**After Wave 2:**
```bash
dotnet toolchain.cs build
dotnet toolchain.cs test
dotnet format --verify-no-changes
# Spot-check: grep for residual violations
grep -rn "== null\|!= null" src/ --include="*.cs" | grep -v "test" | wc -l  # should be 0
grep -rn "#region" src/ --include="*.cs" | wc -l  # should be 0
```

---

## What's Already Good

The codebase has strong foundations that should be preserved:
- File-scoped namespaces everywhere
- Consistent factory pattern across all session classes
- Excellent ZeroMemory discipline (79 files, 290+ call sites)
- Well-organized partial classes in PIV, OpenPGP, YubiOtp
- Comprehensive feature gating with Version-based Feature constants
- Strong test infrastructure (WithYubiKey, test state, device filtering)
- Clean APDU processor chain architecture matching Python canonical source
- Per-module CLAUDE.md documentation
