---
name: jira-delete
description: Use when permanently removing a Jira issue - irreversible action
---

# Jira Issue Delete

## Use when

- Need to permanently delete a Jira issue
- Cleaning up duplicate or erroneous issues
- Removing test issues created by automation

**Warning:** This action is irreversible.

Permanently removes an issue from Jira.

## Prerequisites
**IMPORTANT: Assume `JIRA_DOMAIN`, `JIRA_EMAIL`, and `JIRA_TOKEN` are set.**
If auth fails, ask the user to verify them.

## Usage

```bash
bun run .claude/skills/jira-issue-delete/jira-issue-delete.ts --key <KEY> [--recursive]
```

## Arguments

Argument,Required,Description,Example
--key,Yes,The issue key to delete.,YESDK-123
--recursive,No,Required if the issue has subtasks. Deletes the parent and all children.,(Flag only)

## Output & Interpretation

**Success Response:**

```Plaintext
[Delete] Attempting to delete YESDK-123...
✅ Success! Issue YESDK-123 has been deleted.


**Failure Response (Subtasks exist):**
```Plaintext
Error: This issue has subtasks. You must use the --recursive flag to delete it.
```

## Agent Logic & Safety
Verification: Before deleting, consider searching for the issue first (jira-issue-search) to confirm it is the correct ticket (e.g., check the summary matches the user's intent).Subtasks: If you receive a "subtasks" error, inform the user: "The issue has subtasks. Should I delete all of them?" Do not run --recursive automatically without confirmation unless explicitly instructed to "force delete".Irreversible: There is no "Undo". Be precise with the Key.ExamplesStandard DeleteBashbun run .claude/skills/jira-issue-delete/jira-issue-delete.ts --key YESDK-123
Force Delete (Parent + Subtasks)Bashbun run .claude/skills/jira-issue-delete/jira-issue-delete.ts --key YESDK-123 --recursive

### 3. Final Verification

To verify this works without destroying a real ticket, create a dummy one first:

```bash
# 1. Create dummy
bun run .claude/skills/jira-issue-create/jira-issue-create.ts --project YESDK --summary "Delete Me Test"

# 2. (Optional) Check ID from output, e.g., YESDK-999

# 3. Delete it
bun run .claude/skills/jira-issue-delete/jira-issue-delete.ts --key YESDK-999