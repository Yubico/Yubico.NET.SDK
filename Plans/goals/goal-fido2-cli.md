# GOAL: Add CLI Tool for FIDO2 Applet in Yubico.NET.SDK

## Context

This is the Yubico.NET.SDK (YubiKit), a .NET 10 / C# 14 SDK for YubiKey devices. The **FIDO2 applet is already fully implemented** (44 source files). This task ONLY adds a CLI tool. This is a 2.0 effort on `yubikit-*` branches -- do NOT touch `develop` or `main`.

**IMPORTANT:** Do NOT modify any existing FIDO2 source files. Only ADD the `examples/FidoTool/` directory.

## MANDATORY: Read These Files First

Before writing ANY code, you MUST read and internalize these files line by line:

1. **`CLAUDE.md`** (repository root) - All coding standards, modern C# patterns, build/test
2. **`Yubico.YubiKit.Fido2/src/IFidoSession.cs`** - Understand ALL available FIDO2 operations
3. **`Yubico.YubiKit.Fido2/src/FidoSession.cs`** - Session implementation details
4. **`docs/TESTING.md`** - Test categories (user presence tests)

## MANDATORY: Study Existing CLI Tools

Study these for the EXACT structure to replicate:
- `Yubico.YubiKit.Management/examples/ManagementTool/` - ManagementTool (16 files, Spectre.Console)
- `Yubico.YubiKit.Piv/examples/PivTool/` (on `yubikit-piv` branch) - PivTool (36 files, Spectre.Console)

## CLI Tool (in `Yubico.YubiKit.Fido2/examples/FidoTool/`)

```
FidoTool/
├── FidoTool.csproj                    # References Fido2 project + Spectre.Console
├── Program.cs                         # FigletText banner + main menu loop
├── README.md
├── Cli/
│   ├── Output/OutputHelpers.cs        # Formatted output helpers
│   ├── Prompts/DeviceSelector.cs      # Device selection prompts
│   └── Menus/
│       ├── InfoMenu.cs                # Authenticator info display
│       ├── PinMenu.cs                 # PIN management (set, change, get retries)
│       ├── CredentialMenu.cs          # Make credential, get assertion
│       ├── CredentialMgmtMenu.cs      # Credential management (list RPs, list credentials, delete)
│       ├── BioMenu.cs                 # Bio enrollment (if supported)
│       ├── ConfigMenu.cs              # Authenticator config (if supported)
│       └── ResetMenu.cs              # Reset authenticator
└── FidoExamples/
    ├── GetAuthenticatorInfo.cs        # Query and display authenticator capabilities
    ├── MakeCredential.cs              # Create a new credential
    ├── GetAssertion.cs                # Get an assertion for existing credential
    ├── PinManagement.cs               # Set/change PIN, get retries
    ├── CredentialManagement.cs         # List/delete resident credentials
    ├── BioEnrollment.cs               # Fingerprint enrollment operations
    └── ResetAuthenticator.cs          # Factory reset
```

The CLI tool MUST support **command-line parameters** (not just interactive menus) so automated testing can drive it. Examples:
- `FidoTool info` - show authenticator information
- `FidoTool pin set --pin "12345678"` - set PIN
- `FidoTool pin change --old "12345678" --new "87654321"` - change PIN
- `FidoTool pin retries` - show PIN retry count
- `FidoTool credential make --rp "example.com" --user "test@example.com"` - make credential
- `FidoTool credential list` - list resident credentials
- `FidoTool reset` - factory reset

**IMPORTANT:** Many FIDO2 operations require **user presence** (touch). The CLI should:
- Clearly indicate when touch is needed (e.g., "Touch your YubiKey now...")
- Handle timeouts gracefully with informative messages
- Mark touch-requiring operations in help text

## Coding Standards Checklist

Every file MUST:
- [ ] Use file-scoped namespaces (`namespace Yubico.YubiKit.Fido2.Examples;` or similar)
- [ ] Use `is null` / `is not null` (NEVER `== null`)
- [ ] Use switch expressions (NEVER old switch statements)
- [ ] Use collection expressions `[..]`
- [ ] Use `readonly` on fields that don't change
- [ ] Use `{ get; init; }` for immutable properties
- [ ] Handle `CancellationToken` in all async methods
- [ ] Use `.ConfigureAwait(false)` on all awaits
- [ ] NO `#region`, NO `.ToArray()` unless data must escape scope
- [ ] Static logger: `LoggingFactory.CreateLogger<T>()` (NEVER inject ILogger)

## Anti-Patterns (FORBIDDEN)

- `== null` (use `is null`)
- `#region` (split large classes instead)
- `.ToArray()` in hot paths
- Injected `ILogger` (use static `LoggingFactory`)
- `dotnet test` (use `dotnet build.cs test`)
- `git add .` or `git add -A`
- Old switch statements
- Exceptions for control flow
- Nullable warnings suppressed with `!` without justification
- Modifying ANY existing FIDO2 source files

## Git

- Branch: `yubikit-fido2-cli` (already created for you)
- Commit messages: `feat(fido2): add FidoTool CLI example`
- NEVER use `git add .` or `git add -A` - add files explicitly
- NEVER modify existing FIDO2 source files

## Build & Test

```bash
dotnet build.cs build    # Must succeed with zero warnings
dotnet format            # Must produce no changes
```

## Definition of Done

1. FidoTool CLI builds without warnings
2. All available FIDO2 operations are exposed in the CLI
3. Command-line parameter support for automated testing
4. Follows PivTool/ManagementTool directory structure exactly
5. Clear user-presence prompts ("Touch your YubiKey now...")
6. Code follows all CLAUDE.md coding standards
7. No existing FIDO2 source files modified
8. No anti-patterns present
