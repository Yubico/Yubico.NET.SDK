# GitHub Copilot Instructions

This file provides guidance to GitHub Copilot CLI and other LLM-based tools when working with this repository.

## Source of Truth

**CRITICAL:** [`CLAUDE.md`](../CLAUDE.md) at the repository root is the canonical source of truth for all development guidelines in this project. You MUST read and follow it.

`CLAUDE.md` contains:
- Modern C# 14 language features and idioms
- Memory management with `Span<T>`, `Memory<T>`, and `ArrayPool<T>`
- Security best practices for cryptographic operations
- Code quality standards and EditorConfig compliance
- Build and test workflows using `build.cs`
- Architecture patterns for device connections and APDU processing
- Git workflow and commit discipline

## Critical Testing Rule

**ALWAYS use `dotnet build.cs test` - NEVER use `dotnet test` directly.**

This codebase uses a mix of xUnit v2 and xUnit v3 test projects that require different CLI invocations. The build script handles this automatically. Using `dotnet test` directly will fail on xUnit v3 projects.

See [`docs/TESTING.md`](../docs/TESTING.md) for full testing guidance.

## Key Documentation

| Document | Purpose |
|----------|---------|
| `CLAUDE.md` | Primary development guidelines (read first) |
| `docs/TESTING.md` | Testing infrastructure and xUnit v2/v3 differences |
| `docs/COMMIT_GUIDELINES.md` | Git commit discipline for agents |
| `BUILD.md` | Build script usage and targets |
| `docs/DEV-GUIDE.md` | Analyzer configuration and formatting |
| Subproject `CLAUDE.md` files | Module-specific patterns (e.g., `Yubico.YubiKit.Piv/CLAUDE.md`) |

## Skills

This project uses Agent Skills for specialized tasks:
- Project skills: `.claude/skills/` (shared across LLM tools)
- Personal skills: `~/.copilot/skills/` (user-specific)

## Custom Agents

Custom agents for specialized workflows are available in `.github/agents/`:
- `code-reviewer.agent.md` - Code review assistance
- `yubikit-porter.agent.md` - Porting from Java YubiKit

Git commit discipline is documented in `docs/COMMIT_GUIDELINES.md`.

## Subproject Context

When working in a subproject directory, also check for module-specific `CLAUDE.md` files:
- `Yubico.YubiKit.Core/CLAUDE.md`
- `Yubico.YubiKit.Piv/CLAUDE.md`
- `Yubico.YubiKit.Fido2/CLAUDE.md`
- etc.

These contain patterns, test harnesses, and context specific to that module.