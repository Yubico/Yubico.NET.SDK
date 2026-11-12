# B4 expansion and protected-output replacement whole units

**No content verdicts.** Pinned source `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; read complete units and relevant enclosing context.

## `core-001:unit:f5a358ec89b4c4ce`
MATERIAL-MISS-FULL-ORIGINAL-NARRATIVE-QUOTA; `src/Core/CLAUDE.md:6–13`; original rank 5.

````markdown
## Documentation Maintenance

> **Important:** This documentation is subject to change. When working on this module:
> - **Notable changes** to APIs, patterns, or behavior should be documented in both CLAUDE.md and README.md
> - **New features** (e.g., new protocol handlers, connection types) should include usage examples
> - **Breaking changes** require updates to both files with migration guidance
> - **Test infrastructure changes** should be reflected in the test pattern sections below


````

## `core-001:unit:9a180bff18d16922`
MATERIAL-MISS-FULL-ORIGINAL-NARRATIVE-QUOTA; `src/Core/PACKAGE_README.md:13–19`; original rank 6.

````markdown
## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Core --prerelease
```


````

## `core-001:unit:7070cc291aec3e7e`
MATERIAL-MISS-FULL-ORIGINAL-NARRATIVE-QUOTA; `src/Core/CLAUDE.md:1–5`; original rank 7.

````markdown
# CLAUDE.md - Core Module

This file provides module-specific guidance for working in **Yubico.YubiKit.Core**.
For overall repo conventions, see the repository root [CLAUDE.md](../../CLAUDE.md).


````

## `core-001:unit:db781bee61b48612`
MATERIAL-MISS-FULL-ORIGINAL-NARRATIVE-QUOTA; `CLAUDE.md:585–596`; original rank 8.

````markdown
## What NOT to Do

- ❌ String concatenation in loops — use `StringBuilder`
- ❌ Suppress nullable warnings with `!` without justification — use `?? throw`
- ❌ Exceptions for control flow — use `FirstOrDefault`, `TryGet`, etc.
- ❌ Public mutable state (`public byte[] Data;`) — use `{ get; init; }` or `ReadOnlyMemory<byte>`
- ❌ `#region` — split the class instead
- ❌ `var` when the type isn't obvious from the right-hand side
- ❌ `[DllImport]` / `extern` for a new native entry point — use `[LibraryImport]` + `static partial`

> Examples for each: `docs/CSHARP-PATTERNS.md` ("What NOT to Do" section).


````

## `core-001:unit:7dc8f2fc14f0186c`
PROTECTED-OUTPUT-ELIGIBILITY-REPLACEMENT; NOT A NEW RUNTIME FINDING; `nuget.config:1–20`; original rank 12.

````markdown
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <!-- Development NativeShims builds come from the private feed; released packages
       are available from nuget.org. Credentials belong in the environment or user
       configuration, never in this file. -->
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="Yubico_GH" value="https://nuget.pkg.github.com/Yubico/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="Yubico_GH">
      <package pattern="Yubico.NativeShims" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="Yubico.NativeShims" />
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>

````

