# CLAUDE.md - Management Module

This file provides Claude-specific guidance for working with the Management module. **Read [README.md](README.md) first** for general module documentation.

## Documentation Maintenance

> **Important:** This documentation is subject to change. When working on this module:
> - **Notable changes** to APIs, patterns, or behavior should be documented in both CLAUDE.md and README.md
> - **New features** should include usage examples in README.md and implementation guidance in CLAUDE.md
> - **Breaking changes** require updates to both files with migration guidance
> - **Test infrastructure changes** should be reflected in the test pattern sections below

## Module Context

`ApplicationSession` prepares the protocol before Management probes firmware. On HID FIDO, the session reads
the firmware from the already-initialized channel and does not initialize it again. SCP requested on either
HID transport is rejected before the first initialization exchange.

The Management module is the primary interface for YubiKey device configuration:

1. **Device-level operations**: Works across all applications (PIV, OATH, FIDO2, etc.)
2. **No automatic test reset**: Test setup does not reset device configuration. The public combined reset
   operation is for Bio Multi-protocol Edition, not a generic test cleanup primitive.
3. **Persistent operations**: Configuration changes can disable applications; an explicit reboot request
   can disconnect the device and disrupt other sessions.
4. **Shared device filtering**: Tests use the same Tests.Shared harness as other applets.

**Key Files:**
- [`ManagementSession.cs`](src/ManagementSession.cs) - Main session class
- [`DeviceConfig.cs`](src/DeviceConfig.cs) - Nullable configuration settings and TLV encoding
- [`SetDeviceConfigOptions.cs`](src/SetDeviceConfigOptions.cs) - Reboot request and per-command lock codes
- [`IYubiKeyExtensions.cs`](src/IYubiKeyExtensions.cs) - Convenience extensions for `IYubiKey`

Read-only device metadata types returned by `GetDeviceInfoAsync` (`DeviceInfo`, `DeviceCapabilities`, `DeviceFlags`, `FormFactor`, `VersionQualifier`) live in `Core/src/Devices` under `Yubico.YubiKit.Core.Devices`.

### CLI selection and freshness

The active standalone `examples/ManagementTool/Cli/Prompts/DeviceSelector.cs` is a thin adapter to the
concrete selector in `Cli.Shared`, configured with SmartCard and `RequireChoice`. Do not widen it to HID
because the SDK supports HID: this preserves the actual tool's eligibility, including its reset menu.
Full All discovery already groups composite views; the old three-entries-per-key rationale is obsolete.
Unified `YkCommandBase` reuses selection metadata for its context/banner, while Management info/config/reset
explicitly call `ReadLiveInfoAsync`, carrying `SessionOptions` and never substituting stale context info on failure.
See [CLI selection policies](../../docs/usage/device-discovery.md#selection-in-the-cli-examples).

## Test Infrastructure - Advanced Device Filtering

### The `[WithYubiKey]` Attribute System

Use the shared filters to select dedicated, allow-listed devices with the characteristics needed by an
operation. Filters describe cached device metadata; they do not prove operation support or pin a session's
transport. Use three-component firmware strings, for example `"5.3.0"`; malformed strings are currently
ignored by the shared parser rather than rejected.

```csharp
[SkippableTheory]
[WithYubiKey(
    MinFirmware = "5.3.0",
    Capability = DeviceCapabilities.Piv,
    RequireUsb = true)]
public async Task ReadMetadata_ForMatchingKey(YubiKeyTestState state)
{
    await state.WithManagementAsync(async (mgmt, _) =>
    {
        var info = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(state.SerialNumber, info.SerialNumber);
    });
}
```

### Multi-Device Testing Pattern

Each `[WithYubiKey]` attribute produces one placeholder row. At execution, the row binds to the first
matching authorized device, not to every matching device. With three allowed serials and one attribute,
there is still only one row. Multiple attributes bind independently and can select the same physical key.
For a genuine device matrix, use explicit selection criteria or a separately designed enumeration test;
do not infer matrix coverage from the number of connected keys.

```csharp
[SkippableTheory]
[WithYubiKey]
public async Task FirstMatchingKey_ReturnsSameSerial(YubiKeyTestState state)
{
    var info = await state.Device.GetDeviceInfoAsync();
    Assert.Equal(state.SerialNumber, info.SerialNumber);
}
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
    // Three independently bound rows; each row requires its attribute's form factor.
    var info = await state.Device.GetDeviceInfoAsync();
    Assert.Equal(state.FormFactor, info.FormFactor);
}
```

Current Core parsing can retain product flag bits in `FormFactor`, so exact base-enum filters can miss
flagged devices. The enum itself is not a flags enum. FIPS capability/approval tags also currently use
the wrong bit mapping; `FipsCapable`/`FipsApproved` filters and their derived helpers cannot establish
application approval. `IsFips` is a distinct manufacturing indicator, not an operating mode. Do not
use these fields to identify Bio MPE reset eligibility.

## YubiKeyTestState - Device Context

`YubiKeyTestState` exposes cached metadata from shared device initialization. These property reads do
not refresh it. The first state access can trigger lazy discovery/binding; a later configuration write
does not update the cache. Use an explicit session read for current persisted configuration.

```csharp
    // Composable fragment inside a test receiving YubiKeyTestState state.
    int? serial = state.SerialNumber;
    FirmwareVersion fw = state.FirmwareVersion;
    FormFactor form = state.FormFactor;
    bool isUsb = state.IsUsbTransport;
    bool isNfc = state.IsNfcTransport;
    IYubiKey device = state.Device;
    
    // Capability checks
    bool hasPiv = state.HasCapability(DeviceCapabilities.Piv);
    
    // Firmware version checks
    bool isModern = state.FirmwareVersion.IsAtLeast(5, 3, 0);
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
        public async Task<DeviceInfo> GetDeviceInfoAsync(
            SessionCreationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            // 'yubiKey' parameter is implicitly the extension target
            await using var mgmtSession = await yubiKey.CreateManagementSessionAsync(options, cancellationToken);
            return await mgmtSession.GetDeviceInfoAsync(cancellationToken);
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

Consumer fragments below are independent scopes, not one concatenated program. They assume an
`IYubiKey yubiKey` and `CancellationToken cancellationToken`, with no other live connection to that key.
Import Management, Core.Devices, Core.Sessions and the typed transport namespaces needed by the fragment.
Test fragments assume a `YubiKeyTestState state` from the allow-listed shared harness.

#### 1. High-Level: GetDeviceInfoAsync

**Use when:** You only need device information, one-time query

```csharp
var deviceInfo = await yubiKey.GetDeviceInfoAsync(cancellationToken: cancellationToken);
```

For an explicitly SmartCard-capable key, the manual alternative is below. The one-shot extension can
also select HID, so this is not an equivalent transport choice for every key. Compile/run the alternatives
independently, after any previous session has ended.

```csharp
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

**Use when:** One configuration write whose supported settings and authorization are already known.
This fragment assumes an unlocked device that supports PIV over USB. Its full replacement mask disables
other USB applications; use read/modify/write if they must remain enabled.

```csharp
var config = new DeviceConfig { UsbEnabled = DeviceCapabilities.Piv };

// Extension handles everything
await yubiKey.SetDeviceConfigAsync(
    config,
    new SetDeviceConfigOptions
    {
        Reboot = true,
    },
    cancellationToken: cancellationToken);

```

The explicit SmartCard alternative uses the same reboot request and unlocked-device prerequisite:

```csharp
var config = new DeviceConfig { UsbEnabled = DeviceCapabilities.Piv };
await using var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken);
await using var mgmt = await ManagementSession.CreateAsync(connection, cancellationToken: cancellationToken);
await mgmt.SetDeviceConfigAsync(
    config,
    new SetDeviceConfigOptions { Reboot = true },
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

SCP parameters are caller-owned and borrowed for initialization. This example creates them before the
session so the session is disposed first, then the parameter scope disposes its owned static key arrays.

```csharp
// Requires a SmartCard-capable key configured with the supplied SCP03 keys.
using var scpParameters = Scp03KeyParameters.Default;
await using var mgmtSession = await yubiKey.CreateManagementSessionAsync(
    new SessionCreationOptions
    {
        ProtocolConfiguration = customProtocolConfig,
        ScpKeyParameters = scpParameters
    },
    cancellationToken: cancellationToken);

// Multiple operations in same session
var info1 = await mgmtSession.GetDeviceInfoAsync(cancellationToken);
var info2 = await mgmtSession.GetDeviceInfoAsync(cancellationToken);
// Session stays open for both calls

// At scope end, the session is disposed before the caller-owned SCP parameters.
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
- ✅ One connection held across sequential operations (not an atomic transaction)
- ❌ Must manage session disposal (prefer `await using`)
- ❌ More verbose code

### Decision Matrix: Which Pattern to Use?

| Scenario | Recommended Pattern | Reason |
|----------|-------------------|--------|
| Single device info query | `yubiKey.GetDeviceInfoAsync()` | Simplest, one-line |
| Single config change | `yubiKey.SetDeviceConfigAsync()` | Simplest, automatic cleanup |
| Multiple queries | `CreateManagementSessionAsync()` | Reuse session, more efficient |
| Query + config change | `CreateManagementSessionAsync()` | Need both in same session |
| SCP authentication required | Pass `SessionCreationOptions` to either pattern | Both one-shot operations and session creation accept `ScpKeyParameters` |
| Custom protocol configuration | Pass `SessionCreationOptions` to either pattern | Both one-shot operations and session creation accept `ProtocolConfiguration` |
| Need logging | Any | Configure `YubiKitLogging.LoggerFactory` (or DI) once at startup |
| Testing (YubiKeyTestState) | `state.WithManagementAsync()` | Test helper, automatic cleanup |

### Choosing a useful lifetime

A session for one operation is valid; the one-shot extension simply saves lifetime-management code.
Separate one-shot calls also remain appropriate when an application intentionally releases the key
between operations. Reuse a session when a sequence should hold one connection, but do not assume that
reuse makes several public calls atomic or prevents another process from accessing the hardware.

For a query followed by a write, keep the read/modify/write intent visible. This example removes OTP
while preserving the other reported USB bits and rejects a resulting empty set:

```csharp
await using var mgmt = await yubiKey.CreateManagementSessionAsync(cancellationToken: cancellationToken);
var info = await mgmt.GetDeviceInfoAsync(cancellationToken);
var remaining = info.UsbEnabled & ~DeviceCapabilities.Otp;
if ((remaining & info.UsbSupported) == DeviceCapabilities.None)
{
    throw new InvalidOperationException("Preserve a supported USB application.");
}
await mgmt.SetDeviceConfigAsync(
    new DeviceConfig { UsbEnabled = remaining },
    new SetDeviceConfigOptions { Reboot = true },
    cancellationToken);
// End this scope; rediscover the intended key and open a fresh connection after reboot.
```

The example assumes unlocked configuration. For protected writes, supply the current 16-byte code in
each operation's options and clear caller-owned buffers in `finally`, independently of hardware cleanup.

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
is no session finalizer recovery; a connection's best-effort native finalizer is not a lifetime guarantee.
One live session per connection is allowed, and sequential reuse after
session disposal is supported.

A direct factory does not select another interface: `PreferredConnectionType`, if supplied, asserts
the already-open connection's type. A mismatch fails rather than reconnecting. Failed direct creation
cleans up session/protocol state without disposing that borrowed connection; its creator still owns it.

Within the process, a grouped physical YubiKey admits one live connection across its known member
interfaces. Ungrouped records cannot establish cross-interface exclusion. Management
selects exactly one transport from `SmartCard -> HidFido -> HidOtp` (or an explicit override) and opens it
once. `ConnectionInUseException`, PC/SC sharing errors, and initialization failures propagate without
trying another interface.

### Testing Considerations

Use `YubiKeyTestState.WithManagementAsync()` when the test needs a session and the shared state's
cached metadata. Direct sessions and one-shot reads are also valid when they better express the test.

```csharp
// Session helper: automatic cleanup and cached device info.
await state.WithManagementAsync(async (mgmt, deviceInfo) =>
{
    var freshInfo = await mgmt.GetDeviceInfoAsync();
    Assert.Equal(deviceInfo.SerialNumber, freshInfo.SerialNumber);
});

// A one-shot alternative, after the helper's session has been disposed:
var oneShotInfo = await state.Device.GetDeviceInfoAsync();
```

## Test Helper Extensions

### WithManagementAsync Pattern

Located in [`YubiKeyTestStateExtensions.cs`](../Tests.Shared/YubiKeyTestStateExtensions.cs) (shared test infrastructure):

The async overload accepts `(action, configuration, scpKeyParams, preferredConnection,
cancellationToken)`, where `action` is `Func<ManagementSession, DeviceInfo, Task>`, `configuration` is
`ProtocolConfiguration?`, `scpKeyParams` is `ScpKeyParameters?`, and `preferredConnection` is
`ConnectionType?`. Optional values default to null and the token defaults to `default`. Use named
arguments for policy settings to avoid confusing protocol configuration with SCP parameters.

It creates an owned session through `state.Device.CreateManagementSessionAsync`, calls the action with
`state.DeviceInfo`, then disposes the session and hidden connection. It does not refresh the callback's
metadata. Session initialization may perform a separate firmware-discovery read; that read is not the
source of the cached callback value.

Usage pattern:

```csharp
[SkippableTheory]
[WithYubiKey(MinFirmware = "5.0.0")]
public async Task MyTest(YubiKeyTestState state) =>
    await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
    {
        // mgmt = ManagementSession (already initialized)
        // cachedDeviceInfo = shared test-state snapshot, not a query at helper entry
        
        var freshInfo = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(cachedDeviceInfo.SerialNumber, freshInfo.SerialNumber);
    });
```

## Common Test Patterns

### 1. Read-Only Device Information Tests

`ScopedDiscoveryReadOnlyTests` exercises Core's public `FindAllAsync` and `CachedDeviceInfo` contracts with
independent live Management reads. Unlike `[WithYubiKey]`'s first-match binding, it loops over all attached
allow-listed serial-bearing USB fixtures. It owns no background monitor: monitoring can probe disjoint
sibling interfaces while a scoped connection is held. The applet test base likewise no longer starts tracking. Finish scans
before opening any live connection, then asynchronously dispose each borrowed session and connection
before the next one. Its bounded retries report missing metadata and connection contention without
swallowing unrelated errors. Excluded native call counts and disjoint ownership limits belong in Core's
deterministic scope tests, not concurrent hardware experiments. The canonical read-only filter and fixture
requirements are in [device discovery testing](../../docs/usage/device-discovery.md#read-only-scoped-discovery-hardware-checks).

`OwnedMonitorReadOnlyTests` additionally requires at least two attached allowed keys and exercises the public
initialized factory, immediate snapshot, initial replay, subscriber-local cancellation, factory-token
independence, disposal/recreate, and combined-interface conveniences/no-match predicates. It reuses the
scoped suite's readiness/authorization and semantic metadata/live-read helpers, not its background lifetime
(there is none). Incomplete initial snapshots fail immediately. It disposes every monitor before explicit
live comparisons; do not introduce monitor/live-read overlap to shorten the tests. Read-only operations
need neither UP nor UV, and these cases never mutate key state or satisfy attended hotplug acceptance.

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
public async Task DeviceInfo_Firmware57Plus_HasVersionDisplay(YubiKeyTestState state) =>
    await state.WithManagementAsync(async (mgmt, cachedInfo) =>
    {
        // This test only runs on firmware >= 5.7.0
        Assert.True(state.FirmwareVersion.IsAtLeast(5, 7, 0));
        
        // Check the reported display value, not generic firmware feature support.
        var info = await mgmt.GetDeviceInfoAsync();
        Assert.False(string.IsNullOrWhiteSpace(info.VersionName));
        Assert.Equal(cachedInfo.SerialNumber, info.SerialNumber);
    });
```

### 3. Form Factor-Specific Tests

```csharp
[SkippableTheory]
[WithYubiKey(FormFactor = FormFactor.UsbABiometricKeychain)]
public async Task BioFormFactor_MetadataMatchesCache(YubiKeyTestState state)
{
    Assert.Equal(FormFactor.UsbABiometricKeychain, state.FormFactor);
    
    await state.WithManagementAsync(async (mgmt, cachedInfo) =>
    {
        var info = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(FormFactor.UsbABiometricKeychain, info.FormFactor);
        
        // Metadata checks only; this does not exercise biometric operations or prove MPE eligibility.
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

### 5. FIPS Metadata Tests

The shared harness exposes `FipsCapable` and `FipsApproved` filters, but the current Core decoder uses
the application-bit mapping for the distinct FIPS-tag encoding. The derived masks and filters are not
reliable evidence of application approval. Comparing a filter result with the same decoded property
only checks consistency of that interpretation; it does not validate the mapping or approved operations.
Keep decoder/filter fixture work separate from hardware operation tests. `IsFips` comes from a distinct
manufacturing flag and must not be relabeled as an active FIPS mode.

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
        
        // RequireUsb and Capability are independent: PIV may be enabled over USB OR NFC.
        Assert.True(info.FirmwareVersion.Major >= 5);
        Assert.True(((info.UsbEnabled | info.NfcEnabled) & DeviceCapabilities.Piv) != 0);
    });
}
```

## Critical Warnings for Configuration Tests

### Plan configuration changes and restoration

The problem with capability changes is **blast radius across the suite**, not destruction as such. An allow-listed device is a dedicated test device and destructive operations against it are expected — see [docs/TESTING.md](../../docs/TESTING.md#hardware-authorization), which is canonical: *"the allow list is the boundary, and adding a second one would only obscure it."* Do **not** gate these behind an extra environment variable.

`SetDeviceConfigAsync` changes persistent state and can disable applications other tests depend on.
It reboots only when requested. The rule is restore what you changed, not prohibit configuration tests:

1. Save every field the test changes, including lock state when testing lock behavior. Do not copy all
   `DeviceInfo` fields blindly: read/write types differ, notably the challenge-response timeout bytes.
2. Preserve a usable recovery transport and document the suite-wide effects of disabling an application.
3. Attempt restoration in `finally`, including after a failed assertion, and read back restored persisted
   settings when possible. A `finally` guarantees an attempt, not successful hardware recovery.
4. Keep secret zeroing in an independent `finally` so a restoration failure cannot bypass it. A newly set
   lock code must remain available until it has been removed or replaced successfully.
5. If requesting reboot, end old session/connection scopes, rediscover the same authorized key, then open
   fresh scopes for verification and restoration. A fixed delay cannot reconnect an old session.

```csharp
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
public async Task AutoEject_PersistsAndRestorationIsAttempted(YubiKeyTestState state)
{
    await using var mgmt = await state.Device.CreateManagementSessionAsync(
        new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard });
    var original = await mgmt.GetDeviceInfoAsync();
    Assert.False(original.IsLocked); // Example prerequisite: unlocked dedicated test key.
    Assert.InRange(original.AutoEjectTimeout, (ushort)0, (ushort)3600); // Must be restorable through this SDK.
    ushort changed = original.AutoEjectTimeout == 30 ? (ushort)31 : (ushort)30;
    try
    {
        await mgmt.SetDeviceConfigAsync(new DeviceConfig { AutoEjectTimeout = changed });
        var persisted = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(changed, persisted.AutoEjectTimeout);
    }
    finally
    {
        await mgmt.SetDeviceConfigAsync(new DeviceConfig { AutoEjectTimeout = original.AutoEjectTimeout });
        var restored = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(original.AutoEjectTimeout, restored.AutoEjectTimeout);
    }
}
```

This example checks persistence without reboot, not actual CCID ejection or activation timing. It changes
only the field it saves. A restoration exception can replace the original assertion failure; production
test cleanup should preserve both failure reports. For reboot/discovery invariants, read
`ReconfigurationDiscoveryInvariantTests.cs`; its assertions concern completed scan snapshots, not proof
that every transient hardware state was observed. Do not generalize its timing to all keys or platforms.

For transport-specific read tests, use a concrete attribute row, pass
`preferredConnection: state.ConnectionType` to the helper and assert `mgmt.ConnectionType`. A default row
can contain several connection flags and must not be passed as a single transport preference. See
[transport-specific testing](../../docs/TESTING.md#transport-specific-testing) for the complete pattern.

## Performance Considerations

### Device Info Caching

`WithManagementAsync` passes the existing `state.DeviceInfo` snapshot to your action:

```csharp
await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
{
    // cachedDeviceInfo comes from shared device initialization, not helper entry.
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

### DeviceInfo - Core-owned snapshot

`DeviceInfo` is Core's readonly record struct. Optional serials remain nullable and absent optional
fields can have default values. It is not a mutable session cache. Compare the fields relevant to the
test rather than assuming two snapshots must match every field during reconfiguration:

```csharp
var info1 = await mgmt.GetDeviceInfoAsync();
var info2 = await mgmt.GetDeviceInfoAsync();
Assert.Equal(info1.SerialNumber, info2.SerialNumber); // Value comparison
```

### DeviceConfig - Partial configuration record

Null properties are omitted. Non-null masks and flags replace their whole field. This fragment assumes
an unlocked key supporting both requested USB applications; it disables the other USB apps. TouchEject
only takes effect in CCID-only mode, so PIV/OATH is a deliberate choice rather than a generic default:

```csharp
var config = new DeviceConfig
{
    UsbEnabled = DeviceCapabilities.Piv | DeviceCapabilities.Oath,
    AutoEjectTimeout = 30,
    DeviceFlags = DeviceFlags.TouchEject,
    ChallengeResponseTimeout = 15
};

// The session serializes the configuration and clears the encoded buffer after transmission.
await session.SetDeviceConfigAsync(
    config,
    new SetDeviceConfigOptions { Reboot = true },
    cancellationToken);
```

Timeout setters validate 0–3600 auto-eject seconds and 0–60 challenge-response seconds at assignment.
Those are SDK limits, not firmware maxima. Zero disables auto-eject or requests the challenge-response
default (normally 15 seconds), respectively. Auto-eject is an inactivity timer; assigning it does not
implicitly set TouchEject. `Validate()` only rejects exact USB `None`; it does not validate supported
device applications or all arbitrary enum widths. Check supported masks before writing.

`CurrentLockCode` authorizes each protected command. `NewLockCode` sets/replaces the code, or removes
it when all sixteen bytes are zero; an empty memory value is invalid. The session borrows both buffers
until completion and clears its encoded configuration in `finally`; callers still clear source secrets.
See the README's [lock procedure](README.md#lock-the-configuration) for complete cleanup examples.

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
VersionQualifier qualifier = info.VersionQualifier;  // e.g., 5.7.2.beta.1
string versionName = info.VersionName;               // Display string

// VersionQualifierType enum values:
// - Final: Production release (5.7.2)
// - Alpha: Alpha prerelease (5.7.2.alpha.1)
// - Beta: Beta prerelease (5.7.2.beta.1)
```

Use `FirmwareVersion` for comparisons and `VersionName` for display. `VersionName` uses bare firmware
for Final, whereas `VersionQualifier.ToString()` itself includes the qualifier type and iteration.

### Feature Gating by Firmware

Operations call inherited `EnsureSupports(FeatureSetConfig)` (5.0.0) or
`EnsureSupports(FeatureDeviceReset)` (5.6.0). Do not replace these with a local `_version` comparison:
the inherited check uses effective `FirmwareVersion`, including overrides and major-zero development
version policy. Alpha/Beta display qualifiers are not that major-zero bypass.

`FeatureDeviceInfo` declares the supported 4.1.0 read minimum, but current read/factory paths do not
enforce it. Do not promise `NotSupportedException` before an unsupported read. Likewise the generic
write check does not enforce NFC restriction's 5.7/NFC-hardware conditions or device-supported masks.
Reset's firmware check is not a family check: the documented operation resets Bio MPE's FIDO/PIV
applications and shared PIN, retaining the F9 attestation slot. It does not establish Management lock
recovery. Do not add a family policy or reset-success lifecycle policy as a documentation-only change.

## Session Initialization Pattern

The following is a source excerpt inside `ManagementSession`, not a standalone consumer factory:

```csharp
public static Task<ManagementSession> CreateAsync(
    IConnection connection,
    SessionCreationOptions? options = null,
    CancellationToken cancellationToken = default) =>
    ConstructInitializedAsync(
        connection,
        options,
        () => new ManagementSession(connection),
        (session, token) => session.InitializeAsync(
            options?.ProtocolConfiguration, options?.FirmwareVersionOverride, options?.ScpKeyParameters, token),
        cancellationToken);
```

The applet callback calls `PrepareProtocolAsync`, then explicitly dispatches discovery: SmartCard uses
`SmartCardBackend.SelectApplicationAsync`, OTP HID uses `OtpBackend.ReadStatusVersionAsync`, and FIDO HID
reads firmware from the prepared protocol. These static probes do not require an operational backend.
Firmware resolution prefers DeviceInfo and falls back to the discovered version. After `ConfigureProtocolAsync`,
the session constructs its backend with the effective protocol. The shared factory marks the session ready only
after this callback completes. SCP parameters are callback inputs, not session fields.

The metadata probe occurs before optional SCP establishment; an SCP-authenticated return does not imply
that every initialization exchange used SCP. A firmware override does not skip probes. It configures
the protocol and feature checks, while `_version` retains discovered metadata for subsequent reads.
Non-cancellation metadata-probe failures fall back to the transport version; cancellation propagates.

Management does not forward `SessionCreationOptions.UserPresencePrompt` to its base constructor. The
service is unused here: this module does not retain, invoke or dispose it. Do not copy the general
retained-prompt contract into Management-specific ownership explanations.

## Architecture - Backend Pattern

ManagementSession uses backends for configuration writes and the internal legacy mode operation across
SmartCard, FIDO HID and OTP HID. Shared device-info reads deliberately bypass the backend; reset deliberately
branches to SmartCard. Keep those responsibilities visible rather than adding unsupported methods to every backend.

### Internal Structure

These declarations belong to different internal scopes (session field and backend interface); they
illustrate the source structure rather than a consumer program:

```csharp
// ManagementSession delegates transport-specific work to a backend
private IManagementBackend _backend = null!;

// Backend abstraction: deliberately NOT IDisposable
internal interface IManagementBackend
{
    ValueTask WriteConfigAsync(ReadOnlyMemory<byte> config, CancellationToken cancellationToken);
    ValueTask SetModeAsync(byte[] data, CancellationToken cancellationToken);
}
```

Device-info reads do not go through the backend; `ManagementSession` uses `DeviceInfoReader.ReadAsync` with the base-owned `Protocol` directly.

`DeviceResetAsync` belongs only to `SmartCardBackend`. The session preserves disposal and firmware checks before
dispatching reset or rejecting HID; HID backends do not expose unsupported reset methods.

### Implementations

- **SmartCardBackend**: SELECT probe, configuration write (INS 0x1C), internal mode (0x16), reset (0x1F).
- **FidoHidBackend**: Configuration/mode vendor commands (0xC3/0xC0); Core reads info with 0xC2.
- **OtpBackend**: Delegates slot commands to Core's OTP HID protocol. Core handles report framing and
  outbound frame CRC; `DeviceInfoReader` checks inbound device-info CRC, not this backend.

### Key Design Decisions

1. **Backend owns nothing**: it borrows the protocol and holds no disposable state, so it is not `IDisposable`. Ownership rule is Core's — see [Core CLAUDE.md](../Core/CLAUDE.md) gotcha #2 and the `IProtocol` doc comment: the session disposes the protocol, and whoever created the connection disposes it.
2. **SCP wrapping works**: the backend can be recreated over an SCP-wrapped protocol without disposing anything.
3. **Narrow delegation**: writes share one backend contract; info reads use Core and reset is SmartCard-only.
   There is no current public legacy `SetMode` surface, even though the backend retains that primitive.

`ManagementBackendLifecycleTests` pins the non-disposable invariant. It does not prove zero branching,
hardware feature support, or correctness of Core's multi-page reader.

### Why the backend stays small

The session performs argument/firmware checks, builds the length-prefixed configuration and clears the
encoded buffer in `finally`. The backend performs one transport write without owning the payload or
protocol. This keeps the sensitive-buffer lifetime visible and avoids duplicating configuration policy
across transports. Keep a test fake at that narrow seam when checking session behavior.

## TLV Encoding/Decoding

Device information uses TLV (Tag-Length-Value) encoding. The current reader is
[`Core/src/Devices/DeviceInfoReader.cs`](../Core/src/Devices/DeviceInfoReader.cs), shared with discovery:

1. Read page zero using the supplied protocol, then verify the one-byte declared response length against
   the actual TLV body. OTP response CRC is checked before stripping its CRC trailer.
2. Decode each page and reject duplicate more-data tags (0x10) within that page. A nonempty tag's final
   byte supplies the remaining-page count, not a boolean `hasMore` flag.
3. Accumulate non-pagination TLVs, request subsequent pages, and reject more data beyond the byte-sized
   page index (256 pages). Each page is its own guarded exchange; callers must sequence public operations.
4. Parse the accumulated metadata through `DeviceInfo.CreateFromTlvs`, with discovered firmware as the
   session's fallback. Do not route reads through a nonexistent backend `ReadConfigAsync` method.

Writes use one total-length byte followed by the modeled TLVs. Null fields are omitted, including a null
NFC restriction. True/false restriction values encode 0x01/0x00 respectively, but the false request is not
a verified NFC recovery procedure. On NFC-capable 5.7+ keys, documented recovery is USB power for three
seconds; the NDEF URL remains readable while restricted. Do not invent a touch or reboot prerequisite.

`DeviceConfig.GetBytes` emits a fixed local order (reboot/current authorization first when present), not
a protocol-mandated sort order. The configuration reference contains a conflicting NFC write-tag row;
current encoding uses 0x0E, not the reboot tag 0x0C. The exact-byte test's pinned sibling citation supports
its fixture order, not a general conformance claim. TLV copies and the intermediate encoding are cleared
locally; the returned encoded buffer escapes to the session, which clears it after the backend completes.

## Debugging Tips

### Enable Verbose Logging

```csharp
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Trace);
    builder.AddConsole();
});

YubiKitLogging.LoggerFactory = loggerFactory;

await using var mgmt = await ManagementSession.CreateAsync(
    connection);
```

### Device Enumeration

```csharp
// Find all connected YubiKeys
var devices = await YubiKeyManager.FindAllAsync(cancellationToken: cancellationToken);
foreach (var device in devices)
{
    var info = await device.GetDeviceInfoAsync(cancellationToken: cancellationToken);
    Console.WriteLine($"Serial: {info.SerialNumber}");
    Console.WriteLine($"Firmware: {info.FirmwareVersion}");
    Console.WriteLine($"USB enabled: {info.UsbEnabled}");
}

// Find specific device
var yubiKey = devices.SingleOrDefault(device => device.SerialNumber == 12345678);
```

## Known Gotchas

1. **Configuration lock**: Current-code authorization is per command. Losing the code has no documented
   reset-based recovery; retain it securely until a replacement/removal write succeeds.
2. **Reboot**: A reboot request is not factory reset. End old scopes and rediscover/reopen afterwards;
   neither three seconds nor a discovery result's position proves the intended key is ready.
3. **USB reachability**: Exact USB `None` is rejected; device-supported-mask and arbitrary-width validation
   are not comprehensive. Preserve a supported application and a recovery route.
4. **Metadata interpretation**: Flagged form factors can miss equality filters; current FIPS masks use the
   wrong mapping. Do not derive approval, active mode or product-family eligibility from these filters.
5. **NFC**: Check hardware and 5.7+ support for restriction; false encoding alone is not verified recovery.
6. **Test proof**: Readback checks persisted metadata, not necessarily active interfaces. A broad
   `ThrowsAnyAsync<Exception>` does not isolate authentication or lock enforcement. Existing restoration
   and failure-path secret cleanup tests have gaps; do not describe an attempted write as guaranteed recovery.
7. **Connection lifetime**: Registry exclusion is process-local and limited to proven groups. Dispose
   every owned session/connection; do not rely on best-effort native finalization.

## Related Modules

- **Core.Abstractions / Core.Devices**: IYubiKey, discovery and read-only metadata
- **Core.Sessions**: ApplicationSession, options, effective firmware and lifetime
- **Core.Protocols.SmartCard.Apdu / .Scp**: APDU handling and secure channels
- **Tests.Shared**: YubiKeyTestState, test infrastructure with `[WithYubiKey]` attribute
