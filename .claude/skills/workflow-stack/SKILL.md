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

**Rebase on a cadence, not at the end.** The cost of this whole section scales with how far trunk drifted. Rebase weekly, or on every merge to trunk that touches the same area, whichever comes first — while the conflict surface is still one PR wide and you still remember why you wrote the code. A stack that absorbs three overlapping trunk PRs at once is a different and much worse job than three small rebases.

#### Resolving a conflict: read intent, not markers

Before resolving a file, find out what the incoming branch was *trying to do* there:

```bash
B=$(git merge-base origin/<trunk> origin/<branch>)
git diff $B origin/<branch> -- <file>     # what the branch actually changed
```

This matters because the conflict markers are not the whole change. Two traps that a markers-only resolution walks straight into:

- **`git checkout --ours/--theirs` takes the whole file.** If the branch also added something elsewhere in that file which auto-merged, taking a side silently deletes it. Only use `--ours`/`--theirs` after confirming the branch's diff for that file contains *nothing* but the conflicted region.
- **The markers can cover one signature while a sibling method auto-merges** carrying the old names. Grep the file for the old identifiers after resolving, not just for `<<<<<<<`.

#### Then sweep for semantic conflicts

Git only conflicts when both sides edited the same lines. It says nothing when trunk **renamed or re-meant** something and your branch still uses the old meaning in a file trunk never touched. Those merge clean and are wrong.

```bash
# 1. what did trunk rename or re-mean since the merge base?
git log --oneline $B..origin/<trunk>
git diff $B origin/<trunk> -- '*.cs' | grep -E '^[-+].*(public|internal).*\(' | less

# 2. grep the WHOLE rebased tree for each old identifier - not just .cs
grep -rn "<oldName>" --include="*.cs" --include="*.md" --include="*.txt" .
```

Include prose, XML doc comments and samples in the sweep. The compiler is a partial safety net at best:

| Drift | Caught by |
|---|---|
| Renamed parameter, positional call | nothing — still compiles |
| Renamed parameter, named argument | compiler (`CS1739`) |
| Two branches consolidating different params under the *same* name | compiler (`CS1503`) **only if the types differ** |
| `<paramref name="old"/>` in a doc comment | **nothing** — `paramref` still resolves, build and tests stay green |

The last row is not hypothetical; it is how doc drift ships. Read the XML docs of every signature you touched.

#### Public API declaration files

If the repo tracks a public API surface (this one does — see `docs/architecture/applet-public-api.md`), those files record **parameter names**, so a rename on trunk invalidates them with zero textual conflict. Follow the reconciliation policy in that document; do not simply regenerate them.

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
| Rebase on a cadence, not once at the end | Conflict cost scales with trunk drift |
| After resolving, sweep for renames trunk made | Clean merges can still be semantically wrong |
| Never `git push --force` manually on stack branches | Use `gh stack push` (uses `--force-with-lease` safely) |
| Changing an owner-decided API shape mid-resolution needs ratification | A code comment is not a decision record; say it in the PR |

## Common Mistakes

**❌ Hardcoding `--base develop` in `gh stack init`**
The base should reflect your current context. Only pass `--base` when you explicitly need a non-default trunk.

**❌ Mixing unrelated concerns in one stack**
If the changes aren't dependent on each other, they don't belong in the same stack. Start a new stack.

**❌ Manual `git push --force` on stack branches**
Always use `gh stack push` — it uses `--force-with-lease` per branch and won't silently overwrite remote changes.

**❌ Forgetting to `gh stack rebase` after trunk moves**
GitHub requires fully linear history to merge. If trunk moved, rebase before submitting.

**❌ Leaving a stack branch checked out in a second worktree**
`gh stack rebase` cascades by checking out each branch in turn, so it aborts with `fatal: '<branch>' is already used by worktree at ...` and restores every branch. One worktree per stack, not one per layer — remove the others first (`git worktree remove <path>`) and re-add them afterwards if you want them.

**❌ Trusting `rerere` across a semantically fixed-up rebase**
`gh stack init` enables `git rerere`, which records the resolution you staged — *not* the follow-up fixes you made afterwards when the build failed. Re-running a similar rebase replays the incomplete resolution with no conflict prompt. Clear it once you have fixed things up beyond what you staged:
```bash
rm -rf "$(git rev-parse --git-common-dir)/rr-cache"
```
Use `--git-common-dir`, not `--git-dir`. In a worktree `.git` is a *file* and `--git-dir` points at the worktree's private directory, so `rm -rf .git/rr-cache` silently does nothing and a check against `--git-dir` reports zero entries whether or not the real cache is still there.

**❌ Reporting a scoped `dotnet format` run as verified without a control**
`--include` takes a **space-separated** list. Passing several paths as one quoted string joined by `;` or `,` matches zero documents and exits `0`, which is indistinguishable from clean. Prove the check can fail: add a trailing space to one of your own files, confirm it reports `error WHITESPACE`, revert. See `CLAUDE.md` for the full caveat.

## Verification

- [ ] `gh stack view` shows all expected layers in order
- [ ] Each PR targets the branch of the layer below it (not `develop` directly for mid-stack PRs)
- [ ] `gh stack view --json` shows `needsRebase: false` for all layers
- [ ] Each PR is independently reviewable (standalone diff, clear title, no unrelated changes)
- [ ] After a conflicted rebase: swept for trunk's renames across `.cs`, `.md` and XML doc comments
- [ ] After a conflicted rebase: every public API declaration delta is explained by a known trunk change

## Related Skills

- `workflow-worktree-stack` — Use when you want one worktree per layer for parallel/isolated development
- `git-commit` — Use before `gh stack add` to ensure clean, conventional commits per layer
- `workflow-finish` — Use when the stack is fully merged and the branch needs cleanup
