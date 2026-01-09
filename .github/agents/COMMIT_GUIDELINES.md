# Git Commit Guidelines for Agents

**CRITICAL:** These guidelines apply to ALL agents and automated workflows that make commits.

## Core Rule

**Only commit files YOU created or modified in the current session.**

## Safe Commit Pattern

```bash
# Step 1: ALWAYS check what's staged before committing
git status

# Step 2: Only add YOUR files explicitly
git add path/to/your/new/file.cs
git add path/to/your/modified/file.cs

# Step 3: Commit only what you added
git commit -m "feat(scope): description"
```

## Forbidden Commands

| Command | Problem |
|---------|---------|
| `git add .` | May include unrelated staged files |
| `git add -A` | Same problem - adds everything |
| `git commit -a` | Commits all tracked changes, including others' work |
| `git add *` | Shell expansion may include unwanted files |

## What to Do If Files Are Already Staged

If `git status` shows files staged that you didn't modify:

1. **Leave them alone** - do NOT unstage them
2. **Do NOT commit them** - they belong to another task
3. **Only `git add` your own files** explicitly
4. **Commit only your additions**

```bash
# Example: files were already staged by someone else
$ git status
Changes to be committed:
  modified:   some-other-file.cs    # <-- NOT yours, leave it!

Changes not staged for commit:
  new file:   your-new-file.cs      # <-- Yours, add this

# Correct approach:
git add your-new-file.cs
git commit -m "feat(hid): add your feature"
# The other file remains staged but uncommitted
```

## Commit Message Format

Follow conventional commits:

```
<type>(<scope>): <description>

[optional body]
```

**Types:** `feat`, `fix`, `docs`, `test`, `refactor`, `chore`

**Examples:**
```bash
git commit -m "feat(hid): add MacOSHidDevice implementation"
git commit -m "fix(piv): correct key derivation for RSA"
git commit -m "test(fido): add attestation verification tests"
git commit -m "docs(readme): update build instructions"
```

## Pre-Commit Checklist

Before every commit:

- [ ] Ran `git status` to see what's staged
- [ ] Only adding files I created or modified
- [ ] Not using `git add .` or `git add -A`
- [ ] Not committing someone else's staged changes
- [ ] Commit message follows conventional format

## Integration with Build Verification

Typical workflow:

```bash
# 1. Make changes
# 2. Build and test
dotnet build.cs build
dotnet build.cs test

# 3. If successful, commit YOUR changes only
git status                           # Check what's staged
git add path/to/your/files.cs       # Add only your files
git commit -m "feat(scope): description"
```

## Why This Matters

- **Avoids accidental commits** of unrelated work
- **Preserves others' staged changes** for their own commits
- **Keeps git history clean** with focused, atomic commits
- **Prevents merge conflicts** from committing unexpected files
- **Makes code review easier** with clear, scoped changes
