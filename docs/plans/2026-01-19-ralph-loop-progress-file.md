# Ralph Loop Progress File Mode Implementation Plan

**Goal:** Move execution protocol into ralph-loop engine, making progress files declarative and self-sufficient.

**Architecture:** Detect progress file format via YAML frontmatter, inject execution protocol automatically, parse/update checkbox state each iteration.

**Decision Record:** Discussion in session 2026-01-19 - progress file pattern was embedded in prd-to-ralph template, but belongs in the execution engine itself.

---

## Task 1: Define Progress File Format in ralph-loop/SKILL.md

**Files:**
- Modify: `.claude/skills/agent-ralph-loop/SKILL.md`

**Changes:**
1. Add "Progress File Format" section with:
   - YAML frontmatter schema (`type: progress`, `feature`, `prd` optional)
   - Phase structure (P0/P1/P2 priority)
   - Task checkbox format `- [ ]` / `- [x]`
   - Files section per phase
2. Add "Execution Protocol" section documenting what engine enforces:
   - TDD loop (RED→GREEN→Refactor)
   - Security protocol (ZeroMemory, no secrets in logs)
   - Git discipline (explicit adds, conventional commits)
   - Phase ordering rules
3. Clarify two modes: progress-file vs ad-hoc

**Verify:** Review rendered markdown for completeness.

---

## Task 2: Implement Progress File Detection in ralph-loop.ts

**Files:**
- Modify: `.claude/skills/agent-ralph-loop/ralph-loop.ts`

**Changes:**
1. Add `isProgressFile(content: string): boolean` - detect YAML frontmatter with `type: progress`
2. Add `parseProgressFile(content: string): ProgressFileState` interface:
   ```typescript
   interface ProgressFileState {
     feature: string;
     prd?: string;
     phases: Phase[];
     currentTask: Task | null;
   }
   ```
3. Add `findNextTask(state: ProgressFileState): Task | null` - find first unchecked `[ ]`

**Verify:** `bun .claude/skills/agent-ralph-loop/ralph-loop.ts --help` runs without error.

---

## Task 3: Inject Execution Protocol for Progress Files

**Files:**
- Modify: `.claude/skills/agent-ralph-loop/ralph-loop.ts`

**Changes:**
1. Add `EXECUTION_PROTOCOL` constant with TDD/security/git rules
2. In `RalphLoop.start()`, after reading prompt file:
   - If `isProgressFile()`, inject protocol + current task context
   - Otherwise, use existing ad-hoc behavior
3. Protocol injection includes:
   - TDD loop instructions
   - Security checklist
   - "Update this file after completing task" instruction
   - Current phase/task extracted from state

**Verify:** `bun .claude/skills/agent-ralph-loop/ralph-loop.ts --help` runs without error.

---

## Task 4: Simplify prd-to-ralph/SKILL.md

**Files:**
- Modify: `.claude/skills/workflow-prd-to-ralph/SKILL.md`

**Changes:**
1. Remove "Workflow Instructions (For Autonomous Agent)" section from template (lines 68-105)
2. Remove security protocol from template (it's now in engine)
3. Remove TDD loop from template
4. Keep: PRD→Phase mapping, task structure, file paths, priority markers
5. Update "Generate Ralph Prompt" section - simpler invocation without custom prompt
6. Add note: "Execution protocol is injected automatically by ralph-loop engine"

**Verify:** Review that template is now purely declarative.

---

## Task 5: Update write-ralph-prompt/SKILL.md

**Files:**
- Modify: `.claude/skills/agent-ralph-prompt/SKILL.md`

**Changes:**
1. Add header clarifying this is for "ad-hoc mode" (no progress file)
2. Add note: "For progress-file mode, use prd-to-ralph or plan-to-ralph - protocol is automatic"
3. Keep all existing content (still valid for ad-hoc prompts)

**Verify:** Review for clarity.

---

## Completion Criteria

- [x] Task 1: SKILL.md documents format + protocol
- [x] Task 2: Detection/parsing functions added
- [x] Task 3: Protocol injection working
- [x] Task 4: prd-to-ralph simplified
- [x] Task 5: write-ralph-prompt clarified
- [ ] Integration: Test with sample progress file (manual)

**Total estimate:** ~45 minutes interactive execution
