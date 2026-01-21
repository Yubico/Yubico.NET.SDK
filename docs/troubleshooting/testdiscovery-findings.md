# Test Discovery Troubleshooting Findings

> **INSTRUCTION TO AGENTS**: This is a living document. Each troubleshooting session MUST update this file with:
> - New findings (what you discovered)
> - What didn't work (failed attempts)
> - What moved us forward (successful changes)
> - Current state after your changes
> 
> Do NOT delete previous entries. Append new sessions as dated sections.

---

## Session 1: 2026-01-19

### Problem Statement
VS Code test explorer doesn't show some integration test projects (Management, SecurityDomain), while others appear (Piv, Core, Fido2).

### Root Cause Analysis

1. **`[WithYubiKey]` attribute causes test discovery issues**
   - Projects using `[WithYubiKey]` (Piv, Management, SecurityDomain) had discovery problems
   - Projects using plain `[Fact]` (Core, Fido2) worked fine

2. **Test infrastructure crashed during discovery**
   - `YubiKeyTestInfrastructure.InitializeDevicesAsync()` called `Environment.Exit(-1)` on failure
   - This killed the test host process, preventing discovery

3. **Hardware access during discovery**
   - `AllAuthorizedDevices` static property triggered device enumeration on type load
   - YubiKeys lit up during discovery (connecting to hardware)

### Changes Made

| File | Change |
|------|--------|
| `YubiKeyTestInfrastructure.cs` | Replaced `Environment.Exit(-1)` with `return []` |
| `YubiKeyTestInfrastructure.cs` | Made `AllAuthorizedDevices` use `Lazy<T>` |
| `YubiKeyTestInfrastructure.cs` | Added `IsInitialized` property |
| `WithYubiKeyAttribute.cs` | Return placeholder when `!IsInitialized` instead of accessing hardware |
| `WithYubiKeyAttribute.cs` | Use `yield break` instead of throwing exceptions |
| `YubiKeyTestState.cs` | Added `Placeholder` static instance and `IsPlaceholder` property |

### Current State After Session 1

| Project | Uses `[WithYubiKey]` | Shows in VS Code |
|---------|---------------------|------------------|
| Core.IntegrationTests | ❌ No | ✅ Yes |
| Fido2.IntegrationTests | ❌ No | ✅ Yes |
| Piv.IntegrationTests | ✅ Yes | ✅ Yes (with placeholder) |
| Management.IntegrationTests | ✅ Yes | ❌ No |
| SecurityDomain.IntegrationTests | ✅ Yes | ❌ No |

### What Worked
- Removing `Environment.Exit(-1)` stopped test host crashes
- Using `Lazy<T>` for `AllAuthorizedDevices` deferred initialization
- Returning placeholder in `GetData()` when `!IsInitialized` - this fixed Piv.IntegrationTests
- YubiKeys no longer light up during discovery (hardware access fixed)

### What Didn't Work / Remaining Issues
- Management.IntegrationTests still doesn't appear
- SecurityDomain.IntegrationTests still doesn't appear
- CPU stays at 50% for ~1 minute after discovery (something still running)

### Remaining Mystery
- Piv works with placeholder, but Management and SecurityDomain don't appear
- All three use `[WithYubiKey]` identically
- Need to investigate why Piv behaves differently

### Suggested Next Steps
1. Compare Piv vs Management csproj files more closely
2. Check if Management/SecurityDomain have additional test infrastructure code (e.g., `IntegrationTestBase`)
3. Look at VS Code's C# extension logs for discovery errors
4. Check if there's something different about the test class structure
5. Verify solution file ordering/nesting differences
6. Check for assembly-level attributes differences

---

## Appendix: Key Files

- `Yubico.YubiKit.Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs`
- `Yubico.YubiKit.Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs`
- `Yubico.YubiKit.Tests.Shared/YubiKeyTestState.cs`
- Integration test projects:
  - `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/`
  - `Yubico.YubiKit.Management/tests/Yubico.YubiKit.Management.IntegrationTests/`
  - `Yubico.YubiKit.SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/`

---

## Session 2: 2026-01-19 (Evening)

### Problem Statement
Following Session 1, tests were discoverable via CLI (`dotnet test --list-tests`) but VS Code Test Explorer still didn't show Management and SecurityDomain integration tests.

### Key Finding
**CLI test discovery worked for ALL projects.** This indicated the problem was VS Code Test Explorer specific, not xUnit or the test infrastructure.

### Root Cause Analysis (via VS Code Logs)

Analyzed `~/.config/Code/logs/.../ms-dotnettools.csdevkit/'C# Dev Kit - Test Explorer.log'` and found:

1. **`Yubico.YubiKit.Tests.Shared` was marked as a test project**
   - Location: `Yubico.YubiKit.Tests.Shared/Yubico.YubiKit.Tests.Shared.csproj`
   - Problem: `<IsTestProject>true</IsTestProject>` caused VS Code to attempt test discovery on this utilities library
   - Error: `Testhost process for source(s) '...Yubico.YubiKit.Tests.Shared.dll' exited with error`
   - This error caused test discovery to **abort** (not just fail gracefully)
   - Log showed: `========== Test discovery aborted: 668 Tests found in 2.8 sec ==========`

2. **Duplicate test IDs from multiple `[WithYubiKey]` attributes**
   - Tests with multiple `[WithYubiKey]` attributes (e.g., `FormFactor_MatchesExpectedType` with 7 form factor filters)
   - All attributes returned the same `YubiKeyTestState.Placeholder` singleton
   - xUnit reported: `Skipping test case with duplicate ID '34d52d7b6301f8d67643c6b1c7e52a3ee63928e1'`
   - This caused tests to be skipped during discovery

### Changes Made

| File | Change |
|------|--------|
| `Yubico.YubiKit.Tests.Shared.csproj` | Changed `<IsTestProject>true</IsTestProject>` → `<IsTestProject>false</IsTestProject>` |
| `YubiKeyTestState.cs` | Added `CreatePlaceholder(string? filterDescription)` method |
| `YubiKeyTestState.cs` | Added `PlaceholderId` property (incremented via `Interlocked`) |
| `YubiKeyTestState.cs` | Added `FilterDescription` property |
| `YubiKeyTestState.cs` | Updated `ToString()` to include filter description: `YubiKey(Placeholder #N: FilterDesc)` |
| `WithYubiKeyAttribute.cs` | Added `BuildFilterDescription()` method |
| `WithYubiKeyAttribute.cs` | Changed to call `YubiKeyTestState.CreatePlaceholder(filterDescription)` |

### Verification

CLI discovery now shows unique placeholders for each `[WithYubiKey]` attribute:

```
ManagementSessionAdvancedTests.GetDeviceInfo_AllDevices_ReturnsValidData(state: YubiKey(Placeholder #1: All))
ManagementSessionAdvancedTests.ModernFeatures_FirmwareAtLeast530_SupportsAdvancedProtocols(state: YubiKey(Placeholder #2: FW>=5.3.0))
ManagementSessionTests.FormFactor_MatchesExpectedType(state: YubiKey(Placeholder #20: FF=UsbAKeychain))
ManagementSessionTests.FormFactor_MatchesExpectedType(state: YubiKey(Placeholder #21: FF=UsbCKeychain))
...
```

### Current State After Session 2

| Project | Uses `[WithYubiKey]` | CLI Discovery | VS Code (verify after reload) |
|---------|---------------------|---------------|-------------------------------|
| Core.IntegrationTests | ❌ No | ✅ Yes | ✅ Yes |
| Fido2.IntegrationTests | ❌ No | ✅ Yes | ✅ Yes |
| Piv.IntegrationTests | ✅ Yes | ✅ Yes | 🔄 Verify |
| Management.IntegrationTests | ✅ Yes | ✅ Yes | 🔄 Verify |
| SecurityDomain.IntegrationTests | ✅ Yes | ✅ Yes | 🔄 Verify |

### What Worked
- Removing `IsTestProject=true` from Tests.Shared eliminated the discovery abort
- Creating unique placeholders with filter descriptions eliminated duplicate test IDs
- Each `[WithYubiKey]` attribute now produces a distinct test case in the explorer

---

## Session 2b: 2026-01-19 (Later Evening)

### Problem Statement
Tests appeared in VS Code but running them failed with `NullReferenceException` - placeholders were being used during execution instead of real devices.

### Root Cause
xUnit serializes test data during discovery and deserializes it during execution. The flow was:
1. Discovery: `GetData()` returns placeholders → placeholders serialized
2. Execution: xUnit deserializes stored data → gets placeholder with `Device = null`
3. Test runs with null device → `NullReferenceException`

The previous fix only addressed `GetData()`, but xUnit doesn't call `GetData()` again during execution when using serialized data.

### Solution
Updated serialization/deserialization in `YubiKeyTestState`:
- **Serialize**: Now includes `IsPlaceholder` flag and `FilterDescription`
- **Deserialize**: When deserializing a placeholder, initializes the infrastructure and binds to a real device

### Changes Made

| File | Change |
|------|--------|
| `YubiKeyTestState.cs` | `Serialize()` now saves `IsPlaceholder` and `FilterDescription` |
| `YubiKeyTestState.cs` | `Deserialize()` detects placeholders and initializes real device binding |
| `WithYubiKeyAttribute.cs` | Simplified - only returns placeholder when `XUNIT_DISCOVERY_MODE=1` |

### Key Code Change in Deserialize

```csharp
public void Deserialize(IXunitSerializationInfo info)
{
    var isPlaceholder = info.GetValue<bool>(nameof(IsPlaceholder));
    
    if (isPlaceholder)
    {
        // Initialize infrastructure and bind to first available device
        var allDevices = YubiKeyTestInfrastructure.AllAuthorizedDevices;
        if (allDevices.Count == 0)
            throw new Xunit.SkipException("No authorized YubiKey devices available.");
        
        var device = allDevices[0];
        Device = device.Device;
        DeviceInfo = device.DeviceInfo;
        ConnectionType = device.ConnectionType;
        IsPlaceholder = false;
        return;
    }
    // ... normal deserialization for real devices
}
```

### Next Steps for User
1. **Rebuild solution**: `dotnet build`
2. **Reload VS Code window** (if tests don't update)
3. **Try running a test** - should now bind to real YubiKey device

### Diagnostic Commands Used

```bash
# Find VS Code C# Dev Kit logs
find ~/.config/Code/logs -name "ms-dotnettools.csdevkit" -type d

# View Test Explorer log
cat ~/.config/Code/logs/[latest]/window1/exthost/ms-dotnettools.csdevkit/'C# Dev Kit - Test Explorer.log'

# Search for errors
grep -i -E "(error|fail|aborted)" [logfile]

# CLI test discovery (works reliably)
dotnet test path/to/project.csproj --list-tests
```

---

## Known Issues / Future Investigation

### 1. ThreadAbortException During Debugging

**Status**: Observed, not yet investigated

**Symptom**: When step-debugging integration tests, occasionally get:
```
System.Threading.ThreadAbortException: 'System error.'
   at YubiKeyTestInfrastructure.InitializeDevicesAsync() line 349
   at System.Lazy`1.ViaFactory(LazyThreadSafetyMode mode)
   at YubiKeyTestInfrastructure.get_AllAuthorizedDevices()
   at YubiKeyTestState.BindToRealDevice()
   at YubiKeyTestState.get_Device()
```

**Possible Causes**:
- Test runner timeout during slow debugger stepping
- Async/sync deadlock in `Lazy<T>.ViaFactory` calling async code
- CancellationToken being triggered

**To Investigate**:
- Check if `InitializeDevicesAsync()` is being called synchronously (sync-over-async)
- Review test runner timeout settings
- Check for `CancellationToken` propagation issues

### 2. ObjectDisposedException During Debugging

**Status**: Observed intermittently, not reproducible on demand

**Symptom**: Occasional `ObjectDisposedException` during test debugging sessions.

**Possible Causes**:
- Device connection being disposed while debugger paused
- Race condition in device lifecycle management
- USB connection timeout during long debug pauses

**To Investigate**:
- Identify which object is being disposed
- Check device connection timeout settings
- Review IDisposable patterns in connection classes
