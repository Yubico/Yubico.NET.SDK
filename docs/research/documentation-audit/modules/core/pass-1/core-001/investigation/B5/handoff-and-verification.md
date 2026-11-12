# Session/framing handoff and bounded verification proposal

Everything below is source/assertion reasoning or an **unexecuted** proposal. This packet ran no SDK,
test host, native/library/device command or compiler. The original source pin stays
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; later record commits supply no new product baseline.
Wire command/status bytes use hexadecimal notation; sizes, offsets and versions use decimal.

## Attachment, preparation, authentication and readiness

| Step | Current actor and exact source | Ownership/outcome |
| --- | --- | --- |
| Choose transport | Each applet IYubiKey extension calls ResolveSessionTransport; Core YubiKeyConnectionExtensions, lines 239–303, opens exactly one typed connection | Supported exposed transport chosen once; no failure fallback. SCP without preference forces SmartCard only in multi-transport applet extensions. |
| Create direct session | ApplicationSession.Construct, lines 227–245, runs constructor, then ConnectionSessionGuard.Attach | Caller-created connection borrowed. Constructor failure attaches nothing. Refused new object is disposed; holder-specific Detach preserves incumbent. |
| Validate options | ConstructInitializedAsync, lines 247–269, validates PreferredConnectionType equality before construction | Direct factory asserts supplied connection; it never reconnects. ProtocolFactory dispatches runtime interfaces (SmartCard/FIDO/OTP priority), not Type flags. Applet supported-type constraints still belong to applet. |
| Prepare | PrepareProtocolAsync, lines 323–338, checks disposed/bound/token; creates protocol and assigns base Protocol before SCP validation | Base is disposal owner even if non-PCSC SCP rejection or FIDO INIT fails. Non-PCSC SCP rejected before initialization I/O. FIDO applet prepare eagerly awaits guarded INIT; raw FIDO stays lazy. |
| Discover applet state | Actual applet callback selects/probes through borrowed backend | Initial select/version/probe traffic precedes optional SCP. Override changes effective configuration; required probes still occur. Applet-specific gates can use detected or effective version. |
| Configure/authenticate | ConfigureProtocolAsync, lines 359–397, configures then awaits PcscProtocol.InitializeScpAsync when parameters supplied | Effective Protocol/version/IsAuthenticated publish after successful configuration/auth step. Base protocol remains owned during partial setup. B6 owns handshake counters, keys, certificate/profile validation and failed setup cleanup, not B5. |
| Finish applet setup | InitializeSessionAsync, lines 307–320, awaits entire callback, rechecks disposal, sets readiness | Metadata/cache/backend work after Configure must complete first. Authenticated setup can precede ready=true; a subsequent callback failure disposes protocol and clears flags. |
| Successful convenience return | Each extension calls OwnConnection only after CreateAsync succeeds | Session now owns hidden connection. Before that, CreateSessionOverTransportAsync catch owns disposing opened connection after callback failure; cleanup errors are logged without replacing initial exception. Native retained claim still follows platform release proof. |
| Failed direct initialization | ConstructInitializedAsync catch calls DisposeAfterInitializationFailure, lines 403–420 | Synchronous cleanup can wait for guard/native completion; exceptions and logging failures suppressed to preserve original initialization failure. Borrowed connection survives as an ownership fact; partial wire state may require reopen. |

Neither no-I/O precondition rejection nor session detachment proves device recovery. A handshake may have
changed card state before failure. The historical OATH serial 125 SCP attempts returned 6A88 at INITIALIZE
UPDATE before EXTERNAL AUTHENTICATE or RESET; those establish no secure RESET outcome. B6 should keep
that exact setup failure alongside its initializer analysis rather than inferring an unsupported OATH
version or general hardware minimum.

## Public async subject/result and input lifetime

This covers every public async method in B5 primary source, plus internal protocol operations supporting
those methods. Internal IProtocol machinery is not a consumer extension API. No `MemoryResult.Clear`
method exists in these raw HID APIs: the returned type is ReadOnlyMemory<byte>; ApduResponse has Clear.

| Surface | Subject/lifetime and input timing | Result, cancellation and exception boundary |
| --- | --- | --- |
| RawCtapHidSession.CreateAsync | Binds borrowing protocol, returns Task.FromResult; no channel I/O | IsInitialized=true, firmware remains 0.0.0; pre-cancel/constructor/attach failure can throw synchronously before Task return. |
| RawOtpHidSession.CreateAsync | Same borrowing shape, no status I/O | First operation lazy-initializes; inherited firmware remains 0.0.0. Same synchronous failure shape. |
| RawSmartCardSession.CreateAsync simple/SCP overloads | Simple creates default protocol with no SELECT; SCP accepts borrowed parameters/version/config and configures first | Actual async core returns after handshake when requested. Required parameters stay alive through creation; SD retention exception belongs to its own factory. Non-null SCP produces protocol auth on success. |
| RawCtapHidSession.SendAndReceiveAsync | Caller payload borrowed across lazy INIT and packet-by-packet copies | Fixed 64-byte SDK packet copies clear in finally; successful responseData array transfers and survives disposal; callback/late-cancel failure clears accumulator, not all source reports (C005). Guard-entry token, between-keepalive CANCEL, valid terminal drain. Method forwards protocol Task before returning; a synchronous lower connection can still block invocation. |
| RawOtpHidSession.SendAndReceiveAsync | Payload borrowed until initialization completes, then copied to a padded 64-byte buffer; internal 70-byte frame and 8-byte report temporaries clear | Returns six copied status bytes or independent assembled report data including command-specific CRC/padding; no inbound CRC interpretation. Caller touch cancellation triggers one abort after wire attempt. Reset failure latches refusal; caller closes/reopens connection. |
| RawSmartCardSession.SelectAsync | Supplied AID borrowed through protocol formatting/transmission; caller retains input | Returns Data view backed by ApduResponse's owned copy; no ApduResponse owner/clear handle exposed through this signature. Returned view lifetime is distinct from constructor/native input copies. B6 retains actual SELECT cleanup/response decisions. |
| RawSmartCardSession.TransmitAndReceiveAsync | ApduCommand borrows Data; actual logical-exchange Task is input lifetime boundary | Returns ApduResponse copied owner. Check SW/data first, then Clear sensitive response including status aliases; caller input remains unchanged. throwOnError=true may throw before result transfers; B6 owns error-response cleanup. Entry token only for constituent I/O; no rollback. |
| ApplicationSession.DisposeAsync | BeginDisposal sets disposal-start immediately, clears public readiness/auth, then DisposalGate shares winner | Protocol async disposal if supported else sync fallback; finally detaches, disposes owned connection asynchronously. Derived managed Dispose(bool) runs afterward even on failure. No universal operation-task drain. ValueTask consumed once; separate invocations share SDK completion. |
| IFidoHidConnection.SendAsync / FidoHidConnection.SendAsync | Public contract borrows exactly 64 bytes until actual Task terminal; compatibility clone then synchronous SetReport | Adapter ignores token, clears copy finally. Built-in Linux/macOS sends own copies before worker dispatch; after dispatch await native return. Details remain platform-owner-qualified. |
| IFidoHidConnection.ReceiveAsync / FidoHidConnection.ReceiveAsync | Direct Tier 2 callers own sequencing/correlation and returned report | Adapter GetReport synchronous, validates 64-byte length then returns same array. Linux/macOS pending reader cancellation detaches only that reader, native input remains; no protocol CANCEL is implicit. Custom/Windows separate. |
| FidoHidConnection.DisposeAsync | Closes its underlying expert report facade synchronously via Dispose | Completed ValueTask means that compatibility call returned, not a universal typed-owner async shutdown policy. |
| OtpHidConnection.SendAsync / ReceiveAsync / DisposeAsync | Exact 8-byte clone/synchronous SetReport, synchronous GetReport, sync Dispose respectively | Adapter ignores tokens. Built-in Linux/macOS feature owners use separate workers and cannot detach dispatched GET/SET on cancellation. IOtpHidConnection declaration itself is B4 primary, read as dependency only. |
| Internal FIDO InitializeAsync/SendCommandAsync/DisposeAsync | ExchangeGuard spans lazy channel INIT plus full framed request/response; Dispose closes/drains and may terminal-wake active built-in connection | Dispose self-flow rejects through AsyncLocal; idle borrowed reuse supported, active terminal wake can end native connection usability. FirmwareVersion on protocol retains INIT value even after channel field clears; raw session property is separate. |
| Internal OTP ReadStatusAsync/SendAndReceiveAsync/DisposeAsync | ReadStatus initializes, reads another report, returns report[1..7] alias; command path copies as above | Uncancellable constituent exchanges; touch signal checked separately. No self-disposal detector. Internal Configure synchronously waits on init guard; public Management/YubiOTP await status first. |

The public synchronous Configure, IsSupported, WithPreferredConnectionType, CRC and AID getters were also
read in full. An outer `WaitAsync` timeout/cancellation completes a different wait; it neither drains the
original operation nor permits early caller-buffer mutation. Detached user work and prompt lifetimes
remain application-owned. Do not blanket-zero public metadata; classify authentication material,
secret-derived output and plaintext separately.

## Disposal/recovery layering

1. Session disposal admission makes IsInitialized/IsAuthenticated false and future ThrowIfDisposed calls
   fail. It does not erase retained effective firmware or make ConnectionType/IsSupported throw.
2. Protocol close stops new exchanges and waits the admitted exchange. FIDO optionally terminal-wakes
   only an active connection supporting ITerminalWakeControl; idle disposal leaves connection untouched.
3. Session detaches the exact connection-holder slot once. Direct borrowing never disposes connection;
   convenience ownership disposes it. Protocol failure still runs release in finally. Repeated callers
   observe one disposal outcome, including failure; an error task alone is not native release proof.
4. B1 portable PC/SC retains rooted owner/claim until checked disconnect+context release. Linux B2 FIDO
   separately waits true reactor quiescence/reference release/checked close; OTP waits GET/SET then close.
   Linux EINTR/EIO can report diagnostic failure after proven release; EBADF/unknown retains quarantine.
5. macOS B3 FIDO distinguishes Cancel, WaitShutdown quiescence and successful Destroy; OTP separately
   checks Close then releases CF owner. Windows/custom compatibility disposal behavior remains B4's
   source boundary, not imported Linux or macOS guarantees.

All stages distinguish admitted, wait canceled, native pending, actual terminal, drained, release proven
and uncertain retained claim. Raw direct calls bypass the session/exchange guards; keep them separate
from session use for the full lifetime. Per-exchange admission neither groups several public APIs into
one transaction nor observes detached async callbacks.

## Nine bounded boundary seeds

Eight derive from ApplicationSession; WebAuthn is a ninth indirect client boundary. All actual factory
methods and initializer scopes below were read; these are **seeds**, not dispositions of applet units.
Each boundary owner still reads full operations/helpers/test oracles and its original reopening maps.

| Boundary/current path | Transport/open/initialization and version source | Retained dependency and exact later question |
| --- | --- | --- |
| Management `src/Management/src/{IYubiKeyExtensions,ManagementSession}.cs` | SmartCard→FIDO→OTP; explicit preference or implicit SCP→SmartCard. Select/status/prepared INIT fallback, then DeviceInfoReader best-effort metadata; noncancel errors fall back, cancellation propagates. Override affects effective protocol; `_version` remains discovered for later reads. | Prompt ignored. Creation probe before SCP. Recheck A metadata alias/cache concerns and `_version` versus live returned metadata without assuming immutable/fresh metadata. |
| CTAP2 `src/Ctap2/src/{IYubiKeyExtensions,Ctap2Session}.cs` | FIDO→SmartCard, SCP default→SmartCard. Prepare initializes HID; only SmartCard selects shared FIDO AID. GetInfo before gate/config/SCP; detected GetInfo firmware or 0.0.0 controls USB gate 5.8, NFC allowed, override applied afterward. | Prompt retained; HidBackend requests lower handle through Core and resolves after status, before typed decode. CTAP payload status/app/model cleanup remain applet-owned; encoded CTAPHID versus CTAP operation byte must stay separate. |
| WebAuthn `src/WebAuthn/src/IYubiKeyExtensions.cs:60–82` | Validates origin/client options RpIdValidator before creating underlying CTAP2 session; forwards independent sessionOptions unchanged. CTAP2 transport/version rules apply. | WebAuthnClient owns successfully constructed CTAP2 session; construction failure disposes it. Client prompt differs from session presence prompt. Complete ceremonies/probes/extension secret-result owners need boundary review, not Core framing acceptance. |
| OATH `src/Oath/src/{IYubiKeyExtensions,OathSession}.cs` | SmartCard only. Backend SELECT initialization applied before Configure; custom send-remaining instruction supplied. Effective override used for OATH 5.6.3 SCP policy; Core floors separate B6. | Prompt retained, creation parameters not session-retained. Successful RESET sends DE/AD then DisposeAsync, no reselect/rehandshake. Caller still closes borrowed connection. Plain historical success and blocked SCP 6A88 remain distinct. |
| OpenPGP `src/OpenPgp/src/{IYubiKeyExtensions,OpenPgpSession}.cs` | SmartCard; backend SELECT, ACTIVATE/reselect on selected terminated statuses, GET_VERSION BCD/fallback; configure/SCP, then cache application-related data before ready. | Prompt retained; KDF/cache alias/disposal remain local. Backend GetVersion's actual throwOnError=false fallback accepts more than the contributor shorthand ConditionsNotSatisfied; boundary owner rechecks this narrow prose/source discrepancy, no new whole-applet disposition here. |
| PIV `src/Piv/src/{IYubiKeyExtensions,PivSession}.cs` | SmartCard; SELECT PIV+FD version; effective override configured/SCP then management-key-type discovery before readiness. `_detectedFirmwareVersion` remains independent of override. | Prompt retained; applet management auth separate IsAuthenticated. Multi-APDU auth/PIN-only recovery/reset can race between exchanges; preserve physical PIN/handle/UP deferral. No read-only/hardware check was performed. |
| Security Domain `src/SecurityDomain/src/{IYubiKeyExtensions,SecurityDomainSession}.cs` | SmartCard; SELECT then supplied version or assumed 5.3.0; config/SCP errors selected types wrapped only inside configuration catch. No exact firmware detection. | Stores caller `_scpKeyParams` for reset, ignores prompt. Reset disposes protocol and clears flags; reinitializes using retained params but omits original configuration/version and clears explicit-version indicator. Core callback readiness change did not fix that local SD-IMP-005 question. |
| YubiOTP `src/YubiOtp/src/{IYubiKeyExtensions,YubiOtpSession}.cs` | SmartCard→OTP, implicit SCP→SmartCard. SmartCard Management SELECT probe then OTP SELECT status, version reconciliation; HID ReadStatus initializes first. Configure/SCP then backend with effective firmware/progseq. | Prompt retained; raw `_status` distinct effective override. Core requests observed HID wait, applet backend validates expected prefix CRC then resolves/transfers; SmartCard cached touch heuristic remains local. Recheck command parity/status fallback/progseq policies. |
| HSM Auth `src/YubiHsm/src/{IYubiKeyExtensions,HsmAuthSession}.cs` | SmartCard only; SELECT version tag 79 with exactly three bytes, else 5.4.3 policy; effective override then config/SCP. | Prompt retained. CALCULATE/HSM handshake/48-versus-56-byte output profiles are applet-owned; Core auth is not an authenticated HSM connection. Follow own key/result/PIN lifetime and historical 6982 classification without inferring native interruption. |

Every owned extension transfers OwnConnection only after successful static factory. The shared helper
disposes a just-opened connection if factory initialization fails; no alternate interface is tried.
Applet-returned copied models are different from Core transport replay/report/accumulation buffers.
Public rawSmartCard sends arbitrary caller APDUs; it adds no command-specific scaffolding/decoding.
B6 should consume this table without B5 dispositioning its APDU/SCP inventory.

## Complete composable examples — UNCOMPILED

These are proposed documentation bodies for the existing coordinator validation foundation, not shipping
edits or executed examples. Every non-SDK callback is an explicitly typed application prerequisite.
Compiler/analyzer binding remains `VALID-NOT-CHECKED`. Use ordinary explicit connection scopes so session
cleanup precedes connection cleanup even when later work throws.

### Owned CTAPHID PING exchange with real SDK surface

```csharp
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Sessions;

static async Task WithPingAsync(
    IYubiKey device,
    byte[] ownedPayload,
    Func<ReadOnlyMemory<byte>, CancellationToken, Task> consume,
    CancellationToken cancellationToken)
{
    ReadOnlyMemory<byte> response = default;
    try
    {
        await using IFidoHidConnection connection =
            await device.ConnectAsync<IFidoHidConnection>(cancellationToken);
        await using RawCtapHidSession raw =
            await RawCtapHidSession.CreateAsync(connection, cancellationToken);
        response = await raw.SendAndReceiveAsync(0x01, ownedPayload, cancellationToken);
        // consume borrows the response, awaits all of its work, and retains no aliases.
        await consume(response, cancellationToken);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(ownedPayload);
        // This is the SDK-returned caller-owned result, not arbitrary borrowed memory.
        CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(response).Span);
    }
}
```

The method accepts ownership of mutable input, up to 7609 bytes. PING echoes arbitrary bytes and avoids
inventing a CBOR ceremony or no-response CANCEL request. Ordinary public metadata PING need not be
secret, but this ownership pattern handles caller-selected sensitive bytes. It clears actual SDK result
storage; C005 records earlier transport copies the caller cannot reach. Do not separately clear/reuse
ownedPayload when an outer WaitAsync stops waiting: this original method still owns it through terminal.

### Borrowed raw SmartCard session with explicit sensitive response cleanup

```csharp
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;

static async Task WithRawApduAsync(
    ISmartCardConnection borrowedConnection,
    FirmwareVersion knownFirmware,
    byte[] ownedData,
    Func<ApduResponse, CancellationToken, Task> consume,
    CancellationToken cancellationToken)
{
    ApduResponse? response = null;
    try
    {
        await using RawSmartCardSession raw =
            await RawSmartCardSession.CreateAsync(borrowedConnection, cancellationToken);
        raw.Configure(knownFirmware);
        // The application supplies an appropriate APDU for its already-selected applet.
        response = await raw.TransmitAndReceiveAsync(
            new ApduCommand(0x00, 0xCA, 0x00, 0x00, ownedData),
            throwOnError: false,
            cancellationToken: cancellationToken);
        // Inspect SW and data before Clear; callback retains no response aliases.
        await consume(response, cancellationToken);
    }
    finally
    {
        response?.Clear();
        CryptographicOperations.ZeroMemory(ownedData);
    }
}
```

The specific 00 CA 00 00 command is an application-dependent generic GET DATA shape, not a PIV/ISO applet
recipe or a complete select/authenticate workflow. The caller must own/keep borrowedConnection outside
this method, coordinate selected applet state and close/reopen after uncertainty. The example deliberately
uses throwOnError:false so a returned status can be inspected and owned response cleared on error too.
B6 owns generic APDU formatting/error behavior; no unsupported command semantics are promised.

## Bounded coordinator verification proposal — DO NOT RUN AS PART OF B5

Use the existing reviewed example/compiler foundation; do not create another generic host or mirror
validation-only tests. Serialize assigned repository runs. Re-read current build/test skills and toolchain
selectors before execution. Route TMPDIR/TMP/TEMP and explicit output/logs to
`/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/B5/`; preserve failures and actual nonzero counts.

| Lane | Finite meaningful input and oracle | Bound/cleanup/interpretation |
| --- | --- | --- |
| Existing managed regression | Core.UnitTests exact B5 classes from packet, with portable controlled-native helper dependencies. PublicApi AppletSessionShape/AsyncSurfaceConvention/FactoryShape in actual project. | Proposed only. Inspect native dependencies before labeling a selection no-native. Existing B5 ControlledSCard uses fake API. Preserve assertions; do not count source existence as executions. |
| B5-E01 raw version/readiness | Fake INIT 5.8.0 and OTP status 5.4.3; observe before/after first complete exchange and after disposal: session FirmwareVersion 0.0.0, protocol discovered value, readiness/auth flags and IsSupported. | One deterministic exchange each, including pre-cancel and post-disposal controls. Captures C001 observed versus selected intended contract, not device firmware. |
| B5-E02 actual application framing hold | Pre-initialize FIDO before hold, then a 58- or 100-byte payload; hold continuation send rather than INIT. Observe second-operation refusal and exact original request/response bytes. | At most two application requests. Release gate and await actual exchange/disposal in finally; independent fixed wire expectation, not same encoder recomputation. |
| B5-E03 cancel matrix | CBOR 10 versus encoded 90; terminal 00+CBOR and 2D/27; vendor 42 script; no-further-keepalive pending receive. Count CANCEL, inspect CID/CMD/BCNT 0 and actual terminal drain. | Use fake no-keepalive release in finally, no elapsed-time claim; restore after finite injected response. Normative CBOR response is not a CANCEL acknowledgement. No physical touch required for fake lane. |
| B5-E04 report ownership | Retain actual incoming init/continuation/feature arrays with nonsecret sentinel, plus accumulator and returned result. Success, wrong continuation, prompt failure and cancel-drain. | Assert each identified array independently before/after task and disposal; current expected input-report leftovers distinguish C005. Do not mutate arbitrary readonly borrowed provider storage. |
| B5-E05 malformed control/recovery | INIT reserved 0/FFFFFFFF, nonce mismatch/short response, extended valid INIT; KEEPALIVE length 0/2/oversize, ERROR length 0, malformed tail followed by independently tagged next response. | Fixed handful of frames; explicitly record current accept/reject and leftover queue, no speculative global conformance expectation. Establish chosen recovery policy before writing changed regression assertions. |
| B5-E06 OTP cadence/NEO | Count unchanged-status polls from immediate fake, held-native GET control; NEO refresh pre-write read failure and expected rejection separated. | Isolated watchdog host with gate release/drain; record poll count/elapsed interval/caller invocation separately. A three-second Stopwatch condition is not a native completion budget. No actual slot/config writes. |
| B5-E07 disposal/callback boundary | Held prompt requests attempt self-DisposeAsync in controlled isolated host; separate two-exchange fake applet pauses between exchanges and post-exchange parsing. | Observe deadlock/refusal/terminal flags and which actual public task remains pending; watchdog kill is host bound, not recovery. This settles policy without adding production-wide lease machinery. |
| Consumer compilation | Compile above complete typed methods and meaningful selected raw-tier/creation examples under actual .NET 10/Core dependencies/analyzers. | Capture exact bodies, compiler inputs/outputs and typed external prerequisites. No fake SDK classes, synthetic signatures or uncompiled success claims. |

**Native/platform lanes remain separate:** on each assigned OS, bind runtime/native package/loaded
artifact and driver/resource-manager version; record admission, cancellation request, native return,
terminal report/ack, drain, checked release and same-device reopen independently. Windows waits for
explicit user handoff and fresh source delta. Linux fake/native-only integration does not qualify
Windows/macOS; B3 selected historical Mac probes do not qualify a later installed binary. No new run
is assigned here.

**Device lanes remain unrun:** later authorized selected serial/firmware/transport can check read-only
INIT/PING/status/reopen. Actual CTAP cancellation requires a supported ceremony and human-coordinated
UP/UV; OTP touch challenge requires known dedicated slot policy and observable cleanup. Historical
serial 9681620 requires the matching Linux two-key/interface/PID scenario; other fake/physical successes
do not explain it. PIV physical prerequisite/PIN/handle/touch/hotplug and touch-dependent cancellation
remain user-deferred. Do not perform even read-only physical checks in this packet.

The coordinator's next actions are candidate triage, B6 consumption and nine independent boundary
reviews. This file authorizes no runtime repair, harness assertion change, compiler/device execution,
product edit or whole-Core closeout.
