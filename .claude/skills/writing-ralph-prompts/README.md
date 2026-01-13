# Writing Ralph Loop Prompts (Plans)

This skill is for writing **prompt files / implementation plans** intended to be executed autonomously using the **`ralph-loop` skill**.

**Reference (required):** `.claude/skills/ralph-loop/SKILL.md`
- Run loop executable: `bun .claude/skills/ralph-loop/ralph-loop.ts`
- State + logs: `./docs/ralph-loop/` (e.g., `state.md`, `iteration-*.log`, `learning/*`)

## Save Location

Offer to save the plan/prompt file to:

- `./docs/plans/ralph-loop/YYYY-MM-DD-<feature-name>.md`

If the user declines, still format the plan exactly as if it were saved there and include the suggested filename.

## Prompt File Requirements (Prompt-File Safe)

The plan MUST be runnable via `--prompt-file` and MUST include:
- A clear objective and **verification criteria**
- Exact file paths to create/modify
- Exact commands to run (build/test), with expected outcomes
- Bite-sized steps (2–5 minutes each), preferably TDD
- A unique **Completion Promise** token
- The **Autonomy Injection** block (below)

## Plan Template

```markdown
# [Feature Name] Implementation Plan (Ralph Loop)

**Goal:** [One sentence describing what this builds]

**Architecture:** [2-3 sentences about approach]

**Tech Stack:** [Key technologies/libraries]

**Completion Promise:** <PROMISE_TOKEN>

---

### Task 1: [Component]

**Files:**
- Create: `exact/path/to/file.cs`
- Modify: `exact/path/to/existing.cs:123-145`
- Test: `tests/exact/path/to/test.cs`

**Step 1: Write the failing test**
- Include complete test code (no placeholders)

**Step 2: Run test to confirm failure**
Run: `dotnet test --filter "FullyQualifiedName~TestName"`
Expected: FAIL (describe expected failure)

**Step 3: Minimal implementation**
- Include complete implementation code

**Step 4: Re-run test to confirm pass**
Run: `dotnet test --filter "FullyQualifiedName~TestName"`
Expected: PASS

**Step 5: Commit**
```bash
git add <paths>
git commit -m "feat: <message>"
```

---

## Autonomy Injection (MUST BE PRESENT)

CONTEXT: You are in NON-INTERACTIVE mode. The user is not present. Do not ask for clarification. If a decision is ambiguous, select the standard industry pattern and EXECUTE immediately. Output <promise>{COMPLETION_PROMISE}</promise> ONLY when the specific objective is verified.
```

## Handoff (ALWAYS END WITH THIS)

After presenting (and optionally saving) the plan, end by printing the exact one-liner to start the loop:

```bash
bun .claude/skills/ralph-loop/ralph-loop.ts --prompt-file ./docs/plans/ralph-loop/<plan-file>.md --completion-promise "<PROMISE_TOKEN>" --max-iterations 20 --learn
```
