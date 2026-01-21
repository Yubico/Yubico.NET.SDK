---
name: ux-heuristics
description: Use when auditing PRDs for usability - Nielsen's heuristics adapted for SDK, error states, and empty state checklists (loaded by ux-validator agent)
---

# UX Heuristics for SDK Design

## Overview

This skill provides the rulebook for auditing Product Requirements Documents (PRDs) from a user experience perspective. Even SDKs have "UX"—error messages, exception patterns, and API behavior ARE the user experience.

**Core principle:** Every user action must have defined success AND failure behaviors.

## Use when

**Use this skill when:**
- Auditing a PRD as the `ux-validator` agent
- Reviewing error handling completeness
- Checking if unhappy paths are defined

**Don't use when:**
- Writing the PRD (use `spec-writing-standards`)
- Checking API naming conventions (use `api-design-standards`)
- Reviewing security concerns (use `security-guidelines`)

## SDK UX Heuristics (Nielsen's Adapted)

| # | Heuristic | SDK Application | Audit Question |
|---|-----------|-----------------|----------------|
| 1 | **Visibility of system status** | Methods should indicate progress for long operations | Does the PRD define how long-running operations report progress? |
| 2 | **Match between system and real world** | Use domain terminology (FIDO2, PIV, not internal jargon) | Are all terms defined or referenced to YubiKey documentation? |
| 3 | **User control and freedom** | Operations should be cancellable where possible | Can the user abort/cancel operations? Is this defined? |
| 4 | **Consistency and standards** | Follow existing SDK patterns and .NET conventions | Does the proposed API match existing `*Session` patterns? |
| 5 | **Error prevention** | Validate inputs; make invalid states unrepresentable | Are preconditions checked? Can the user avoid errors? |
| 6 | **Recognition over recall** | Intellisense-friendly APIs; enums over magic strings | Are options discoverable via IDE? No stringly-typed APIs? |
| 7 | **Flexibility and efficiency** | Provide both simple and advanced overloads | Is there a "pit of success" default AND power-user options? |
| 8 | **Aesthetic and minimalist design** | Don't expose unnecessary complexity in public API | Is the API surface minimal? No leaking of internal concepts? |
| 9 | **Help users recognize and recover from errors** | Exceptions should be specific and actionable | Do error messages explain WHAT failed and HOW to fix it? |
| 10 | **Help and documentation** | XML docs, examples, and migration guides | Does the PRD require documentation for the feature? |

## Error State Checklist

For EVERY user action in the PRD, verify:

| Check | Question | CRITICAL if Missing |
|-------|----------|---------------------|
| **Failure defined** | What happens when this action fails? | ✅ Yes |
| **Error type specified** | Is it an exception? Return code? Both? | ✅ Yes |
| **Recovery guidance** | Does the error tell the user how to fix it? | ❌ No (WARN) |
| **Retry semantics** | Can the user retry? Is it safe to retry? | ❌ No (WARN) |

## Empty State Checklist

| Scenario | Question | Severity if Missing |
|----------|----------|---------------------|
| **No data** | What happens if the collection is empty? | WARN |
| **First use** | Is there onboarding/setup guidance? | INFO |
| **Data deleted** | What happens after data is removed? | WARN |

## SDK-Relevant WCAG Considerations

| WCAG Principle | SDK Application | Audit Check |
|----------------|-----------------|-------------|
| **Perceivable** | Error messages must be clear text, not just codes | Are all error codes accompanied by human-readable messages? |
| **Operable** | APIs must work in constrained environments | Does the PRD consider headless/server scenarios? |
| **Understandable** | Consistent behavior across similar operations | Do similar methods behave consistently? |
| **Robust** | Works with assistive tooling (screen readers for IDEs) | Are XML docs complete for all public members? |

## Audit Report Template

Create `docs/specs/{feature}/ux_audit.md`:

```markdown
# UX Audit Report

**PRD:** [Feature Name]
**Auditor:** ux-validator
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
**Section:** [PRD section reference, e.g., "3.2 Error States"]
**Issue:** [What is wrong or missing]
**Impact:** [Why this matters]
**Recommendation:** [Specific fix]

### WARN-001: [Short Title]
**Section:** [PRD section reference]
**Issue:** [What could be improved]
**Recommendation:** [Suggested improvement]

### INFO-001: [Short Title]
**Section:** [PRD section reference]
**Note:** [Observation or suggestion, non-blocking]

---

## Checklist Results

| Heuristic | Result | Notes |
|-----------|--------|-------|
| 1. Visibility of system status | ✅/❌ | [Details] |
| 2. Match system and real world | ✅/❌ | [Details] |
| 3. User control and freedom | ✅/❌ | [Details] |
| 4. Consistency and standards | ✅/❌ | [Details] |
| 5. Error prevention | ✅/❌ | [Details] |
| 6. Recognition over recall | ✅/❌ | [Details] |
| 7. Flexibility and efficiency | ✅/❌ | [Details] |
| 8. Minimalist design | ✅/❌ | [Details] |
| 9. Error recovery | ✅/❌ | [Details] |
| 10. Documentation | ✅/❌ | [Details] |

---

## Verdict Justification

[Paragraph explaining why PASS or FAIL was chosen. FAIL requires at least one CRITICAL finding.]
```

## Severity Definitions

| Severity | Definition | Effect on Workflow |
|----------|------------|-------------------|
| **CRITICAL** | Blocks implementation. Missing required information or fundamental UX flaw. | Triggers self-correction loop. PRD cannot proceed. |
| **WARN** | Should be addressed but doesn't block. Suboptimal UX or missing edge case. | Logged for spec-writer to address. Does not trigger loop. |
| **INFO** | Observation or suggestion. Nice-to-have improvement. | Logged for reference. No action required. |

## CRITICAL Triggers (Auto-Fail)

- Missing error state for ANY user action
- No defined behavior for empty/null results
- Error messages that don't explain the problem
- Missing acceptance criteria on any user story

## Verification

Audit is complete when:

- [ ] All 10 heuristics evaluated against PRD
- [ ] Every user action checked for error state
- [ ] Empty state behavior verified
- [ ] Findings documented with section references
- [ ] Verdict is PASS (no CRITICAL) or FAIL (has CRITICAL)

## Related Skills

- `spec-writing-standards` - Template the PRD should follow
- `api-design-standards` - Runs in parallel (DX concerns)
- `security-guidelines` - Runs after (security concerns)
