---
name: write-skill
description: REQUIRED before creating any skill file - NEVER create .claude/skills/ files manually
---

# Writing Skills

## Overview

Skills are reusable workflows invoked via `/skillname` in Claude Code or through Copilot CLI's skill tool. A well-written skill has clear trigger conditions, a concrete process, and predictable output.

**Core principle:** A skill must be specific enough to invoke confidently but general enough to reuse across contexts.

## Use when

**MANDATORY - invoke this skill when:**
- Creating ANY new skill file in `.claude/skills/`
- Updating an existing skill that isn't being invoked correctly
- Converting ad-hoc instructions into a reusable workflow

**DO NOT manually create skill files.** Always invoke this skill first.

**Don't use when:**
- Task requires judgment/adaptation (use `write-agent` instead)
- Task is one-time or exploratory
- Task spans multiple unrelated domains

## Naming Conventions

### Directory Name

Format: `{category}-{topic}` in kebab-case

| Category | Purpose | Example |
|----------|---------|---------|
| `workflow-` | Multi-step development processes | `workflow-tdd`, `workflow-debug` |
| `tool-` | Wrapper around specific tools | `tool-build`, `tool-import-skill` |
| `docs-` | Documentation creation | `docs-module`, `docs-write-skill` |
| `agent-` | Agent orchestration patterns | `agent-dispatch`, `agent-ralph-loop` |
| `domain-` | Domain-specific knowledge | `domain-yubikit-compare` |
| `jira-` | Jira operations | `jira-create`, `jira-search` |
| `review-` | Code review workflows | `review-request`, `review-receive` |

### Frontmatter `name`

The `name` field is what appears in tool listings and invocation. Use verb-object form:
- `write-skill` (not `skill-writing`)
- `dispatch-agents` (not `agent-dispatch`)
- `build` (short form acceptable for tools)

## Skill Structure

### File Naming - CRITICAL

**The skill markdown file MUST be named `SKILL.md`** - not `CLAUDE.md`, not `README.md`, not any other name.

```
.claude/skills/
└── domain-example-skill/
    └── SKILL.md          ← ✅ REQUIRED - file must be named exactly SKILL.md
```

This is how Claude Code and Copilot CLI discover and load skills.

### Required Elements

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

[Numbered steps or clear subsections]

## Verification

[How to confirm the skill completed successfully]
```

### Optional Elements

- `## Examples` - Concrete usage with expected outcomes
- `## Common Mistakes` - What to avoid
- `## Related Skills` - When to use something else

## Writing the Description

The `description` field in frontmatter is **critical** - it's what triggers skill invocation.

**Pattern:** `Use when [condition] - [brief action]`

| ❌ Weak | ✅ Strong |
|---------|-----------|
| `Helps with testing` | `Use when implementing features - write failing test first, then minimal code to pass` |
| `Build tool` | `Use when compiling, testing, or packaging .NET code - runs build.cs targets (NEVER use dotnet test directly)` |
| `For debugging` | `Use when encountering bugs, test failures, or unexpected behavior - systematic root cause analysis before fixes` |

**Rules:**
- Start with "Use when" - Claude/Copilot looks for this phrase
- Include concrete trigger conditions
- Mention what NOT to do if critical (e.g., "NEVER use dotnet test directly")

## Writing the "Use when" Section

This section determines whether the skill gets invoked. Front-load trigger conditions.

**Structure:**
```markdown
## Use when

**Use this skill when:**
- [Most common trigger]
- [Second most common]
- [Edge case trigger]

**Don't use when:**
- [Common misuse case]
- [When another skill is better]
```

**Good triggers are:**
- Observable states ("3+ test files failing")
- User actions ("implementing a new feature")
- Specific contexts ("working with Jira issues")

**Bad triggers are:**
- Vague ("when appropriate")
- Subjective ("when it makes sense")
- Rare edge cases first

## Writing the Process

Use numbered steps for sequential processes, subsections for parallel concerns.

**Sequential (do in order):**
```markdown
## Process

1. **Step Name**
   What to do and why.

2. **Step Name**
   What to do and why.
```

**Parallel (do any/all):**
```markdown
## Core Build Command

```bash
dotnet build.cs [target] [options]
```

## Available Targets

| Target | Description |
|--------|-------------|
| `build` | Build solution |
| `test` | Run tests |
```

## Examples Section

Show concrete usage with expected outcomes:

```markdown
## Example: Bug Fix with TDD

**Scenario:** Empty email accepted in form

**RED**
```csharp
[Fact]
public void SubmitForm_RejectsEmptyEmail()
{
    var result = SubmitForm(new FormData { Email = "" });
    Assert.Equal("Email required", result.Error);
}
```

**Verify RED**
```bash
$ dotnet test
FAIL: expected 'Email required', got null
```
```

## Verification Section

Always include how to confirm success:

```markdown
## Verification

Before marking work complete:

- [ ] Every new function has a test
- [ ] All tests pass
- [ ] No warnings in output
```

Or for simpler skills:

```markdown
## Verification

Skill completed successfully when:
- Build passes with no warnings
- All tests green
- Changes committed
```

## Common Mistakes

**❌ Too abstract:** Rules without examples
**✅ Concrete:** Show code, commands, expected output

**❌ Missing triggers:** No "Use when" section
**✅ Clear triggers:** Multiple specific conditions

**❌ Wall of text:** Long paragraphs
**✅ Scannable:** Tables, bullets, code blocks

**❌ Duplicating docs:** Copying from other files
**✅ Referencing:** Link to canonical sources

**❌ Vague output:** "Fix the issue"
**✅ Specific output:** "Return summary of root cause and changes"

## Template

Create a new directory with the format `.claude/skills/{category}-{topic}/` and create a file **named exactly `SKILL.md`** inside it:

```markdown
---
name: verb-object
description: Use when [condition] - [action] (NEVER [anti-pattern] if critical)
---

# Skill Title

## Overview

One-two sentence description.

**Core principle:** [Guiding philosophy]

## Use when

**Use this skill when:**
- Trigger 1
- Trigger 2

**Don't use when:**
- Exception 1
- Exception 2

## [Main Section - named for the domain]

### Subsection (if needed)

Content with code examples:

```language
// Example code
```

## Common Mistakes (optional)

**❌ Bad:** Description
**✅ Good:** Description

## Verification

- [ ] Checklist item 1
- [ ] Checklist item 2

## Related Skills (optional)

- `skill-name` - When to use instead
```

**Remember:** The file MUST be named `SKILL.md` for Claude to discover it.

## Verification

Skill is ready when:

- [ ] **File is named `SKILL.md`** (not CLAUDE.md, not README.md - exactly `SKILL.md`)
- [ ] Directory follows format: `.claude/skills/{category}-{topic}/`
- [ ] Frontmatter has `name` (verb-object form) and `description` (starts with "Use when")
- [ ] "Use when" section has concrete trigger conditions
- [ ] "Don't use when" lists exceptions
- [ ] Process has numbered steps or clear structure
- [ ] Examples use real code/commands from the codebase
- [ ] Verification section exists

## Related Skills

- `write-module-docs` - For README.md/CLAUDE.md in subdirectories
- `write-agent` - For custom agents (judgment-based, not checklists)
- `write-ralph-prompt` - For autonomous agent prompts (different structure)
