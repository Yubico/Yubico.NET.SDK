# CLAUDE.md - Core Module

This file provides module-specific guidance for working in **Yubico.YubiKit.Core**.
For overall repo conventions, see the repository root [CLAUDE.md](../../CLAUDE.md).

## Documentation Maintenance

> **Important:** This documentation is subject to change. When working on this module:
> - **Notable changes** to APIs, patterns, or behavior should be documented in both CLAUDE.md and README.md
> - **New features** (e.g., new protocol handlers, connection types) should include usage examples
> - **Breaking changes** require updates to both files with migration guidance
> - **Test infrastructure changes** should be reflected in the test pattern sections below

## Module Context

Core is the **foundational library** for the entire SDK. It provides:
- **Device Management**: Discovery, monitoring, and lifecycle management
- **Connection Layer**: SmartCard (PC/SC) and HID connection abstractions
- **Protocol Layer**: ISO 7816-4 APDU processing, SCP (Secure Channel Protocol) support
- **Platform Interop**: Cross-platform native library loading (Windows, macOS, Linux)
- **Cryptography**: Key types, COSE encoding, ASN.1 utilities
- **TLV Processing**: Tag-Length-Value parsing and construction

**Key Directories:**
```
src/
├── Abstractions/        # Shared public contracts: IYubiKey, IConnection, IProtocol
├── Devices/             # Physical YubiKey model, discovery, monitoring, metadata
├── Sessions/            # ApplicationSession and ApplicationIds
├── Transports/          # HID and SmartCard device/connection/listener implementations
│   ├── Hid/             # HID transport, platform implementations, keyboard translation
│   └── SmartCard/       # PC/SC transport, device discovery, connection factory
├── Protocols/           # Protocol layers over transports
│   ├── SmartCard/Apdu/  # ISO 7816-4 APDU pipeline
│   ├── SmartCard/Scp/   # Secure Channel Protocol implementations
│   ├── Fido/Hid/        # FIDO/CTAP HID protocol binding
│   └── Otp/Hid/         # OTP HID protocol binding
├── Cryptography/        # Key types, COSE, ASN.1
│   └── Cose/            # COSE key representations
├── Native/              # Native interop per platform
│   ├── Desktop/SCard/   # PC/SC interop
│   ├── Windows/         # Windows-specific (HidD, Cfgmgr32)
│   ├── MacOS/           # macOS-specific (IOKit, CoreFoundation)
│   └── Linux/           # Linux-specific (udev, libc)
├── Credentials/         # Secure credential reading helpers
└── Utilities/           # TLV, CRC, byte, buffer utilities
```

## Logging

Core modules use `Microsoft.Extensions.Logging` via the global `YubiKitLogging.LoggerFactory`.

### Configure Logging

Configure once at application startup:

```csharp
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;

YubiKitLogging.LoggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
```

If using DI, configure logging explicitly from the DI-provided `ILoggerFactory` once during startup: `YubiKitLogging.Configure(serviceProvider.GetRequiredService<ILoggerFactory>())`.

## Critical Patterns

### Listener and Native Retry Loops

Background listeners and native/resource-manager retry loops must block, back off, exit, or throttle on every failure path. Do not ignore native return values inside loops unless another call in the same path provides a bounded wait. Persistent failures such as stale PC/SC handles must have no-hardware fault-injection tests that prove call cadence is backoff-bounded.

### APDU Processing Pipeline

The APDU processing pipeline uses the decorator pattern:

```
ApduCommand
    ↓
[ChainedApduTransmitter]    ← Splits large commands into chained APDUs
    ↓
[ApduFormatterShort/Extended]  ← Formats for wire protocol
    ↓
ISmartCardConnection.TransmitAsync()
    ↓
[ChainedResponseReceiver]   ← Reassembles chained responses
    ↓
ApduResponse
```

**Key classes:**
- `PcscProtocol` - Main protocol implementation (`Protocols/SmartCard/Apdu/PcscProtocol.cs`)
- `ApduCommand` / `ApduResponse` - APDU representations
- `IApduProcessor` - Pipeline element interface
- `ChainedApduTransmitter` / `ChainedResponseReceiver` - Chaining handlers

**Configuration:**
```csharp
// Protocol auto-configures based on firmware version
protocol.Configure(firmwareVersion);

// Force short APDUs (for compatibility testing)
protocol.Configure(firmwareVersion, new ProtocolConfiguration { ForceShortApdus = true });
```

### Secure Channel Protocol (SCP)

SCP wraps the base protocol with encryption/authentication:

```csharp
// SCP03 - Symmetric keys
var scp03Params = new Scp03KeyParameters(keyRef, staticKeys);
var scpProtocol = await protocol.WithScpAsync(scp03Params, cancellationToken);

// SCP11b - Public key only (YubiKey authenticates to host)
var scp11Params = new Scp11KeyParameters(keyRef, sdPublicKey);
var scpProtocol = await protocol.WithScpAsync(scp11Params, cancellationToken);

// SCP11a/c - Mutual authentication with certificates
var scp11Params = new Scp11KeyParameters(keyRef, sdPublicKey, ocePrivateKey, oceKeyRef, certChain);
```

**Key files:**
- `Protocols/SmartCard/Scp/` - SCP implementations
- `Protocols/SmartCard/Scp/SessionKeys.cs` - Derived session keys
- `Protocols/SmartCard/Scp/ScpKid.cs` - Key identifiers

### TLV Processing

Use `TlvHelper` and `Tlv` for parsing/constructing TLV data:

```csharp
// Parsing
var tlvs = TlvHelper.ParseMany(data);
var specificTlv = tlvs.FirstOrDefault(t => t.Tag == 0x9F);

// Construction
using var builder = new TlvBuilder();
builder.Add(0x9F, value);
var encoded = builder.ToArray();

// Nested TLV
using var builder = new TlvBuilder();
using (var nested = builder.AddNested(0xE0))
{
    nested.Add(0x83, kidKvn);
}
```

**Important:** `DisposableTlvList` and `TlvBuilder` must be disposed to avoid memory leaks.

### Platform Interop Pattern

Native methods are isolated in `Native/`:

```csharp
// Platform detection
if (SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows)
{
    // Windows-specific code
}

// Safe handles for native resources
using var handle = new SafeLibraryHandle(libraryPath);

// Platform-specific factory
var scanner = SdkPlatformInfo.OperatingSystem switch
{
    SdkPlatform.Windows => new WindowsDeviceScanner(),
    SdkPlatform.MacOS => new MacOSDeviceScanner(),
    SdkPlatform.Linux => new LinuxDeviceScanner(),
    _ => throw new PlatformNotSupportedException()
};
```

### Connection Factory Pattern

Connections are created via factories:

```csharp
// SmartCard connection
var connectionFactory = new SmartCardConnectionFactory();
using var connection = await connectionFactory.CreateAsync(reader, cancellationToken);

// HID connection
var hidFactory = new HidConnectionFactory();
using var connection = await hidFactory.CreateAsync(device, cancellationToken);
```

### Physical Device Model

`IYubiKey` represents **one physical YubiKey**, not a single transport handle. A composite USB key exposes
several interfaces at once (CCID, HID FIDO, HID OTP), and discovery returns one `IYubiKey` for it with those
interfaces in `AvailableConnections`. Use `SupportsConnection(...)` and the typed `ConnectAsync<TConnection>()`
to select an interface; the parameterless `ConnectAsync()` throws on a multi-interface device. Read-only
metadata types (`DeviceInfo`, `FormFactor`, `DeviceCapabilities`, `DeviceFlags`, `VersionQualifier`,
`VersionQualifierType`) are Core-owned (`Yubico.YubiKit.Core.Devices`); mutating operations stay in
Management. Applet session extensions choose a transport via a documented default order plus an optional
`preferredConnection` override, with held-transport fallback on the default path. Full reference:
[Physical Device Model](../../docs/architecture/physical-device-model.md).

### ConnectionType Semantics

`ConnectionType` is a `[Flags]` enum with explicit values. `HidFido`, `HidOtp`, and `SmartCard` represent concrete discovered device interfaces. `Hid` is a group filter that includes both HID FIDO and HID OTP interfaces when used with discovery/cache filtering APIs. `Unknown` matches no devices.

## Session Base Class

`ApplicationSession` centralizes shared session state:
- `FirmwareVersion`
- `IsInitialized`
- `IsAuthenticated`
- `Protocol` ownership/disposal

Prefer using `IsSupported(feature)` / `EnsureSupports(feature)` on `IApplicationSession` rather than duplicating firmware gates in each module.

## Test Infrastructure

### Unit Test Structure

```
tests/
├── Yubico.YubiKit.Core.UnitTests/
│   ├── Devices/              # YubiKey model, discovery, metadata tests
│   ├── Protocols/
│   │   ├── SmartCard/Apdu/   # APDU protocol tests and fakes
│   │   ├── SmartCard/Scp/    # SCP protocol tests
│   │   └── Otp/Hid/          # OTP HID protocol tests
│   ├── Transports/           # HID and SmartCard transport tests
│   ├── Cryptography/
│   ├── Credentials/
│   └── Utilities/            # TLV, utility tests
└── Yubico.YubiKit.Core.IntegrationTests/
    ├── Devices/              # YubiKeyManager, device tests
    └── Transports/           # HID and SmartCard integration tests
```

### Faking Connections

Use `FakeSmartCardConnection` for unit tests:

```csharp
var fakeConnection = new FakeSmartCardConnection();

// Queue expected responses
fakeConnection.QueueResponse([0x90, 0x00]); // Success
fakeConnection.QueueResponse([0x69, 0x82]); // Security status not satisfied

// Create protocol with fake
var protocol = new PcscProtocol(fakeConnection);

// Test
var result = await protocol.SelectAsync(ApplicationIds.Piv, CancellationToken.None);

// Verify commands sent
Assert.Single(fakeConnection.SentCommands);
```

### Integration Test Base

Integration tests inherit from `IntegrationTestBase`:

```csharp
public class MyTests : IntegrationTestBase
{
    [Theory]
    [WithYubiKey]
    public async Task MyTest_DoesX_Succeeds(YubiKeyTestState state)
    {
        // state.YubiKey is available
        await using var connection = await state.YubiKey.ConnectAsync<ISmartCardConnection>();
        // Test logic
    }
}
```

## Common Operations

### Creating a Protocol

```csharp
// From connection
using var connection = await connectionFactory.CreateAsync(reader, ct);
var protocol = PcscProtocolFactory<ISmartCardConnection>.Create().Create(connection);

// Select application
await protocol.SelectAsync(ApplicationIds.SecurityDomain, ct);

// Configure for firmware
protocol.Configure(firmwareVersion);
```

### Sending APDUs

```csharp
// Non-sensitive command — no zeroing needed
var command = new ApduCommand
{
    Cla = 0x00,
    Ins = 0xA4,
    P1 = 0x04,
    P2 = 0x00,
    Data = applicationId
};
var responseData = await protocol.TransmitAndReceiveAsync(command, ct);

// Sensitive command (PIN, key material) — caller zeroes source buffer after transmission
// ApduCommand is a readonly record struct: it stores a reference, not a clone.
var command = new ApduCommand(0x00, InsVerify, 0x00, 0x80, pinnedPin.AsMemory(0, 8));
var response = await protocol.TransmitAndReceiveAsync(command, ct);
CryptographicOperations.ZeroMemory(pinnedPin); // zeroes what command.Data referenced
```

### Error Handling

```csharp
try
{
    var response = await protocol.TransmitAndReceiveAsync(command, ct);
}
catch (ApduException ex) when (ex.StatusWord == 0x6982)
{
    // Security status not satisfied - need to authenticate
}
catch (ApduException ex) when (ex.StatusWord == 0x6A82)
{
    // Application/file not found
}
```

## Firmware Version Considerations

```csharp
// APDU size limits
if (firmwareVersion.IsAtLeast(FirmwareVersion.V4_0_0))
{
    // Extended APDUs supported
    MaxApduSize = SmartCardMaxApduSizes.Yubikey4;
}
else
{
    // Short APDUs only
    MaxApduSize = SmartCardMaxApduSizes.Neo;
}

// Feature checks
if (firmwareVersion.IsAtLeast(FirmwareVersion.V5_3_0))
{
    // SCP support available
}

if (firmwareVersion.IsAtLeast(FirmwareVersion.V5_7_2))
{
    // SCP11 protocols available
}
```

## Known Gotchas

1. **APDU Size Limits**: YubiKey Neo uses 254-byte max; YubiKey 4+ uses extended APDUs up to 2048 bytes
2. **Protocol Disposal**: `PcscProtocol` disposes its underlying connection - don't dispose both
3. **SCP Key Zeroing**: Always zero SCP keys after use; `StaticKeys` implements `IDisposable`
4. **TLV Disposal**: `TlvBuilder` and `DisposableTlvList` must be disposed
5. **Platform-Specific Behavior**: PC/SC APIs behave differently across platforms; test on all three
6. **Chained Response Assembly**: `INS_SEND_REMAINING` (0xC0) is used by default; some apps use custom values
7. **Connection Sharing**: Don't share connections across threads without synchronization

## Related Modules

- **Yubico.YubiKit.Management** - Uses Core for device info queries
- **Yubico.YubiKit.SecurityDomain** - Uses Core's SCP implementation
- **Yubico.YubiKit.Fido2** - Uses Core's HID and cryptography
- **Yubico.YubiKit.Piv** - Uses Core's SmartCard protocol
