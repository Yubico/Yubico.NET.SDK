# Raw Access Tiers

YubiKit exposes three deliberate access tiers. Choose the highest-level tier that can express the operation.
Dropping a tier removes SDK guarantees; it does not merely make the API more verbose.

## Tier 0: Applet Sessions

Applet sessions are the supported golden path. They select the application, interpret applet semantics,
validate inputs, enforce firmware and feature gates, and manage protocol state.

```csharp
await using PivSession piv = await yubiKey.CreatePivSessionAsync(
    cancellationToken: cancellationToken);
PinMetadata metadata = await piv.GetPinMetadataAsync(cancellationToken);
```

Use Tier 0 unless the operation is undocumented or intentionally below an applet API.

## Tier 1: Raw Sessions

Raw sessions are the supported power-user path:

- `RawSmartCardSession` supplies APDU formatting, command/response chaining, overlap refusal, explicit
  application selection, and optional SCP.
- `RawCtapHidSession` supplies CTAP HID channel allocation, packet framing, continuation packets,
  keep-alive handling, final-command correlation, CTAPHID error rejection, and overlap refusal.
- `RawOtpHidSession` supplies OTP feature-report framing, sequencing, polling, outbound CRC generation, and
  overlap refusal. It cannot validate command-specific inbound CRC without caller-supplied response semantics.

They intentionally do **not** select an applet during creation, apply applet-operation feature gates, or interpret
application-specific payloads. Transport and protocol prerequisites still apply; for example, SCP establishment
enforces its minimum firmware version. Bypassing applet checks does not bypass physical transport safety: raw
sessions still participate in the one-live-connection-per-physical-key and one-live-session-per-connection
contracts.

Readiness has a narrow meaning at this tier. Raw HID creation performs no wire initialization; the
first exchange initializes lazily, and the inherited raw HID `FirmwareVersion` currently remains
`0.0.0` even after protocol discovery. A configured version is effective SDK policy, not hardware
attestation. Major-zero versions pass optimistic feature checks. Use applet sessions for their actual
selection, version discovery, authorization, and command-specific prerequisites.

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
For sensitive data, put consumption in `try` and `response.Clear()` in `finally`; clearing also
invalidates status bytes and all aliases into that response owner. Clear source and copied buffers
separately. These snippets are composable fragments whose APDU, AID, payload, version, device, and token
inputs come from the caller's protocol implementation.
The `IYubiKey` extension opens exactly the SmartCard transport and transfers ownership of the hidden connection
to the returned session. Disposing the session therefore disposes that connection.

When the protocol marks a failed command chain or response continuation unrecoverable, it refuses
further transmissions, selections, and configuration without replaying the exchange. Its current
offset-based detection does not cover every uncertain first-fragment failure; a released guard alone
is not proof of recovery. Dispose and reopen after an interrupted or uncertain exchange, including when a raw session borrowed
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
await using PivSession piv = await PivSession.CreateAsync(connection, cancellationToken: cancellationToken);
```

### FIDO HID

```csharp
await using RawCtapHidSession raw =
    await yubiKey.CreateRawCtapHidSessionAsync(cancellationToken);

ReadOnlyMemory<byte> response = await raw.SendAndReceiveAsync(
    ctapHidCommand,
    payload,
    cancellationToken);
```

The caller supplies and interprets the CTAPHID command payload. This API does not add CTAP2 or WebAuthn
request semantics. A final response command must match the request; `CTAPHID_KEEPALIVE` is accepted only while
waiting, and `CTAPHID_ERROR` fails the exchange instead of returning an apparently successful payload.

Supply ordinary request/response commands to `SendAndReceiveAsync`. `CTAPHID_CANCEL` is a no-response
control command, not a request with a cancel acknowledgment. Core's caller-token cancellation sends it
only after a keep-alive and then drains the original response; silence can leave that drain pending.
The late-success exception currently requires the original unencoded CBOR command byte `0x10` and a
response payload beginning with `0x00`. Encoded `0x90` can pass terminal-command correlation, which masks
the initialization bit, but misses that success classification and can surface cancellation instead.
This is a current normalization gap, not a different intended CBOR grammar or authenticator requirement.
Malformed control data or a partial FIDO failure does not establish a general safe-reuse guarantee.

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

OTP status polling and touch-wait conditions use local deadlines; they do not impose a deadline on an
already-dispatched native GET/SET. The current NEO refresh path also catches broader failures than
cancellation alone, and tight status polling has no general backoff guarantee. Treat those as current
limits when diagnosing latency, rather than assuming vendor/Python timing policy is enforced here.

### Raw SmartCard With SCP

Select the applet on a plain borrowed session before establishing a raw secure channel. This complete
method borrows caller-supplied parameters and request data; the caller keeps those alive through the
await and clears sensitive source buffers afterward. It consumes only status and length, then clears
the response. The command and selected applet must be valid for the provisioned SCP profile:

```csharp
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;

static async Task SendProtectedAsync(
    Yubico.YubiKit.Core.Abstractions.IYubiKey yubiKey,
    ScpKeyParameters scpParameters,
    FirmwareVersion firmwareVersion,
    ReadOnlyMemory<byte> applicationId,
    ApduCommand command,
    CancellationToken cancellationToken)
{
    await using ISmartCardConnection connection =
        await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken);
    await using (var plain = await RawSmartCardSession.CreateAsync(connection, cancellationToken))
    {
        plain.Configure(firmwareVersion);
        await plain.SelectAsync(applicationId, cancellationToken);
    }

    await using var secure = await RawSmartCardSession.CreateAsync(
        connection, scpParameters, firmwareVersion, cancellationToken: cancellationToken);
    ApduResponse response = await secure.TransmitAndReceiveAsync(
        command, throwOnError: false, cancellationToken: cancellationToken);
    try
    {
        Console.WriteLine($"SW={response.SW:X4}, {response.Data.Length} bytes");
    }
    finally
    {
        response.Clear();
    }
}
```

Configuration is applied to the base APDU processor before SCP establishment. An SCP raw session cannot be
reconfigured afterward because replacing or partially updating the established processor graph would invalidate
the secure-channel framing assumptions. The existing Core SCP processor owns secure-channel session material.
The caller retains the normal ownership obligations of the supplied key-parameter type and source key buffers.
Pass a `ProtocolConfiguration` during creation only when the target requires a non-default framing option, such as
forcing short APDUs.

`SelectAsync` after SCP sends a plain SELECT while retaining the existing processor graph. It is not
a reauthentication/recovery procedure. Prefer an applet factory when its modeled operation fits.
SCP setup and `IsAuthenticated` supply no PIN, UV, or management authorization. SCP11b authenticates
the card to the host; a/c setup completion is distinct from the device's first protected-command phase.
The current fixed P-256/AES-128 SCP11 implementation and supplied certificate/key references need a
matching provisioned device profile; accepted constructor shapes alone establish neither trust-chain
validation nor full SCP conformance.

For source comparisons, keep GlobalPlatform editions separate: SCP03 1.1.2 is the March 2019 Public
Release; code-cited 1.1.2.6 is a December 2019 Public Review. The inspected SCP11 1.3.0.13 is an August
2023 Public Review, not the published final 1.4. Final/profile clause verification and independent wire
qualification remain outstanding. In particular, current empty-command and protected response/error
handling should not be presented as complete conformance established by synthetic tests.

## Tier 2: Raw Connections

`ISmartCardConnection.TransmitAndReceiveAsync`, `IFidoHidConnection.SendAsync` / `ReceiveAsync`, and
`IOtpHidConnection.SendAsync` / `ReceiveAsync` remain public for direct raw-connection access. These methods bypass
`ApplicationSession`, `ConnectionSessionGuard`, and `ExchangeGuard`.

At Tier 2 the caller owns APDU or packet formatting, command chaining, response correlation, CRC validation,
keep-alive handling, sequencing, concurrency exclusion, and recovery from partial or cancelled exchanges.
Never drive a raw connection concurrently with a live session or another raw operation. If traffic is interrupted,
interleaved, or otherwise leaves device state uncertain, dispose the connection and open a new one before continuing.
Keep caller-owned command memory valid until the returned task finishes, even after requesting cancellation;
only then clear sensitive input. The built-in macOS/Linux/Windows HID sends copy their reports before native dispatch, but
the public interfaces also admit custom implementations and do not promise that copy. A macOS/Linux raw FIDO receive
cancelled while waiting may detach its reader while the input owner remains active; the next report can still
arrive. A built-in OTP report already dispatched may succeed after cancellation. Neither case proves the
device protocol is reset. Custom connections define their own scheduling, cancellation and release behavior.

If an outer `WaitAsync` times out or is canceled, retain and observe the original operation task. Its
completion controls the borrowed-memory lifetime; the outer wait supplies no native completion or
release evidence. Custom HID compatibility adapters can execute report calls synchronously before
returning a task; their task shape does not add native cancellation or release proof.

Built-in Windows FIDO uses `WindowsFidoHidNativeOwner` with `WindowsHidReportApi` and
`ThreadPoolBoundHandle` overlapped completion; OTP uses `WindowsOtpHidNativeOwner` with the separate
blocking `WindowsHidFeatureReportApi` worker route and zero-desired-access open. Each admits one ordinary
operation without a backlog. Cancellation before dispatch submits nothing. After dispatch, FIDO requests
targeted `CancelIoEx`, but the cancellation request, including `ERROR_NOT_FOUND`, is not completion
proof. The owner retains storage and admission until submission has returned and the completion
callback has published its last use of request resources, then retires the request. Native success
can win cancellation. OTP retains storage and admission until GET/SET returns and may succeed after
cancellation. These are native
lifetime rules, separate from CTAPHID_CANCEL, OTP recovery and safe protocol reuse.

Windows factory/discovery routes transfer the original physical claim before native acquisition. Only
the owner releases a transferred claim after proven cleanup; a bounded discovery wait does not detach
native ownership. Checked-close failure retains unproven resources and the claim. Known pre-dispatch BCL
overlapped-preparation failure faults the public operation/shutdown while retaining unproven BCL state.
Public fault completion therefore does not by itself establish safe reopen.

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
`IHidInterface` backed by a platform implementation. Cancellation before dispatch skips enumeration; once dispatched,
the task stays pending through enumeration even if the token is canceled. The discovery task does not make
the resulting report connection asynchronous or cancel an already-running native scan. Built-in typed macOS FIDO
and the direct macOS FIDO report connection share the persistent input-owner implementation; OTP uses its
own migrated route through `HidConnectionSlot`.

Built-in Linux typed FIDO/OTP now route through separate native owners too. FIDO readiness uses a
bounded nonblocking read reactor; open/metadata/output/quiescence/checked close use the connection worker.
OTP open/feature GET/SET/checked close use its own worker, never the reactor. Both accept the original
grouped-key claim before acquisition. Direct Linux report access stays synchronous over those owners.

| Public method(s) | Execution and owner | Evidence gap / caller limit |
|---|---|---|
| `IHidInterface.ConnectToIOReports()` / `IHidInterface.ConnectToFeatureReports()` | The `MacOSHidInterface.ConnectToIOReports()` / `MacOSHidInterface.ConnectToFeatureReports()` implementations synchronously construct `MacOSHidIOReportConnection` / `MacOSHidFeatureReportConnection`. The caller owns the returned `IHidConnection`. | Direct opens do not take the grouped-key registry claim used by `IYubiKey.ConnectAsync<TConnection>()`; do not infer its quarantine or native-release guarantees. |
| `IHidConnection.GetReport()` / `IHidConnection.SetReport(byte[])` | macOS IO `GetReport` synchronously awaits the persistent input owner's receive with a six-second cancellation timeout; `SetReport` synchronously awaits an owner-worker output using an operation-owned copy and accepts the caller's report length (unlike typed FIDO sends, which require 64 bytes). macOS feature reports synchronously await the OTP owner worker; direct feature `SetReport` accepts arbitrary length, unlike typed eight-byte OTP sends. | No public cancellation token. The IO timeout detaches the reader, not the native owner; late reports remain queued for retry. Native output duration is not bounded. |
| Linux `IHidInterface.ConnectToIOReports()` / `ConnectToFeatureReports()` | `LinuxHidInterface` synchronously constructs the IO/feature facade over the FIDO/OTP native owner. | Direct opens take no implicit grouped-key claim; open and descriptor ioctls can wait indefinitely. |
| Linux `IHidConnection.GetReport()` / `SetReport(byte[])` / `IDisposable.Dispose()` | IO reads block indefinitely for descriptor-sized, actual-length input; EOF is terminal. Feature GET and metadata remain eight bytes. Direct SET preserves supported caller lengths. Disposal waits for admitted native work, FIDO true reactor quiescence/reference release, and checked close on the original worker. | No public report cancellation token or macOS six-second timeout. Fake-native managed tests cover these owners, not hidraw/kernel timing. Unknown release retains the rooted owner; direct opens still have no grouped claim to quarantine. |
| Windows `IHidInterface.ConnectToIOReports()` / `ConnectToFeatureReports()` | `WindowsHidInterface` synchronously constructs IO/feature facades over the same FIDO/OTP owners used by typed connections. Worker-owned open and metadata precede return. | No implicit grouped-key claim on direct opens; no fixed native completion deadline. |
| Windows `IHidConnection.GetReport()` / `SetReport(byte[])` / `IDisposable.Dispose()` | Direct IO/feature payload metadata excludes the report-ID byte and follows descriptor sizes. SET requires exactly the output/feature descriptor payload length; typed FIDO64/OTP8 contracts remain separate. Disposal drains accepted work and checked close on the original owner. | No report cancellation token or drain timeout. Unproven release retains rooted native state. Controlled-seam tests do not establish actual Windows completion/HID-driver behavior. |
| `ISmartCardConnection.BeginTransaction(token)` / returned `IDisposable.Dispose()` | On built-in PC/SC connections, synchronous begin and transaction end block the caller awaiting the connection's native worker. The caller ends the scope before disposing the connection. A held transmit delays synchronous scope disposal until native work exits. | Admitted native acquisition/end has no guaranteed duration. Prefer `BeginTransactionAsync` and async-dispose the built-in scope as in the [Core example](../../src/Core/README.md#send-raw-apdus). The default `BeginTransactionAsync` on an external implementation executes its synchronous `BeginTransaction` **before returning a task**; an async name does not make that implementation nonblocking. An override can provide caller-thread responsiveness, but its native drain behavior depends on that implementation. |
| `IConnection.Dispose()` / `IConnection.DisposeAsync()` | macOS IO report disposal shares the typed FIDO owner's acknowledged shutdown; macOS feature-report disposal shares the OTP worker's checked shutdown and drains accepted GET/SET work without `Task.Run`. Built-in PC/SC `Dispose` blocks for its shared native-worker shutdown outcome: a held transmit delays disconnect and context release; repeat callers share the outcome. | Native shutdown may wait indefinitely for release proof. Never dispose synchronously from work whose completion disposal must await. Custom connections own their own drain and failure semantics. |

macOS IO report reads wake on disposal; native callback state and the physical owner are retained until
acknowledged shutdown. Failed native release retains the owner instead of claiming successful disposal.
The feature-report connection drains an admitted report before release; overlapping raw report calls are
refused. Direct report opens do not acquire a grouped-key claim. The macOS input-owner
behavior does not establish drain guarantees for all public synchronous entry points.

Direct macOS IO descriptor metadata can describe lengths other than 64, while the shared FIDO owner
normalizes received input to 64 bytes. Direct feature GET separately returns eight-byte storage and can
pad a short successful native report. Descriptor metadata alone therefore does not promise arbitrary
input lengths or exact native transfer counts on those facades.

The Core [source-site inventory](../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/README.md)
classifies waits and pre-task-return dispatch, but a listed site is not automatically a public boundary
or a verified drain. The [finite public entry registry](../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/PublicSyncBoundaryRegistry.cs)
independently requires Linux/macOS/Windows direct-report, portable SmartCard, listener and scan
entry links from public symbols to their current internal owners. It links existing behavioral tests
where possible; its reflection validation is not a native drain test. The separate
[adapter contract registry](../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/AdapterContractRegistry.cs)
at the L6a managed checkpoint required 43 operation rows and 76 links to runnable behavior tests, including Linux typed FIDO/OTP
and retained direct IO/feature paths (16 new rows / 27 links), plus synchronous
SmartCard begin, scope end and disposal and the external default async-begin fallback. Removing
a required operation or profile role fails validation independently of the JSON rows. Neither
registry certifies custom implementations.

At incoming source `e322712d609aaf03b9bd1cf2964a99326728866a`, the inventory README records
59 adapter operations / 104 runnable profile links, 31 public reachability entries, and 245 classified
source sites. Its consistency comparison against `bdac2f4f4224fa0ccf92006971af8659736aa2b1`
retains 229 full site identities and maps 16 through renames or extractions. The integrated Linux-hosted
ordinary unit run checked actual Core source against all 245 sites and validated the 59-operation /
104-link and 31-entry registries. Linked managed behavior tests and applet cross-layer tests ran in
the same ordinary suite; their links are not additional executions. Source families remain
documented/outstanding, not verified native behavior. The 229/16 split remains a historical mapping,
not a new runtime count. See the [software results](../research/documentation-audit/integrations/windows-upstream-20261113/validation/summary.md).
The earlier 43-operation / 76-link totals describe L6a only. Neither checkpoint establishes native
completion, HID-driver behavior, or platform acceptance. Source-bound Windows and macOS results and
their remaining gaps are recorded in the
[integration intake](../research/documentation-audit/integrations/windows-upstream-20261113/intake-review/report.md#4-received-execution-evidence-and-its-limits).
The scoped reachability map:

| Public entry / path | Contract and existing evidence | Remaining disposition |
|---|---|---|
| `FindHidInterfaces.Create().FindAllAsync(token)` → platform `GetList()` → `IHidInterface.ConnectToIOReports()` / `ConnectToFeatureReports()` | `FindHidInterfacesBoundaryTests` gates real `FindAllAsync` dispatch with injected platform enumeration: pre-cancellation submits no scan; invocation returns a pending task while enumeration is withheld; cancellation after dispatch does not complete it early; enumeration faults propagate without replay. Returned interfaces expose synchronous constructors. macOS direct report facades have compatibility tests and selected-key probes; Linux and Windows facades have managed fake-native tests. | This is an enumeration delegate gate, not native handle teardown proof. `Task.Run` has no per-finder/global worker bound. Windows native teardown acceptance remains pending; Linux selected-key evidence is bounded in [native acceptance](../research/linux-native-acceptance.md). |
| `IYubiKey.ConnectAsync<TConnection>()` → `HidConnectionSlot.OpenRawConnectionAsync()` | macOS/Linux/Windows built-in FIDO/OTP open on connection-owned workers. Linux public CTAP2/YubiOTP journeys use actual slots and owners under fake native seams. Windows routing/facade tests exercise actual owners under controlled report seams. Only custom fallback invokes `IHidInterface.ConnectTo*Reports()` *before* `Task.FromResult`. | Custom fallback can block at task invocation; custom code is external. Managed route tests do not attest physical device behavior. Windows native/integration acceptance remains pending. |
| `ISmartCardConnection.BeginTransaction()` / `BeginTransactionAsync()` → scope `Dispose()` | Built-in PC/SC synchronous begin/end wait on one worker; controlled withheld-native begin proves built-in async entry returns pending. A controlled custom sync-only implementation proves default async begin does not return until sync begin does. | Synchronous begin/end may wait indefinitely for native completion. External implementations have no SDK-enforced native drain; returned scopes promise only `IDisposable`. |
| `IConnection.Dispose()` → `DisposalGate`, `ExchangeGuard.CloseAndDrain()` or listener `Stop()` | Built-in PC/SC and macOS/Linux/Windows FIDO/OTP share their respective native-owner shutdown. PC/SC tests hold transmit and each native release independently, retain the public claim until both succeed, and preserve live release-failure quarantine. Linux owned-session tests retain claims through true reactor quiescence. Static manager initialization/shutdown has been removed. Owned monitor asynchronous disposal stops listeners, cancels and awaits its single scan loop without a drain timeout, retires discovery, then completes the feed; already-open discovery work retains its native lease until actual teardown. | Caller must not dispose from its own admitted operation. Bounded discovery waits do not prove native drain; listener implementations have their own shutdown limits. Custom connections and other-platform listener teardown remain outstanding; managed fakes do not attest drivers. |
| `OtpHidProtocol.Configure()` via applet initialization | The internal synchronous initialization wait remains classified. Public Management/YubiOTP initialization awaits `ReadStatusAsync` first, making subsequent Configure initialization already complete. The Linux public YubiOTP journey holds initial native GET and proves creation returns pending; no extra Configure GET is consumed. Raw OTP creation remains lazy and exposes no Configure. | No broad removal of this latent internal boundary is claimed. Direct uninitialized internal Configure can still wait indefinitely; this is not a public `RawOtpHidSession.Configure` API. |
| Raw session `SendAndReceiveAsync`/`SelectAsync` → protocol interface; registered connection `SendAsync`/`ReceiveAsync`/`TransmitAndReceiveAsync` | Forwarding happens before the returned task; built-in macOS/Linux/Windows and PC/SC managed lifecycle tests cover their selected paths. | External interfaces and custom FIDO/OTP wrappers may use synchronous `IHidConnection.GetReport`/`SetReport` before task return; task shape is not responsiveness proof. |

This describes identified macOS/Linux/Windows managed paths, not a complete all-platform or whole-assembly
call-graph certification. The inventory also records worker parking, credential-console
polling and native imports; those are not additional public synchronous report methods.
Direct calls on other platforms may block until native operations drain; custom implementations
own their own dispatch, cancellation and disposal behavior. Do not infer native Linux or Windows acceptance
for either case. These Core-only inventories do not certify other SDK-wide boundaries.

## Ownership And Sequencing

- A grouped physical YubiKey admits one live connection across its known interfaces.
- One connection admits one live applet or raw session.
- Dispose session N before creating session N+1 over the same caller-owned connection.
- `Raw*Session.CreateAsync(connection)` borrows the connection; the caller disposes both.
- `IYubiKey.CreateRaw*SessionAsync(...)` owns its hidden connection and disposes it with the returned session.
- Overlapping guarded exchanges on one raw session throw `InvalidOperationException` immediately.
  Await the complete public workflow before starting another or disposing the session; exchange drain
  does not cover later workflow exchanges, parsing, or callback work.
- Overlapping raw calls on a built-in SmartCard connection also throw `InvalidOperationException` immediately.
- Once admitted, constituent I/O in a stateful session exchange is not cancelled mid-frame. A SmartCard
  exchange observes the token only at admission; canceling afterwards lets the exchange finish and return
  its response or failure. FIDO HID sends `CTAPHID_CANCEL` if it observes caller cancellation during a
  keep-alive, then reads and validates the terminal response before reporting cancellation when that
  response is valid; OTP HID attempts a dummy-report reset after touch-wait cancellation. A failed partial
  APDU or failed OTP reset makes that protocol instance unusable; dispose and reopen the connection.
  Cancellation does not roll back a device command, and native work has no guaranteed completion deadline.
- Disposal atomically closes admission, waits for an admitted exchange, and only then disposes protocol/SCP state
  and any convenience-owned connection. New operations are refused as soon as disposal begins.
- A borrowed connection can host the next session after healthy idle or sequential use, and after a SmartCard
  disposal whose drained exchange succeeded. If a FIDO HID exchange is active when disposal begins, the built-in
  macOS/Linux connection is terminally woken; the connection is not disposed but may
  become unusable, even if the exchange finishes concurrently with the wake. Windows terminal wake requests
  shutdown of its native owner. Dispose and reopen after either terminal wake; healthy idle borrowed reuse
  remains supported.
- Prefer `DisposeAsync`. Synchronous `Dispose` performs the same drain by blocking and must not be invoked from
  inside the operation being drained.

## Sensitive Buffers

FIDO and OTP protocols clear selected SDK-owned outgoing payload copies, frames, and report staging in
`finally`. Incoming report arrays, accumulated responses, transport staging, and applet-decoded output
are separate owners; cleanup of one does not erase the others. Caller-owned request memory remains
unchanged. Retain sensitive response data only as long as needed and clear mutable storage you own.
`ReadOnlyMemory<byte>` alone supplies neither a disposal contract nor permission to cast its backing
storage writable. A separately cleared copy also leaves the original intact. For SmartCard responses,
use the explicit `ApduResponse.Clear()` owner API after inspecting status.

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

Use `RawCtapHidSession` or `RawOtpHidSession` for the corresponding HID logical exchanges. Third-party protocol
composition should wrap a raw session rather than injecting or implementing Core protocol internals.
