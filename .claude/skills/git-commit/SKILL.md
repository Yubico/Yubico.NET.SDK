---
name: commit
description: Use when committing changes - enforces commit guidelines (NEVER use git add . or git commit -a)
---

# Git Commit Skill

## Overview

Commit changes following project commit guidelines. Ensures only your files are committed with factual, conventional commit messages.

**Core principle:** Only commit files YOU modified. Describe what changed, not why you think it changed.

## Use when

**Use this skill when:**
- User says "commit" or "commit this"
- Finishing a task that produced file changes
- Need to checkpoint work before switching tasks

**Don't use when:**
- No files have been modified
- Changes belong to someone else (leave staged files alone)

## Process

### 1. Check Current State

```bash
git status
```

Review output. Note:
- **Your changes:** Files you created or modified this session
- **Others' changes:** Files already staged that you didn't touch (leave alone)

### 2. Stage Only Your Files

```bash
git add path/to/file1.cs path/to/file2.cs
```

**FORBIDDEN commands:**
| Command | Why Forbidden |
|---------|---------------|
| `git add .` | Adds everything, including unrelated files |
| `git add -A` | Same problem |
| `git add *` | Shell expansion risk |
| `git commit -a` | Commits all tracked changes |

### 3. Write Factual Commit Message

Format: `type(scope): <factual description>`

**Types:**
| Type | Use For |
|------|---------|
| `feat` | New functionality |
| `fix` | Bug fixes |
| `refactor` | Code restructuring (no behavior change) |
| `test` | Adding or fixing tests |
| `docs` | Documentation only |
| `chore` | Build, config, tooling |

**Rules:**
- Describe the **transformation**, not the **philosophy**
- Do NOT invent intent that wasn't stated

| ❌ Hallucinated | ✅ Factual |
|-----------------|------------|
| `refactor: rename for clarity` | `refactor: rename userId to userID` |
| `fix: improve error handling` | `fix: add null check in ProcessUser()` |
| `feat: enhance user experience` | `feat: add loading spinner to submit button` |

### 4. Commit

```bash
git commit -m "type(scope): factual description"
```

For multi-line messages:
```bash
git commit -m "type(scope): summary

- Detail 1
- Detail 2"
```

### 5. Verify

```bash
git --no-pager log -1 --stat
```

Confirm only your files appear in the commit.

## Examples

### Simple Single-File Change

```bash
# You modified .gitignore
git status
git add .gitignore
git commit -m "chore: add codebase_ast/ to gitignore"
```

### Multi-File Feature

```bash
# You created a new skill with docs update
git status
git add .claude/skills/git-commit/SKILL.md CLAUDE.md
git commit -m "feat(skills): add git commit skill

- Enforce commit guidelines automatically
- Document forbidden commands
- Add factual message examples"
```

### When Others Have Staged Files

```bash
$ git status
Changes to be committed:
  modified:   some-other-file.cs    # NOT yours - leave it

Changes not staged for commit:
  modified:   your-file.cs          # Yours - add this

# Only add YOUR file
git add your-file.cs
git commit -m "fix: correct null handling in your-file.cs"
```

## Common Mistakes

**❌ Inventing intent:** "refactor for better readability" when no one asked for readability
**✅ State the change:** "refactor: extract method ProcessUser from HandleRequest"

**❌ Using `git add .`:** Commits everything including unrelated changes
**✅ Explicit paths:** `git add file1.cs file2.cs`

**❌ Vague messages:** "fix bug" or "update code"
**✅ Specific messages:** "fix: prevent null reference in DeviceChannel.SendAsync"

## Verification

Before completing:

- [ ] Only YOUR files are staged (check `git status`)
- [ ] Commit message starts with valid type (`feat`, `fix`, `refactor`, `test`, `docs`, `chore`)
- [ ] Message describes WHAT changed, not invented WHY
- [ ] No forbidden commands used (`git add .`, `git add -A`, `git commit -a`)

## Reference

See `docs/COMMIT_GUIDELINES.md` for full guidelines.
