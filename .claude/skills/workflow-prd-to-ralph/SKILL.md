---
name: prd-to-ralph
description: Use when converting approved PRDs to Ralph Loop prompts - bridges product-orchestrator output to autonomous execution
---

# PRD to Ralph Loop Converter

## Overview

Converts validated Product Requirements Documents (PRDs) from the `product-orchestrator` workflow into executable Ralph Loop prompts with proper TDD phases, verification requirements, and completion promises.

**Core principle:** Every user story becomes a testable phase; every acceptance criterion becomes a verification step.

## Use when

**Use this skill when:**
- PRD has been approved (`final_spec.md` exists with APPROVED status)
- Ready to begin implementation of a validated feature
- Need to convert requirements into executable code tasks
- Want autonomous implementation with proper checkpoints

**Don't use when:**
- PRD is still in DRAFT or VALIDATING status (complete validation first)
- Feature is exploratory/experimental (use `experiment` skill instead)
- Task is simple enough for manual implementation
- PRD has unresolved CRITICAL findings

## Input Requirements

Before starting, verify:

1. **PRD Location:** `docs/specs/{feature-slug}/final_spec.md`
2. **Status:** Must be `APPROVED`
3. **Audit Reports:** All must show `PASS`
   - `ux_audit.md`
   - `dx_audit.md`
   - `feasibility_report.md`
   - `security_audit.md`

## Process

### 1. Extract from PRD

Read `final_spec.md` and extract:

| PRD Section | Extract |
|-------------|---------|
| §1 Problem Statement | Goal summary (1 sentence) |
| §2 User Stories | Phase definitions |
| §2 Acceptance Criteria | Test assertions |
| §3.1 Happy Path | Implementation steps |
| §3.2 Error States | Error handling tests |
| §3.3 Edge Cases | Edge case tests |
| §5 Technical Constraints | Implementation constraints |

### 2. Map User Stories to Phases

Each user story becomes one Ralph Loop phase:

```markdown
## Phase N: [User Story Title]

**User Story:** As a [user], I want to [action], so that [benefit].

**Files:**
- Create: `Yubico.YubiKit.{Module}/src/{Feature}.cs`
- Test: `Yubico.YubiKit.{Module}/tests/{Feature}Tests.cs`

**Acceptance Criteria → Tests:**
- [ ] Criterion 1 → `Test_Criterion1_ExpectedBehavior()`
- [ ] Criterion 2 → `Test_Criterion2_ExpectedBehavior()`
```

### 3. Convert to TDD Steps

For each phase, follow the TDD cycle:

```markdown
**Step 1: Write failing tests**
Create tests for ALL acceptance criteria before implementation.

**Step 2: Verify RED**
```bash
dotnet build.cs test --filter "FullyQualifiedName~{TestClass}"
```
Expected: All new tests FAIL (compilation or assertion)

**Step 3: Minimal implementation**
Implement just enough to pass the tests. Follow patterns from:
- `feasibility_report.md` (architecture approach)
- `dx_audit.md` (naming conventions)
- `security_audit.md` (sensitive data handling)

**Step 4: Verify GREEN**
```bash
dotnet build.cs test --filter "FullyQualifiedName~{TestClass}"
```
Expected: All tests PASS

**Step 5: Commit**
```bash
git add {specific files}
git commit -m "feat({module}): {description}"
```

→ Output `<promise>PHASE_N_DONE</promise>`
```

### 4. Add Error State Tests

From PRD §3.2 Error States, create dedicated test phase:

```markdown
## Phase N+1: Error Handling

**From PRD Error States:**
| Condition | Expected Behavior | Test |
|-----------|-------------------|------|
| [From PRD] | [From PRD] | `Test_{Condition}_Throws{Exception}()` |

**Step 1: Write error tests**
[Test code for each error condition]

**Step 2: Implement error handling**
[Implementation that throws correct exceptions]
```

### 5. Add Edge Case Tests

From PRD §3.3 Edge Cases:

```markdown
## Phase N+2: Edge Cases

**From PRD Edge Cases:**
| Scenario | Expected Behavior | Test |
|----------|-------------------|------|
| Empty/null input | [From PRD] | `Test_NullInput_...()` |
| Maximum bounds | [From PRD] | `Test_MaxBounds_...()` |
```

### 6. Security Verification Phase

From `security_audit.md`, add security checks:

```markdown
## Phase N+3: Security Verification

**Required Checks (from security_audit.md):**
- [ ] Sensitive data zeroed after use (`CryptographicOperations.ZeroMemory`)
- [ ] No secrets in logs
- [ ] PIN handling follows YubiKey constraints
- [ ] Input validation on all public methods

**Verification:**
```bash
# Check for ZeroMemory usage
grep -r "ZeroMemory" Yubico.YubiKit.{Module}/src/

# Check for logging of sensitive data (should return nothing)
grep -rE "(Log|Console).*([Pp]in|[Kk]ey|[Ss]ecret)" Yubico.YubiKit.{Module}/src/
```
```

### 7. Final Verification Requirements

Always end with comprehensive verification:

```markdown
## Verification Requirements (MUST PASS BEFORE COMPLETION)

1. **Build:** `dotnet build.cs build` (must exit 0)
2. **All Tests:** `dotnet build.cs test` (all tests must pass)
3. **No Regressions:** Existing tests still pass
4. **Coverage:** New code has test coverage
5. **Security:** All security checks from Phase N+3 pass

Only after ALL pass, output `<promise>{FEATURE}_COMPLETE</promise>`.
If any fail, fix and re-verify.
```

## Output Format

Create file at: `docs/plans/ralph-loop/{date}-{feature-slug}.md`

```markdown
# {Feature Name} Implementation Plan (Ralph Loop)

**Goal:** {One sentence from PRD Problem Statement}
**PRD:** `docs/specs/{feature-slug}/final_spec.md`
**Completion Promise:** `{FEATURE_SLUG}_COMPLETE`

---

## Phase 1: {First User Story Title}

**User Story:** {From PRD}

**Files:**
- Create: {paths}
- Test: {paths}

**Step 1: Write failing tests**
{Test code}

**Step 2: Verify RED**
{Command}

**Step 3: Implement**
{Implementation guidance}

**Step 4: Verify GREEN**
{Command}

**Step 5: Commit**
{Commit command}

→ Output `<promise>PHASE_1_DONE</promise>`

---

## Phase 2: {Next User Story}
...

---

## Phase N: Security Verification
...

---

## Verification Requirements (MUST PASS BEFORE COMPLETION)
...

---

## Handoff

```bash
bun .claude/skills/agent-ralph-loop/ralph-loop.ts \
  --prompt-file ./docs/plans/ralph-loop/{date}-{feature-slug}.md \
  --completion-promise "{FEATURE_SLUG}_COMPLETE" \
  --max-iterations 25 \
  --learn \
  --model claude-opus-4.5
```
```

## Example: Converting a PRD

**Input:** `docs/specs/fido2-resident-key-enum/final_spec.md`

**User Story from PRD:**
```markdown
### Story 1: Enumerate Resident Keys
**As a** security administrator,
**I want to** list all resident credentials on a YubiKey,
**So that** I can audit which services have stored keys.

**Acceptance Criteria:**
- [ ] Returns list of credential IDs with relying party info
- [ ] Works with PIN verification
- [ ] Returns empty list when no credentials exist
```

**Converted to Ralph Loop Phase:**
```markdown
## Phase 1: Enumerate Resident Keys

**User Story:** As a security administrator, I want to list all resident credentials...

**Files:**
- Create: `Yubico.YubiKit.Fido2/src/CredentialManagement.cs`
- Test: `Yubico.YubiKit.Fido2/tests/CredentialManagementTests.cs`

**Step 1: Write failing tests**
```csharp
public class CredentialManagementTests
{
    [Fact]
    public void EnumerateCredentials_WithValidPin_ReturnsCredentialList()
    {
        // Arrange
        using var session = new Fido2Session(device);
        session.VerifyPin(testPin);
        
        // Act
        var credentials = session.EnumerateCredentials();
        
        // Assert
        Assert.NotNull(credentials);
        Assert.All(credentials, c => Assert.NotNull(c.RelyingParty));
    }
    
    [Fact]
    public void EnumerateCredentials_NoCredentials_ReturnsEmptyList()
    {
        // ... test for empty case
    }
}
```

**Step 2: Verify RED**
```bash
dotnet build.cs test --filter "FullyQualifiedName~CredentialManagementTests"
```
Expected: Compilation failure (CredentialManagement doesn't exist)

**Step 3: Implement**
Follow patterns from `Fido2Session.cs` for session methods.
Use `EnumerateCredentials` naming per dx_audit.md.

**Step 4: Verify GREEN**
```bash
dotnet build.cs test --filter "FullyQualifiedName~CredentialManagementTests"
```

**Step 5: Commit**
```bash
git add Yubico.YubiKit.Fido2/src/CredentialManagement.cs \
        Yubico.YubiKit.Fido2/tests/CredentialManagementTests.cs
git commit -m "feat(fido2): add credential enumeration"
```

→ Output `<promise>PHASE_1_DONE</promise>`
```

## Common Mistakes

**❌ Skipping RED verification:** Tests must fail first to prove they test something
**✅ Always verify RED:** Run tests before implementation

**❌ One giant phase:** All stories in single phase
**✅ One story per phase:** Keeps iterations short, maximizes fresh context

**❌ Missing security phase:** Forgetting security_audit.md requirements
**✅ Dedicated security phase:** Explicit verification of all security requirements

**❌ Vague completion:** "Output DONE when finished"
**✅ Explicit verification:** Build + test + security checks must all pass

**❌ Missing error tests:** Only testing happy path
**✅ Error states from PRD:** Every §3.2 error becomes a test

## Verification

Conversion is complete when:

- [ ] All user stories from PRD mapped to phases
- [ ] Each phase has TDD steps (RED → GREEN → Commit)
- [ ] Error states (§3.2) have dedicated tests
- [ ] Edge cases (§3.3) have dedicated tests
- [ ] Security phase includes all requirements from security_audit.md
- [ ] Final verification requires build + test + security
- [ ] Handoff command is correct and ready to execute
- [ ] File saved to `docs/plans/ralph-loop/{date}-{feature-slug}.md`

## Related Skills

- `product-orchestrator` - Creates the PRD this skill consumes
- `write-ralph-prompt` - Low-level prompt writing guidance
- `ralph-loop` - Executes the generated prompt
- `write-plan` - Alternative for manual implementation planning
