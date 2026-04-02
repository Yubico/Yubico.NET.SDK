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

## Integration Test Strategy

Integration tests require a physical YubiKey and can be slow (especially RSA keygen). Follow this tiered approach:

### During Development

Run integration tests **only for the module you changed**:

```bash
# Quick smoke test — skips slow keygen and user-presence tests
dotnet build.cs -- test --integration --project Piv --smoke

# Targeted test for a specific method you touched
dotnet build.cs -- test --integration --project Oath --filter "FullyQualifiedName~CalculateAll"
```

### When Finishing a Module

Run the **full integration suite** for the affected module (no `--smoke`):

```bash
dotnet build.cs -- test --integration --project Piv
```

### Before PR / Final Validation

Run full integration for all affected modules. You do **not** need to run all modules unless changes touch Core or shared infrastructure.

### What `--smoke` Skips

The `--smoke` flag excludes tests with these traits:
- **`Slow`** — RSA 3072/4096 key generation (30+ seconds each), long delays
- **`RequiresUserPresence`** — Tests needing physical touch or device insert/remove

This typically cuts PIV integration time from ~4 minutes to under 1 minute.

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

### xUnit v2 vs v3 Filter Differences

When running filtered tests **outside** the build script (ad-hoc debugging), syntax differs by xUnit version:

| Version | Detection | Filter Syntax |
|---------|-----------|---------------|
| xUnit v2 | No `UseMicrosoftTestingPlatformRunner` | `--filter "FullyQualifiedName~TestName"` |
| xUnit v3 | Has `UseMicrosoftTestingPlatformRunner` | `-m TestName` or `--method TestName` |

**Check version:** Look for `<PackageReference Include="xunit"` in the `.csproj`:
- `3.x.x` → xUnit v3 syntax
- `2.x.x` → xUnit v2 syntax

**Recommendation:** Use `dotnet build.cs test --filter "..."` which handles this automatically.

### Standard Filter Expressions

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

## xUnit v3 Known Limitations

### `[WithYubiKey]` + `[InlineData]` Incompatibility

The `[WithYubiKey]` attribute (used for integration tests requiring physical YubiKeys) is **incompatible** with `[InlineData]` parameterized tests.

**Problem:** When you combine `[WithYubiKey]` with `[Theory]` and `[InlineData]`, xUnit v3 fails to properly inject the `YubiKeyTestState` parameter alongside inline data parameters.

```csharp
// ❌ WRONG - Does not work
[WithYubiKey(MinFirmware = "5.7.0")]
[Theory]
[InlineData(PivAlgorithm.Rsa3072)]
[InlineData(PivAlgorithm.Rsa4096)]
public async Task SignAsync_LargeRsa_Works(PivAlgorithm algorithm, YubiKeyTestState state)
{
    // This will fail - state won't be injected correctly
}

// ✅ CORRECT - Use separate tests
[WithYubiKey(MinFirmware = "5.7.0")]
public async Task SignAsync_Rsa3072_Works(YubiKeyTestState state) { /* ... */ }

[WithYubiKey(MinFirmware = "5.7.0")]
public async Task SignAsync_Rsa4096_Works(YubiKeyTestState state) { /* ... */ }
```

**Workaround:** Split parameterized tests into separate test methods, one per parameter combination.

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

---

## Test Traits and Categories

Tests are categorized using xUnit traits to enable filtering. Use the `TestCategories` constants from `Yubico.YubiKit.Tests.Shared.Infrastructure`.

### Available Categories

| Category | Constant | Description |
|----------|----------|-------------|
| `RequiresHardware` | `TestCategories.RequiresHardware` | Test needs a physical YubiKey connected |
| `RequiresUserPresence` | `TestCategories.RequiresUserPresence` | Test needs user interaction (insert/remove/touch) |
| `Slow` | `TestCategories.Slow` | Test takes >5 seconds (delays, performance tests) |
| `Integration` | `TestCategories.Integration` | Test exercises multiple components |
| `RequiresFirmware` | `TestCategories.RequiresFirmware` | Test needs specific firmware features |

### How to Apply Traits

```csharp
using Yubico.YubiKit.Tests.Shared.Infrastructure;

public class MyTests
{
    // Test requires hardware (device must be connected, but runs automatically)
    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task FindAllAsync_ReturnsDevice() { }

    // Test requires user to insert/remove device (cannot run in CI/agents)
    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task DeviceChanges_DetectsRemoval() { }

    // Slow test with long delays
    [Fact]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task Performance_ManyOperations() { }
}
```

### Filtering Tests by Category

```bash
# Skip tests requiring user interaction (for CI/agents)
dotnet build.cs test --filter "Category!=RequiresUserPresence"

# Skip slow tests
dotnet build.cs test --filter "Category!=Slow"

# Skip hardware tests (run only unit tests)
dotnet build.cs test --filter "Category!=RequiresHardware"

# Run only fast unit tests (no hardware, no user presence, not slow)
dotnet build.cs test --filter "Category!=RequiresHardware&Category!=RequiresUserPresence&Category!=Slow"
```

### When to Apply Each Trait

**`RequiresHardware`:**
- Tests that call `YubiKeyManager.FindAllAsync()` expecting results
- Tests that open connections to devices
- Tests that send APDU commands

**`RequiresUserPresence`:**
- Tests waiting for device insertion/removal events
- Tests requiring touch for user presence verification
- Tests that prompt for PIN entry via physical interaction
- **AI agents cannot run these tests** - they require human interaction

**`Slow`:**
- Tests with `Task.Delay()` > 5 seconds
- Performance benchmark tests
- Tests waiting for timeout conditions

### AI Agent Guidelines

**When writing new tests, agents MUST apply appropriate traits:**

1. If the test calls `YubiKeyManager.FindAllAsync()` or opens device connections:
   → Add `[Trait(TestCategories.Category, TestCategories.RequiresHardware)]`

2. If the test waits for device insertion/removal or requires touch:
   → Add `[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]`
   → Add `[Trait(TestCategories.Category, TestCategories.Slow)]`

3. If the test has intentional delays > 5 seconds:
   → Add `[Trait(TestCategories.Category, TestCategories.Slow)]`

**Agents should skip `RequiresUserPresence` tests** when running test suites:
```bash
dotnet build.cs test --filter "Category!=RequiresUserPresence"
```