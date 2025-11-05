# YubiKit Integration Test Infrastructure

This project provides shared infrastructure for YubiKey integration tests across the .NET SDK.

## Overview

The test infrastructure provides:

- **Safety-first allow list** - Hard fail if device is not authorized
- **Automatic device discovery** - No manual device setup required
- **Declarative device filtering** - Attribute-based requirements (firmware, form factor, capabilities, etc.)
- **Parameterized testing** - Single test runs on all matching devices
- **Fluent test helpers** - Extension methods for session management
- **SCP support** - Transparent secure channel protocol handling

## Quick Start

```csharp
using Yubico.YubiKit.Tests.Shared.Infrastructure;

public class MyIntegrationTests
{
    /// Test runs on ALL authorized devices
    [YubiKeyTheory]
    public async Task GetDeviceInfo_ReturnsValidData(YubiKeyTestState state)
    {
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            var info = await mgmt.GetDeviceInfoAsync();
            Assert.Equal(state.SerialNumber, info.SerialNumber);
        });
    }

    /// Test runs ONLY on devices with firmware >= 5.7.0
    [YubiKeyTheory(MinFirmware = "5.7.0")]
    public async Task ModernFeatures_RequiresFirmware570(YubiKeyTestState state)
    {
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            // Test modern features
        }, Scp03KeyParams.Default);
    }
}
```

## Safety: Allow List

**CRITICAL**: All integration tests verify the device serial number against an allow list before running any tests.

### Configuration

Add your test device serial numbers to `appsettings.json` in your integration test project:

```json
{
  "YubiKeyTests": {
    "AllowedSerialNumbers": [
      12345678,
      87654321
    ]
  }
}
```

### Behavior

- If no devices found: Tests are skipped (no test execution)
- If device serial cannot be read: **Hard fail** (`Environment.Exit(-1)`)
- If device serial not in allow list: **Hard fail** (`Environment.Exit(-1)`)
- If allow list is empty: **Hard fail** (no devices authorized)

This prevents accidentally running destructive tests on production YubiKeys.

## Architecture

### Attribute-Based Testing

The new architecture uses attributes instead of inheritance:

```
Test Method
    ↓ decorated with
[YubiKeyTheory(MinFirmware = "5.7.0", FormFactor = FormFactor.UsbAKeychain)]
    ↓ discovered by
YubiKeyTheoryDiscoverer (implements IXunitTestCaseDiscoverer)
    ↓ uses
YubiKeyTestInfrastructure (static device cache and filtering)
    ↓ validates with
AllowList (verifies device authorization)
```

### Key Components

**YubiKeyTheoryAttribute**
- xUnit theory attribute with built-in device filtering
- Combines `[Theory]` and `[YubiKeyData]` into single attribute
- Supports declarative filtering: firmware, form factor, transport, capabilities, FIPS

**YubiKeyTestState**
- Wrapper for `IYubiKey` and `DeviceInfo`
- Passed as parameter to test methods
- Implements `IXunitSerializable` for xUnit parameterization
- Provides convenience properties: `SerialNumber`, `FirmwareVersion`, `FormFactor`, etc.

**YubiKeyTestInfrastructure**
- Centralized device discovery (runs once per test run)
- Device filtering logic shared across all tests
- Static `AllAuthorizedDevices` cache

**YubiKeyTestDeviceExtensions**
- Extension methods for fluent test API
- `WithManagementAsync()` - Automatic session creation/disposal
- Future: `WithPivAsync()`, `WithOathAsync()`, etc.

## Writing Integration Tests

### Basic Test (All Devices)

```csharp
[YubiKeyTheory]
public async Task GetDeviceInfo_AllDevices_ReturnsValidData(YubiKeyTestState state)
{
    // This test runs on EVERY authorized device in the allow list

    await state.WithManagementAsync(async (mgmt, deviceInfo) =>
    {
        var info = await mgmt.GetDeviceInfoAsync();

        Assert.Equal(state.SerialNumber, info.SerialNumber);
        Assert.Equal(state.FirmwareVersion, info.FirmwareVersion);
        Assert.Equal(state.FormFactor, info.FormFactor);
    });
}
```

### Firmware Filtering

```csharp
[YubiKeyTheory(MinFirmware = "5.7.0")]
public async Task ModernFeatures_Firmware570Plus(YubiKeyTestState state)
{
    // Only runs on devices with firmware >= 5.7.0
    // Devices with older firmware are automatically skipped

    Assert.True(state.FirmwareVersion.IsAtLeast(5, 7, 0));

    await state.WithManagementAsync(async (mgmt, deviceInfo) =>
    {
        // Test features that require 5.7.0+
    });
}
```

### Form Factor Filtering

```csharp
[YubiKeyTheory(FormFactor = FormFactor.UsbABiometricKeychain)]
public async Task BiometricFeatures_BioKeysOnly(YubiKeyTestState state)
{
    // Only runs on USB-A Bio keys

    Assert.Equal(FormFactor.UsbABiometricKeychain, state.FormFactor);

    await state.WithManagementAsync(async (mgmt, deviceInfo) =>
    {
        // Test biometric-specific features
    });
}
```

### Transport Filtering

```csharp
[YubiKeyTheory(RequireUsb = true)]
public async Task UsbFeatures_UsbDevicesOnly(YubiKeyTestState state)
{
    // Only runs on devices with USB transport

    Assert.True(state.IsUsbTransport);

    await state.WithManagementAsync(async (mgmt, deviceInfo) =>
    {
        Assert.True(deviceInfo.UsbSupported != DeviceCapabilities.None);
    });
}
```

### Capability Filtering

```csharp
[YubiKeyTheory(Capability = DeviceCapabilities.Piv)]
public async Task PivFeatures_PivEnabledOnly(YubiKeyTestState state)
{
    // Only runs on devices with PIV capability enabled

    Assert.True(state.HasCapability(DeviceCapabilities.Piv));

    await state.WithManagementAsync(async (mgmt, deviceInfo) =>
    {
        var pivEnabled = (deviceInfo.UsbEnabled & DeviceCapabilities.Piv) != 0 ||
                         (deviceInfo.NfcEnabled & DeviceCapabilities.Piv) != 0;
        Assert.True(pivEnabled);
    });
}
```

### FIPS Filtering

```csharp
[YubiKeyTheory(FipsCapable = DeviceCapabilities.Piv)]
public async Task FipsCapable_PivDevices(YubiKeyTestState state)
{
    // Only runs on devices that are FIPS-capable for PIV

    Assert.True(state.IsFipsCapable(DeviceCapabilities.Piv));

    await state.WithManagementAsync(async (mgmt, deviceInfo) =>
    {
        Assert.True((deviceInfo.FipsCapabilities & DeviceCapabilities.Piv) != 0);
    });
}

[YubiKeyTheory(FipsApproved = DeviceCapabilities.Piv)]
public async Task FipsApproved_PivDevices(YubiKeyTestState state)
{
    // Only runs on devices in FIPS-approved mode for PIV

    Assert.True(state.IsFipsApproved(DeviceCapabilities.Piv));

    await state.WithManagementAsync(async (mgmt, deviceInfo) =>
    {
        Assert.True((deviceInfo.FipsApproved & DeviceCapabilities.Piv) != 0);
    });
}
```

### Combined Filtering

```csharp
[YubiKeyTheory(
    MinFirmware = "5.0.0",
    RequireUsb = true,
    Capability = DeviceCapabilities.Piv)]
public async Task AdvancedPiv_ModernUsbKeysWithPiv(YubiKeyTestState state)
{
    // Multiple requirements - ALL must be met:
    // 1. Firmware >= 5.0.0
    // 2. USB transport available
    // 3. PIV capability enabled

    Assert.True(state.FirmwareVersion.IsAtLeast(5, 0, 0));
    Assert.True(state.IsUsbTransport);
    Assert.True(state.HasCapability(DeviceCapabilities.Piv));
}
```

## Available Filter Properties

| Property       | Type                 | Description                                      | Example                                       |
|----------------|----------------------|--------------------------------------------------|-----------------------------------------------|
| MinFirmware    | string?              | Minimum firmware version (format: "5.7.2")       | `MinFirmware = "5.7.0"`                       |
| FormFactor     | FormFactor           | Required form factor                             | `FormFactor = FormFactor.UsbAKeychain`        |
| RequireUsb     | bool                 | Requires USB transport                           | `RequireUsb = true`                           |
| RequireNfc     | bool                 | Requires NFC transport                           | `RequireNfc = true`                           |
| Capability     | DeviceCapabilities   | Required enabled capability                      | `Capability = DeviceCapabilities.Piv`         |
| FipsCapable    | DeviceCapabilities   | Required FIPS-capable capability                 | `FipsCapable = DeviceCapabilities.Piv`        |
| FipsApproved   | DeviceCapabilities   | Required FIPS-approved capability                | `FipsApproved = DeviceCapabilities.Piv`       |

## YubiKeyTestState API

The `YubiKeyTestState` parameter provides access to device information and the underlying `IYubiKey` instance.

### Properties

```csharp
public class YubiKeyTestState
{
    // Direct device access
    public IYubiKey Device { get; }
    public DeviceInfo DeviceInfo { get; }

    // Convenience properties
    public FirmwareVersion FirmwareVersion { get; }
    public FormFactor FormFactor { get; }
    public int SerialNumber { get; }
    public bool IsUsbTransport { get; }
    public bool IsNfcTransport { get; }

    // Helper methods
    public bool IsFipsCapable(DeviceCapabilities capability);
    public bool IsFipsApproved(DeviceCapabilities capability);
    public bool HasCapability(DeviceCapabilities capability);
}
```

### Extension Methods

```csharp
// Management session
await state.WithManagementAsync(async (mgmt, deviceInfo) =>
{
    // mgmt: ManagementSession<ISmartCardConnection>
    // deviceInfo: DeviceInfo
});

// With SCP03
await state.WithManagementAsync(async (mgmt, deviceInfo) =>
{
    // Session uses SCP03
}, Scp03KeyParams.Default);

// With custom cancellation
await state.WithManagementAsync(async (mgmt, deviceInfo) =>
{
    // Can be cancelled
}, scpKeyParams: null, cancellationToken: cts.Token);
```

## SCP (Secure Channel Protocol) Support

### Using SCP in Tests

```csharp
[YubiKeyTheory(MinFirmware = "5.7.0")]
public async Task TestWithScp03(YubiKeyTestState state)
{
    // Pass SCP key parameters to extension method
    await state.WithManagementAsync(async (mgmt, deviceInfo) =>
    {
        // Session automatically uses SCP03
        var info = await mgmt.GetDeviceInfoAsync();
        Assert.NotNull(info);

    }, Scp03KeyParams.Default);
}

[YubiKeyTheory(MinFirmware = "5.7.0")]
public async Task TestWithCustomScpKeys(YubiKeyTestState state)
{
    var scpKeys = new Scp03KeyParams(
        StaticKeys.FromBytes(channelEncKey, channelMacKey, channelDecKey));

    await state.WithManagementAsync(async (mgmt, deviceInfo) =>
    {
        // Session uses custom SCP03 keys
    }, scpKeys);
}
```

## Advanced Patterns

### Conditional Test Logic

```csharp
[YubiKeyTheory]
public async Task AdaptiveTest_VariesByDevice(YubiKeyTestState state)
{
    await state.WithManagementAsync(async (mgmt, deviceInfo) =>
    {
        if (state.FirmwareVersion.Major >= 5)
        {
            // Test modern features
            Assert.True(deviceInfo.UsbEnabled != DeviceCapabilities.None);
        }
        else
        {
            // Test legacy features
            Assert.NotNull(deviceInfo.FirmwareVersion);
        }

        if (state.FormFactor == FormFactor.UsbABiometricKeychain)
        {
            // Bio-specific assertions
            Assert.True(state.FirmwareVersion.Major >= 5);
        }
    });
}
```

### Performance Testing

```csharp
[YubiKeyTheory]
public async Task Performance_GetDeviceInfo_CompletesQuickly(YubiKeyTestState state)
{
    using var connection = await state.Device.ConnectAsync<ISmartCardConnection>();
    using var mgmt = await ManagementSession<ISmartCardConnection>.CreateAsync(connection);

    var sw = Stopwatch.StartNew();
    var deviceInfo = await mgmt.GetDeviceInfoAsync();
    sw.Stop();

    Assert.True(sw.ElapsedMilliseconds < 1000,
        $"GetDeviceInfo took {sw.ElapsedMilliseconds}ms on {state}");
}
```

### Serial Number Consistency

```csharp
[YubiKeyTheory]
public async Task SerialNumber_MultipleReads_RemainsConsistent(YubiKeyTestState state)
{
    using var connection1 = await state.Device.ConnectAsync<ISmartCardConnection>();
    using var mgmt1 = await ManagementSession<ISmartCardConnection>.CreateAsync(connection1);
    var serial1 = (await mgmt1.GetDeviceInfoAsync()).SerialNumber;

    using var connection2 = await state.Device.ConnectAsync<ISmartCardConnection>();
    using var mgmt2 = await ManagementSession<ISmartCardConnection>.CreateAsync(connection2);
    var serial2 = (await mgmt2.GetDeviceInfoAsync()).SerialNumber;

    Assert.Equal(serial1, serial2);
    Assert.Equal(state.SerialNumber, serial1);
}
```

## Project Setup

### References

Integration test projects should reference:

```xml
<ItemGroup>
    <ProjectReference Include="..\..\src\Yubico.YubiKit.Management\Yubico.YubiKit.Management.csproj"/>
    <ProjectReference Include="..\..\..\Yubico.YubiKit.Tests.Shared\Yubico.YubiKit.Tests.Shared.csproj"/>
</ItemGroup>
```

### Configuration Files

Configuration files must be copied to output:

```xml
<ItemGroup>
    <None Update="appsettings.json">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

### Dependencies

```xml
<ItemGroup>
    <PackageReference Include="xunit" Version="2.6.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
</ItemGroup>
```

## Lifecycle

### Test Execution Flow

1. **Test Discovery** (once per test run)
    - xUnit discovers all `[YubiKeyTheory]` test methods
    - `YubiKeyTheoryDiscoverer.Discover()` is called for each test method
    - `YubiKeyTestInfrastructure.AllAuthorizedDevices` is accessed (lazy initialization)
        - Discovers all YubiKey devices via `YubiKey.FindAllAsync()`
        - **Verifies allow list** for each device (hard fail if not authorized)
        - Retrieves `DeviceInfo` for each authorized device
        - Caches devices in `YubiKeyDeviceCache`
    - Filters devices based on attribute properties (firmware, form factor, etc.)
    - Creates one `XunitTestCase` per matching device

2. **Test Execution** (once per test case)
    - xUnit deserializes `YubiKeyTestState` parameter
        - Looks up device from `YubiKeyDeviceCache` by serial number
    - Test method runs with `YubiKeyTestState` parameter
    - Test uses extension methods (`WithManagementAsync()`, etc.) for session management

3. **Cleanup**
    - Sessions are disposed via `using` statements
    - Connections are disposed via `using` statements
    - No explicit cleanup needed (no test class lifecycle)

### Device Cache

Devices are discovered **once per test run**, not once per test:

```csharp
public static IReadOnlyList<YubiKeyTestState> AllAuthorizedDevices { get; } =
    InitializeDevicesAsync();
```

This is efficient because:
- Device discovery is expensive (USB enumeration)
- Device info doesn't change during test run
- All tests share the same device cache
- Allow list verification happens once

## Error Handling

### No Devices Found

If no YubiKey devices are connected:
- Device discovery returns empty list
- All `[YubiKeyTheory]` tests are skipped
- No errors or failures

**Solution**: Connect a YubiKey device.

### Serial Number Not in Allow List

```
═══════════════════════════════════════════════════════════════════════════
                        YUBIKEY ALLOW LIST VIOLATION
═══════════════════════════════════════════════════════════════════════════

Device with serial number 12345678 is NOT authorized for testing.

Tests can only run on YubiKeys explicitly listed in the allow list.
This prevents accidental testing on production devices.

To authorize this device, add the serial number to your appsettings.json:

{
  "YubiKeyTests": {
    "AllowedSerialNumbers": [
      12345678
    ]
  }
}

═══════════════════════════════════════════════════════════════════════════
                        TESTS WILL NOT RUN
═══════════════════════════════════════════════════════════════════════════
```

**Solution**: Add the serial number to `appsettings.json`.

### Empty Allow List

```
═══════════════════════════════════════════════════════════════════════════
                        NO AUTHORIZED DEVICES FOUND
═══════════════════════════════════════════════════════════════════════════

Found 1 YubiKey device(s), but NONE are authorized for testing.

Tests can only run on YubiKeys explicitly listed in the allow list.
Add device serial numbers to appsettings.json in your test project:

{
  "YubiKeyTests": {
    "AllowedSerialNumbers": [
      12345678,
      87654321
    ]
  }
}

═══════════════════════════════════════════════════════════════════════════
                        TESTS WILL NOT RUN
═══════════════════════════════════════════════════════════════════════════
```

**Solution**: Configure at least one serial number in `appsettings.json`.

### No Devices Match Filter

If a test has requirements that no device meets:

```
[YubiKeyTheory] No devices match criteria for test 'TestScp11'.
Criteria: MinFirmware >= 5.7.2
```

The test is **skipped** (not failed). This is expected behavior when testing features that require specific firmware/hardware.

## Best Practices

### DO

- ✅ **Use `[YubiKeyTheory]`** for all integration tests
- ✅ **Use extension methods** (`WithManagementAsync()`, etc.) for session management
- ✅ **Use declarative filtering** (attribute properties) instead of runtime checks
- ✅ **Add test device serial numbers to allow list** before running tests
- ✅ **Test on multiple firmware versions** when possible (allow list multiple devices)
- ✅ **Use descriptive test names** that indicate what's being tested
- ✅ **Assert on cached device info** (`state.SerialNumber`, etc.) for consistency checks
- ✅ **Use `using` statements** for connections and sessions

### DON'T

- ❌ **Never bypass the allow list** - it prevents production key damage
- ❌ **Don't manually acquire devices** - use `YubiKeyTestState.Device`
- ❌ **Don't hardcode serial numbers** in test code - use allow list
- ❌ **Don't use runtime checks for requirements** - use attribute filtering
- ❌ **Don't inherit from base classes** - use attributes instead
- ❌ **Don't forget to dispose** connections and sessions
- ❌ **Don't leave device in unknown state** - reset or restore in cleanup

### Migration from Old Architecture

If you have tests using the old `YubiKeyTestBase` pattern:

```csharp
// OLD (inheritance-based)
public class MyTests : ManagementTestFixture
{
    [SkippableFact]
    public async Task TestExample()
    {
        RequireFirmware(5, 7, 0);

        await State.WithManagementAsync(async (mgmt, state) =>
        {
            // Test code
        });
    }
}

// NEW (attribute-based)
public class MyTests
{
    [YubiKeyTheory(MinFirmware = "5.7.0")]
    public async Task TestExample(YubiKeyTestState state)
    {
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            // Same test code
        });
    }
}
```

**Changes**:
1. Remove inheritance (no base class)
2. Replace `[SkippableFact]` with `[YubiKeyTheory]`
3. Move `RequireXxx()` calls to attribute properties
4. Add `YubiKeyTestState state` parameter
5. Change `State.WithManagementAsync` to `state.WithManagementAsync`

## Examples

See `Yubico.YubiKit.Management.IntegrationTests/AdvancedManagementTests.cs` for comprehensive examples of:
- Basic device filtering
- Firmware version filtering
- Form factor filtering
- Transport filtering
- Capability filtering
- FIPS filtering
- Combined filtering
- SCP usage
- Performance testing
- Serial number consistency testing

## Architecture Rationale

### Why Attributes Instead of Inheritance?

The previous architecture used inheritance (`YubiKeyTestBase` → `ManagementTestFixture` → test class):

**Problems**:
- Single inheritance limitation (can't inherit from multiple fixtures)
- Verbose test setup (base class constructors, fixture initialization)
- Runtime requirement checks (`RequireFirmware()`) - fail at execution time
- Implicit device lifecycle (hidden in base class)

The new architecture uses attributes:

**Benefits**:
- No inheritance constraints - just add `[YubiKeyTheory]`
- Declarative filtering - requirements are visible in attribute
- Compile-time filtering - xUnit discovers applicable devices before test execution
- Explicit device parameter - clear what device is being tested
- Simpler mental model - no hidden state in base classes

### Why Static Device Cache?

The device cache is initialized once as a static property:

```csharp
public static IReadOnlyList<YubiKeyTestState> AllAuthorizedDevices { get; } =
    InitializeDevicesAsync();
```

**Benefits**:
- Device discovery is expensive (USB enumeration, APDU communication)
- Device info doesn't change during test run
- Allow list verification happens once (fail fast)
- Consistent device state across all tests
- Simple and thread-safe (initialized once by CLR)

### Why Extension Methods Instead of TestState Classes?

The previous architecture had application-specific `TestState` classes (`ManagementTestState`, `PivTestState`):

**Problems**:
- Code duplication (each TestState reimplements same patterns)
- Tight coupling (TestState knows about specific applications)
- Limited extensibility (adding new application requires new TestState)

The new architecture uses extension methods:

**Benefits**:
- Single `YubiKeyTestState` class (wraps `IYubiKey` + `DeviceInfo`)
- Extension methods add application-specific helpers (`WithManagementAsync()`, `WithPivAsync()`)
- Composition over inheritance
- Easy to add new applications (just add extension method)
- Clear separation of concerns (YubiKeyTestState = device data, extensions = session management)

## Future Enhancements

### Planned Extension Methods

```csharp
// PIV support
await state.WithPivAsync(async (piv, deviceInfo) =>
{
    // piv: PivSession<ISmartCardConnection>
}, scpKeyParams: null, resetToDefaults: true);

// OATH support
await state.WithOathAsync(async (oath, deviceInfo) =>
{
    // oath: OathSession<ISmartCardConnection>
}, scpKeyParams: null);

// FIDO support
await state.WithFidoAsync(async (fido, deviceInfo) =>
{
    // fido: FidoSession
});
```

### Planned Attribute Features

- `ExcludeFirmware` - Exclude specific firmware versions
- `ExcludeFormFactor` - Exclude specific form factors
- `SerialNumbers` - Run only on specific serial numbers (useful for hardware-specific tests)

## Contributing

When adding support for new applications (PIV, OATH, FIDO):

1. Add extension method to `YubiKeyTestDeviceExtensions.cs`:
   ```csharp
   public static async Task WithPivAsync(
       this YubiKeyTestState state,
       Func<PivSession<ISmartCardConnection>, DeviceInfo, Task> action,
       ScpKeyParams? scpKeyParams = null,
       bool resetToDefaults = false,
       CancellationToken cancellationToken = default)
   {
       using var connection = await state.Device.ConnectAsync<ISmartCardConnection>(cancellationToken);
       using var session = await PivSession<ISmartCardConnection>.CreateAsync(
           connection, scpKeyParams: scpKeyParams, cancellationToken: cancellationToken);

       if (resetToDefaults)
       {
           await session.ResetAsync(cancellationToken);
       }

       await action(session, state.DeviceInfo);
   }
   ```

2. Add tests in `Yubico.YubiKit.{Application}.IntegrationTests/`

3. Update this README with examples

## License

Copyright 2025 Yubico AB - Licensed under Apache 2.0
