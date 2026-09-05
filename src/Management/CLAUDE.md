# CLAUDE.md - Management Module

This file provides Claude-specific guidance for working with the Management module. **Read [README.md](README.md) first** for general module documentation.

## Documentation Maintenance

> **Important:** This documentation is subject to change. When working on this module:
> - **Notable changes** to APIs, patterns, or behavior should be documented in both CLAUDE.md and README.md
> - **New features** should include usage examples in README.md and implementation guidance in CLAUDE.md
> - **Breaking changes** require updates to both files with migration guidance
> - **Test infrastructure changes** should be reflected in the test pattern sections below

## Module Context

The Management module is the **primary interface for YubiKey device configuration**. It's unique because:

1. **Device-level operations**: Works across all applications (PIV, OATH, FIDO2, etc.)
2. **No reset mechanism**: Unlike SecurityDomain/PIV, Management has no automatic test reset
3. **Destructive operations**: Configuration changes cause device reboots and can lock out applications
4. **Rich device filtering**: Test infrastructure provides declarative device selection

**Key Files:**
- [`ManagementSession.cs`](src/ManagementSession.cs) - Main session class
- [`DeviceConfig.cs`](src/DeviceConfig.cs) - Configuration model (~190 lines)
- [`IYubiKeyExtensions.cs`](src/IYubiKeyExtensions.cs) - Convenience extensions for `IYubiKey`

Read-only device metadata types returned by `GetDeviceInfoAsync` (`DeviceInfo`, `DeviceCapabilities`, `DeviceFlags`, `FormFactor`, `VersionQualifier`) live in `Core/src/Devices` under `Yubico.YubiKit.Core.Devices`.

## Test Infrastructure - Advanced Device Filtering

### The `[WithYubiKey]` Attribute System

This module showcases the most **powerful test filtering system** in the SDK. Unlike SecurityDomain/PIV which test features, Management tests **device characteristics**.

```csharp
/// <summary>
/// [WithYubiKey] provides declarative device filtering with rich criteria:
/// - Firmware version ranges
/// - Form factor matching
/// - Capability requirements
/// - Transport requirements (USB/NFC)
/// - FIPS status filtering
/// </summary>
[SkippableTheory]
[WithYubiKey(
    MinFirmware = "5.3.0",        // Only firmware >= 5.3.0
    FormFactor = FormFactor.UsbAKeychain,  // Only USB-A keychains
    Capability = DeviceCapabilities.Piv,   // Must have PIV enabled
    RequireUsb = true,            // USB transport required
    FipsCapable = DeviceCapabilities.Piv   // FIPS-capable for PIV
)]
public async Task MyTest(YubiKeyTestState state)
{
    // Test runs ONLY on devices matching ALL criteria
    // Each matching device in appsettings.json runs the test once
}
```

### Multi-Device Testing Pattern

Tests execute **once per matching device**:

```csharp
// appsettings.json has: [12345678, 23456789, 34567890]
// Test runs 3 times (once per device)
[SkippableTheory]
[WithYubiKey]
public async Task AllDevices_Test(YubiKeyTestState state) { }

// If only device 12345678 is USB-C, test runs once
[SkippableTheory]
[WithYubiKey(FormFactor = FormFactor.UsbCKeychain)]
public async Task UsbC_Test(YubiKeyTestState state) { }
```

### Multiple Attribute Pattern

Use multiple `[WithYubiKey]` attributes to test across different configurations:

```csharp
[SkippableTheory]
[WithYubiKey(FormFactor = FormFactor.UsbAKeychain)]
[WithYubiKey(FormFactor = FormFactor.UsbCKeychain)]
[WithYubiKey(FormFactor = FormFactor.UsbABiometricKeychain)]
public async Task MultiFormFactor_Test(YubiKeyTestState state)
{
    // Test runs on all devices with ANY of these form factors
    // Each matching device runs the test once
}
```

## YubiKeyTestState - Device Context

The `YubiKeyTestState` provides **rich device information** without querying:

```csharp
public async Task MyTest(YubiKeyTestState state)
{
    // Pre-populated device information (no query needed)
    int serial = state.SerialNumber;
    FirmwareVersion fw = state.FirmwareVersion;
    FormFactor form = state.FormFactor;
    bool isUsb = state.IsUsbTransport;
    bool isNfc = state.IsNfcTransport;
    IYubiKey device = state.Device;
    
    // Capability checks
    bool hasPiv = state.HasCapability(DeviceCapabilities.Piv);
    bool isFipsCapable = state.IsFipsCapable(DeviceCapabilities.Piv);
    bool isFipsApproved = state.IsFipsApproved(DeviceCapabilities.Piv);
    
    // Firmware version checks
    bool isModern = state.FirmwareVersion.IsAtLeast(5, 3, 0);
}
```

## IYubiKey Extension Methods - C# 14 Extensions

### Modern Extension Syntax

The [`IYubiKeyExtensions.cs`](src/IYubiKeyExtensions.cs) file uses **C# 14's `extension` feature**:

```csharp
public static class IYubiKeyExtensions
{
    // C# 14 syntax: extension(Type param) defines extensions for Type
    extension(IYubiKey yubiKey)
    {
        // Methods here extend IYubiKey
        public async Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken ct = default)
        {
            // 'yubiKey' parameter is implicitly the extension target
            await using var mgmtSession = await yubiKey.CreateManagementSessionAsync(cancellationToken: ct);
            return await mgmtSession.GetDeviceInfoAsync(ct);
        }
    }
}
```

**Why this syntax:**
- Cleaner than traditional `this Type` parameter syntax
- Groups related extensions together
- More explicit about extension target
- Still compiles to standard extension methods

### Three Convenience Patterns

The extension class provides three levels of abstraction:

#### 1. High-Level: GetDeviceInfoAsync

**Use when:** You only need device information, one-time query

```csharp
// Extension handles everything
var deviceInfo = await yubiKey.GetDeviceInfoAsync(cancellationToken);

// Equivalent manual code:
await using var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken);
await using var mgmt = await ManagementSession.CreateAsync(connection, cancellationToken: cancellationToken);
var deviceInfo = await mgmt.GetDeviceInfoAsync(cancellationToken);
```

**Lifecycle:**
- Creates connection (disposed automatically)
- Creates session (disposed automatically)
- Queries device info
- Returns info, disposes session + connection

**Tradeoffs:**
- ✅ Simplest code
- ✅ No resource management needed
- ❌ Can't reuse session for multiple operations
- ❌ Connection overhead repeated for each call

#### 2. High-Level: SetDeviceConfigAsync

**Use when:** Single configuration change, don't need to query device first

```csharp
var config = new DeviceConfig
{
    EnabledCapabilities = new Dictionary<Transport, int>
    {
        { Transport.Usb, (int)DeviceCapabilities.Piv }
    }
};

// Extension handles everything
await yubiKey.SetDeviceConfigAsync(
    config,
    new SetDeviceConfigOptions
    {
        Reboot = true,
        CurrentLockCode = lockCode // If device is locked
    },
    cancellationToken: cancellationToken);

// Equivalent manual code:
await using var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken);
await using var mgmt = await ManagementSession.CreateAsync(connection, cancellationToken: cancellationToken);
await mgmt.SetDeviceConfigAsync(
    config,
    new SetDeviceConfigOptions { Reboot = reboot, CurrentLockCode = lockCode },
    cancellationToken);
```

**Lifecycle:**
- Creates connection (disposed automatically)
- Creates session (disposed automatically)
- Applies configuration
- Disposes session + connection (even if device reboots)

**Tradeoffs:**
- ✅ Single-line configuration changes
- ✅ No resource management needed
- ❌ Can't query device info before/after config change in same session
- ❌ If you need device info + config change, two separate connections

#### 3. Low-Level: CreateManagementSessionAsync

**Use when:** Multiple operations, batch queries, need control over session lifetime

```csharp
// Manual session management for multiple operations
await using var mgmtSession = await yubiKey.CreateManagementSessionAsync(
    new SessionCreationOptions
    {
        ProtocolConfiguration = customProtocolConfig,
        ScpKeyParameters = Scp03KeyParameters.Default
    },
    cancellationToken: cancellationToken);

// Multiple operations in same session
var info1 = await mgmtSession.GetDeviceInfoAsync(cancellationToken);
var info2 = await mgmtSession.GetDeviceInfoAsync(cancellationToken);
// Session stays open for both calls

// YOU are responsible for disposing
```

**Lifecycle:**
- Creates connection (managed by session)
- Creates session
- **Caller owns session** - must dispose
- The connection was opened by this entry point, so the session owns it and disposes it when the session disposes. A connection you opened yourself and passed to `ManagementSession.CreateAsync` stays yours.
- Use `await using`; a missing disposal can retain the physical-device lease and block later opens.

**Tradeoffs:**
- ✅ Reuse session for multiple operations (more efficient)
- ✅ Full control over SCP and protocol configuration
- ✅ Batch operations with consistent state
- ❌ Must manage session disposal (use `using` statement)
- ❌ More verbose code

### Decision Matrix: Which Pattern to Use?

| Scenario | Recommended Pattern | Reason |
|----------|-------------------|--------|
| Single device info query | `yubiKey.GetDeviceInfoAsync()` | Simplest, one-line |
| Single config change | `yubiKey.SetDeviceConfigAsync()` | Simplest, automatic cleanup |
| Multiple queries | `CreateManagementSessionAsync()` | Reuse session, more efficient |
| Query + config change | `CreateManagementSessionAsync()` | Need both in same session |
| SCP authentication required | `CreateManagementSessionAsync()` | Need to pass `scpKeyParams` |
| Custom protocol configuration | `CreateManagementSessionAsync()` | Need `configuration` parameter |
| Need logging | Any | Configure `YubiKitLogging.LoggerFactory` (or DI) once at startup |
| Testing (YubiKeyTestState) | `state.WithManagementAsync()` | Test helper, automatic cleanup |

### Common Anti-Patterns

#### ❌ Creating session for single operation

```csharp
// DON'T DO THIS - unnecessary complexity
await using var mgmtSession = await yubiKey.CreateManagementSessionAsync();
var info = await mgmtSession.GetDeviceInfoAsync();
// (end of method, session disposed)

// DO THIS INSTEAD - simpler
var info = await yubiKey.GetDeviceInfoAsync();
```

#### ❌ Multiple high-level calls

```csharp
// DON'T DO THIS - creates 3 separate sessions/connections
var info1 = await yubiKey.GetDeviceInfoAsync();
var info2 = await yubiKey.GetDeviceInfoAsync();
var info3 = await yubiKey.GetDeviceInfoAsync();

// DO THIS INSTEAD - reuse session
using var mgmt = await yubiKey.CreateManagementSessionAsync();
var info1 = await mgmt.GetDeviceInfoAsync();
var info2 = await mgmt.GetDeviceInfoAsync();
var info3 = await mgmt.GetDeviceInfoAsync();
```

#### ❌ Mixing patterns unnecessarily

```csharp
// DON'T DO THIS - two separate sessions
var info = await yubiKey.GetDeviceInfoAsync();
await yubiKey.SetDeviceConfigAsync(config, reboot: true);

// DO THIS INSTEAD - single session
using var mgmt = await yubiKey.CreateManagementSessionAsync();
var info = await mgmt.GetDeviceInfoAsync();
await mgmt.SetDeviceConfigAsync(config, reboot: true);
```

### Implementation Details

Management extensions that create their own session use the same transport/session pipeline internally:

1. **Transport resolution**: `ResolveSessionTransport` selects exactly one supported transport from
   `SmartCard -> HidFido -> HidOtp`, or uses an explicit valid override. Supplying SCP parameters without
   an override forces SmartCard because SCP is not available over HID.
2. **One-shot connection opening**: `CreateSessionOverTransportAsync` opens exactly one typed connection —
   `ISmartCardConnection`, `IFidoHidConnection`, or `IOtpHidConnection` — for the selected transport.
3. **Session creation**: `await ManagementSession.CreateAsync(connection, ...)`
4. **Operation**: Call the requested session method.
5. **Disposal**: The high-level operation disposes its session; a returned session owns the hidden connection
   until its caller disposes that session.

The difference is **who manages the session lifecycle**:
- High-level extensions: Method manages lifecycle (automatic)
- Low-level extension: Caller manages lifecycle (manual)

Direct `ManagementSession.CreateAsync(connection)` validates that `connection` is SmartCard, FIDO
HID, or OTP HID before the `ApplicationSession` base attaches its one-session guard. It borrows the
connection and never disposes it. The creator must dispose the connection with `await using`; there
is no finalizer backstop. One live session per connection is allowed, and sequential reuse after
session disposal is supported.

A grouped physical YubiKey admits one live connection across CCID, FIDO HID, and OTP HID. Management
selects exactly one transport from `SmartCard -> HidFido -> HidOtp` (or an explicit override) and opens it
once. `ConnectionInUseException`, PC/SC sharing errors, and initialization failures propagate without
trying another interface.

### Testing Considerations

When writing tests, prefer `YubiKeyTestState.WithManagementAsync()` over any of these:

```csharp
// ✅ Best for tests - automatic cleanup, cached device info
await state.WithManagementAsync(async (mgmt, deviceInfo) =>
{
    // mgmt is ready, deviceInfo is pre-queried
});

// ⚠️ Acceptable but less convenient
using var mgmt = await state.Device.CreateManagementSessionAsync();
var info = await mgmt.GetDeviceInfoAsync();

// ❌ Avoid in tests - less efficient
var info = await state.Device.GetDeviceInfoAsync();
```

## Test Helper Extensions

### WithManagementAsync Pattern

Located in [`YubiKeyTestStateExtensions.cs`](../Tests.Shared/YubiKeyTestStateExtensions.cs) (shared test infrastructure):

```csharp
extension(YubiKeyTestState state)
{
    public async Task WithManagementAsync(
        Func<ManagementSession, DeviceInfo, Task> action,
        ScpKeyParameters? scpKeyParams = null,
        ProtocolConfiguration? configuration = null,
        CancellationToken cancellationToken = default)
    {
        // Automatically:
        // 1. Creates connection
        // 2. Creates Management session (with optional SCP)
        // 3. Queries device info
        // 4. Calls your action
        // 5. Disposes everything properly
    }
}
```

Usage pattern:

```csharp
[SkippableTheory]
[WithYubiKey(MinFirmware = "5.0.0")]
public async Task MyTest(YubiKeyTestState state) =>
    await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
    {
        // mgmt = ManagementSession (already initialized)
        // cachedDeviceInfo = DeviceInfo from initial query
        
        var freshInfo = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(cachedDeviceInfo.SerialNumber, freshInfo.SerialNumber);
    });
```

## Common Test Patterns

### 1. Read-Only Device Information Tests

```csharp
[SkippableTheory]
[WithYubiKey]
public async Task GetDeviceInfo_ReturnsValidData(YubiKeyTestState state) =>
    await state.WithManagementAsync(async (mgmt, cachedInfo) =>
    {
        var info = await mgmt.GetDeviceInfoAsync();
        
        Assert.True(info.SerialNumber > 0);
        Assert.Equal(state.SerialNumber, info.SerialNumber);
        Assert.Equal(state.FirmwareVersion, info.FirmwareVersion);
    });
```

### 2. Firmware Version-Specific Tests

```csharp
[SkippableTheory]
[WithYubiKey(MinFirmware = "5.7.0")]
public async Task ModernFeature_Firmware57Plus_Works(YubiKeyTestState state) =>
    await state.WithManagementAsync(async (mgmt, cachedInfo) =>
    {
        // This test only runs on firmware >= 5.7.0
        Assert.True(state.FirmwareVersion.IsAtLeast(5, 7, 0));
        
        // Test modern features
        var info = await mgmt.GetDeviceInfoAsync();
        Assert.NotNull(info.VersionQualifier);
    });
```

### 3. Form Factor-Specific Tests

```csharp
[SkippableTheory]
[WithYubiKey(FormFactor = FormFactor.UsbABiometricKeychain)]
public async Task BiometricFeatures_BioKeys_Present(YubiKeyTestState state)
{
    Assert.Equal(FormFactor.UsbABiometricKeychain, state.FormFactor);
    
    await state.WithManagementAsync(async (mgmt, cachedInfo) =>
    {
        var info = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(FormFactor.UsbABiometricKeychain, info.FormFactor);
        
        // Bio keys have modern firmware
        Assert.True(info.FirmwareVersion.Major >= 5);
    });
}
```

### 4. Capability-Specific Tests

```csharp
[SkippableTheory]
[WithYubiKey(Capability = DeviceCapabilities.Piv)]
public async Task PivCapability_EnabledDevices_Accessible(YubiKeyTestState state)
{
    Assert.True(state.HasCapability(DeviceCapabilities.Piv));
    
    await state.WithManagementAsync(async (mgmt, cachedInfo) =>
    {
        var info = await mgmt.GetDeviceInfoAsync();
        
        // Verify PIV is enabled on USB or NFC
        bool pivEnabled = (info.UsbEnabled & DeviceCapabilities.Piv) != 0 ||
                         (info.NfcEnabled & DeviceCapabilities.Piv) != 0;
        Assert.True(pivEnabled);
    });
}
```

### 5. FIPS Testing

```csharp
[SkippableTheory]
[WithYubiKey(FipsCapable = DeviceCapabilities.Piv)]
public async Task FipsCapable_PivDevices_HasSupport(YubiKeyTestState state)
{
    Assert.True(state.IsFipsCapable(DeviceCapabilities.Piv));
    
    await state.WithManagementAsync(async (mgmt, cachedInfo) =>
    {
        var info = await mgmt.GetDeviceInfoAsync();
        Assert.True((info.FipsCapabilities & DeviceCapabilities.Piv) != 0);
    });
}

[SkippableTheory]
[WithYubiKey(FipsApproved = DeviceCapabilities.Piv)]
public async Task FipsApproved_PivDevices_InFipsMode(YubiKeyTestState state)
{
    Assert.True(state.IsFipsApproved(DeviceCapabilities.Piv));
    
    await state.WithManagementAsync(async (mgmt, cachedInfo) =>
    {
        var info = await mgmt.GetDeviceInfoAsync();
        Assert.True((info.FipsApproved & DeviceCapabilities.Piv) != 0);
    });
}
```

### 6. Multi-Criteria Filtering

```csharp
[SkippableTheory]
[WithYubiKey(
    MinFirmware = "5.0.0",
    RequireUsb = true,
    Capability = DeviceCapabilities.Piv)]
public async Task AdvancedFiltering_ModernUsbPiv_Works(YubiKeyTestState state)
{
    // Multiple requirements enforced by attribute:
    Assert.True(state.FirmwareVersion.IsAtLeast(5, 0, 0));
    Assert.True(state.IsUsbTransport);
    Assert.True(state.HasCapability(DeviceCapabilities.Piv));
    
    await state.WithManagementAsync(async (mgmt, cachedInfo) =>
    {
        var info = await mgmt.GetDeviceInfoAsync();
        
        // All criteria verified by infrastructure
        Assert.True(info.FirmwareVersion.Major >= 5);
        Assert.True((info.UsbEnabled & DeviceCapabilities.Piv) != 0);
    });
}
```

## Critical Warnings for Configuration Tests

### ⚠️ DO NOT Write Configuration Change Tests

**NEVER** write tests that modify device configuration in the shared test suite:

```csharp
// ❌ NEVER DO THIS - Breaks other tests
[SkippableTheory]
[WithYubiKey]
public async Task BAD_TEST_DisableCapabilities(YubiKeyTestState state) =>
    await state.WithManagementAsync(async (mgmt, cachedInfo) =>
    {
        var config = new DeviceConfig
        {
            EnabledCapabilities = new Dictionary<Transport, int>
            {
                { Transport.Usb, (int)DeviceCapabilities.Otp } // Disables PIV!
            }
        };
        
        // This BREAKS all PIV tests that run after this!
        await mgmt.SetDeviceConfigAsync(config, reboot: true);
    });
```

**Why this is bad:**
1. Device reboots (3+ second delay, disrupts test flow)
2. Changes persist across test runs
3. Breaks tests that depend on specific capabilities
4. Requires manual device reconfiguration to fix
5. May lock configuration if lock code is set

### Safe Configuration Testing

The problem with capability changes is **blast radius across the suite**, not destruction as such. An allow-listed device is a dedicated test device and destructive operations against it are expected — see [docs/TESTING.md](../../docs/TESTING.md#hardware-authorization), which is canonical: *"the allow list is the boundary, and adding a second one would only obscure it."* Do **not** gate these behind an extra environment variable.

What makes `SetDeviceConfigAsync` special is that it reboots the device and can disable applications other tests depend on. So the rule is restore-what-you-changed, not don't-run-it:

1. **Restore configuration** in a `finally` — always, including on failure
2. **Document the test** clearly as configuration-mutating
3. **Account for the reboot** — the device disappears and re-enumerates
4. **Avoid lock codes** in tests; a set lock code can make the change unrecoverable

```csharp
// ✅ Safe pattern for configuration testing
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
public async Task ConfigurationChange_AppliesAndRestores(YubiKeyTestState state)
{
    using var connection = await state.Device.ConnectAsync<ISmartCardConnection>();
    using var mgmt = await ManagementSession.CreateAsync(connection);
    
    // Save original config
    var originalInfo = await mgmt.GetDeviceInfoAsync();
    var originalUsb = originalInfo.UsbEnabled;
    var originalNfc = originalInfo.NfcEnabled;
    
    try
    {
        // Perform destructive test
        var testConfig = new DeviceConfig { /* ... */ };
        await mgmt.SetDeviceConfigAsync(testConfig, reboot: true);
        
        // Wait for reboot
        await Task.Delay(3000);
        
        // Verify changes
        // ...
    }
    finally
    {
        // Restore original configuration
        var restoreConfig = new DeviceConfig
        {
            EnabledCapabilities = new Dictionary<Transport, int>
            {
                { Transport.Usb, (int)originalUsb },
                { Transport.Nfc, (int)originalNfc }
            }
        };
        await mgmt.SetDeviceConfigAsync(restoreConfig, reboot: true);
    }
}
```

## Performance Considerations

### Device Info Caching

`WithManagementAsync` queries `DeviceInfo` once and passes it to your action:

```csharp
await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
{
    // cachedDeviceInfo was queried once at the start
    // Use it instead of re-querying if data hasn't changed
    
    var serial = cachedDeviceInfo.SerialNumber; // ✅ Fast
    
    // Only query again if testing consistency
    var freshInfo = await mgmt.GetDeviceInfoAsync(); // ⚠️ APDU overhead
    Assert.Equal(cachedDeviceInfo.SerialNumber, freshInfo.SerialNumber);
});
```

### Session Reuse

If testing multiple operations, reuse the session:

```csharp
await state.WithManagementAsync(async (mgmt, info) =>
{
    // Multiple operations in one session
    var info1 = await mgmt.GetDeviceInfoAsync();
    var info2 = await mgmt.GetDeviceInfoAsync();
    var info3 = await mgmt.GetDeviceInfoAsync();
    
    // More efficient than creating 3 separate sessions
});
```

## Model Patterns

### DeviceInfo - Immutable Record Struct

```csharp
public readonly record struct DeviceInfo
{
    public required bool IsSky { get; init; }
    public required FormFactor FormFactor { get; init; }
    public int? SerialNumber { get; init; }
    public required FirmwareVersion FirmwareVersion { get; init; }
    // ... more properties
}

// Usage: immutable, compared by value
var info1 = await mgmt.GetDeviceInfoAsync();
var info2 = await mgmt.GetDeviceInfoAsync();
Assert.Equal(info1.SerialNumber, info2.SerialNumber); // Value comparison
```

### DeviceConfig - Configuration Builder

```csharp
var config = new DeviceConfig
{
    EnabledCapabilities = new Dictionary<Transport, int>
    {
        { Transport.Usb, (int)(DeviceCapabilities.Piv | DeviceCapabilities.Oath) }
    },
    AutoEjectTimeout = 30,
    DeviceFlags = DeviceConfig.FlagEject,
    ChallengeResponseTimeout = 15
};

// The session serializes the configuration and clears the encoded buffer after transmission.
await session.SetDeviceConfigAsync(
    config,
    new SetDeviceConfigOptions { Reboot = true },
    cancellationToken);
```

### Capability Flags Pattern

```csharp
// Flags enum - use bitwise operations
var capabilities = DeviceCapabilities.Piv | DeviceCapabilities.Oath;

// Check if specific capability is set
bool hasPiv = (capabilities & DeviceCapabilities.Piv) != 0;

// Add capability
capabilities |= DeviceCapabilities.Fido2;

// Remove capability  
capabilities &= ~DeviceCapabilities.Otp;

// Check multiple
bool hasPivAndOath = (capabilities & (DeviceCapabilities.Piv | DeviceCapabilities.Oath)) 
    == (DeviceCapabilities.Piv | DeviceCapabilities.Oath);
```

## Firmware Version Handling

### Version Qualifier System

```csharp
var info = await mgmt.GetDeviceInfoAsync();

// Different version representations
FirmwareVersion fw = info.FirmwareVersion;           // e.g., 5.7.2
VersionQualifier qualifier = info.VersionQualifier;  // e.g., "5.7.2-rc1"
string versionName = info.VersionName;               // Display string

// VersionQualifierType enum values:
// - Final: Production release (5.7.2)
// - ReleaseCandidate: RC version (5.7.2-rc1)
// - Development: Dev version (5.7.2-dev)
```

### Feature Gating by Firmware

```csharp
private static readonly Feature FeatureDeviceReset =
    new("Device Reset", 5, 6, 0);

private void EnsureSupports(Feature feature)
{
    if (_version < feature.Version)
        throw new NotSupportedException(
            $"{feature.Name} requires firmware {feature.Version} or later");
}

// Usage
public async Task ResetDeviceAsync(CancellationToken cancellationToken = default)
{
    EnsureSupports(FeatureDeviceReset); // Throws if firmware < 5.6.0
    // ...
}
```

## Session Initialization Pattern

```csharp
public static async Task<ManagementSession> CreateAsync(
    IConnection connection,
    ProtocolConfiguration? configuration = null,
    ScpKeyParameters? scpKeyParams = null,
    CancellationToken cancellationToken = default)
{
    // Two-phase initialization
    var session = new ManagementSession(connection, configuration, scpKeyParams);
    await session.InitializeAsync(configuration, cancellationToken);

    return session;
}

private async Task InitializeAsync(
    ProtocolConfiguration? configuration,
    CancellationToken cancellationToken)
{
    // 1. Get firmware version (needed for feature detection)
    FirmwareVersion = await GetVersionAsync(cancellationToken);

    // 2. Configure protocol with version info
    Protocol!.Configure(FirmwareVersion, configuration);

    // 3. Optionally establish SCP through ApplicationSession.InitializeProtocolAsync(...)
    //    when the effective protocol is Core's PC/SC SmartCard implementation.

    IsInitialized = true;
}
```

## Architecture - Backend Pattern

ManagementSession uses the **Backend pattern** to abstract protocol differences between SmartCard (APDU) and FIDO (CTAP HID) without branching in public APIs.

### Internal Structure

```csharp
// ManagementSession delegates transport-specific work to a backend
private IManagementBackend _backend = null!;

// Backend abstraction: deliberately NOT IDisposable
internal interface IManagementBackend
{
    ValueTask<FirmwareVersion?> InitializeAsync(CancellationToken cancellationToken);
    ValueTask WriteConfigAsync(ReadOnlyMemory<byte> config, CancellationToken cancellationToken);
    ValueTask SetModeAsync(byte[] data, CancellationToken cancellationToken);
    ValueTask DeviceResetAsync(CancellationToken cancellationToken);
}
```

Device-info reads do not go through the backend; `ManagementSession` uses `DeviceInfoReader.ReadAsync(_protocol, ...)` directly.

### Implementations

- **SmartCardBackend**: Encodes operations as ISO 7816 APDUs (INS: 0x1D, 0x1C, 0x16, 0x1F)
- **FidoHidBackend**: Encodes operations as CTAP vendor commands (0xC2, 0xC3, 0xC0)
- **OtpBackend**: Encodes operations as OTP HID slot commands with CRC validation

### Key Design Decisions

1. **Backend owns nothing**: it borrows the protocol and holds no disposable state, so it is not `IDisposable`. Ownership rule is Core's — see [Core CLAUDE.md](../Core/CLAUDE.md) gotcha #2 and the `IProtocol` doc comment: the session disposes the protocol, and whoever created the connection disposes it.
2. **SCP wrapping works**: the backend can be recreated over an SCP-wrapped protocol without disposing anything.
3. **Zero branching**: all public methods delegate to `_backend`, so no protocol-specific logic leaks into business operations, and the session is testable against a fake `IManagementBackend`.

This matches the Java yubikit-android Backend pattern where Backend doesn't implement Closeable. `ManagementBackendLifecycleTests` pins the non-disposable invariant.

### Why This Matters

**Before (protocol branching):**
```csharp
if (_fidoProtocol is not null)
    result = await _fidoProtocol.SendVendorCommandAsync(0xC2, data, ct);
else
    result = await _smartCardProtocol.TransmitAsync(apdu, ct);
```

**After (backend delegation):**
```csharp
await _backend.WriteConfigAsync(config, ct);
```

## TLV Encoding/Decoding

Device info uses TLV (Tag-Length-Value) encoding:

```csharp
// GetDeviceInfoAsync implements multi-page TLV retrieval
byte page = 0;
var allPagesTlvs = new List<Tlv>();

while (hasMoreData)
{
    // Backend abstracts protocol (APDU for SmartCard, CTAP for FIDO)
    var encodedResult = await _backend.ReadConfigAsync(page, cancellationToken);
    
    // Decode TLVs from response
    var pageTlvs = TlvHelper.DecodeList(encodedResult.AsSpan()[1..]);
    allPagesTlvs.AddRange(pageTlvs);
    
    // Check for "more data" indicator
    var moreData = pageTlvs.SingleOrDefault(t => t.Tag == 0x10);
    hasMoreData = moreData?.Length == 1 && moreData.Value.Span[0] == 1;
    ++page;
}

// Parse all TLVs into DeviceInfo struct
return DeviceInfo.CreateFromTlvs(allPagesTlvs, _version);
```

## Debugging Tips

### Enable Verbose Logging

```csharp
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Trace);
    builder.AddConsole();
});

YubiKitLogging.LoggerFactory = loggerFactory;

using var mgmt = await ManagementSession.CreateAsync(
    connection);
```

### Device Enumeration

```csharp
// Find all connected YubiKeys
var devices = await YubiKeyManager.FindAllAsync(forceRescan: true, cancellationToken);
foreach (var device in devices)
{
    var info = await device.GetDeviceInfoAsync(cancellationToken);
    Console.WriteLine($"Serial: {info.SerialNumber}");
    Console.WriteLine($"Firmware: {info.FirmwareVersion}");
    Console.WriteLine($"USB enabled: {info.UsbEnabled}");
}

// Find specific device
var yubiKey = devices.SingleOrDefault(device => device.SerialNumber == 12345678);
```

## Known Gotchas

1. **Configuration Lock**: Once locked, can only unlock with correct 16-byte code (no recovery on firmware <5.6)
2. **Reboot Required**: Capability changes require device reboot (~3 seconds, all sessions terminated)
3. **Enumeration Delay**: After reboot, wait 3+ seconds before re-enumerating device
4. **USB Capability Minimum**: Cannot disable all USB capabilities (at least one required)
5. **Form Factor Constants**: Form factor enum includes flags (0x80=FIPS, 0x40=SKY) in upper bits
6. **NFC Availability**: Not all YubiKeys have NFC; check `NfcSupported` before configuring NFC
7. **FIPS Mode Restrictions**: FIPS-approved devices have configuration restrictions
8. **Version from Select**: Firmware version from SELECT is less reliable than from DeviceInfo

## Related Modules

- **Core.YubiKey**: IYubiKey interface, device enumeration
- **Core.SmartCard**: Protocol abstractions, APDU handling
- **Core.SmartCard.Scp**: SCP03/SCP11 for secure management
- **Tests.Shared**: YubiKeyTestState, test infrastructure with `[WithYubiKey]` attribute
