# Add HID Devices Work Case

## Description
Find out which YubiKey applications are supported over HID. 

Implement HID devices (into device repository, listeners, etc)

Connect to new HID based application

## Status

### macOS HID Support - ✅ COMPLETE

#### Implementation Summary
- ✅ Ported `MacOSHidDevice` from legacy SDK with modern C# patterns
- ✅ Ported `MacOSHidFeatureReportConnection` for OTP (keyboard) feature reports
- ✅ Ported `MacOSHidIOReportConnection` for FIDO I/O reports with CFRunLoop callbacks
- ✅ Created `IFindHidDevices` service for HID device enumeration
- ✅ Created `HidYubiKey` implementing `IYubiKey` interface
- ✅ Created `IAsyncHidConnection` for async HID operations
- ✅ Integrated HID into `YubiKeyFactory` and `FindYubiKeys`
- ✅ Registered services in DependencyInjection container
- ✅ Added integration tests

#### Architecture
- HID devices are enumerated via `MacOSHidDevice.GetList()` using IOKit framework
- Filtered to Yubico VendorId (0x1050) in `FindHidDevices`
- `FindYubiKeys` aggregates both PCSC and HID devices in parallel
- DeviceId format: `hid:{VendorId:X4}:{ProductId:X4}:{Usage:X4}`
- Connection type selection based on UsagePage:
  - `HidUsagePage.Fido` (0xF1D0) → IOReports
  - `HidUsagePage.Keyboard` (1) → FeatureReports

#### Usage Example
```csharp
using Yubico.YubiKit.Core.YubiKey;

// Enumerate all YubiKeys (includes both PCSC and HID)
var finder = FindYubiKeys.Create();
var yubiKeys = await finder.FindAllAsync();

foreach (var yubiKey in yubiKeys)
{
    Console.WriteLine($"Found: {yubiKey.DeviceId}");
    // DeviceId examples:
    // - pcsc:Yubico YubiKey OTP+FIDO+CCID
    // - hid:1050:0407:0001  (Keyboard/OTP)
    // - hid:1050:0407:F1D0  (FIDO)
}

// Connect to HID device
var hidYubiKey = yubiKeys.First(yk => yk.DeviceId.StartsWith("hid:"));
using var connection = await hidYubiKey.ConnectAsync<IAsyncHidConnection>();

// Use the connection for FIDO or OTP operations
```

### Windows and Linux - ⏳ PENDING
See: `docs/plans/2026-01-09-add-hid-devices-win-linux.md`

## Platform Coverage

| Platform | Status | Notes |
|----------|--------|-------|
| macOS    | ✅ Complete | IOKit-based implementation |
| Windows  | ⏳ TODO | Will use SetupDi + HidD APIs |
| Linux    | ⏳ TODO | Will use udev + hidraw |

## Supported YubiKey Applications over HID

| Application | HID Interface | Status |
|-------------|---------------|--------|
| FIDO2/U2F   | FIDO (0xF1D0) | ✅ Enumeration complete, protocol layer separate work |
| OTP         | Keyboard (1)  | ✅ Enumeration complete, protocol layer separate work |

## Integration with Device Discovery

HID devices now flow through the same architecture as PCSC devices:
- `FindYubiKeys` → aggregates from multiple finders
- `DeviceChannel` → publishes device lists
- `DeviceRepositoryCached` → maintains reactive cache with `IObservable<DeviceEvent>`
- `DeviceMonitorService` → polls for changes in background

Output DONE (macOS)