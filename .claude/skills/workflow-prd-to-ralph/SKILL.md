---
name: prd-to-ralph
description: Use when converting approved PRDs to Ralph Loop prompts - bridges product-orchestrator output to autonomous execution
---

# PRD to Ralph Loop Converter

## Overview

Converts validated Product Requirements Documents (PRDs) from the `product-orchestrator` workflow into a dynamic **Living Specification** using the "Progress File" pattern.

**Core Principle:**
1. **Plan:** Create a dynamic `progress.md` file that acts as both a **Status Board** and a **Detailed Specification**.
2. **Execute:** Dispatch an autonomous agent (`ralph-loop`) that follows the priorities (P0/P1/P2) defined in this file.

## Use when

**Use this skill when:**
- PRD has been approved (`final_spec.md` exists with APPROVED status)
- Ready to begin implementation of a validated feature
- Want robust autonomous execution that handles state/resumption automatically

**Don't use when:**
- PRD is still in DRAFT or VALIDATING status
- Feature is a simple one-off script (use `experiment` instead)

## Input Requirements

Before starting, verify:
1. **PRD Location:** `docs/specs/{feature-slug}/final_spec.md`
2. **Status:** Must be `APPROVED`
3. **Audit Reports:** All must show `PASS`
   - `ux_audit.md`, `dx_audit.md`, `feasibility_report.md`, `security_audit.md`

## Process

### 1. Extract from PRD

Read `final_spec.md` and extract the following:

| PRD Section | Extract | Maps To |
|-------------|---------|---------|
| §1 Problem Statement | Goal summary (1 sentence) | Progress File header |
| §2 User Stories | Phase definitions | One Phase per story |
| §2 Acceptance Criteria | Test assertions | `[ ] Task` items |
| §3.1 Happy Path | Implementation steps | Core Implementation tasks |
| §3.2 Error States | Error handling tests | Error Handling sub-section |
| §3.3 Edge Cases | Edge case tests | Edge Cases sub-section |
| §5 Technical Constraints | Implementation constraints | Notes in Phase |

### 2. Create Progress File

Create a file at `docs/ralph-loop/{feature}-progress.md`.
**CRITICAL:** Use this exact rich template. It provides the context the autonomous agent needs.

```markdown
# {Feature Name} Implementation Progress

**Started:** {YYYY-MM-DD}
**PRD:** `docs/specs/{feature-slug}/final_spec.md`
**Status:** In Progress

## Workflow Instructions (For Autonomous Agent)

**Role:** You are an autonomous .NET engineer.
**Source of Truth:** THIS FILE. It contains your tasks, priorities, and rules.

### The Loop
1. **Read this file** to understand the current state.
2. **Select Phase:** Find the highest priority incomplete Phase (P0 > P1 > P2).
   - *Rule:* Finish the current phase completely before moving to the next.
   - *Rule:* Within a phase, complete "Core Implementation" before "Edge Cases".
3. **Execute Task:** Pick the next unchecked item `[ ]`.
   - **Step 1: Write Failing Test (RED)**
     - Create/Update the test file listed in the Phase.
     - Write a test that asserts the specific criteria of the task.
     - Run: `dotnet build.cs test --filter "FullyQualifiedName~{TestClass}"` -> Expect FAILURE.
   - **Step 2: Implement (GREEN)**
     - Write minimal code in the implementation file.
     - Follow patterns from `dx_audit.md` and `security_audit.md`.
     - Run: `dotnet build.cs test --filter "FullyQualifiedName~{TestClass}"` -> Expect SUCCESS.
   - **Step 3: Refactor & Secure**
     - Check: Is sensitive data zeroed? (See Security Protocol below).
     - Check: Are public APIs documented?
   - **Step 4: Commit**
     - `git add {specific files only}`
     - `git commit -m "feat({scope}): {task description}"`
4. **Update Status:** Change `[ ]` to `[x]` in this file and add notes.
5. **Loop:** Repeat.

### Security Protocol (MUST FOLLOW)
- **ZeroMemory:** Always zero sensitive data (PINs, Keys) using `CryptographicOperations.ZeroMemory`.
- **No Logs:** Never log sensitive values.
- **Validation:** Validate all input lengths and ranges.

### Guidelines & Anti-Patterns
- **❌ Skipping RED verification:** Tests must fail first to prove they test something.
- **✅ Always verify RED:** Run tests before implementation.
- **❌ One giant phase:** Do not tackle all stories in a single phase.
- **✅ Vertical Slices:** Implement one story/feature completely (including errors) before moving on.
- **❌ Missing security:** Forgetting requirements from `security_audit.md`.
- **✅ Explicit Verification:** Build + Test + Security must pass before marking `[x]`.
- **❌ Using `dotnet test`:** This will fail on mixed xUnit v2/v3 projects.
- **✅ Use `dotnet build.cs test`:** Always use the build script.

---

## Phases

### Phase 1: {Core Feature Name} (P0)

**Goal:** {User Story from PRD}
**Files:**
- Src: `Yubico.YubiKit.{Module}/src/{Feature}.cs`
- Tests: `Yubico.YubiKit.{Module}/tests/{Feature}Tests.cs`

**Tasks:**
#### Core Implementation
- [ ] 1.1: Create project/files and basic class structure
- [ ] 1.2: Implement {First Function} (Happy Path)
- [ ] 1.3: Implement {Second Function} (Happy Path)

#### Error Handling (PRD §3.2)
- [ ] 1.4: Handle {Error Condition 1} -> Throw {ExceptionType}
- [ ] 1.5: Handle {Error Condition 2} -> Throw {ExceptionType}

#### Edge Cases (PRD §3.3)
- [ ] 1.6: Handle {Edge Case 1} (e.g., empty input, max bounds)

---

### Phase 2: {Next Feature / Extension} (P0)

**Goal:** {User Story}
**Files:** {Paths}

**Tasks:**
- [ ] 2.1: ...
- [ ] 2.2: ...

---

### Phase N: Security Verification (P0)

**Goal:** Verify all security requirements from `security_audit.md`

**Tasks:**
- [ ] S.1: Audit: Verify all sensitive buffers are zeroed
  ```bash
  grep -r "ZeroMemory" Yubico.YubiKit.{Module}/src/
  ```
- [ ] S.2: Audit: Verify no secrets in logs
  ```bash
  grep -rE "(Log|Console).*([Pp]in|[Kk]ey|[Ss]ecret)" Yubico.YubiKit.{Module}/src/
  # Should return nothing
  ```
- [ ] S.3: Audit: Verify PIN handling compliance

---

## Verification Requirements (MUST PASS BEFORE COMPLETION)

1. **Build:** `dotnet build.cs build` (must exit 0)
2. **All Tests:** `dotnet build.cs test` (all tests must pass)
3. **No Regressions:** Existing tests still pass
4. **Coverage:** New code has test coverage
5. **Security:** All security checks from Phase N pass

Only after ALL pass, output `<promise>{FEATURE}_COMPLETE</promise>`.
If any fail, fix and re-verify.

---

## Session Notes
* {Date}: Project initialized.
```

### 3. Map PRD to Phases (Detailed Instructions)

Read `final_spec.md` and populate the template above using these rules:

1.  **Extract User Stories:** Each major User Story becomes a **Phase**.
2.  **Assign Priority:**
    *   **P0:** Core functionality, Security, Reliability.
    *   **P1:** Extensions, Convenience APIs, Performance optimization.
    *   **P2:** Nice-to-haves.
3.  **Embed Requirements:**
    *   Copy **Acceptance Criteria** directly into the `[ ] Tasks` list.
    *   Copy **Error States (§3.2)** into the "Error Handling" sub-section of the relevant phase.
    *   Copy **Edge Cases (§3.3)** into the "Edge Cases" sub-section.
4.  **Define Files:** Explicitly list the `Src` and `Test` file paths for every phase so the agent doesn't guess.

### 4. Generate Ralph Prompt

Construct the command. This prompt delegates the logic to the `progress.md` file we just created.

```bash
bun .claude/skills/agent-ralph-loop/ralph-loop.ts \
  --prompt "You are an autonomous .NET engineer implementing {Feature Name}.
  
  **Your Source of Truth:** \`docs/ralph-loop/{feature}-progress.md\`
  
  **Your Mission:**
  Follow the 'Workflow Instructions' defined in the progress file exactly.
  
  **Key Behaviors:**
  1. **Read-First:** Always read the progress file first.
  2. **Priority-Driven:** Execute P0 phases before P1.
  3. **TDD-Strict:** Write the test *before* the implementation.
  4. **State-Aware:** Update the progress file after *every* commit.
  
  Output <promise>{FEATURE}_COMPLETE</promise> only when ALL tasks in the progress file are marked [x]." \
  --completion-promise "{FEATURE}_COMPLETE" \
  --max-iterations 50 \
  --learn \
  --model claude-opus-4.5
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

**Converted to Progress File Phase:**
```markdown
### Phase 1: Enumerate Resident Keys (P0)

**Goal:** As a security administrator, I want to list all resident credentials on a YubiKey.
**Files:**
- Src: `Yubico.YubiKit.Fido2/src/CredentialManagement.cs`
- Tests: `Yubico.YubiKit.Fido2/tests/CredentialManagementTests.cs`

**Tasks:**
#### Core Implementation
- [ ] 1.1: Create `CredentialManagement.cs` and `CredentialManagementTests.cs`
- [ ] 1.2: Implement `EnumerateCredentials` returns list with RP info
- [ ] 1.3: Implement PIN verification flow

#### Error Handling (PRD §3.2)
- [ ] 1.4: Handle PIN blocked -> Throw `InvalidPinException(0)`
- [ ] 1.5: Handle wrong PIN -> Throw `InvalidPinException(retriesRemaining)`

#### Edge Cases (PRD §3.3)
- [ ] 1.6: Handle no credentials -> Return empty list (not null)
```

**Example Test Code (for Task 1.2):**
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
        // Arrange
        using var session = new Fido2Session(freshDevice);
        session.VerifyPin(testPin);
        
        // Act
        var credentials = session.EnumerateCredentials();
        
        // Assert
        Assert.NotNull(credentials);
        Assert.Empty(credentials);
    }
}
```

## Common Mistakes

**❌ Skipping RED verification:** Tests must fail first to prove they test something.
**✅ Always verify RED:** Run tests before implementation.

**❌ One giant phase:** All stories in single phase.
**✅ One story per phase:** Keeps iterations short, maximizes fresh context.

**❌ Missing security phase:** Forgetting `security_audit.md` requirements.
**✅ Dedicated security phase:** Explicit verification of all security requirements.

**❌ Vague completion:** "Output DONE when finished."
**✅ Explicit verification:** Build + test + security checks must all pass.

**❌ Missing error tests:** Only testing happy path.
**✅ Error states from PRD:** Every §3.2 error becomes a test.

**❌ Using `dotnet test` directly:** Will fail on mixed xUnit v2/v3.
**✅ Use `dotnet build.cs test`:** Always use the build script.

## Verification Criteria

The task is complete when:
1.  **Progress File Exists:** `docs/ralph-loop/{feature}-progress.md`
2.  **Rich Context:** The file includes detailed **Files** paths and **Goal** descriptions for each phase.
3.  **Comprehensive Coverage:**
    *   Happy Path tasks
    *   Error Handling tasks (from PRD §3.2)
    *   Edge Case tasks (from PRD §3.3)
    *   Security tasks
4.  **Prioritization:** Phases are explicitly marked (P0/P1/P2).

## Final Verification Checklist (for the Agent)

When the agent claims completion, ensure:
1. **Build:** `dotnet build.cs build` (must exit 0)
2. **All Tests:** `dotnet build.cs test` (all tests must pass)
3. **No Regressions:** Existing tests still pass
4. **Coverage:** New code has test coverage
5. **Security:** All security checks from Phase N pass

Only after ALL pass, output `<promise>{FEATURE}_COMPLETE</promise>`.
If any fail, fix and re-verify.

## Related Skills

- `product-orchestrator` - Creates the PRD this skill consumes
- `write-ralph-prompt` - Low-level prompt writing guidance
- `ralph-loop` - Executes the generated prompt
- `write-plan` - Alternative for manual implementation planning
