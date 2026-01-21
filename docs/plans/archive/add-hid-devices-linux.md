# Add HID Devices Implementation Plan (Linux)

**Status:** Ready for implementation (macOS HID support is complete)

**Goal:** Enable YubiKey applications (FIDO2, OTP) to connect via HID transport on Linux.

---

## Reference Implementations

### Legacy C# SDK (this repo)
- **Location:** `./legacy-develop/Yubico.Core/src/Yubico/Core/Devices/Hid/`
- **Linux files:**
  - `LinuxHidDevice.cs` - udev enumeration
  - `LinuxHidIOReportConnection.cs` - hidraw file I/O
  - `LinuxHidFeatureReportConnection.cs` - ioctl for feature reports
  - `LinuxHidDeviceListener.cs` - udev monitor
- **P/Invoke:** `./legacy-develop/Yubico.Core/src/Yubico/PlatformInterop/Linux/` - udev and libc bindings

### Java SDK (yubikit-android)
- **Location:** `../yubikit-android/`
- **Use for:** Protocol logic, not platform-specific code

---

## Linux Implementation

### Tasks

#### Task L1: Add udev P/Invoke

**Reference:** `legacy-develop/Yubico.Core/src/Yubico/PlatformInterop/Linux/Udev/`

**Files:**
- Create: `Yubico.YubiKit.Core/src/PlatformInterop/Linux/Udev/Udev.Interop.cs`
- Create: `Yubico.YubiKit.Core/src/PlatformInterop/Linux/Udev/UdevHidEnumerator.cs`

**P/Invoke functions:**
- `udev_new` - Create udev context
- `udev_enumerate_new` - Create enumeration context
- `udev_enumerate_add_match_subsystem` - Filter by "hidraw" subsystem
- `udev_enumerate_scan_devices` - Perform device scan
- `udev_enumerate_get_list_entry` - Get first device in list
- `udev_list_entry_get_next` - Iterate device list
- `udev_list_entry_get_name` - Get device syspath
- `udev_device_new_from_syspath` - Create device handle from path
- `udev_device_get_devnode` - Get /dev/hidraw* path
- `udev_device_get_property_value` - Get device properties (vendor ID, product ID, etc.)
- `udev_device_get_parent_with_subsystem_devtype` - Get parent USB device for attributes
- `udev_device_unref` - Release device handle
- `udev_enumerate_unref` - Release enumeration context
- `udev_unref` - Release udev context

#### Task L2: Create LinuxHidDevice

**Reference:** `legacy-develop/Yubico.Core/src/Yubico/Core/Devices/Hid/LinuxHidDevice.cs`

**Files:**
- Create: `Yubico.YubiKit.Core/src/Hid/LinuxHidDevice.cs`

**Implementation:**
- Implement `IHidDevice` interface
- Use udev enumeration to discover devices
- Filter by Yubico vendor ID (0x1050)
- Extract device properties:
  - Vendor ID, Product ID
  - Usage Page, Usage ID (from HID descriptor)
  - Device path (/dev/hidraw*)
  - Serial number (if available)

#### Task L3: Create LinuxHidConnection (hidraw)

**Reference:** `legacy-develop/Yubico.Core/src/Yubico/Core/Devices/Hid/LinuxHidIOReportConnection.cs`

**Files:**
- Create: `Yubico.YubiKit.Core/src/Hid/LinuxHidIOReportConnection.cs`
- Create: `Yubico.YubiKit.Core/src/Hid/LinuxHidFeatureReportConnection.cs`
- Create: `Yubico.YubiKit.Core/src/PlatformInterop/Linux/Libc/Libc.Interop.cs` (if not exists)

**Implementation:**

**For IO Reports (FIDO2):**
- Open hidraw device file (`/dev/hidraw*`) with `open()`
- Read reports using `read()` syscall
- Write reports using `write()` syscall
- Close with `close()`

**For Feature Reports (OTP):**
- Use `ioctl()` with:
  - `HIDIOCGRDESCSIZE` - Get HID report descriptor size
  - `HIDIOCGRDESC` - Get HID report descriptor
  - `HIDIOCSFEATURE` - Set feature report
  - `HIDIOCGFEATURE` - Get feature report

**ioctl constants:**
```csharp
// From linux/hidraw.h
const uint HIDIOCGRDESCSIZE = 0x01;  // _IOR('H', 0x01, int)
const uint HIDIOCGRDESC = 0x02;      // _IOR('H', 0x02, struct hidraw_report_descriptor)
const uint HIDIOCGFEATURE = 0x07;    // _IOWR('H', 0x07, buffer)
const uint HIDIOCSFEATURE = 0x06;    // _IOWR('H', 0x06, buffer)
```

#### Task L4: Update FindHidDevices for Linux

**Files:**
- Modify: `Yubico.YubiKit.Core/src/Hid/FindHidDevices.cs`

**Implementation:**
```csharp
private IReadOnlyList<IHidDevice> FindAll(HidUsagePage? usagePage) =>
    OperatingSystem.IsWindows() ? FindAllWindows(usagePage) :
    OperatingSystem.IsMacOS() ? FindAllMacOS(usagePage) :
    OperatingSystem.IsLinux() ? FindAllLinux(usagePage) :
    [];
```

---

## Platform Considerations

### Permissions
- Requires udev rules for non-root hidraw access
- Typical rule file `/etc/udev/rules.d/70-yubikey.rules`:
```
KERNEL=="hidraw*", ATTRS{idVendor}=="1050", MODE="0660", GROUP="plugdev"
```
- User must be in `plugdev` group (or whatever group is specified)

### Security Contexts
- SELinux may restrict access to /dev/hidraw* devices
- AppArmor may also restrict access
- May need to document required SELinux/AppArmor policies

### Library Dependencies
- Requires `libudev.so.1` at runtime
- Usually available as part of systemd (`libsystemd` package)
- On non-systemd systems, may need `libudev-dev` or `eudev`

---

## Testing

### Linux Tests
- udev enumeration finds YubiKey devices
- hidraw file I/O for FIDO2 reports
- Feature report ioctl for OTP
- Permission denied handling (when udev rules not set)
- Multiple YubiKey handling

### Test Setup
- Ensure udev rules are installed
- Ensure user is in correct group
- Verify /dev/hidraw* devices appear when YubiKey inserted

---

## Common Patterns

All implementations should:
- Follow `IHidDevice`/`IHidConnection` interfaces (same as macOS)
- Use `PlatformApiException` for errors
- Support the same `IFindHidDevices` interface
- Integrate with `DeviceRepositoryCached` via `FindYubiKeys`

---

## File Structure

```
Yubico.YubiKit.Core/src/
├── Hid/
│   ├── LinuxHidDevice.cs              # L2: Device enumeration
│   ├── LinuxHidIOReportConnection.cs  # L3: FIDO2 I/O reports
│   └── LinuxHidFeatureReportConnection.cs  # L3: OTP feature reports
└── PlatformInterop/
    └── Linux/
        ├── Udev/
        │   ├── Udev.Interop.cs        # L1: P/Invoke declarations
        │   └── UdevHidEnumerator.cs   # L1: Enumeration helper
        └── Libc/
            └── Libc.Interop.cs        # L3: open/read/write/ioctl
```

---

## Implementation Order

1. **L1: udev P/Invoke** - Foundation for device discovery
2. **L2: LinuxHidDevice** - Can enumerate devices after L1
3. **L3: LinuxHidConnection** - Can communicate after L2
4. **L4: FindHidDevices update** - Wire everything together
