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
- [`ManagementSession.cs`](src/ManagementSession.cs) - Main session class (~236 lines)
- [`DeviceInfo.cs`](src/DeviceInfo.cs) - Device information model (~261 lines)
- [`DeviceConfig.cs`](src/DeviceConfig.cs) - Configuration model (~190 lines)
- [`IYubiKeyExtensions.cs`](src/IYubiKeyExtensions.cs) - Convenience extensions for `IYubiKey`

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
[Theory]
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
[Theory]
[WithYubiKey]
public async Task AllDevices_Test(YubiKeyTestState state) { }

// If only device 12345678 is USB-C, test runs once
[Theory]
[WithYubiKey(FormFactor = FormFactor.UsbCKeychain)]
public async Task UsbC_Test(YubiKeyTestState state) { }
```

### Multiple Attribute Pattern

Use multiple `[WithYubiKey]` attributes to test across different configurations:

```csharp
[Theory]
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
            using var mgmtSession = await yubiKey.CreateManagementSessionAsync(cancellationToken: ct);
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
using var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken);
using var mgmt = await ManagementSession.CreateAsync(connection, cancellationToken: cancellationToken);
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
    reboot: true,
    currentLockCode: lockCode,  // If device is locked
    cancellationToken: cancellationToken);

// Equivalent manual code:
using var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken);
using var mgmt = await ManagementSession.CreateAsync(connection, cancellationToken: cancellationToken);
await mgmt.SetDeviceConfigAsync(config, reboot, lockCode, null, cancellationToken);
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
using var mgmtSession = await yubiKey.CreateManagementSessionAsync(
    scpKeyParams: Scp03KeyParameters.Default,  // Optional SCP
    configuration: customProtocolConfig,       // Optional protocol config
    loggerFactory: loggerFactory,             // Optional logging
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
- Session owns connection - disposes it when session disposes

**Tradeoffs:**
- ✅ Reuse session for multiple operations (more efficient)
- ✅ Full control over SCP, protocol configuration, logging
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
| Need logging | `CreateManagementSessionAsync()` | Need `loggerFactory` parameter |
| Testing (YubiKeyTestState) | `state.WithManagementAsync()` | Test helper, automatic cleanup |

### Common Anti-Patterns

#### ❌ Creating session for single operation

```csharp
// DON'T DO THIS - unnecessary complexity
using var mgmtSession = await yubiKey.CreateManagementSessionAsync();
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

All three extension methods follow the same pattern internally:

1. **Connection creation**: `await yubiKey.ConnectAsync<ISmartCardConnection>()`
2. **Session creation**: `await ManagementSession.CreateAsync(connection, ...)`
3. **Operation**: Call session method
4. **Disposal**: `using` ensures cleanup even on exceptions

The difference is **who manages the session lifecycle**:
- High-level extensions: Method manages lifecycle (automatic)
- Low-level extension: Caller manages lifecycle (manual)

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

Located in [`ManagementTestState.cs`](../Yubico.YubiKit.Tests.Shared/ManagementTestState.cs) (shared test infrastructure):

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
[Theory]
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
[Theory]
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
[Theory]
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
[Theory]
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
[Theory]
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
[Theory]
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

[Theory]
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
[Theory]
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
[Theory]
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

If you **must** test configuration changes:

1. **Use a dedicated test device** not in the shared allowlist
2. **Document the test** clearly as destructive
3. **Reset configuration** at test end (if possible)
4. **Skip by default** with `[SkippableFact]` or environment check
5. **Test in isolation** - never in CI or shared environments

```csharp
// ✅ Safe pattern for configuration testing
[SkippableFact]
public async Task DESTRUCTIVE_ConfigurationChange_DedicatedDevice()
{
    // Check environment variable to explicitly enable
    Skip.IfNot(Environment.GetEnvironmentVariable("YUBIKIT_ALLOW_DESTRUCTIVE_TESTS") == "1",
        "Destructive tests disabled. Set YUBIKIT_ALLOW_DESTRUCTIVE_TESTS=1 to enable.");
    
    // Use specific device, not from shared allowlist
    var dedicatedSerial = 12345678; // Document this requirement
    var device = YubiKeyDevice.FindBySerialNumber(dedicatedSerial);
    Skip.If(device == null, $"Dedicated test device {dedicatedSerial} not found");
    
    using var connection = await device.ConnectAsync<ISmartCardConnection>();
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

// Serializes to TLV format for transmission
Memory<byte> bytes = config.GetBytes(reboot: true, null, null);
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
    ILoggerFactory? loggerFactory = null,
    ScpKeyParameters? scpKeyParams = null,
    CancellationToken cancellationToken = default)
{
    loggerFactory ??= NullLoggerFactory.Instance;
    
    // Two-phase initialization
    var session = new ManagementSession(connection, loggerFactory, scpKeyParams);
    await session.InitializeAsync(configuration, cancellationToken);
    
    return session;
}

private async Task InitializeAsync(
    ProtocolConfiguration? configuration,
    CancellationToken cancellationToken)
{
    // 1. Get firmware version (needed for feature detection)
    _version = await GetVersionAsync(cancellationToken);
    
    // 2. Configure protocol with version info
    _protocol.Configure(_version, configuration);
    
    // 3. Optionally establish SCP and recreate backend
    if (_scpKeyParams is not null && _protocol is ISmartCardProtocol sc)
    {
        _protocol = await sc.WithScpAsync(_scpKeyParams, cancellationToken);
        _backend = new SmartCardBackend(_protocol as ISmartCardProtocol, _version);
    }
    
    _isInitialized = true;
}
```

## Architecture - Backend Pattern

ManagementSession uses the **Backend pattern** to abstract protocol differences between SmartCard (APDU) and FIDO (CTAP HID) without branching in public APIs.

### Internal Structure

```csharp
// ManagementSession delegates all operations to a backend
private readonly IManagementBackend _backend;

// Backend interface defines four operations
internal interface IManagementBackend : IDisposable
{
    ValueTask<byte[]> ReadConfigAsync(int page, CancellationToken ct);
    ValueTask WriteConfigAsync(byte[] config, CancellationToken ct);
    ValueTask SetModeAsync(byte[] data, CancellationToken ct);
    ValueTask DeviceResetAsync(CancellationToken ct);
}
```

### Implementations

- **SmartCardBackend**: Encodes operations as ISO 7816 APDUs (INS: 0x1D, 0x1C, 0x16, 0x1F)
- **FidoBackend**: Encodes operations as CTAP vendor commands (0xC2, 0xC3, 0xC0)

### Key Design Decisions

1. **Backend is stateless**: Doesn't own the protocol or connection
2. **ManagementSession owns disposal**: Backend.Dispose() is a no-op
3. **SCP wrapping works**: Backend can be recreated with SCP-wrapped protocol without disposing connection
4. **Zero branching**: All public methods delegate to `_backend.ReadConfigAsync()` etc.

This matches the Java yubikit-android Backend pattern where Backend doesn't implement Closeable.

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
result = await _backend.ReadConfigAsync(page, ct);
```

Makes the code testable via `IManagementBackend` mocking and eliminates protocol-specific logic from business operations.

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

using var mgmt = await ManagementSession.CreateAsync(
    connection,
    loggerFactory: loggerFactory);
```

### Device Enumeration

```csharp
// Find all connected YubiKeys
var devices = YubiKeyDevice.FindAll();
foreach (var device in devices)
{
    Console.WriteLine($"Serial: {device.SerialNumber}");
    Console.WriteLine($"Firmware: {device.FirmwareVersion}");
    Console.WriteLine($"USB: {device.HasSmartCard}");
}

// Find specific device
var yubiKey = YubiKeyDevice.FindBySerialNumber(12345678);
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
