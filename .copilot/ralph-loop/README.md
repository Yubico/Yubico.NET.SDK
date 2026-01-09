# Ralph Loop for Copilot CLI

Implementation of the Ralph Wiggum technique for iterative, self-referential AI development loops in GitHub Copilot CLI.

## What is Ralph Loop?

Ralph Loop is a development methodology based on continuous AI agent loops. As Geoffrey Huntley describes it: **"Ralph is a Bash loop"** - a simple `while true` that repeatedly feeds an AI agent a prompt file, allowing it to iteratively improve its work until completion.

### Core Concept

Unlike Claude Code's plugin-based stop hooks, Copilot CLI uses an **external bash loop**:

```bash
./ralph-loop.sh "Your task description" --completion-promise "DONE"

# The script automatically:
# 1. Pipes the prompt to copilot
# 2. Captures output
# 3. Checks for completion promise
# 4. If not complete, loops with same prompt
# 5. Repeat until completion or max iterations
```

This creates a **self-referential feedback loop** where:
- The prompt never changes between iterations
- Copilot's previous work persists in files
- Each iteration sees modified files and git history
- Copilot autonomously improves by reading its own past work in files

## Quick Start

```bash
# Make executable
chmod +x .copilot/ralph-loop/ralph-loop.sh

# Run with a task
./.copilot/ralph-loop/ralph-loop.sh "Build a REST API for todos. Requirements: CRUD operations, input validation, tests. Output <promise>COMPLETE</promise> when done." --completion-promise "COMPLETE" --max-iterations 50
```

Copilot will:
- Implement the API iteratively
- Run tests and see failures
- Fix bugs based on test output
- Iterate until all requirements met
- Output the completion promise when done

## Commands

### ralph-loop.sh

Start a Ralph loop.

**Usage:**
```bash
./ralph-loop.sh "<prompt>" [OPTIONS]
```

**Options:**
- `--max-iterations <n>` - Stop after N iterations (default: 0 = unlimited)
- `--completion-promise <text>` - Phrase that signals completion
- `--delay <seconds>` - Delay between iterations (default: 2)
- `--learn` - Enable learning mode (captures patterns for review)
- `-h, --help` - Show help

### cancel-ralph.sh

Cancel the active Ralph loop by removing the state file.

**Usage:**
```bash
./cancel-ralph.sh
```

## Prompt Writing Best Practices

### 1. Clear Completion Criteria

❌ Bad: "Build a todo API and make it good."

✅ Good:
```markdown
Build a REST API for todos.

When complete:
- All CRUD endpoints working
- Input validation in place
- Tests passing (coverage > 80%)
- README with API docs
- Output: <promise>COMPLETE</promise>
```

### 2. Incremental Goals

❌ Bad: "Create a complete e-commerce platform."

✅ Good:
```markdown
Phase 1: User authentication (JWT, tests)
Phase 2: Product catalog (list/search, tests)
Phase 3: Shopping cart (add/remove, tests)

Output <promise>COMPLETE</promise> when all phases done.
```

### 3. Self-Correction

❌ Bad: "Write code for feature X."

✅ Good:
```markdown
Implement feature X following TDD:
1. Write failing tests
2. Implement feature
3. Run tests
4. If any fail, debug and fix
5. Refactor if needed
6. Repeat until all green
7. Output: <promise>COMPLETE</promise>
```

### 4. Escape Hatches

Always use `--max-iterations` as a safety net:

```bash
# Recommended: Always set a reasonable iteration limit
./ralph-loop.sh "Try to implement feature X" --max-iterations 20
```

## Learning Mode

Enable with `--learn` to capture patterns and generate improvement suggestions:

```bash
./ralph-loop.sh "Build authentication system" --learn --max-iterations 15 --completion-promise "AUTH_COMPLETE"
```

### What Learning Mode Captures

At the end of each session (success, max iterations, or Ctrl+C), Ralph:

1. **Analyzes iteration logs** - Reviews what happened in each iteration
2. **Identifies tool patterns** - Frequent tool combinations that could be optimized
3. **Tracks file changes** - Which files were modified most often
4. **Documents strategies** - What worked and what didn't
5. **Suggests skills** - Proposes reusable skills for `.copilot/skills/`
6. **Proposes improvements** - To both the loop script and future prompts

### Review Files

All learning output goes to `.copilot/ralph-loop/learning/review-*.md`:

```
.copilot/ralph-loop/learning/
├── review-20260108-235630.md   # Human-reviewable suggestions
├── analysis.log                 # Raw analysis output
```

**Nothing is auto-applied!** You review and decide what to implement.

### Example Review File Structure

```markdown
# Ralph Loop Learning Review

## Summary
Completed authentication system in 8 iterations...

## Suggested Skills

### skill: jwt-auth-setup
```yaml
name: jwt-auth-setup
description: Set up JWT authentication with refresh tokens
triggers: ["jwt auth", "authentication system", "login endpoint"]
steps:
  1. Create auth middleware
  2. Implement token generation
  3. Add refresh token logic
  ...
```

## Tool Usage Patterns
- `grep` + `edit` combination used 12 times for find-and-replace
- `bash npm test` followed by `edit` for test-driven fixes

## Proposed Improvements

### To ralph-loop.sh
- Add `--checkpoint` flag to save state every N iterations
- Consider adding `--dry-run` for prompt validation

### To Prompts
- Include specific test commands in the prompt
- Break into smaller phases for complex tasks
```

## Philosophy

Ralph embodies several key principles:

### 1. Iteration > Perfection
Don't aim for perfect on first try. Let the loop refine the work.

### 2. Failures Are Data
"Deterministically bad" means failures are predictable and informative. Use them to tune prompts.

### 3. Operator Skill Matters
Success depends on writing good prompts, not just having a good model.

### 4. Persistence Wins
Keep trying until success. The loop handles retry logic automatically.

## When to Use Ralph

**Good for:**
- Well-defined tasks with clear success criteria
- Tasks requiring iteration and refinement (e.g., getting tests to pass)
- Greenfield projects where you can walk away
- Tasks with automatic verification (tests, linters)

**Not good for:**
- Tasks requiring human judgment or design decisions
- One-shot operations
- Tasks with unclear success criteria
- Production debugging (use targeted debugging instead)

## Differences from Claude Code Version

| Feature | Claude Code | Copilot CLI |
|---------|-------------|-------------|
| Loop mechanism | Stop hook (in-session) | External bash loop |
| State file | `.claude/ralph-loop.local.md` | `.copilot/ralph-loop/state.md` |
| Transcript access | Yes (hooks API) | No (output capture only) |
| Cancel method | `/cancel-ralph` command | `cancel-ralph.sh` or Ctrl+C |
| Promise detection | Transcript parsing | Output parsing |
| Learning mode | No | Yes (`--learn` flag) |

## Learn More

- Original technique: https://ghuntley.com/ralph/
- Ralph Orchestrator: https://github.com/mikeyobrien/ralph-orchestrator
- Claude Code plugin: https://github.com/anthropics/claude-plugins-official/tree/main/plugins/ralph-loop
