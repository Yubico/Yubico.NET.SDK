---
name: write-agent-copilot
description: REQUIRED before creating Copilot CLI agents - NEVER create .github/agents/ files manually
---

# Writing Copilot CLI Agents

## Overview

Agents are specialized personas with domain expertise invoked via `@agent-name` in Copilot CLI. Unlike skills (step-by-step workflows), agents exercise judgment and adapt to context.

**Core principle:** An agent is an expert, not a checklist. Define expertise and boundaries, not rigid procedures.

## Use when

**MANDATORY - invoke this skill when:**
- Creating ANY new Copilot CLI agent in `.github/agents/`
- Task requires deep domain expertise and judgment calls
- Need a "specialist persona" for complex work

**DO NOT manually create `.github/agents/*.agent.md` files.** Always invoke this skill first.

**Don't use when:**
- Task is a repeatable checklist (use `write-skill` instead)
- Creating Claude Code agents (use `write-agent-claudecode` instead)
- Creating for both platforms (use `write-agent` instead)
- Task is simple enough for inline instructions

## File Location and Naming

**Locations (searched in order):**
1. `.github/agents/` - Repository-specific (most common)
2. `{org}/.github/agents/` - Organization-wide
3. `~/.copilot/agents/` - User-specific

**File naming:**
- Pattern: `kebab-case.agent.md`
- Examples: `code-reviewer.agent.md`, `security-audit.agent.md`
- Allowed characters: `a-z`, `A-Z`, `0-9`, `.`, `-`, `_`

## Frontmatter Specification

```yaml
---
name: agent-name              # Display name (optional, defaults to filename)
description: Expert in X      # REQUIRED - what the agent specializes in
tools: ["read", "edit", "search", "terminal"]  # Optional, omit for all tools
model: inherit                # Optional - inherit, or specific model
infer: false                  # Optional - if true, auto-invoked by Copilot
mcp-servers:                  # Optional - extra MCP server configs
metadata:                     # Optional - arbitrary key-value pairs
  team: security
---
```

**Required fields:**
- `description` - The only required field. Be specific about expertise.

**Common tool sets:**
| Use Case | Tools |
|----------|-------|
| Read-only analysis | `["read", "search"]` |
| Code modification | `["read", "edit", "search", "terminal"]` |
| Full access | Omit `tools` or use `["*"]` |
| GitHub integration | Include `"mcp__github"` |

## Agent Body Structure

```markdown
---
name: agent-name
description: Expert in X for Y context
tools: ["read", "edit", "search", "terminal"]
---

# Agent Title

[1-2 sentence persona description - who this agent "is"]

## Purpose

[2-3 sentences on what this agent specializes in and why it exists]

## Use When

**Invoke this agent when:**
- [Trigger condition 1]
- [Trigger condition 2]

**DO NOT invoke when:**
- [Exception 1] - use `alternative` instead
- [Exception 2] - use `alternative` instead

## Capabilities

- [Domain knowledge area 1]
- [Domain knowledge area 2]
- [What patterns/tools it knows]

## Process

1. **Phase Name**
   [High-level description - agents adapt, don't follow rigid steps]

2. **Phase Name**
   [What the agent does in this phase]

## Output Format

[What the agent produces - reports, code, recommendations]
[Include template if output is structured]

## Data Sources

- [What files/directories to read]
- [What skills to load for guidance]

## Related Resources

- [Links to relevant documentation]
- [Links to related skills or agents]
```

## Writing Effective Descriptions

The `description` field triggers agent selection. Be specific.

| ❌ Weak | ✅ Strong |
|---------|-----------|
| `Helps with code review` | `Reviews completed project steps against original plans and ensures code quality standards are met` |
| `Security expert` | `Expert in cross-language porting of security-critical code with meticulous attention to protocol correctness` |
| `Documentation helper` | `Technical documentation expert for .NET SDK projects with API reference expertise` |

## Writing the "Use When" Section

**Structure:**
```markdown
## Use When

**Invoke this agent when:**
- [Observable condition that signals need for this agent]
- [Task type this agent excels at]
- [Context where expertise applies]

**DO NOT invoke when:**
- [Common misuse] - use `alternative` instead
- [Out of scope task] - use `alternative` instead
```

**Good triggers:**
- "A major project step or feature implementation is complete"
- "Porting a feature from `yubikit-android` to `Yubico.NET.SDK`"
- "Code needs review before merging to `develop`"

**Bad triggers:**
- "When needed" (vague)
- "For complex tasks" (subjective)
- "When you want help" (too broad)

## Writing Capabilities

List specific expertise, not generic abilities:

```markdown
## Capabilities

- **Plan Alignment**: Compare implementations against planning documents
- **Security Audit**: Identify OWASP top 10 vulnerabilities, memory safety issues
- **Languages**: Java 17+, C# 14, modern async/await patterns
- **Security Protocols**: SCP (03, 11a/b/c), FIDO/FIDO2, PIV, OpenPGP
```

## Writing the Process

Unlike skills (numbered steps), agents have **phases** that guide without constraining:

```markdown
## Process

1. **Analysis Phase**
   Understand the request, gather context, identify constraints.

2. **Execution Phase**
   Apply expertise to solve the problem, adapting approach as needed.

3. **Verification Phase**
   Validate results against requirements, document findings.
```

For complex agents, use visual workflows:

```markdown
## Process

```
┌─────────────────────────────────────────┐
│            WORKFLOW LOOP                │
│  1. ANALYSIS → 2. DESIGN → 3. IMPLEMENT │
│       ↑                        ↓        │
│       └──── FAILED? ←── 4. VERIFY ──────┘
└─────────────────────────────────────────┘
```
```

## Output Format Section

Define what the agent produces:

```markdown
## Output Format

### Review Report Structure

```markdown
## Review: [Feature/Task Name]

### Summary
[1-2 sentence overall assessment]

### Issues Found

#### Critical (Must Fix)
- **[Issue]**: [Description]
  - Location: `file.cs:123`
  - Fix: [Specific recommendation]

### Recommendation
[APPROVE / REQUEST_CHANGES / NEEDS_DISCUSSION]
```
```

## Common Mistakes

**❌ Too procedural:** Agents aren't checklists - describe expertise, not rigid steps
**✅ Expertise-focused:** Define capabilities and judgment areas

**❌ Generic description:** "Helps with X"
**✅ Specific description:** "Expert in X for Y context with Z methodology"

**❌ Missing alternatives:** "DO NOT use when: debugging"
**✅ With alternatives:** "DO NOT use when: debugging - use `systematic-debugging` skill instead"

**❌ No output format:** Agent produces inconsistent results
**✅ Clear output:** Template with expected structure

**❌ Walls of text:** Hard to scan
**✅ Structured:** Tables, bullets, code blocks, diagrams

## Example: Well-Structured Agent

```markdown
---
name: code-reviewer
description: Reviews completed project steps against original plans and ensures code quality standards are met.
model: inherit
---

# Code Reviewer Agent

Senior Code Reviewer with expertise in software architecture, design patterns, and best practices.

## Purpose

Review completed work against original plans and project standards. Ensure code quality, architectural consistency, and adherence to established patterns.

## Use When

**Invoke this agent when:**
- A major project step or feature implementation is complete
- Code needs review before merging to `develop`
- Verifying implementation matches the design spec

**DO NOT invoke when:**
- Just looking for quick syntax fixes
- Need debugging help (use `systematic-debugging` skill instead)
- Writing new code (use appropriate development skills)

## Capabilities

- **Plan Alignment**: Compare implementations against planning documents
- **Code Quality**: Assess patterns, error handling, type safety
- **Security Audit**: Identify OWASP top 10, memory safety issues
- **Standards Compliance**: Verify adherence to `CLAUDE.md` guidelines

## Process

1. **Plan Alignment Analysis**
   Compare implementation against original planning document.

2. **Code Quality Assessment**
   Review patterns, error handling, test coverage.

3. **Issue Identification**
   Categorize as Critical/Important/Suggestions with actionable fixes.

## Output Format

[Structured template...]

## Related Resources

- [CLAUDE.md](../../CLAUDE.md) - Primary coding standards
```

## Verification

Agent is ready when:

- [ ] Frontmatter has `description` (required) - specific expertise statement
- [ ] `## Use When` has concrete trigger conditions with alternatives for "DO NOT"
- [ ] `## Capabilities` lists specific domain expertise
- [ ] `## Process` describes phases (not rigid steps)
- [ ] `## Output Format` defines expected deliverables
- [ ] File named `kebab-case.agent.md` in `.github/agents/`

## Related Skills

- `write-agent-claudecode` - For Claude Code agents (different format)
- `write-skill` - For repeatable workflows (not judgment-based tasks)
- `write-module-docs` - For README.md/CLAUDE.md documentation
