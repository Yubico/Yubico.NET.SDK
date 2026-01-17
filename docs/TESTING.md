# Testing Guidelines

**CRITICAL: Read this before running any tests.**

## The #1 Rule

**ALWAYS use `dotnet build.cs test` - NEVER use `dotnet test` directly.**

This codebase uses a mix of xUnit v2 and xUnit v3 test projects that require different CLI invocations. The build script handles this automatically.

## Why This Matters

| Runner | Command | Filter Syntax |
|--------|---------|---------------|
| xUnit v3 (Microsoft.Testing.Platform) | `dotnet run --project <proj>` | `-- --filter "..."` |
| xUnit v2 (traditional) | `dotnet test <proj>` | `--filter "..."` |

If you use the wrong command or filter syntax, tests will fail with confusing errors like:
- "No test matches the given testcase filter"
- "The test run was aborted"
- Build succeeds but no tests run

## Correct Commands

```bash
# Run all tests
dotnet build.cs test

# Run tests for a specific module (partial match)
dotnet build.cs test --project Core
dotnet build.cs test --project Fido2
dotnet build.cs test --project Piv

# Run tests with a filter
dotnet build.cs test --filter "FullyQualifiedName~MyTestClass"
dotnet build.cs test --filter "Method~Sign"

# Combine project and filter
dotnet build.cs test --project Piv --filter "Method~Sign"
```

## Common Mistakes

```bash
# WRONG - May fail on xUnit v3 projects
dotnet test Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Yubico.YubiKit.Fido2.UnitTests.csproj

# WRONG - Filter syntax incompatible with xUnit v3
dotnet test --filter "FullyQualifiedName~MyTest"

# CORRECT - Always use the build script
dotnet build.cs test --project Fido2 --filter "FullyQualifiedName~MyTest"
```

## How Detection Works

The build script checks each test project's `.csproj` file for:
```xml
<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
```

- If present: xUnit v3 (Microsoft.Testing.Platform) - uses `dotnet run`
- If absent: xUnit v2 (traditional) - uses `dotnet test`

## Test Project Locations

Tests are organized per-module:
```
Yubico.YubiKit.Core/tests/Yubico.YubiKit.Core.UnitTests/
Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/
Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.UnitTests/
... etc
```

Run `dotnet build.cs -- --help` to see all discovered test projects.

## Filter Syntax Reference

```
FullyQualifiedName~MyClass     Tests containing 'MyClass' in full name
Name=MyTestMethod              Exact test method name
ClassName~Integration          Classes containing 'Integration'
Name!=SkipMe                   Exclude tests named 'SkipMe'
```

## Summary

1. **Always** use `dotnet build.cs test`
2. **Never** use `dotnet test` directly
3. Use `--project` for module filtering
4. Use `--filter` for test filtering
5. When in doubt, run `dotnet build.cs test` without filters first