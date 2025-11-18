# Build Script

This project uses a .NET 10 C# script for build automation with Bullseye task runner.

## Prerequisites

- .NET 10 SDK

## Usage

Run targets with:
```bash
dotnet build.cs [target] [options]
```

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
- `-h, --help` - Show help message with all targets, options, and discovered projects

### Examples

```bash
# Show help (use -- separator to pass args to script)
dotnet build.cs -- --help

# Clean artifacts
dotnet build.cs clean

# Build the solution
dotnet build.cs build

# Run tests
dotnet build.cs test

# Run tests with code coverage
dotnet build.cs coverage

# Create and publish packages with custom version
dotnet build.cs publish --package-version 1.0.0-preview.2

# Dry run to see what would be published
dotnet build.cs publish --dry-run

# Setup local NuGet feed
dotnet build.cs setup-feed

# Full clean build with tests
dotnet build.cs --clean
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

## Project Discovery

The build script automatically discovers projects using glob patterns:

- **Packable projects**: All `Yubico.YubiKit.*/src/*.csproj` files
- **Test projects**: All `Yubico.YubiKit.*.UnitTests/*.csproj` files under `tests/` directories

This means you don't need to manually update the build script when adding new projects that follow the standard structure. Run `dotnet build.cs -- --help` to see the current list of discovered projects.
