This generic version removes all project-specific file extensions and build systems, making it suitable for any software project. I have also added a "System Prompt" block at the end that you can copy-paste directly into the instructions for other AI agents.

---

# Git Commit Guidelines for Agents

**CRITICAL:** These guidelines apply to ALL agents and automated workflows that make commits.

## Core Rule

**Only commit files YOU created or modified in the current session.** Do not include files that were already staged by someone else.

## 1. Commit Message Honesty (NO Hallucinated Intent)

**Do NOT invent the "why" behind a change.** If you were not explicitly given a reason for a change (e.g., "for clarity"), describe only **what** was done.

* **🚫 FORBIDDEN:** `refactor(api): rename variable X for better readability` (if "readability" wasn't requested).
* **✅ REQUIRED:** `refactor(api): rename variable X to Y`
* **The Rule:** Describe the *transformation*, not the *philosophy*.

## 2. Safe Commit Pattern

Always follow these steps to ensure you are only committing your own work:

```bash
# Step 1: Check what's already staged
git status

# Step 2: Add ONLY your modified/new files explicitly
git add path/to/your/file.ext

# Step 3: Commit only what you added
git commit -m "type(scope): factual description"

```

## 3. Forbidden Commands

| Command | Why it is Forbidden |
| --- | --- |
| `git add .` / `git add -A` | Adds everything, including unrelated or unwanted files. |
| `git commit -a` | Commits all tracked changes, potentially including others' work. |
| `git add *` | Risk of including files via shell expansion. |

## 4. Handling Existing Staged Changes

If `git status` shows files already staged that you did not modify:

1. **Leave them alone** (do not unstage them).
2. **Do NOT commit them.**
3. **Only `git add` your specific files.**

## 5. Commit Message Format

Use Conventional Commits. Keep descriptions **objective and factual**.

`type(scope): <description>`

* **Types:** `feat`, `fix`, `docs`, `test`, `refactor`, `chore`
* **Fact-Based Examples:**
* `refactor(auth): rename login() to authenticate()`
* `fix(ui): change button color from blue to red`
* `feat(db): add 'last_login' column to users table`



---