---
name: review-ralph-learning
description: Use when assessing Ralph Loop learning reviews - presents filtered recommendations for user approval (never auto-applies)
---

# Review Ralph Loop Learning

## Overview

Analyze Ralph Loop learning review files and present a curated list of high-value improvements for user approval. **Never auto-apply changes.**

**Core principle:** Kill your darlings. Most learnings are noise. Only surface improvements that will save 10+ minutes of agent/developer time repeatedly.

## Use when

**Use this skill when:**
- A Ralph Loop completed with `--learn` flag
- Review file exists in `docs/ralph-loop/*/learning/`
- User wants to assess learnings for potential incorporation

**Don't use when:**
- Ralph Loop is still running
- No learning review file exists
- User wants immediate auto-application (this skill doesn't do that)

## Interaction Model

```
┌─────────────────────────────────────────────────────┐
│ Agent: Analyzes review, applies ruthless filtering   │
│        ↓                                            │
│ Agent: Presents ranked recommendations table         │
│        ↓                                            │
│ User:  Approves/rejects each item                   │
│        ↓                                            │
│ Agent: Implements ONLY approved items               │
└─────────────────────────────────────────────────────┘
```

**You are presenting options. User has final say.**

## Process

### 1. Locate Latest Review

```bash
ls -lt docs/ralph-loop/*/learning/review-*.md | head -1
```

Read from `## Summary` section onwards.

### 2. Kill Your Darlings Assessment

For each suggested skill/improvement, apply these filters:

| Filter | Question | Threshold |
|--------|----------|-----------|
| **Frequency** | How often will this apply? | Must apply to >20% of future tasks |
| **Time saved** | How much time does it save per use? | Must save >10 min per occurrence |
| **Specificity** | Is it generic or domain-specific? | Generic → skill; Domain → CLAUDE.md |
| **Existing coverage** | Is it already documented? | Skip if documented elsewhere |
| **Complexity** | How hard to capture in a skill? | Skip if requires complex judgment |

**Kill criteria (reject if ANY apply):**
- ❌ One-off pattern unlikely to recur
- ❌ Already covered in existing skill/documentation
- ❌ Too specific to single technology/module
- ❌ Requires human judgment to apply correctly
- ❌ Marginal improvement (<5 min saved)

### 3. Present Recommendations Table

Output a table for user review:

```markdown
## Recommended Incorporations

| # | Learning | Type | Destination | Est. Value | Rationale |
|---|----------|------|-------------|------------|-----------|
| 1 | [Name] | Skill/Doc/Prompt | [Location] | High/Med | [Why worth it] |
| 2 | [Name] | Skill/Doc/Prompt | [Location] | High/Med | [Why worth it] |

## Rejected (Kill Your Darlings)

| Learning | Reason for Rejection |
|----------|---------------------|
| [Name] | Too specific to FIDO2 module |
| [Name] | Already in build skill |
| [Name] | One-off, unlikely to recur |
```

### 4. Wait for User Approval

**DO NOT PROCEED until user confirms.**

Example prompt:
> "Review the recommendations above. Reply with the numbers you want implemented (e.g., '1, 3') or 'none' to skip."

### 5. Implement Approved Items Only

For each approved item:

| Type | Action |
|------|--------|
| New skill | Invoke `write-skill` skill first, then create |
| Module CLAUDE.md | Add under appropriate section |
| Prompt guidance | Update `write-ralph-prompt/SKILL.md` |
| Loop mechanics | Discuss approach before implementing |

## Value Assessment Guide

### High Value (Implement)

- Pattern that caused 2+ wasted iterations in the review
- Generic refactoring pattern (applies to any language/framework)
- Missing verification step that caused repeated failures
- Prompt structure issue affecting completion detection

### Medium Value (Consider)

- Improvement to existing skill documentation
- Domain knowledge that will be reused in this project
- Test pattern specific to this tech stack but reusable

### Low Value (Skip)

- Single module's internal patterns (put in module CLAUDE.md)
- Obvious patterns any developer would know
- Improvements to tools/commands (already in build skill)
- "Nice to have" without clear time savings

## Example Output

```markdown
## Review: review-2026-01-17.md

### Recommended Incorporations

| # | Learning | Type | Destination | Est. Value | Rationale |
|---|----------|------|-------------|------------|-----------|
| 1 | Interface refactor pattern | Skill | `.claude/skills/workflow-interface-refactor/` | High | Caused 2 wasted iterations; generic pattern |
| 2 | Test audit before refactor | Prompt | `write-ralph-prompt/SKILL.md` | High | Universal; prevents reactive test fixing |
| 3 | Phase deferral documentation | Prompt | `write-ralph-prompt/SKILL.md` | Med | Improves prompt clarity |

### Rejected (Kill Your Darlings)

| Learning | Reason for Rejection |
|----------|---------------------|
| `transport-validation` skill | YubiKey-specific; add to FIDO2 CLAUDE.md instead |
| `cbor-test-patterns` skill | FIDO2-specific; add to FIDO2 CLAUDE.md instead |
| Pre-iteration checkpoint | Already captured in metrics |
| Git history check | Already in autonomy directives |

---

**Reply with numbers to implement (e.g., "1, 2, 3") or "none" to skip.**
```

## Verification

Review complete when:

- [ ] All learnings categorized with clear rationale
- [ ] Kill-your-darlings filter applied (most learnings rejected)
- [ ] Recommendations presented in table format
- [ ] User approval received before any implementation
- [ ] Only approved items implemented

## Related Skills

- `write-skill` - Required before creating new skills
- `write-module-docs` - For updating module CLAUDE.md files
- `write-ralph-prompt` - For prompt structure improvements
