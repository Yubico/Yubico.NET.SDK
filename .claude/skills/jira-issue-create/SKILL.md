---
name: jira-issue-create
description: Create Jira issues via the REST API. Use when you need to create bugs, tasks, stories, or epics in Jira.
---

# Jira Issue Create

Creates issues in Jira Cloud using the REST API v3.

## Prerequisites

Set these environment variables:
- `JIRA_DOMAIN` - Your Jira domain (e.g., `mycompany.atlassian.net`)
- `JIRA_EMAIL` - Your Atlassian account email
- `JIRA_TOKEN` - API token from https://id.atlassian.com/manage-profile/security/api-tokens

IMPORTANT: Assume these environment variables are already set in the environment variables.

## Usage

```bash
bun run .claude/skills/jira-issue-create/jira-issue-create.ts \
  --project <KEY> \
  --summary "<TITLE>" \
  [--type <TYPE>] \
  [--desc "<DESCRIPTION>"] \
  [--labels "<LABEL1,LABEL2>"] \
  [--parent "<PARENT-KEY>"]
```

## Arguments

| Argument | Required | Default | Description |
|----------|----------|---------|-------------|
| `--project` | Yes | - | Project key (e.g., `SDK`, `YUBIKIT`) |
| `--summary` | Yes | - | Issue title/summary |
| `--type` | No | `Task` | Issue type: `Bug`, `Story`, `Epic`, `Task`, `Sub-task` |
| `--desc` | No | - | Issue description (plain text) |
| `--labels` | No | - | Comma-separated labels (always includes `jira-agent-skill-automation`) |
| `--parent` | No | - | Parent issue key (e.g., `SDK-123`) - required for subtasks |

## Examples

### Create a Task
```bash
bun run .claude/skills/jira-issue-create/jira-issue-create.ts \
  --project SDK \
  --summary "Implement SCP03 key derivation"
```

### Create a Bug with Description
```bash
bun run .claude/skills/jira-issue-create/jira-issue-create.ts \
  --project SDK \
  --type Bug \
  --summary "APDU chaining fails on firmware < 4.0" \
  --desc "When sending chained APDUs to devices with firmware below 4.0, the response is truncated."
```

### Create a Story
```bash
bun run .claude/skills/jira-issue-create/jira-issue-create.ts \
  --project SDK \
  --type Story \
  --summary "As a developer, I want to connect via HID"
```

### Create with Custom Labels
```bash
bun run .claude/skills/jira-issue-create/jira-issue-create.ts \
  --project SDK \
  --summary "Refactor SCP03 implementation" \
  --labels "refactoring,security"
```

### Create a Subtask
```bash
bun run .claude/skills/jira-issue-create/jira-issue-create.ts \
  --project SDK \
  --type Sub-task \
  --parent SDK-123 \
  --summary "Implement key derivation function"
```

**Note:** All issues automatically include the `jira-agent-skill-automation` label for easy filtering.

## Agent Usage

When creating issues programmatically:

1. Determine the appropriate project key
2. Choose issue type based on the work:
   - `Bug` - Defects, broken functionality
   - `Task` - Technical work, chores
   - `Story` - User-facing features
   - `Epic` - Large initiatives (multiple stories)
   - `Sub-task` - Child task of a parent issue (requires `--parent`)
3. Write a clear, actionable summary
4. Include relevant context in the description
5. For subtasks, always specify the parent issue key

## Output

On success:
```
[Drafting Issue] Project: SDK | Type: Task | Summary: "Implement SCP03 key derivation"
Success! Issue Created: SDK-123
Link: https://mycompany.atlassian.net/browse/SDK-123
```

On failure, the script outputs the Jira API error response for debugging.

## References

- [Jira REST API v3 - Create Issue](https://developer.atlassian.com/cloud/jira/platform/rest/v3/api-group-issues/#api-rest-api-3-issue-post)
- [Atlassian Document Format (ADF)](https://developer.atlassian.com/cloud/jira/platform/apis/document/structure/)
