# Multi-Transport Test Infrastructure Implementation Plan

**Goal:** Refactor test infrastructure so `YubiKeyTestState` carries its connection type, `WithManagementAsync` uses it automatically, and tests can filter by transport via `[WithYubiKey]`.

**Architecture:** Each physical YubiKey produces multiple `YubiKeyTestState` instances (one per available transport: Ccid, HidFido, HidOtp). The state carries `ConnectionType`, and helper methods like `WithManagementAsync` use `device.ConnectAsync()` which automatically selects the correct connection type. Tests become transport-aware without duplication.

**Tech Stack:** xUnit v3, C# 14, extension members, `ConnectionType` enum from `Yubico.YubiKit.Core`

**Key Simplification:** `IYubiKey.ConnectAsync()` (default interface implementation) already handles connection type selection based on `device.ConnectionType`. No switch statements needed in test helpers.

---

## Prerequisites

Before starting, read these files:
- `Yubico.YubiKit.Tests.Shared/YubiKeyTestState.cs` - Current state wrapper
- `Yubico.YubiKit.Tests.Shared/YubiKeyTestStateExtensions.cs` - Current extension methods
- `Yubico.YubiKit.Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs` - Current data attribute
- `Yubico.YubiKit.Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs` - Device discovery
- `Yubico.YubiKit.Core/src/Interfaces/IYubiKey.cs` - IYubiKey with ConnectAsync default implementation
- `Yubico.YubiKit.Core/src/Interfaces/IConnection.cs` - ConnectionType enum
- `docs/TESTING.md` - Build and test commands

---

## Task 1: Add ConnectionType to YubiKeyTestState

**Files:**
- Modify: `Yubico.YubiKit.Tests.Shared/YubiKeyTestState.cs`

### Step 1: Add ConnectionType property

Add the property and update constructor:

```csharp
// In YubiKeyTestState.cs, add using at top:
using Yubico.YubiKit.Core.Interfaces;

// Add property after line 98 (after IsNfcTransport):
/// <summary>
///     Gets the connection type this test state represents.
///     Each physical YubiKey may produce multiple test states (one per transport).
/// </summary>
public ConnectionType ConnectionType { get; private set; } = ConnectionType.Ccid;
```

### Step 2: Update constructor to accept ConnectionType

```csharp
// Replace constructor at line 56 with:
/// <summary>
///     Initializes a new instance with the specified device, device information, and connection type.
/// </summary>
/// <param name="device">The YubiKey device.</param>
/// <param name="deviceInfo">The device information.</param>
/// <param name="connectionType">The connection type this state represents.</param>
public YubiKeyTestState(IYubiKey device, DeviceInfo deviceInfo, ConnectionType connectionType = ConnectionType.Ccid)
{
    Device = device ?? throw new ArgumentNullException(nameof(device));
    DeviceInfo = deviceInfo;
    ConnectionType = connectionType;
}
```

### Step 3: Update serialization to include ConnectionType

```csharp
// Update Serialize method at line 134:
public void Serialize(IXunitSerializationInfo info)
{
    info.AddValue(nameof(SerialNumber), DeviceInfo.SerialNumber);
    info.AddValue(nameof(ConnectionType), (int)ConnectionType);
}

// Update Deserialize method at line 110:
public void Deserialize(IXunitSerializationInfo info)
{
    var serialNumber = info.GetValue<int>(nameof(SerialNumber));
    var connectionTypeInt = info.GetValue<int>(nameof(ConnectionType));
    var connectionType = (ConnectionType)connectionTypeInt;

    // Look up device from static cache by serial number AND connection type
    var cacheKey = GetCacheKey(serialNumber, connectionType);
    var deviceFromCache = YubiKeyDeviceCache.GetDevice(cacheKey);
    if (deviceFromCache is null)
        throw new InvalidOperationException(
            $"Device with serial number {serialNumber} and connection type {connectionType} not found in cache.");

    Device = deviceFromCache.Device;
    DeviceInfo = deviceFromCache.DeviceInfo;
    ConnectionType = deviceFromCache.ConnectionType;
}

// Add helper method:
private static string GetCacheKey(int? serialNumber, ConnectionType connectionType) =>
    $"{serialNumber}:{connectionType}";
```

### Step 4: Update ToString to include ConnectionType

```csharp
// Replace ToString at line 144:
public override string ToString() =>
    $"YubiKey(SN:{DeviceInfo.SerialNumber},FW:{DeviceInfo.FirmwareVersion},{DeviceInfo.FormFactor},{ConnectionType})";
```

### Step 5: Update YubiKeyDeviceCache to use composite key

```csharp
// Replace the cache implementation starting at line 177:
/// <summary>
///     Static cache for YubiKey devices shared across test data attributes.
/// </summary>
internal static class YubiKeyDeviceCache
{
    // Key is now "serialNumber:connectionType"
    private static readonly Dictionary<string, YubiKeyTestState> s_devices = new();
    private static readonly object s_lock = new();

    private static string GetCacheKey(YubiKeyTestState state) =>
        $"{state.SerialNumber}:{state.ConnectionType}";

    /// <summary>
    ///     Adds a device to the cache.
    /// </summary>
    public static void AddDevice(YubiKeyTestState state)
    {
        lock (s_lock)
        {
            s_devices[GetCacheKey(state)] = state;
        }
    }

    /// <summary>
    ///     Gets a device from the cache by composite key.
    /// </summary>
    public static YubiKeyTestState? GetDevice(string cacheKey)
    {
        lock (s_lock)
        {
            return s_devices.GetValueOrDefault(cacheKey);
        }
    }

    /// <summary>
    ///     Clears all cached devices.
    /// </summary>
    public static void Clear()
    {
        lock (s_lock)
        {
            s_devices.Clear();
        }
    }

    /// <summary>
    ///     Gets all cached devices.
    /// </summary>
    public static IReadOnlyList<YubiKeyTestState> GetAllDevices()
    {
        lock (s_lock)
        {
            return [.. s_devices.Values];
        }
    }
}
```

### Step 6: Build and verify compilation

Follow instructions in `docs/TESTING.md` to build and run tests.
Expected: Build succeeds

### Step 7: Commit

```bash
git add Yubico.YubiKit.Tests.Shared/YubiKeyTestState.cs
git commit -m "feat(tests): add ConnectionType to YubiKeyTestState"
```

---

## Task 2: Update Device Discovery to Create Per-Transport States

**Files:**
- Modify: `Yubico.YubiKit.Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs`

### Step 1: Update InitializeDevicesAsync to create multiple states per device

Replace the `InitializeDevicesAsync` method to create one `YubiKeyTestState` per transport. **Note:** We use the device's `ConnectionType` property directly — no capability checking.

```csharp
// Replace InitializeDevicesAsync (starting around line 234):
private static List<YubiKeyTestState> InitializeDevicesAsync()
{
    Console.WriteLine("[YubiKey Infrastructure] Initializing devices (once per test run)...");

    try
    {
        // Discover all devices - this returns one entry per transport per physical device
        var allDevices = YubiKey.FindAllAsync().GetAwaiter().GetResult();
        Console.WriteLine($"[YubiKey Infrastructure] Found {allDevices.Count} device(s)");

        if (allDevices.Count == 0)
        {
            Console.WriteLine("[YubiKey Infrastructure] No YubiKey devices found");
            return [];
        }

        // Filter by allow list and create test states
        var authorizedDevices = new List<YubiKeyTestState>();
        var filteredCount = 0;

        foreach (var device in allDevices)
        {
            try
            {
                DeviceInfo? deviceInfo = device.GetDeviceInfoAsync().GetAwaiter().GetResult();
                if (deviceInfo is not { SerialNumber: not null })
                {
                    filteredCount++;
                    Console.WriteLine("[YubiKey Infrastructure] Device with unknown serial FILTERED");
                    continue;
                }

                if (!AllowList.IsDeviceAllowed(deviceInfo.Value.SerialNumber))
                {
                    filteredCount++;
                    Console.WriteLine(
                        $"[YubiKey Infrastructure] Device SN:{deviceInfo.Value.SerialNumber} FILTERED (not in allow list)");
                    continue;
                }

                // Create test state using device's ConnectionType directly
                var testDevice = new YubiKeyTestState(device, deviceInfo.Value, device.ConnectionType);
                authorizedDevices.Add(testDevice);
                YubiKeyDeviceCache.AddDevice(testDevice);

                Console.WriteLine(
                    $"[YubiKey Infrastructure] Device SN:{deviceInfo.Value.SerialNumber} authorized " +
                    $"(FW:{deviceInfo.Value.FirmwareVersion}, {deviceInfo.Value.FormFactor}, {device.ConnectionType})");
            }
            catch (Exception ex) when (ex is SCardException scardException)
            {
                filteredCount++;
                Console.WriteLine(
                    $"[YubiKey Infrastructure] DeviceId:{device.DeviceId} FILTERED " +
                    $"(allow list check failed: {scardException.Message})");
            }
        }

        // Hard fail if no authorized devices
        if (authorizedDevices.Count == 0)
        {
            var errorMessage =
                "═══════════════════════════════════════════════════════════════════════════\n" +
                "                        NO AUTHORIZED DEVICES FOUND\n" +
                "═══════════════════════════════════════════════════════════════════════════\n" +
                // ... rest of error message unchanged
                "═══════════════════════════════════════════════════════════════════════════";

            Console.Error.WriteLine(errorMessage);
            Environment.Exit(-1);
        }

        Console.WriteLine(
            $"[YubiKey Infrastructure] Initialization complete: {authorizedDevices.Count} test states " +
            $"({authorizedDevices.Select(d => d.SerialNumber).Distinct().Count()} physical devices), " +
            $"{filteredCount} filtered");

        return authorizedDevices;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[YubiKey Infrastructure] FATAL: Device initialization failed: {ex.Message}");
        Console.Error.WriteLine(ex.StackTrace);
        Environment.Exit(-1);
        throw;
    }
}
```

**Note:** `YubiKey.FindAllAsync()` already returns multiple entries per physical device (one per transport). We just use `device.ConnectionType` directly — no need to enumerate transports ourselves.
```

### Step 2: Add using directive

```csharp
// Add at top of file:
using Yubico.YubiKit.Core.Interfaces;
```

### Step 3: Build and verify

Follow instructions in `docs/TESTING.md` to build.
Expected: Build succeeds

### Step 4: Commit

```bash
git add Yubico.YubiKit.Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs
git commit -m "feat(tests): create per-transport YubiKeyTestState during discovery"
```

---

## Task 3: Add ConnectionType Filter to WithYubiKeyAttribute

**Files:**
- Modify: `Yubico.YubiKit.Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs`

### Step 1: Add ConnectionType property

```csharp
// Add using at top:
using Yubico.YubiKit.Core.Interfaces;

// Add property after FipsApproved (around line 103):
/// <summary>
///     Gets or sets the required connection type.
///     Use ConnectionType.Unknown (default) to match any connection type.
/// </summary>
/// <remarks>
///     When set, tests will only run on device states with the specified connection type.
///     For example, ConnectionType.Ccid will only run on SmartCard connections.
/// </remarks>
public ConnectionType ConnectionType { get; set; } = ConnectionType.Unknown;
```

### Step 2: Update GetData to pass ConnectionType to filter

```csharp
// Update the FilterDevices call in GetData (around line 158):
var filteredDevices = YubiKeyTestInfrastructure.FilterDevices(
    allDevices,
    MinFirmware,
    FormFactor,
    RequireUsb,
    RequireNfc,
    Capability,
    FipsCapable,
    FipsApproved,
    ConnectionType,  // Add this parameter
    CustomFilter).ToList();

// Update the GetFilterCriteriaDescription call (around line 171):
var criteria = YubiKeyTestInfrastructure.GetFilterCriteriaDescription(
    MinFirmware,
    FormFactor,
    RequireUsb,
    RequireNfc,
    Capability,
    FipsCapable,
    FipsApproved,
    ConnectionType,  // Add this parameter
    CustomFilter);
```

### Step 3: Build (will fail - need to update YubiKeyTestInfrastructure)

Follow instructions in `docs/TESTING.md` to build.
Expected: Build fails (method signature mismatch) - this is expected

### Step 4: Commit partial progress

```bash
git add Yubico.YubiKit.Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs
git commit -m "feat(tests): add ConnectionType property to WithYubiKeyAttribute"
```

---

## Task 4: Update FilterDevices to Support ConnectionType

**Files:**
- Modify: `Yubico.YubiKit.Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs`

### Step 1: Update FilterDevices signature and implementation

```csharp
// Update FilterDevices method signature and add connection type filter (around line 69):
public static IEnumerable<YubiKeyTestState> FilterDevices(
    IEnumerable<YubiKeyTestState> devices,
    string? minFirmware,
    FormFactor formFactor,
    bool requireUsb,
    bool requireNfc,
    DeviceCapabilities capability,
    DeviceCapabilities fipsCapable,
    DeviceCapabilities fipsApproved,
    ConnectionType connectionType = ConnectionType.Unknown,  // Add parameter
    Type? customFilterType = null)
{
    var filtered = devices;

    // Filter by minimum firmware
    if (!string.IsNullOrEmpty(minFirmware))
    {
        var minFw = FirmwareVersion.FromString(minFirmware);
        if (minFw is not null)
            filtered = filtered.Where(d => d.FirmwareVersion >= minFw);
    }

    // Filter by form factor
    if (formFactor != FormFactor.Unknown)
        filtered = filtered.Where(d => d.FormFactor == formFactor);

    // Filter by USB transport
    if (requireUsb)
        filtered = filtered.Where(d => d.IsUsbTransport);

    // Filter by NFC transport
    if (requireNfc)
        filtered = filtered.Where(d => d.IsNfcTransport);

    // Filter by capability
    if (capability != DeviceCapabilities.None)
        filtered = filtered.Where(d => d.HasCapability(capability));

    // Filter by FIPS-capable
    if (fipsCapable != DeviceCapabilities.None)
        filtered = filtered.Where(d => d.IsFipsCapable(fipsCapable));

    // Filter by FIPS-approved
    if (fipsApproved != DeviceCapabilities.None)
        filtered = filtered.Where(d => d.IsFipsApproved(fipsApproved));

    // Filter by connection type
    if (connectionType != ConnectionType.Unknown)
        filtered = filtered.Where(d => d.ConnectionType == connectionType);

    // Apply custom filter if provided
    if (customFilterType is not null)
    {
        var filter = InstantiateCustomFilter(customFilterType);
        if (filter is not null)
            filtered = filtered.Where(d => filter.Matches(d));
    }

    return filtered;
}
```

### Step 2: Update GetFilterCriteriaDescription

```csharp
// Update GetFilterCriteriaDescription signature and implementation (around line 128):
public static string GetFilterCriteriaDescription(
    string? minFirmware,
    FormFactor formFactor,
    bool requireUsb,
    bool requireNfc,
    DeviceCapabilities capability,
    DeviceCapabilities fipsCapable,
    DeviceCapabilities fipsApproved,
    ConnectionType connectionType = ConnectionType.Unknown,  // Add parameter
    Type? customFilterType = null)
{
    var criteria = new List<string>();

    if (minFirmware is not null)
        criteria.Add($"MinFirmware >= {minFirmware}");

    if (formFactor != FormFactor.Unknown)
        criteria.Add($"FormFactor = {formFactor}");

    if (requireUsb)
        criteria.Add("Transport = USB");

    if (requireNfc)
        criteria.Add("Transport = NFC");

    if (capability != DeviceCapabilities.None)
        criteria.Add($"Capability = {capability}");

    if (fipsCapable != DeviceCapabilities.None)
        criteria.Add($"FipsCapable = {fipsCapable}");

    if (fipsApproved != DeviceCapabilities.None)
        criteria.Add($"FipsApproved = {fipsApproved}");

    // Add connection type to criteria description
    if (connectionType != ConnectionType.Unknown)
        criteria.Add($"ConnectionType = {connectionType}");

    if (customFilterType is not null)
    {
        var filter = InstantiateCustomFilter(customFilterType);
        if (filter is not null)
            criteria.Add($"CustomFilter = {filter.GetDescription()}");
        else
            criteria.Add($"CustomFilter = {customFilterType.Name} (failed to instantiate)");
    }

    return criteria.Count > 0 ? string.Join(", ", criteria) : "None (all devices)";
}
```

### Step 3: Build and verify

Follow instructions in `docs/TESTING.md` to build.
Expected: Build succeeds

### Step 4: Commit

```bash
git add Yubico.YubiKit.Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs
git commit -m "feat(tests): add ConnectionType filtering to device discovery"
```

---

## Task 5: Update YubiKeyTestStateExtensions to Use device.ConnectAsync()

**Files:**
- Modify: `Yubico.YubiKit.Tests.Shared/YubiKeyTestStateExtensions.cs`

**Key insight:** `IYubiKey.ConnectAsync()` (the parameterless overload) already uses `device.ConnectionType` to select the right connection type. No switch statement needed.

### Step 1: Simplify WithManagementAsync

```csharp
// Replace the WithManagementAsync implementation (around line 56):
public async Task WithManagementAsync(
    Func<ManagementSession, DeviceInfo, Task> action,
    ProtocolConfiguration? configuration = null,
    ScpKeyParameters? scpKeyParams = null,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(state);
    ArgumentNullException.ThrowIfNull(action);

    // Uses device.ConnectionType automatically via default interface implementation
    await using var connection = await state.Device
        .ConnectAsync(cancellationToken)
        .ConfigureAwait(false);

    using var session = await ManagementSession
        .CreateAsync(connection, configuration, scpKeyParams, cancellationToken)
        .ConfigureAwait(false);
        
    await action(session, state.DeviceInfo).ConfigureAwait(false);
}
```

### Step 2: Simplify WithConnectionAsync

```csharp
// Replace WithConnectionAsync implementation (around line 134):

/// <summary>
///     Executes an action with a connection of the type specified by the device's ConnectionType.
///     Automatically handles connection lifecycle.
/// </summary>
public async Task WithConnectionAsync(
    Func<IConnection, Task> action,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(state);
    ArgumentNullException.ThrowIfNull(action);

    // Uses device.ConnectionType automatically
    await using var connection = await state.Device
        .ConnectAsync(cancellationToken)
        .ConfigureAwait(false);
        
    await action(connection).ConfigureAwait(false);
}

/// <summary>
///     Executes an action with a SmartCard connection specifically.
///     Use this when you need SmartCard-specific functionality regardless of device's ConnectionType.
/// </summary>
public async Task WithSmartCardConnectionAsync(
    Func<ISmartCardConnection, Task> action,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(state);
    ArgumentNullException.ThrowIfNull(action);

    await using var connection = await state.Device
        .ConnectAsync<ISmartCardConnection>(cancellationToken)
        .ConfigureAwait(false);
    await action(connection).ConfigureAwait(false);
}
```

### Step 3: Build and verify

Follow instructions in `docs/TESTING.md` to build.
Expected: Build succeeds

### Step 4: Commit

```bash
git add Yubico.YubiKit.Tests.Shared/YubiKeyTestStateExtensions.cs
git commit -m "feat(tests): simplify WithManagementAsync to use device.ConnectAsync()"
```

---

## Task 6: Delete Unused ManagementTestState and TestState

**Files:**
- Delete: `Yubico.YubiKit.Tests.Shared/ManagementTestState.cs`
- Delete: `Yubico.YubiKit.Tests.Shared/Infrastructure/TestState.cs`

These classes are not used by any tests — only referenced in documentation.

### Step 1: Delete the files

```bash
rm Yubico.YubiKit.Tests.Shared/ManagementTestState.cs
rm Yubico.YubiKit.Tests.Shared/Infrastructure/TestState.cs
```

### Step 2: Build and verify no references

Follow instructions in `docs/TESTING.md` to build.
Expected: Build succeeds (no code depends on these)

### Step 3: Commit

```bash
git add -u
git commit -m "chore(tests): remove unused ManagementTestState and TestState classes"
```

---

## Task 7: Update Existing Integration Tests

**Files:**
- Modify: `Yubico.YubiKit.Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionAdvancedTests.cs`

### Step 1: Update SerialNumber_MultipleReads test

```csharp
// Replace the test at line 316:
[SkippableTheory]
[WithYubiKey]
public async Task SerialNumber_MultipleReads_RemainsConsistent(YubiKeyTestState state)
{
    // Now uses state.ConnectionType automatically via WithManagementAsync
    await state.WithManagementAsync(async (mgmt, deviceInfo) =>
    {
        var serial1 = (await mgmt.GetDeviceInfoAsync()).SerialNumber;
        
        // Second read within same session
        var serial2 = (await mgmt.GetDeviceInfoAsync()).SerialNumber;
        
        Assert.Equal(serial1, serial2);
        Assert.Equal(state.SerialNumber, serial1);
    });
}
```

### Step 2: Add transport-specific test examples

```csharp
/// <summary>
///     SmartCard-only test: Device reset requires CCID transport.
/// </summary>
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.Ccid)]
public async Task ResetDevice_SmartCardOnly_Succeeds(YubiKeyTestState state)
{
    Assert.Equal(ConnectionType.Ccid, state.ConnectionType);
    
    // Skip actual reset to avoid destructive behavior in example
    Skip.If(true, "Skipping destructive reset - example test for ConnectionType filtering");
}

/// <summary>
///     All transports test: Verify device info works on all connection types.
/// </summary>
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.Ccid)]
[WithYubiKey(ConnectionType = ConnectionType.HidFido)]
[WithYubiKey(ConnectionType = ConnectionType.HidOtp)]
public async Task GetDeviceInfo_AllTransports_ReturnsValidData(YubiKeyTestState state)
{
    await state.WithManagementAsync(async (mgmt, deviceInfo) =>
    {
        var info = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(state.SerialNumber, info.SerialNumber);
    });
}
```

### Step 3: Build and run tests

Follow instructions in `docs/TESTING.md` to build and run tests.
Expected: Build succeeds, tests pass (or skip appropriately)

### Step 4: Commit

```bash
git add Yubico.YubiKit.Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionAdvancedTests.cs
git commit -m "test(management): update tests to use ConnectionType-aware infrastructure"
```

---

## Task 8: Clean Up ManagementSessionSimpleTests

**Files:**
- Modify: `Yubico.YubiKit.Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionSimpleTests.cs`

### Step 1: Remove string-based DeviceId parsing and use proper infrastructure

Replace the fragile tests with clean multi-transport test:

```csharp
// Replace CreateManagementSession_with_Hid_CreateAsync and related tests with:
[Theory]
[WithYubiKey(ConnectionType = ConnectionType.Ccid)]
[WithYubiKey(ConnectionType = ConnectionType.HidFido)]
[WithYubiKey(ConnectionType = ConnectionType.HidOtp)]
public async Task CreateManagementSession_AllTransports_ReturnsValidDeviceInfo(YubiKeyTestState state)
{
    await state.WithManagementAsync(async (mgmt, deviceInfo) =>
    {
        var info = await mgmt.GetDeviceInfoAsync();
        Assert.NotEqual(0, info.SerialNumber);
    });
}
```

### Step 2: Build and verify

Follow instructions in `docs/TESTING.md` to build.
Expected: Build succeeds

### Step 3: Commit

```bash
git add Yubico.YubiKit.Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionSimpleTests.cs
git commit -m "refactor(tests): remove DeviceId string parsing, use ConnectionType filtering"
```

---

## Task 9: Update Documentation

**Files:**
- Modify: `Yubico.YubiKit.Tests.Shared/README.md` (if exists)
- Modify: `docs/TESTING.md`

### Step 1: Document the new ConnectionType behavior

Add section explaining:
- Each physical YubiKey produces multiple test states (one per transport discovered)
- How to filter by ConnectionType in `[WithYubiKey]`
- How `WithManagementAsync` automatically uses the correct transport via `device.ConnectAsync()`

### Step 2: Add examples

```markdown
## Multi-Transport Testing

Each discovered YubiKey interface produces a `YubiKeyTestState` with its `ConnectionType`:
- `ConnectionType.Ccid` - SmartCard/CCID
- `ConnectionType.HidFido` - FIDO HID
- `ConnectionType.HidOtp` - OTP HID

### Running on All Transports

```csharp
[Theory]
[WithYubiKey]  // Runs once per discovered transport
public async Task MyTest(YubiKeyTestState state)
{
    // state.ConnectionType tells you which transport
    await state.WithManagementAsync(async (mgmt, info) =>
    {
        // Uses the correct transport automatically
    });
}
```

### Filtering to Specific Transport

```csharp
[Theory]
[WithYubiKey(ConnectionType = ConnectionType.Ccid)]  // SmartCard only
public async Task SmartCardOnlyTest(YubiKeyTestState state) { }

[Theory]
[WithYubiKey(ConnectionType = ConnectionType.Ccid)]
[WithYubiKey(ConnectionType = ConnectionType.HidFido)]
[WithYubiKey(ConnectionType = ConnectionType.HidOtp)]  // All transports explicitly
public async Task AllTransportsTest(YubiKeyTestState state) { }
```
```

### Step 3: Commit

```bash
git add docs/TESTING.md Yubico.YubiKit.Tests.Shared/README.md
git commit -m "docs: document multi-transport test infrastructure"
```

---

## Task 10: Final Verification

### Step 1: Build entire solution

Follow instructions in `docs/TESTING.md` to build.
Expected: Build succeeds with no errors

### Step 2: Run all Management integration tests

Follow instructions in `docs/TESTING.md` to run tests.
Expected: Tests pass (with appropriate skips for unavailable transports)

### Step 3: Verify test output shows transport

Look for output like:
```
YubiKey(SN:12345678,FW:5.7.2,UsbAKeychain,Ccid)
YubiKey(SN:12345678,FW:5.7.2,UsbAKeychain,HidFido)
YubiKey(SN:12345678,FW:5.7.2,UsbAKeychain,HidOtp)
```

### Step 4: Final commit

```bash
git add -A
git commit -m "feat(tests): complete multi-transport test infrastructure"
```

---

## Summary

| Task | Description | Files Modified |
|------|-------------|----------------|
| 1 | Add ConnectionType to YubiKeyTestState | `YubiKeyTestState.cs` |
| 2 | Use device.ConnectionType during discovery | `YubiKeyTestInfrastructure.cs` |
| 3 | Add ConnectionType filter to attribute | `WithYubiKeyAttribute.cs` |
| 4 | Update FilterDevices for ConnectionType | `YubiKeyTestInfrastructure.cs` |
| 5 | Simplify extensions with device.ConnectAsync() | `YubiKeyTestStateExtensions.cs` |
| 6 | **Delete** unused ManagementTestState/TestState | Delete 2 files |
| 7 | Update existing integration tests | `ManagementSessionAdvancedTests.cs` |
| 8 | Clean up SimpleTests | `ManagementSessionSimpleTests.cs` |
| 9 | Update documentation | `TESTING.md`, `README.md` |
| 10 | Final verification | All files |

---

## Key Design Decisions

1. **`device.ConnectionType` is the source of truth** — no capability checking during discovery
2. **`IYubiKey.ConnectAsync()` handles connection selection** — default interface implementation, no switch statements needed
3. **Cache key is `serialNumber:connectionType`** — supports multiple states per physical device
4. **Deleted unused classes** — ManagementTestState and TestState were never used
5. **Tests combine transports with multiple `[WithYubiKey]` attributes** — cleaner than separate test methods
