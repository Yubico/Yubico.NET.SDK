---
name: build-project
description: REQUIRED for building/compiling .NET code - NEVER use dotnet build directly
---

# Building .NET Projects

## Overview

Build the Yubico.NET.SDK using the custom `toolchain.cs` script with Bullseye task runner.

**Core principle:** Always use `dotnet toolchain.cs build` - never `dotnet build` directly.

## Use when

**MANDATORY - use this skill when:**
- Building the solution or specific projects
- Restoring NuGet dependencies
- Cleaning build artifacts
- Creating or publishing NuGet packages

**NEVER use `dotnet build`, `dotnet restore`, or `dotnet pack` directly.**

**Don't use when:**
- Running tests (use `test-project` skill instead)
- Running tests with coverage (use `test-project` skill instead)

## Core Command

```bash
dotnet toolchain.cs build [options]
```

## Available Targets

| Target | Description | Dependencies |
|--------|-------------|--------------|
| `clean` | Remove artifacts directory | None |
| `restore` | Restore NuGet dependencies | clean |
| `build` | Build the solution or specific project | restore |
| `pack` | Create NuGet packages | build |
| `setup-feed` | Configure local NuGet feed | None |
| `publish` | Publish packages to local feed | pack, setup-feed |

## Key Options

| Option | Description |
|--------|-------------|
| `--project <name>` | Build specific project (partial match, e.g., `Piv`) |
| `--clean` | Run `dotnet clean` before build |
| `--package-version <version>` | Override package version |
| `--dry-run` | Show what would be published without publishing |
| `--include-docs` | Include XML documentation in packages |

## Common Workflows

### Build Everything

```bash
dotnet toolchain.cs build
```

### Build Specific Project

```bash
# Partial match on project name
dotnet toolchain.cs build --project Piv
dotnet toolchain.cs build --project Fido2
```

### Clean Build from Scratch

```bash
dotnet toolchain.cs build --clean
```

### Create Packages

```bash
# Default version from project files
dotnet toolchain.cs pack

# Custom version
dotnet toolchain.cs publish --package-version 1.0.0-preview.1

# Dry run to verify
dotnet toolchain.cs publish --dry-run
```

## Project Discovery

The build script automatically discovers:
- **Packable projects**: `Yubico.YubiKit.*/src/*.csproj`

To see discovered projects:
```bash
dotnet toolchain.cs -- --help
```

## Output Locations

| Output | Location |
|--------|----------|
| Packages | `artifacts/packages/*.nupkg` |
| Local NuGet feed | `artifacts/nuget-feed/` |

## The `--` Separator

Use `--` when arguments might conflict with dotnet's own options:

```bash
# REQUIRED for --help
dotnet toolchain.cs -- --help

# Optional but safer when using multiple options
dotnet toolchain.cs -- build --project Piv --clean
```

## Build Execution Time

For AI tools using bash with `mode="sync"`:
- Building: set `initial_wait` to 60 seconds
- Full solution: may take 30-60 seconds

## Project Name Matching

The `--project` option uses partial, case-insensitive matching:
- `--project Piv` matches `Yubico.YubiKit.Piv`
- `--project Fido` matches `Yubico.YubiKit.Fido2`

If no match is found, the script lists available projects.

## Troubleshooting

### Build Failures

```bash
# Clean and rebuild
dotnet toolchain.cs clean
dotnet toolchain.cs build --clean
```

### Package Issues

```bash
# Dry run to verify
dotnet toolchain.cs publish --dry-run

# Clean and repack
dotnet toolchain.cs clean
dotnet toolchain.cs pack
```

## Verification

Build completed successfully when:
- [ ] Exit code is 0
- [ ] No build errors in output
- [ ] No warnings (or only expected warnings)

## Related Skills

- `test-project` - For running tests and coverage
