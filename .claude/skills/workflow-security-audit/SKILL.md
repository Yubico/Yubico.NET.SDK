---
name: security-audit
description: Use when auditing the YubiKit codebase for sensitive data handling errors, security taxonomy violations, and memory-safety bugs. Two-phase: mechanical grep pass first, then semantic agent analysis for issues grep cannot detect.
---

# YubiKit Security Audit

## Overview

Detects sensitive-data-handling violations across the YubiKit codebase using a two-phase strategy:
- **Phase 1 (Mechanical):** Run `scripts/security-audit.sh` — 9 taxonomy checks via ripgrep. Fast, CI-compatible.
- **Phase 2 (Semantic):** Dispatch an agent for T5 (early return before ZeroMemory) and T7 (missing IDisposable on key-holding classes) — patterns that require control-flow or class-hierarchy analysis.

**Core principle:** Grep finds the obvious; the agent finds the subtle. Both are required for a complete audit.

## Use when

**Use when:**
- Running a security pass before a release or PR merge
- A new module was added and needs a security baseline
- Copilot or a reviewer flags a security taxonomy concern
- You want a CI-enforced security gate

**Don't use when:**
- Auditing PRDs (use `domain-security-guidelines` / `security-auditor` agent)
- General code quality scan (use global `/CodeAudit`)

---

## Taxonomy Reference

| ID | Name | FP Rate | Detection Method |
|----|------|---------|-----------------|
| T1 | `.ToArray()` copies of sensitive buffers | MEDIUM | grep |
| T2 | `Encoding.UTF8.GetBytes()` → intermediate `byte[]` | LOW | grep |
| T3 | `Convert.ToHexString()` inside `LogTrace`/`LogDebug` | MEDIUM | grep |
| T4 | `ArrayPool.Return` without prior `ZeroMemory` or `clearArray:true` | MEDIUM | grep |
| T5 | Early return before `ZeroMemory` (control-flow) | — | **AGENT ONLY** |
| T6 | `string` parameter for `pin`/`password`/`secret` in public API | MEDIUM | grep |
| T7 | `IDisposable` missing on class holding key material | — | **AGENT ONLY** |
| T8 | `Console.Write` in production source | LOW | grep |
| T9 | Crypto disposable (`AesCmac`/`IncrementalHash`) without `using` | HIGH | grep |

Run `scripts/security-audit.sh --help` for taxonomy descriptions and false-positive guidance.

---

## Phase 1: Mechanical Grep Scan

```bash
# Production scope only (src/*/src/**/*.cs)
./scripts/security-audit.sh

# All source including examples and tests
./scripts/security-audit.sh --all
```

**Interpret results:**
- Exit code 0 → no mechanical findings → proceed to Phase 2
- Exit code N → N findings require human triage before fixing
- Each finding shows taxonomy ID, false-positive rate, and guidance

**Known acceptable findings (do not fix):**
- T3: `PcscProtocol.cs` — AID selection is public data, not sensitive
- T1a: `ScpProcessor.cs` lines 82/114 — requires `ApduCommand` API change (tracked separately)

---

## Phase 2: Semantic Agent Analysis (T5 + T7)

Dispatch a `general-purpose` subagent with this prompt:

```
You are performing a YubiKit security audit for two taxonomy items that require semantic analysis.
This is a READ-ONLY pass — do NOT modify any files.

**Project root:** {cwd}
**Scope:** src/*/src/**/*.cs (production only, exclude obj/ and bin/)

---

## T5: Early Return Before ZeroMemory

Find try blocks that allocate a sensitive buffer but contain a return path
that exits the try BEFORE the finally { ZeroMemory() } executes.

Note: In C#, `finally` does run on `return` — so this only applies to patterns
where ZeroMemory is placed AFTER the try/finally block (not inside finally).

**What to look for:**
1. A method allocates a byte[] or rents from ArrayPool for a sensitive purpose
2. Inside the try block, there is an early `return` on an error path
3. ZeroMemory is called AFTER the try/finally, not inside the finally
4. This means the early-return path leaves the buffer un-zeroed

**Example — BAD:**
```csharp
var pin = ArrayPool<byte>.Shared.Rent(8);
try
{
    if (!Authenticate())
        return false;        // EARLY RETURN — ZeroMemory below never runs
    
    ProcessPin(pin);
}
finally
{
    ArrayPool<byte>.Shared.Return(pin);
}
CryptographicOperations.ZeroMemory(pin);   // BUG: unreachable on early return
```

**Example — GOOD:**
```csharp
var pin = ArrayPool<byte>.Shared.Rent(8);
try
{
    if (!Authenticate())
        return false;
    
    ProcessPin(pin);
}
finally
{
    CryptographicOperations.ZeroMemory(pin);   // Always runs, including on early return
    ArrayPool<byte>.Shared.Return(pin, clearArray: false);
}
```

---

## T7: Missing IDisposable on Class Holding Key Material

Find classes that:
1. Have a `byte[]` field named `_key`, `_sessionKey`, `_mac`, `_salt`, `_token`, 
   `_encKey`, `_macKey`, `_kek`, `_hmacKey`, or similar cryptographic-sounding name
2. Do NOT implement `IDisposable`
3. (Or implement `IDisposable` but do NOT call `CryptographicOperations.ZeroMemory()` 
   on the sensitive field in `Dispose()`)

**Also check:**
- Classes with `ReadOnlyMemory<byte>` properties backed by owned arrays (from `.ToArray()`)
  that aren't zeroed in `Dispose()` — see `KdfIterSaltedS2k` for a correct example.

---

## Output Format

### T5 Findings
- [T5] file:line — description of the early-return path and where ZeroMemory is misplaced
  Risk: which sensitive data escapes zeroing
  Fix: move ZeroMemory into the finally block

### T7 Findings
- [T7] file:line (class name) — field `_name : byte[]` not zeroed on disposal
  Risk: key material survives Dispose() call, lingering until GC
  Fix: implement IDisposable; call ZeroMemory(field) in Dispose()

### Clean Bill
If no findings: state "T5: No findings" and "T7: No findings" explicitly.
```

---

## Phase 3: Triage and Fix

For each finding:

1. **Check false-positive guidance** in `scripts/security-audit.sh --help` for the taxonomy ID
2. **Verify manually** — read the surrounding code, not just the matched line
3. **Fix pattern:**
   - T1/T2: Pass `ReadOnlyMemory<byte>` directly; use span overload of `Encoding.UTF8.GetBytes`
   - T3: Replace `Convert.ToHexString(buf)` with `{buf.Length} bytes`
   - T4: Add `CryptographicOperations.ZeroMemory(buf)` before `ArrayPool.Return`
   - T5: Move `ZeroMemory` into the `finally` block
   - T6: Change `string` parameter to `ReadOnlyMemory<byte>`
   - T7: Implement `IDisposable`; call `ZeroMemory` on each sensitive field
   - T8: Remove `Console.Write`; use `ILogger`
   - T9: Wrap with `using var mac = new AesCmac(...)`

4. **Re-run** `scripts/security-audit.sh` after fixes to confirm exit code 0

---

## CI Integration

Add to your CI pipeline:

```yaml
- name: Security Taxonomy Audit
  run: ./scripts/security-audit.sh
  # Exit code equals number of findings; non-zero fails the build
```

T5 and T7 (semantic checks) are agent-only and cannot be automated in CI.

---

## Verification

- [ ] `scripts/security-audit.sh` exits 0 (or all remaining findings are documented exceptions)
- [ ] T5 agent pass completed with no new findings
- [ ] T7 agent pass completed with no new findings
- [ ] Any exceptions are documented in this file under "Known acceptable findings"
- [ ] Fixes committed and pushed

## Related Skills

- `domain-security-guidelines` — PRD-phase security audit
- `domain-build` — Build before audit to catch compile errors
- `workflow-tdd` — Write tests for any new zeroing paths added
