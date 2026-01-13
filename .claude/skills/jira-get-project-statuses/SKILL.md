---
name: jira-project-statuses
description: Get valid statuses for a project, grouped by their category (To Do, In Progress, Done).
---

# Jira Get Project Statuses

Queries the Jira API to find the workflow statuses for a project. Returns the status name and its semantic category.

## Prerequisites

**IMPORTANT: Assume these environment variables are already set in the environment.**
If the command fails with an authentication error, ask the user to verify:
- `JIRA_DOMAIN`
- `JIRA_EMAIL`
- `JIRA_TOKEN`

## Usage

```bash
bun run .claude/skills/jira-get-project-statuses/jira-get-project-statuses.ts --project <PROJECT_KEY>
```
## Output & Interpretation
The tool returns a JSON object containing a list of statuses. Crucial: You must use the category field to understand the meaning of a status, but use the name field when executing commands.

### Example Response:

```json
{
  "project": "YESDK",
  "statuses": [
    { "name": "Todo", "category": "To Do" },
    { "name": "Review", "category": "In Progress" },
    { "name": "Closed", "category": "Done" }
  ]
}
```

## Agent Logic
- If the user asks for "Open tickets": Look for statuses where category is "To Do" or "In Progress".
- If the user asks for "Finished work": Look for statuses where category is "Done".

**Command Execution: **When calling jira-issue-search or jira-issue-update, ALWAYS use the exact string from the name field (e.g., "Review"), not the category.

### Example Scenario
1. User: "Find active bugs."
2. Call jira-get-project-statuses.
3. Identify active statuses (e.g., Todo, Review).
4. Call jira-issue-search --status "Todo, Review".