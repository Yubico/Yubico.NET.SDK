---
name: write-ralph-prompt
description: Use when crafting Ralph Loop prompts - ensures proper verification and exit criteria
---

# Writing Ralph Loop Prompts

This skill provides guidance for creating Ralph Loop prompts that enforce proper verification, phased exit criteria, and robust completion requirements.

**Reference:** `.claude/skills/agent-ralph-loop/SKILL.md` (loop semantics + required autonomy injection).

**Logs/state:** `./docs/ralph-loop/` (state.md, iteration-*.log, and learning artifacts under `./docs/ralph-loop/learning/`).

**Save location:** `./docs/plans/ralph-loop/YYYY-MM-DD-<feature-name>.md` (offer to save; create directories as needed).

## Use when
- Creating a new Ralph Loop prompt for a coding task
- Ensuring a Ralph Loop does not complete prematurely
- Enforcing build/test verification before completion

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

### 7. One Phase Per Iteration
- Design prompts so the agent completes **one phase**, verifies, and commits per iteration.
- This keeps iterations short, maximizing fresh context window for each phase.
- Avoids context compaction and "context rot" from overly long sessions.
- The ralph-loop will restart with full context on the next iteration.
- **Exception:** If phases are small and related, the agent may complete multiple per iteration.
- Example phase boundaries:
  ```markdown
  ## Phase 1: Create interfaces
  Files: IFoo.cs, IBar.cs
  Verify: `dotnet build.cs build`
  Commit: "feat(core): add IFoo and IBar interfaces"
  → Output <promise>PHASE_1_DONE</promise>

  ## Phase 2: Implement classes
  Files: Foo.cs, Bar.cs
  Verify: `dotnet build.cs build`
  Commit: "feat(core): implement Foo and Bar"
  → Output <promise>PHASE_2_DONE</promise>

  ## Phase 3: Add tests
  Files: FooTests.cs, BarTests.cs
  Verify: `dotnet build.cs test`
  Commit: "test(core): add Foo and Bar tests"
  → Output <promise>ALL_DONE</promise>
  ```

## Prompt Template

```markdown
# [Feature Name] Implementation Plan (Ralph Loop)

**Goal:** [One sentence describing what this builds]
**Architecture:** [2-3 sentences about approach]
**Tech Stack:** [Key technologies/libraries]
**Completion Promise:** <PROMISE_TOKEN>

---

## Task 1: [Component]

**Files:**
- Create: `exact/path/to/file.cs`
- Modify: `exact/path/to/existing.cs:123-145`
- Test: `tests/exact/path/to/test.cs`

**Step 1: Write the failing test**
- Include complete test code (no placeholders)

**Step 2: Run test to confirm failure**
Run: `dotnet build.cs test --filter "FullyQualifiedName~TestName"`
Expected: FAIL (describe expected failure)

**Step 3: Minimal implementation**
- Include complete implementation code

**Step 4: Re-run test to confirm pass**
Run: `dotnet build.cs test --filter "FullyQualifiedName~TestName"`
Expected: PASS

**Step 5: Commit**
```bash
git add <paths>
git commit -m "feat: <message>"
```

---

## Verification Requirements (MUST PASS BEFORE COMPLETION)

1. Build: `dotnet build.cs build` (must exit 0)
2. Test: `dotnet build.cs test` (all tests must pass)
3. No regressions: existing tests pass, new code covered

Only after ALL pass, output <promise>{COMPLETION_PROMISE}</promise>.
If any fail, fix and re-verify.

```

> **Note:** Autonomy directives and skill awareness are auto-injected by `ralph-loop.ts`. Do not add them manually. The agent will see available skills (mandatory vs optional) at the start of each iteration.

### 8. Test Audit for Refactoring Phases

When a phase involves interface changes, signature changes, or dependency refactoring, add a test audit step:

```markdown
**Step 0: Audit Test Impact**
Before modifying any class constructor or interface:
```bash
grep -r "Mock<.*ClassName>" tests/ --include="*.cs"
grep -r "Substitute.For<ClassName>" tests/ --include="*.cs"
```
Document which tests need mock updates BEFORE making code changes.
```

### 9. Phase Deferral Documentation

If a phase should be deferred, document clearly:

```markdown
**Phase N: [Name] - DEFERRED**
- **Reason:** [low priority / requires hardware / needs investigation / blocked by X]
- **What's needed to complete:** [specific requirements]
- **Blocks completion?** [Yes/No - if No, other phases can complete]
```

This prevents ambiguity about what "skipped" means and whether the overall task can still complete.

## Anti-Patterns to Avoid
- Vague completion criteria ("when finished")
- Missing test requirement ("when code compiles")
- Wrong commands (`dotnet test` instead of `dotnet build.cs test`)
- No failure guidance
- Using `git add .` or `git add -A` blindly
- **Cramming multiple phases into one iteration** (causes context rot)
- **Skipping test audit** before interface/signature changes (causes reactive test fixing)

## Handoff (ALWAYS END WITH THIS)

After presenting (and optionally saving) the plan, print the exact one-liner to start the loop:

```bash
bun .claude/skills/agent-ralph-loop/ralph-loop.ts --prompt-file ./docs/plans/ralph-loop/<plan-file>.md --completion-promise "<PROMISE_TOKEN>" --max-iterations 20 --learn --model claude-opus-4.5
```
