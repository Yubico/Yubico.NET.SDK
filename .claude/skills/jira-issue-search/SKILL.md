---
name: jira-issue-search
description: Search for Jira issues using safe filters. Supports cursor-based pagination.
---

# Jira Issue Search

Search and retrieve issues from Jira Cloud. Uses the new `search/jql` endpoint for performance.

## Prerequisites
- `JIRA_DOMAIN`
- `JIRA_EMAIL`
- `JIRA_TOKEN`

## Usage

```bash
bun run .claude/skills/jira-issue-search/jira-issue-search.ts \
  [--project <KEY>] \
  [--status "<STATUS>"] \
  [--assignee "<USER>"] \
  [--text "<KEYWORDS>"] \
  [--limit <NUM>] \
  [--token <NEXT_PAGE_TOKEN>]