# Yubico.YubiKit.Core

Core foundational library for the Yubico.NET.SDK. This module provides device management, connection abstractions, protocol handling, and platform interop for all YubiKey applications.

## Overview

Yubico.YubiKit.Core is the foundation that all other SDK modules build upon. It handles the low-level details of communicating with YubiKey devices across different transport types (SmartCard/PC/SC and HID) and operating systems (Windows, macOS, Linux).

**Key Capabilities:**
- 🔍 **Device Discovery** - Automatic detection and monitoring of connected YubiKeys
- 🔌 **Connection Management** - Unified abstraction over SmartCard (PC/SC) and HID transports
- 📡 **Protocol Handling** - ISO 7816-4 APDU processing with automatic command chaining
- 🔐 **Secure Channel Protocol (SCP)** - SCP03, SCP11a/b/c support for secure communication
- 🖥️ **Platform Interop** - Cross-platform native library loading and device enumeration
- 🧾 **Device Metadata Models** - Read-only `DeviceInfo`, capability, form-factor, flag, and version qualifier types
- 🛠️ **Utilities** - TLV processing, cryptographic key types, COSE encoding

## Installation

```bash
dotnet add package Yubico.YubiKit.Core
```

This package is automatically included when you install any application-specific package (PIV, FIDO2, etc.).

## Quick Start

### Device Discovery

An `IYubiKey` represents **one physical YubiKey** (which may expose several interfaces — CCID, HID FIDO,
HID OTP — at once), not a single transport handle. See [Physical Device Model](../../docs/architecture/physical-device-model.md).
HID interface enumeration is implemented on macOS, Linux, and Windows. Exact composite-grouping
guarantees and conservative split cases are documented in
[Device Discovery Guarantees](../../docs/architecture/device-discovery-guarantees.md).

```csharp
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;

// One IYubiKey per physical device, even when several interfaces are present.
var devices = await YubiKeyManager.FindAllAsync();

foreach (var device in devices)
{
    Console.WriteLine($"{device.DeviceId}: {device.AvailableConnections}");
}

// Force a rescan when device topology may have changed
var freshDevices = await YubiKeyManager.FindAllAsync(forceRescan: true);

// Filter discovery. ConnectionType.Hid includes HID FIDO and HID OTP interfaces.
var hidDevices = await YubiKeyManager.FindAllAsync(ConnectionType.Hid);
var fidoDevices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
```

### Device Monitoring

`YubiKeyManager.StartMonitoring()` starts platform listeners and performs an initial repository rescan. Listener
notifications are only rescan hints; public `YubiKeyManager.DeviceChanges` events are emitted after discovery
updates the repository and computes an `Added` or `Removed` diff. This means an OS-level HID notification does
not by itself mean a public YubiKey device was added or removed.

### Opening a Connection

Open a specific interface with the typed overload. The parameterless `ConnectAsync()` is only for
single-interface devices; on a composite device it throws rather than guessing a transport. Applet session
extensions (e.g. `CreateManagementSessionAsync`) select a transport via a documented default order plus an
optional `preferredConnection` override — see [Physical Device Model](../../docs/architecture/physical-device-model.md).

```csharp
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

// Open SmartCard connection
await using var smartCardConnection = await device.ConnectAsync<ISmartCardConnection>();

// Open HID FIDO connection
await using var fidoConnection = await device.ConnectAsync<IFidoHidConnection>();

// Open HID OTP connection
await using var otpConnection = await device.ConnectAsync<IOtpHidConnection>();
```

### Protocol Communication

```csharp
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;

// Create protocol from connection
using ISmartCardProtocol protocol = ProtocolFactory.Create(smartCardConnection);

// Select an application (e.g., PIV)
await protocol.SelectAsync(ApplicationIds.Piv, cancellationToken);

// Configure for firmware version
protocol.Configure(firmwareVersion);

// Send APDU commands
var command = new ApduCommand
{
    Cla = 0x00,
    Ins = 0xA4,  // SELECT
    P1 = 0x04,
    P2 = 0x00,
    Data = applicationId
};

var responseData = await protocol.TransmitAndReceiveAsync(command, cancellationToken: cancellationToken);
```

### Secure Channel Protocol (SCP)

```csharp
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;

// Core supplies the key-parameter types consumed by SCP-capable session factories.
var staticKeys = new StaticKeys(
    keyRef: 0x01,
    encKey: encKeyBytes,
    macKey: macKeyBytes,
    dekKey: dekKeyBytes
);

var scp03Params = new Scp03KeyParameters(keyRef, staticKeys);

// Always zero sensitive key material
staticKeys.Dispose();
```

SCP establishment is an internal `PcscProtocol` capability. Session factories own protocol
configuration, channel establishment, and cleanup; `PcscProtocolScp` cannot be constructed by consumers.

### TLV Processing

```csharp
using Yubico.YubiKit.Core.Utilities;

// Parse TLV data
var tlvs = TlvHelper.ParseMany(responseData);
var certificateTlv = tlvs.FirstOrDefault(t => t.Tag == 0x53);

// Build TLV structure
using var builder = new TlvBuilder();
builder.Add(0x5C, new byte[] { 0x5F, 0xC1, 0x02 });  // Tag list
builder.Add(0x53, certificateData);  // Certificate
var encodedData = builder.ToArray();

// Nested TLV
using var nestedBuilder = new TlvBuilder();
using (var nested = nestedBuilder.AddNested(0x7F49))  // Public key template
{
    nested.Add(0x81, modulusBytes);   // RSA modulus
    nested.Add(0x82, exponentBytes);  // RSA exponent
}
```

## Architecture

### Connection Abstraction

A physical `IYubiKey` exposes one or more concrete interfaces; a typed `ConnectAsync<TConnection>()` routes
to the requested interface.

```
IYubiKey (one physical device)
    │  AvailableConnections / SupportsConnection(...)
    ↓  ConnectAsync<TConnection>()
IConnection
    ├── ISmartCardConnection (PC/SC)
    ├── IFidoHidConnection (HID FIDO)
    └── IOtpHidConnection (HID OTP)
```

### APDU Processing Pipeline

```
ApduCommand
    ↓
[ChainedApduTransmitter]         ← Splits large commands
    ↓
[ApduFormatterShort/Extended]    ← Formats for wire protocol
    ↓
ISmartCardConnection
    ↓
[ChainedResponseReceiver]        ← Reassembles responses
    ↓
ApduResponse
```

### Concurrency

- **Sessions/protocols are safe for concurrent calls, executed sequentially.** SmartCard (APDU/SCP), FIDO HID, and OTP HID protocols serialize full logical exchanges internally, so concurrent operations on one session never interleave packets on the wire. Cancellation tokens cancel only the wait for a turn — an exchange in flight runs to completion.
- **Discovery never disturbs open sessions.** Per-interface connection leases and a nonblocking exclusive discovery lease make ownership atomic: a connection owns the interface before physical connect, discovery skips interfaces with a live connection, and a connect cannot cross a Management metadata read already in progress. CCID, FIDO HID, and OTP HID each admit one live connection per physical interface. CCID, FIDO, and OTP remain independent transports; Management can fall back across free interfaces when an earlier candidate is held.
- **One live session per connection.** A second session is refused before any wire operation. Sequential reuse is supported: dispose session A, then create session B over the same caller-owned connection.
- **Connection ownership follows creation.** A direct `Session.CreateAsync(connection)` borrows the connection and does not dispose it. A `device.Create<App>SessionAsync()` convenience method owns its hidden connection and closes it with the returned session. Always use `await using`; leaking a caller-created connection can retain an exclusive CCID, FIDO HID, or OTP HID lease and block later opens. There is no finalizer backstop.
- **Discovery work is bounded independently from caller waits.** Each caller has its own timeout/cancellation, while repeated scans share at most one underlying read per stable interface and connection type. Completion removes the single-flight entry for later retry; a permanently hung native call remains one operation rather than accumulating one operation per scan.
- **Monitor hints are bounded occurrence signals.** Concurrent HID/SmartCard callbacks share one capacity-one wake-up signal; storms cannot build a payload queue, while quiet-period debounce, maximum coalescing, and periodic fallback scans remain intact. HID and SmartCard listeners start independently as best-effort latency accelerators; unavailable listeners are cleaned up without aborting monitoring, which can fall back to interval-only rescans.
- **A monitor generation may do anything except publish stale truth.** Monitor lifecycle is an epoch model, not a state machine: each `StartMonitoring` creates an immutable generation that the loop, manual rescans, and listener callbacks capture once. Every device-snapshot publication is mutually exclusive under one never-disposed gate and is admitted only if its generation is still current, so a scan hung in native I/O can return long after its generation was retired and simply be discarded. Start, stop, and dispose take only a small state lock, so a blocking `DeviceChanges` subscriber cannot wedge them, and restart after an abandoned stop always succeeds. Dispose drains in-flight publication with a bounded timeout; a publication that outlives the bound may complete afterwards, which the manager's repository disposal silences.
- **Connections are disposed exactly once, and disposal means disposed.** The registered-connection wrappers run teardown through a one-shot gate: the first caller disposes the inner connection and then releases the ownership lease, and every other caller — sync or async, concurrent or later — observes that same completion. Any disposal call returning therefore implies teardown finished, so a caller cannot reopen an interface whose physical handle is still closing.

- **Composite grouping is an evidence hierarchy, not a guess.** Interfaces of one physical key are grouped by USB topology (Windows), then serial, then PID completeness, then pigeonhole deduction, falling back to publishing an interface on its own rather than guessing. What this guarantees per platform - including two cases it deliberately cannot solve on macOS and Linux - is documented in [Device Discovery Guarantees](../../docs/architecture/device-discovery-guarantees.md).

### Platform Support

The Core module provides platform-specific implementations for:
- **Windows**: HidD, Cfgmgr32, WinSCard APIs
- **macOS**: IOKit, CoreFoundation, PC/SC
- **Linux**: udev, libpcsclite

Platform detection is automatic via `SdkPlatformInfo.OperatingSystem`.

## Key Classes

| Class | Purpose |
|-------|---------|
| `YubiKeyManager` | Static entry point for YubiKey discovery and cache management |
| `IYubiKey` | Represents a physical or virtual YubiKey device |
| `ISmartCardConnection` | SmartCard (PC/SC) transport connection |
| `IFidoHidConnection` | HID FIDO transport connection |
| `IOtpHidConnection` | HID OTP transport connection |
| `PcscProtocol` | ISO 7816-4 APDU protocol implementation |
| `ApduCommand` / `ApduResponse` | APDU command/response representations |
| `ScpProtocol` | Secure Channel Protocol wrapper (SCP03, SCP11) |
| `TlvHelper` / `TlvBuilder` | TLV parsing and construction utilities |
| `ApplicationSession` | Base class for application-specific sessions |

## Logging

Configure logging at application startup:

```csharp
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;

YubiKitLogging.LoggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

With dependency injection, configure YubiKit logging from the DI-provided logger factory during startup:

```csharp
services.AddLogging(builder => builder.AddConsole());

using var provider = services.BuildServiceProvider();
YubiKitLogging.Configure(provider.GetRequiredService<ILoggerFactory>());
```

## Firmware Version Considerations

Different YubiKey firmware versions have different capabilities:

```csharp
// Check firmware version
if (firmwareVersion.IsAtLeast(FirmwareVersion.V4_0_0))
{
    // Extended APDUs supported (up to 2048 bytes)
}

if (firmwareVersion.IsAtLeast(FirmwareVersion.V5_3_0))
{
    // SCP03 available
}

if (firmwareVersion.IsAtLeast(FirmwareVersion.V5_7_2))
{
    // SCP11 protocols available
}
```

## Security Considerations

- **Key Zeroing**: Always zero sensitive key material with `CryptographicOperations.ZeroMemory()` or dispose `StaticKeys`
- **Connection Lifetime**: Don't share connections across threads without synchronization
- **SCP Keys**: Store SCP keys securely; never log or persist them unencrypted
- **APDU Logging**: Disable trace logging in production to avoid leaking sensitive APDUs

## Related Modules

- **[Yubico.YubiKit.Management](../Management/)** - Device information and capability queries
- **[Yubico.YubiKit.Piv](../Piv/)** - PIV smart card operations
- **[Yubico.YubiKit.Fido2](../Fido2/)** - FIDO2/WebAuthn authentication

## Developer Documentation

For in-depth patterns, test infrastructure, and implementation details, see [CLAUDE.md](CLAUDE.md).

For the physical-device model (one `IYubiKey` per physical key, metadata ownership, applet transport
selection, session/connection ownership, and migration from per-interface handles), see
[Physical Device Model](../../docs/architecture/physical-device-model.md).
