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
| T5 | Early return before `ZeroMemory` (ZeroMemory after try/finally) | — | **AGENT ONLY** |
| T6 | `string` parameter for `pin`/`password`/`secret` in public API | MEDIUM | grep |
| T7 | `IDisposable` missing on class holding key material | — | **AGENT ONLY** |
| T8 | `Console.Write` in production source | LOW | grep |
| T9 | Crypto disposable (`AesCmac`/`IncrementalHash`) without `using` | HIGH | grep |
| T10 | `IDisposable` not disposed on exception/failure paths | — | **AGENT ONLY** |
| T11 | Conditional `byte[]` allocation not covered by `ZeroMemory` | — | **AGENT ONLY** |
| T12 | `Dispose()` zeros caller-provided buffer (ownership violation) | — | **AGENT ONLY** |

**T10 vs T7 vs T9:** T7 = class never *has* IDisposable. T9 = crypto object created without `using`. T10 = object IS IDisposable and created correctly, but exception/failure paths skip `Dispose()` entirely.

**T11 vs T5:** T5 = `ZeroMemory` placed *after* the try/finally (wrong position). T11 = `ZeroMemory` IS inside finally, but the buffer was allocated conditionally (inside `if`) so the finally has no reference to zero it.

**T12 distinction:** All other entries are about *failing* to zero. T12 is zeroing memory you don't own — a correctness and ownership bug that can corrupt caller-held data.

**Known API limitation (T1 subtype):** `ApduCommand` internally does `Data = data?.ToArray()`, creating an untracked clone that callers cannot zero. This is a framework-level issue; flag only at the `ApduCommand` API design layer, not at individual call sites.

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

## Phase 2: Semantic Agent Analysis (T5, T7, T10, T11, T12)

Dispatch a `general-purpose` subagent with this prompt:

```
You are performing a YubiKit security audit for taxonomy items that require semantic analysis.
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

## T10: IDisposable Not Disposed on Exception/Failure Paths

Find methods that create an `IDisposable` object holding key material, where the
object is NOT wrapped in `using var` and NOT disposed in all exit paths (including
exception paths and early-return/failure conditions).

**Distinct from T7** (class lacks IDisposable) **and T9** (crypto obj without `using`).
T10 = the class IS IDisposable, but callers don't call Dispose() on failure paths.

**What to look for:**
1. `var x = new SomeDisposable(...)` without `using var` in a method that creates
   objects holding session keys, MAC chains, or other sensitive material
2. The method has a failure path (throw, early return on non-success status word)
   where `x.Dispose()` is never called
3. A `finally` block either doesn't exist or doesn't dispose `x`

**Key classes to focus on:** `ScpProcessor`, `ScpState`, `SessionKeys`, any class
implementing `IDisposable` in `src/Core/src/SmartCard/Scp/`

**Example — BAD:**
```csharp
var processor = new ScpProcessor(state);
await TransmitAsync(authCommand);          // throws on auth failure
// processor.Dispose() never called if throw
```

**Example — GOOD:**
```csharp
var processor = new ScpProcessor(state);
try
{
    await TransmitAsync(authCommand);
}
catch
{
    processor.Dispose();
    throw;
}
```

---

## T11: Conditional Buffer Allocation Not Covered by ZeroMemory

Find methods where a `byte[]` holding sensitive data is allocated inside a
conditional branch (e.g. `if (encrypt)`, `if (compress)`), but the `finally`
block's ZeroMemory call only covers unconditionally-allocated variables.

**Distinct from T5:** T5 = ZeroMemory is in the wrong position (after try/finally).
T11 = ZeroMemory IS in the finally, but the conditionally-allocated buffer is
outside its scope because the variable was declared inside the `if` block.

**What to look for:**
1. A method has a `try/finally` with ZeroMemory calls in the finally
2. Inside the `try`, there is an `if` branch that allocates a new `byte[]`
   for sensitive data (encryption output, padding, etc.)
3. The `finally` does NOT zero this conditionally-allocated variable
4. The variable goes out of scope without zeroing (declared inside the `if` block)

**Example — BAD:**
```csharp
try
{
    if (encrypt)
    {
        byte[] encryptedData = State.Encrypt(plaintext);   // allocated in branch
        // ... use encryptedData
    }
}
finally
{
    ZeroMemory(scpCommandData);   // zeros unconditional buffer
    ZeroMemory(mac);              // zeros unconditional buffer
    // encryptedData is never zeroed — it's out of scope here
}
```

**Example — GOOD:**
```csharp
byte[]? encryptedData = null;
try
{
    if (encrypt)
    {
        encryptedData = State.Encrypt(plaintext);
    }
}
finally
{
    if (encryptedData is not null)
        ZeroMemory(encryptedData);
    ZeroMemory(scpCommandData);
    ZeroMemory(mac);
}
```

---

## T12: Dispose() Zeros Caller-Provided Buffer (Ownership Violation)

Find `Dispose()` methods that call `CryptographicOperations.ZeroMemory()` on
a field whose backing array was provided by the caller (via constructor parameter
or property setter), rather than allocated by this class itself.

**This is an inverted pattern** — all other taxonomy items are about *failing* to
zero. T12 is zeroing memory *you don't own*, which corrupts the caller's view.

**What to look for:**
1. A class receives `ReadOnlyMemory<byte>` or `byte[]` from a constructor parameter
2. The class stores it in a field
3. `Dispose()` calls `ZeroMemory` on that field's backing array
4. The caller may still hold a reference to the same memory and see unexpected zeros

**Key question:** Was the backing array allocated by *this class* (e.g. via `.ToArray()`
on a span, or via `ArrayPool.Rent()`)? If so, zeroing is correct. If the memory
came from the caller's allocation, zeroing it is an ownership violation.

---

## Output Format

### T5 Findings
- [T5] file:line — description of early-return path and where ZeroMemory is misplaced
  Risk: which sensitive data escapes zeroing
  Fix: move ZeroMemory into the finally block

### T7 Findings
- [T7] file:line (class name) — field `_name : byte[]` not zeroed on disposal
  Risk: key material survives Dispose(), lingering until GC
  Fix: implement IDisposable; call ZeroMemory(field) in Dispose()

### T10 Findings
- [T10] file:line (method name) — `ClassName` created but not disposed on [failure path]
  Risk: session keys / MAC chain in memory until GC
  Fix: use `using var`, or dispose in catch/finally

### T11 Findings
- [T11] file:line — `variableName` allocated inside `if (condition)`, not zeroed in finally
  Risk: sensitive bytes remain on heap after method returns
  Fix: declare variable before try block (= null); ZeroMemory if not null in finally

### T12 Findings
- [T12] file:line (class name) — Dispose() zeros `_fieldName` which was caller-provided
  Risk: caller's memory unexpectedly zeroed; potential corruption of shared buffer
  Fix: only zero memory this class allocated; document caller retains zeroing responsibility

### Clean Bill
State "Tx: No findings" explicitly for each taxonomy checked.
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
