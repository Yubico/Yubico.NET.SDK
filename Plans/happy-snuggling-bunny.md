# Plan: Fix Git Worktree Parallel Lock Contention

## Context

When spawning 2+ agents with `isolation: "worktree"` in a single message, Claude Code creates worktrees concurrently. Each `git worktree add` writes branch tracking info to `.git/config`, requiring `.git/config.lock`. Concurrent writes fail because Git uses exclusive file locking — this is fundamental to Git's design.

**Your setup is not the cause.** No custom `WorktreeCreate` hook exists, and your hooks don't affect worktree creation. This is a gap in Claude Code's implementation — they should serialize worktree creation or use `--no-track`.

## Design Goal

Make parallel worktree agents "just work" whenever Claude encounters:
- "parallelize the work" / "do worktrees"
- `/DevTeam` in worktrees
- Any workflow spawning 2+ agents with worktree isolation

**No separate skill invocation needed** — the fix is woven into the existing Delegation skill and reinforced by a global behavioral rule.

## Changes

### 1. Update Delegation Skill — Replace Section 2 "Worktree-Isolated Agents"

**File:** `/Users/Dennis.Dyall/.claude/skills/Utilities/Delegation/SKILL.md`

Replace the current section 2 (lines 46-58) with a safer pattern that pre-creates worktrees sequentially before launching agents in parallel:

```markdown
### 2. Worktree-Isolated Agents

Run agents in their own git worktree for file-safe parallelism.

**CRITICAL: Never use `isolation: "worktree"` on 2+ parallel agents in the same message.**
Git's `.git/config.lock` causes concurrent worktree creation to fail. Instead, pre-create worktrees sequentially, then launch agents at those paths.

#### Single Agent (safe as-is)
```
Task(subagent_type="Engineer", isolation: "worktree", prompt="...")
```

#### Multiple Parallel Agents (use pre-creation pattern)
```bash
# Step 1: Create worktrees SEQUENTIALLY (each <1s, no lock contention)
git worktree add --no-track .claude/worktrees/agent-1 -b wt-agent-1 HEAD
git worktree add --no-track .claude/worktrees/agent-2 -b wt-agent-2 HEAD
git worktree add --no-track .claude/worktrees/agent-3 -b wt-agent-3 HEAD
```

```
# Step 2: Launch agents IN PARALLEL pointing at worktree directories (no isolation flag)
Task(subagent_type="Engineer", prompt="Work in /path/.claude/worktrees/agent-1. ...")
Task(subagent_type="Engineer", prompt="Work in /path/.claude/worktrees/agent-2. ...")
Task(subagent_type="Engineer", prompt="Work in /path/.claude/worktrees/agent-3. ...")
```

```bash
# Step 3: After agents complete, cleanup
git worktree remove .claude/worktrees/agent-1
git worktree remove .claude/worktrees/agent-2
git worktree remove .claude/worktrees/agent-3
# Also clean up branches if no longer needed
git branch -d wt-agent-1 wt-agent-2 wt-agent-3
```

**Guidelines:**
- `--no-track` avoids `.git/config` writes (the lock contention root cause)
- Use `HEAD` as the base ref (or specify a branch like `develop`)
- Worktree paths go under `.claude/worktrees/` (gitignored)
- Always cleanup after agents finish, even on failure
```

### 2. Add Global Behavioral Rule

**File:** `/Users/Dennis.Dyall/.claude/CLAUDE.md`

Add to the behavioral rules section:

```markdown
### Parallel worktree agents
When spawning 2+ agents that need git worktree isolation in parallel, NEVER use `isolation: "worktree"` on multiple Agent/Task calls in the same message. Instead: (1) pre-create worktrees sequentially with `git worktree add --no-track`, (2) launch agents pointing at those directories without the isolation flag, (3) cleanup worktrees after agents complete. See `skills/Utilities/Delegation/SKILL.md` section 2.
```

### 3. Ensure `.claude/worktrees/` is gitignored

**File:** `/Users/Dennis.Dyall/.claude/.gitignore` (or project `.gitignore`)

Add `.claude/worktrees/` if not already present — verify first.

## Files to Modify

1. `/Users/Dennis.Dyall/.claude/skills/Utilities/Delegation/SKILL.md` — Replace section 2
2. `/Users/Dennis.Dyall/.claude/CLAUDE.md` — Add behavioral rule
3. `.gitignore` — Ensure `.claude/worktrees/` is ignored (verify first)

## Why Not a Separate Skill?

- The pattern is 3 bash commands + agent launches — too thin for a standalone skill
- Embedding it in the Delegation skill means it's automatically consulted whenever parallel agents are orchestrated
- The CLAUDE.md rule catches cases where Delegation isn't explicitly invoked
- Other workflows (DevTeam, agent-dispatch) already route through Delegation for parallelism

## Verification

1. Create 3 worktrees sequentially with `--no-track` — all succeed, no lock errors
2. Launch 3 agents in parallel at those paths — all run concurrently without contention
3. Cleanup worktrees — clean state restored
4. Test with `/DevTeam` to confirm the pattern activates automatically
5. Verify single-agent `isolation: "worktree"` still works (no regression)
