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
**CRITICAL:** Use this exact template. The execution protocol is injected automatically by `ralph-loop.ts`.

```markdown
---
type: progress
feature: {feature-slug}
prd: docs/specs/{feature-slug}/final_spec.md
started: {YYYY-MM-DD}
status: in-progress
---

# {Feature Name} Progress

## Phase 1: {Core Feature Name} (P0)

**Goal:** {User Story from PRD}
**Files:**
- Src: `Yubico.YubiKit.{Module}/src/{Feature}.cs`
- Test: `Yubico.YubiKit.{Module}/tests/{Feature}Tests.cs`

### Tasks
- [ ] 1.1: Create project/files and basic class structure
- [ ] 1.2: Implement {First Function} (Happy Path)
- [ ] 1.3: Implement {Second Function} (Happy Path)

### Error Handling (PRD §3.2)
- [ ] 1.4: Handle {Error Condition 1} -> Throw {ExceptionType}
- [ ] 1.5: Handle {Error Condition 2} -> Throw {ExceptionType}

### Edge Cases (PRD §3.3)
- [ ] 1.6: Handle {Edge Case 1} (e.g., empty input, max bounds)

### Notes
<!-- Engine appends notes here after each task -->

---

## Phase 2: {Next Feature / Extension} (P1)

**Goal:** {User Story}
**Files:**
- Src: `{path}`
- Test: `{path}`

### Tasks
- [ ] 2.1: ...
- [ ] 2.2: ...

### Notes

---

## Phase N: Security Verification (P0)

**Goal:** Verify all security requirements from `security_audit.md`

### Tasks
- [ ] S.1: Audit: Verify all sensitive buffers are zeroed
- [ ] S.2: Audit: Verify no secrets in logs
- [ ] S.3: Audit: Verify PIN handling compliance

### Notes
```

**Note:** The execution protocol (TDD loop, security rules, git discipline, verification requirements) is injected automatically by `ralph-loop.ts` when it detects the `type: progress` frontmatter. Do NOT add workflow instructions to the progress file.

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

### 4. Launch Ralph Loop

The progress file is self-contained. No custom prompt needed - the engine detects the format and injects the execution protocol automatically.

```bash
bun .claude/skills/agent-ralph-loop/ralph-loop.ts \
  --prompt-file docs/ralph-loop/{feature}-progress.md \
  --completion-promise "{FEATURE}_COMPLETE" \
  --max-iterations 50 \
  --learn \
  --model claude-opus-4.5
```

The engine will:
1. Detect `type: progress` frontmatter
2. Inject TDD loop, security protocol, git discipline
3. Parse current phase/task and provide context
4. Re-read the progress file each iteration to track state

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
## Phase 1: Enumerate Resident Keys (P0)

**Goal:** As a security administrator, I want to list all resident credentials on a YubiKey.
**Files:**
- Src: `Yubico.YubiKit.Fido2/src/CredentialManagement.cs`
- Test: `Yubico.YubiKit.Fido2/tests/CredentialManagementTests.cs`

### Tasks
- [ ] 1.1: Create `CredentialManagement.cs` and `CredentialManagementTests.cs`
- [ ] 1.2: Implement `EnumerateCredentials` returns list with RP info
- [ ] 1.3: Implement PIN verification flow

### Error Handling (PRD §3.2)
- [ ] 1.4: Handle PIN blocked -> Throw `InvalidPinException(0)`
- [ ] 1.5: Handle wrong PIN -> Throw `InvalidPinException(retriesRemaining)`

### Edge Cases (PRD §3.3)
- [ ] 1.6: Handle no credentials -> Return empty list (not null)

### Notes
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

## Common Mistakes (When Creating Progress Files)

**❌ Adding workflow instructions:** The execution protocol belongs in the engine.
**✅ Keep it declarative:** Just phases, tasks, files, priorities.

**❌ One giant phase:** All stories in single phase.
**✅ One story per phase:** Keeps iterations short, maximizes fresh context.

**❌ Missing security phase:** Forgetting `security_audit.md` requirements.
**✅ Dedicated security phase:** Explicit verification of all security requirements.

**❌ Missing error tests:** Only testing happy path.
**✅ Error states from PRD:** Every §3.2 error becomes a task.

**❌ Missing YAML frontmatter:** Engine won't detect progress file format.
**✅ Include frontmatter:** `type: progress` is required for auto-detection.

## Verification Criteria

The PRD-to-Ralph conversion is complete when:
1. **Progress File Exists:** `docs/ralph-loop/{feature}-progress.md`
2. **Valid Frontmatter:** Has `type: progress` in YAML frontmatter
3. **Comprehensive Coverage:**
    * Happy Path tasks
    * Error Handling tasks (from PRD §3.2)
    * Edge Case tasks (from PRD §3.3)
    * Security tasks
4. **Prioritization:** Phases are explicitly marked (P0/P1/P2)
5. **File Paths:** Each phase has explicit `Src` and `Test` paths

**Note:** Final verification (build, test, security) is handled by the ralph-loop execution protocol, not this conversion skill.

## Related Skills

- `product-orchestrator` - Creates the PRD this skill consumes
- `ralph-loop` - Executes the progress file (auto-injects protocol)
- `write-ralph-prompt` - Guidance for ad-hoc mode (no progress file)
- `write-plan` - Creates implementation plans (alternative input source)
- `plan-to-ralph` - Converts implementation plans to progress files (future)
