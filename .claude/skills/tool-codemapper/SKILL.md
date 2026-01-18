---
name: codemapper
description: Use when exploring codebase structure - generates AST maps of public API surface (~1.5s for entire repo)
---

# CodeMapper Skill

## Overview

Generate and query structural maps of C# codebases using CodeMapper. Maps show public/internal API surface with line numbers, enabling fast codebase orientation without reading every file.

**Core principle:** Always regenerate - at ~1.5 seconds, fresh maps are cheaper than stale context.

## Use when

**Use this skill when:**
- Starting work on an unfamiliar module
- Need to find where a type/interface is defined
- Understanding inheritance hierarchies or DI dependencies
- Pre-loading context before implementation tasks
- Comparing what exists before designing new features

**Don't use when:**
- You already know exactly which file to edit
- Searching for string literals or implementation details (use `grep` instead)
- Need to understand runtime behavior (maps show structure only)

## Quick Reference

| Task | Command |
|------|---------|
| Generate fresh maps | `codemapper .` |
| Find a symbol | `codemapper . && grep -rn "IYubiKey" ./codebase_ast/` |
| Load project context | `cat ./codebase_ast/Yubico.YubiKit.Fido2.txt` |
| Codebase stats | `head -1 ./codebase_ast/*.txt` |

## Output Location

Default: `./codebase_ast/` - one `.txt` file per project.

**Note:** This directory is gitignored but agents can still read it. The `.gitignore` only prevents Git from tracking files—it doesn't prevent tools from reading them from the filesystem. Always check `./codebase_ast/` even though it's gitignored.

## Workflow 1: Codebase Orientation

When starting work on a new area:

1. **Generate fresh maps**
   ```bash
   codemapper .
   ```

2. **Get overview stats**
   ```bash
   head -1 ./codebase_ast/*.txt
   ```
   Output shows file/namespace/type/method counts per project.

3. **Identify relevant project**
   Pick the project(s) relevant to your task.

4. **Load that project's map**
   ```bash
   cat ./codebase_ast/Yubico.YubiKit.Fido2.txt
   ```

## Workflow 2: Find a Symbol

When you need to locate a type, interface, or method:

1. **Generate + search in one command**
   ```bash
   codemapper . && grep -rn "IDeviceRepository" ./codebase_ast/
   ```

2. **Review results**
   Output shows file, line number, and signature:
   ```
   Yubico.YubiKit.Core.txt:59:    [Interface] IDeviceRepository : IDisposable :24
   ```

3. **Jump to code**
   Line number (`:24`) tells you exactly where to look in the source file.

## Workflow 3: Pre-Task Context

Before implementing a feature, load relevant API surface:

1. **Generate maps**
   ```bash
   codemapper .
   ```

2. **Load relevant project(s)**
   ```bash
   cat ./codebase_ast/Yubico.YubiKit.Core.txt
   ```

3. **Use context for implementation**
   Now you know what interfaces exist, their methods, constructor dependencies, etc.

## What the Maps Show

| Element | Example |
|---------|---------|
| **Namespace** | `[Namespace] Yubico.YubiKit.Core :15` |
| **Class** | `[Class] DeviceChannel : IDeviceChannel, IDisposable :27` |
| **Interface** | `[Interface] IDeviceRepository : IDisposable :24` |
| **Method** | `[Method] Task<IReadOnlyList<IYubiKey>> FindAllAsync(...) :91` |
| **Property** | `[Property] IObservable<DeviceEvent> DeviceChanges :26` |
| **Constructor** | `[Constructor] DeviceChannel(ILogger logger) :30` |
| **Static** | `[Class:static]` or `[Method:static]` |
| **Enum** | `[Enum] Transport { None, Usb, Nfc, All } [Flags] :17` |
| **Doc comment** | `// First sentence of XML summary` |

## Example: Understanding Device Discovery

**Scenario:** Need to understand how device discovery works.

```bash
# Generate and search
codemapper . && grep -rn "Device" ./codebase_ast/Yubico.YubiKit.Core.txt | head -20
```

**Output:**
```
[Interface] IDeviceRepository : IDisposable :24
  [Method] Task<IReadOnlyList<IYubiKey>> FindAllAsync(...) :27
[Class] DeviceRepositoryCached : IDeviceRepository :31
[Class] DeviceMonitorService : BackgroundService :25
[Class] DeviceEvent :26
  [Property] IYubiKey? Device :28
  [Property] DeviceAction Action :29
```

**Insight:** Now you know the key players - `IDeviceRepository`, `DeviceRepositoryCached`, `DeviceMonitorService`, and their relationships.

## Project-Specific Maps

This codebase generates maps for:

| Project | Content |
|---------|---------|
| `Yubico.YubiKit.Core.txt` | Device discovery, transport, logging, DI |
| `Yubico.YubiKit.Fido2.txt` | WebAuthn/FIDO2 protocol |
| `Yubico.YubiKit.Piv.txt` | PIV smart card operations |
| `Yubico.YubiKit.*.UnitTests.txt` | Test patterns and fixtures |
| `Yubico.YubiKit.*.IntegrationTests.txt` | Integration test structure |

## CLI Options

```bash
codemapper <path> [options]

Options:
  --format <text|json>    Output format (default: text)
  --output <dir>          Output directory (default: ./codebase_ast)
```

Use `--format json` for programmatic parsing.

## Common Mistakes

**❌ Using stale maps:** Don't assume `./codebase_ast/` is current
**✅ Always regenerate:** `codemapper .` takes ~1.5 seconds

**❌ Searching implementation:** Maps show signatures, not code bodies
**✅ Use grep on source:** For string literals or logic, grep the actual `.cs` files

**❌ Loading all maps:** Context overflow from 4000+ lines
**✅ Load targeted:** Only the project(s) relevant to your task

## Verification

Skill completed successfully when:
- Maps regenerated (check timestamp or summary line)
- Relevant symbol/type found with file and line number
- Enough context loaded to proceed with task

## Related Skills

- `workflow-brainstorm` - Use codemapper first, then brainstorm with context
- `workflow-plan` - Include relevant AST in planning
- `domain-yubikit-compare` - Map C# side before comparing to Java
