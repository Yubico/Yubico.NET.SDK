---
name: ralph-loop
description: Use when spawning autonomous agent for complex multi-step tasks without supervision
---

# Ralph Loop

## Use when

- Need autonomous execution of complex, multi-step engineering tasks
- Task requires trial-and-error or iterative debugging
- Want hands-off execution with verification checkpoints
- Deep refactoring across multiple files
- Iterative debugging until tests pass

## 1. Description
The `ralph-loop` skill forces a sub-instance of GitHub Copilot to enter a recursive execution loop. It is designed to solve tasks that require trial-and-error, file exploration, or multiple distinct steps that cannot be completed in a single conversational turn.

**Use this skill for:**
* **Deep Refactoring:** Changing patterns across multiple files (e.g., "Migrate all `var` to `const` in `/src`").
* **Iterative Debugging:** Running tests, reading errors, fixing code, and repeating until pass.
* **Scaffolding:** Creating complex directory structures and boilerplates.

## 2. Operating Modes

### Progress File Mode (Recommended)
For structured, multi-phase work. The engine:
- Detects progress file format via YAML frontmatter
- Injects the full execution protocol automatically
- Tracks phase/task state
- Re-reads the file each iteration to find next task

```bash
bun .claude/skills/agent-ralph-loop/ralph-loop.ts \
  --prompt-file docs/ralph-loop/feature-progress.md \
  --completion-promise "DONE" --max-iterations 30
```

### Ad-hoc Mode
For quick, unstructured tasks. Provide a plain prompt (no progress file format).
The engine injects skill awareness and autonomy directives only.

```bash
bun .claude/skills/agent-ralph-loop/ralph-loop.ts \
  "Fix the failing test in FooTests.cs" \
  --completion-promise "FIXED"
```

---

## 3. Progress File Format

Progress files are **declarative** - they define WHAT to do, not HOW. The engine handles execution protocol.

### YAML Frontmatter (Required for detection)

```yaml
---
type: progress
feature: feature-name
prd: docs/specs/feature/final_spec.md  # optional, for traceability
started: 2026-01-19
status: in-progress  # in-progress | completed | blocked
---
```

### Structure

```markdown
# {Feature Name} Progress

## Phase 1: {Phase Name} (P0)

**Goal:** {One sentence from user story}
**Files:**
- Src: `path/to/implementation.cs`
- Test: `path/to/tests.cs`

### Tasks
- [ ] 1.1: {Task description}
- [ ] 1.2: {Task description}
- [x] 1.3: {Completed task} <!-- engine marks these -->

### Notes
<!-- Engine appends notes here after each task -->

---

## Phase 2: {Phase Name} (P1)
...
```

### Priority Markers

| Marker | Meaning | Order |
|--------|---------|-------|
| `(P0)` | Critical - must complete | First |
| `(P1)` | Important - should complete | Second |
| `(P2)` | Nice-to-have | Last |

### Task Selection Rules

1. Find highest priority incomplete phase (P0 → P1 → P2)
2. Within phase, find first unchecked `[ ]` task
3. Complete task fully before moving to next
4. Mark `[x]` only after verification passes

---

## 4. Execution Protocol (Auto-Injected)

When the engine detects a progress file, it injects these instructions automatically. **Do not add these to your progress file.**

### TDD Loop (For each task)

```
1. RED: Write failing test asserting the task's behavior
   Run: `dotnet build.cs test --filter "FullyQualifiedName~{TestClass}"`
   Expect: FAILURE

2. GREEN: Write minimal code to pass
   Run: `dotnet build.cs test --filter "FullyQualifiedName~{TestClass}"`
   Expect: SUCCESS

3. REFACTOR: Clean up, check security, add docs

4. COMMIT: `git add {specific files}` then `git commit -m "feat(scope): description"`

5. UPDATE: Mark task `[x]` in progress file, add notes
```

### Security Protocol

- **ZeroMemory:** Zero sensitive data (PINs, keys) using `CryptographicOperations.ZeroMemory`
- **No Logs:** Never log sensitive values
- **Validation:** Validate all input lengths and ranges

### Git Discipline

- **Explicit adds only:** `git add path/to/file.cs` - NEVER `git add .`
- **Conventional commits:** `feat:`, `fix:`, `test:`, `refactor:`, `docs:`
- **Commit per task:** One logical change per commit

### Build Commands

| Action | Command |
|--------|---------|
| Build | `dotnet build.cs build` |
| Test | `dotnet build.cs test` |
| Test filtered | `dotnet build.cs test --filter "..."` |

**NEVER use `dotnet build` or `dotnet test` directly** - they fail on mixed xUnit v2/v3.

---

## 5. Creating Progress Files

Progress files are created by converter skills:

| Source | Converter | Output |
|--------|-----------|--------|
| PRD spec | `prd-to-ralph` | `docs/ralph-loop/{feature}-progress.md` |
| Implementation plan | `plan-to-ralph` | `docs/ralph-loop/{feature}-progress.md` |
| Manual | Direct creation | `docs/ralph-loop/{feature}-progress.md` |

The engine never creates progress files - it only reads and updates them.

## 6. Command Signature

**Executable:** `bun .claude/skills/agent-ralph-loop/ralph-loop.ts`

### Parameters

| Parameter | Type | Required | Description |
| :--- | :--- | :--- | :--- |
| `[PROMPT]` | `string` | **Yes** (or via file) | The objective string passed as an argument. |
| `--prompt-file` | `path` | **Yes** (alternative) | Read the prompt from a file. **Progress files detected automatically.** |
| `--session` | `string` | No | Session name for output folder. Default: derived from prompt-file or timestamp. |
| `--completion-promise` | `string` | Recommended | A unique string (e.g., `"DONE_V1"`) that signals success. Without this, the loop runs until `--max-iterations`. |
| `--max-iterations` | `number` | No | Safety limit. Default: `0` (unlimited). |
| `--delay` | `number` | No | Seconds between loops. Default: `2`. |
| `--learn` | `flag` | No | Enable to generate a `review.md` post-mortem analysis. |
| `--model` | `string` | No | LLM model to use. Recommended: `claude-sonnet-4.5` (balanced), `claude-haiku-4.5` (fast/cheap), or `claude-opus-4.5` (highest quality). |

**Outputs:** Each session writes to `./docs/ralph-loop/<session>/` containing:
- `state.md` - Current loop state
- `iteration-*.log` - Output from each iteration
- `learning/` - Learning artifacts (when `--learn` is enabled)

**Session naming priority:**
1. `--session` flag (explicit)
2. Slug from `--prompt-file` name (e.g., `2026-01-18-fido2-testing.md` → `fido2-testing`)
3. Timestamp fallback (e.g., `2026-01-18T161244`)

## 7. Auto-Injected Directives

The script automatically injects directives based on the input type:

### Always Injected (Both Modes)
- **Skill awareness:** Categorized list of available skills (mandatory vs optional)
- **Autonomy directives:** Non-interactive mode, no questions, execute on ambiguity
- **Git exploration:** Use git to check previous work and continue

### Progress File Mode (Additional)
- **Execution protocol:** TDD loop, security rules, git discipline
- **Task context:** Current phase, next task, file paths
- **Update instructions:** How to mark tasks complete

**You do not need to add these instructions manually.**

## 8. Usage Examples

### Example 1: Progress File Mode (Recommended)
**Goal:** Execute a structured implementation from a progress file.

```bash
bun .claude/skills/agent-ralph-loop/ralph-loop.ts \
  --prompt-file docs/ralph-loop/fido2-credential-mgmt-progress.md \
  --completion-promise "DONE" \
  --max-iterations 30 \
  --learn
```

### Example 2: Simple Ad-hoc Command
**Goal:** Quick scaffolding or simple refactor.

```bash
bun .claude/skills/agent-ralph-loop/ralph-loop.ts \
  "Create a 'Hello World' Express server in src/app.ts and a test in src/app.test.ts." \
  --completion-promise "DONE"
```

### Example 3: Ad-hoc with Prompt File
**Goal:** Complex ad-hoc task (no progress file format).

```bash
bun .claude/skills/agent-ralph-loop/ralph-loop.ts \
  --prompt-file task_prompt.md \
  --completion-promise "REFACTOR_COMPLETE" \
  --max-iterations 20 \
  --learn
```

### Example 4: Autonomous Debugging
**Goal:** Fix a failing test suite by iterating on the code.

```bash
bun .claude/skills/agent-ralph-loop/ralph-loop.ts \
  "Run 'dotnet build.cs test'. Analyze failures. Fix code. Repeat until passing." \
  --completion-promise "TESTS_PASSED" \
  --max-iterations 12 \
  --model claude-sonnet-4.5
```

## 9. Success Criteria

**Success:** The process exits with code 0 and logs `✅ Ralph loop: Detected <promise>...</promise>`.

**Failure:** The process exits because `max_iterations` was reached. This indicates the agent got stuck or the task was too complex.

## 10. Related Skills

| Skill | Relationship |
|-------|--------------|
| `prd-to-ralph` | Creates progress files from PRD specs |
| `plan-to-ralph` | Creates progress files from implementation plans |
| `write-ralph-prompt` | Guidance for ad-hoc mode prompts |
| `write-plan` | Creates implementation plans (upstream of plan-to-ralph) |