# CLAUDE.md - Management Tests

This file provides guidance for the Management module test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet build.cs test` - NEVER use `dotnet test` directly.**

For Management-specific test patterns, device filtering, and test state utilities, see the **Test Infrastructure** section in [`../CLAUDE.md`](../CLAUDE.md#test-infrastructure---advanced-device-filtering).

## Test Projects

- `Yubico.YubiKit.Management.UnitTests` - Unit tests for Management module (xUnit v3)
- `Yubico.YubiKit.Management.IntegrationTests` - Integration tests requiring YubiKey hardware (xUnit v2)

## Advanced Device Filtering

Management tests use the most powerful device filtering system in the SDK via `[WithYubiKey]` attribute:

```csharp
[Theory]
[WithYubiKey(
    MinFirmware = "5.3.0",                     // Only firmware >= 5.3.0
    FormFactor = FormFactor.UsbAKeychain,      // Only USB-A keychains
    Capability = DeviceCapabilities.Piv,       // Must have PIV enabled
    RequireUsb = true,                         // USB transport required
    FipsCapable = DeviceCapabilities.Piv       // FIPS-capable for PIV
)]
public async Task MyTest(YubiKeyTestState state)
{
    // Test runs ONLY on devices matching ALL criteria
}
```

## Test Helper Extension

Use `WithManagementAsync` for automatic session management:

```csharp
[Theory]
[WithYubiKey(MinFirmware = "5.0.0")]
public async Task MyTest(YubiKeyTestState state) =>
    await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
    {
        // mgmt = ManagementSession (already initialized)
        // cachedDeviceInfo = DeviceInfo from initial query
        
        var info = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(cachedDeviceInfo.SerialNumber, info.SerialNumber);
    });
```

## Critical Warnings

**⚠️ NEVER write tests that modify device configuration in the shared test suite:**
- Configuration changes cause device reboots (3+ seconds)
- Changes persist across test runs
- Breaks tests that depend on specific capabilities
- Requires manual device reconfiguration to fix

See [`../CLAUDE.md`](../CLAUDE.md#critical-warnings-for-configuration-tests) for safe configuration testing patterns.

## Running Tests

```bash
# Run all Management tests
dotnet build.cs test --filter "FullyQualifiedName~Yubico.YubiKit.Management"

# Run unit tests only
dotnet build.cs test --filter "FullyQualifiedName~Yubico.YubiKit.Management.UnitTests"

# Run integration tests only (requires YubiKey)
dotnet build.cs test --filter "FullyQualifiedName~Yubico.YubiKit.Management.IntegrationTests"

# Run specific test class
dotnet build.cs test --filter "FullyQualifiedName~ManagementIntegrationTests"
```

