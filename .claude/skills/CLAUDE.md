# CLAUDE.md - Skills Directory

This file provides guidance for working in the `.claude/skills/` directory.
For overall repo conventions, see the repository root [CLAUDE.md](../../CLAUDE.md).

## Documentation Maintenance

When creating or modifying skills, update this file if adding new categories or significant patterns.

## Directory Purpose

Skills are reusable workflows invoked via `/skillname` in Claude Code or through the `skill` tool in Copilot CLI. Each skill provides step-by-step guidance for a specific task.

## Directory Structure

```
.claude/skills/
├── CLAUDE.md                     # This file
├── {category}-{topic}/           # Skill directory (kebab-case)
│   ├── SKILL.md                  # Skill definition (required)
│   └── helper.ts/cs              # Supporting scripts (optional)
```

## Category Prefixes

| Category | Purpose | Examples |
|----------|---------|----------|
| `workflow-` | Multi-step development processes | `workflow-tdd`, `workflow-debug`, `workflow-verify` |
| `tool-` | Wrapper around specific tools | `tool-import-skill`, `tool-jira-*` |
| `docs-` | Documentation creation | `docs-module`, `docs-write-skill` |
| `agent-` | Agent orchestration patterns | `agent-dispatch`, `agent-ralph-loop` |
| `domain-` | Domain-specific knowledge | `domain-yubikit-compare`, `domain-build`, `domain-test` |
| `jira-` | Jira operations | `jira-create`, `jira-search`, `jira-update` |
| `review-` | Code review workflows | `review-request`, `review-receive` |

## Creating New Skills

**MANDATORY: Use the `write-skill` skill before creating any files manually.**

```
/write-skill
```

This ensures proper YAML frontmatter, structure, and registration.

## Required SKILL.md Structure

```markdown
---
name: verb-object-form           # e.g., tdd, build-project, dispatch-agents
description: Use when [trigger] - [what it does]
---

# Skill Title

## Overview

Brief description (1-2 sentences).

**Core principle:** [Guiding philosophy in one sentence]

## Use when

**Use when:**
- Trigger condition 1
- Trigger condition 2

**Don't use when:**
- Exception 1
- Exception 2

## [Main Process Section]

Name this section for the domain (e.g., "The Pattern", "Core Command").

1. **Step Name**
   Description of what to do.

## Verification

- [ ] Checklist item 1
- [ ] Checklist item 2
```

## Key Requirements

1. **YAML Frontmatter** - `name` (verb-object) and `description` (starts with "Use when")
2. **Overview Section** - With `**Core principle:**` statement
3. **Use when Section** - Lowercase "when" (Claude uses this to decide invocation)
4. **Verification Section** - Checklist or success criteria
5. **Concise** - AI tools have context limits; every word earns its place

## Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Directory | `{category}-{topic}` kebab-case | `workflow-tdd/` |
| Frontmatter name | verb-object form | `tdd`, `dispatch-agents` |
| File | Always `SKILL.md` | `SKILL.md` |

## Existing Skills Reference

| Skill | Purpose |
|-------|---------|
| `build-project` | **REQUIRED** for building .NET code |
| `test-project` | **REQUIRED** for running tests |
| `tdd` | Test-driven development workflow |
| `debug` | Systematic debugging process |
| `verify` | Verify work before claiming done |
| `write-plan` | Create implementation plans |
| `dispatch-agents` | Parallel agent orchestration |
| `ralph-loop` | Autonomous agent loops |
| `write-skill` | **REQUIRED** before creating skill files |
| `write-agent` | **REQUIRED** before creating agent files |
| `yubikit-compare` | Compare Java and C# YubiKit implementations |
| `experiment` | Create standalone experiment scripts |
| `interface-refactor` | Refactor classes to use interfaces |

## Common Mistakes

**❌ Duplicate root CLAUDE.md content** - Reference instead
**✅ Link to root:** See [Memory Management](../../CLAUDE.md#memory-management-hierarchy)

**❌ Abstract rules only** - Forgettable
**✅ Concrete examples:** Show code snippets for what to do

**❌ Verbose prose** - Context limits
**✅ Use tables, bullets, code blocks:** Structure for scanning

**❌ Creating skills manually**
**✅ Use `/write-skill`:** Ensures proper structure and registration

## Verification Checklist

Before committing a new or modified skill:

- [ ] YAML frontmatter has `name` and `description`
- [ ] `description` starts with "Use when"
- [ ] Has `## Overview` with **Core principle**
- [ ] Has `## Use when` section (lowercase)
- [ ] Has `## Verification` section
- [ ] No duplicate content from other docs
- [ ] Code examples follow codebase conventions
- [ ] Directory follows `{category}-{topic}` naming

## Related Documentation

- [AI-DOCS-GUIDE.md](../../docs/AI-DOCS-GUIDE.md) - Full documentation standards
- [Root CLAUDE.md](../../CLAUDE.md) - Primary codebase context
- `.github/copilot-instructions.md` - Copilot CLI entry point
