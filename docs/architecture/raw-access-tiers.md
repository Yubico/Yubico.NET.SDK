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

If a chained command or response continuation fails, the original exception is returned and that protocol
instance refuses further transmissions, selections, and configuration; it does not replay or drain the
unfinished exchange. Dispose and reopen the connection before continuing, including when a raw session borrowed
the connection: replacing only the session over the same connection is not a recovery procedure. The protocol
cannot guard direct raw calls or a different protocol instance using that connection.

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

Once an OTP frame write is attempted, transport errors, cancellation during touch, timeouts, and incomplete
responses trigger one dummy-report abort before another exchange is admitted. A successful abort permits reuse of
that session; an abort or response-completion reset failure makes the protocol instance refuse further operations
without replaying the command. The original exchange error is preserved on abort failure. Pre-wire validation
does not issue an abort. Recovery after a failed reset requires disposal and reopening the connection; a new
session over the same borrowed connection is not a recovery procedure.

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
`IOtpHidConnection.SendAsync` / `ReceiveAsync` remain public for direct raw-connection access. These methods bypass
`ApplicationSession`, `ConnectionSessionGuard`, and `ExchangeGuard`.

At Tier 2 the caller owns APDU or packet formatting, command chaining, response correlation, CRC validation,
keep-alive handling, sequencing, concurrency exclusion, and recovery from partial or cancelled exchanges.
Never drive a raw connection concurrently with a live session or another raw operation. If traffic is interrupted,
interleaved, or otherwise leaves device state uncertain, dispose the connection and open a new one before continuing.

The built-in PC/SC connection enforces the caller-exclusion rule at native admission: a second raw operation is
refused immediately rather than queued. Open, transmit, transaction begin/end, disconnect, and context release use
one connection-lifetime background worker. Cancellation observed before dispatch submits no native call. After
dispatch, the task remains pending until PC/SC returns; only then may the caller reuse or clear command memory.
`DisposeAsync` closes admission promptly and completes after accepted work and checked native cleanup. If cleanup
cannot prove both card disconnect and context release, the physical-interface claim remains quarantined and later
managed opens fail with `UnrecoveredConnectionException`.

### Retained synchronous compatibility paths

Prefer applet sessions or typed raw sessions for normal asynchronous exchanges. Public raw report access
remains an expert blocking path; its method names do not promise caller-thread responsiveness.
`FindHidInterfaces.Create().FindAllAsync(token)` runs its platform
scan via per-call `Task.Run` (an unbounded thread-pool queue, not a bounded scan executor) and can return an
`IHidInterface` backed by `MacOSHidInterface`. Cancellation before dispatch skips enumeration; once dispatched,
the task stays pending through enumeration even if the token is canceled. The discovery task does not make
the resulting report connection asynchronous or cancel an already-running native scan. Built-in typed macOS FIDO
and the direct macOS FIDO report connection share the persistent input-owner implementation; OTP uses its
own migrated route through `HidConnectionSlot`.

| Public method(s) | Execution and owner | Evidence gap / caller limit |
|---|---|---|
| `IHidInterface.ConnectToIOReports()` / `IHidInterface.ConnectToFeatureReports()` | The `MacOSHidInterface.ConnectToIOReports()` / `MacOSHidInterface.ConnectToFeatureReports()` implementations synchronously construct `MacOSHidIOReportConnection` / `MacOSHidFeatureReportConnection`. The caller owns the returned `IHidConnection`. | Direct opens do not take the grouped-key registry claim used by `IYubiKey.ConnectAsync<TConnection>()`; do not infer its quarantine or native-release guarantees. |
| `IHidConnection.GetReport()` / `IHidConnection.SetReport(byte[])` | macOS IO `GetReport` synchronously awaits the persistent input owner's receive with a six-second cancellation timeout; `SetReport` synchronously awaits an owner-worker output using an operation-owned copy and accepts the caller's report length (unlike typed FIDO sends, which require 64 bytes). macOS feature reports synchronously await the OTP owner worker; direct feature `SetReport` accepts arbitrary length, unlike typed eight-byte OTP sends. | No public cancellation token. The IO timeout detaches the reader, not the native owner; late reports remain queued for retry. Native output duration is not bounded. |
| `ISmartCardConnection.BeginTransaction(token)` / returned `IDisposable.Dispose()` | On built-in PC/SC connections, synchronous begin and transaction end block the caller awaiting the connection's native worker. The caller ends the scope before disposing the connection. A held transmit delays synchronous scope disposal until native work exits. | Admitted native acquisition/end has no guaranteed duration. Prefer `BeginTransactionAsync` and async-dispose the built-in scope as in the [Core example](../../src/Core/README.md#send-raw-apdus). The default `BeginTransactionAsync` on an external implementation executes its synchronous `BeginTransaction` **before returning a task**; an async name does not make that implementation nonblocking. An override can provide caller-thread responsiveness, but its native drain behavior depends on that implementation. |
| `IConnection.Dispose()` / `IConnection.DisposeAsync()` | macOS IO report disposal shares the typed FIDO owner's acknowledged shutdown; macOS feature-report disposal shares the OTP worker's checked shutdown and drains accepted GET/SET work without `Task.Run`. Built-in PC/SC `Dispose` blocks for its shared native-worker shutdown outcome: a held transmit delays disconnect and context release; repeat callers share the outcome. | Native shutdown may wait indefinitely for release proof. Never dispose synchronously from work whose completion disposal must await. Custom connections own their own drain and failure semantics. |

macOS IO report reads wake on disposal; native callback state and the physical owner are retained until
acknowledged shutdown. Failed native release retains the owner instead of claiming successful disposal.
The feature-report connection drains an admitted report before release; overlapping raw report calls are
refused. Direct report opens do not acquire a grouped-key claim. The macOS input-owner
behavior does not establish drain guarantees for all public synchronous entry points.

The Core [source-site inventory](../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/README.md)
classifies waits and pre-task-return dispatch, but a listed site is not automatically a public boundary
or a verified drain. The [finite public entry registry](../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/PublicSyncBoundaryRegistry.cs)
independently requires sixteen macOS direct-report, portable SmartCard, listener, manager and scan
entry links from public symbols to their current internal owners. It links existing behavioral tests
where possible; its reflection validation is not a native drain test. The separate
[adapter contract registry](../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/AdapterContractRegistry.cs)
requires 27 operation rows and 49 links to runnable behavior tests, including synchronous
SmartCard begin, scope end and disposal and the external default async-begin fallback. Removing
a required operation or profile role fails validation independently of the JSON rows. Neither
registry certifies custom implementations. The scoped reachability map:

| Public entry / path | Contract and existing evidence | Remaining disposition |
|---|---|---|
| `FindHidInterfaces.Create().FindAllAsync(token)` → platform `GetList()` → `IHidInterface.ConnectToIOReports()` / `ConnectToFeatureReports()` | `FindHidInterfacesBoundaryTests` gates real `FindAllAsync` dispatch with injected platform enumeration: pre-cancellation submits no scan; invocation returns a pending task while enumeration is withheld; cancellation after dispatch does not complete it early; enumeration faults propagate without replay. Returned interfaces expose synchronous constructors. macOS direct report facades have IO/feature compatibility tests and selected-key read-only probes. | This is an enumeration delegate gate, not proof of native handle teardown. `Task.Run` has no per-finder or global worker bound; Windows/Linux direct report teardown and cancellation remain unverified. |
| `IYubiKey.ConnectAsync<TConnection>()` → `HidConnectionSlot.OpenRawConnectionAsync()` | macOS built-in FIDO/OTP open on connection-owned workers; both direct and typed macOS paths have focused tests. Non-macOS fallback invokes `IHidInterface.ConnectTo*Reports()` *before* `Task.FromResult`. | Windows/Linux fallback can block at task invocation and remains outstanding; custom `IHidInterface` code is external. |
| `ISmartCardConnection.BeginTransaction()` / `BeginTransactionAsync()` → scope `Dispose()` | Built-in PC/SC synchronous begin/end wait on one worker; controlled withheld-native begin proves built-in async entry returns pending. A controlled custom sync-only implementation proves default async begin does not return until sync begin does. | Synchronous begin/end may wait indefinitely for native completion. External implementations have no SDK-enforced native drain; returned scopes promise only `IDisposable`. |
| `IConnection.Dispose()` → `DisposalGate`, `ExchangeGuard.CloseAndDrain()` or listener `Stop()` | Built-in PC/SC, macOS FIDO/OTP and direct report connections share their respective native-owner shutdown; controlled synchronous PC/SC disposal holds disconnect/context release behind a native transmit and observes the same native worker. `YubiKeyManager.Shutdown()` blocks on its async shutdown; monitor/listener stop has a bounded wait with possible retained/abandoned work. `YubiKeyDeviceManagerTests.DisposeAsync_WithAPublicationResumingAfterDisposeReturned_EmitsNothing` pins late publication suppression after the timeout, not a held native scan. | Caller must not dispose from its own admitted operation. Manager timeout is not native drain proof. Custom connections and other-platform listener teardown remain outstanding. |
| `OtpHidProtocol.Configure()` via applet initialization | Synchronous protocol configuration can wait for a feature-report exchange when firmware state is not initialized; raw OTP session creation defers status initialization and exposes no `Configure` method. | This is an internal session initialization boundary, not a public `RawOtpHidSession.Configure` API. Native wait has no proven upper bound. |
| Raw session `SendAndReceiveAsync`/`SelectAsync` → protocol interface; registered connection `SendAsync`/`ReceiveAsync`/`TransmitAndReceiveAsync` | Forwarding happens before the returned task; built-in macOS and PC/SC adapter lifecycle tests cover their selected paths. | External interfaces and legacy FIDO/OTP wrappers use synchronous `IHidConnection.GetReport`/`SetReport` before task return; task shape is not responsiveness proof. |

This describes identified macOS paths, not a complete all-platform or whole-assembly
call-graph certification. The inventory also records worker parking, credential-console
polling and native imports; those are not additional public synchronous report methods.
Direct calls on other platforms may block until native operations drain; custom implementations
own their own dispatch, cancellation and disposal behavior. Do not infer macOS drain behavior
for either case. These Core-only inventories do not certify other SDK-wide boundaries.

## Ownership And Sequencing

- A grouped physical YubiKey admits one live connection across its known interfaces.
- One connection admits one live applet or raw session.
- Dispose session N before creating session N+1 over the same caller-owned connection.
- `Raw*Session.CreateAsync(connection)` borrows the connection; the caller disposes both.
- `IYubiKey.CreateRaw*SessionAsync(...)` owns its hidden connection and disposes it with the returned session.
- Overlapping operations on one raw session throw `InvalidOperationException` immediately.
- Overlapping raw calls on a built-in SmartCard connection also throw `InvalidOperationException` immediately.
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
