---
active: true
iteration: 1
max_iterations: 0
completion_promise: "DONE"
started_at: "2026-01-09T01:20:32Z"
---

# Add HID Devices Implementation Plan (macOS)

**Goal:** Enable YubiKey applications (FIDO2, OTP) to connect via HID transport in addition to SmartCard on macOS.

**Architecture:** Integrate HID device discovery into the new SDK's background service/channel/cache architecture. HID devices flow through `FindYubiKeys` → `DeviceChannel` → `DeviceRepositoryCached` like PCSC devices.

**Platform Scope:** macOS only. See `docs/plans/2026-01-09-add-hid-devices-win-linux.md` for Windows/Linux.

---

## Reference Implementations

### Java SDK (yubikit-android)
- **Location:** `../yubikit-android/`
- **Key abstractions:** `FidoConnection` (64-byte HID packets), `OtpConnection` (8-byte feature reports)
- **Constants:** Yubico VID=0x1050, FIDO UsagePage=0xF1D0, OTP UsagePage=0x0001
- **Use for:** Protocol logic, CTAP framing, error handling patterns

### Legacy C# SDK (this repo)
- **Location:** `./legacy-develop/Yubico.Core/src/Yubico/Core/Devices/Hid/`
- **Key files:**
  - `MacOSHidDevice.cs` - IOKit device enumeration via `IOHIDManagerCopyDevices`
  - `MacOSHidIOReportConnection.cs` - FIDO I/O reports with CFRunLoop callbacks
  - `MacOSHidFeatureReportConnection.cs` - OTP feature reports
  - `MacOSHidDeviceListener.cs` - Background listener with arrival/removal callbacks
- **P/Invoke:** `./legacy-develop/Yubico.Core/src/Yubico/PlatformInterop/macOS/IOKitFramework/`
- **Use for:** P/Invoke signatures, IOKit patterns, memory pinning for callbacks

### Current SDK (this repo)
- **Architecture:**
  - `DeviceMonitorService` - Background polling service
  - `DeviceChannel` - Channel-based pub/sub for device lists
  - `DeviceRepositoryCached` - Reactive cache with `IObservable<DeviceEvent>`
  - `FindYubiKeys` - Aggregates device finders (currently PCSC only)
- **Goal:** Integrate HID into this architecture, not replicate legacy patterns

---

## CLAUDE.md Guidelines Summary

**Memory Management:**
- ✅ Sync + ≤512 bytes → `Span<byte>` with `stackalloc`
- ✅ Sync + >512 bytes → `ArrayPool<byte>.Shared.Rent()`
- ✅ Async → `Memory<byte>` or `IMemoryOwner<byte>`
- ❌ NEVER use `.ToArray()` unless data must escape scope

**Code Quality:**
- ✅ Use `is null` / `is not null` (never `== null`)
- ✅ Use switch expressions
- ✅ Use file-scoped namespaces
- ✅ Use collection expressions `[..]`
- ❌ NEVER use `#region`

**Build/Test:**
- Use `dotnet build.cs build` and `dotnet build.cs test`

---

## Existing Infrastructure in Current SDK

**IOKit P/Invoke (already exists):**
- `Yubico.YubiKit.Core/src/PlatformInterop/MacOS/IOKitFramework/IOKitHid.Interop.cs`
- `Yubico.YubiKit.Core/src/PlatformInterop/MacOS/IOKitFramework/IOKitHidConstants.cs`
- `Yubico.YubiKit.Core/src/PlatformInterop/MacOS/CoreFoundation/CoreFoundation.Interop.cs`
- `Yubico.YubiKit.Core/src/Hid/IOKitHelpers.cs`

**HID Interfaces (already exists):**
- `IHidDevice` - VendorId, ProductId, Usage, UsagePage, ConnectToFeatureReports/IOReports
- `IHidConnection` - SetReport/GetReport (sync)
- `HidUsagePage` enum - Fido=0xF1D0, Keyboard=1

**What's Missing:**
1. `MacOSHidDevice` implementing `IHidDevice`
2. `MacOSHidIOReportConnection` for FIDO (uses CFRunLoop callbacks)
3. `MacOSHidFeatureReportConnection` for OTP
4. `IFindHidDevices` service analogous to `IFindPcscDevices`
5. `HidYubiKey` implementing `IYubiKey`
6. Integration into `YubiKeyFactory` and `FindYubiKeys`
7. Registration in `DependencyInjection.cs`

---

## Task 1: Port MacOSHidDevice from Legacy

**Reference:** `legacy-develop/Yubico.Core/src/Yubico/Core/Devices/Hid/MacOSHidDevice.cs`

**Files:**
- Create: `Yubico.YubiKit.Core/src/Hid/MacOSHidDevice.cs`

**Key patterns from legacy:**
- Use `IOHIDDeviceGetService` → `IORegistryEntryGetRegistryEntryID` for stable entry ID
- Properties via `IOKitHelpers.GetNullableIntPropertyValue`
- Static `GetList()` method using `IOHIDManagerCreate` → `IOHIDManagerCopyDevices`

**Modern C# adaptations:**
- File-scoped namespace
- Primary constructor if appropriate
- `[SupportedOSPlatform("macos")]` attribute
- No `#region` blocks

**Implementation:**
```csharp
// Port from legacy-develop/Yubico.Core/src/Yubico/Core/Devices/Hid/MacOSHidDevice.cs
// Key changes:
// - Modern C# 14 syntax
// - File-scoped namespace
// - Use existing IOKitHelpers from current SDK
```

**Test:** Verify type implements `IHidDevice` and can enumerate devices.

**Commit:** `feat(hid): add MacOSHidDevice ported from legacy SDK`

---

## Task 2: Port MacOSHidIOReportConnection from Legacy

**Reference:** `legacy-develop/Yubico.Core/src/Yubico/Core/Devices/Hid/MacOSHidIOReportConnection.cs`

**Files:**
- Create: `Yubico.YubiKit.Core/src/Hid/MacOSHidIOReportConnection.cs`

**Key patterns from legacy:**
- Uses `IORegistryEntryIDMatching` → `IOServiceGetMatchingService` → `IOHIDDeviceCreate`
- Registers `IOHIDReportCallback` for async input reports
- Uses `ConcurrentQueue<byte[]>` to buffer reports
- CFRunLoop integration with 6-second timeout for reclaim
- GCHandle pinning for callback buffers

**Critical implementation details:**
- Delegate instances must be kept alive (stored as fields)
- Read buffer must be pinned via `GCHandle.Alloc(..., GCHandleType.Pinned)`
- Must call `IOHIDDeviceRegisterInputReportCallback` with `IntPtr.Zero` on dispose

**Modern C# adaptations:**
- Consider `Channel<byte[]>` instead of `ConcurrentQueue` for async consumption
- Use `ObjectDisposedException.ThrowIf(_disposed, this)`

**Test:** Verify I/O report send/receive with mock or real device.

**Commit:** `feat(hid): add MacOSHidIOReportConnection for FIDO HID`

---

## Task 3: Port MacOSHidFeatureReportConnection from Legacy

**Reference:** `legacy-develop/Yubico.Core/src/Yubico/Core/Devices/Hid/MacOSHidFeatureReportConnection.cs`

**Files:**
- Create: `Yubico.YubiKit.Core/src/Hid/MacOSHidFeatureReportConnection.cs`

**Key patterns:**
- Simpler than IO reports - direct `IOHIDDeviceGetReport`/`IOHIDDeviceSetReport`
- Uses `kIOHidReportTypeFeature` (2) instead of output (1)
- No callback mechanism needed

**Test:** Verify feature report send/receive.

**Commit:** `feat(hid): add MacOSHidFeatureReportConnection for OTP`

---

## Task 4: Create IFindHidDevices Service

**Files:**
- Create: `Yubico.YubiKit.Core/src/Hid/IFindHidDevices.cs`
- Create: `Yubico.YubiKit.Core/src/Hid/FindHidDevices.cs`

**Design:**
```csharp
public interface IFindHidDevices
{
    Task<IReadOnlyList<IHidDevice>> FindAllAsync(CancellationToken cancellationToken = default);
}
```

**Implementation:**
- Call `MacOSHidDevice.GetList()` (ported in Task 1)
- Filter to Yubico VendorId (0x1050)
- Support optional UsagePage filter

**Test:** Verify enumeration returns Yubico devices only.

**Commit:** `feat(hid): add FindHidDevices service`

---

## Task 5: Create HidYubiKey and IAsyncHidConnection

**Files:**
- Create: `Yubico.YubiKit.Core/src/Hid/IAsyncHidConnection.cs`
- Create: `Yubico.YubiKit.Core/src/YubiKey/HidYubiKey.cs`

**Design:**
```csharp
public interface IAsyncHidConnection : IConnection
{
    int InputReportSize { get; }
    int OutputReportSize { get; }
    Task SetReportAsync(ReadOnlyMemory<byte> report, CancellationToken ct = default);
    Task<ReadOnlyMemory<byte>> GetReportAsync(CancellationToken ct = default);
}
```

**HidYubiKey:**
- Implements `IYubiKey`
- DeviceId format: `hid:{VendorId:X4}:{ProductId:X4}:{Usage:X4}`
- `ConnectAsync<IAsyncHidConnection>()` returns wrapped connection
- FIDO → IOReports, Keyboard → FeatureReports

**Test:** Verify DeviceId format and connection type selection.

**Commit:** `feat(hid): add HidYubiKey wrapper`

---

## Task 6: Integrate into YubiKeyFactory and FindYubiKeys

**Files:**
- Modify: `Yubico.YubiKit.Core/src/YubiKey/YubiKeyFactory.cs`
- Modify: `Yubico.YubiKit.Core/src/YubiKey/FindYubiKeys.cs`

**YubiKeyFactory changes:**
```csharp
public IYubiKey Create(IDevice device) =>
    device switch
    {
        IPcscDevice pcscDevice => CreatePcscYubiKey(pcscDevice),
        IHidDevice hidDevice => CreateHidYubiKey(hidDevice),
        _ => throw new NotSupportedException(...)
    };
```

**FindYubiKeys changes:**
- Add `IFindHidDevices` dependency
- Run PCSC and HID enumeration in parallel
- Aggregate results

**Test:** Verify factory handles both device types.

**Commit:** `feat(hid): integrate HID into YubiKeyFactory and FindYubiKeys`

---

## Task 7: Register in DependencyInjection

**Files:**
- Modify: `Yubico.YubiKit.Core/src/DependencyInjection.cs`

**Changes:**
```csharp
services.AddTransient<IFindHidDevices, FindHidDevices>();
```

**Test:** Verify DI resolution works.

**Commit:** `feat(hid): register HID services in DI container`

---

## Task 8: Integration Tests

**Files:**
- Create: `Yubico.YubiKit.IntegrationTests/Hid/MacOSHidIntegrationTests.cs`

**Tests:**
- Enumerate HID devices (requires hardware)
- Connect to FIDO device and verify report sizes
- Optional: Send CTAP2 GetInfo command

**Commit:** `test(hid): add macOS HID integration tests`

---

## Task 9: Update Documentation

**Files:**
- Modify: `docs/docs:add-HID.md`

**Mark as complete for macOS with usage examples.**

Output `<promise>DONE</promise>` when all tasks verified.

---

## Verification Checklist

- [ ] `dotnet build.cs build` passes
- [ ] `dotnet build.cs test` passes
- [ ] HID enumeration works on macOS (manual test)
- [ ] `FindYubiKeys.FindAllAsync()` returns both PCSC and HID devices
- [ ] `HidYubiKey.ConnectAsync<IAsyncHidConnection>()` works
- [ ] Code follows CLAUDE.md (no #region, modern C#, memory patterns)
- [ ] Integration with DeviceRepositoryCached works via FindYubiKeys

---

## Future Work (separate plans)

- Windows HID (SetupDi + HidD)
- Linux HID (udev + hidraw)  
- Native event-based HID listener (IOKit callbacks instead of polling)
- CTAP2 protocol layer over HID
- Composite device correlation

---

## Hardware Test Environment

**Available YubiKey:** Serial #125 is connected via USB.

**Test Guidelines:**
- Hardware tests MAY be attempted but should not block progress
- If a hardware test fails 2-3 times, skip it and proceed with other tasks
- Do NOT endlessly retry failing hardware operations
- The device may need manual intervention (touch, PIN entry, replug)
- Mark hardware-dependent tests with `[Trait("RequiresHardware", "true")]`
- Use `Skip` attribute or early return if no device detected

**Failure Handling:**
```csharp
// Pattern for hardware tests
[Fact]
[Trait("RequiresHardware", "true")]
public async Task SomeHardwareTest()
{
    var devices = await finder.FindAllAsync();
    if (devices.Count == 0)
    {
        // No device - skip gracefully, don't fail
        return;
    }
    
    // Proceed with test...
}
```

**If hardware tests consistently fail:**
1. Log the failure reason
2. Move on to the next task
3. Document what manual steps may be needed
4. Do NOT loop indefinitely on hardware operations
