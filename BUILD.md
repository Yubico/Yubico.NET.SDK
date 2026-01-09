# Build Script

This project uses a .NET 10 C# script for build automation with Bullseye task runner.

## Prerequisites

- .NET 10 SDK

## Usage

Run targets with:
```bash
dotnet build.cs [target] [options]
```

### When to Use `--` Separator

The `--` separator tells `dotnet run` to pass arguments to the script instead of interpreting them:

```bash
# These work WITHOUT -- (target names and most options)
dotnet build.cs build
dotnet build.cs test --project Piv
dotnet build.cs build --clean

# These REQUIRE -- (options that dotnet run might intercept)
dotnet build.cs -- --help          # --help conflicts with dotnet's help
dotnet build.cs -- -h              # Same issue

# When in doubt, use -- before any options
dotnet build.cs -- build --project Piv --clean
```

**Rule of thumb:** If your command isn't working as expected, try adding `--` before the arguments.

### Available Targets

- **clean** - Remove artifacts directory (and optionally run `dotnet clean`)
- **restore** - Restore NuGet dependencies (depends on: clean)
- **build** - Build the solution (depends on: clean, restore)
- **test** - Run unit tests with nice summary output (depends on: clean, restore, build)
- **coverage** - Run tests with code coverage collection (depends on: clean, restore, build)
- **pack** - Create NuGet packages (depends on: clean, restore, build)
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
- `--project <name>` - Build/test specific project only (partial match, e.g., `Piv`)
- `-h, --help` - Show help message (use `dotnet build.cs -- --help`)

### Examples

```bash
# Show help (requires -- to avoid dotnet intercepting --help)
dotnet build.cs -- --help

# Clean artifacts
dotnet build.cs clean

# Build the solution
dotnet build.cs build

# Build specific project (partial match)
dotnet build.cs build --project Piv

# Run tests
dotnet build.cs test

# Run tests for specific project with filter
dotnet build.cs test --project Piv --filter "Method~Sign"

# Run tests with code coverage
dotnet build.cs coverage

# Create and publish packages with custom version
dotnet build.cs publish --package-version 1.0.0-preview.2

# Dry run to see what would be published
dotnet build.cs publish --dry-run

# Full clean build
dotnet build.cs build --clean
```

## Target Dependencies

```
default
├── test
│   └── build
│       └── restore
│           └── clean
└── publish
    ├── pack
    │   └── build (shared)
    └── setup-feed
```

## Output

- **Packages**: `artifacts/packages/*.nupkg`
- **Coverage reports**: `artifacts/coverage/**/coverage.cobertura.xml`
- **Local NuGet feed**: `artifacts/nuget-feed/`

## Analyzers and Formatting

- Run `dotnet format` (or `dotnet format --verify-no-changes` in CI) to apply analyzer-driven fixes and ensure the workspace matches the shared `.editorconfig` rules.
- Analyzer configuration details live in `docs/DEV-GUIDE.md`; review that guide before introducing new rules or suppressions.

## Project Discovery

The build script automatically discovers projects using glob patterns:

- **Packable projects**: All `Yubico.YubiKit.*/src/*.csproj` files
- **Test projects**: All `Yubico.YubiKit.*.UnitTests/*.csproj` files under `tests/` directories

This means you don't need to manually update the build script when adding new projects that follow the standard structure. Run `dotnet build.cs -- --help` to see the current list of discovered projects.
