# Build Script

This project uses a .NET 10 C# script for build automation with Bullseye task runner.

## Prerequisites

- .NET 10 SDK
- Bash for `docs-inventory` and `docs-architecture` (macOS/Linux, Git Bash, or WSL)

## Usage

Run targets with:
```bash
dotnet toolchain.cs [target]
dotnet toolchain.cs -- [target] [options]
```

### When to Use `--` Separator

The `--` separator tells the .NET file runner to pass arguments to the script instead of interpreting them. It is required before every script long option, including `--project`; otherwise the runner can reject the command because it sees both `--project` and the script file.

```bash
# These work WITHOUT -- (target names only)
dotnet toolchain.cs build

# These REQUIRE -- (every script long option)
dotnet toolchain.cs -- --help          # --help conflicts with dotnet's help
dotnet toolchain.cs -- -h              # Same issue
dotnet toolchain.cs -- test --project Piv
dotnet toolchain.cs -- build --clean

# Put -- before the target whenever the script receives a long option
dotnet toolchain.cs -- build --project Piv --clean
```

**Rule:** Use `dotnet toolchain.cs -- <target> <script-long-options>` for any script `--option`. Target-only commands may omit the separator.

### Available Targets

- **clean** - Remove artifacts directory (and optionally run `dotnet clean`); must be specified explicitly
- **restore** - Restore NuGet dependencies
- **build** - Build the solution (depends on: restore)
- **test** - Run unit tests with nice summary output (depends on: restore, build)
- **docs-qa** - Validate active documentation hygiene
- **docs-list-active** - Print the exact active documentation set used by `docs-qa`
- **docs-inventory** - Generate the report-only active documentation inventory
- **docs-architecture** - Validate architecture diagram evidence map and rendered-image freshness
- **coverage** - Run tests with code coverage collection (depends on: restore, build)
- **pack** - Create NuGet packages (depends on: restore, build)
- **setup-feed** - Configure local NuGet feed
- **publish** - Publish packages to local feed (depends on: pack, setup-feed)
- **default** - Run tests and publish (depends on: test, publish)

### Options

- `--package-version <version>` - Override package version (e.g., `1.2.3-preview.1`)
- `--nuget-feed-name <name>` - NuGet feed name (default: `Yubico.YubiKit-LocalNuGet`)
- `--nuget-feed-path <path>` - NuGet feed directory (default: `artifacts/nuget-feed`)
- `--include-docs` - Include XML documentation in packages
- `--dry-run` - Show what would be published without actually publishing
- `--clean` - Run `dotnet clean` before build
- `--filter <expression>` - Test filter expression (e.g., `"FullyQualifiedName~MyTest"`)
- `--project <name>` - Build/test specific project only (partial match, e.g., `Piv`; requires the preceding `--` separator)
- `--integration` - Include integration tests (requires `--project`)
- `--smoke` - Smoke test mode: skip `Slow` and `RequiresUserPresence` tests (fast integration runs)
- `-h, --help` - Show help message (use `dotnet toolchain.cs -- --help`)

### Examples

```bash
# Show help (requires -- to avoid dotnet intercepting --help)
dotnet toolchain.cs -- --help

# Clean artifacts
dotnet toolchain.cs clean

# Build the solution
dotnet toolchain.cs build

# Build specific project (partial match)
dotnet toolchain.cs -- build --project Piv

# Run tests
dotnet toolchain.cs test

# Validate active documentation hygiene
dotnet toolchain.cs docs-qa

# Generate report-only active documentation inventory
dotnet toolchain.cs -- docs-inventory

# Run tests for specific project with filter
dotnet toolchain.cs -- test --project Piv --filter "Method~Sign"

# Run tests with code coverage (xUnit v2 unit test projects only)
dotnet toolchain.cs coverage

# Run integration tests for a specific module
dotnet toolchain.cs -- test --integration --project Piv

# Quick smoke test (skips slow RSA keygen and user-presence tests)
dotnet toolchain.cs -- test --integration --project Piv --smoke

# Create and publish packages with custom version
dotnet toolchain.cs -- publish --package-version 1.0.0-preview.2

# Dry run to see what would be published
dotnet toolchain.cs -- publish --dry-run

# Full clean build (delete artifacts, then build)
dotnet toolchain.cs clean build
```

## Target Dependencies

```
default
├── test
│   └── build
│       └── restore
└── publish
    ├── pack
    │   └── build (shared)
    └── setup-feed

clean  (standalone — must be specified explicitly)
```

## Output

- **Packages**: `artifacts/packages/*.nupkg`
- **Coverage reports**: `artifacts/coverage/**/coverage.cobertura.xml`
- **Local NuGet feed**: `artifacts/nuget-feed/`

## Analyzers and Formatting

- Scope `dotnet format` to your staged files via `--include` (never the whole solution for routine work) — see `CLAUDE.md` Pre-Commit Checklist for the exact command. In CI, or when a repo-wide `.editorconfig`/analyzer rule changes, run it unscoped with `--verify-no-changes`.
- Analyzer configuration details live in `docs/DEV-GUIDE.md`; review that guide before introducing new rules or suppressions.

## Documentation QA

Run `dotnet toolchain.cs docs-qa` to validate bounded active documentation hygiene.

The target scans:

- root `*.md` files
- top-level `docs/*.md`
- `docs/usage/**`, `docs/troubleshooting/**`, and `docs/architecture/**`
- module `src/**/README.md` and `src/**/CLAUDE.md`

It intentionally excludes archived or planning material under `docs/archive`, `docs/completed`, `docs/plans`, `docs/research`, `docs/reviews`, `docs/specs`, and `docs/templates`.

Current checks:

- fenced code blocks are balanced
- local markdown links outside fenced code examples resolve to existing files or directories
- stale FIDO2 user-presence trait examples that do not use `Category=RequiresUserPresence` are rejected

Snippet compilation is not part of this target. README examples are treated as documentation samples whose local links and fences must stay valid; compile-time snippet validation needs a separate approved phase.

Use `dotnet toolchain.cs -- docs-list-active` to print the exact active documentation set consumed by `docs-qa`. Use `dotnet toolchain.cs -- docs-inventory` to regenerate `docs/docs-inventory-report.md`; the inventory is report-only triage input and must not be treated as an auto-rewrite instruction.

## Project Discovery

The build script automatically discovers projects using glob patterns:

- **Packable projects**: All `Yubico.YubiKit.*/src/*.csproj` files
- **Test projects**: All `Yubico.YubiKit.*.UnitTests/*.csproj` files under `tests/` directories

This means you don't need to manually update the build script when adding new projects that follow the standard structure. Run `dotnet toolchain.cs -- --help` to see the current list of discovered projects.

## Code Coverage

The `coverage` target collects coverage via `dotnet test` with the `coverlet` collector. Both xUnit v2 and v3 (MTP) projects are supported via the VSTest compatibility layer. Use `--project` to run coverage for a specific module.

## xUnit v2 vs v3 Test Runner Detection

**IMPORTANT: Always use `dotnet toolchain.cs test` instead of invoking `dotnet test` directly.**

This codebase uses a mix of xUnit v2 and xUnit v3 test projects, which require different command-line invocation:

| Runner | Detection | Command | Filter Syntax |
|--------|-----------|---------|---------------|
| **xUnit v3** (Microsoft.Testing.Platform) | `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>` in .csproj | `dotnet run --project <proj>` | `-- --filter "..."` |
| **xUnit v2** (traditional) | No such setting | `dotnet test <proj>` | `--filter "..."` |

### Why This Matters

If you invoke `dotnet test` on an xUnit v3 project, or use the wrong filter syntax, the tests will fail with confusing errors. The build script automatically detects which runner each project uses and invokes the correct command.

### Examples

```bash
# ✅ CORRECT - Let the build script handle runner detection
dotnet toolchain.cs test
dotnet toolchain.cs -- test --project Core
dotnet toolchain.cs -- test --filter "FullyQualifiedName~MyTest"

# ❌ WRONG - May fail if project uses xUnit v3
dotnet test Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Yubico.YubiKit.Fido2.UnitTests.csproj
```

### Test Filtering Tips

- **Always combine `--project` with `--filter`** to avoid building and running all test projects:
  ```bash
  # ✅ Fast — only builds and runs WebAuthn tests
  dotnet toolchain.cs -- test --project WebAuthn --filter "FullyQualifiedName~PreviewSign"

  # ⚠️ Slow — builds ALL test projects, runs filter against each (most find 0 matches)
  dotnet toolchain.cs -- test --filter "FullyQualifiedName~PreviewSign"
  ```
- Filter syntax: `FullyQualifiedName~Substring`, `Method~Name`, `Category!=Slow`
- The toolchain auto-translates VSTest filter expressions to xUnit v3 native options (`--filter-method`, `--filter-trait`, etc.)
- It also normalises `Method~` / `Name~` to `FullyQualifiedName~` for the xUnit v2 (VSTest) projects, which have no `Method` property. Without that, `Method~Sign` matches **zero** tests on every integration project while working fine on the unit projects. `FullyQualifiedName~` is the precise form if you want to be explicit
- **A filter matching no tests is an error, not a pass.** The toolchain preflights both runners and fails with `No tests matched the specified filter`. Left unguarded, VSTest prints `No test matches the given testcase filter` and still exits `0`, which previously surfaced as `✓ All tests passed` from a run that never happened

### For AI Agents / Automation

When writing scripts or automation that runs tests:

1. **Always use `dotnet toolchain.cs test`** - it handles the complexity for you
2. **Never assume** `dotnet test` will work for all projects
3. **Use `--project`** to filter to specific projects: `dotnet toolchain.cs -- test --project Fido2`
4. **Combine `--project` with `--filter`** for targeted test runs: `dotnet toolchain.cs -- test --project Fido2 --filter "Method~Sign"`
5. **Read the per-project `total:` line, not the final summary line.** The closing
   `Passed: 1 | Failed: 0 | Skipped: 1 | Total: 2` counts **projects**, not tests. Grepping for
   `Passed:` therefore reads green off a run that executed nothing. Assert on the per-project
   `total:` / `Total tests:` figure instead:

   ```bash
   dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~FidoHidProtocol" \
     | grep -E "total:|failed:"
   ```

   A zero-match filter now fails the target outright, but the project-vs-test counting still
   catches people out when a filter matches fewer tests than intended.
6. **Add `--smoke` to any unattended hardware run.** It skips `Slow` and `RequiresUserPresence`,
   so a missing human cannot leave a ceremony parked and wedge the key. Be aware it can thin
   coverage sharply — on WebAuthn it reduces the integration lane to a single SmartCard test, so
   check the `total:` figure before treating a smoke pass as meaningful validation.
