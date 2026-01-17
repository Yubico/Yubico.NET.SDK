# GitHub Copilot Instructions

This file provides guidance to GitHub Copilot CLI and other LLM-based tools when working with this repository.

## Source of Truth

**CRITICAL:** [`CLAUDE.md`](../CLAUDE.md) at the repository root is the canonical source of truth for all development guidelines. You MUST read and follow it.

## Key Documentation

| Document | Purpose |
|----------|---------|
| [`CLAUDE.md`](../CLAUDE.md) | Primary development guidelines (read first) |
| [`docs/AI-DOCS-GUIDE.md`](../docs/AI-DOCS-GUIDE.md) | How to write AI documentation (skills, agents, CLAUDE.md) |
| [`docs/TESTING.md`](../docs/TESTING.md) | Testing infrastructure and xUnit v2/v3 differences |
| [`docs/COMMIT_GUIDELINES.md`](../docs/COMMIT_GUIDELINES.md) | Git commit discipline for agents |
| [`BUILD.md`](../BUILD.md) | Build script usage and targets |
| Subproject `CLAUDE.md` files | Module-specific patterns |

## Critical Rules

### Testing

**ALWAYS use `dotnet build.cs test` - NEVER use `dotnet test` directly.**

This codebase uses a mix of xUnit v2 and xUnit v3 test projects that require different CLI invocations. The build script handles this automatically.

### Code Quality

- Modern C# 14 patterns (see `CLAUDE.md`)
- Memory management with `Span<T>`, `Memory<T>`, `ArrayPool<T>`
- Security: zero sensitive data, use `CryptographicOperations.ZeroMemory()`
- EditorConfig compliance: run `dotnet format` before commit

### Git Discipline

- Main branch: `develop` (not `main`)
- **Only commit files YOU modified** - never use `git add .`
- Follow conventional commits: `feat:`, `fix:`, `refactor:`, etc.

## Skills

Project skills in `.claude/skills/`:

| Skill | Purpose |
|-------|---------|
| `build` | Build, test, package using `build.cs` |
| `systematic-debugging` | Structured debugging process |
| `test-driven-development` | TDD workflow |
| `writing-plans` | Create implementation plans |
| `verification-before-completion` | Verify work before claiming done |
| `yubikit-codebase-comparison` | Compare Java and C# implementations |
| `dotnet-script-experiments` | Create experiment scripts |
| `write-skill` | **REQUIRED** before creating skill files |
| `write-agent` | **REQUIRED** before creating agent files |

### Mandatory Skills

**NEVER create these files manually. ALWAYS invoke the corresponding skill first:**

| Action | Required Skill | Rationale |
|--------|----------------|-----------|
| Creating files in `.claude/skills/` | `write-skill` | Ensures proper frontmatter, structure, naming |
| Creating files in `.github/agents/` | `write-agent` or `write-agent-copilot` | Ensures proper format for platform |
| Creating files in `.claude/agents/` | `write-agent` or `write-agent-claudecode` | Ensures proper format for platform |

**Violations:** If you create skill/agent files without invoking the skill, delete them and start over using the skill.

## Agents

Custom agents in `.github/agents/`:

| Agent | Purpose |
|-------|---------|
| `code-reviewer` | Review completed work against plans and standards |
| `yubikit-porter` | Port functionality from Java YubiKit |

## Subproject Context

When working in a subproject directory, check for module-specific `CLAUDE.md`:
- `Yubico.YubiKit.Core/CLAUDE.md`
- `Yubico.YubiKit.Piv/CLAUDE.md`
- `Yubico.YubiKit.Fido2/CLAUDE.md`
- `Yubico.YubiKit.SecurityDomain/CLAUDE.md`
- `Yubico.YubiKit.Management/CLAUDE.md`

These contain patterns, test infrastructure, and module-specific context.
