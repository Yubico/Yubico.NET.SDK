# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is the .NET YubiKey SDK - an enterprise-grade cross-platform SDK for YubiKey integration built on .NET. The project provides official .NET libraries for interacting with YubiKey hardware authenticators on Windows, macOS, and Linux.

## Architecture

The SDK is organized into two main assemblies:

### Yubico.Core (Platform Abstraction Layer)
- Located in `Yubico.Core/`
- Provides OS-specific functionality abstraction
- Device enumeration and HID device interaction
- Utility classes for encoding/decoding (Base16, Base32, TLV, ModHex)
- **Internal dependency only** - should not be consumed directly by end users

### Yubico.YubiKey (Main SDK)
- Located in `Yubico.YubiKey/`
- Primary assembly for YubiKey interaction
- Application-specific functionality (PIV, OATH, FIDO2, U2F, OTP, YubiHSM Auth)
- Depends on Yubico.Core
- **Public API** - this is what developers consume

### Yubico.NativeShims
- Located in `Yubico.NativeShims/`
- Unmanaged library providing stable ABI for P/Invoke operations
- **Internal use only** - not for public consumption

## Common Development Commands

### Building the Project
```bash
# Build entire solution
dotnet build Yubico.NET.SDK.sln --configuration Debug

# Build for Release
dotnet build Yubico.NET.SDK.sln --configuration Release

# Create NuGet packages
dotnet pack Yubico.NET.SDK.sln --configuration Release
```

### Running Tests
```bash
# Run all unit tests
dotnet test Yubico.Core/tests/Yubico.Core.UnitTests.csproj
dotnet test Yubico.YubiKey/tests/unit/Yubico.YubiKey.UnitTests.csproj

# Run integration tests (requires YubiKey devices)
dotnet test Yubico.YubiKey/tests/integration/Yubico.YubiKey.IntegrationTests.csproj

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Documentation
```bash
# Build documentation (requires docfx)
dotnet tool install --global docfx --version "2.*"
docfx docfx.json --logLevel warning --warningsAsErrors

# Build docs from YubiKey project specifically
dotnet msbuild Yubico.YubiKey/src/Yubico.YubiKey.csproj /t:DocFXBuild
```

### PowerShell Build Scripts
The project includes PowerShell build functions in `build/build.ps1`:
```powershell
# Import build functions
. ./build/build.ps1

# Build projects
Build-YubiKeySdkProject

# Run tests
Test-YubiKeySdkProject

# Clear build cache
Clear-YubiKeySdkCache
```

## Target Frameworks
- .NET Framework 4.7
- .NET Standard 2.0
- .NET Standard 2.1
- .NET 6 and above

## Git Workflow
This project uses **Gitflow**:
- `main` - official release history
- `develop` - integration branch (default for PRs)
- `feature/*` - new functionality branches (from develop)
- `bugfix/*` - bug fix branches (from develop)
- `release/*` - release preparation (from develop)
- `hotfix/*` - production fixes (from main)

### Branch Naming Conventions
- Features: `feature/issue-123-short-description`
- Bug fixes: `bugfix/issue-321-short-description`
- Use lowercase to avoid case sensitivity issues

## Testing Strategy

### Test Projects
- **Unit Tests**: `Yubico.Core.UnitTests`, `Yubico.YubiKey.UnitTests`
- **Integration Tests**: `Yubico.YubiKey.IntegrationTests` (requires physical YubiKeys)
- **Test Utilities**: `Yubico.YubiKey.TestUtilities`
- **Sandbox**: `Yubico.YubiKey.TestApp`

### Standard Test Devices
Integration tests use standardized YubiKey devices enumerated in `StandardTestDevice`:
- Fw3 (Major version 3, USB A, not FIPS)
- Fw4Fips (Major version 4, USB A, FIPS)
- Fw5 (Major version 5, USB A, not FIPS)
- Fw5Fips (Major version 5, USB A, FIPS)
- Fw5ci (Major version 5, USB C Lightning, not FIPS)

## Code Quality & Style

### Required Before PR Submission
1. Build the solution without errors/warnings
2. Run unit tests (all must pass)
3. Build documentation to check for doc errors
4. Add XML documentation for all public APIs
5. Write unit/integration tests for new functionality

### Documentation Requirements
- All public APIs must have XML documentation
- Use DocFX for API reference generation
- Follow existing patterns in code documentation

### Code Review Process
- Minimum 2 reviewers required
- Include area experts or original file authors
- All feedback threads must be resolved before merge
- Use specific, actionable feedback

## Key Directories

- `Yubico.Core/src/` - Core library source code
- `Yubico.YubiKey/src/` - Main SDK source code
- `Yubico.YubiKey/examples/` - Sample applications
- `docs/` - Documentation source files
- `build/` - Build configuration and scripts
- `contributordocs/` - Developer documentation
- `.github/workflows/` - CI/CD pipeline definitions

## NuGet Package Dependencies

### Yubico.Core
- Microsoft.Extensions.Logging.Abstractions
- Microsoft.Extensions.Configuration.Json
- System.Memory
- PolySharp (build-time)
- Yubico.NativeShims (internal)

### Yubico.YubiKey
- Microsoft.Bcl.AsyncInterfaces
- System.Formats.Cbor
- Yubico.Core (project reference)

## Important Notes

- Strong-name signed assemblies using `Yubico.NET.SDK.snk`
- Multi-targeting for broad .NET compatibility
- Internal visibility configured for test assemblies
- FIPS-compliant cryptographic implementations available
- Extensive integration test coverage requiring physical devices