---
name: writing-ralph-prompts
aliases: [ralph-prompt-writing, ralph-loop-prompt]
description: Guidance for writing effective Ralph Loop prompts with proper verification and exit criteria. Use for any Ralph Loop coding task, especially when completion criteria or verification steps are unclear.
---

# Writing Ralph Loop Prompts

## Overview

This skill provides expert guidance for creating Ralph Loop prompts that enforce proper verification, phased exit criteria, and robust completion requirements. It ensures that Copilot CLI agents iteratively refine their work and only complete when all requirements are met and verified.

**Reference:** `.claude/skills/ralph-loop/SKILL.md` (loop semantics + required autonomy injection).

**Logs/state:** the loop writes `state.md`, `iteration-*.log`, and learning artifacts under `./docs/ralph-loop/`.

**Preferred workflow:** produce a prompt file and run `ralph-loop` with `--prompt-file` (avoids shell escaping issues).

**Offer to save the plan/prompt file to:** `./docs/plans/ralph-loop/YYYY-MM-DD-<feature-name>.md` (create directories as needed).

**Always end by printing the one-liner to start the loop:**

```bash
bun .claude/skills/ralph-loop/ralph-loop.ts --prompt-file ./docs/plans/ralph-loop/<plan-file>.md --completion-promise "<PROMISE_TOKEN>" --max-iterations 20 --learn
```

## When to Use
- Creating a new Ralph Loop prompt for a coding task
- Ensuring a Ralph Loop does not complete prematurely
- Enforcing build/test verification before completion
- Improving prompt quality for iterative agent workflows

## Core Principles

### 1. Never Trust "Done" Without Verification
- Always require explicit build and test verification before completion.
- Example (bad):
  ```
  Output <promise>DONE</promise> when all tasks verified.
  ```
- Example (good):
  ```markdown
  ## Verification Requirements (MUST PASS BEFORE COMPLETION)
  1. Build: `dotnet build.cs build` (must exit 0)
  2. Test: `dotnet build.cs test` (all tests must pass)
  3. No regressions: existing tests pass, new code covered
  Only after ALL pass, output <promise>DONE</promise>.
  If any fail, fix and re-verify.
  ```

### 2. Git Commit Discipline
- Only commit files you created/modified in this session.
- Never use `git add .` or `git add -A` blindly.
- Example safe pattern:
  ```bash
  git status
  git add path/to/your/file.cs
  git commit -m "feat(scope): description"
  ```

### 3. Explicit Command References
- Use build/test commands from CLAUDE.md, not raw dotnet commands.
- Example:
  | Action | Command |
  |--------|---------|
  | Build | `dotnet build.cs build` |
  | Test | `dotnet build.cs test` |
  | Coverage | `dotnet build.cs coverage` |

### 4. Phased Exit Criteria
- For complex tasks, require phase completion:
  ```markdown
  ## Exit Criteria by Phase
  - Implementation: files created, build passes
  - Testing: unit tests written, all tests pass
  - Integration: code integrated, manual smoke test (if needed)
  Only output <promise>DONE</promise> when ALL phases complete.
  ```

### 5. Failure Handling
- Specify what to do if build or tests fail:
  ```markdown
  ## On Failure
  If build fails: fix errors, re-run build
  If tests fail: fix, re-run ALL tests
  Do NOT output completion until all green
  ```

### 6. Hardware/Integration Test Handling
- Hardware tests are best-effort; document and skip after 2-3 failures.
- Mark with `[Trait("RequiresHardware", "true")]`. Unit tests MUST pass.

## Template: Ralph Loop Prompt
```markdown
# [Task Title]

**Goal:** [One-sentence goal]
**Scope:** [What's in/out of scope]

---
## Context
[Background, reference files, architecture notes]
---
## Tasks
- [ ] Task 1: [Name]
- [ ] Task 2: [Name]
---
## Build & Test Commands
```bash
dotnet build.cs build
dotnet build.cs test
```
---
## Verification Checklist
- [ ] Build exits 0
- [ ] All tests pass
- [ ] No regressions
- [ ] CLAUDE.md guidelines followed
---
## Completion
Only after ALL verification passes, output:
<promise>DONE</promise>
If verification fails: fix and re-verify.
```

## Anti-Patterns to Avoid
- Vague completion criteria ("when finished")
- Missing test requirement ("when code compiles")
- Wrong commands ("dotnet test" instead of build.cs)
- No failure guidance

## Example: Fixing a Weak Prompt
**Before (weak):**
```markdown
# Add HID Support
Implement HID device support for macOS.
Output <promise>DONE</promise> when all tasks verified.
```
**After (strong):**
```markdown
# Add HID Support
**Goal:** Implement HID device support for macOS.
## Tasks
1. Create MacOSHidDevice.cs
2. Create MacOSHidConnection.cs
3. Add unit tests
4. Integrate with FindYubiKeys
## Verification
```bash
dotnet build.cs build
dotnet build.cs test
```
Checklist:
- [ ] Build passes
- [ ] All unit tests pass
- [ ] CLAUDE.md patterns followed
Only after verification passes:
<promise>DONE</promise>
```
