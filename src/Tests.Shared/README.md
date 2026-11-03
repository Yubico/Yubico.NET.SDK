# YubiKit Integration Test Infrastructure

This project provides shared infrastructure for YubiKey integration tests across the .NET SDK.

## Overview

The test infrastructure provides:

- **Safety-first allow list** - hard-fails when no device is explicitly authorized.
- **Lazy device discovery** - discovers and binds devices at test execution time, not discovery time.
- **Declarative device filtering** - filters by firmware, form factor, connection type, capabilities, FIPS state, and custom predicates.
- **Parameterized testing** - standard xUnit `[Theory]` tests receive `YubiKeyTestState` values from `[WithYubiKey]`.
- **Fluent session helpers** - extension methods create application sessions with consistent disposal.
- **Byte-level unit-test recorder** - `RecordingSmartCardConnection` captures SmartCard APDUs and returns queued responses for focused unit tests.

## Quick Start

```csharp
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Xunit;

public class MyIntegrationTests
{
    [SkippableTheory]
    [WithYubiKey]
    public async Task GetDeviceInfo_ReturnsValidData(YubiKeyTestState state)
    {
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            var info = await mgmt.GetDeviceInfoAsync();
            Assert.Equal(state.SerialNumber, info.SerialNumber);
        });
    }

    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.7.0")]
    public async Task ModernFeatures_RequireFirmware570(YubiKeyTestState state)
    {
        using var scpKeyParams = Scp03KeyParameters.Default;
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            var fresh = await mgmt.GetDeviceInfoAsync();
            Assert.Equal(deviceInfo.SerialNumber, fresh.SerialNumber);
        }, scpKeyParams: scpKeyParams);
    }
}
```

The SCP example requires a SmartCard-capable dedicated key configured with those SCP03 keys; firmware
alone does not establish that prerequisite. Each attribute yields one placeholder bound to the first
matching authorized key. Multiple attributes bind independently, not once per connected key. Helpers
pass cached `state.DeviceInfo`; query the session explicitly when testing fresh persisted settings.
The caller owns the SCP parameters. The using scope outlives the awaited helper call, so the helper's
session and connection are disposed before the parameters clear their owned key arrays.

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

### Allow-listed keys are dedicated test keys

The keys listed in `AllowedSerialNumbers` are dedicated test hardware, not production keys.
State mutation, PIV reset, and key generation on them are authorized and need no per-run
approval. The allow list itself is the safety boundary — a key that is not on it hard-fails
before any hardware operation runs, so there is no second gate to add.

What still requires a human is timing and presence, not destruction: User Presence and touch
ceremonies, User Verification, and insert/remove sequences cannot be driven unattended. Mark
those with the `RequiresUserPresence` and `Slow` traits so `--smoke` excludes them, and
coordinate them with a person at the keyboard.

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

Use three-component firmware criteria such as `"5.7.0"`; malformed strings are currently ignored.
`RequireUsb` and `Capability` are independent: the latter accepts an application enabled over USB or NFC.
Current Core FIPS-tag decoding uses the wrong bit mapping, so the FIPS masks and derived filters cannot
establish application approval. Product flag bits can remain in `FormFactor`, causing exact base-enum
filters to miss flagged devices. `IsFips` is a separate manufacturing indicator, not an operating mode.

**YubiKeyTestState**

- Wraps `IYubiKey` plus cached `DeviceInfo`.
- Implements xUnit serialization for placeholder test data.
- Exposes convenience properties such as `SerialNumber`, `FirmwareVersion`, and transport/capability helpers.

**YubiKeyTestInfrastructure**

- Performs authorized device discovery.
- Applies allow-list and filter criteria.
- Caches authorized devices lazily for the test run.

**RecordingSmartCardConnection**

- Unit-test-only `ISmartCardConnection` that records transmitted APDUs and returns queued raw responses.
- Use when asserting byte-level SmartCard command flow without hardware.
- Do not use as an integration-test hardware abstraction or as a broad APDU protocol emulator.

## Writing Integration Tests

### Basic Test

```csharp
[SkippableTheory]
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

This example uses the SecurityDomain integration project's helper, not a helper shipped in Tests.Shared.
To compile it, reference `Yubico.YubiKit.SecurityDomain.IntegrationTests` and its normal dependencies,
or link that project's actual [TestStateExtensions.cs](../SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/TestStateExtensions.cs)
with a SecurityDomain project reference. Import the helper namespace shown below.

Execution requires a dedicated, allow-listed SmartCard key supporting Security Domain and SCP03, with
device state suitable for the helper's reset and subsequent authentication using SCP03 defaults. The
helper resets Security Domain before reading key information, destroying custom Security Domain key
configuration. The 5.3.0 minimum and exposed SmartCard filter do not prove application support, key
configuration or successful reset; the helper explicitly opens SmartCard rather than relying on the filter to pin it.

```csharp
using Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;

[SkippableTheory]
[WithYubiKey(MinFirmware = "5.3.0", ConnectionType = ConnectionType.SmartCard)]
public async Task Scp03_AfterReset_ReturnsKeyInfo(YubiKeyTestState state)
{
    using var scpKeyParams = Scp03KeyParameters.Default;
    await state.WithSecurityDomainSessionAsync(
        resetBeforeUse: true,
        async session =>
        {
            var keyInfo = await session.ListKeyInformationAsync();
            Assert.NotEmpty(keyInfo);
        },
        scpKeyParams: scpKeyParams);
}
```

Keep the caller-owned SCP parameters alive through the awaited helper; that helper disposes its
sessions/connection, not the supplied parameters. This example supplies SCP03 defaults and reads key
information; it does not demonstrate an SCP11 handshake.

### Custom Filter

```csharp
public sealed class ProductionKeysOnly : IYubiKeyFilter
{
    public bool Matches(YubiKeyTestState device) => device.SerialNumber > 10_000_000;
    public string GetDescription() => "Production keys";
}

[SkippableTheory]
[WithYubiKey(CustomFilter = typeof(ProductionKeysOnly))]
public async Task RunsOnlyOnProductionKeyRange(YubiKeyTestState state)
{
    await state.WithManagementAsync(async (mgmt, info) =>
    {
        var freshInfo = await mgmt.GetDeviceInfoAsync();
        Assert.True(freshInfo.SerialNumber > 10_000_000);
        Assert.Equal(info.SerialNumber, freshInfo.SerialNumber);
    });
}
```

## Common Helper Pattern

Application helpers should keep connection/session ownership visible. The Management helper uses its
device extension to create an owned session, passes cached `state.DeviceInfo`, and disposes that session
and hidden connection when the callback completes. Its optional arguments are `configuration`,
`scpKeyParams`, `preferredConnection` and `cancellationToken`, in that order. Name policy arguments.
To test a transport, filter one concrete row, pin it and assert what was opened:

```csharp
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.HidOtp)]
public async Task ReadMetadata_OverOtpHid(YubiKeyTestState state)
{
    await state.WithManagementAsync(async (mgmt, _) =>
    {
        Assert.Equal(ConnectionType.HidOtp, mgmt.ConnectionType);
        var info = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(state.SerialNumber, info.SerialNumber);
    }, preferredConnection: state.ConnectionType);
}
```

An unfiltered state's connection set may have multiple flags and is not a valid single preference.
Default Management selection chooses one exposed interface (SmartCard, HID FIDO, then HID OTP), with
no fallback after open/initialization failure. For a SmartCard-only applet, direct borrowing is useful:

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
        new SessionCreationOptions { ScpKeyParameters = scpKeyParams },
        cancellationToken: cancellationToken);

    await action(session, state.DeviceInfo);
}
```

For **OATH reset setup**, use the module's `WithOathSessionAsync`: successful RESET ends the old session,
and setup closes its connection before opening a fresh body session/connection. Do not reset and continue
using the session in the generic example above. See [OATH reset migration](../Oath/README.md#migrating-reset-callers).
This is OATH-specific, not a change to other applets' helpers.

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

## Multi-Key Discovery Expectations

Tests that connect two or more keys of the same model should read
[Device Discovery Guarantees](../../docs/architecture/device-discovery-guarantees.md) first. Some
conservative outcomes are documented platform bounds with pinning tests rather than bugs - notably
that serial-less multi-interface keys cannot be grouped on macOS or Linux, and that partially
enumerated same-PID keys can transiently share a composite. Assertions like `Assert.Single` on a
discovery result only hold on a single-key rig; prefer key-count-agnostic invariants
(conservation, completeness per PID, stability across scans).

## Hardware Safety Rules

- Allow-listed keys are dedicated test keys: state mutation, PIV reset, and key generation on
  them are authorized without per-run approval. The allow list is the boundary.
- Do not run User Presence, UV, touch, or insert/remove tests without human coordination — those
  need a person present, regardless of which key is connected.
- Add `Slow` and `RequiresUserPresence` traits where applicable so `--smoke` can exclude them.
- Never commit real production serial numbers.

## Known Limitations

- Devices must be connected before the test runner starts.
- Allow-list violations call `Environment.Exit(-1)` by design.
- Each `[WithYubiKey]` attribute produces one placeholder that binds to the first matching authorized
  device at execution; multiple attributes are not deduplicated by physical key.
