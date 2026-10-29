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

### 1. Decide the trunk — and ALWAYS pass it as `--base`

The trunk is the branch the bottom PR targets. This repository holds two SDKs with **unrelated git histories**:

| Line | Trunk | How to recognise it |
|------|-------|---------------------|
| **v2** (YubiKit rewrite, `toolchain.cs`, `src/<Module>/`) | **`yubikit`**, or the `yubikit-*` branch the work was cut from | shares history with `origin/yubikit` |
| **v1** (legacy `Yubico.YubiKey` SDK) | **`develop`** (`main` is v1 releases) | shares history with `origin/develop` |

v2 lives only on `yubikit` and `yubikit-*` branches. It is expected to move to `main` one day; until then, v2 work never targets `develop` or `main`.

`develop` is the repository's **default branch**, and `gh stack init`, `gh stack link` and `gh stack submit` fall back to it when no `--base` is given. For v2 work that is always wrong. A v2 PR retargeted to `develop` has no common history with it, so **GitHub closes the PR immediately and refuses to reopen it or change its base back**. The only recovery is a new PR.

Because the histories are disjoint, git answers "which line am I on?" exactly. No need to ask the user:

```bash
git fetch -q origin
if git merge-base HEAD origin/yubikit >/dev/null 2>&1; then TRUNK=yubikit     # v2
elif git merge-base HEAD origin/develop >/dev/null 2>&1; then TRUNK=develop   # v1
else echo "HEAD shares history with neither yubikit nor develop - stop and ask"; fi
echo "trunk: $TRUNK"
```

Then pass it on **every** command that creates or links a stack:

```bash
gh stack init --base "$TRUNK" <branches...>
gh stack link --base "$TRUNK" <prs-or-branches...>
```

Exceptions, both deliberate:
- **Stacking on someone else's open PR:** the trunk is that PR's head branch, not `yubikit`/`develop`.
- **A `yubikit-*` integration branch** (e.g. `yubikit-applets`, which shares history with `yubikit`): the history check cannot tell them apart. Use the branch the work was cut from (`git log --oneline origin/yubikit-applets..HEAD` is short when it was), or the one the user named.

> ⚠️ Never let a `gh stack` command default its base. If you see `→ develop` (or "Updated base branch ... to develop") on v2 work, stop: that PR is about to be closed.

### 2. Initialise the stack

```bash
# Interactive — prompts for first branch name
gh stack init --base "$TRUNK"

# Non-interactive — name the first layer upfront
gh stack init --base "$TRUNK" <layer-1-branch>

# Adopt branches (and their open PRs) that already exist, bottom to top
gh stack init --base "$TRUNK" <layer-1> <layer-2> <layer-3>
gh stack link --base "$TRUNK" <pr-1> <pr-2> <pr-3>   # link existing PRs into a GitHub stack
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
| Always pass `--base <trunk>` to `gh stack init` / `gh stack link` (v2: `yubikit`, v1: `develop`) | The default is `develop`; a v2 PR retargeted there is closed and cannot be reopened |
| Unrelated work → new stack | Keeps reviewer scope narrow |
| Each layer independently understandable | Reviewer can approve without reading the whole stack |
| `gh stack rebase` before adding new layers | Keeps history linear |
| Never `git push --force` manually on stack branches | Use `gh stack push` (uses `--force-with-lease` safely) |

## Common Mistakes

**❌ Letting `gh stack` default to `develop` on v2 work**
`gh stack link 101 102` (no `--base`) retargets the bottom v2 PR to `develop`. GitHub closes it because the histories are unrelated, and it cannot be reopened. Always `--base yubikit` for v2.

**❌ Hardcoding a trunk instead of detecting it**
`--base develop` on v2 work closes the PR; `--base yubikit` on v1 work fails the same way in reverse. Detect the trunk (step 1) and pass that.

**❌ Mixing unrelated concerns in one stack**
If the changes aren't dependent on each other, they don't belong in the same stack. Start a new stack.

**❌ Manual `git push --force` on stack branches**
Always use `gh stack push` — it uses `--force-with-lease` per branch and won't silently overwrite remote changes.

**❌ Forgetting to `gh stack rebase` after trunk moves**
GitHub requires fully linear history to merge. If trunk moved, rebase before submitting.

## Verification

- [ ] `gh stack view` shows the trunk you detected in step 1 at the bottom (`yubikit` for v2, `develop` for v1)
- [ ] `gh stack view` shows all expected layers in order
- [ ] The bottom PR targets the trunk; each other PR targets the branch of the layer below it
- [ ] `gh stack view --json` shows `needsRebase: false` for all layers
- [ ] Each PR is independently reviewable (standalone diff, clear title, no unrelated changes)

## Related Skills

- `workflow-worktree-stack` — Use when you want one worktree per layer for parallel/isolated development
- `git-commit` — Use before `gh stack add` to ensure clean, conventional commits per layer
- `workflow-finish` — Use when the stack is fully merged and the branch needs cleanup
