---
name: worktree-stack
description: Use when combining git worktrees with stacked PRs for parallel isolated development - one worktree per stack layer, no branch switching required
---

# Worktree + Stack Workflow

## Overview

Combines `git worktree` (parallel filesystem isolation per layer) with `gh stack` (PR chain management) for maximum development velocity. Each stack layer lives in its own directory, so multiple layers — and multiple agents — can be worked on simultaneously without stashing or branch switching.

**Core principle:** One worktree per concern. Share the object store, isolate the working trees.

## Use when

**Use this skill when:**
- Building a feature that decomposes into 2+ dependent layers you want to work on in parallel
- Running autonomous agents on multiple stack layers simultaneously
- You want to work on layer N+1 while layer N is under review (zero context-switch cost)
- Bisecting or experimenting without disturbing active working trees

**Don't use when:**
- A single PR is sufficient — worktrees add overhead not worth it for one layer
- The layers are strictly sequential and you're not doing parallel work
- You're on a machine with very limited disk (worktrees share the object store but duplicate working files)

## Mental Model

```
repo/
├── .git/                          ← shared object store (all worktrees share this)
├── <main-clone>/                  ← trunk / develop (read-mostly)
├── feature-infra/                 ← stack layer 1  (PR #N)
├── feature-api/                   ← stack layer 2  (PR #N+1, base = layer 1)
└── feature-ui/                    ← stack layer 3  (PR #N+2, base = layer 2)
```

All worktrees share `.git` — commits, refs, and objects are immediately available across all trees with no pushing between them.

## Process

### 1. Set up trunk worktree (if not already)

```bash
# You're already in the main clone — this is your "trunk" tree
git branch --show-current   # should be develop or your base
```

### 2. Create a worktree for each layer

```bash
# Layer 1: branch off trunk
git worktree add ../feature-infra -b feature-infra

# Layer 2: branch off layer 1 (not trunk)
git worktree add ../feature-api -b feature-api feature-infra

# Layer 3: branch off layer 2
git worktree add ../feature-ui -b feature-ui feature-api
```

> Each worktree starts at the tip of its parent branch. Work flows trunk → layer 1 → layer 2 → layer 3.

### 3. Initialise the stack

From **any** worktree in the chain, initialise the stack with all branches:

```bash
cd ../feature-infra
gh stack init feature-infra feature-api feature-ui
```

Or init the bottom layer and add the rest:

```bash
cd ../feature-infra
gh stack init feature-infra
gh stack add feature-api   # must be run from the top of the current stack
gh stack add feature-ui
```

### 4. Work in parallel

Each worktree is an independent filesystem. Open separate terminals (or dispatch separate agents):

```
Terminal A: cd ../feature-infra && <work>
Terminal B: cd ../feature-api   && <work>
Terminal C: cd ../feature-ui    && <work>
```

Commits made in one worktree are **immediately visible** in all others via the shared `.git` — no push/pull needed between local worktrees.

### 5. Submit the stack

From any worktree that has the stack tracked:

```bash
cd ../feature-infra
gh stack submit --open
```

This pushes all branches and creates/updates PRs for all layers.

### 6. Handle review feedback on a lower layer

When a reviewer requests changes on layer 1 while you're building layer 3:

```bash
# In feature-infra worktree — make the fix, commit
cd ../feature-infra
# ... edit, git add, git commit

# Cascade rebase from feature-infra upward
gh stack rebase
gh stack push

# Layer 2 and 3 are now rebased — verify in their worktrees
cd ../feature-api && dotnet toolchain.cs test --smoke
cd ../feature-ui  && dotnet toolchain.cs test --smoke
```

### 7. Parallel agent dispatch

Each worktree is safe for an autonomous agent because it has an isolated working tree. From your orchestrator:

```bash
# Dispatch agents, one per worktree
# Agent A owns feature-infra/
# Agent B owns feature-api/    (depends on A's output, starts after A commits)
# Agent C owns feature-ui/     (independent of B, can run in parallel)
```

After agents complete, run:

```bash
cd ../feature-infra
gh stack rebase   # ensure linear history
gh stack push
gh stack submit
```

### 8. Cleanup after merge

```bash
# After PRs merge, prune local tracking
cd ../feature-infra
gh stack sync --prune

# Remove worktrees
git worktree remove ../feature-infra
git worktree remove ../feature-api
git worktree remove ../feature-ui

# Delete local branches
git branch -d feature-infra feature-api feature-ui
```

## Rules

| Rule | Why |
|------|-----|
| One worktree per layer | No branch switching, no stashing, no context loss |
| Branch each layer off its parent, not trunk | Preserves the dependency chain for `gh stack` |
| Only `gh stack push` to push stack branches | `--force-with-lease` safety; avoids clobbering remote |
| Run `gh stack rebase` after any lower-layer amendment | Cascades linearity to all layers above |
| Remove worktrees after merge | `git worktree list` should stay clean |

## Common Mistakes

**❌ Branching all layers off trunk**
```bash
# Wrong — all three branch off develop, no dependency chain
git worktree add ../feature-api -b feature-api develop
git worktree add ../feature-ui  -b feature-ui  develop
```
**✅ Branch each layer off its parent:**
```bash
git worktree add ../feature-api -b feature-api feature-infra
git worktree add ../feature-ui  -b feature-ui  feature-api
```

**❌ Running `gh stack` from the wrong worktree**
`gh stack` tracks the stack per-branch. Always run stack commands from the worktree whose branch is in the stack.

**❌ Forgetting `gh stack rebase` after a lower-layer fix**
Layer 2 and 3 won't automatically rebase — you must propagate the change manually.

**❌ Using `git push --force` on a stack branch**
Always use `gh stack push`. It uses `--force-with-lease` per branch, preventing silent overwrites.

## Verification

- [ ] `git worktree list` shows one entry per layer plus the main tree
- [ ] Each layer branch has the layer below it as its parent: `git log --oneline feature-api | grep $(git rev-parse --short feature-infra)`
- [ ] `gh stack view` from any layer worktree shows all layers in order
- [ ] All layers show `needsRebase: false` in `gh stack view --json`
- [ ] Agents or terminal sessions each confined to their own worktree directory

## Related Skills

- `workflow-stack` — Use first to understand `gh stack` commands before adding worktrees
- `dispatch-agents` — Use to dispatch parallel agents, one per worktree layer
- `git-commit` — Use within each worktree for conventional commits before `gh stack add`
- `workflow-finish` — Use after all layers are merged and worktrees need cleanup
