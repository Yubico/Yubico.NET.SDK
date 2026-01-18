---
name: product-orchestrator
description: Use when creating PRDs - orchestrates spec-writer and validator agents through define→validate→audit→finalize workflow
---

# Product Orchestrator

## Overview

This skill orchestrates the "Product Swarm" to automate the definition, validation, and auditing of requirements before code is written. It manages the lifecycle of a PRD from user request to finalized specification.

**Core principle:** The orchestrator doesn't do the work—it delegates to specialized agents and manages state.

## Use when

**Use this skill when:**
- User requests "orchestrate a PRD for..."
- User wants to "create a spec for..."
- User says "design a feature for..."
- A feature needs full requirements definition

**Don't use when:**
- Writing implementation plans (use `write-plan`)
- PRD already exists and just needs one validator
- Quick exploratory discussion

## Workflow

```
Define → Validate (UX + DX parallel) → Refine (if needed) → Audit (Tech + Sec) → Finalize
         ↑__________________________|
              (Self-Correction Loop)
```

## Process

### Phase 1: Define

1. **Parse Request**
   Extract feature name and context from user request.

2. **Create Feature Slug**
   ```
   "Add FIDO2 Resident Key Enumeration" → "add-fido2-resident-key-enumeration"
   ```
   Rules: lowercase, spaces-to-hyphens, remove special chars, max 50 chars.

3. **Create Directory**
   ```bash
   mkdir -p docs/specs/{feature-slug}/
   ```

4. **Spawn Spec Writer**
   Dispatch `spec-writer` agent with user's request.
   
   **Input:** User's feature request + any context
   **Output:** `docs/specs/{feature-slug}/draft.md`

5. **Report Progress**
   "Phase 1 complete. Draft PRD created at `docs/specs/{slug}/draft.md`."

### Phase 2: Validate (Parallel)

6. **Spawn Validators in Parallel**
   Dispatch both validators simultaneously:
   
   - `ux-validator` → reads draft → writes `ux_audit.md`
   - `dx-validator` → reads draft → writes `dx_audit.md`

7. **Collect Results**
   Wait for both audits to complete.

8. **Check for CRITICAL Findings**
   Read both audit files. If either contains "CRITICAL":
   - Count CRITICAL findings
   - Extract issues to fix

### Phase 3: Refine (Self-Correction Loop)

9. **If CRITICAL found:**
   ```
   iteration = 1
   max_iterations = 3
   
   while has_critical AND iteration <= max_iterations:
       respawn spec-writer with instruction:
         "Fix CRITICAL issues from {audit_files}. Do NOT change passing sections."
       re-run failed validators
       iteration++
   ```

10. **If max iterations reached:**
    Escalate to human:
    ```
    "Self-correction failed after 3 attempts.
    Remaining issues: {n} CRITICAL, {m} WARN.
    Human review required."
    ```
    STOP workflow.

11. **Report Progress**
    "Phase 2/3 complete. All validations passed."

### Phase 4: Audit (Sequential)

12. **Spawn Technical Validator**
    Dispatch `technical-validator` agent.
    
    **Input:** `draft.md` + `dx_audit.md`
    **Output:** `feasibility_report.md`

13. **Spawn Security Auditor**
    Dispatch `security-auditor` agent.
    
    **Input:** `draft.md` + `dx_audit.md`
    **Output:** `security_audit.md`

14. **If CRITICAL in audits:**
    Return to Phase 3 (self-correction loop) with new audit files.

### Phase 5: Finalize

15. **Create Final Spec**
    Create `docs/specs/{slug}/final_spec.md`:
    - Copy draft.md content
    - Update status to APPROVED
    - Add "Audit Summary" section linking to all audit reports

16. **Report Completion**
    ```
    PRD workflow complete.
    
    Feature: {name}
    Location: docs/specs/{slug}/
    
    Artifacts:
    - draft.md (original + revisions)
    - ux_audit.md (PASS)
    - dx_audit.md (PASS)
    - feasibility_report.md (PASS)
    - security_audit.md (PASS)
    - final_spec.md (APPROVED)
    
    Next step: Run `write-plan` skill with final_spec.md to create implementation plan.
    ```

## Artifact Locations

| Artifact | Location | Created By |
|----------|----------|------------|
| Draft PRD | `docs/specs/{slug}/draft.md` | spec-writer |
| UX Audit | `docs/specs/{slug}/ux_audit.md` | ux-validator |
| DX Audit | `docs/specs/{slug}/dx_audit.md` | dx-validator |
| Feasibility | `docs/specs/{slug}/feasibility_report.md` | technical-validator |
| Security | `docs/specs/{slug}/security_audit.md` | security-auditor |
| Final Spec | `docs/specs/{slug}/final_spec.md` | orchestrator |

## Self-Correction Rules

```yaml
max_iterations: 3

on_critical:
  action: respawn_spec_writer
  instruction: |
    Fix the CRITICAL issues identified in {audit_file}.
    Do NOT change anything that passed validation.
    Update only the sections referenced in the findings.
    Add revision notes documenting changes.

on_warn:
  action: log_and_continue
  # Warnings passed to spec-writer but don't block

on_max_iterations:
  action: escalate_to_human
  message: |
    Self-correction failed after {n} attempts.
    Remaining issues: {critical_count} CRITICAL, {warn_count} WARN.
    Human review required before proceeding.
```

## Final Spec Template

```markdown
# PRD: [Feature Name] - FINAL

**Status:** APPROVED
**Author:** spec-writer agent
**Approved:** [ISO 8601 timestamp]
**Feature Slug:** [slug]

---

[Full content from draft.md after all revisions]

---

## Audit Summary

All validators passed. This specification is approved for implementation.

| Audit | Result | Report |
|-------|--------|--------|
| UX | PASS | [ux_audit.md](./ux_audit.md) |
| DX | PASS | [dx_audit.md](./dx_audit.md) |
| Technical | PASS | [feasibility_report.md](./feasibility_report.md) |
| Security | PASS | [security_audit.md](./security_audit.md) |

---

## Next Steps

1. Run `write-plan` skill with this spec to create implementation plan
2. Execute plan using TDD workflow
3. Request code review before merge
```

## Example Session

**User:** "Orchestrate a PRD for enumerating resident keys on a YubiKey"

**Orchestrator:**
```
Phase 1: Spawning Spec Writer...
✓ Created docs/specs/fido2-resident-key-enum/draft.md

Phase 2: Spawning Validators (parallel)...
⏳ ux-validator running...
⏳ dx-validator running...
✓ UX audit complete: 0 CRITICAL, 1 WARN
✗ DX audit: 1 CRITICAL - "Naming inconsistent with existing patterns"

Phase 3: Self-correction (iteration 1/3)...
⏳ Respawning spec-writer with fixes...
✓ Draft updated
⏳ Re-running dx-validator...
✓ DX audit: PASS

Phase 4: Technical + Security audits...
⏳ technical-validator running...
✓ Feasibility: PASS
⏳ security-auditor running...
✓ Security: PASS

Phase 5: Finalizing...
✓ Created docs/specs/fido2-resident-key-enum/final_spec.md

PRD workflow complete. Ready for implementation planning.
```

## Common Mistakes

**❌ Skipping validators:** Never skip UX or DX validation—they catch different issues.

**❌ Manual editing during workflow:** Let the self-correction loop handle fixes.

**❌ Ignoring WARN findings:** While they don't block, address them before finalization.

**❌ Proceeding after max iterations:** If self-correction fails, human review is mandatory.

## Verification

Workflow is complete when:

- [ ] `docs/specs/{slug}/` directory exists
- [ ] All 6 artifacts created
- [ ] All audits show PASS verdict
- [ ] `final_spec.md` has APPROVED status
- [ ] No CRITICAL findings remain

## Related Skills

- `spec-writing-standards` - Template for PRDs
- `write-plan` - Next step after PRD is approved
- `dispatch-agents` - Used internally for parallel validation
