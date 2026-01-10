# Yubico.YubiKit.Management

> **Note:** This documentation is subject to change as the module evolves. Please check for updates regularly.

This module provides access to the YubiKey Management application, enabling device configuration, capability management, and device information retrieval.

## Overview

The Management application is the primary interface for configuring and managing YubiKey devices. It provides:

- **Device Information**: Query serial number, firmware version, form factor, capabilities
- **Capability Management**: Enable/disable applications over USB and NFC transports
- **Device Configuration**: Configure timeouts, device flags, NFC restrictions
- **Configuration Locking**: Protect device settings with a lock code
- **Device Reset**: Factory reset the device (firmware 5.6+)

## Requirements

- **Minimum Firmware**: YubiKey 4.1.0 for basic management
- **Advanced Features**: YubiKey 5.0+ for full configuration options
- **Device Reset**: YubiKey 5.6+ required

## Key Concepts

### Device Capabilities

YubiKeys support multiple applications that can be enabled/disabled independently:

- **Otp** (0x01): YubiOTP application
- **U2f** (0x02): FIDO U2F (CTAP1)
- **OpenPgp** (0x08): OpenPGP Card protocol
- **Piv** (0x10): PIV smart card
- **Oath** (0x20): OATH (TOTP/HOTP)
- **HsmAuth** (0x100): YubiHSM Auth
- **Fido2** (0x200): FIDO2 (CTAP2)

### Transports

Capabilities can be configured separately for each transport:

- **USB**: Over USB connection (all YubiKeys)
- **NFC**: Over NFC connection (NFC-enabled YubiKeys only)

### Form Factors

YubiKeys come in different physical form factors:

- `UsbAKeychain`: USB-A keychain form factor (YubiKey 5)
- `UsbANano`: USB-A nano form factor (YubiKey 5 Nano)
- `UsbCKeychain`: USB-C keychain form factor (YubiKey 5C)
- `UsbCNano`: USB-C nano form factor (YubiKey 5C Nano)
- `UsbCLightning`: USB-C + Lightning (YubiKey 5Ci)
- `UsbABiometricKeychain`: USB-A with fingerprint sensor (YubiKey Bio)
- `UsbCBiometricKeychain`: USB-C with fingerprint sensor (YubiKey Bio C)

### Device Flags

- `FlagEject` (0x80): Auto-eject in CCID-only mode
- `FlagRemoteWakeup` (0x40): Allow device to wake suspended host

### Configuration Locking

Configuration can be protected with a 16-byte lock code. Once locked:
- Configuration changes require the lock code
- Unlocking requires the current lock code
- The lock code can be changed with the current code

## Core API

### IYubiKey Extension Methods

The module provides convenience extension methods on `IYubiKey` for common operations:

```csharp
using Yubico.YubiKit.Management;

// Quick device info (creates session automatically)
var deviceInfo = await yubiKey.GetDeviceInfoAsync(cancellationToken);

// Quick configuration change (creates session automatically)
await yubiKey.SetDeviceConfigAsync(
    config,
    reboot: true,
    cancellationToken: cancellationToken);

// Manual session management (for multiple operations)
using var mgmtSession = await yubiKey.CreateManagementSessionAsync(
    cancellationToken: cancellationToken);
```

**When to use:**
- **Extension methods**: Single operations (query device info, apply one config)
- **Manual session**: Multiple operations, batch queries, custom lifecycle control

### Creating a Session

```csharp
using Yubico.YubiKit.Management;

// Using IYubiKey extension (recommended for single operations)
using var mgmtSession = await yubiKey.CreateManagementSessionAsync(
    cancellationToken: cancellationToken);

// Or manually over SmartCard (CCID/NFC)
using var connection = await yubiKey.ConnectAsync<ISmartCardConnection>();
using var mgmtSession = await ManagementSession.CreateAsync(
    connection,
    cancellationToken: cancellationToken);

// Or manually over HID (FIDO interface)
using var connection = await yubiKey.ConnectAsync<IFidoConnection>();
using var mgmtSession = await ManagementSession.CreateAsync(
    connection,
    cancellationToken: cancellationToken);

// With SCP03 authentication (SmartCard only)
using var connection = await yubiKey.ConnectAsync<ISmartCardConnection>();
using var mgmtSession = await ManagementSession.CreateAsync(
    connection,
    scpKeyParams: Scp03KeyParameters.Default,
    cancellationToken: cancellationToken);
```

### Getting Device Information

```csharp
var deviceInfo = await mgmtSession.GetDeviceInfoAsync(cancellationToken);

Console.WriteLine($"Serial: {deviceInfo.SerialNumber}");
Console.WriteLine($"Firmware: {deviceInfo.FirmwareVersion}");
Console.WriteLine($"Form Factor: {deviceInfo.FormFactor}");
Console.WriteLine($"USB Enabled: {deviceInfo.UsbEnabled}");
Console.WriteLine($"NFC Enabled: {deviceInfo.NfcEnabled}");
Console.WriteLine($"FIPS: {deviceInfo.IsFips}");
Console.WriteLine($"Locked: {deviceInfo.IsLocked}");
```

### Managing Capabilities

```csharp
// Get current device info
var deviceInfo = await mgmtSession.GetDeviceInfoAsync(cancellationToken);

// Enable PIV and OATH over USB, disable others
var usbCapabilities = DeviceCapabilities.Piv | DeviceCapabilities.Oath;
var config = new DeviceConfig
{
    EnabledCapabilities = new Dictionary<Transport, int>
    {
        { Transport.Usb, (int)usbCapabilities }
    }
};

await mgmtSession.SetDeviceConfigAsync(
    config,
    reboot: true, // Device will reboot to apply changes
    cancellationToken: cancellationToken);

// After reboot, need to re-enumerate device
await Task.Delay(3000); // Wait for reboot
var updatedYubiKey = YubiKeyDevice.FindBySerialNumber(deviceInfo.SerialNumber.Value);
```

### Configuration Locking

```csharp
// Lock configuration with a new lock code
byte[] lockCode = new byte[16];
RandomNumberGenerator.Fill(lockCode);

var config = new DeviceConfig
{
    EnabledCapabilities = new Dictionary<Transport, int>() // No changes
};

await mgmtSession.SetDeviceConfigAsync(
    config,
    reboot: false,
    newLockCode: lockCode,
    cancellationToken: cancellationToken);

// Later, modify configuration with lock code
var newConfig = new DeviceConfig
{
    EnabledCapabilities = new Dictionary<Transport, int>
    {
        { Transport.Usb, (int)(DeviceCapabilities.Piv | DeviceCapabilities.Oath) }
    }
};

await mgmtSession.SetDeviceConfigAsync(
    newConfig,
    reboot: true,
    currentLockCode: lockCode,
    cancellationToken: cancellationToken);
```

### Device Reset (Firmware 5.6+)

```csharp
// Factory reset the device
await mgmtSession.ResetDeviceAsync(cancellationToken);
// WARNING: This permanently deletes all data on the device
```

## Project Structure

```
Yubico.YubiKit.Management/
├── src/
│   ├── ManagementSession.cs           # Main session class
│   ├── DeviceInfo.cs                  # Device information model
│   ├── DeviceConfig.cs                # Configuration model
│   ├── DeviceCapabilities.cs          # Capability flags enum
│   ├── DeviceFlags.cs                 # Device flags enum
│   ├── FormFactor.cs                  # Form factor enum
│   ├── VersionQualifier.cs            # Firmware version qualifier
│   ├── IYubiKeyExtensions.cs          # Convenience extensions
│   ├── DependencyInjection.cs         # DI support
│   └── Yubico.YubiKit.Management.csproj
└── tests/
    ├── Yubico.YubiKit.Management.IntegrationTests/
    │   ├── ManagementIntegrationTests.cs
    │   ├── AdvancedManagementTests.cs
    │   └── ManagementTests.cs
    └── Yubico.YubiKit.Management.UnitTests/
        ├── CapabilityMapperTests.cs
        └── FirmwareVersionTests.cs
```

## Common Use Cases

### 1. Query Device Capabilities

```csharp
var deviceInfo = await mgmtSession.GetDeviceInfoAsync(cancellationToken);

// Check what's available
bool hasPiv = (deviceInfo.UsbSupported & DeviceCapabilities.Piv) != 0;
bool hasOath = (deviceInfo.NfcSupported & DeviceCapabilities.Oath) != 0;

// Check what's enabled
bool pivEnabled = (deviceInfo.UsbEnabled & DeviceCapabilities.Piv) != 0;
bool oathEnabled = (deviceInfo.NfcEnabled & DeviceCapabilities.Oath) != 0;
```

### 2. Disable NFC for Security

```csharp
var deviceInfo = await mgmtSession.GetDeviceInfoAsync(cancellationToken);

// Disable all NFC capabilities
var config = new DeviceConfig
{
    EnabledCapabilities = new Dictionary<Transport, int>
    {
        { Transport.Nfc, (int)DeviceCapabilities.None }
    }
};

await mgmtSession.SetDeviceConfigAsync(config, reboot: true, cancellationToken: cancellationToken);
```

### 3. Configure Auto-Eject Timeout

```csharp
// Set 30-second auto-eject timeout for CCID-only mode
var config = new DeviceConfig
{
    EnabledCapabilities = new Dictionary<Transport, int>(),
    AutoEjectTimeout = 30,
    DeviceFlags = DeviceConfig.FlagEject
};

await mgmtSession.SetDeviceConfigAsync(config, reboot: true, cancellationToken: cancellationToken);
```

### 4. Restrict NFC (Firmware 5.7+)

```csharp
// Disable NFC temporarily (can be re-enabled)
var config = new DeviceConfig
{
    EnabledCapabilities = new Dictionary<Transport, int>(),
    NfcRestricted = true
};

await mgmtSession.SetDeviceConfigAsync(config, reboot: false, cancellationToken: cancellationToken);
```

### 5. Check FIPS Status

```csharp
var deviceInfo = await mgmtSession.GetDeviceInfoAsync(cancellationToken);

if (deviceInfo.IsFips)
{
    Console.WriteLine("FIPS Series YubiKey");
    Console.WriteLine($"FIPS Capable: {deviceInfo.FipsCapabilities}");
    Console.WriteLine($"FIPS Approved: {deviceInfo.FipsApproved}");
}
```

## Important Notes

### Device Reboot

Configuration changes that enable/disable capabilities require a device reboot:
- YubiKey will disconnect and reconnect
- Application needs to re-enumerate the device after ~3 seconds
- All active sessions are terminated during reboot

### Capability Restrictions

- At least one USB capability must be enabled
- Cannot disable Management application itself
- Some capabilities are not available on all YubiKey models
- FIPS-approved mode limits some configuration options

### Transport Differences

- USB: All capabilities typically available
- NFC: Not all YubiKey models have NFC
- Some applications work better over specific transports

### Lock Code Security

- Lock codes must be exactly 16 bytes
- Store lock codes securely (cannot be recovered if lost)
- Locked configuration can only be changed/unlocked with the correct code
- No factory reset option for locked configuration on firmware <5.6

## Testing Guidance

See [CLAUDE.md](CLAUDE.md) for detailed test infrastructure information, including the powerful `[WithYubiKey]` attribute system for declarative device filtering.

## References

- **YubiKey Manager**: https://developers.yubico.com/yubikey-manager/
- **YubiKey Management Commands**: https://developers.yubico.com/yubikey-manager/Config_Reference.html
- **YubiKey Capabilities**: https://developers.yubico.com/Software_Projects/YubiKey_Personalization_Manager/
