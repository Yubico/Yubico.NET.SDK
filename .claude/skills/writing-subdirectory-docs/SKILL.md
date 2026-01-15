---
name: writing-subdirectory-docs
description: Use when creating or updating CLAUDE.md and README.md files for subdirectories (modules, test directories, etc.)
---

# Writing Subdirectory Documentation

## Overview

This skill guides creation of `README.md` and `CLAUDE.md` files for subdirectories. These serve **different audiences** with **different needs**.

## The Two Files

| File | Audience | Purpose | Tone |
|------|----------|---------|------|
| `README.md` | Human developers | How to use, run, configure | Tutorial, reference |
| `CLAUDE.md` | Claude/AI assistants | How it works internally | Technical, implementation-focused |

## README.md - For Human Developers

**Reader:** A developer who wants to use this module/test suite/component.

**Questions it answers:**
- How do I run this?
- What commands do I need?
- What are the prerequisites?
- What's the quick start?
- Where are things located?

**Structure:**
```markdown
# [Component Name]

Brief description (1-2 sentences).

## Quick Start

[Minimal steps to get running]

## Prerequisites / Setup

[What you need before using this]

## Usage / Running

[Commands, configuration, examples]

## Structure / Organization

[What's where - file/folder layout]

## Common Tasks

[How to do X, Y, Z]
```

**Style guidelines:**
- Scannable (headers, tables, code blocks)
- Action-oriented ("Run this", "Add this")
- Minimal explanation (link to CLAUDE.md for deep dives)
- Copy-pasteable commands

## CLAUDE.md - For AI Assistants

**Reader:** Claude or another AI working on this code.

**Questions it answers:**
- How does this work internally?
- What patterns are used?
- What are the gotchas/edge cases?
- Why was it built this way?
- What should I watch out for?

**Structure:**
```markdown
# CLAUDE.md - [Component Name]

This file provides Claude-specific guidance for [component].

## [Key Concept 1]

### How It Works

[Implementation details, code flow]

### Patterns

[Code patterns with examples]

## [Key Concept 2]

[...]

## Common Gotchas

1. **[Gotcha]** - [Explanation]
2. **[Gotcha]** - [Explanation]

## Related

- [Links to related modules/files]
```

**Style guidelines:**
- Implementation-focused (the "why" and "how")
- Code examples showing patterns
- Gotchas and edge cases prominently featured
- Internal details that wouldn't interest most developers
- Cross-references to source code (file:line)

## Decision Matrix: What Goes Where?

| Content | README.md | CLAUDE.md |
|---------|-----------|-----------|
| How to run tests | ✅ | ❌ |
| Test infrastructure internals | ❌ | ✅ |
| Installation/setup steps | ✅ | ❌ |
| Why a pattern was chosen | ❌ | ✅ |
| Command reference | ✅ | ❌ |
| Code flow / architecture | ❌ | ✅ |
| Prerequisites | ✅ | ❌ |
| Edge cases / gotchas | ❌ | ✅ |
| File structure | ✅ | ❌ |
| Implementation patterns | ❌ | ✅ |
| Quick examples | ✅ | ✅ (more detailed) |

## Examples

### Test Directory

**README.md** (for developers):
```markdown
# Security Domain Tests

## Running Tests

```bash
# Unit tests (no hardware)
dotnet test Yubico.YubiKit.SecurityDomain.UnitTests

# Integration tests (requires YubiKey)
dotnet test Yubico.YubiKit.SecurityDomain.IntegrationTests
```

## Test Projects

| Project | Hardware Required |
|---------|-------------------|
| UnitTests | No |
| IntegrationTests | Yes |

## Writing Tests

```csharp
[Theory]
[WithYubiKey(MinFirmware = "5.4.3")]
public async Task MyTest(YubiKeyTestState state) =>
    await state.WithSecurityDomainSessionAsync(...);
```
```

**CLAUDE.md** (for AI):
```markdown
# CLAUDE.md - Security Domain Tests

## Test Extension Methods

### WithSecurityDomainSessionAsync

```csharp
extension(YubiKeyTestState state)
{
    public Task WithSecurityDomainSessionAsync(
        bool resetBeforeUse,
        Func<SecurityDomainSession, Task> action,
        ...)
}
```

**Implementation:**
- Creates `SharedSmartCardConnection` to share connection
- Reset session uses unauthenticated connection
- Test session uses SCP authentication
- Connection disposed after test completes

### Reset Mechanism (line 685)

The `ResetAsync()` method:
1. Enumerates keys via `GetKeyInfoAsync()`
2. For each key, sends 65 failed auth attempts
3. Waits for `0x6983` (blocked) status
4. Reinitializes session

## Common Gotchas

1. **Reset destroys all keys** - `resetBeforeUse: true` wipes custom keys
2. **DI prerequisite** - `AddYubiKeySecurityDomain()` requires `AddYubiKeyManagerCore()` first
```

## When to Create These Files

Create both files when:
- Adding a new module/package
- Adding a test directory with custom infrastructure
- Creating a significant subdirectory with its own patterns

Skip if:
- The directory is trivial (just a few utility files)
- Parent CLAUDE.md already covers it adequately

## Maintenance

When making significant changes:
1. Update README.md if usage/commands change
2. Update CLAUDE.md if patterns/internals change
3. Keep gotchas current - add new ones as discovered

## Anti-Patterns

**README.md anti-patterns:**
- ❌ Deep implementation details
- ❌ "Why we chose X over Y" discussions
- ❌ Internal code flow explanations
- ❌ Exhaustive API documentation (use XML docs)

**CLAUDE.md anti-patterns:**
- ❌ Basic usage instructions
- ❌ Installation steps
- ❌ Command reference without context
- ❌ Duplicate of XML documentation