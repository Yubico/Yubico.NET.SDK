# Ralph Loop Prompt Writing Skill

Expert guidance for creating effective Ralph Loop prompts that enforce proper verification and exit criteria.

## When to Use This Skill

Use when:
- Creating a new Ralph Loop prompt for a coding task
- A Ralph Loop completed too quickly without verification
- You need to ensure tests are run before completion

## Core Principles

### 1. Never Trust "Done" Without Verification

❌ **Bad prompt ending:**
```
Output <promise>DONE</promise> when all tasks verified.
```

✅ **Good prompt ending:**
```
## Verification Requirements (MUST PASS BEFORE COMPLETION)

Before outputting the completion promise, you MUST:

1. **Build the solution:**
   ```bash
   dotnet build.cs build
   ```
   ➡️ Must exit with code 0

2. **Run all unit tests:**
   ```bash
   dotnet build.cs test
   ```
   ➡️ Must show all tests passing

3. **Verify no regressions:**
   - Check that existing tests still pass
   - New code has test coverage

Only after ALL verification steps pass with no errors, output:
<promise>DONE</promise>

If any verification fails:
- Fix the issues
- Re-run verification
- Do NOT output the promise until everything passes
```

### 2. Git Commit Discipline

**CRITICAL:** Only commit files YOU created or modified in this session.

```markdown
## Git Guidelines

⚠️ **NEVER commit files you didn't modify:**
- Before committing, run `git status` to see what's staged
- Only `git add` files YOU created or edited
- Do NOT use `git add .` or `git add -A` blindly
- If you see staged files you didn't touch, leave them alone

**Safe commit pattern:**
```bash
# Check what's already staged (leave these alone!)
git status

# Only add YOUR new/modified files explicitly
git add path/to/your/new/file.cs
git add path/to/your/modified/file.cs

# Commit only what you added
git commit -m "feat(scope): description"
```

**Do NOT:**
- `git add .` (may include unrelated staged files)
- `git add -A` (same problem)
- `git commit -a` (commits all tracked changes, including others' work)
- Commit files that were already staged before you started
```

### 2. Explicit Command References

Always include the exact commands from CLAUDE.md:

```markdown
## Build & Test Commands (from CLAUDE.md)

Use these exact commands - do NOT use raw `dotnet build` or `dotnet test`:

| Action | Command |
|--------|---------|
| Build | `dotnet build.cs build` |
| Test (all) | `dotnet build.cs test` |
| Test (specific project) | `dotnet build.cs test --project Piv` |
| Test (filtered) | `dotnet build.cs test --filter "FullyQualifiedName~MyTest"` |
| Test (project + filter) | `dotnet build.cs test --project Piv --filter "Method~Sign"` |
| Coverage | `dotnet build.cs coverage` |
| Clean build | `dotnet build.cs build --clean` |

### Filter Syntax
- `FullyQualifiedName~MyClass` - Tests containing 'MyClass' in full name
- `Name=MyTestMethod` - Exact test method name
- `ClassName~Integration` - Classes containing 'Integration'
- `Name!=SkipMe` - Exclude tests named 'SkipMe'
- `Category=Unit` - Tests with `[Trait("Category", "Unit")]`
```

### 3. Phased Exit Criteria

For complex tasks, require phase completion:

```markdown
## Exit Criteria by Phase

### Phase 1: Implementation
- [ ] All new files created
- [ ] Code compiles: `dotnet build.cs build` passes

### Phase 2: Testing  
- [ ] Unit tests written for new code
- [ ] All tests pass: `dotnet build.cs test` passes
- [ ] No test regressions

### Phase 3: Integration
- [ ] Integration with existing code verified
- [ ] Manual smoke test (if applicable)

**Completion:** Only output `<promise>DONE</promise>` when ALL phases complete.
```

### 4. Failure Handling

Tell Ralph what to do on failure:

```markdown
## On Failure

If `dotnet build.cs build` fails:
1. Read the error output carefully
2. Fix the compilation errors
3. Re-run build
4. Do NOT proceed to tests until build passes

If `dotnet build.cs test` fails:
1. Identify which tests failed
2. Determine if it's your code or a test bug
3. Fix the issue
4. Re-run ALL tests (not just the failing ones)
5. Do NOT output completion promise until all tests green
```

### 5. Hardware/Integration Test Handling

For tasks involving hardware:

```markdown
## Hardware Test Policy

- Hardware tests MAY fail due to device state
- If a hardware test fails 2-3 times, document and skip
- Do NOT block completion on flaky hardware tests
- Mark with `[Trait("RequiresHardware", "true")]`

Unit tests MUST all pass. Hardware tests are best-effort.
```

## Template: Complete Ralph Loop Prompt

```markdown
# [Task Title]

**Goal:** [Clear one-sentence goal]

**Scope:** [What's in/out of scope]

---

## Context

[Background information, reference files, architecture notes]

---

## Tasks

### Task 1: [Name]
- [ ] Step 1
- [ ] Step 2
- **Commit:** `feat(scope): description`

### Task 2: [Name]
- [ ] Step 1
- [ ] Step 2
- **Commit:** `feat(scope): description`

[... more tasks ...]

---

## Build & Test Commands

**IMPORTANT:** Use build.cs, not raw dotnet commands.

```bash
# Build
dotnet build.cs build

# Test all
dotnet build.cs test

# Test specific project (use for focused testing during development)
dotnet build.cs test --project [ProjectName]

# Test with filter (run specific tests)
dotnet build.cs test --filter "FullyQualifiedName~[TestClass]"

# Clean build (if needed)
dotnet build.cs build --clean
```

---

## Verification Checklist (REQUIRED)

Before completion, ALL must pass:

- [ ] `dotnet build.cs build` exits with code 0
- [ ] `dotnet build.cs test` shows all tests passing
- [ ] New code follows CLAUDE.md guidelines
- [ ] No regressions in existing functionality

---

## Completion

**Only after ALL verification passes**, output:

<promise>DONE</promise>

**If verification fails:** Fix issues and re-verify. Do NOT output the promise until everything passes.
```

## Anti-Patterns to Avoid

### ❌ Vague completion criteria
```
Output <promise>DONE</promise> when finished.
```

### ❌ Missing test requirement
```
Output <promise>DONE</promise> when code compiles.
```

### ❌ Wrong commands
```
Run `dotnet test` to verify.
```

### ❌ No failure guidance
```
Complete all tasks and output the promise.
```

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

## Verification (REQUIRED BEFORE COMPLETION)

```bash
# Must pass:
dotnet build.cs build
dotnet build.cs test
```

Checklist:
- [ ] Build passes with no errors
- [ ] All unit tests pass (new and existing)
- [ ] Code follows CLAUDE.md patterns

**Only after verification passes:**
<promise>DONE</promise>
```
