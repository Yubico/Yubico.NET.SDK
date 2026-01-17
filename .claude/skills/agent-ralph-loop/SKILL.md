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

## 2. Command Signature

**Executable:** `bun .claude/skills/agent-ralph-loop/ralph-loop.ts`

### Parameters

| Parameter | Type | Required | Description |
| :--- | :--- | :--- | :--- |
| `[PROMPT]` | `string` | **Yes** (or via file) | The objective string passed as an argument. |
| `--prompt-file` | `path` | **Yes** (alternative) | Read the prompt from a file. **Preferred for complex instructions** to avoid shell escaping issues. |
| `--completion-promise` | `string` | Recommended | A unique string (e.g., `"DONE_V1"`) that signals success. Without this, the loop runs until `--max-iterations`. |
| `--max-iterations` | `number` | No | Safety limit. Default: `0` (unlimited). |
| `--delay` | `number` | No | Seconds between loops. Default: `2`. |
| `--learn` | `flag` | No | Enable to generate a `review.md` post-mortem analysis. |
| `--model` | `string` | No | LLM model to use. Recommended: `claude-sonnet-4.5` (balanced), `claude-haiku-4.5` (fast/cheap), or `claude-opus-4.5` (highest quality). |

**Outputs:** State + logs are written under `./docs/ralph-loop/` (e.g., `state.md`, `iteration-*.log`, and learning artifacts under `./docs/ralph-loop/learning/`).

## 3. Autonomy Directives (Auto-Injected)

The script automatically appends autonomy directives to your prompt, instructing the agent to:
- Operate in non-interactive mode without asking questions
- Execute immediately on ambiguous decisions using standard patterns
- Use git to explore the codebase and check previous work
- Output the completion promise only when the objective is fully verified

**You do not need to add these instructions to your prompt manually.**

## 4. Usage Examples

### Example 1: Simple Command Line (Fastest)
**Goal:** Quick scaffolding or simple refactor.

```bash
bun .claude/skills/agent-ralph-loop/ralph-loop.ts \
  "Create a 'Hello World' Express server in src/app.ts and a test in src/app.test.ts." \
  --completion-promise "DONE"
```

### Example 2: Using a Prompt File (Recommended for Complex Tasks)
**Goal:** Complex refactor defined in a markdown file.

Create a file `task_prompt.md` containing your objective (autonomy directives are auto-injected).

```bash
bun .claude/skills/agent-ralph-loop/ralph-loop.ts --prompt-file task_prompt.md \
  --completion-promise "REFACTOR_COMPLETE" \
  --max-iterations 20 \
  --learn
```

### Example 3: Autonomous Debugging
**Goal:** Fix a failing test suite by iterating on the code.

```bash
bun .claude/skills/agent-ralph-loop/ralph-loop.ts \
  "Run 'npm test'. Analyze the stderr output. Locate the failing code in src/. Apply fixes. Re-run 'npm test'. Repeat until passing." \
  --completion-promise "TESTS_PASSED" \
  --max-iterations 12 \
  --model claude-sonnet-4.5
```
## 5. Success Criteria
**Success:** The process exits with code 0 and logs ✅ Ralph loop: Detected <promise>...</promise>.

**Failure:** The process exits because max_iterations was reached. This indicates the agent got stuck or the prompt was too vague.