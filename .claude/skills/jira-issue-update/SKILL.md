---
name: jira-issue-update
description: Update an existing Jira issue. Edit content (title/description), change status, add comments, or reassign.
---

# Jira Issue Update

A comprehensive tool to modify existing Jira tickets. Use this to refine issue details, move tickets through the workflow, or add progress notes.

## Prerequisites
- `JIRA_DOMAIN`
- `JIRA_EMAIL`
- `JIRA_TOKEN`

IMPORTANT: Assume these environment variables are already set in the environment variables.

## Usage

```bash
bun run .claude/skills/jira-issue-update/jira-issue-update.ts \
  --key <ISSUE_KEY> \
  [--summary "<NEW_TITLE>"] \
  [--desc "<NEW_DESCRIPTION>"] \
  [--status "<STATUS>"] \
  [--comment "<TEXT>"] \
  [--assignee "<USER>"]

```

### Output & Interpretation
**Success Response:**

```plaintext
[Batch Update] Sending changes for YESDK-123...
✅ Content & Comment updated.
✅ Status changed to: Review
Update Complete! 🔗 https://...
Failure Response:
```

```plaintext
Error: Cannot move to "Done". Valid next steps: "In Progress", "Review"
```
### Agent Logic
- Workflow Errors: If you receive a "Cannot move to..." error, STOP. Read the "Valid next steps" provided in the error message.
- Self-Correction: If you tried to move to "Done" but are only allowed to move to "Review", inform the user or try moving to "Review" first.
- Verification: If updating the description, note that this is a replacement operation, not an append. Ensure you have the full text intended for the ticket.

### Valid Status Values
Do not guess statuses. Use jira-project-statuses to find the exact names (e.g., "Closed" vs "Done").