---
name: porting-orchestrator
description: Use when porting features from yubikit-android - orchestrates extraction, validation, and audit of porting requirements
---

# Porting Orchestrator

## Overview

Orchestrates the porting of features from yubikit-android (Java) to Yubico.NET.SDK (C#) with full validation rigor. Uses the same swarm topology as `product-orchestrator`, but with `porting-spec-writer` instead of `spec-writer`.

**Core principle:** Porting is not "just copying"—it's extracting implicit requirements, validating them against SDK patterns, and ensuring security compliance before implementation.

## Path Rules (CRITICAL)

**When spawning agents, ALWAYS include this directive in the prompt:**

> "Use ONLY relative paths from the repository root (e.g., `./docs/specs/...`, `./.claude/skills/...`). NEVER construct absolute paths like `/home/*/...` or `/Users/*/...`."

All paths in this workflow are relative:
- `./docs/specs/{slug}/` - PRD artifacts (relative to repo root)
- `./.claude/skills/` - Skill definitions (relative to repo root)
- `./Yubico.YubiKit.*/` - Source code (relative to repo root)
- `../yubikit-android/` - Java reference (sibling directory)
- `../yubico-old/Yubico.NET.SDK/` - Old C# SDK reference (sibling directory)

**Allowed external paths (siblings only):**
- `../yubikit-android/` - Java YubiKit for porting reference
- `../yubico-old/Yubico.NET.SDK/` - Legacy C# SDK for migration reference

## Use when

**Use this skill when:**
- Porting a feature from `yubikit-android` to this SDK
- Need audit trail for ported functionality
- Want DX/security validation before implementation
- Feature is complex enough to warrant formal specification

**Don't use when:**
- Feature doesn't exist in Java (use `product-orchestrator` instead)
- Quick exploratory porting (use `yubikit-porter` directly)
- Simple bug fix or refactor

## Prerequisites

Verify Java source is accessible:
```bash
ls ../yubikit-android/
```

If not available, clone it first:
```bash
git clone https://github.com/Yubico/yubikit-android.git ../yubikit-android
```

## Workflow

```
Extract → Validate (UX + DX parallel) → Refine (if needed) → Audit (Tech + Sec) → Finalize
           ↑__________________________|
                (Self-Correction Loop)
```

Same flow as `product-orchestrator`, but:
- **Extract** uses `porting-spec-writer` (reads Java code)
- **Validate** same validators (check C# patterns, not Java)
- **Audit** same auditors (verify feasibility in C#)

## Process

### Phase 1: Extract (from Java)

1. **Parse Request**
   Identify the Java class/feature to port.

2. **Locate Java Source**
   ```bash
   find ../yubikit-android -name "{ClassName}.java" -type f
   ```

3. **Create Feature Slug**
   From Java class name: `PivSession` → `piv-session-port`

4. **Create Directory**
   ```bash
   mkdir -p docs/specs/{feature-slug}/
   ```

5. **Spawn Porting Spec Writer**
   Dispatch `porting-spec-writer` agent with:
   - Java file path(s)
   - Target C# module
   
   **Input:** Java source location
   **Output:** `docs/specs/{feature-slug}/draft.md`

6. **Report Progress**
   "Phase 1 complete. Extracted PRD from Java at `docs/specs/{slug}/draft.md`."

### Phase 2: Validate (Parallel)

Same as `product-orchestrator`:

7. **Spawn Validators in Parallel**
   - `ux-validator` → checks error states, empty states
   - `dx-validator` → checks C# naming, SDK patterns
   
   **Note:** Validators check against **C# conventions**, not Java.

8. **Collect Results**
   Wait for both audits.

9. **Check for CRITICAL Findings**
   Common porting issues:
   - Java naming → must convert to C# conventions
   - Java exceptions → must map to C# equivalents
   - Java patterns → must use existing `*Session` patterns

### Phase 3: Refine (Self-Correction Loop)

10. **If CRITICAL found:**
    Respawn `porting-spec-writer` with fixes.
    
    Common fixes:
    - Rename `getFoo()` → `GetFoo()` or property `Foo`
    - Map `IOException` → appropriate C# exception
    - Align with existing `*Session` method patterns

11. **Max iterations:** 3, then escalate to human.

### Phase 4: Audit (Sequential)

12. **Spawn Technical Validator**
    Checks:
    - P/Invoke availability for native calls
    - Existing C# infrastructure to build on
    - Breaking changes to existing C# API
    
    **Input:** `draft.md` + `dx_audit.md` + Java source
    **Output:** `feasibility_report.md`

13. **Spawn Security Auditor**
    Checks:
    - Sensitive data handling matches Java patterns
    - C# ZeroMemory requirements
    - PIN/key handling constraints
    
    **Input:** `draft.md` + `dx_audit.md` + Java source
    **Output:** `security_audit.md`

14. **If CRITICAL in audits:**
    Return to Phase 3.

### Phase 5: Finalize

15. **Create Final Spec**
    Create `docs/specs/{slug}/final_spec.md`:
    - Status: APPROVED
    - Include Java source inventory
    - Link all audit reports

16. **Report Completion**
    ```
    Porting PRD workflow complete.
    
    Feature: {name}
    Java Source: ../yubikit-android/{path}
    Location: docs/specs/{slug}/
    
    Artifacts:
    - draft.md (extracted from Java)
    - ux_audit.md (PASS)
    - dx_audit.md (PASS)
    - feasibility_report.md (PASS)
    - security_audit.md (PASS)
    - final_spec.md (APPROVED)
    
    Next step: Run `prd-to-ralph` skill with final_spec.md
    ```

## Artifact Locations

| Artifact | Location | Created By |
|----------|----------|------------|
| Draft PRD | `docs/specs/{slug}/draft.md` | porting-spec-writer |
| UX Audit | `docs/specs/{slug}/ux_audit.md` | ux-validator |
| DX Audit | `docs/specs/{slug}/dx_audit.md` | dx-validator |
| Feasibility | `docs/specs/{slug}/feasibility_report.md` | technical-validator |
| Security | `docs/specs/{slug}/security_audit.md` | security-auditor |
| Final Spec | `docs/specs/{slug}/final_spec.md` | orchestrator |

## Self-Correction Rules

Same as `product-orchestrator`:

```yaml
max_iterations: 3

on_critical:
  action: respawn_porting_spec_writer
  instruction: |
    Fix the CRITICAL issues identified in {audit_file}.
    Re-read the Java source if needed for clarification.
    Update only the sections referenced in the findings.

on_max_iterations:
  action: escalate_to_human
  message: |
    Self-correction failed after {n} attempts.
    Java source may have patterns that don't translate cleanly to C#.
    Human review required.
```

## Common Porting Issues (Auto-Detected by Validators)

| Java Pattern | Issue | C# Resolution |
|--------------|-------|---------------|
| `getFoo()` | Naming convention | Property `Foo` or method `GetFoo()` |
| `IOException` | Exception mapping | `YubiKeyException` or specific type |
| `byte[]` return | Memory safety | `ReadOnlySpan<byte>` or `ReadOnlyMemory<byte>` |
| `void close()` | Disposal pattern | `IDisposable.Dispose()` |
| `@Nullable` | Nullability | `?` suffix on type |
| Callbacks | Async pattern | `Task<T>` / `ValueTask<T>` |

## Example Session

**User:** "Orchestrate porting of PivSession from yubikit-android"

**Orchestrator:**
```
Checking Java source availability...
✓ Found ../yubikit-android/piv/src/main/java/com/yubico/yubikit/piv/PivSession.java

Phase 1: Extracting requirements from Java...
⏳ porting-spec-writer analyzing Java source...
✓ Created docs/specs/piv-session-port/draft.md
  - 12 user stories extracted
  - 8 error states identified
  - 3 security constraints noted

Phase 2: Validating against C# patterns (parallel)...
⏳ ux-validator running...
⏳ dx-validator running...
✓ UX audit: 0 CRITICAL, 2 WARN
✗ DX audit: 2 CRITICAL
  - "getCertificate() should be GetCertificate() or Certificate property"
  - "PivException should use existing YubiKeyException hierarchy"

Phase 3: Self-correction (iteration 1/3)...
⏳ Respawning porting-spec-writer with fixes...
✓ Draft updated with C# naming conventions
⏳ Re-running dx-validator...
✓ DX audit: PASS

Phase 4: Technical + Security audits...
⏳ technical-validator checking P/Invoke...
✓ Feasibility: PASS (can use existing SmartCard infrastructure)
⏳ security-auditor checking sensitive data...
✓ Security: PASS (ZeroMemory patterns identified)

Phase 5: Finalizing...
✓ Created docs/specs/piv-session-port/final_spec.md

Porting PRD complete. Ready for prd-to-ralph.
```

## Differences from product-orchestrator

| Aspect | product-orchestrator | porting-orchestrator |
|--------|---------------------|---------------------|
| Spec Writer | `spec-writer` (creates from scratch) | `porting-spec-writer` (extracts from Java) |
| Input | User's feature request | Java source path |
| Requirements | Invented/designed | Extracted/documented |
| Evidence | User research, issues | Existing Java implementation |
| Technical Validator | Checks feasibility | Also verifies Java→C# translation |

## Verification

Porting workflow is complete when:

- [ ] Java source was successfully analyzed
- [ ] `docs/specs/{slug}/` directory exists
- [ ] All 6 artifacts created
- [ ] All audits show PASS verdict
- [ ] `final_spec.md` has APPROVED status
- [ ] Java source inventory is documented
- [ ] No CRITICAL findings remain

## Related Skills

- `product-orchestrator` - For new features (not porting)
- `prd-to-ralph` - Next step after porting spec approved
- `yubikit-porter` - Direct porting without validation (faster, less rigorous)
- `yubikit-compare` - Compare Java and C# implementations
