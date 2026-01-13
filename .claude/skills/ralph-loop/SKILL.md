---
name: ralph-loop
description: Spawn an autonomous recursive agent loop to handle complex, multi-step engineering tasks (refactors, debugging, scaffolding) without user supervision.
---

## 1. Description
The `ralph-loop` skill forces a sub-instance of GitHub Copilot to enter a recursive execution loop. It is designed to solve tasks that require trial-and-error, file exploration, or multiple distinct steps that cannot be completed in a single conversational turn.

**Use this skill for:**
* **Deep Refactoring:** Changing patterns across multiple files (e.g., "Migrate all `var` to `const` in `/src`").
* **Iterative Debugging:** Running tests, reading errors, fixing code, and repeating until pass.
* **Scaffolding:** Creating complex directory structures and boilerplates.

## 2. Command Signature

**Executable:** `bun .claude/skills/ralph-loop/ralph-loop.ts`

### Parameters

| Parameter | Type | Required | Description |
| :--- | :--- | :--- | :--- |
| `[PROMPT]` | `string` | **Yes** (or via file) | The objective string passed as an argument. |
| `--prompt-file` | `path` | **Yes** (alternative) | Read the prompt from a file. **Preferred for complex instructions** to avoid shell escaping issues. |
| `--completion-promise` | `string` | **Yes** | A unique string (e.g., `"DONE_V1"`) that signals success. |
| `--max-iterations` | `number` | No | Safety limit. Default: `0` (unlimited). |
| `--delay` | `number` | No | Seconds between loops. Default: `2`. |
| `--learn` | `flag` | No | Enable to generate a `review.md` post-mortem analysis. |

**Outputs:** State + logs are written under `./docs/ralph-loop/` (e.g., `state.md`, `iteration-*.log`, and learning artifacts under `./docs/ralph-loop/learning/`).

## 3. Autonomy Injection (CRITICAL)

The script is passive by default. To ensure the agent is truly autonomous and never asks the user for input, you **MUST** append the following text block to the end of your prompt (whether passed via string or file):

> "CONTEXT: You are in NON-INTERACTIVE mode. The user is not present. Do not ask for clarification. If a decision is ambiguous, select the standard industry pattern and EXECUTE immediately. Output <promise>{COMPLETION_PROMISE}</promise> ONLY when the specific objective is verified."

## 4. Usage Examples

### Example 1: Simple Command Line (Fastest)
**Goal:** Quick scaffolding or simple refactor.

```bash
bun .claude/skills/ralph-loop/ralph-loop.ts "Create a 'Hello World' Express server in src/app.ts and a test in src/app.test.ts. CONTEXT: You are in NON-INTERACTIVE mode. Do not ask questions. Output <promise>DONE</promise> when files are created." \
  --completion-promise "DONE"
```

### Example 2: Using a Prompt File (Recommended for Complex Tasks)
**Goal:** Complex refactor defined in a generated markdown file.

Create a temporary file task_prompt.md containing the objective AND the Autonomy Injection text.

Run the loop pointing to that file.

```bash

bun .claude/skills/ralph-loop/ralph-loop.ts --prompt-file task_prompt.md \
  --completion-promise "REFACTOR_COMPLETE" \
  --max-iterations 20 \
  --learn
```
### Example 3: Autonomous Debugging
**Goal:** Fix a failing test suite by iterating on the code.

```bash

bun .claude/skills/ralph-loop/ralph-loop.ts "Run 'npm test'. Analyze the stderr output. Locate the failing code in src/. Apply fixes. Re-run 'npm test'. Repeat until passing. CONTEXT: You are in NON-INTERACTIVE mode. The user is not present. Do not ask for clarification. If a decision is ambiguous, select the standard industry pattern and EXECUTE immediately. Output <promise>TESTS_PASSED</promise> ONLY when the specific objective is verified." \
  --completion-promise "TESTS_PASSED" \
  --max-iterations 12 \
  --delay 2
```
## 5. Success Criteria
**Success:** The process exits with code 0 and logs ✅ Ralph loop: Detected <promise>...</promise>.

**Failure:** The process exits because max_iterations was reached. This indicates the agent got stuck or the prompt was too vague.