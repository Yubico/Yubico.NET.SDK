---
name: todo-to-jira
description: Use when converting TODO comments to Jira issues from files you just worked with
---

# TODO → Jira (Working Set)

## Overview
Turn inline TODO/FIXME/HACK comments into actionable Jira issues, scoped to the **current working set** (the files we just modified) or an explicitly provided directory/diff range. This is meant for the common flow: during a coding conversation, the agent adds TODOs, and the user says “make Jira issues for those TODOs”.

## Use when
- You and the agent just added TODOs and you want Jira tracking items for them.
- The user says: “Create Jira issues for the TODOs in the files we just worked with.”
- The user says: “Create Jira issues for TODOs in this directory.”

## Prerequisites
- Jira environment variables are configured (same as `jira-issue-create`).
- Repository is a git repo for best results (diff-scoped “new TODOs” mode).

## Inputs (Ask Only If Missing)
- **Jira project key** (required): e.g. `SDK`, `YUBIKIT`
- Optional:
  - default issue type (default: `Task`)
  - parent issue key (only if user wants subtasks)

## Scope Rules (Critical)
Default scope is **working set** only — do NOT grep the entire repository unless explicitly requested.

Supported scopes:
- `working-set` (default): files in `git status --porcelain`
- `dir:<path>`: files under a directory (exclude `bin/`, `obj/`)
- `diff:<base>..<head>`: only within files changed in a diff range
- `last-commit`: files touched in `HEAD`

## Discovery Rules
Two modes:
- `new-only` (default): only TODO-like lines newly introduced in the diff (added lines)
- `all-in-scope`: find TODO-like lines anywhere inside scoped files

Match tokens:
- `TODO`, `FIXME`, `HACK` (case-sensitive match is fine; keep it simple)

## Jira Issue Content (Per TODO)
Each created issue MUST include:
- **Summary**: `TODO(<module>): <short normalized text>`
- **Location**: `<file>:<line>` (and range if applicable)
- **Exact TODO text**
- **Code snippet**: ~10 lines around the TODO
- **Blame** (if git): author, date, commit SHA (`git blame -L line,line`)
- **Acceptance Criteria**: 2–5 checkboxes inferred from TODO; if unclear, ask ONE clarifying question for that TODO.

## Labels
- Always include: `jira-agent-skill-automation`, `todo`, `autocreated`
- Also add inferred labels when confident:
  - `module:<name>` (from path segment like `Yubico.YubiKit.SecurityDomain`)
  - `area:tests`, `area:docs`, `area:src` (based on path)

## Issue Type Heuristic
- `FIXME` → `Bug`
- `TODO` / `HACK` → default issue type (`Task`)

## Workflow
1) Determine scope (default `working-set`).
2) Discover TODOs in scope (default `new-only`).
3) Show preview list (file:line + proposed summary) and ask for confirmation.
4) Create issues via skill `jira-issue-create`.
5) Print a mapping: `file:line -> JIRA-KEY` and links.

## Commands (Implementation Hints)
- Working set files:
  - `git status --porcelain=v1`
- TODOs introduced in diff:
  - `git diff -U0 -- <files> | rg -n '^\\+.*\\b(TODO|FIXME|HACK)\\b'`
- TODOs in files:
  - `rg -n '\\b(TODO|FIXME|HACK)\\b' -- <files>`
- Blame:
  - `git blame -L <line>,<line> <file>`

## Exit Criteria
- If zero TODOs found: report “none found in scope” and offer alternate scope (`all-in-scope` or `dir:<path>`).
- If preview accepted: Jira issues created and keys returned.
