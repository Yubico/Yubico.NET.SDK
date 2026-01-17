---
name: write-agent-claudecode
description: REQUIRED before creating Claude Code agents - NEVER create .claude/agents/ files manually
---

# Writing Claude Code Agents

## Overview

Claude Code agents are specialized subagents that run in isolated contexts, spawned on demand for delegated or long-running tasks. They have their own model selection and tools.

**Core principle:** Claude Code agents run independently - design them to work without access to the parent conversation's context.

## Use when

**MANDATORY - invoke this skill when:**
- Creating ANY new Claude Code agent in `.claude/agents/`
- Task requires deep, isolated work (long-running, parallel)
- Need a specialist subagent that can be spawned multiple times

**DO NOT manually create `.claude/agents/*.md` files.** Always invoke this skill first.

**Don't use when:**
- Creating Copilot CLI agents (use `write-agent-copilot` instead)
- Creating for both platforms (use `write-agent` instead)
- Task is a repeatable checklist (use `write-skill` instead)
- Task needs access to parent conversation context

## File Location and Naming

**Locations:**
1. `.claude/agents/` - Project-specific (most common)
2. `~/.claude/agents/` - User-specific (personal agents)

**File naming:**
- Pattern: `kebab-case.md`
- Examples: `code-reviewer.md`, `test-generator.md`
- Note: Claude Code uses `.md` (not `.agent.md` like Copilot)

## Frontmatter Specification

```yaml
---
name: agent-name              # REQUIRED - unique kebab-case identifier
description: What it does     # REQUIRED - brief summary for UI
model: sonnet                 # REQUIRED - sonnet | opus | haiku
color: blue                   # REQUIRED - UI accent color
tools:                        # Optional - available tools
  - Read
  - Grep
  - Glob
  - Edit
  - Bash
---
```

**Required fields (all four):**
- `name` - Unique kebab-case identifier
- `description` - Brief summary shown in UI
- `model` - Which Claude model to use
- `color` - UI accent color

**Model selection:**

| Model | Use Case |
|-------|----------|
| `haiku` | Fast, simple tasks (exploration, quick lookups) |
| `sonnet` | Balanced (code review, generation, most tasks) |
| `opus` | Complex reasoning (architecture, difficult debugging) |

**Color options:** `blue`, `green`, `red`, `yellow`, `purple`, `orange`, `cyan`

**Common tool sets:**

| Use Case | Tools |
|----------|-------|
| Read-only analysis | `[Read, Grep, Glob]` |
| Code modification | `[Read, Edit, Grep, Glob, Bash]` |
| Full access | Omit `tools` for all available |

## Agent Body Structure

```markdown
---
name: agent-name
description: Brief summary of what this agent does
model: sonnet
color: blue
tools:
  - Read
  - Grep
  - Edit
---

You are [persona description - who this agent "is"].

## Purpose

[What this agent does and when to use it - 2-3 sentences]

## Scope

**Focus on:**
- [What the agent should do]
- [What areas it covers]

**Out of scope:**
- [What to avoid] - use `alternative` instead
- [What to delegate elsewhere] - use `alternative` instead

## Process

1. [Step or phase 1]
2. [Step or phase 2]
3. [Step or phase 3]

## Data Sources

- [What files/directories to scan]
- [What systems to access]

## Output Format

[Expected output structure - be specific]

```example
[Template of expected output]
```

## Examples

_Input:_ [Example invocation]
_Output:_ [Expected result]

## Constraints

- [Limitation 1 - e.g., "Do not modify source files"]
- [Limitation 2 - e.g., "Only analyze, don't execute"]
```

## Key Differences from Copilot Agents

| Aspect | Claude Code | Copilot CLI |
|--------|-------------|-------------|
| Location | `.claude/agents/` | `.github/agents/` |
| File extension | `.md` | `.agent.md` |
| Required frontmatter | `name`, `description`, `model`, `color` | `description` only |
| Model selection | Required (`sonnet`/`opus`/`haiku`) | Optional (`inherit`) |
| Color | Required | Not supported |
| Tools syntax | YAML list, PascalCase: `- Read` | JSON array, lowercase: `["read"]` |
| Execution | Isolated subagent context | Same conversation context |
| "Don't use" section | `## Scope` → `**Out of scope:**` | `## Use When` → `**DO NOT invoke when:**` |

## Writing Effective Instructions

Claude Code agents run in isolation - they don't see parent context. Be explicit:

**❌ Vague:**
```markdown
Review the code changes.
```

**✅ Self-contained:**
```markdown
You are a senior code reviewer. Review all modified files in the current git diff.

## Review Checklist
1. **Bugs & Logic Errors** — off-by-one, null handling
2. **Security** — injection, hardcoded secrets
3. **Performance** — inefficient loops, N+1 queries
4. **Code Quality** — naming, DRY, single responsibility
```

## Writing the Output Format

Be precise about expected deliverables:

```markdown
## Output Format

For each issue found:
- **Severity**: Critical / High / Medium / Low
- **File**: path/to/file.cs
- **Line**: 40-44
- **Issue**: Succinct summary
- **Fix**: Suggested remedy

### Example Output

- **Severity**: High
- **File**: src/Auth/TokenValidator.cs
- **Line**: 127-130
- **Issue**: SQL injection vulnerability in query construction
- **Fix**: Use parameterized queries instead of string concatenation
```

## Writing Constraints

Explicitly state boundaries:

```markdown
## Constraints

- Only generate tests; do not alter source files
- Do not execute any commands that modify state
- Report findings but do not auto-fix
- Stay within the specified directory scope
```

## Example: Complete Claude Code Agent

```markdown
---
name: test-generator
description: Generates comprehensive C# test suites using xUnit
model: sonnet
color: green
tools:
  - Read
  - Grep
  - Glob
  - Edit
---

You are a test generation specialist for C# projects using xUnit.

## Purpose

Generate unit tests for C# classes and methods. Focus on edge cases, error handling, and behavior verification.

## Scope

**Focus on:**
- Public methods and their contracts
- Edge cases and boundary conditions
- Error handling paths
- Async/await patterns

**Out of scope:**
- Integration tests requiring external systems
- UI/controller tests
- Performance benchmarks

## Process

1. Analyze the target file for public methods and classes
2. Identify test scenarios (happy path, edge cases, errors)
3. Generate xUnit test methods with Arrange-Act-Assert pattern
4. Include appropriate `[Fact]` and `[Theory]` attributes

## Data Sources

- Source files in `src/` directory
- Existing tests in `tests/` for pattern reference

## Output Format

For each method, generate:

```csharp
public class {ClassName}Tests
{
    [Fact]
    public void {MethodName}_{Scenario}_{ExpectedResult}()
    {
        // Arrange
        
        // Act
        
        // Assert
    }
}
```

## Examples

_Input:_ `src/Services/UserService.cs`
_Output:_ `tests/Services/UserServiceTests.cs` with test methods for each public method

## Constraints

- Follow existing test patterns in the codebase
- Use `CLAUDE.md` coding standards
- Do not modify source files, only generate tests
- Prefer real objects over mocks when feasible
```

## Common Mistakes

**❌ Missing required frontmatter:** All four fields (`name`, `description`, `model`, `color`) are required
**✅ Complete frontmatter:** Include all required fields

**❌ Assuming parent context:** Agent can't see what you were discussing
**✅ Self-contained instructions:** Include all necessary context in the agent file

**❌ Vague output format:** "Generate a report"
**✅ Specific template:** Show exact structure with examples

**❌ No constraints:** Agent might modify files unexpectedly
**✅ Explicit boundaries:** "Do not modify source files"

**❌ Wrong file location:** `.github/agents/` (that's Copilot)
**✅ Correct location:** `.claude/agents/`

## Verification

Agent is ready when:

- [ ] Frontmatter has all required fields: `name`, `description`, `model`, `color`
- [ ] `model` is one of: `sonnet`, `opus`, `haiku`
- [ ] `## Purpose` or persona description explains what agent does
- [ ] `## Scope` defines focus and out-of-scope items
- [ ] `## Output Format` has specific template/structure
- [ ] `## Constraints` lists explicit boundaries
- [ ] File is in `.claude/agents/` with `.md` extension
- [ ] Instructions are self-contained (no parent context assumptions)

## Related Skills

- `write-agent-copilot` - For Copilot CLI agents (different format)
- `write-skill` - For repeatable workflows (not isolated subagents)
- `write-module-docs` - For README.md/CLAUDE.md documentation
