---
name: plan-to-ralph
description: Use when converting implementation plans to progress files for ralph-loop execution
---

# Plan to Ralph Converter

## Overview

Converts implementation plans from the `write-plan` skill into progress files for autonomous execution via `ralph-loop`.

**Input:** `docs/plans/YYYY-MM-DD-feature.md` (from `write-plan`)
**Output:** `docs/ralph-loop/{feature}-progress.md` (for `ralph-loop`)

## Use when

- You have a completed implementation plan from `write-plan`
- Want to execute the plan autonomously via `ralph-loop`
- Need structured, resumable execution with progress tracking

**Don't use when:**
- Plan is still being developed (finish it first)
- Source is a PRD (use `prd-to-ralph` instead)
- Task is simple enough for ad-hoc mode

## Input Requirements

The plan should follow the `write-plan` format:

```markdown
# [Feature Name] Implementation Plan

**Goal:** [One sentence]
**Architecture:** [2-3 sentences]
**Tech Stack:** [Key technologies]

---

### Task N: [Component Name]

**Files:**
- Create: `path/to/file.cs`
- Test: `path/to/test.cs`

**Step 1: Write the failing test**
...
**Step 5: Commit**
...
```

## Process

### 1. Extract Plan Metadata

Read the plan header and extract:

| Plan Field | Maps To |
|------------|---------|
| `# [Feature Name]` | Progress file title + `feature` in frontmatter |
| `**Goal:**` | Reference in progress file header |
| Plan file path | `plan` field in frontmatter |

### 2. Group Tasks into Phases

Group related tasks into phases:

- **Phase 1 (P0):** Core implementation tasks (usually Tasks 1-3)
- **Phase 2 (P0):** Error handling / edge cases (if present)
- **Phase 3 (P1):** Extensions, optimizations (if present)
- **Final Phase (P0):** Security verification (always add)

**Grouping heuristics:**
- Tasks that share the same component/class → same phase
- Tasks explicitly marked as "error handling" → error handling phase
- Tasks marked as "optional" or "nice-to-have" → P1 or P2

### 3. Convert Tasks to Checkboxes

Each `### Task N:` becomes checkbox items:

**From plan:**
```markdown
### Task 2: Implement Parser

**Files:**
- Create: `src/Parser.cs`
- Test: `tests/ParserTests.cs`

**Step 1: Write the failing test**
...
```

**To progress file:**
```markdown
## Phase 1: Core Implementation (P0)

**Goal:** Implement the parser component
**Files:**
- Src: `src/Parser.cs`
- Test: `tests/ParserTests.cs`

### Tasks
- [ ] 1.1: Create Parser class structure
- [ ] 1.2: Implement Parse() method (happy path)
- [ ] 1.3: Add input validation
```

**Note:** Each plan "Task" typically becomes multiple progress file checkboxes (one per logical step).

### 4. Create Progress File

Create `docs/ralph-loop/{feature}-progress.md` with this structure:

```markdown
---
type: progress
feature: {feature-slug}
plan: docs/plans/YYYY-MM-DD-{feature}.md
started: {YYYY-MM-DD}
status: in-progress
---

# {Feature Name} Progress

## Phase 1: {Component Name} (P0)

**Goal:** {From task description or plan goal}
**Files:**
- Src: `{path from plan}`
- Test: `{path from plan}`

### Tasks
- [ ] 1.1: {First logical step}
- [ ] 1.2: {Second logical step}
- [ ] 1.3: {Third logical step}

### Notes

---

## Phase 2: {Next Component} (P0)

**Goal:** {Description}
**Files:**
- Src: `{path}`
- Test: `{path}`

### Tasks
- [ ] 2.1: ...

### Notes

---

## Phase N: Security Verification (P0)

**Goal:** Verify security requirements

### Tasks
- [ ] S.1: Audit sensitive data handling (ZeroMemory)
- [ ] S.2: Audit logging (no secrets)
- [ ] S.3: Audit input validation

### Notes
```

### 5. Launch Ralph Loop

```bash
bun .claude/skills/agent-ralph-loop/ralph-loop.ts \
  --prompt-file docs/ralph-loop/{feature}-progress.md \
  --completion-promise "{FEATURE}_COMPLETE" \
  --max-iterations 30 \
  --learn \
  --model claude-sonnet-4.5
```

## Example Conversion

**Input:** `docs/plans/2026-01-19-tlv-parser.md`

```markdown
# TLV Parser Implementation Plan

**Goal:** Create a TLV (Tag-Length-Value) parser for APDU responses
**Architecture:** Single class with static Parse method, returns structured TlvData
**Tech Stack:** C#, xUnit

---

### Task 1: Create TlvData Structure

**Files:**
- Create: `Yubico.YubiKit.Core/src/Tlv/TlvData.cs`
- Test: `Yubico.YubiKit.Core/tests/Tlv/TlvDataTests.cs`
...

### Task 2: Implement Parser

**Files:**
- Create: `Yubico.YubiKit.Core/src/Tlv/TlvParser.cs`
- Test: `Yubico.YubiKit.Core/tests/Tlv/TlvParserTests.cs`
...

### Task 3: Handle Nested TLV

**Files:**
- Modify: `Yubico.YubiKit.Core/src/Tlv/TlvParser.cs`
...
```

**Output:** `docs/ralph-loop/tlv-parser-progress.md`

```markdown
---
type: progress
feature: tlv-parser
plan: docs/plans/2026-01-19-tlv-parser.md
started: 2026-01-19
status: in-progress
---

# TLV Parser Progress

## Phase 1: Core TLV Structure (P0)

**Goal:** Create TlvData structure and basic parser
**Files:**
- Src: `Yubico.YubiKit.Core/src/Tlv/TlvData.cs`
- Test: `Yubico.YubiKit.Core/tests/Tlv/TlvDataTests.cs`

### Tasks
- [ ] 1.1: Create TlvData record with Tag, Length, Value properties
- [ ] 1.2: Add constructor validation
- [ ] 1.3: Implement equality comparison

### Notes

---

## Phase 2: Parser Implementation (P0)

**Goal:** Implement TLV parsing logic
**Files:**
- Src: `Yubico.YubiKit.Core/src/Tlv/TlvParser.cs`
- Test: `Yubico.YubiKit.Core/tests/Tlv/TlvParserTests.cs`

### Tasks
- [ ] 2.1: Create TlvParser class with Parse() method
- [ ] 2.2: Handle single TLV parsing
- [ ] 2.3: Handle multiple TLVs in sequence
- [ ] 2.4: Handle nested TLV structures

### Error Handling
- [ ] 2.5: Handle truncated data → throw TlvParseException
- [ ] 2.6: Handle invalid tag → throw TlvParseException

### Notes

---

## Phase 3: Security Verification (P0)

**Goal:** Verify security requirements

### Tasks
- [ ] S.1: Audit sensitive data handling
- [ ] S.2: Audit logging (no secrets)
- [ ] S.3: Audit input validation

### Notes
```

## Verification Criteria

The conversion is complete when:
1. **Progress file exists:** `docs/ralph-loop/{feature}-progress.md`
2. **Valid frontmatter:** Has `type: progress` and `plan` reference
3. **All tasks covered:** Every plan task maps to progress checkboxes
4. **Phases prioritized:** P0/P1/P2 markers present
5. **Security phase included:** Always add security verification

## Related Skills

- `write-plan` - Creates the plans this skill converts
- `ralph-loop` - Executes the progress file
- `prd-to-ralph` - Alternative converter for PRD specs
