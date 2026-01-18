---
name: write-agent
description: REQUIRED before creating any agent file - creates mirrored agents for both Copilot CLI and Claude Code
---

# Writing Custom Agents

## Overview

This skill creates custom agents that work across both GitHub Copilot CLI and Claude Code. By default, it creates **mirrored agents** - one for each platform with equivalent functionality.

**Core principle:** Agents should behave consistently regardless of which AI tool invokes them.

## Use when

**MANDATORY - invoke this skill when:**
- Creating ANY new agent file (for either platform)
- Need agent to work in both Copilot CLI and Claude Code
- Want consistent agent behavior across AI tools

**DO NOT manually create agent files.** Always invoke this skill (or platform-specific variant) first.

**Don't use when:**
- Creating platform-specific agent only (use `write-agent-copilot` or `write-agent-claudecode` directly)
- Creating a repeatable workflow (use `write-skill` instead)
- Task doesn't require agent-level expertise

## Default Behavior: Mirrored Agents

Unless told otherwise, create **both**:

| Platform | Location | File |
|----------|----------|------|
| Copilot CLI | `.github/agents/` | `{name}.agent.md` |
| Claude Code | `.claude/agents/` | `{name}.md` |

Both agents should have:
- Same name and purpose
- Equivalent capabilities
- Same process/workflow
- Same output format
- Platform-appropriate frontmatter

## Process

### 1. Gather Agent Requirements

Determine:
- **Name**: kebab-case identifier (e.g., `security-auditor`)
- **Purpose**: What expertise does this agent provide?
- **Capabilities**: What domain knowledge does it have?
- **Process**: What phases/steps does it follow?
- **Output**: What does it produce?
- **Constraints**: What should it NOT do?

### 2. Create Copilot CLI Agent

Use the `write-agent-copilot` skill to create `.github/agents/{name}.agent.md`:

```yaml
---
name: {name}
description: {purpose statement}
tools: ["read", "edit", "search", "terminal"]  # as needed
model: inherit
---
```

Follow the Copilot agent structure from `write-agent-copilot`.

### 3. Create Claude Code Agent

Use the `write-agent-claudecode` skill to create `.claude/agents/{name}.md`:

```yaml
---
name: {name}
description: {purpose statement}
model: sonnet  # or opus/haiku as appropriate
color: blue    # choose appropriate color
tools:
  - Read
  - Edit
  - Grep
  - Glob
---
```

Follow the Claude Code agent structure from `write-agent-claudecode`.

### 4. Ensure Consistency

Verify both agents have equivalent:

| Section | Must Match | Platform Difference |
|---------|------------|---------------------|
| Purpose/Description | ✅ Same intent | Copilot: `## Purpose`, Claude: persona + `## Purpose` |
| When to use | ✅ Same triggers | Copilot: `## Use When`, Claude: `## Scope` |
| Capabilities | ✅ Same expertise | Copilot: `## Capabilities`, Claude: within `## Scope` |
| Process | ✅ Same workflow | Same section name |
| Data Sources | ✅ Same references | Same section name |
| Output Format | ✅ Same structure | Same section name |
| Constraints | ✅ Same boundaries | Copilot: in `## Use When`, Claude: `## Constraints` |
| Related Resources | ✅ Same links | Same section name |

### 5. Verify Both Agents

- [ ] Copilot agent exists at `.github/agents/{name}.agent.md`
- [ ] Claude Code agent exists at `.claude/agents/{name}.md`
- [ ] Both have valid frontmatter for their platform
- [ ] Content sections are equivalent (not identical - adapted to platform)
- [ ] Related resources link to same documentation

## Platform Differences to Adapt

| Aspect | Copilot CLI | Claude Code |
|--------|-------------|-------------|
| Frontmatter | `description` required | `name`, `description`, `model`, `color` required |
| Model | `model: inherit` or omit | `model: sonnet/opus/haiku` required |
| Tools syntax | JSON array, lowercase: `["read", "edit"]` | YAML list, PascalCase: `- Read` |
| Context | Same conversation context | Isolated subagent context |
| Instructions | Can reference conversation | Must be fully self-contained |
| "Don't use" section | `## Use When` → `**DO NOT invoke when:**` | `## Scope` → `**Out of scope:**` |

**Key adaptation:** Claude Code agents run in isolation, so their instructions must be more explicit and self-contained than Copilot agents.

## Single-Platform Override

If user specifies a single platform:

**"Create a Copilot agent only"**
→ Use `write-agent-copilot` skill directly

**"Create a Claude Code agent only"**
→ Use `write-agent-claudecode` skill directly

## Example: Creating Mirrored Agents

**Request:** "Create a security auditor agent"

**Step 1: Create Copilot agent** (`.github/agents/security-auditor.agent.md`):

```markdown
---
name: security-auditor
description: Audits code for security vulnerabilities following OWASP guidelines and secure coding practices.
tools: ["read", "search", "terminal"]
model: inherit
---

# Security Auditor Agent

Security specialist focused on identifying vulnerabilities in .NET applications.

## Purpose

Audit code for security vulnerabilities, focusing on OWASP Top 10, .NET-specific issues, and cryptographic best practices.

## Use When

**Invoke this agent when:**
- Reviewing code before security-sensitive releases
- Auditing authentication/authorization code
- Checking cryptographic implementations

**DO NOT invoke when:**
- General code review (use `code-reviewer` agent)
- Performance optimization
- Style/formatting issues

## Capabilities

- **OWASP Top 10**: Injection, XSS, CSRF, etc.
- **Cryptography**: Key handling, algorithm selection, secure random
- **.NET Security**: `CryptographicOperations`, secure string handling
- **Memory Safety**: Buffer handling, sensitive data clearing

## Process

1. **Threat Modeling**
   Identify attack surfaces and trust boundaries.

2. **Static Analysis**
   Scan for known vulnerability patterns.

3. **Manual Review**
   Deep review of security-critical paths.

4. **Reporting**
   Document findings with severity and remediation.

## Output Format

### Security Audit Report

**Summary**
[Overview of findings]

**Critical Findings**
- **[Vuln Type]**: [Description]
  - Location: `file.cs:123`
  - Risk: [Impact]
  - Fix: [Remediation]

**Recommendations**
[Prioritized action items]

## Related Resources

- [CLAUDE.md](../../CLAUDE.md) - Security best practices
```

**Step 2: Create Claude Code agent** (`.claude/agents/security-auditor.md`):

```markdown
---
name: security-auditor
description: Audits code for security vulnerabilities following OWASP guidelines
model: sonnet
color: red
tools:
  - Read
  - Grep
  - Glob
---

You are a security specialist focused on identifying vulnerabilities in .NET applications.

## Purpose

Audit code for security vulnerabilities, focusing on OWASP Top 10, .NET-specific issues, and cryptographic best practices.

## Scope

**Focus on:**
- OWASP Top 10 vulnerabilities
- Cryptographic implementation issues
- .NET-specific security patterns
- Memory safety and sensitive data handling

## Out of scope

- General code quality (use `code-reviewer` agent)
- Performance optimization
- Style/formatting

## Process

1. Identify attack surfaces and trust boundaries
2. Scan for known vulnerability patterns
3. Deep review of security-critical paths
4. Document findings with severity and remediation

## Data Sources

- Source files in `src/` and module directories
- Security-sensitive areas: authentication, crypto, input handling

## Output Format

### Security Audit Report

**Summary**
[Overview of findings]

**Critical Findings**
- **[Vuln Type]**: [Description]
  - Location: `file.cs:123`
  - Risk: [Impact]
  - Fix: [Remediation]

**Recommendations**
[Prioritized action items]

## Constraints

- Do not modify source files
- Report only, no auto-fixes
- Flag uncertain findings for human review
```

## Verification

Mirrored agents are ready when:

- [ ] Copilot agent exists at `.github/agents/{name}.agent.md`
- [ ] Claude Code agent exists at `.claude/agents/{name}.md`
- [ ] Both have valid platform-specific frontmatter
- [ ] Purpose/description is equivalent
- [ ] Capabilities/scope match
- [ ] Process/workflow is equivalent
- [ ] Output format is identical
- [ ] Constraints match
- [ ] Claude Code version is fully self-contained

## Related Skills

- `write-agent-copilot` - Copilot CLI agent only
- `write-agent-claudecode` - Claude Code agent only
- `write-skill` - For repeatable workflows (not agents)
