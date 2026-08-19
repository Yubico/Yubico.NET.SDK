# Raw Access Tiers

YubiKit exposes three deliberate access tiers. Choose the highest tier that can express the operation.
Dropping a tier removes SDK guarantees; it does not merely make the API more verbose.

## Tier 0: Applet Sessions

Applet sessions are the supported golden path. They select the application, interpret applet semantics,
validate inputs, enforce firmware and feature gates, and manage protocol state.

```csharp
await using PivSession piv = await yubiKey.CreatePivSessionAsync(
    cancellationToken: cancellationToken);
PivPinMetadata metadata = await piv.GetPinMetadataAsync(cancellationToken);
```

Use Tier 0 unless the operation is undocumented or intentionally below an applet API.

## Tier 1: Raw Sessions

Raw sessions are the supported power-user path:

- `RawSmartCardSession` supplies APDU formatting, command/response chaining, overlap refusal, explicit
  application selection, and optional SCP.
- `RawFidoHidSession` supplies CTAP HID channel allocation, packet framing, continuation packets,
  keep-alive handling, response correlation, and overlap refusal.
- `RawOtpHidSession` supplies OTP feature-report framing, sequencing, polling, CRC handling, and overlap refusal.

They intentionally do **not** select an applet during creation, apply applet feature gates, or interpret
application-specific payloads. Bypassing applet checks does not bypass physical transport safety: raw sessions
still participate in the one-live-connection-per-physical-key and one-live-session-per-connection contracts.

### SmartCard/APDU

```csharp
await using ISmartCardConnection connection =
    await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken);
await using RawSmartCardSession raw =
    await RawSmartCardSession.CreateAsync(connection, cancellationToken);

await raw.SelectAsync(applicationId, cancellationToken);
raw.Configure(firmwareVersion, new ProtocolConfiguration { ForceShortApdus = true });

ApduResponse response = await raw.TransmitAndReceiveAsync(
    new ApduCommand(cla, ins, p1, p2, commandData),
    throwOnError: false,
    cancellationToken);
```

When `throwOnError` is `false`, inspect `response.Data`, `response.SW1`, and `response.SW2` directly.

### FIDO HID

```csharp
await using RawFidoHidSession raw =
    await yubiKey.CreateRawFidoHidSessionAsync(cancellationToken);

ReadOnlyMemory<byte> response = await raw.SendAndReceiveAsync(
    ctapHidCommand,
    payload,
    cancellationToken);
```

The caller supplies and interprets the CTAP HID command payload. This API does not add FIDO2 or WebAuthn
request semantics.

### OTP HID

```csharp
await using RawOtpHidSession raw =
    await yubiKey.CreateRawOtpHidSessionAsync(cancellationToken);

ReadOnlyMemory<byte> response = await raw.SendAndReceiveAsync(
    commandOrSlot,
    payload,
    cancellationToken);
```

The caller supplies and interprets the OTP command payload. This API does not add slot-configuration semantics.

### Raw SmartCard With SCP

Load SCP parameters from secure application storage; do not embed or log real keys:

```csharp
using ScpKeyParameters scpParameters = LoadScpParametersFromSecureStorage();
await using RawSmartCardSession raw = await yubiKey.CreateRawSmartCardSessionAsync(
    scpParameters,
    cancellationToken);

raw.Configure(firmwareVersion);
await raw.SelectAsync(applicationId, cancellationToken);
ApduResponse response = await raw.TransmitAndReceiveAsync(command, cancellationToken: cancellationToken);
```

The existing Core SCP processor establishes and owns secure-channel session material. The caller retains the
normal ownership obligations of the supplied key-parameter type and any source key buffers.

## Tier 2: Raw Connections

`ISmartCardConnection.TransmitAndReceiveAsync`, `IFidoHidConnection.SendAsync` / `ReceiveAsync`, and
`IOtpHidConnection.SendAsync` / `ReceiveAsync` remain public as an expert escape hatch. These methods bypass
`ApplicationSession`, `ConnectionSessionGuard`, and `ExchangeGuard`.

At Tier 2 the caller owns APDU or packet formatting, command chaining, response correlation, CRC validation,
keep-alive handling, sequencing, concurrency exclusion, and recovery from partial or cancelled exchanges.
Never drive a raw connection concurrently with a live session or another raw operation. If traffic is interrupted,
interleaved, or otherwise leaves device state uncertain, dispose the connection and open a new one before continuing.

## Ownership And Sequencing

- A grouped physical YubiKey admits one live connection across its known interfaces.
- One connection admits one live applet or raw session.
- Dispose session N before creating session N+1 over the same caller-owned connection.
- `Raw*Session.CreateAsync(connection)` borrows the connection; the caller disposes both.
- `IYubiKey.CreateRaw*SessionAsync(...)` owns its hidden connection and disposes it with the returned session.
- Overlapping operations on one raw session throw `InvalidOperationException` immediately.
- Once admitted, a stateful exchange runs to completion so cancellation cannot strand protocol state.

## Migration From ProtocolFactory

`ProtocolFactory` and the `IProtocol` family are internal session machinery in v2. Code that previously called
`ProtocolFactory.Create(connection)` should use the corresponding raw session:

```csharp
// Before
using ISmartCardProtocol protocol = ProtocolFactory.Create(connection);
await protocol.SelectAsync(applicationId, cancellationToken);

// After
await using RawSmartCardSession raw = await RawSmartCardSession.CreateAsync(connection, cancellationToken);
await raw.SelectAsync(applicationId, cancellationToken);
```

Use `RawFidoHidSession` or `RawOtpHidSession` for the corresponding HID logical exchanges. Third-party protocol
composition should wrap a raw session rather than injecting or implementing Core protocol internals.
