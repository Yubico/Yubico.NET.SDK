# YubiKit Integration Test Infrastructure

This project provides shared infrastructure for YubiKey integration tests across the .NET SDK.

## Overview

The test infrastructure provides:

- **Safety-first allow list** - hard-fails when no device is explicitly authorized.
- **Lazy device discovery** - discovers and binds devices at test execution time, not discovery time.
- **Declarative device filtering** - filters by firmware, form factor, connection type, capabilities, FIPS state, and custom predicates.
- **Parameterized testing** - standard xUnit `[Theory]` tests receive `YubiKeyTestState` values from `[WithYubiKey]`.
- **Fluent session helpers** - extension methods create application sessions with consistent disposal.
- **Shared connection helpers** - `SharedSmartCardConnection` lets reset/setup helpers share one physical SmartCard connection safely.
- **Byte-level unit-test recorder** - `RecordingSmartCardConnection` captures SmartCard APDUs and returns queued responses for focused unit tests.

## Quick Start

```csharp
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

public class MyIntegrationTests
{
    [Theory]
    [WithYubiKey]
    public async Task GetDeviceInfo_ReturnsValidData(YubiKeyTestState state)
    {
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            var info = await mgmt.GetDeviceInfoAsync();
            Assert.Equal(state.SerialNumber, info.SerialNumber);
        });
    }

    [Theory]
    [WithYubiKey(MinFirmware = "5.7.0")]
    public async Task ModernFeatures_RequireFirmware570(YubiKeyTestState state)
    {
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            Assert.True(deviceInfo.FirmwareVersion.IsAtLeast(5, 7, 0));
        }, Scp03KeyParameters.Default);
    }
}
```

## Safety: Allow List

All integration tests verify the device serial number against an allow list before running any hardware operation.

Add test device serial numbers to `appsettings.json` in the integration test project:

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

Behavior:

- If no devices are found, tests are skipped.
- If device serial cannot be read, the process hard-fails with `Environment.Exit(-1)`.
- If a device serial is not in the allow list, the process hard-fails with `Environment.Exit(-1)`.
- If the allow list is empty, the process hard-fails before tests can run.

This prevents accidentally running destructive or stateful tests on production YubiKeys.

## Attribute-Based Testing

The current integration-test shape is standard xUnit `[Theory]` plus one or more `[WithYubiKey]` data attributes:

```text
Test method
    -> [Theory]
    -> [WithYubiKey(MinFirmware = "5.7.0", FormFactor = FormFactor.UsbAKeychain)]
    -> WithYubiKeyAttribute returns a serialized placeholder at discovery
    -> YubiKeyTestState binds to a real authorized device at execution
    -> YubiKeyTestInfrastructure filters devices and validates the allow list
```

`WithYubiKeyAttribute` derives from xUnit `DataAttribute`, not from `TheoryAttribute`. That distinction is important: use both `[Theory]` and `[WithYubiKey]` on test methods.

## Key Components

**WithYubiKeyAttribute**

- Provides filtered `YubiKeyTestState` test data for xUnit theories.
- Supports filters such as `MinFirmware`, `FormFactor`, `RequireUsb`, `RequireNfc`, `ConnectionType`, `Capability`, `FipsCapable`, `FipsApproved`, and `CustomFilter`.
- Returns placeholders during discovery so test discovery does not touch hardware.

**YubiKeyTestState**

- Wraps `IYubiKey` plus cached `DeviceInfo`.
- Implements xUnit serialization for placeholder test data.
- Exposes convenience properties such as `SerialNumber`, `FirmwareVersion`, and transport/capability helpers.

**YubiKeyTestInfrastructure**

- Performs authorized device discovery.
- Applies allow-list and filter criteria.
- Caches authorized devices lazily for the test run.

**SharedSmartCardConnection**

- Non-owning wrapper for sharing one `ISmartCardConnection` across multiple sessions in a single integration helper.
- Useful for reset-then-test flows where the setup session must not dispose the physical connection before the test session starts.

**RecordingSmartCardConnection**

- Unit-test-only `ISmartCardConnection` that records transmitted APDUs and returns queued raw responses.
- Use when asserting byte-level SmartCard command flow without hardware.
- Do not use as an integration-test hardware abstraction or as a broad APDU protocol emulator.

## Writing Integration Tests

### Basic Test

```csharp
[Theory]
[WithYubiKey]
public async Task GetDeviceInfo_AllDevices_ReturnsValidData(YubiKeyTestState state)
{
    await state.WithManagementAsync(async (mgmt, cachedInfo) =>
    {
        var info = await mgmt.GetDeviceInfoAsync();

        Assert.Equal(state.SerialNumber, info.SerialNumber);
        Assert.Equal(cachedInfo.FirmwareVersion, info.FirmwareVersion);
    });
}
```

### Filtered Test

```csharp
[Theory]
[WithYubiKey(MinFirmware = "5.7.2", Capability = DeviceCapabilities.SecurityDomain)]
public async Task Scp11_ModernDevice_ReturnsKeyInfo(YubiKeyTestState state)
{
    await state.WithSecurityDomainSessionAsync(
        resetBeforeUse: true,
        async session =>
        {
            var keyInfo = await session.GetKeyInfoAsync();
            Assert.NotEmpty(keyInfo.Keys);
        },
        scpKeyParams: Scp03KeyParameters.Default);
}
```

### Custom Filter

```csharp
public sealed class ProductionKeysOnly : IYubiKeyFilter
{
    public bool Matches(YubiKeyTestState device) => device.SerialNumber > 10_000_000;
    public string GetDescription() => "Production keys";
}

[Theory]
[WithYubiKey(CustomFilter = typeof(ProductionKeysOnly))]
public async Task RunsOnlyOnProductionKeyRange(YubiKeyTestState state)
{
    await state.WithManagementAsync(async (mgmt, info) =>
    {
        Assert.True(info.SerialNumber > 10_000_000);
    });
}
```

## Common Helper Pattern

Application helpers should create the transport explicitly, create the session, invoke the action, and dispose in the same visible flow:

```csharp
public static async Task WithOathAsync(
    this YubiKeyTestState state,
    Func<OathSession, DeviceInfo, Task> action,
    ScpKeyParameters? scpKeyParams = null,
    CancellationToken cancellationToken = default)
{
    await using var connection = await state.Device.ConnectAsync<ISmartCardConnection>(cancellationToken);

    await using var session = await OathSession.CreateAsync(
        connection,
        scpKeyParams: scpKeyParams,
        cancellationToken: cancellationToken);

    await action(session, state.DeviceInfo);
}
```

## Byte-Level Unit Test Recorder

Use `RecordingSmartCardConnection` in module unit tests that need to verify exact SmartCard APDU bytes:

```csharp
var connection = new RecordingSmartCardConnection(
    [0x90, 0x00],
    [0x01, 0x02, 0x90, 0x00]);

await using var session = await SomeSession.CreateAsync(connection, cancellationToken: ct);

Assert.Contains(connection.TransmittedCommands, command => command[1] == expectedInstruction);
```

This helper is intentionally narrow: queued responses in, transmitted APDUs out. It does not replace `[WithYubiKey]`, allow-list checks, or integration-test hardware coordination.

## Running Tests

Use the repository toolchain so xUnit v2 and v3 projects get the correct runner:

```bash
# Unit tests for a module
dotnet toolchain.cs -- test --project Management --filter "FullyQualifiedName~Yubico.YubiKit.Management.UnitTests"

# Smoke-filtered integration tests for a module
dotnet toolchain.cs -- test --integration --project SecurityDomain --smoke --filter "FullyQualifiedName~SecurityDomainSession"
```

Do not run raw `dotnet test` directly.

## Hardware Safety Rules

- Keep destructive tests opt-in and clearly marked.
- Do not run User Presence, UV, touch, insert/remove, reset, or persistent-state tests without human coordination.
- Add `Slow` and `RequiresUserPresence` traits where applicable so `--smoke` can exclude them.
- Never commit real production serial numbers.

## Known Limitations

- Devices must be connected before the test runner starts.
- Allow-list violations call `Environment.Exit(-1)` by design.
- Each `[WithYubiKey]` attribute produces one placeholder that binds to a matching authorized device at execution.
