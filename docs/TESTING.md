# Testing Guidelines

**CRITICAL: Read this before running any tests.**

Canonical platform findings and simplification direction: [Testing Platform Findings](TESTING_PLATFORM_FINDINGS.md).

## The #1 Rule

**ALWAYS use `dotnet toolchain.cs test` - NEVER use `dotnet test` directly.**

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
dotnet toolchain.cs test

# Run tests for a specific module (partial match)
dotnet toolchain.cs -- test --project Core
dotnet toolchain.cs -- test --project Fido2
dotnet toolchain.cs -- test --project Piv

# Run tests with a filter
dotnet toolchain.cs -- test --filter "FullyQualifiedName~MyTestClass"
dotnet toolchain.cs -- test --filter "Method~Sign"

# Combine project and filter
dotnet toolchain.cs -- test --project Piv --filter "Method~Sign"
```

### Reading filter results

Two things routinely mislead people here, both worth knowing before you trust a green run.

**`Method~` is not a VSTest property.** The unit projects run xUnit v3 / Microsoft Testing
Platform; the integration projects run xUnit v2 under VSTest, where only `FullyQualifiedName`,
`DisplayName` and traits exist. The toolchain normalises `Method~` and `Name~` to
`FullyQualifiedName~` for those projects so one syntax works on both runners. `FullyQualifiedName~`
is the precise form if you would rather be explicit.

**A zero-match filter fails the target.** VSTest prints `No test matches the given testcase filter`
and still exits `0`, so an unguarded run reports `✓ All tests passed` having executed nothing. The
toolchain preflights both runners and fails with `No tests matched the specified filter` instead.

**The closing summary counts projects, not tests.** This line:

```
Passed: 1 | Failed: 0 | Skipped: 1 | Total: 2
```

means two *projects*. Grepping for `Passed:` will read green off a run of zero tests. Assert on the
per-project figure instead:

```bash
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~FidoHidProtocol" \
  | grep -E "total:|failed:"
```

## Integration Test Strategy

Integration tests require a physical YubiKey and can be slow (especially RSA keygen). Follow this tiered approach:

### During Development

Run integration tests **only for the module you changed**:

```bash
# Quick smoke test — skips slow keygen and user-presence tests
dotnet toolchain.cs -- test --integration --project Piv --smoke

# Targeted test for a specific method you touched
dotnet toolchain.cs -- test --integration --project Oath --filter "FullyQualifiedName~CalculateAll"
```

### When Finishing a Module

Run the **full integration suite** for the affected module (no `--smoke`):

```bash
dotnet toolchain.cs -- test --integration --project Piv
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
dotnet toolchain.cs -- test --project Fido2 --filter "FullyQualifiedName~MyTest"
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

Run `dotnet toolchain.cs -- --help` to see all discovered test projects.

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

**Recommendation:** Use `dotnet toolchain.cs -- test --filter "..."` which handles this automatically.

### xUnit v3 Focused Filters

`toolchain.cs` translates common VSTest-style filters to Microsoft.Testing.Platform-native xUnit v3 flags. Positive filters are preflighted with `--list-tests`; if a selected xUnit v3 project has no matching tests, that project is reported as `no matching tests`. If every selected xUnit v3 project preflighted by the positive filter has no matching tests, the toolchain fails clearly with `No tests matched the specified filter`. Exclusion filters still apply to the actual run.

This matters when a project filter selects multiple FIDO2/WebAuthn test projects and only one contains the focused method.

### Standard Filter Expressions

```
FullyQualifiedName~MyClass     Tests containing 'MyClass' in full name
Name=MyTestMethod              Exact test method name
ClassName~Integration          Classes containing 'Integration'
Name!=SkipMe                   Exclude tests named 'SkipMe'
```

## Summary

1. **Always** use `dotnet toolchain.cs test`
2. **Never** use `dotnet test` directly
3. Use `--project` for module filtering
4. Use `--filter` for test filtering
5. When in doubt, run `dotnet toolchain.cs test` without filters first

## Hardware Authorization

Integration tests only ever touch devices whose serial number is in the gitignored
`YubiKeyTests:AllowedSerialNumbers` list; anything else hard-fails with `Environment.Exit(-1)`
before a single hardware operation runs. Those listed keys are **dedicated test keys**.

That makes the authorization boundary simple, and worth stating plainly so it is not
re-litigated per run:

| Operation class | Authorized unattended? |
|---|---|
| State mutation, PIV reset, key generation on an allow-listed key | Yes — no per-run approval needed |
| User Presence / touch ceremonies | No — human must be present |
| User Verification, PIN, bio enrollment | No — human must approve and know device state |
| Insert / remove / power-cycle timing | No — human must coordinate |

The distinction is presence and timing, not destruction. There is no additional config gate:
the allow list is the boundary, and adding a second one would only obscure it.

## FIDO2/WebAuthn Hardware Coordination

FIDO2 and WebAuthn tests often need User Presence (touch), User Verification (PIN/bio), credential creation, or reset timing. These checks are not unattended agent gates.

Use these lanes:

| Lane | Examples | Agent-runnable? | Rule |
|------|----------|-----------------|------|
| Read-only smoke | `GetInfo`, construction/unit tests | Yes | Use `dotnet toolchain.cs test` or integration `--smoke` |
| User Presence | `MakeCredential`, `GetAssertion`, `previewSign` ceremonies | No by default | Mark with `Category=RequiresUserPresence`; run only with a human present |
| User Verification / PIN | PIN token, UV-required/preferred, bio enrollment | No by default | Requires explicit human approval and known device/PIN state |
| Destructive state change | Persistent credential deletion, PIV reset, key generation | Yes on an allow-listed test key | See [Hardware Authorization](#hardware-authorization); destruction alone is not the gate |
| FIDO2 reset | `authenticatorReset` | No | Needs the power-cycle window plus touch — timing, not destruction, is why |
| Insert/remove/touch timing | Reset power-cycle window, physical insertion/removal | No | Human-coordinated timing only |

Agent-safe FIDO2/WebAuthn integration commands must skip User Presence:

```bash
dotnet toolchain.cs -- test --integration --project Fido2 --smoke
dotnet toolchain.cs -- test --integration --project WebAuthn --smoke
dotnet toolchain.cs -- test --integration --project WebAuthn --filter "Category!=RequiresUserPresence"
```

Human-coordinated UP/UV commands require approval immediately before execution:

```bash
dotnet toolchain.cs -- test --integration --project Fido2 --filter "Category=RequiresUserPresence"
dotnet toolchain.cs -- test --integration --project WebAuthn --filter "Category=RequiresUserPresence"
```

## Hardware Test Infrastructure Limitations

### `[WithYubiKey]` + `[InlineData]` Incompatibility

The `[WithYubiKey]` attribute (used for integration tests requiring physical YubiKeys) is **incompatible** with `[InlineData]` parameterized tests.

**Problem:** `[WithYubiKey]` is a custom xUnit v2 `DataAttribute` that supplies the complete argument row. Combining it with `[InlineData]` creates separate, incomplete rows rather than merging the arguments.

```csharp
// ❌ WRONG - Does not work
[WithYubiKey(MinFirmware = "5.7.0")]
[SkippableTheory]
[InlineData(PivAlgorithm.Rsa3072)]
[InlineData(PivAlgorithm.Rsa4096)]
public async Task SignAsync_LargeRsa_Works(PivAlgorithm algorithm, YubiKeyTestState state)
{
    // This will fail - state won't be injected correctly
}

// ✅ CORRECT - Use separate tests
[SkippableTheory]
[WithYubiKey(MinFirmware = "5.7.0")]
public async Task SignAsync_Rsa3072_Works(YubiKeyTestState state) { /* ... */ }

[SkippableTheory]
[WithYubiKey(MinFirmware = "5.7.0")]
public async Task SignAsync_Rsa4096_Works(YubiKeyTestState state) { /* ... */ }
```

**Workaround:** Split parameterized tests into separate test methods, one per parameter combination.

---

## Multi-Transport Test Infrastructure

### Overview

One physical YubiKey can expose several connection types, such as SmartCard, HID FIDO, and HID OTP. The test infrastructure represents those connections on one `YubiKeyTestState`; `[WithYubiKey]` filters devices but does not automatically enumerate one test row per connection.

### How It Works

Each `[WithYubiKey]` attribute:

1. Creates one placeholder test row during discovery.
2. Binds that row to the first matching authorized device during execution.
3. Applies `ConnectionType` as a device filter; it does not open that connection or force a session to use it.

With an unfiltered `[WithYubiKey]`, `state.ConnectionType` is the device's complete `AvailableConnections` flag set and can contain several values. With a concrete filter such as `[WithYubiKey(ConnectionType = ConnectionType.HidFido)]`, `state.ConnectionType` records that requested connection type while `state.AvailableConnections` still reports the device's complete set.

To create separate rows for separate connection types, apply one attribute per concrete connection type. Each attribute still binds independently to a matching authorized device.

### ConnectionType Filtering

Use the `ConnectionType` property to require that the bound device exposes a connection type:

```csharp
// One device row; state.ConnectionType may contain multiple flags.
[SkippableTheory]
[WithYubiKey]
public async Task DefaultSelection(YubiKeyTestState state) { }

// One row whose device must expose SmartCard.
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
public async Task SmartCardOnly(YubiKeyTestState state) { }

// Three separate rows, one for each requested connection type.
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
[WithYubiKey(ConnectionType = ConnectionType.HidFido)]
[WithYubiKey(ConnectionType = ConnectionType.HidOtp)]
public async Task EachManagementConnection(YubiKeyTestState state) { }
```

The filter alone does not pin Management to that connection. Pin the session explicitly when the test's meaning depends on the connection used.

### Test Output Format

Test output includes `state.ConnectionType`. An unfiltered row can therefore display a combined flag set, while rows created by concrete `ConnectionType` filters display the requested single value. Multiple output rows come from multiple `[WithYubiKey]` attributes, not automatic per-interface enumeration.

### Default Management Selection

`WithManagementAsync` leaves `preferredConnection` as `null` unless the caller supplies it. Management then tries its default order: SmartCard, HID FIDO, and HID OTP. Use this form when the behavior under test is transport-independent:

```csharp
[SkippableTheory]
[WithYubiKey]
public async Task GetDeviceInfo_DefaultManagementSelection(YubiKeyTestState state)
{
    await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
    {
        var deviceInfo = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(state.SerialNumber, deviceInfo.SerialNumber);
    });
}
```

Do not pass `preferredConnection: state.ConnectionType` from an unfiltered row. On a composite device, that value is a multi-flag set rather than one valid Management connection type.

### Transport-Specific Testing

When behavior must be exercised over concrete connections, declare one row per connection, pass the row's concrete `state.ConnectionType` as `preferredConnection`, and assert the session's actual `Transport`:

```csharp
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
[WithYubiKey(ConnectionType = ConnectionType.HidFido)]
[WithYubiKey(ConnectionType = ConnectionType.HidOtp)]
public async Task GetDeviceInfo_UsesRequestedManagementConnection(YubiKeyTestState state)
{
    Assert.True(state.ConnectionType is
        ConnectionType.SmartCard or ConnectionType.HidFido or ConnectionType.HidOtp);

    await state.WithManagementAsync(async (mgmt, _) =>
    {
        Assert.Equal(state.ConnectionType, mgmt.Transport);

        var info = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(state.SerialNumber, info.SerialNumber);
    }, preferredConnection: state.ConnectionType);
}
```

An explicit `preferredConnection` is a single-candidate request and does not fall back. If the requested interface is already owned, the test receives the connection error instead of silently exercising a different transport.

### Best Practices

1. **Use default selection for transport-independent tests**: Apply one unfiltered `[WithYubiKey]` and omit `preferredConnection`.
2. **Create explicit rows for connection-specific tests**: Apply separate attributes with one concrete `ConnectionType` each.
3. **Pin and verify concrete connections**: Pass `preferredConnection: state.ConnectionType` and assert `mgmt.Transport` in those rows.
4. **Do not pin from unfiltered state**: Its `ConnectionType` can be a multi-flag set that is invalid as a preference.
5. **Avoid DeviceId parsing**: Use `ConnectionType` filters instead of inferring interfaces from identifier strings.

### Migration from Old Patterns

**Old (fragile):**
```csharp
var devices = await YubiKeyManager.FindAllAsync(ConnectionType.Hid);
var fidoDevice = devices.FirstOrDefault(d => 
    d.DeviceId.Contains(":0001") || d.DeviceId.Contains(":F1D0"));
```

**New (declarative and transport-specific):**
```csharp
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.HidFido)]
public async Task ManagementOverFido_Works(YubiKeyTestState state)
{
    await state.WithManagementAsync(async (mgmt, _) =>
    {
        Assert.Equal(ConnectionType.HidFido, mgmt.Transport);
        // Exercise HID FIDO-specific Management behavior.
    }, preferredConnection: state.ConnectionType);
}
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
dotnet toolchain.cs -- test --filter "Category!=RequiresUserPresence"

# Skip slow tests
dotnet toolchain.cs -- test --filter "Category!=Slow"

# Skip hardware tests (run only unit tests)
dotnet toolchain.cs -- test --filter "Category!=RequiresHardware"

# Run only fast unit tests (no hardware, no user presence, not slow)
dotnet toolchain.cs -- test --filter "Category!=RequiresHardware&Category!=RequiresUserPresence&Category!=Slow"
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
dotnet toolchain.cs -- test --filter "Category!=RequiresUserPresence"
```

---

## Writing New Tests

> READ WHEN authoring a new unit or integration test, naming a test method, or writing setup/cleanup for hardware-dependent tests.

### Test Project Layout

- **UnitTests** — xUnit, no hardware required
- **IntegrationTests** — xUnit, requires physical YubiKey
- **TestProject** — ASP.NET Core with NSubstitute, targets .NET 9 with AOT

### Test All Public APIs

```csharp
[Fact]
public async Task ConnectAsync_WhenDeviceAvailable_ReturnsConnection()
{
    // Arrange
    var device = new MockYubiKey { IsConnected = true };

    // Act
    var connection = await device.ConnectAsync<ISmartCardConnection>();

    // Assert
    Assert.NotNull(connection);
    Assert.True(connection.IsConnected);
}
```

### Use Descriptive Test Names

```csharp
// ✅ GOOD
[Fact]
public void CommandApdu_WithNullData_ThrowsArgumentNullException()

// ❌ BAD
[Fact]
public void Test1()
```

Naming pattern: `Subject_WhenCondition_ExpectedBehavior`.

### Clean Up in Integration Tests

```csharp
[Fact]
public async Task IntegrationTest_WithRealDevice()
{
    await using var connection = await _device.ConnectAsync<ISmartCardConnection>();

    try
    {
        var result = await connection.TransmitAsync(apdu);
        Assert.NotNull(result);
    }
    finally
    {
        await ResetDeviceAsync(connection);
    }
}
```

> The **Test Philosophy: Value Over Coverage** rules (no validation-only tests, no skipped tests as placeholders) live in `CLAUDE.md` verbatim — those mandates are load-on-startup because they catch the most common AI-agent failure mode.
