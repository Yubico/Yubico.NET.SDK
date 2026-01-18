---
name: spec-writing-standards
description: Use when writing PRDs - provides templates and INVEST model rules for product requirements (loaded by spec-writer agent)
---

# PRD Writing Standards

## Overview

This skill defines the rules and templates for writing Product Requirements Documents (PRDs) for the Yubico.NET.SDK. It ensures consistent, actionable specifications that can be validated by downstream agents.

**Core principle:** Focus on WHAT the user needs, not HOW it will be implemented.

## Use when

**Use this skill when:**
- Writing a new PRD for a feature
- Revising a PRD based on validator feedback
- Checking if a PRD meets quality standards

**Don't use when:**
- Writing implementation plans (use `write-plan`)
- Writing code documentation (use `write-module-docs`)
- Designing API signatures (that's the DX validator's concern)

## PRD Template

Create `docs/specs/{feature-slug}/draft.md`:

```markdown
# PRD: [Feature Name]

**Status:** DRAFT | VALIDATING | APPROVED
**Author:** spec-writer agent
**Created:** [ISO 8601 timestamp]
**Feature Slug:** [kebab-case-identifier]

---

## 1. Problem Statement

### 1.1 The Problem
[One paragraph describing the user pain point. Must be specific and measurable.]

### 1.2 Evidence
| Type | Source | Finding |
|------|--------|---------|
| Quantitative | [GitHub Issues / Support Tickets / Analytics] | [Specific numbers] |
| Qualitative | [User Interviews / Forum Posts / Stack Overflow] | [Direct quotes or summaries] |

### 1.3 Impact of Not Solving
[What happens if we don't build this? Who is affected and how?]

---

## 2. User Stories

### Story 1: [Primary Use Case]
**As a** [type of SDK user],
**I want to** [action],
**So that** [benefit].

**Acceptance Criteria:**
- [ ] [Testable criterion 1]
- [ ] [Testable criterion 2]
- [ ] [Testable criterion 3]

### Story 2: [Secondary Use Case]
[Same format...]

---

## 3. Functional Requirements

### 3.1 Happy Path
| Step | User Action | System Response |
|------|-------------|-----------------|
| 1 | [Action] | [Response] |
| 2 | [Action] | [Response] |

### 3.2 Error States (Unhappy Paths)
| Condition | System Behavior | Error Type |
|-----------|-----------------|------------|
| [When X happens] | [System does Y] | [Exception/Return code] |
| [When Y happens] | [System does Z] | [Exception/Return code] |

### 3.3 Edge Cases
| Scenario | Expected Behavior |
|----------|-------------------|
| Empty/null input | [Behavior] |
| Maximum bounds | [Behavior] |
| Concurrent access | [Behavior] |

---

## 4. Non-Functional Requirements

### 4.1 Performance
- [Latency requirements]
- [Memory constraints]

### 4.2 Security
- [Authentication requirements]
- [Sensitive data handling]

### 4.3 Compatibility
- [Supported platforms]
- [Minimum YubiKey firmware]

---

## 5. Technical Constraints

### 5.1 Must Use
- [Existing components that MUST be used]

### 5.2 Must Not
- [Patterns or approaches that are forbidden]

### 5.3 Dependencies
- [External dependencies required]

---

## 6. Out of Scope

- [Explicitly excluded feature 1]
- [Explicitly excluded feature 2]

---

## 7. Open Questions

- [ ] [Question 1 - needs resolution before implementation]
- [ ] [Question 2 - needs resolution before implementation]
```

## INVEST Model Checklist

Each user story MUST pass all six criteria:

| Criterion | Question | Fail Condition |
|-----------|----------|----------------|
| **I**ndependent | Can this story be implemented without depending on another story in this PRD? | Story references "after Story X is done" |
| **N**egotiable | Is the story focused on WHAT, not HOW? | Story contains implementation details (class names, algorithms) |
| **V**aluable | Does the story deliver value to the end user (not just the developer)? | Story is purely technical ("refactor X") |
| **E**stimable | Is there enough detail to estimate effort? | Vague terms like "handle errors appropriately" |
| **S**mall | Can this be implemented in ≤3 days? | Story covers multiple distinct behaviors |
| **T**estable | Can you write a test that proves this works? | Subjective criteria ("user feels confident") |

## Feature Slug Rules

Derive from PRD title:
- Lowercase
- Spaces to hyphens
- Remove special characters
- Max length: 50 characters

**Example:** "Add FIDO2 Resident Key Enumeration" → `add-fido2-resident-key-enumeration`

## Mandatory Requirements

**Every PRD MUST have:**
1. At least one user story with acceptance criteria
2. Error states defined for EVERY user action
3. Edge cases section (even if "N/A - no edge cases identified")
4. Out of Scope section (prevents scope creep)

**Every PRD MUST NOT have:**
1. Implementation details (class names, algorithms, data structures)
2. UI mockups or wireframes (SDK has no UI)
3. Timeline estimates (that's planning, not requirements)

## Revision Notes (Self-Correction)

When revising based on audit feedback:

1. Add a `## Revision Notes` section at the end
2. List what changed and why
3. Reference the audit finding being addressed

```markdown
## Revision Notes

### Revision 1 (2026-01-17)
- **Fixed CRITICAL-001 from dx_audit.md:** Changed `GetResidentKeys()` to `EnumerateCredentials()` to match existing pattern
- **Fixed WARN-001 from ux_audit.md:** Added empty state behavior in Section 3.3
```

## Verification

PRD is ready for validation when:

- [ ] All sections of template are filled (or explicitly marked N/A)
- [ ] Every user story passes INVEST checklist
- [ ] Every user action has an error state defined
- [ ] No implementation details present
- [ ] Feature slug is valid kebab-case

## Related Skills

- `ux-heuristics` - Loaded by UX validator to audit this PRD
- `api-design-standards` - Loaded by DX validator to audit this PRD
- `security-guidelines` - Loaded by security auditor to audit this PRD
