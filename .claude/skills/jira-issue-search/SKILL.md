---
name: jira-issue-search
description: Search for Jira issues using structured filters. Use this to find active work or discover project status names.
---

# Jira Issue Search

A strict search tool that builds valid JQL from your inputs.

## Prerequisites
- `JIRA_DOMAIN`
- `JIRA_EMAIL`
- `JIRA_TOKEN`

## Usage

```bash
bun run .claude/skills/jira-issue-search/jira-issue-search.ts \
  [--project <KEY>] \
  [--key <ISSUE_KEY[,ISSUE_KEY...]>] \
  [--status "<STATUS>"] \
  [--exclude-status "<STATUS>"] \
  [--assignee "<USER>"] \
  [--text "<KEYWORDS>"] \
  [--limit <NUM>] \
  [--token <NEXT_PAGE_TOKEN>]

```

## Output & Interpretation
The output is a JSON object with two top-level keys: meta and issues.

### Example Response:


```json

{
  "meta": {
    "count": 10,
    "next_token": "CnZw..." 
  },
  "issues": [
    {
      "key": "YESDK-101",
      "summary": "Crash on startup",
      "status": "Review",
      "description": "...",
      "comments": [...]
    }
  ]
}
```

### Agent Logic
- Pagination: Check meta.next_token. If it is not null, there are more results. Ask the user if they want to see the next page. If yes, run the exact same command again, adding --token "CnZw...".

- Context Loading: The description and comments are already flattened to plain text. Read these to understand the task before generating code.

- Status Check: Verify the status field of the returned issues. If you expected "In Progress" but see "Review", adjust your mental model of the ticket's state.

### Discovery Workflow
If you don't know the status names, run:

```bash

bun run .claude/skills/jira-issue-search/jira-issue-search.ts --project <KEY> --exclude-status "Closed"
```