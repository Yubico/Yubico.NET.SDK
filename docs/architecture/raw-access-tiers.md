# Raw Access Tiers

YubiKit exposes three deliberate access tiers. Choose the highest-level tier that can express the operation.
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
  keep-alive handling, final-command correlation, CTAPHID error rejection, and overlap refusal.
- `RawOtpHidSession` supplies OTP feature-report framing, sequencing, polling, outbound CRC generation, and
  overlap refusal. It cannot validate command-specific inbound CRC without caller-supplied response semantics.

They intentionally do **not** select an applet during creation, apply applet-operation feature gates, or interpret
application-specific payloads. Transport and protocol prerequisites still apply; for example, SCP establishment
enforces its minimum firmware version. Bypassing applet checks does not bypass physical transport safety: raw
sessions still participate in the one-live-connection-per-physical-key and one-live-session-per-connection
contracts.

### SmartCard/APDU

```csharp
await using RawSmartCardSession raw =
    await yubiKey.CreateRawSmartCardSessionAsync(cancellationToken);

// Optional when the firmware version is known. Configure before the first APDU.
raw.Configure(firmwareVersion);

await raw.SelectAsync(applicationId, cancellationToken);
ApduResponse response = await raw.TransmitAndReceiveAsync(
    new ApduCommand(cla, ins, p1, p2, commandData),
    throwOnError: false,
    cancellationToken);
```

When `throwOnError` is `false`, inspect `response.Data`, `response.SW1`, and `response.SW2` directly.
The `IYubiKey` extension opens exactly the SmartCard transport and transfers ownership of the hidden connection
to the returned session. Disposing the session therefore disposes that connection.

Use the connection-taking factory when the caller needs to reuse one connection sequentially across sessions:

```csharp
await using ISmartCardConnection connection =
    await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken);

await using (RawSmartCardSession raw =
    await RawSmartCardSession.CreateAsync(connection, cancellationToken))
{
    raw.Configure(firmwareVersion);
    await raw.SelectAsync(applicationId, cancellationToken);
    await raw.TransmitAndReceiveAsync(command, cancellationToken: cancellationToken);
}

// RawSmartCardSession borrowed the connection, so it remains available for the next session.
await using PivSession piv = await PivSession.CreateAsync(connection, cancellationToken);
```

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
request semantics. A final response command must match the request; `CTAPHID_KEEPALIVE` is accepted only while
waiting, and `CTAPHID_ERROR` fails the exchange instead of returning an apparently successful payload.

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
Core generates the outbound frame CRC but returns inbound bytes without command-specific CRC validation. When a
command defines an `expectedLength`, validate its data plus two CRC bytes explicitly:

```csharp
if (response.Length < expectedLength + 2 ||
    !ChecksumUtils.CheckCrc(response.Span, expectedLength + 2))
{
    throw new InvalidOperationException("OTP response CRC validation failed.");
}
```

### Raw SmartCard With SCP

Load SCP parameters from secure application storage; do not embed or log real keys:

```csharp
using ScpKeyParameters scpParameters = LoadScpParametersFromSecureStorage();
await using RawSmartCardSession raw = await yubiKey.CreateRawSmartCardSessionAsync(
    scpParameters,
    firmwareVersion,
    cancellationToken: cancellationToken);

await raw.SelectAsync(applicationId, cancellationToken);
ApduResponse response = await raw.TransmitAndReceiveAsync(command, cancellationToken: cancellationToken);
```

Configuration is applied to the base APDU processor before SCP establishment. An SCP raw session cannot be
reconfigured afterward because replacing or partially updating the established processor graph would invalidate
the secure-channel framing assumptions. The existing Core SCP processor owns secure-channel session material.
The caller retains the normal ownership obligations of the supplied key-parameter type and source key buffers.
Pass a `ProtocolConfiguration` during creation only when the target requires a non-default framing option, such as
forcing short APDUs.

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
- Disposal atomically closes admission, waits for an admitted exchange, and only then disposes protocol/SCP state
  and any convenience-owned connection. New operations are refused as soon as disposal begins.
- Prefer `DisposeAsync`. Synchronous `Dispose` performs the same drain by blocking and must not be invoked from
  inside the operation being drained.

## Sensitive Buffers

FIDO and OTP protocols clear SDK-owned outgoing payload copies, frames, and reports in `finally` after each awaited
send, including failure paths. Caller-owned request memory is never modified. Returned response memory must remain
valid after the method returns and therefore cannot be cleared by the SDK. The caller owns sensitive response-data
handling: retain it only as long as required, avoid logging it, and zero any caller-owned mutable copy after use.

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
