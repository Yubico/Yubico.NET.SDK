# GitHub Copilot Instructions

This file provides guidance to GitHub Copilot CLI and other LLM-based tools when working with this repository.

## Primary Documentation

**IMPORTANT:** The canonical development guidelines for this project are maintained in [`CLAUDE.md`](../CLAUDE.md) at the repository root.

All contributors and AI assistants should follow the patterns, conventions, and best practices documented there, including:

- Modern C# 14 language features and idioms
- Memory management with `Span<T>`, `Memory<T>`, and `ArrayPool<T>`
- Security best practices for cryptographic operations
- Code quality standards and EditorConfig compliance
- Build and test workflows using `build.cs`
- Architecture patterns for device connections and APDU processing

## Skills

This project uses Agent Skills for specialized tasks. Skills are located in:
- Project skills: `.claude/skills/` (shared across LLM tools)
- Personal skills: `~/.copilot/skills/` (user-specific)

Copilot will automatically load relevant skills based on your prompts.

## Custom Agents

Custom agents for specialized workflows are available in `.github/agents/`. Use the `/agent` command to browse and select available agents, or mention them by name in your prompts.

## Additional Context

For subproject-specific guidance, check for `CLAUDE.md` files in individual project directories (e.g., `Yubico.YubiKit.Piv/CLAUDE.md`).
