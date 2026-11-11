# CLAUDE.md - Management Tests

This file provides guidance for the Management module test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet toolchain.cs test` - NEVER use `dotnet test` directly.**

For Management-specific test patterns, device filtering, and test state utilities, see the **Test Infrastructure** section in [`../CLAUDE.md`](../CLAUDE.md#test-infrastructure---advanced-device-filtering).

## Test Projects

- `Yubico.YubiKit.Management.UnitTests` - Unit tests for Management module (xUnit v3)
- `Yubico.YubiKit.Management.IntegrationTests` - Integration tests requiring YubiKey hardware (xUnit v2)

`ScopedDiscoveryReadinessTests` is hardware-free despite living beside the read-only hardware suite. It injects
scan results and an allow list into that suite's actual reference-scan path, including metadata readiness retries.
Complete serial-less bystanders are allowed; missing metadata on any view remains incomplete readiness.
`OwnedMonitorReadOnlyTests` requires two attached allowed serial-bearing USB fixtures, asserts immediate
initialized snapshots/replay and owned lifetimes, and performs sequential live reads only after monitor
disposal. Use the explicit [read-only filter](../../../docs/usage/device-discovery.md#read-only-scoped-discovery-hardware-checks),
not a broad Management project run, for unattended discovery work. Attended hotplug acceptance is separate.

## Advanced Device Filtering

Management uses the shared `[WithYubiKey]` filters. Each attribute yields one placeholder row, bound to
the first matching authorized device at execution. It does not enumerate every matching device.
Multiple attributes bind independently. Use three-component firmware strings; malformed criteria are
currently ignored rather than rejected.

```csharp
[SkippableTheory]
[WithYubiKey(
    MinFirmware = "5.3.0",                     // Only firmware >= 5.3.0
    Capability = DeviceCapabilities.Piv,      // PIV enabled over USB OR NFC
    RequireUsb = true                         // Independent USB availability requirement
)]
public async Task MyTest(YubiKeyTestState state)
{
    var info = await state.Device.GetDeviceInfoAsync();
    Assert.Equal(state.SerialNumber, info.SerialNumber);
}
```

## Test Helper Extension

Use `WithManagementAsync` for automatic session management:

```csharp
[SkippableTheory]
[WithYubiKey(MinFirmware = "5.0.0")]
public async Task MyTest(YubiKeyTestState state) =>
    await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
    {
        // mgmt = ManagementSession (already initialized)
        // cachedDeviceInfo = shared test-state snapshot, not fresh helper-entry metadata
        
        var info = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(cachedDeviceInfo.SerialNumber, info.SerialNumber);
    });
```

## Critical Warnings

The committed allow list authorizes dedicated test devices, including persistent-state changes. Do not
add a second environment-variable gate. Configuration tests must save every changed field, preserve a
recovery route, attempt restoration in `finally` and verify restored values where possible. Restoration
can fail; clear secret buffers in an independent `finally` even if hardware cleanup throws.

Reboot occurs when explicitly requested. End old scopes and rediscover the intended key before opening
a fresh connection; a delay does not reconnect the old session. The no-reboot timeout example in the
module guide checks persistence only, not active CCID behavior. Coordinate physical presence/timing tests
with a human and mark the appropriate traits.

Connection filters do not pin operation transport. For a concrete row, pass
`preferredConnection: state.ConnectionType` and assert `mgmt.ConnectionType`; do not pass a combined
unfiltered connection set as a preference. Current FIPS masks/derived filters have a Core decoding issue,
and flagged form-factor bytes can miss exact base-enum filters. These filters cannot establish FIPS
approval or Bio MPE reset eligibility.

See [`../CLAUDE.md`](../CLAUDE.md#critical-warnings-for-configuration-tests) for safe configuration testing patterns.

## Running Tests

```bash
# Run unit tests only
dotnet toolchain.cs -- test --project Management

# Run unit and integration tests (requires YubiKey)
dotnet toolchain.cs -- test --integration --project Management

# Run specific test class
dotnet toolchain.cs -- test --integration --project Management --filter "FullyQualifiedName~ManagementSessionSimpleTests"
```
