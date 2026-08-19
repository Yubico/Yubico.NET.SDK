---
name: stack
description: Use when creating, managing, or submitting stacked pull requests - guides gh stack workflow for keeping PRs clean and focused (one concern per PR)
---

# Stacked PR Workflow

## Overview

Guides the full `gh stack` lifecycle: initialising a stack, adding layers, submitting PRs, staying rebased, and merging. Enforces the rule that **unrelated work belongs in a separate stack** and each PR is independently reviewable.

**Core principle:** One concern per PR. Stack layers for dependent work; separate stacks for unrelated work.

## Use when

**Use this skill when:**
- User says "make a stack", "stack this", or "submit as stacked PR"
- Work naturally decomposes into 2+ dependent layers (e.g. infra → feature → tests)
- You need to deliver a PR chain where reviewers approve layers independently
- Rebasing or syncing an existing stack after trunk or lower layers changed

**Don't use when:**
- A single standalone PR is sufficient (no layering needed)
- Work is unrelated to an existing stack — start a fresh stack instead
- The stack already exists and you only need `gh stack sync` (just run it)

## Process

### 1. Decide the trunk

The trunk is the branch the bottom PR targets. Default is the repo default branch (`develop` here), but it can be any branch — including the head of an existing PR if you're layering on top.

```bash
# Check what branch you're on — that becomes the implicit trunk if no --base given
git branch --show-current

# Or explicitly set trunk to any branch
gh stack init --base <trunk-branch>
```

> ⚠️ **Do not hardcode `develop` in the command.** Use `--base` only when you need to override the default. `gh stack init` with no flags bases on the repository default.

### 2. Initialise the stack

```bash
# Interactive — prompts for first branch name
gh stack init

# Non-interactive — name the first layer upfront
gh stack init <layer-1-branch>
```

`gh stack init` enables `git rerere` automatically (conflict resolution memory across rebases).

### 3. Add layers

Each call to `gh stack add` creates a new branch at the current HEAD and checks it out:

```bash
# Prompt for branch name
gh stack add

# Name it explicitly
gh stack add <layer-2-branch>

# Stage + commit + auto-name in one step
gh stack add -Am "Add API layer"
```

Commit your changes on the new branch before adding the next layer.

### 4. View the stack

```bash
gh stack view          # full view with PR links and timestamps
gh stack view --short  # branch names only
```

### 5. Submit (push + create PRs + link)

```bash
gh stack submit        # interactive editor — review titles/descriptions per PR
gh stack submit --auto # skip editor, use auto-generated titles
gh stack submit --open # mark all new PRs as ready for review
```

First submit creates the PRs and links them into a GitHub stack. Subsequent submits update existing PRs.

### 6. Stay rebased (when trunk or a lower layer changes)

```bash
gh stack rebase        # fetch, cascading rebase from trunk upward
gh stack push          # force-with-lease push all branches
gh stack submit        # sync PR state on GitHub
```

If a conflict occurs during rebase:
```bash
# Resolve conflicts, then:
git add <resolved-files>
gh stack rebase --continue

# Or bail:
gh stack rebase --abort
```

### 7. Navigate between layers

```bash
gh stack up            # move up toward stack tip (away from trunk)
gh stack down          # move down toward trunk
gh stack switch        # interactive picker
```

### 8. Merge

```bash
gh stack merge         # interactive: choose which PRs to merge and method
gh stack merge --yes --squash  # non-interactive squash merge of whole stack
```

All PRs below the one you merge must also meet branch protection requirements.

### 9. Cleanup after merge

```bash
gh stack sync --prune  # prune local branches for merged PRs
```

## Rules

| Rule | Why |
|------|-----|
| Unrelated work → new stack | Keeps reviewer scope narrow |
| Each layer independently understandable | Reviewer can approve without reading the whole stack |
| `gh stack rebase` before adding new layers | Keeps history linear |
| Never `git push --force` manually on stack branches | Use `gh stack push` (uses `--force-with-lease` safely) |

## Common Mistakes

**❌ Hardcoding `--base develop` in `gh stack init`**
The base should reflect your current context. Only pass `--base` when you explicitly need a non-default trunk.

**❌ Mixing unrelated concerns in one stack**
If the changes aren't dependent on each other, they don't belong in the same stack. Start a new stack.

**❌ Manual `git push --force` on stack branches**
Always use `gh stack push` — it uses `--force-with-lease` per branch and won't silently overwrite remote changes.

**❌ Forgetting to `gh stack rebase` after trunk moves**
GitHub requires fully linear history to merge. If trunk moved, rebase before submitting.

## Verification

- [ ] `gh stack view` shows all expected layers in order
- [ ] Each PR targets the branch of the layer below it (not `develop` directly for mid-stack PRs)
- [ ] `gh stack view --json` shows `needsRebase: false` for all layers
- [ ] Each PR is independently reviewable (standalone diff, clear title, no unrelated changes)

## Related Skills

- `workflow-worktree-stack` — Use when you want one worktree per layer for parallel/isolated development
- `git-commit` — Use before `gh stack add` to ensure clean, conventional commits per layer
- `workflow-finish` — Use when the stack is fully merged and the branch needs cleanup
