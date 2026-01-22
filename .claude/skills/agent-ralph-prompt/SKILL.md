---
name: write-ralph-prompt
description: Use when crafting Ralph Loop prompts - ensures proper verification and exit criteria
---

# Writing Ralph Loop Prompts (Ad-hoc Mode)

This skill provides guidance for creating Ralph Loop prompts **when NOT using a progress file**. For structured, multi-phase work, use `prd-to-ralph` or `plan-to-ralph` instead - they create progress files that auto-inject the execution protocol.

**When to use this skill:**
- Ad-hoc tasks without a progress file
- Quick one-off tasks ("fix this test", "refactor these files")
- Tasks that don't need phased tracking

**When NOT to use this skill:**
- PRD-driven implementation → use `prd-to-ralph`
- Plan-driven implementation → use `plan-to-ralph` (or create progress file manually)
- These create progress files with `type: progress` frontmatter, and `ralph-loop.ts` auto-injects the execution protocol.

**Reference:** `.claude/skills/agent-ralph-loop/SKILL.md` (loop semantics + progress file format).

**Logs/state:** `./docs/ralph-loop/<session>/` (state.md, iteration-*.log, and learning artifacts under `./docs/ralph-loop/<session>/learning/`).

**Save location:** `./docs/plans/ralph-loop/YYYY-MM-DD-<feature-name>.md` (offer to save; create directories as needed).

## Use when
- Creating ad-hoc Ralph Loop prompts (no progress file)
- Quick one-off automation tasks
- Tasks that don't need progress tracking

## Core Principles

### 0. Pre-Flight State Verification (MANDATORY)

Before starting ANY work, verify actual progress state:

```bash
# Check for existing commits matching this feature
git log --oneline -5 --grep="feature-name"

# If progress file exists, verify checkbox state
grep -c "^\- \[x\]" progress.md  # Completed tasks
grep -c "^\- \[ \]" progress.md  # Pending tasks
```

**Rule:** If mismatch between prompt message and evidence, **trust the evidence**.

The prompt may contain completion promises as instructions (e.g., "output `<promise>DONE</promise>` when finished"). This does NOT mean work is already done. Always verify via git history and progress file checkboxes.

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

### 2a. Batch Commit Strategy for Refactoring

Do NOT blindly commit after each phase. Group by logical concern:

**Separate commits for:**
- Test changes vs implementation changes (different concerns)
- Different utilities/patterns being introduced
- Different risk profiles

**Single commit for:**
- Multiple phases using same utility (e.g., all TLV refactoring together)
- Related refactorings with no behavioral change
- Changes that must be reviewed together

**Example:**
```bash
# Phase 1: Test changes (separate concern)
git commit -m "test(piv): replace hardcoded sizes with KeyDefinitions"

# Phases 2-7: All implementation refactoring (same concern)
git commit -m "refactor(piv): replace manual TLV parsing with Tlv/TlvHelper

- PivSession.Crypto.cs: use Tlv.Create for response parsing
- PivSession.KeyPairs.cs: use TlvHelper.DecodeDictionary
- PivSession.Metadata.cs: use TlvHelper for metadata extraction
- (etc.)"
```

**Why:** Cleaner git history, easier code review, 28% faster execution (1 build vs N builds).

### 3. Explicit Command References
- Use build/test commands from CLAUDE.md, not raw dotnet commands.
- Example:
  | Action | Command |
  |--------|---------|
  | Build | `dotnet build.cs build` |
  | Test | `dotnet build.cs test` |
  | Coverage | `dotnet build.cs coverage` |

### 3a. Using Directives First (Before Refactoring)

When refactoring code to use a new utility/namespace:

1. **Identify all files** that will use the utility
2. **Add using directives to all files FIRST** (before editing logic)
3. **Then perform refactoring**
4. **Build once** (not per file)

```bash
# ❌ BAD: Refactor first, fix using directives after build fails
# Results in "type or namespace not found" errors

# ✅ GOOD: Add using directives proactively
# Step 1: Add to all target files
for f in File1.cs File2.cs File3.cs; do
  # Add using directive at top of file
done

# Step 2: Now refactor logic
# Step 3: Build once - no namespace errors
```

**Why:** Prevents "type not found" build failures and reduces build cycles.

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

### 8. Test Infrastructure Study (Before First Test)

Before writing integration tests in a new module, study existing test patterns:

```markdown
**Step 0: Study Test Infrastructure**
Before writing tests, examine existing test files:
```bash
# Find similar integration test
find . -name "*IntegrationTests.cs" -path "*/Yubico.YubiKit.*" | head -1

# Extract patterns
grep -A3 "WithYubiKey" <file>  # Check attribute syntax (MinFirmware vs MinimumFirmware)
grep "state\." <file>          # Check state property name (Device vs YubiKey)
```
Use discovered patterns for all test methods in this module.
```

**Why:** Test framework patterns vary between modules. Studying existing tests prevents attribute/property errors that cause multiple fix cycles.

### 9. Incremental Build Gates

After creating 3+ files, run intermediate build before proceeding:

```markdown
**Step 2.5: Incremental Build Verification**
After creating 3+ files, run intermediate build:
```bash
dotnet build Yubico.YubiKit.<Module>/Yubico.YubiKit.<Module>.csproj
```
Fix compilation errors before proceeding to next files.
DO NOT create all implementation files without intermediate checks.
```

**Why:** Batch creation without validation causes cascading errors. Incremental builds catch issues early, reducing fix cycles by ~40%.

### 10. Test Audit for Refactoring Phases

When a phase involves interface changes, signature changes, or dependency refactoring, add a test audit step:

```markdown
**Step 0: Audit Test Impact**
Before modifying any class constructor or interface:
```bash
grep -r "Mock<.*ClassName>" tests/ --include="*.cs"
grep -r "Substitute.For<ClassName>" tests/ --include="*.cs"
```
Document which tests need mock updates BEFORE making code changes.

### 11. Phase Deferral Documentation

If a phase should be deferred, document clearly:

```markdown
**Phase N: [Name] - DEFERRED**
- **Reason:** [low priority / requires hardware / needs investigation / blocked by X]
- **What's needed to complete:** [specific requirements]
- **Blocks completion?** [Yes/No - if No, other phases can complete]
```

This prevents ambiguity about what "skipped" means and whether the overall task can still complete.

### 11a. Completion Integrity (CRITICAL)

**You CANNOT claim completion if ANY task is skipped or incomplete:**

```markdown
## Completion Integrity Rules

1. **Progress File Honesty:**
   - Only check `[x]` for tasks you ACTUALLY completed with verification
   - Skipped tasks remain `[ ]` - NEVER mark them complete
   - Deferred tasks get `[DEFERRED]` tag but stay unchecked

2. **Completion Promise:**
   - ONLY emit `<promise>DONE</promise>` when ALL tasks are `[x]`
   - If ANY task is unchecked → DO NOT emit promise
   - Next iteration will continue the work

3. **"Time Constraints" is NOT an excuse:**
   - If you're running low on time, finish current task and stop
   - Do NOT skip tasks and claim completion
   - Do NOT mark progress file as complete with unchecked items

**Anti-pattern (FORBIDDEN):**
```
- [x] Task 1: Create tests ✅
- [ ] Task 2: Edge cases (skipped - time constraints)  ← UNCHECKED
- [x] Task 3: Commit ✅
<promise>DONE</promise>  ← FORBIDDEN - Task 2 incomplete!
```

**Correct behavior:**
```
- [x] Task 1: Create tests ✅
- [ ] Task 2: Edge cases  ← Still unchecked
(No promise emitted - next iteration will continue)
```
```

**Why:** Ralph cannot self-congratulate for skipping work. Incomplete progress files trigger another iteration.

### 12. Test-First Discipline

For code changes, run existing tests BEFORE editing to establish baseline:

```markdown
## Test Discipline
For each code change:
1. ✅ Run existing test to see current behavior (baseline)
2. ✅ Edit code to improve behavior
3. ✅ Run test again to verify improvement
4. ✅ Commit

This ensures tests actually verify your changes, not just that code compiles.
```

**Why:** Build-then-test (reactive) catches errors late. Test-first (proactive) establishes baseline and validates the change actually improved behavior.

### 13. Documentation Inline (Same Commit)

Documentation is part of the feature, not a separate phase:

```markdown
## Documentation Updates
When modifying infrastructure or public API:
- Update relevant docs (TESTING.md, README.md) in the SAME commit as code
- Add docstrings to new public methods
- Update progress file immediately after each change

Do NOT batch documentation into a separate phase.
```

**Why:** Separate documentation phases cause drift—details forgotten, examples outdated. Inline documentation captures intent while fresh.

### 14. Template Reference Mandate (Documentation Tasks)

Before creating any documentation file, study existing patterns:

```markdown
## Documentation Creation Rules

Before creating ANY documentation file:
1. MUST view existing file of same type for pattern reference
2. MUST replicate structure and style
3. MUST adapt content to module context

Template Locations:
- README.md: See existing module README.md (e.g., Yubico.YubiKit.Core/README.md)
- CLAUDE.md: See existing module CLAUDE.md (e.g., Yubico.YubiKit.Core/CLAUDE.md)
- tests/CLAUDE.md: See existing tests/CLAUDE.md (e.g., Yubico.YubiKit.Management/tests/CLAUDE.md)
```

**Why:** Prevents style drift and ensures SDK-wide consistency. Agent naturally created consistent docs in audit session by studying templates first.

### 15. Final Verification Checklist

The final phase must include explicit verification steps:

```markdown
## Final Phase: Verification (MANDATORY)

Before delivering completion promise:

1. **Build Verification**
   Run: `dotnet build.cs build`
   Must: Exit 0 with no errors

2. **Coverage Check** (for documentation tasks)
   Verify all target files created/updated

3. **Commit History**
   Run: `git log --oneline -10`
   Verify: One commit per phase, conventional format

4. **Progress File**
   All tasks marked [x] or [SKIPPED] with reason

Only after ALL pass, deliver: <promise>COMPLETION_TOKEN</promise>
```

**Why:** Prevents premature completion claims. Explicit checklist catches missed steps.

### 16. Build Error Baseline

Before starting work, capture baseline build errors to distinguish pre-existing issues:

```markdown
## Build Error Baseline (Start of Session)

Run BEFORE making any changes:
```bash
dotnet build.cs build 2>&1 | grep -E "error (CS|MSB)" | sort > /tmp/baseline-errors.txt || true
```

After each phase, compare:
```bash
dotnet build.cs build 2>&1 | grep -E "error (CS|MSB)" | sort > /tmp/current-errors.txt || true
NEW_ERRORS=$(comm -13 /tmp/baseline-errors.txt /tmp/current-errors.txt)
if [ -n "$NEW_ERRORS" ]; then
    echo "⚠️ NEW BUILD ERRORS (fix before proceeding):"
    echo "$NEW_ERRORS"
fi
```

- Pre-existing errors: Ignore (not your responsibility)
- New errors: Fix before proceeding
```

**Why:** Saves 2-3 minutes per iteration by eliminating manual error investigation. Prevents false attribution of pre-existing failures.

### 17. Parallel Tool Calls for Exploration

When exploring codebases, use PARALLEL tool calls for efficiency:

```markdown
## Efficient Exploration Pattern

❌ SLOW (sequential):
1. view file1.cs → wait
2. view file2.cs → wait  
3. view file3.cs → wait

✅ FAST (parallel):
Use codemapper (SKILL) to generate and query structural maps of C# codebases using CodeMapper. Maps show public/internal API surface with line numbers, enabling fast codebase orientation without reading every file.

Call view(file1.cs), view(file2.cs), view(file3.cs) in SAME response

Apply to:
- Viewing multiple test files for pattern discovery
- Checking multiple API classes simultaneously
- Reading related documentation files
- Examining similar implementations across modules
```

**Why:** 30-40% reduction in exploration time (3-4 minutes saved per session).

### 18. Test Bug vs Implementation Bug Checklist

When tests fail, distinguish between test bugs and real implementation bugs:

```markdown
## Test Failure Triage

When a test fails, check BEFORE assuming implementation bug:

- [ ] **Right key/cert?** Is the test using the correct key pair (device vs software)?
- [ ] **Device-specific assertions?** Are expected values based on device behavior that may vary?
- [ ] **Simpler test passes?** Does a simpler test for the same functionality pass?

**Red flags for TEST bugs:**
- Test creates software key but verifies device signature
- Hardcoded validity periods/dates that depend on device RTC
- Assertions that work on one device model but fail on another
- Complex workflow test fails but atomic operation tests pass

**Action:** If simpler test passes, the workflow test likely has a bug in setup/assertions, not the implementation.
```

**Why:** Saved 13+ minutes in PIV debug session. Agent initially assumed implementation bugs but the test created a software key for cert verification while signing with YubiKey—mismatched keys.

### 19. Priority Announcement at Session Start

Announce priority decisions BEFORE starting implementation:

```markdown
## Priority Declaration (First Action)

At the START of each iteration, BEFORE any implementation:

1. Scan all phases and identify P0, P1, P2 priorities
2. Announce: "Focusing on P0 phases: {list}. P1 phases ({list}) will be completed in subsequent iterations if time permits."
3. Begin P0 work only

If mid-session you realize time is constrained:
- Finish current P0 task
- Commit progress
- Do NOT skip to completion
- Let next iteration handle remaining tasks
```

**Why:** User can object to priority decisions early. Prevents mid-session justifications for skipped work.

### 20. Security Audit Checklist (For Crypto/Sensitive Data Tasks)

For any task involving cryptographic code, PIN/PUK handling, or sensitive data, include a security verification section:

```markdown
## Security Verification (MUST PASS BEFORE COMPLETION)

Run these commands and verify expected results:

```bash
# Verify ZeroMemory usage for sensitive buffers
grep -c "ZeroMemory" path/to/module/src/*.cs
# Expected: >= N (one per sensitive buffer)

# Verify no plaintext secrets in logs
grep -rn "Log.*\(pin\|key\|puk\|secret\)" path/to/module/src/ --include="*.cs"
# Expected: 0 matches (or only variable names, never values)

# Verify ArrayPool cleanup pattern
grep -A5 "ArrayPool.*Rent" path/to/module/src/*.cs | grep -c "finally"
# Expected: Every Rent has corresponding finally block
```

Only after ALL security checks pass, output completion promise.
```

**Why:** Converts subjective "I secured the code" into objective "grep proves N ZeroMemory calls exist." Saved manual security review in PIV refactor session.

### 21. Interface Change Impact Checklist

When a phase involves changing interface signatures, include explicit impact tracking:

```markdown
## Interface Change Checklist (Task N.X)

Before modifying `IFooInterface`:

- [ ] **Audit scope first:**
  - Run `codemapper .` to generate API surface maps (if stale)
  - `grep -rn "IFooInterface" --include="*.cs"` to find all references

- [ ] **All implementations updated:**
  - List: `FooImpl.cs`, `BarImpl.cs`, etc.

- [ ] **All call sites updated:**
  - List: `ConsumerA.cs`, `ConsumerB.cs`, etc.

- [ ] **Test mocks/fakes updated:**
  - `grep -rn "Mock<.*IFoo\|Substitute.For<IFoo\|Fake.*IFoo" tests/ --include="*.cs"`
  - List: `FooTests.cs` mock setup, `FakeApduProcessor.cs`, etc.

- [ ] **Other modules checked:**
  - List modules that depend on this interface
```

**Why:** Test mocks are frequently forgotten during interface refactors. The PIV security refactor required mid-session test file updates when build failed. Explicit checklist prevents this surprise.

### 22. Multi-Phase Prompt Structure Template

For complex refactors (3+ phases), use this proven structure:

```markdown
## Phase N: [Goal] (Priority: P0/P1/P2)

**Goal:** One sentence describing the outcome.

**Files:**
- Modify: `path/to/file.cs`
- Create: `path/to/new/file.cs`

### Tasks

- [ ] N.1: **Audit scope**
  - Run `codemapper .` to refresh API maps (if >1 day old)
  - Run grep to find all affected locations:
    ```bash
    grep -rn "TargetPattern" path/to/search --include="*.cs"
    ```
  - Document findings before proceeding

- [ ] N.2: **Fix [specific item]**
  ```csharp
  // Exact code example to implement
  public void ExampleMethod() { ... }
  ```

- [ ] N.3: **Fix [next item]**
  (Include code example)

- [ ] N.X-2: **Build verification**
  ```bash
  dotnet build.cs build
  ```
  Must exit 0.

- [ ] N.X-1: **Test verification**
  ```bash
  dotnet build.cs test --filter "FullyQualifiedName~Module"
  ```
  All tests must pass.

- [ ] N.X: **Commit**
  ```bash
  git add path/to/files
  git commit -m "type(scope): description

  - Bullet point 1
  - Bullet point 2"
  ```
```

**Why:** This structure (audit→fix→verify→commit) enabled single-iteration completion of a 5-phase security refactor. Each phase has explicit scope discovery, code examples, and verification gates.

## Anti-Patterns to Avoid
- Vague completion criteria ("when finished")
- Missing test requirement ("when code compiles")
- Wrong commands (`dotnet test` instead of `dotnet build.cs test`)
- No failure guidance
- Using `git add .` or `git add -A` blindly
- **Cramming multiple phases into one iteration** (causes context rot)
- **Skipping test audit** before interface/signature changes (causes reactive test fixing)
- **Build-then-test** instead of test-first (catches errors late)
- **Batching documentation** into separate phases (causes doc drift)
- **Claiming completion with unchecked tasks** (violates completion integrity)
- **Marking skipped tasks as complete** (dishonest progress tracking)
- **Sequential file exploration** when parallel calls would work (wastes time)
- **Assuming implementation bug** when workflow tests fail but atomic tests pass (check test setup first)
- **Skipping codemapper** before interface changes (misses structural dependencies)
- **No security verification** for crypto code (leaves memory leaks undetected)

## Handoff (ALWAYS END WITH THIS)

After presenting (and optionally saving) the plan, print the exact one-liner to start the loop:

```bash
bun .claude/skills/agent-ralph-loop/ralph-loop.ts --prompt-file ./docs/plans/ralph-loop/<plan-file>.md --completion-promise "<PROMISE_TOKEN>" --max-iterations 20 --learn --model claude-opus-4.5
```
