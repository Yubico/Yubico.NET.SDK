---
name: jira-issue-update
description: Update an existing Jira issue. Edit content (title/description), change status, change parent, add comments, or reassign.
---

# Jira Issue Update

A comprehensive tool to modify existing Jira tickets. Use this to refine issue details, move tickets through the workflow, organize hierarchies (Epics/Subtasks), or add progress notes.

## Prerequisites
- `JIRA_DOMAIN`
- `JIRA_EMAIL`
- `JIRA_TOKEN`

**IMPORTANT:** Assume these environment variables are already set in the environment variables.

## Usage

```bash
bun run .claude/skills/jira-issue-update/jira-issue-update.ts \
  --key <ISSUE_KEY> \
  [--summary "<NEW_TITLE>"] \
  [--desc "<NEW_DESCRIPTION>"] \
  [--status "<STATUS>"] \
  [--parent "<PARENT_KEY>"] \
  [--comment "<TEXT>"] \
  [--assignee "<USER>"]

```

## Arguments

| Argument | Description | Example |
| --- | --- | --- |
| `--key` | **Required.** The Jira Issue Key. | `YESDK-123` |
| `--summary` | Overwrite the issue title. | `Fix race condition` |
| `--desc` | Overwrite the description. | `Updated steps...` |
| `--parent` | Link to an Epic or Parent Task. Pass `""` to unlink. | `YESDK-40` |
| `--status` | Move the ticket to a new status. | `In Progress` |
| `--comment` | Add a comment to the history. | `Investigation complete.` |
| `--assignee` | Change the assignee. | `me`, `unassigned` |

## Output & Interpretation

**Success Response:**

```plaintext
[Batch Update] Sending changes for YESDK-123...
✅ Content/Parent updated.
✅ Status changed to: Review
Update Complete! 🔗 https://...

```

**Failure Response:**

```plaintext
Error: Cannot move to "Done". Valid next steps: "In Progress", "Review"

```

## Agent Logic

1. **Workflow Errors:** If you receive a "Cannot move to..." error, **STOP**. Read the "Valid next steps" provided in the error message.
* *Self-Correction:* If you tried to move to "Done" but are only allowed to move to "Review", inform the user or try moving to "Review" first.


2. **Epics & Hierarchies:** **Always** use the `--parent` flag when linking child issues (Stories, Tasks, Bugs) to an Epic.
* *Example:* To add Story `YESDK-105` to Epic `YESDK-40`, run with `--parent YESDK-40`.
* *Unlinking:* To remove a link (orphan a ticket), use `--parent ""`.


3. **Verification:** If updating the description, note that this is a **replacement** operation, not an append. Ensure you have the full text intended for the ticket.

## Valid Status Values

**Do not guess statuses.** Use `jira-project-statuses` to find the exact names (e.g., "Closed" vs "Done").

## Examples

### 1. Link to an Epic & Comment

```bash
bun run .claude/skills/jira-issue-update/jira-issue-update.ts \
  --key YESDK-105 \
  --parent YESDK-40 \
  --comment "Linking this story to the main feature Epic."

```

### 2. Move Status & Assign

```bash
bun run .claude/skills/jira-issue-update/jira-issue-update.ts \
  --key YESDK-123 \
  --status "In Progress" \
  --assignee "me"

```

### 3. Unlink from Parent

```bash
bun run .claude/skills/jira-issue-update/jira-issue-update.ts \
  --key YESDK-105 \
  --parent ""

```