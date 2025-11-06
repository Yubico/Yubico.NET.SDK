# YubiKey Device State Update After Configuration Changes - Research Findings

**Date:** 2025-10-31
**Author:** Claude (AI Research Assistant)
**Session:** claude/update-yubikey-state-after-config-011CUfMSuSHaao6LjWcmV3tw

## Executive Summary

Users expect that after calling configuration methods like `SetEnabledUsbCapabilities()`, `SetEnabledNfcCapabilities()`, or `SetIsNfcRestricted()`, the `YubiKeyDevice` instance properties should automatically reflect the new configuration. However, currently the properties remain stale until the device is re-enumerated.

The SDK has the infrastructure to update device state (via the `YubiKeyDeviceListener` and merge mechanisms), but this happens asynchronously and doesn't update the existing instance reference that users hold.

---

## Problem Statement

### Current Behavior
1. User obtains a `YubiKeyDevice` instance via `YubiKeyDevice.FindAll()` or similar
2. User calls a configuration method (e.g., `device.SetEnabledUsbCapabilities(YubiKeyCapabilities.Piv)`)
3. The method sends a command with `ResetAfterConfig = true`
4. The YubiKey device resets (reboots)
5. The method returns successfully
6. **The `YubiKeyDevice` instance properties remain stale** - they still show the old configuration
7. Eventually, the `YubiKeyDeviceListener` detects the device has reset and merges new info
8. However, the user's reference still points to the stale instance

### Expected Behavior
After calling configuration methods, the `YubiKeyDevice` instance properties should reflect the new configuration without requiring manual re-enumeration.

### Current Workaround
The SDK's own integration tests use `TestDeviceSelection.RenewDeviceEnumeration()` which:
- Waits 200ms between attempts
- Re-enumerates devices by serial number
- Retries up to 40 times (8 seconds total)
- Returns a fresh instance with updated properties

**Example from tests:** `Yubico.YubiKey/tests/integration/Yubico/YubiKey/YubiKeyDeviceTests.cs:507`
```csharp
testDevice.SetEnabledUsbCapabilities(desiredCapabilities);
testDevice = TestDeviceSelection.RenewDeviceEnumeration(testDevice.SerialNumber!.Value);
```

---

## Architecture Analysis

### Key Components

#### 1. YubiKeyDevice (`YubiKeyDevice.Instance.cs`)
- **Location:** `Yubico.YubiKey/src/Yubico/YubiKey/YubiKeyDevice.Instance.cs`
- **Key Field:** `private IYubiKeyDeviceInfo _yubiKeyDeviceInfo` (line 118)
- **Properties:** All device properties (lines 34-101) delegate to `_yubiKeyDeviceInfo`
  - `AvailableUsbCapabilities`
  - `EnabledUsbCapabilities`
  - `AvailableNfcCapabilities`
  - `EnabledNfcCapabilities`
  - `IsNfcRestricted`
  - `AutoEjectTimeout`
  - `ChallengeResponseTimeout`
  - `DeviceFlags`
  - `ConfigurationLocked`
  - etc.

#### 2. Configuration Methods
Methods that trigger device reset:

**SetEnabledUsbCapabilities** (`YubiKeyDevice.Instance.cs:336`)
```csharp
public void SetEnabledUsbCapabilities(YubiKeyCapabilities yubiKeyCapabilities)
{
    var command = new MgmtCmd.SetDeviceInfoCommand
    {
        EnabledUsbCapabilities = yubiKeyCapabilities,
        ResetAfterConfig = true,  // Triggers device reboot
    };

    var response = SendConfiguration(command);
    // Method returns here - device properties are NOT updated
}
```

**SetEnabledNfcCapabilities** (`YubiKeyDevice.Instance.cs:319`)
```csharp
public void SetEnabledNfcCapabilities(YubiKeyCapabilities yubiKeyCapabilities)
{
    var command = new MgmtCmd.SetDeviceInfoCommand
    {
        EnabledNfcCapabilities = yubiKeyCapabilities,
        ResetAfterConfig = true,  // Triggers device reboot
    };

    var response = SendConfiguration(command);
    // Method returns here - device properties are NOT updated
}
```

**Other affected methods:**
- `SetIsNfcRestricted()` (line 400)
- `SetLegacyDeviceConfiguration()` (line 489)
- `SetDeviceFlags()` (line 417) - May or may not trigger reset depending on flags
- `SetAutoEjectTimeout()` (line 379) - Does NOT trigger reset
- `SetChallengeResponseTimeout()` (line 358) - Does NOT trigger reset

#### 3. SetDeviceInfoBaseCommand
- **Location:** `Yubico.YubiKey/src/Yubico/YubiKey/Management/Commands/SetDeviceInfoBaseCommand.cs`
- **Key Property:** `ResetAfterConfig` (line 92)
  - When `true`, the YubiKey reboots after applying configuration
  - Used in TLV encoding at line 251-254

#### 4. YubiKeyDeviceInfo
- **Location:** `Yubico.YubiKey/src/Yubico/YubiKey/YubiKeyDeviceInfo.cs`
- Immutable data class holding all device properties
- **Merge Method** (line 366): Combines two YubiKeyDeviceInfo instances
  ```csharp
  internal YubiKeyDeviceInfo Merge(YubiKeyDeviceInfo? second)
  {
      // Creates a new YubiKeyDeviceInfo with merged properties
      return new YubiKeyDeviceInfo { /* merged properties */ };
  }
  ```

#### 5. YubiKeyDeviceListener
- **Location:** `Yubico.YubiKey/src/Yubico/YubiKey/YubiKeyDeviceListener.cs`
- Singleton background listener that monitors device arrival/removal
- **Update Method** (line 179): Re-enumerates devices and updates cache
- **MergeAndMarkExistingYubiKey** (line 315-323):
  ```csharp
  private void MergeAndMarkExistingYubiKey(
      YubiKeyDevice mergeTarget,
      YubiKeyDevice.YubicoDeviceWithInfo deviceWithInfo)
  {
      mergeTarget.Merge(deviceWithInfo.Device, deviceWithInfo.Info);
      _internalCache[mergeTarget] = true;
  }
  ```

#### 6. YubiKeyDevice.Merge Methods
**Location:** `YubiKeyDevice.Instance.cs`

**Merge with device and info** (lines 231-245):
```csharp
internal void Merge(IDevice device, IYubiKeyDeviceInfo info)
{
    MergeDevice(device);  // Updates internal device references

    // Updates the device info!
    if (_yubiKeyDeviceInfo is YubiKeyDeviceInfo first &&
        info is YubiKeyDeviceInfo second)
    {
        _yubiKeyDeviceInfo = first.Merge(second);
    }
    else
    {
        _yubiKeyDeviceInfo = info;
    }
}
```

**Merge with device only** (lines 216-224):
```csharp
internal void Merge(IDevice device)
{
    MergeDevice(device);  // Only updates device references, NOT info
}
```

---

## Properties Affected by Configuration Changes

### Always Updated by Configuration Commands

| Property | Command | Requires Reset | Location |
|----------|---------|---------------|----------|
| `EnabledUsbCapabilities` | `SetEnabledUsbCapabilities()` | Yes | `IYubiKeyDevice.cs:391` |
| `EnabledNfcCapabilities` | `SetEnabledNfcCapabilities()` | Yes | `IYubiKeyDevice.cs:296` |
| `IsNfcRestricted` | `SetIsNfcRestricted()` | No | `IYubiKeyDevice.cs:657` |
| `DeviceFlags` | `SetDeviceFlags()` | No | `IYubiKeyDevice.cs:490` |
| `AutoEjectTimeout` | `SetAutoEjectTimeout()` | No | `IYubiKeyDevice.cs:461` |
| `ChallengeResponseTimeout` | `SetChallengeResponseTimeout()` | No | `IYubiKeyDevice.cs:423` |
| `ConfigurationLocked` | `LockConfiguration()` | No | `IYubiKeyDevice.cs:528` |

### Potentially Affected by Reset

When the device resets (after USB/NFC capability changes), the following properties might change:

| Property | Why It Changes | Notes |
|----------|---------------|-------|
| `AvailableTransports` | USB capabilities changed | If USB capabilities are disabled, corresponding transports disappear |
| `HasSmartCard` | CCID capability disabled | Internal device references may be removed |
| `HasHidFido` | FIDO capability disabled | Internal device references may be removed |
| `HasHidKeyboard` | OTP capability disabled | Internal device references may be removed |

### Properties Retrieved from Device

All properties in `YubiKeyDeviceInfo` are retrieved via commands:
- **GetDeviceInfoCommand** (Management application, instruction 0x1D)
- **GetPagedDeviceInfoCommand** (for devices with lots of info)
- Device info factories for different transports:
  - `SmartCardDeviceInfoFactory.GetDeviceInfo()`
  - `KeyboardDeviceInfoFactory.GetDeviceInfo()`
  - `FidoDeviceInfoFactory.GetDeviceInfo()`

---

## Solution Options

### Option 1: Automatic Refresh After Configuration Change (Recommended)

**Approach:** After sending a configuration command with `ResetAfterConfig = true`, automatically re-query device info and update `_yubiKeyDeviceInfo`.

**Implementation Steps:**

1. **Add refresh logic to configuration methods:**
   ```csharp
   public void SetEnabledUsbCapabilities(YubiKeyCapabilities yubiKeyCapabilities)
   {
       var command = new MgmtCmd.SetDeviceInfoCommand
       {
           EnabledUsbCapabilities = yubiKeyCapabilities,
           ResetAfterConfig = true,
       };

       var response = SendConfiguration(command);

       if (response.Status != ResponseStatus.Success)
       {
           throw new InvalidOperationException(response.StatusMessage);
       }

       // NEW: Wait for device to reset and refresh state
       RefreshDeviceInfoAfterReset();
   }
   ```

2. **Implement RefreshDeviceInfoAfterReset method:**
   ```csharp
   private void RefreshDeviceInfoAfterReset(int maxRetries = 40, int delayMs = 200)
   {
       for (int i = 0; i < maxRetries; i++)
       {
           Thread.Sleep(delayMs);

           // Try to reconnect and get fresh device info
           try
           {
               var freshInfo = GetDeviceInfoFromDevice();
               if (freshInfo != null)
               {
                   _yubiKeyDeviceInfo = freshInfo;
                   return;
               }
           }
           catch
           {
               // Device not ready yet, continue retrying
           }
       }

       // Log warning but don't throw - device may still work
       _log.LogWarning("Could not refresh device info after reset");
   }
   ```

3. **Update device references after USB capability changes:**
   - When USB capabilities change, some transports may disappear
   - Need to update `_smartCardDevice`, `_hidFidoDevice`, `_hidKeyboardDevice`
   - This is more complex and may require re-enumeration via the listener

**Pros:**
- User-friendly - "just works" as expected
- Consistent with user expectations
- Maintains backward compatibility

**Cons:**
- Synchronous blocking (up to 8 seconds)
- May fail if device takes longer to reset
- Doesn't handle transport changes elegantly
- May conflict with background listener updates

### Option 2: Async Refresh Pattern

**Approach:** Provide an async version of configuration methods that await the device reset.

```csharp
public async Task SetEnabledUsbCapabilitiesAsync(
    YubiKeyCapabilities yubiKeyCapabilities,
    CancellationToken cancellationToken = default)
{
    var command = new MgmtCmd.SetDeviceInfoCommand
    {
        EnabledUsbCapabilities = yubiKeyCapabilities,
        ResetAfterConfig = true,
    };

    var response = SendConfiguration(command);

    if (response.Status != ResponseStatus.Success)
    {
        throw new InvalidOperationException(response.StatusMessage);
    }

    // Wait for device to reset and refresh
    await RefreshDeviceInfoAfterResetAsync(cancellationToken);
}
```

**Pros:**
- Non-blocking for callers
- Better for UI applications
- Can be cancelled

**Cons:**
- Breaking change (new API)
- Doesn't solve the problem for existing sync methods
- More complex implementation

### Option 3: Explicit Refresh Method

**Approach:** Add a public `RefreshDeviceInfo()` method that users can call.

```csharp
/// <summary>
/// Refreshes the device information from the YubiKey.
/// Call this after configuration changes that trigger device reset.
/// </summary>
public void RefreshDeviceInfo()
{
    var freshInfo = GetDeviceInfoFromDevice();
    if (freshInfo != null)
    {
        _yubiKeyDeviceInfo = freshInfo;
    }
}
```

**Usage:**
```csharp
device.SetEnabledUsbCapabilities(YubiKeyCapabilities.Piv);
Thread.Sleep(2000);  // Wait for reset
device.RefreshDeviceInfo();
```

**Pros:**
- Simple implementation
- Explicit control for users
- No breaking changes

**Cons:**
- Requires user action (not automatic)
- Users need to know to call it
- Doesn't solve transport changes
- Poor developer experience

### Option 4: Return Fresh Instance Pattern

**Approach:** Configuration methods return a fresh `IYubiKeyDevice` instance.

```csharp
/// <summary>
/// Sets enabled USB capabilities and returns a fresh device instance.
/// The current instance becomes stale after this call.
/// </summary>
public IYubiKeyDevice SetEnabledUsbCapabilitiesAndRenew(
    YubiKeyCapabilities yubiKeyCapabilities)
{
    SetEnabledUsbCapabilities(yubiKeyCapabilities);

    // Wait and re-enumerate
    return RenewDeviceEnumeration(SerialNumber.Value);
}
```

**Usage:**
```csharp
device = device.SetEnabledUsbCapabilitiesAndRenew(YubiKeyCapabilities.Piv);
```

**Pros:**
- Clear that a new instance is returned
- Handles transport changes correctly
- Similar to test workaround

**Cons:**
- Breaking change in usage pattern
- Requires serial number
- May not work for devices without serial numbers
- Confusing API

### Option 5: Event-Based Notification

**Approach:** Fire an event when device state is updated by the listener.

```csharp
public event EventHandler<YubiKeyDeviceEventArgs>? StateUpdated;

// In YubiKeyDeviceListener.MergeAndMarkExistingYubiKey:
mergeTarget.Merge(deviceWithInfo.Device, deviceWithInfo.Info);
mergeTarget.OnStateUpdated();  // Fires the event
```

**Usage:**
```csharp
device.StateUpdated += (sender, e) =>
{
    Console.WriteLine($"Updated: {e.Device.EnabledUsbCapabilities}");
};

device.SetEnabledUsbCapabilities(YubiKeyCapabilities.Piv);
// Event fires when listener detects and merges the change
```

**Pros:**
- Asynchronous and non-blocking
- Follows observer pattern
- Doesn't require polling

**Cons:**
- Complex for users to handle
- Timing issues
- May fire multiple times
- Doesn't provide immediate access to new state

---

## Recommended Solution: Hybrid Approach

Combine **Option 1** (automatic refresh) with improvements:

### Phase 1: Internal State Refresh (Minimal Breaking Changes)

1. **Add automatic refresh after reset** in methods that trigger device reboot:
   - `SetEnabledUsbCapabilities()`
   - `SetEnabledNfcCapabilities()`
   - `SetLegacyDeviceConfiguration()`

2. **Implement smart retry logic:**
   ```csharp
   private void RefreshDeviceInfoAfterReset()
   {
       const int maxAttempts = 40;
       const int delayMs = 200;

       for (int attempt = 0; attempt < maxAttempts; attempt++)
       {
           Thread.Sleep(delayMs);

           try
           {
               // Try to reconnect and get info
               var freshInfo = TryGetDeviceInfo();
               if (freshInfo != null)
               {
                   // Merge the new info
                   if (_yubiKeyDeviceInfo is YubiKeyDeviceInfo current)
                   {
                       _yubiKeyDeviceInfo = current.Merge(freshInfo);
                   }
                   else
                   {
                       _yubiKeyDeviceInfo = freshInfo;
                   }

                   _log.LogInformation(
                       "Successfully refreshed device info after reset (attempt {Attempt})",
                       attempt + 1);
                   return;
               }
           }
           catch (Exception ex)
           {
               _log.LogDebug(ex, "Failed to refresh device info, retrying...");
           }
       }

       _log.LogWarning(
           "Could not refresh device info after reset within timeout. " +
           "Properties may be stale. Call YubiKeyDevice.FindAll() to get fresh instance.");
   }
   ```

3. **Handle transport changes:**
   - If USB capabilities changed, some transports may be unavailable
   - Log warnings if refresh fails
   - Document that major changes may require re-enumeration

### Phase 2: Enhanced API (Future)

1. **Add async versions:**
   - `SetEnabledUsbCapabilitiesAsync()`
   - `SetEnabledNfcCapabilitiesAsync()`

2. **Add explicit refresh:**
   - `RefreshDeviceInfo()` - synchronous
   - `RefreshDeviceInfoAsync()` - asynchronous

3. **Add configuration result object:**
   ```csharp
   public class ConfigurationResult
   {
       public bool Success { get; set; }
       public bool StateRefreshed { get; set; }
       public string? Message { get; set; }
   }

   public ConfigurationResult SetEnabledUsbCapabilitiesEx(
       YubiKeyCapabilities yubiKeyCapabilities)
   {
       // ...
   }
   ```

---

## Implementation Considerations

### Challenges

1. **Device Reconnection Timing:**
   - Reset takes variable time (typically 1-3 seconds)
   - May take longer on some systems
   - Need retry logic with appropriate timeout

2. **Transport Availability:**
   - After disabling USB capabilities, some transports disappear
   - May not be able to reconnect with same transport
   - Need to try multiple transports in priority order

3. **Concurrent Access:**
   - User may have multiple references to the same device
   - Other threads may be accessing the device
   - Need thread-safe update mechanism

4. **Background Listener Interaction:**
   - `YubiKeyDeviceListener` also updates device state asynchronously
   - May conflict with manual refresh
   - Need coordination between manual and automatic updates

5. **Devices Without Serial Numbers:**
   - Some older devices don't report serial numbers
   - Cannot reliably re-enumerate by serial
   - May need to track by other means (path, etc.)

6. **Error Handling:**
   - What if refresh fails?
   - Should the original method throw?
   - Or return partial success?

### Testing Strategy

1. **Unit Tests:**
   - Mock device reset behavior
   - Verify retry logic
   - Test error handling

2. **Integration Tests:**
   - Test with real YubiKeys (multiple versions)
   - Verify state updates after each config method
   - Test timeout scenarios
   - Verify transport changes

3. **Performance Tests:**
   - Measure reset time across devices
   - Optimize retry delays
   - Verify no regressions

### Documentation Requirements

1. **API Documentation:**
   - Update XML comments for affected methods
   - Document that properties are refreshed automatically
   - Note potential delays (up to 8 seconds)
   - Explain fallback behavior if refresh fails

2. **Migration Guide:**
   - Explain changes in behavior
   - Remove workarounds from examples
   - Update best practices

3. **Known Issues:**
   - Document cases where refresh may fail
   - Explain when re-enumeration is still needed
   - Provide troubleshooting guidance

---

## Impact Assessment

### Breaking Changes
- **None** if implemented as Option 1
- Existing code continues to work
- Behavior change: methods now block longer (but this is desirable)

### Performance Impact
- Methods that trigger reset now block for 1-8 seconds (previously <100ms)
- This matches user expectations (device reset time is unavoidable)
- Consider async versions to avoid blocking

### Backward Compatibility
- Fully compatible with existing code
- Removes need for workarounds
- May break code that relies on stale state (edge case)

---

## Alternative: Document Current Behavior

If automatic refresh is not implemented, at minimum update documentation:

### IYubiKeyDevice Interface Documentation

```csharp
/// <summary>
/// Sets which USB features are enabled (and disabled).
/// </summary>
/// <remarks>
/// <para>
/// The YubiKey will reboot as part of this change. This will cause
/// this <c>IYubiKeyDevice</c> object to become stale, and the properties
/// will not reflect the new configuration until the device is re-enumerated.
/// </para>
///
/// <para>
/// To get fresh properties after this call, use:
/// <code>
/// device.SetEnabledUsbCapabilities(capabilities);
/// Thread.Sleep(2000);  // Wait for device to reset
/// device = YubiKeyDevice.FindAll()
///     .First(d => d.SerialNumber == device.SerialNumber);
/// </code>
/// </para>
///
/// <para>
/// Note: Future connection attempts using the stale object may work
/// or fail depending on which capabilities were changed.
/// </para>
/// </remarks>
void SetEnabledUsbCapabilities(YubiKeyCapabilities yubiKeyCapabilities);
```

---

## Related Code Locations

### Core Files
- `Yubico.YubiKey/src/Yubico/YubiKey/YubiKeyDevice.Instance.cs`
  - Configuration methods: lines 319-574
  - Merge logic: lines 216-245
  - Device info field: line 118

- `Yubico.YubiKey/src/Yubico/YubiKey/YubiKeyDeviceInfo.cs`
  - Device info class: entire file
  - Merge method: line 366

- `Yubico.YubiKey/src/Yubico/YubiKey/YubiKeyDeviceListener.cs`
  - Background listener: entire file
  - Update/merge logic: lines 179-355

- `Yubico.YubiKey/src/Yubico/YubiKey/IYubiKeyDevice.cs`
  - Interface definitions: lines 296-657

### Command Classes
- `Yubico.YubiKey/src/Yubico/YubiKey/Management/Commands/SetDeviceInfoCommand.cs`
- `Yubico.YubiKey/src/Yubico/YubiKey/Management/Commands/SetDeviceInfoBaseCommand.cs`
- `Yubico.YubiKey/src/Yubico/YubiKey/Management/Commands/GetDeviceInfoCommand.cs`

### Test Files
- `Yubico.YubiKey/tests/integration/Yubico/YubiKey/YubiKeyDeviceTests.cs`
  - Shows current workaround pattern
- `Yubico.YubiKey/tests/utilities/Yubico/YubiKey/TestUtilities/TestDeviceSelection.cs`
  - `RenewDeviceEnumeration` method: lines 31-53

---

## Next Steps

### Immediate Actions
1. Review this research document with the team
2. Decide on solution approach (recommend Option 1)
3. Create design document for implementation
4. Estimate effort and prioritize

### Implementation Phases

**CURRENT IMPLEMENTATION (In Progress):**

**Phase 1A: Quick Wins - Non-Resetting Methods (IMPLEMENTING NOW)**
Fix methods that don't trigger device reset - these can update properties immediately:
- ✅ `SetIsNfcRestricted()` - Update `IsNfcRestricted` property after success
- ✅ `SetDeviceFlags()` - Update `DeviceFlags` property after success
- ✅ `SetAutoEjectTimeout()` - Update `AutoEjectTimeout` property after success
- ✅ `SetChallengeResponseTimeout()` - Update `ChallengeResponseTimeout` property after success
- ✅ `LockConfiguration()` / `UnlockConfiguration()` - Update `ConfigurationLocked` property after success

**Implementation approach:**
- After successful `SendConfiguration()` call, directly update `_yubiKeyDeviceInfo`
- Since device doesn't reset, no reconnection logic needed
- Add integration tests to verify property updates immediately
- **Estimated effort:** 1-2 days

**FUTURE IMPLEMENTATION (Saved for later):**

**Phase 1B: Complex - Resetting Methods**
Fix methods that trigger device reset - require retry/reconnection logic:
- ⏳ `SetEnabledUsbCapabilities()` - Requires wait + retry + reconnection
- ⏳ `SetEnabledNfcCapabilities()` - Requires wait + retry + reconnection
- ⏳ `SetLegacyDeviceConfiguration()` - Requires wait + retry + reconnection

**Implementation approach:**
- Implement `RefreshDeviceInfoAfterReset()` with retry logic (40 attempts × 200ms)
- Handle transport availability changes
- More complex due to device reboot timing
- **Estimated effort:** 1-2 weeks

**Phase 2:** Documentation and migration guide (1 week)
**Phase 3:** Beta testing with early adopters (2 weeks)
**Phase 4:** Release with clear release notes

### Long-Term Improvements
1. Consider async API additions
2. Add event notifications for state changes
3. Improve device tracking across resets
4. Optimize retry logic based on telemetry

---

## Conclusion

The current behavior where `YubiKeyDevice` properties remain stale after configuration changes is a significant user experience issue. The SDK has all the infrastructure needed to solve this problem through automatic state refresh after reset operations.

**Recommendation:** Implement automatic refresh (Option 1) in all configuration methods that trigger device reset. This provides the best user experience with minimal breaking changes and aligns with user expectations.

The implementation should:
- Use retry logic (40 attempts × 200ms = 8 seconds max)
- Handle transport availability changes gracefully
- Log warnings if refresh fails
- Update documentation to reflect new behavior
- Add tests to verify state refresh

This change will eliminate the need for the `RenewDeviceEnumeration` workaround used throughout the test suite and provide a more intuitive API for end users.

---

## GitHub Issue Reference

This research and implementation addresses **GitHub Issue #192**: "IsNfcRestricted cache is not invalidated after SetIsNfcRestricted(true) is run"

**User's reported issue:**
```csharp
var yubiKey = YubiKeyDevice.FindAll().First();
yubiKey.SetIsNfcRestricted(true);
yubiKey.IsNfcRestricted  // Returns old cached value, not updated value
```

The issue affects all configuration methods, but Phase 1A implementation focuses on the non-resetting methods (including `SetIsNfcRestricted()`) which can be fixed immediately without complex retry logic.
