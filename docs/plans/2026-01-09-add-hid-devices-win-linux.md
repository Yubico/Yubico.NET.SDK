# Add HID Devices Implementation Plan (Windows & Linux)

**Status:** 🔲 DEFERRED - Implement after macOS HID support is complete.

**Goal:** Enable YubiKey applications (FIDO2, OTP) to connect via HID transport on Windows and Linux platforms.

**Prerequisites:** Complete macOS HID implementation (`docs/plans/2026-01-09-add-hid-devices.md`)

---

## Reference Implementations

### Legacy C# SDK (this repo)
- **Location:** `./legacy-develop/Yubico.Core/src/Yubico/Core/Devices/Hid/`
- **Windows files:**
  - `WindowsHidDevice.cs` - SetupDi enumeration
  - `WindowsHidIOReportConnection.cs` - FIDO I/O via CreateFile/ReadFile/WriteFile
  - `WindowsHidFeatureReportConnection.cs` - OTP via HidD_GetFeature/HidD_SetFeature
  - `WindowsHidDeviceListener.cs` - WMI-based device notifications
- **Linux files:**
  - `LinuxHidDevice.cs` - udev enumeration
  - `LinuxHidIOReportConnection.cs` - hidraw file I/O
  - `LinuxHidFeatureReportConnection.cs` - ioctl for feature reports
  - `LinuxHidDeviceListener.cs` - udev monitor
- **P/Invoke:**
  - Windows: `./legacy-develop/Yubico.Core/src/Yubico/PlatformInterop/Windows/`
  - Linux: udev and libc bindings

### Java SDK (yubikit-android)
- **Location:** `../yubikit-android/`
- **Use for:** Protocol logic, not platform-specific code

---

## Windows Implementation

### Existing Code in Current SDK
- `Yubico.YubiKit.Core/src/PlatformInterop/Windows/HidD/HidDDevice.cs` - Partial implementation
- `Yubico.YubiKit.Core/src/PlatformInterop/Windows/HidD/HidD.Interop.cs` - P/Invoke
- `Yubico.YubiKit.Core/src/Hid/WindowsHidIOReportConnection.cs` - Exists but may need updates
- `Yubico.YubiKit.Core/src/Hid/WindowsHidFeatureReportConnection.cs` - Exists but may need updates

### Tasks

#### Task W1: Add SetupDi P/Invoke for HID Enumeration

**Reference:** `legacy-develop/Yubico.Core/src/Yubico/PlatformInterop/Windows/SetupApi/`

**Files:**
- Create: `Yubico.YubiKit.Core/src/PlatformInterop/Windows/SetupDi/SetupDi.Interop.cs`
- Create: `Yubico.YubiKit.Core/src/PlatformInterop/Windows/SetupDi/HidDeviceEnumerator.cs`

**Implementation:**
- Port P/Invoke from legacy: SetupDiGetClassDevsW, SetupDiEnumDeviceInterfaces, SetupDiGetDeviceInterfaceDetailW
- Filter by GUID_DEVINTERFACE_HID

#### Task W2: Create WindowsHidDevice

**Reference:** `legacy-develop/Yubico.Core/src/Yubico/Core/Devices/Hid/WindowsHidDevice.cs`

**Files:**
- Create: `Yubico.YubiKit.Core/src/Hid/WindowsHidDevice.cs`

**Implementation:**
- Port `GetList()` using SetupDi enumeration
- Use existing `HidDDevice` for device properties

#### Task W3: Update FindHidDevices for Windows

**Files:**
- Modify: `Yubico.YubiKit.Core/src/Hid/FindHidDevices.cs`

**Implementation:**
```csharp
private IReadOnlyList<IHidDevice> FindAll(HidUsagePage? usagePage) =>
    OperatingSystem.IsWindows() ? FindAllWindows(usagePage) :
    OperatingSystem.IsMacOS() ? FindAllMacOS(usagePage) :
    [];
```

---

## Linux Implementation

### Tasks

#### Task L1: Add udev P/Invoke

**Reference:** `legacy-develop/Yubico.Core/src/Yubico/PlatformInterop/Linux/Udev/`

**Files:**
- Create: `Yubico.YubiKit.Core/src/PlatformInterop/Linux/Udev/Udev.Interop.cs`
- Create: `Yubico.YubiKit.Core/src/PlatformInterop/Linux/Udev/UdevHidEnumerator.cs`

**P/Invoke functions:**
- udev_new, udev_enumerate_new, udev_enumerate_add_match_subsystem
- udev_enumerate_scan_devices, udev_enumerate_get_list_entry
- udev_device_new_from_syspath, udev_device_get_devnode
- udev_device_get_property_value

#### Task L2: Create LinuxHidDevice

**Reference:** `legacy-develop/Yubico.Core/src/Yubico/Core/Devices/Hid/LinuxHidDevice.cs`

**Files:**
- Create: `Yubico.YubiKit.Core/src/Hid/LinuxHidDevice.cs`

#### Task L3: Create LinuxHidConnection (hidraw)

**Reference:** `legacy-develop/Yubico.Core/src/Yubico/Core/Devices/Hid/LinuxHidIOReportConnection.cs`

**Files:**
- Create: `Yubico.YubiKit.Core/src/Hid/LinuxHidIOReportConnection.cs`
- Create: `Yubico.YubiKit.Core/src/Hid/LinuxHidFeatureReportConnection.cs`

**Implementation:**
- Open hidraw device file (/dev/hidraw*)
- Use ioctl for HIDIOCGRDESCSIZE, HIDIOCGRDESC
- Read/write for reports

#### Task L4: Update FindHidDevices for Linux

**Files:**
- Modify: `Yubico.YubiKit.Core/src/Hid/FindHidDevices.cs`

---

## Platform Considerations

### Windows
- May need admin privileges for exclusive HID access
- Windows Hello may block FIDO HID access
- Uses setupapi.dll for enumeration

### Linux
- Requires udev rules for non-root hidraw access
- Typical rule: `KERNEL=="hidraw*", ATTRS{idVendor}=="1050", MODE="0660", GROUP="plugdev"`
- SELinux/AppArmor may restrict access

---

## Testing

### Windows Tests
- SetupDi enumeration
- HID device properties
- FIDO I/O report communication
- OTP feature report communication

### Linux Tests
- udev enumeration
- hidraw file I/O
- Feature report ioctl

---

## Common Patterns

All implementations should:
- Follow `IHidDevice`/`IHidConnection` interfaces
- Use `PlatformApiException` for errors
- Support the same `IFindHidDevices` interface
- Integrate with `DeviceRepositoryCached` via `FindYubiKeys`
