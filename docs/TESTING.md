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

---

## Multi-Transport Test Infrastructure

### Overview

The test infrastructure supports testing YubiKeys across multiple connection types (transports) automatically. Each physical YubiKey can present multiple transports (CCID, HidFido, HidOtp), and tests run independently on each transport.

### How It Works

When you write a test using `[WithYubiKey]`, the infrastructure:
1. Discovers all available devices and their transports
2. Creates a separate `YubiKeyTestState` for each transport
3. Runs your test once per transport per device

Example: One YubiKey with CCID and HidFido → test runs twice.

### ConnectionType Filtering

Use the `ConnectionType` property to filter which transports your test runs on:

```csharp
// Run on all transports (default)
[WithYubiKey]
public async Task MyTest(YubiKeyTestState state) { }

// Run only on CCID (SmartCard) connections
[WithYubiKey(ConnectionType = ConnectionType.Ccid)]
public async Task SmartCardOnly(YubiKeyTestState state) { }

// Run only on HidFido connections
[WithYubiKey(ConnectionType = ConnectionType.HidFido)]
public async Task FidoOnly(YubiKeyTestState state) { }
```

### Test Output Format

Test output shows the ConnectionType:
```
Passed MyTest(state: YubiKey(SN:12345678,FW:5.7.2,UsbAKeychain,Ccid))
Passed MyTest(state: YubiKey(SN:12345678,FW:5.7.2,UsbAKeychain,HidFido))
```

### Automatic Transport Selection

The `WithManagementAsync` helper automatically uses the correct transport from `state.ConnectionType`:

```csharp
[WithYubiKey]
public async Task MyTest(YubiKeyTestState state)
{
    await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
    {
        // mgmt uses the transport from state.ConnectionType automatically
        var deviceInfo = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(state.SerialNumber, deviceInfo.SerialNumber);
    });
}
```

### Transport-Specific Testing

Test different transports explicitly using `ConnectionType` filtering:

```csharp
// This test runs ONLY on CCID connections
[WithYubiKey(ConnectionType = ConnectionType.Ccid)]
public async Task SmartCard_Operations(YubiKeyTestState state)
{
    Assert.Equal(ConnectionType.Ccid, state.ConnectionType);
    await state.WithManagementAsync(async (mgmt, _) =>
    {
        // CCID-specific testing
    });
}

// This test runs on ALL available transports
[WithYubiKey]
public async Task AllTransports_Consistency(YubiKeyTestState state)
{
    // Verifies behavior is consistent across transports
    await state.WithManagementAsync(async (mgmt, _) =>
    {
        var info = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(state.SerialNumber, info.SerialNumber);
    });
}
```

### Best Practices

1. **Default to all transports**: Don't specify `ConnectionType` unless you have a transport-specific reason
2. **Avoid DeviceId parsing**: Use `ConnectionType` filtering instead of parsing `DeviceId` strings
3. **Use WithManagementAsync**: Let the infrastructure handle connection management
4. **Test consistency**: Write tests that verify behavior is consistent across transports when possible

### Migration from Old Patterns

**Old (fragile):**
```csharp
var devices = await YubiKeyManager.FindAllAsync(ConnectionType.Hid);
var fidoDevice = devices.FirstOrDefault(d => 
    d.DeviceId.Contains(":0001") || d.DeviceId.Contains(":F1D0"));
```

**New (robust):**
```csharp
var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
var fidoDevice = devices.FirstOrDefault();
```