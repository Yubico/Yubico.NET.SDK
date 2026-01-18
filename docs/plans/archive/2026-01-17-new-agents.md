# Plan: Autonomous Product Lifecycle Agents & Skills

**Goal:** Implement a "Product Management Agent Swarm" to automate the definition, validation, and auditing of requirements before code is written.

**Source Material:**
- `docs/research/Architecting_the_Autonomous_Product_Lifecycle_The Agent Swarm.md` (Primary - includes UX/DX validators)

**Context:** Yubico.NET.SDK (Public SDK requires specific DX focus)

---

## 1. Background & Reasoning

### The Research Insight
The research proposes decomposing the monolithic "Product Manager" role into a swarm of specialized agents.
*   **Skills (Instructions):** Static templates and rules that teach Claude *how* to do a task (e.g., "WCAG 2.1 Accessibility Guidelines").
*   **Agents (Workers):** Specialized instances that execute tasks using those skills (e.g., "A UX Validator who reads the checklist and checks the work").
*   **Artifact Handshake:** Agents communicate via files in `docs/specs/<feature>/` directory, providing version-controlled audit trails.
*   **Context Hygiene:** Sub-agents can read 100 files without polluting the orchestrator's memory—they return only concise summaries.

### The SDK Context
The research model now includes both **UX Validators** and **DX Validators** as first-class roles. For an SDK:
*   **UX Validator:** Ensures error states, edge cases, and unhappy paths are defined (even without a visual UI, SDK users experience "UX" through error messages and API behavior).
*   **DX Validator:** Ensures the public API surface follows .NET Design Guidelines, `CLAUDE.md` patterns, and maintains consistency with existing schemas.

**Key Insight from Research:** These validators run in **parallel** after the initial draft, providing independent critiques from different angles before refinement.

---

## 2. Architecture: The "Product Swarm"

The workflow includes a **parallel validation phase** and a **self-correction loop**, managed by an orchestrator skill.

**Flow:**
```
Define → Validate (UX + DX parallel) → Refine (if needed) → Audit (Tech + Sec) → Finalize
         ↑__________________________|
              (Self-Correction Loop)
```

### The Swarm Topology
1. **`product-orchestrator` (Skill):** The user's main interface and state manager.
2. **`spec-writer` (Agent):** The creative drafter.
3. **`ux-validator` (Agent):** The design/usability critic.
4. **`dx-validator` (Agent):** The API/architecture critic.
5. **`technical-validator` (Agent):** The feasibility checker.
6. **`security-auditor` (Agent):** The safety gatekeeper.

### The Handshake Mechanism
*   **Directory:** `docs/specs/<feature-slug>/` (Version controlled, provides audit trail)
*   **Artifacts:**
    *   `draft.md` (Created by Spec Writer)
    *   `ux_audit.md` (Created by UX Validator)
    *   `dx_audit.md` (Created by DX Validator)
    *   `feasibility_report.md` (Created by Technical Validator)
    *   `security_audit.md` (Created by Security Auditor)
    *   `final_spec.md` (Consolidated after all validations pass)

**Reasoning for VCS:** PRDs and their audit trails are valuable documentation. Version controlling them:
- Provides historical context for "why was this designed this way?"
- Enables PR reviews of specs before implementation
- Creates accountability through git blame
- Allows rollback if requirements change

### The Self-Correction Loop (Research-Backed)
The orchestrator reads audit files after each validation phase. If any audit contains `CRITICAL FAIL`:
1. Orchestrator automatically respawns `spec-writer` with instruction: *"Fix the critical issues identified in [audit file]."*
2. Loop repeats until all audits pass or max iterations reached.
3. Human is notified only after autonomous improvement attempts.

**Reasoning:** This reduces round-trips with the user and catches obvious issues automatically.

---

## 3. Proposed Components

### A. Skills (The Playbooks)

Skills define the *rules* that agents follow. Each validator agent loads its corresponding skill.

#### 1. `spec-writing-standards` (New)
*   **Role:** The PRD Rulebook.
*   **Content:**
    *   PRD Templates (Problem, Evidence, User Stories).
    *   **INVEST** Model rules for User Stories.
    *   No implementation details allowed in the "Definition" phase.
*   **Used By:** `spec-writer` agent.
*   **Reasoning:** Ensures all agents operate on the same definitions. Decouples "How to write a spec" from "Who is writing it."

#### 2. `ux-heuristics` (New - Research-Backed)
*   **Role:** UX/Usability Rulebook.
*   **Content:**
    *   Nielsen's Usability Heuristics.
    *   WCAG accessibility rules.
    *   Error state checklist (unhappy paths).
    *   Empty state / zero-data requirements.
    *   Feedback confirmation requirements.
*   **Used By:** `ux-validator` agent.
*   **Reasoning:** Even SDKs have "UX"—error messages, exception patterns, and API behavior are the user experience. Research shows missing error states are a top PRD defect.

#### 3. `api-design-standards` (New - Research-Backed)
*   **Role:** DX/API Rulebook.
*   **Content:**
    *   .NET Framework Design Guidelines.
    *   C# naming conventions (PascalCase for public, camelCase for parameters).
    *   `Span<T>` / `Memory<T>` usage patterns from `CLAUDE.md`.
    *   Async/Await correctness rules.
    *   Error response patterns (human-readable message + machine-readable code).
    *   Schema consistency rules (don't invent new patterns).
*   **Used By:** `dx-validator` agent.
*   **Reasoning:** Prevents "API Sprawl" and ensures features are maintainable. Catches schema inconsistencies at PRD stage before code exists.

#### 4. `security-guidelines` (New)
*   **Role:** Security Rulebook.
*   **Content:**
    *   OWASP Top 10 checklist.
    *   Sensitive data handling (ZeroMemory requirements).
    *   YubiKey-specific constraints (Attestation, PIN handling, touch policies).
    *   Cryptographic operation patterns.
*   **Used By:** `security-auditor` agent.
*   **Reasoning:** Enforces security compliance *before* code is written.

#### 5. `product-orchestrator` (New)
*   **Role:** The Orchestrator (Manager).
*   **Capabilities:**
    *   Creates `docs/specs/<feature-slug>/` directory for the feature.
    *   Dispatches agents in the correct order.
    *   Runs UX and DX validators **in parallel** (research-backed optimization).
    *   Implements self-correction loop for CRITICAL failures.
    *   Halts and escalates to human if max iterations reached.
    *   Commits intermediate artifacts for audit trail.
*   **Reasoning:** Replaces ad-hoc prompts with a reproducible workflow. The orchestrator doesn't do the work—it delegates.

### B. Agents (The Workers)

Agents are spawned in isolated contexts. They read their skill, do the work, and return concise results.

#### 1. `spec-writer` (Research-Backed)
*   **Role:** The Product Manager.
*   **Loads Skill:** `spec-writing-standards`.
*   **Input:** User Request.
*   **Output:** `docs/specs/<feature>/draft.md`.
*   **Focus:** Value, User Pain Points, Business Logic.
*   **Reasoning:** Keeps the creative "brainstorming" context separate from technical validation. High temperature for creativity.

#### 2. `ux-validator` (New - Research-Backed)
*   **Role:** The Design Critic / UX Researcher.
*   **Loads Skill:** `ux-heuristics`.
*   **Input:** `docs/specs/<feature>/draft.md`.
*   **Output:** `docs/specs/<feature>/ux_audit.md` (PASS/FAIL with findings).
*   **Focus:**
    *   Error Prevention: Are error states defined for every interaction?
    *   Unhappy Paths: What happens when the API fails? When auth is denied?
    *   Empty States: Is the "zero data" state defined?
    *   Feedback: Does the user receive confirmation after actions?
*   **Reasoning:** Research shows "Missing error state for failed login" type issues are caught here. Prevents costly redesigns later.

#### 3. `dx-validator` (Research-Backed, renamed from `api-designer`)
*   **Role:** The Staff Engineer / API Architect.
*   **Loads Skill:** `api-design-standards`.
*   **Input:** `docs/specs/<feature>/draft.md`.
*   **Output:** `docs/specs/<feature>/dx_audit.md` (PASS/FAIL with findings).
*   **Focus:**
    *   Naming: Do proposed APIs follow .NET conventions?
    *   Consistency: Does the data model align with existing schemas?
    *   Errors: Are error responses useful for debugging?
    *   Memory Safety: Are `Span<T>` / `Memory<T>` patterns applied correctly?
*   **Reasoning:** Acts as a senior engineer doing design review. Catches "Schema Violation: Use existing X table" type issues before implementation.

#### 4. `technical-validator` (Research-Backed)
*   **Role:** The Architect.
*   **Input:** `docs/specs/<feature>/draft.md` + `docs/specs/<feature>/dx_audit.md` + `Yubico.YubiKit.*/` (Read Access).
*   **Output:** `docs/specs/<feature>/feasibility_report.md` (PASS/FAIL).
*   **Focus:**
    *   Internal implementation feasibility.
    *   P/Invoke compatibility.
    *   Dependency conflicts.
    *   Breaking changes check.
*   **Reasoning:** Prevents "hallucinated feasibility" where an agent specifies a feature that contradicts the existing architecture. Has read access to actual codebase.

#### 5. `security-auditor` (Research-Backed)
*   **Role:** The Gatekeeper.
*   **Loads Skill:** `security-guidelines`.
*   **Input:** `docs/specs/<feature>/draft.md` + `docs/specs/<feature>/dx_audit.md`.
*   **Output:** `docs/specs/<feature>/security_audit.md` (PASS/FAIL).
*   **Focus:**
    *   OWASP Top 10.
    *   Sensitive data handling (ZeroMemory).
    *   YubiKey specific constraints (Attestation, PIN handling).
*   **Reasoning:** Enforces security compliance *before* code is written. Low temperature for rigorous logic.

---

## 4. Implementation Plan

Order of operations respects dependencies (Skills before Agents, Orchestrator last).

### Phase 1: Foundation Skills (The Rulebooks)
1.  **Create Skill:** `spec-writing-standards` — PRD templates and INVEST model rules.
2.  **Create Skill:** `ux-heuristics` — Nielsen's heuristics, WCAG, error state checklists.
3.  **Create Skill:** `api-design-standards` — .NET conventions, schema consistency rules.
4.  **Create Skill:** `security-guidelines` — OWASP, ZeroMemory, YubiKey constraints.

### Phase 2: Validator Agents (The Workers)
5.  **Create Agent:** `spec-writer` — Depends on `spec-writing-standards` skill.
6.  **Create Agent:** `ux-validator` — Depends on `ux-heuristics` skill.
7.  **Create Agent:** `dx-validator` — Depends on `api-design-standards` skill.
8.  **Create Agent:** `technical-validator` — Depends on codebase read access.
9.  **Create Agent:** `security-auditor` — Depends on `security-guidelines` skill.

### Phase 3: Orchestration
10. **Create Skill:** `product-orchestrator` — Depends on all agents above.
11. **Create directory:** `docs/specs/` — Base directory for all PRD artifacts.

### Mirrored Agents Requirement
Per `write-agent` skill, all agents must be created in both:
- `.github/agents/` (for Copilot CLI)
- `.claude/agents/` (for Claude Code)

## 5. Integration with Existing Workflow

*   **Current:** `write-plan` creates implementation steps from scratch.
*   **Future:** `write-plan` will be updated to accept `docs/specs/<feature>/final_spec.md` as input, converting the validated API design into the existing "Bite-Sized Task" format for TDD execution.
*   **Integration Point:** After `product-orchestrator` completes, invoke `write-plan` skill with `docs/specs/<feature>/final_spec.md`.

---

## 6. Example Workflow (Research-Backed)

**Scenario:** User requests "Add FIDO2 resident key enumeration"

1. **User:** "Orchestrate a PRD for enumerating resident keys on a YubiKey."
2. **Orchestrator:** "Phase 1: Spawning Spec Writer..."
3. **Spec Writer:** Creates `docs/specs/fido2-resident-key-enum/draft.md` with problem statement, user stories, constraints.
4. **Orchestrator:** "Phase 2: Spawning Validators (parallel)..."
5. **UX Validator:**
   - Checks: "PRD says 'user retrieves keys'. Does not define what happens if no keys exist."
   - Writes `ux_audit.md`: "WARN: Missing 'empty state' flow."
6. **DX Validator:**
   - Checks: "PRD proposes `GetResidentKeys()` but existing pattern is `EnumerateCredentials()`."
   - Writes `dx_audit.md`: "FAIL: Naming inconsistent with existing `Fido2Session` patterns."
7. **Orchestrator:** "DX audit found CRITICAL. Respawning Spec Writer with fixes..."
8. **Spec Writer:** Updates draft to use `EnumerateCredentials()` pattern and add empty state.
9. **Orchestrator:** "Phase 3: Technical + Security validation..."
10. **Technical Validator:** Confirms feasibility against `Yubico.YubiKit.Fido2/`.
11. **Security Auditor:** Confirms PIN handling aligns with YubiKey constraints.
12. **Orchestrator:** "All validations passed. Final spec ready for review."

---

## 7. Changelog

| Date | Change | Reasoning |
|------|--------|-----------|
| 2026-01-17 | Initial plan by Gemini | Based on initial research |
| 2026-01-17 | Added `ux-validator` agent | Research confirms UX validation as first-class role |
| 2026-01-17 | Renamed `api-designer` → `dx-validator` | Aligns with research terminology |
| 2026-01-17 | Added skills as agent dependencies | Research pattern: Skills are rulebooks, Agents execute |
| 2026-01-17 | Added self-correction loop | Research Section 6.2: autonomous improvement before human notification |
| 2026-01-17 | Added parallel validation phase | Research Section 2.2: UX + DX validators run simultaneously |
| 2026-01-17 | Fixed `src/` → `Yubico.YubiKit.*/` | Correct repo structure |
| 2026-01-17 | Changed `.product/` → `docs/specs/<feature>/` | User requirement: PRDs should be version controlled for audit trail and PR reviews |
| 2026-01-17 | Removed `.gitignore` step | No longer needed since artifacts are in VCS |
| 2026-01-17 | Added mirrored agents requirement | Per `write-agent` skill |
| 2026-01-17 | Added Appendices A-H | Complete templates, checklists, and prompts for implementation |

---

## Appendix A: PRD Template (`draft.md`)

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

---

## Appendix B: Audit Report Template (`*_audit.md`)

```markdown
# [UX/DX/Security/Feasibility] Audit Report

**PRD:** [Feature Name]
**Auditor:** [agent name]
**Date:** [ISO 8601 timestamp]
**Verdict:** PASS | FAIL

---

## Summary

| Severity | Count |
|----------|-------|
| CRITICAL | [n] |
| WARN | [n] |
| INFO | [n] |

**Overall:** [One sentence summary of findings]

---

## Findings

### CRITICAL-001: [Short Title]
**Section:** [PRD section reference, e.g., "3.2 Error States"]
**Issue:** [What is wrong or missing]
**Impact:** [Why this matters]
**Recommendation:** [Specific fix]

### WARN-001: [Short Title]
**Section:** [PRD section reference]
**Issue:** [What could be improved]
**Recommendation:** [Suggested improvement]

### INFO-001: [Short Title]
**Section:** [PRD section reference]
**Note:** [Observation or suggestion, non-blocking]

---

## Checklist Results

| Check | Result | Notes |
|-------|--------|-------|
| [Checklist item 1] | ✅/❌ | [Details] |
| [Checklist item 2] | ✅/❌ | [Details] |

---

## Verdict Justification

[Paragraph explaining why PASS or FAIL was chosen. FAIL requires at least one CRITICAL finding.]
```

---

## Appendix C: Severity Definitions

| Severity | Definition | Effect on Workflow |
|----------|------------|-------------------|
| **CRITICAL** | Blocks implementation. Missing required information, security vulnerability, or fundamental design flaw. | Triggers self-correction loop. PRD cannot proceed. |
| **WARN** | Should be addressed but doesn't block. Suboptimal design, missing edge case, or deviation from convention. | Logged for spec-writer to address. Does not trigger loop. |
| **INFO** | Observation or suggestion. Nice-to-have improvement. | Logged for reference. No action required. |

### CRITICAL Triggers (Auto-Fail)
- Missing error states for any user action
- Security-sensitive operation without explicit handling
- Breaking change to existing public API
- Missing acceptance criteria on any user story
- Naming that conflicts with existing API patterns

---

## Appendix D: INVEST Model Checklist

Each user story MUST pass all six criteria:

| Criterion | Question | Fail Condition |
|-----------|----------|----------------|
| **I**ndependent | Can this story be implemented without depending on another story in this PRD? | Story references "after Story X is done" |
| **N**egotiable | Is the story focused on WHAT, not HOW? | Story contains implementation details (class names, algorithms) |
| **V**aluable | Does the story deliver value to the end user (not just the developer)? | Story is purely technical ("refactor X") |
| **E**stimable | Is there enough detail to estimate effort? | Vague terms like "handle errors appropriately" |
| **S**mall | Can this be implemented in ≤3 days? | Story covers multiple distinct behaviors |
| **T**estable | Can you write a test that proves this works? | Subjective criteria ("user feels confident") |

---

## Appendix E: SDK UX Heuristics (Nielsen's Adapted)

Adapted from Nielsen's 10 Usability Heuristics for SDK/API context:

| # | Heuristic | SDK Application | Audit Question |
|---|-----------|-----------------|----------------|
| 1 | **Visibility of system status** | Methods should indicate progress for long operations | Does the PRD define how long-running operations report progress? |
| 2 | **Match between system and real world** | Use domain terminology (FIDO2, PIV, not internal jargon) | Are all terms defined or referenced to YubiKey documentation? |
| 3 | **User control and freedom** | Operations should be cancellable where possible | Can the user abort/cancel operations? Is this defined? |
| 4 | **Consistency and standards** | Follow existing SDK patterns and .NET conventions | Does the proposed API match existing `*Session` patterns? |
| 5 | **Error prevention** | Validate inputs; make invalid states unrepresentable | Are preconditions checked? Can the user avoid errors? |
| 6 | **Recognition over recall** | Intellisense-friendly APIs; enums over magic strings | Are options discoverable via IDE? No stringly-typed APIs? |
| 7 | **Flexibility and efficiency** | Provide both simple and advanced overloads | Is there a "pit of success" default AND power-user options? |
| 8 | **Aesthetic and minimalist design** | Don't expose unnecessary complexity in public API | Is the API surface minimal? No leaking of internal concepts? |
| 9 | **Help users recognize and recover from errors** | Exceptions should be specific and actionable | Do error messages explain WHAT failed and HOW to fix it? |
| 10 | **Help and documentation** | XML docs, examples, and migration guides | Does the PRD require documentation for the feature? |

---

## Appendix F: SDK-Relevant WCAG Considerations

While WCAG is for visual UI, these aspects apply to SDKs:

| WCAG Principle | SDK Application | Audit Check |
|----------------|-----------------|-------------|
| **Perceivable** | Error messages must be clear text, not just codes | Are all error codes accompanied by human-readable messages? |
| **Operable** | APIs must work in constrained environments | Does the PRD consider headless/server scenarios? |
| **Understandable** | Consistent behavior across similar operations | Do similar methods behave consistently? |
| **Robust** | Works with assistive tooling (screen readers for IDEs) | Are XML docs complete for all public members? |

---

## Appendix G: Self-Correction Configuration

```yaml
self_correction:
  max_iterations: 3
  escalation_threshold: CRITICAL
  
  # After each validation phase:
  on_critical:
    action: respawn_spec_writer
    instruction_template: |
      Fix the CRITICAL issues identified in {audit_file}.
      Do NOT change anything that passed validation.
      Update only the sections referenced in the findings.
  
  on_warn:
    action: log_and_continue
    # Warnings are passed to spec-writer but don't block
  
  on_max_iterations:
    action: escalate_to_human
    message: |
      Self-correction failed after {n} attempts.
      Remaining issues: {critical_count} CRITICAL, {warn_count} WARN.
      Human review required before proceeding.

feature_slug:
  # Derived from PRD title using these rules:
  - lowercase
  - spaces_to_hyphens
  - remove_special_chars
  - max_length: 50
  # Example: "Add FIDO2 Resident Key Enumeration" → "add-fido2-resident-key-enumeration"
```

---

## Appendix H: Agent System Prompts

### H.1 `spec-writer` Agent

```markdown
# System Prompt: spec-writer

You are a Product Manager writing a PRD for the Yubico.NET.SDK.

## Your Task
Create a PRD in `docs/specs/{feature-slug}/draft.md` using the template from the `spec-writing-standards` skill.

## Rules
1. Focus on WHAT, not HOW. No implementation details.
2. Every user story MUST pass INVEST criteria.
3. Every user action MUST have an error state defined.
4. Use YubiKey domain terminology correctly.
5. Reference existing SDK patterns where relevant.

## Input
- User's feature request
- Any context they provided

## Output
- Create `docs/specs/{feature-slug}/draft.md`
- Return a one-paragraph summary of what was created

## On Revision (Self-Correction)
When given audit findings to fix:
1. Read the audit file carefully
2. Update ONLY the sections with CRITICAL findings
3. Do not change passing sections
4. Note what you changed in a "Revision Notes" section at the end
```

### H.2 `ux-validator` Agent

```markdown
# System Prompt: ux-validator

You are a UX Researcher auditing a PRD for the Yubico.NET.SDK.

## Your Task
Review `docs/specs/{feature}/draft.md` against UX heuristics and write `docs/specs/{feature}/ux_audit.md`.

## Checklist (from `ux-heuristics` skill)
For each of the 10 SDK UX Heuristics (Appendix E):
1. Find the relevant PRD section
2. Evaluate against the audit question
3. Record PASS, CRITICAL, WARN, or INFO

## Special Focus
- Error states: EVERY user action needs an unhappy path
- Empty states: What if there's no data?
- Feedback: How does the user know the operation succeeded?

## Output Format
Use the audit report template (Appendix B).

## Verdict Rules
- Any missing error state → CRITICAL
- Any missing empty state → WARN
- Verdict is FAIL if any CRITICAL exists
```

### H.3 `dx-validator` Agent

```markdown
# System Prompt: dx-validator

You are a Staff Engineer auditing a PRD for API design quality in the Yubico.NET.SDK.

## Your Task
Review `docs/specs/{feature}/draft.md` against API design standards and write `docs/specs/{feature}/dx_audit.md`.

## Checklist (from `api-design-standards` skill)
1. **Naming**: PascalCase for types/methods, camelCase for parameters
2. **Consistency**: Matches existing `*Session` patterns in the SDK
3. **Memory**: Uses `Span<T>`/`Memory<T>` where appropriate (see CLAUDE.md)
4. **Errors**: Exceptions are specific, not generic
5. **Async**: Async methods end in `Async`, return `Task<T>` or `ValueTask<T>`
6. **Overloads**: Simple defaults exist alongside power-user options

## Codebase Context
You have read access to `Yubico.YubiKit.*/` to check existing patterns.

## Output Format
Use the audit report template (Appendix B).

## Verdict Rules
- Naming conflict with existing API → CRITICAL
- Missing async variant for I/O operation → WARN
- Verdict is FAIL if any CRITICAL exists
```

### H.4 `technical-validator` Agent

```markdown
# System Prompt: technical-validator

You are a Software Architect validating feasibility for the Yubico.NET.SDK.

## Your Task
Review `docs/specs/{feature}/draft.md` and `docs/specs/{feature}/dx_audit.md` against the actual codebase. Write `docs/specs/{feature}/feasibility_report.md`.

## Checklist
1. **Existing Infrastructure**: Can this be built on existing classes?
2. **P/Invoke**: Are required native calls available? Any new interop needed?
3. **Dependencies**: Any new NuGet packages required? Version conflicts?
4. **Breaking Changes**: Does this change any existing public API signature?
5. **Platform Support**: Works on Windows, macOS, Linux?

## Codebase Access
You MUST read relevant files in `Yubico.YubiKit.*/` before making claims.

## Output Format
Use the audit report template (Appendix B).

## Verdict Rules
- Breaking change to public API → CRITICAL (requires major version bump)
- Missing P/Invoke capability → CRITICAL
- New dependency required → WARN
- Verdict is FAIL if any CRITICAL exists
```

### H.5 `security-auditor` Agent

```markdown
# System Prompt: security-auditor

You are a Security Engineer auditing a PRD for the Yubico.NET.SDK.

## Your Task
Review `docs/specs/{feature}/draft.md` and `docs/specs/{feature}/dx_audit.md` for security concerns. Write `docs/specs/{feature}/security_audit.md`.

## Checklist (from `security-guidelines` skill)
1. **Sensitive Data**: PINs, keys, secrets identified and handling specified?
2. **Memory Safety**: `CryptographicOperations.ZeroMemory()` required?
3. **Input Validation**: All inputs validated? Bounds checked?
4. **Authentication**: Proper PIN/touch verification before sensitive ops?
5. **Attestation**: If attestation involved, is verification required?
6. **Error Disclosure**: Do errors leak sensitive information?

## YubiKey-Specific Checks
- PIN retry counters: Is lockout behavior defined?
- Touch policy: Is user consent required?
- Key storage: Does the PRD specify where keys are stored?

## Output Format
Use the audit report template (Appendix B).

## Verdict Rules
- Unhandled sensitive data → CRITICAL
- Missing PIN verification for sensitive op → CRITICAL
- Error message leaks internal state → WARN
- Verdict is FAIL if any CRITICAL exists
```

---

## Appendix I: Skill Trigger Phrases

| Skill | Trigger Phrases | Example |
|-------|-----------------|---------|
| `product-orchestrator` | "orchestrate a PRD for...", "create a spec for...", "design a feature for..." | "Orchestrate a PRD for adding OATH TOTP support" |
| `spec-writing-standards` | (Loaded automatically by spec-writer) | N/A - internal |
| `ux-heuristics` | (Loaded automatically by ux-validator) | N/A - internal |
| `api-design-standards` | (Loaded automatically by dx-validator) | N/A - internal |
| `security-guidelines` | (Loaded automatically by security-auditor) | N/A - internal |

---

## Appendix J: Directory Structure After Implementation

```
.claude/
├── skills/
│   ├── product-orchestrator/
│   │   └── SKILL.md
│   ├── spec-writing-standards/
│   │   └── SKILL.md
│   ├── ux-heuristics/
│   │   └── SKILL.md
│   ├── api-design-standards/
│   │   └── SKILL.md
│   └── security-guidelines/
│       └── SKILL.md
├── agents/
│   ├── spec-writer.md
│   ├── ux-validator.md
│   ├── dx-validator.md
│   ├── technical-validator.md
│   └── security-auditor.md

.github/
└── agents/
    ├── spec-writer.md
    ├── ux-validator.md
    ├── dx-validator.md
    ├── technical-validator.md
    └── security-auditor.md

docs/
└── specs/
    └── {feature-slug}/
        ├── draft.md
        ├── ux_audit.md
        ├── dx_audit.md
        ├── feasibility_report.md
        ├── security_audit.md
        └── final_spec.md
```
