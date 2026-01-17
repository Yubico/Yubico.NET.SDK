# AI Documentation Guide

This document standardizes how to write AI helper files for this repository. It ensures consistent structure across skills, agents, and context files so they work effectively with Claude Code, GitHub Copilot, and other AI-powered development tools.

## Overview

| File Type | Location | Purpose |
|-----------|----------|---------|
| Skills | `.claude/skills/<name>/SKILL.md` | Reusable workflows invoked via `/skillname` |
| Agents | `.github/agents/<name>.agent.md` | Specialized personas for complex tasks |
| Root CLAUDE.md | `./CLAUDE.md` | Primary codebase context for all tools |
| Submodule CLAUDE.md | `<module>/CLAUDE.md` | Module-specific context |
| Copilot Instructions | `.github/copilot-instructions.md` | GitHub Copilot entry point |

## Dependency Direction

**Context flows downstream, not upstream.**

```
docs/*                  ← Canonical documentation (humans + AI)
    ↓
CLAUDE.md (root)        ← Primary AI context, references docs/*
    ↓
<module>/CLAUDE.md      ← Module-specific AI context, references root
    ↓
.github/copilot-instructions.md    ← Entry point for Copilot
.junie/guidelines.md               ← Entry point for Junie
.agent/rules/                      ← Entry point for Cursor
```

**Rules:**
- `CLAUDE.md` MAY reference `docs/*` for canonical information
- `CLAUDE.md` MUST NOT reference tool-specific files (copilot-instructions, .junie, etc.)
- Tool-specific files SHOULD reference `CLAUDE.md` as source of truth
- Skills and agents MAY reference any documentation

## File Types

### 1. Skills (`.claude/skills/<name>/SKILL.md`)

Skills are reusable workflows that Claude Code invokes with `/skillname`. They provide step-by-step guidance for specific tasks.

**When to create a skill:**
- Task is performed repeatedly (>3 times)
- Task has a defined process with clear steps
- Task benefits from consistency across invocations
- Task is invoked explicitly by the user (e.g., `/commit`, `/debug`)

**Directory structure:**
```
.claude/skills/
├── {category}-{topic}/       # kebab-case with category prefix
│   └── SKILL.md              # Skill definition (required)
├── workflow-tdd/
│   └── SKILL.md
├── tool-build/
│   ├── SKILL.md
│   └── helper.ts             # Supporting script (optional)
```

**Category prefixes:**

| Category | Purpose | Example |
|----------|---------|---------|
| `workflow-` | Multi-step development processes | `workflow-tdd`, `workflow-debug` |
| `tool-` | Wrapper around specific tools | `tool-build`, `tool-import-skill` |
| `docs-` | Documentation creation | `docs-module`, `docs-write-skill` |
| `agent-` | Agent orchestration patterns | `agent-dispatch`, `agent-ralph-loop` |
| `domain-` | Domain-specific knowledge | `domain-yubikit-compare` |
| `jira-` | Jira operations | `jira-create`, `jira-search` |
| `review-` | Code review workflows | `review-request`, `review-receive` |

**Template:**
```markdown
---
name: verb-object-form
description: Use when [trigger condition] - [what it does]
---

# Skill Title

## Overview

Brief description (1-2 sentences).

**Core principle:** [The guiding philosophy in one sentence]

## Use when

**Use this skill when:**
- Trigger condition 1
- Trigger condition 2

**Don't use when:**
- Exception 1
- Exception 2

## [Main Process Section]

Name this section for the domain (e.g., "The Pattern", "Red-Green-Refactor Cycle", "Core Command").

1. **Step Name**
   Description of what to do.

2. **Step Name**
   Description of what to do.

## Common Mistakes (optional)

**❌ Bad:** Description
**✅ Good:** Description

## Verification

How to confirm the skill completed successfully:
- [ ] Checklist item 1
- [ ] Checklist item 2

## Related Skills (optional)

- `skill-a` - When X
- `skill-b` - When Y
```

**Key requirements:**
- YAML frontmatter with `name` (verb-object form) and `description` (starts with "Use when")
- `## Overview` section with **Core principle**
- `## Use when` section (lowercase "when" - Claude uses this to decide when to invoke)
- `## Verification` section with checklist or success criteria
- Keep descriptions concise; use tables, bullets, code blocks for scannability

### 2. Agents (`.github/agents/<name>.agent.md`)

Agents are specialized personas for complex, multi-faceted tasks. They have broader context and authority than skills.

**When to create an agent:**
- Task requires deep domain expertise
- Task spans multiple tools and files
- Task requires judgment calls and adaptation
- Task would benefit from a "specialist" persona

**Template:**
```markdown
---
name: agent-name
description: What this agent specializes in (1-2 sentences)
tools: ["read", "edit", "search", "terminal"]  # Optional: restrict tools
model: inherit  # Optional: specify model preference
---

# Agent Name

Brief description of the agent's role.

## Purpose

What this agent specializes in (2-3 sentences).

## Use When

**Invoke this agent when:**
- Trigger condition 1
- Trigger condition 2

**DO NOT invoke when:**
- Exception 1
- Exception 2

## Capabilities

- What the agent can do
- What domain knowledge it has
- What patterns it follows

## Process

1. **Phase Name**
   What the agent does in this phase.

2. **Phase Name**
   What the agent does in this phase.

## Output Format

What the agent produces:
- Reports, code, commits, etc.
- Expected format/structure

## Related Resources (optional)

- Links to relevant documentation
- Links to related skills or agents
```

**Key requirements:**
- YAML frontmatter with `name` and `description` (required)
- "Use When" section with clear trigger conditions
- "Capabilities" section describing expertise
- "Process" section with high-level phases (not granular steps)
- Focus on judgment and expertise, not rote procedures

### 3. Root CLAUDE.md (`./CLAUDE.md`)

The primary context file for all AI tools. This is the single source of truth for codebase conventions.

**Required sections:**

```markdown
# CLAUDE.md

## Quick Reference - Critical Rules

[Most important rules in bullet form - memory management, security, patterns]

## Build and Test Commands

[How to build, test, and run the project]

## Architecture

[Core components, key patterns, multi-targeting, platform specifics]

## Performance and Security Best Practices

[Memory management, crypto APIs, sensitive data handling]

## Code Style and Language Features

[EditorConfig compliance, modern C# patterns, what not to do]

## Testing

[Test philosophy, structure, guidelines]

## Git Workflow

[Branch strategy, commit discipline]

## Pre-Commit Checklist

[Final verification before committing]
```

**Key requirements:**
- Quick Reference at top for fast scanning
- Concrete code examples (not just rules)
- Decision trees for complex choices (e.g., memory management)
- "DO" and "DON'T" examples side by side
- Module reference note if submodules exist

### 4. Submodule CLAUDE.md (`<module>/CLAUDE.md`)

Module-specific context that extends the root CLAUDE.md.

**Required sections:**

```markdown
# CLAUDE.md - Module Name

This file provides module-specific guidance for working in **Module.Name**.
For overall repo conventions, see the repository root [CLAUDE.md](../CLAUDE.md).

## Documentation Maintenance

[Note about keeping docs updated with changes]

## Module Context

[What this module does, current state, key files]

## Critical Requirements (if any)

[Security requirements, performance requirements, etc.]

## Test Infrastructure

[Module-specific test patterns, helpers, reset mechanisms]

## Common Patterns

[Patterns specific to this module with code examples]

## Firmware Version Considerations (if applicable)

[Version-dependent features]

## Known Gotchas

[Common pitfalls, edge cases, things that surprised previous developers]

## Related Modules (optional)

[Links to related modules]
```

**Key requirements:**
- ALWAYS link to root CLAUDE.md at top
- Focus on module-specific information only
- Don't duplicate root CLAUDE.md content
- Include test infrastructure details
- Document "gotchas" that aren't obvious

### 5. Tool Entry Points

These files serve as entry points for specific AI tools and should primarily reference CLAUDE.md.

**`.github/copilot-instructions.md`**
```markdown
# GitHub Copilot Instructions

## Source of Truth

**CRITICAL:** [`CLAUDE.md`](../CLAUDE.md) at the repository root is the canonical source.

## Key Documentation

| Document | Purpose |
|----------|---------|
| `CLAUDE.md` | Primary development guidelines |
| `docs/TESTING.md` | Testing infrastructure |
| `docs/COMMIT_GUIDELINES.md` | Git commit discipline |
| etc. |

## Critical Rules (brief)

[Only the most critical rules that need emphasis]

## Skills and Agents

[List available skills and agents]
```

## Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Skill directory | `{category}-{topic}` in kebab-case | `.claude/skills/workflow-tdd/` |
| Skill frontmatter name | verb-object form | `tdd`, `write-skill`, `dispatch-agents` |
| Skill file | `SKILL.md` | `.claude/skills/workflow-tdd/SKILL.md` |
| Agent file | `kebab-case.agent.md` | `.github/agents/code-reviewer.agent.md` |
| Context file | `CLAUDE.md` | Always `CLAUDE.md` |

## Writing Guidelines

### 1. Be Concise

AI tools have context limits. Every word should earn its place.

```markdown
# ❌ Verbose
When you are working on this module, you should always remember to
check the root CLAUDE.md file for general guidelines that apply to
all modules in the repository.

# ✅ Concise
See root [CLAUDE.md](../CLAUDE.md) for repo-wide conventions.
```

### 2. Use Concrete Examples

Abstract rules are forgettable. Examples stick.

```markdown
# ❌ Abstract
Always use pattern matching for null checks.

# ✅ Concrete
// ✅ Use pattern matching
if (obj is null) { }

// ❌ Avoid equality operator
if (obj == null) { }
```

### 3. Structure for Scanning

AI tools scan documents. Use headers, tables, and bullet points.

```markdown
# ❌ Paragraph
The build command is dotnet build.cs build and you can also run
tests with dotnet build.cs test. For coverage you use dotnet build.cs
coverage and to create packages use dotnet build.cs pack.

# ✅ Structured
| Command | Purpose |
|---------|---------|
| `dotnet build.cs build` | Build solution |
| `dotnet build.cs test` | Run tests |
| `dotnet build.cs coverage` | Coverage report |
| `dotnet build.cs pack` | Create packages |
```

### 4. Front-Load Critical Information

Put the most important information first. Claude highlights "Use when" and "When to Use" sections.

### 5. Cross-Reference, Don't Duplicate

Reference other documents instead of copying content.

```markdown
# ❌ Duplicating
Memory management rules: [copies 50 lines from root CLAUDE.md]

# ✅ Referencing
See [Memory Management](../CLAUDE.md#memory-management-hierarchy) in root CLAUDE.md.
```

## Verification Checklist

When creating or updating AI documentation:

- [ ] **Skills** have YAML frontmatter with `name` (verb-object) and `description` (starts with "Use when")
- [ ] **Skills** have `## Overview` with **Core principle**
- [ ] **Skills** have `## Use when` section (lowercase)
- [ ] **Skills** have `## Verification` section
- [ ] **Agents** have YAML frontmatter with `name` and `description`
- [ ] **Agents** have "Use When" and "Capabilities" sections
- [ ] **Submodule CLAUDE.md** links to root CLAUDE.md at top
- [ ] **No circular references** between documentation files
- [ ] **Dependency direction** is correct (docs → CLAUDE.md → tool files)
- [ ] **Code examples** are correct and follow codebase conventions
- [ ] **No duplicate content** - reference instead of copy

## Examples

### Good Skill: workflow-tdd

```markdown
---
name: tdd
description: Use when implementing features or fixes - write failing test first, then minimal code to pass
---

# Test-Driven Development (TDD)

## Overview

Write the test first. Watch it fail. Write minimal code to pass.

**Core principle:** If you didn't watch the test fail, you don't know if it tests the right thing.

## Use when

**Always:**
- New features
- Bug fixes
- Refactoring

**Exceptions (ask your human partner):**
- Throwaway prototypes
- Generated code

## Red-Green-Refactor Cycle

### RED - Write Failing Test

Write one minimal test showing what should happen.

### Verify RED - Watch It Fail

**MANDATORY. Never skip.**

```bash
dotnet build.cs test --filter "FullyQualifiedName~MyTest"
```

### GREEN - Minimal Code

Write simplest code to pass the test. Don't add features beyond the test.

### REFACTOR - Clean Up

After green only: remove duplication, improve names.

## Verification

- [ ] Every new function has a test
- [ ] Watched each test fail before implementing
- [ ] All tests pass
- [ ] No warnings in output
```

### Good Submodule CLAUDE.md: SecurityDomain

```markdown
# CLAUDE.md - Security Domain Module

This file provides Claude-specific guidance for the Security Domain module.
**Read [README.md](README.md) first** for general documentation.

## Module Context

The Security Domain module manages YubiKey's root security application.
This is a **low-level security module** requiring careful key handling.

## Critical Security Requirements

[Specific to this module, not duplicated from root]

## Test Infrastructure

[Module-specific test helpers and patterns]

## Known Gotchas

1. **Reset is irreversible**: Once called, all custom keys are blocked.
2. **Default keys are KVN=0xFF**: After reset.
```

## Maintenance

This guide should be updated when:
- New file types are introduced
- Naming conventions change
- New required sections are identified
- Best practices evolve

Last updated: 2026-01-17
