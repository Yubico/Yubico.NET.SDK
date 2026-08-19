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
├── Abstractions/        # Public device/connection contracts; internal protocol contracts
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

If a change touches Core runtime loops, polling paths, recovery logic, or listener lifecycle cleanup, run `dotnet toolchain.cs -- resilience --fast` in addition to the normal focused tests. Prefer adding or extending no-hardware `Category=RuntimeResilience` coverage before considering live diagnostics.

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

Core owns SCP key-parameter types, handshake processing, and the secure PC/SC protocol decorator.
Applet session factories accept the key parameters and establish the channel through the internal
`PcscProtocol.InitializeScpAsync` capability; consumers cannot construct the decorator directly.

```csharp
// SCP03 - Symmetric keys
var scp03Params = new Scp03KeyParameters(keyRef, staticKeys);

// SCP11b - Public key only (YubiKey authenticates to host)
var scp11Params = new Scp11KeyParameters(keyRef, sdPublicKey);

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
to select an interface; the parameterless `ConnectAsync()` throws on a multi-interface device. One grouped
physical key admits one live connection across all known interfaces. Applet session extensions choose exactly
one transport via a documented default order plus an optional `preferredConnection` override; connection or
initialization failure does not try another interface. Read-only metadata types (`DeviceInfo`, `FormFactor`,
`DeviceCapabilities`, `DeviceFlags`, `VersionQualifier`, `VersionQualifierType`) are Core-owned
(`Yubico.YubiKit.Core.Devices`); mutating operations stay in Management. Full reference:
[Physical Device Model](../../docs/architecture/physical-device-model.md). What grouping of a physical key's interfaces does and does not guarantee - per platform, with every guarantee pinned to a named test - is in [Device Discovery Guarantees](../../docs/architecture/device-discovery-guarantees.md). Read it before changing `CompositeDeviceMerger`, `FindYubiKeys`, or the topology resolver: the merge tiers are an evidence hierarchy that never guesses, and several conservative-looking outcomes are documented bounds with pinning tests, not defects.

### ConnectionType Semantics

`ConnectionType` is a `[Flags]` enum with explicit values. `HidFido`, `HidOtp`, and `SmartCard` represent concrete discovered device interfaces. `Hid` is a group filter that includes both HID FIDO and HID OTP interfaces when used with discovery/cache filtering APIs. `Unknown` matches no devices.

### Listener Event Semantics

HID listeners expose typed `HidDeviceRescanHint` callbacks. These hints are diagnostic only and are never public physical-device truth. `YubiKeyManager.DeviceChanges` must remain repository-diffed output after a rescan. Unknown HID removals still trigger a rescan fallback rather than being suppressed, because the removed interface may be the only native signal for a physical-device diff.

`YubiKeyDeviceMonitorService` logs hint details at ingress and carries only a capacity-one occurrence signal into its single-reader loop; payloads are not queued. Startup is best-effort and degrades gracefully: each listener is started independently, and a listener that throws or reports a post-`Start()` status other than `Started` is logged, individually stopped/disposed, and skipped — it never aborts the other listener or the monitoring loop. Monitoring always starts (worst case, with no listeners at all, it relies solely on the interval fallback rescan), because device truth comes from the full `FindAllAsync` + repository diff, not from listeners. When a transport's *service* is simply unavailable (for example, no PC/SC service, no PC/SC native library, or no readers), that transport enumerates to empty and the other transports are still scanned and diffed every interval, so a transport whose listener is unavailable is still detected. This mirrors canonical yubikit (Rust/Python), where a transport that fails to enumerate is skipped and discovery continues with the others. Listeners are therefore optional latency accelerators, not correctness dependencies. One narrower case is not yet fully isolated at the scan layer: if PC/SC *enumeration itself throws* (discovery-worker saturation, or an `SCardGetStatusChange` error), `FindYubiKeys.FindAllAsync` aborts that single scan before HID is enumerated — deliberately, so a failed PC/SC probe is never committed as a false-empty snapshot that would emit spurious removals — and the monitor's `RescanSafelyAsync` retries on the next interval. Making HID still enumerate when PC/SC enumeration throws (per-transport scan isolation, without reintroducing false removals) is tracked for the polling-migration follow-up. `StartMonitoring` does not throw for listener unavailability (only for an invalid interval or a disposed service). Individually failed listeners are still cleaned up so no partial resources leak, and each listener callback captures its attempt's signal so a detached/stale callback cannot enqueue into a later monitoring run. The loop consumes one occurrence per wake-up before checking debounce/max-coalesce time, so continuously refilled signals cannot starve the deadline check.

## Session Base Class

`ApplicationSession` centralizes shared session state:
- `FirmwareVersion`
- `IsInitialized`
- `IsAuthenticated`
- `Protocol` lifetime/disposal
- One-live-session attachment to the borrowed `Connection`

`ApplicationSession` does not dispose a caller-created connection. Only internal convenience entry
points that created a hidden connection call `OwnConnection()`. Whoever creates a connection must use
`await using` to dispose it; otherwise the physical-device lease can remain held for the
connection/process lifetime and block later opens. There is no finalizer backstop. Dispose one session
before creating the next session over the same connection; sequential reuse is supported.

`IsInitialized` and `IsAuthenticated` become `false` when sync or async disposal is admitted, not
only after teardown completes. `IsAuthenticated` describes application-protocol authentication (for
example, SCP); applet-specific authentication is exposed by the concrete applet session.

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
    [SkippableTheory]
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

### Creating a Raw Session

```csharp
// From a caller-owned connection
await using var connection = await connectionFactory.CreateAsync(reader, ct);
await using RawSmartCardSession raw = await RawSmartCardSession.CreateAsync(connection, ct);

// Select application
await raw.SelectAsync(ApplicationIds.Piv, ct);

// Configure for firmware
raw.Configure(firmwareVersion);
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
var responseData = await raw.TransmitAndReceiveAsync(command, cancellationToken: ct);

// Sensitive command (PIN, key material) — caller zeroes source buffer after transmission
// ApduCommand is a readonly record struct: it stores a reference, not a clone.
var command = new ApduCommand(0x00, InsVerify, 0x00, 0x80, pinnedPin.AsMemory(0, 8));
var response = await raw.TransmitAndReceiveAsync(command, cancellationToken: ct);
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

## Concurrency Model

Behavior added by the discovery/session concurrency hardening (see `ExchangeGuard`, `DeviceConnectionRegistry`):

- **Protocols refuse overlapping logical exchanges.** `PcscProtocol` (+ SCP wrapper), `FidoHidProtocol`, and `OtpHidProtocol` run each full logical exchange through `ExchangeGuard`. A second operation throws `InvalidOperationException` immediately; sequential awaited operations are unchanged.
- **Cancellation is checked at entry only.** Once an exchange claims the guard, constituent transmits use `CancellationToken.None` so chained APDU, CTAP/OTP frame, and SCP state cannot be stranded.
- **The guard is protocol-instance scoped.** An SCP wrapper shares its base PC/SC guard, but independently created raw protocol instances over one connection are not coordinated. The supported contract prevents that shape by admitting one `ApplicationSession` per connection; connection-wide raw protocol ownership is a separate API decision.
- **Discovery/connection ownership is atomic.** A connection to a grouped physical key claims every known stable member interface ID before native open. A second connection through any member throws `ConnectionInUseException` immediately. Claims are sorted, deduplicated, rolled back on failure, and released only after physical teardown. Standalone records use one-element scopes when discovery cannot prove grouping. Discovery remains per-interface and nonblocking; connections may wait cancellably for active discovery, while waiting connections retain priority. One level down, `ConnectionSessionGuard` allows one live `ApplicationSession` per connection and supports sequential reuse. In-process only — cross-process contention is not covered.
- **Discovery reads are time-bounded and single-flight.** Identity reads: 2s/attempt; composite metadata: 3s budget. The budget bounds each caller's wait, while one underlying read per stable interface/`ConnectionType` continues independently. A hung native call is reused by later scans rather than multiplied; completion removes the single-flight entry so faults and cancellations can be retried. Cached identity expires with the hardware and the configuration, not only with scan-observed absence: the monitor forwards every listener event to `IFindYubiKeys.NotifyTransportActivity`, which discards that transport's cached identities (a same-slot swap between scans reuses the interface id and would otherwise attribute the departed key's serial to its successor), and each entry records the PID observed at read time so a hit under a different PID is a miss. Without monitoring running there are no listener events and staleness detection degrades to scan-observed absence — see the identity-cache section of `docs/architecture/device-discovery-guarantees.md`.
- **Monitor lifecycle is an epoch model, not a state machine.** Each `StartMonitoring` builds an immutable `MonitorGeneration` (`{ Id, ScanGate, Signal, Cts }`) held in one field; the loop, manual rescans, and listener callbacks capture that reference once, so a torn gate/generation pair is not representable. Publication is where safety is enforced: all publications from all generations are mutually exclusive under the never-disposed `_publishGate`, held across the admission check and `UpdateCache`, and a snapshot is admitted only if its generation is still current and the service undisposed. Superseded snapshots — including a scan hung in native I/O that returns long after its generation was retired — are discarded. Because publications never interleave, a successor's snapshot is serialized strictly after any in-flight predecessor's, so newer truth always lands last. Lifecycle operations take only the small `_publishLock`, never `_publishGate`, so a blocking `DeviceChanges` subscriber cannot wedge start/stop/dispose, and restart after an abandoned stop always succeeds. Nothing disposes a semaphore anyone can still acquire: scan gates live in their generation and are never disposed, and an abandoned generation is unreachable garbage. `DisposeAsync` drains `_publishGate` with the shutdown bound and, on timeout, warns and abandons — a publication already admitted may then complete after `DisposeAsync` returns, which the manager's subsequent repository disposal silences. That is a documented contract, not an accident. When editing this file, keep the three primitives one-job-each; the design collapsed a four-concept state machine and re-merging their responsibilities is what previously produced the races.
  Safety is not liveness: after abandoning a hung scan, discovery *liveness* is owned by `FindYubiKeys` (its `_scanLock` wait takes the loop's token, so blocked generations do not accumulate) and recovers when the upstream time-bounds release. The epoch model neither causes nor cures a PC/SC enumeration hang.
- **Registered connections dispose exactly once, and disposal implies disposed.** `DisposalGate` gives the first `Dispose`/`DisposeAsync` caller the claim via one atomic compare-exchange; it disposes the inner connection and then releases the registry lease in a `finally`, publishing its completion. Every other caller observes that same completion — async callers await it, sync callers block on it — so any disposal call returning means teardown actually finished and all callers see the same outcome, including the same exception instance. A caller can therefore never reopen an interface whose physical handle is still being torn down.

## Known Gotchas

1. **APDU Size Limits**: YubiKey Neo uses 254-byte max; YubiKey 4+ uses extended APDUs up to 2048 bytes
2. **Connection Ownership**: whoever CREATES a connection disposes it with `await using`. Protocols and sessions are pure users — `PcscProtocol`, `FidoHidProtocol`, `OtpHidProtocol`, and direct `Session.CreateAsync(connection)` never dispose a connection they were handed. The one exception is deliberate and internal: an `IYubiKey.Create<App>SessionAsync` convenience entry point opens a connection the caller never sees, so it calls `ApplicationSession.OwnConnection()` to hand that connection's lifetime to the session it returns. A leaked connection can retain the physical-device lease and block later opens; no finalizer releases it
3. **SCP Key Zeroing**: Always zero SCP keys after use; `StaticKeys` implements `IDisposable`
4. **TLV Disposal**: `TlvBuilder` and `DisposableTlvList` must be disposed
5. **Platform-Specific Behavior**: PC/SC APIs behave differently across platforms; test on all three
6. **Chained Response Assembly**: `INS_SEND_REMAINING` (0xC0) is used by default; some apps use custom values
7. **Access Tiers**: Applet sessions are the golden path. Raw sessions bypass applet checks but retain session ownership and overlap guards. Raw `IConnection` calls bypass both session and exchange guards; do not interleave them, and dispose/reopen after an interrupted exchange. See [Raw Access Tiers](../../docs/architecture/raw-access-tiers.md)

## Related Modules

- **Yubico.YubiKit.Management** - Uses Core for device info queries
- **Yubico.YubiKit.Fido2** - Uses Core's HID and cryptography
- **Yubico.YubiKit.Piv** - Uses Core's SmartCard protocol
