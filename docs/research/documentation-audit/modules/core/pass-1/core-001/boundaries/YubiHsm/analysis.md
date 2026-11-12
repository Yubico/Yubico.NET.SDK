# YubiHSM Auth/Core boundary analysis

Source: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. This is a static investigation.
Exact file, original unit, character-range and pinned Git identities are in the adjacent JSON records.
References below use source line numbers, including enclosing helper/assertion context.

## Complete caller graph

```text
HsmAuthTool Program / command or menu
  -> module DeviceSelector -> Cli.Shared DeviceSelector (fresh All discovery, SmartCard eligibility)
  -> IYubiKeyExtensions.CreateHsmAuthSessionAsync:40–65
     -> ResolveSessionTransport(options.PreferredConnectionType, SmartCard only)
     -> WithPreferredConnectionType (shallow referenced settings)
     -> CreateSessionOverTransportAsync -> ConnectAsync<ISmartCardConnection>
        -> known-member registry / PcscConnectionSlot / built-in PC/SC owner
     -> HsmAuthSession.CreateAsync:128–138
        -> ConstructInitializedAsync -> validate preferred type -> construct -> bind
        -> InitializeSessionAsync -> HSM InitializeAsync:140–170
           -> PrepareProtocolAsync -> ProtocolFactory -> PcscProtocol registered for disposal
           -> HsmAuthBackend.ReadInitialStateAsync -> SELECT YubiHSM Auth
              -> TryFindValue(79) -> three-byte version or base fallback
           -> ConfigureProtocolAsync(effective version, configuration, SCP parameters)
              -> plain Configure -> optional Core SCP initialization
           -> backend rebound when effective protocol differs
        -> publish IsInitialized after complete callback
     -> OwnConnection after factory success
  -> public operation -> applet encode / backend Send -> PcscProtocol or PcscProtocolScp
     -> shared ExchangeGuard -> APDU/SCP/chaining -> built-in connection/native owner
  -> full public method parse / notification resolution / finally cleanup
  -> protocol close/drain -> detach -> owned connection shutdown
```

The direct factory borrows an already opened `ISmartCardConnection`. Its failure disposes the registered
protocol and detaches the session; the caller retains connection disposal. The convenience factory's
outer catch awaits hidden-connection disposal, logs a secondary cleanup error, and preserves the original
creation failure. There is one selected transport and no fallback. A preferred connection is an assertion
at the direct factory. SmartCard eligibility establishes neither enabled applet nor successful SELECT.

`ListHsmAuthCredentialsAsync` owns its convenience session through LIST and returns detached DTO metadata.
All command/menu factory calls are awaited inside `await using`; each example operation borrows that
session. `WithHsmAuthSessionAsync` is a different journey: its shared connection owner creates and disposes
a plain reset session in its own block, then creates the body session with optional SCP parameters.
The helper leaves persistent body changes for the hardware-run owner to reconcile.

## Resource table

| Resource | Acquisition / owner | Release / transfer | Exact limit |
| --- | --- | --- | --- |
| Physical known-member claim | `YubiKeyDevice` before built-in PC/SC open | Original PC/SC owner releases after checked disconnect/context release | Process-local overlapping known IDs; disjoint views and other processes need coordination. Direct factories/custom connections have their own bounds. |
| Native context/card/work | Built-in `PcscConnectionNativeState`, one rooted worker | Accepted work drains before transaction end, disconnect and context release | A cancellation request or completed outer wait supplies no release proof. Unknown release retains quarantine. |
| Connection | Caller of direct factory; hidden convenience creator otherwise | Session disposes only after `OwnConnection`; borrowed connection survives healthy sequential session disposal | Await the actual operation and teardown. Opening another session supplies no physical recovery proof. |
| Protocol/backend | Base session registers protocol before SELECT; backend references it | Base session owns protocol; backend is a thin borrower | Core creates no PC/SC transaction in `PcscProtocol`. HSM reset comment mentioning a retained transaction overstates this graph. |
| Options | HSM initializer receives referenced values; convenience copy retains same references | Options container is not retained by returned session | Prompt, firmware/configuration references and SCP objects have their own lifetimes. |
| Presence prompt / notification | Caller owns prompt; session retains it; each CALCULATE creates one handle | Caller disposes service after full tasks/callbacks end; handle resolves once | No session-wide callback/operation lifetime lease. Never dispose/reenter a session from its callback. |
| SCP03 parameters / static keys | Caller owns parameters; `StaticKeys` clones source arrays | Parameters dispose exact retained static-key object; protocol owns separate derived state | Record clones share contained owners. Clear original source separately; retain owners through initialization/use. |
| SCP11 keys/certificates | Caller-owned parameter wrapper retains exact key/certificate objects; collection is copied | Parameter disposal clears retained private wrapper and disposes retained certificates | Public-key parameter arrays can alias mutable storage. Host trust provisioning/validation is separate from disposal and Core receipt checking. |
| Management/key/password/context/cryptogram inputs | Caller storage borrowed by HSM method | Caller clears only after actual method terminal, including failure | Read-only views give no independent snapshot. An outer `WaitAsync` cancellation is a different task. |
| Padded password | `ParseCredentialPassword`, fresh 16-byte array | Method `finally` clears | Length is byte count; empty accepted; no UTF-8 well-formedness/normalization check is performed. |
| TLV owners / encoded request | `Tlv` copies each value into encoded owner; `EncodeAndDisposeList` copies joined encoding | Helper disposes submitted TLVs; method clears returned request in `finally` | Inline/list construction can fail before helper receives all owners; helper cannot clear owners never submitted. Preserve this exceptional allocation qualification. |
| Derived long-lived pair | `DeriveKeys` directly calls BCL PBKDF2; PUT-derived owns result | `PutCredentialDerivedAsync` finally clears 32 bytes, after delegated PUT | This is host password derivation for enrollment, distinct from device CALCULATE and Core SCP KDF. |
| Formatted APDU | Formatter/transmitter owns independent request encoding | `ApduTransmitter` finally clears after actual connection task | Source TLVs, original inputs and temporary formatter storage remain separate. |
| Native receive staging | Connection rents pool array | Return with `clearArray:true` in finally | Successful exact transport-result array is independently allocated and remains intact. |
| Transport result / APDU copy | Connection result then `new ApduResponse(responseBytes)` clone | HSM clears the final successful CALCULATE response owner | The earlier transport result survives that clear. No whole-pipeline erasure claim follows. |
| Chained response | Individual response owners, writer staging, final response clone | Writer `Clear` only on successful assembly; final HSM clear reaches final clone | Fragments are dropped without `Clear`; exceptional writer cleanup is narrower. B6-C002 owns mechanism. |
| Secure decrypted response | SCP Unmac temporary, decrypted array, copied APDU response | Unmac temporary clears; HSM later clears copied APDU owner | Decrypt temporary passed to copying constructor remains separate. Protected requests/counter/chain also have independent state owners. |
| `SessionKeys` result | Parse allocates 3 × 16-byte copies; reference owner | Successful resolution precedes transfer; callback failure disposes keys; caller later disposes returned owner | Existing spans see cleared arrays. Parse source and extra connector/caller copies remain separate. Exceptional partial Parse allocations need their own policy if hardened. |
| GET_CHALLENGE / GET_PUBLIC_KEY result | Returns response `Data` alias, keeping underlying array reachable | Caller retains returned view; no response owner is exposed | Shape/point checks differ below. Public ephemeral points are distinct from secret session keys; classify actual semantics. |

`ZeroApduResponse` currently uses `MemoryMarshal.TryGetArray` on `RawData` then zeroes the actual returned
segment. With the present sealed `ApduResponse` byte-array owner, this reaches that owned array and status.
Its test observes that owner; this is not a copy-clearing failure. The supported owner API is now `Clear()`.
Routing the helper through it is a later executable change, while current documentation should describe
the actual scope and avoid teaching read-only view write-through as a consumer pattern.

LIST/version/retry/public-key metadata receives ordinary ownership treatment rather than automatic secret
classification. HSM's version use of `TryFindValue` inherits C's extra discarded value copies, but its
three public version bytes supply no separate secret-leak finding.

## Initialization, operation and failure states

| Phase / result | Actual source transition | Consumer/reviewer action |
| --- | --- | --- |
| Create before binding | Validate transport; constructor captures prompt | Keep caller connection/service alive if construction fails. |
| Bound before SELECT | One live session claimed; protocol registered before I/O | A second session fails before it can deselect the incumbent. |
| SELECT/version/config/SCP fails | Direct factory synchronously disposes/detaches; convenience owner subsequently closes hidden connection | Preserve primary error and separately inspect cleanup/release evidence. |
| Setup succeeds | `ConfigureProtocolAsync` assigns effective firmware/auth; full initializer then publishes readiness | `IsAuthenticated` is Core setup history, not management-key, credential or HSM authentication. |
| Management/credential command succeeds | No cached HSM authorization flag is set | Send required management key or password per operation. Success does not authorize later operations globally. |
| Wrong password/key | Retry helper recognizes `63Cx`, throws typed exception with command headers | Inspect count/status; choose a deliberate next attempt. No authentication replay loop exists. |
| GET_CHALLENGE rejection | Backend's ordinary throw path yields general `ApduException` | Inspect SW; inherited HSM-IMP-001 remains open. |
| Pre-admission cancellation | Guard checks caller token; no exchange delegate runs | Prompt may already have completed request; fallback can resolve Cancelled. |
| Admitted SmartCard exchange | Delegate and constituent I/O receive `CancellationToken.None` | Await terminal I/O and cleanup; native call cancellation is a separate boundary. |
| CALCULATE returns OK but Parse fails | Parse throws (including 56 vs 48); finally clears final response and outer request/password; presence fallback is Failed | Preserve applet success versus host result failure. Avoid assigning response-tail meaning. |
| Successful keys / resolution fails | Local keys disposed; final response and request cleared; callback exception propagates | No result transfers. Existing test proves exception identity, not all key-array cleanup. |
| CALCULATE and resolution fail | Core resolves with primary exception; secondary callback failure logs | Primary APDU failure survives; exception payload provenance remains application/provider-owned. |
| Disposal admitted | Readiness/auth immediately false; protocol closes guard and drains an admitted exchange | Await full public method first; parse, callback and outer finally can extend past guard exit. |
| Recovery latch set | Later protocol calls refuse; historical auth flag can still be true until disposal | Dispose/reopen rather than relying on `IsAuthenticated` as a liveness probe. |

CALCULATE comprises an optional LIST, policy selection, password/TLV construction, request callback,
CALCULATE exchange, parser, success-resolution callback and finally cleanup. The exchange guard covers
each wire exchange; it is released between LIST and CALCULATE and before result parsing/callback cleanup.
Two public workflows can therefore interleave between exchanges, and disposal can complete while a
post-exchange callback still runs. RESET similarly has a gap between RESET and re-SELECT. The contract
should ask callers to serialize/await entire public workflows and retain their service/input owners;
choosing a whole-method lifetime lease is separate runtime/policy work.

## Reset and secure-channel boundary

`HsmAuthSession.ResetAsync:507–519` sends INS06/DE/AD, then calls the same backend's
`ReadInitialStateAsync`, discarding the returned version. Plain sessions preserve resolved firmware and
the same protocol. The method clears no readiness/auth flag on entry/failure, calls no
`InitializeSessionAsync`, and stores no SCP parameters for a later handshake.

With SCP, `_backend` references `PcscProtocolScp`: RESET is protected; following SELECT is passed through
with `useScp:false` by `PcscProtocolScp.SelectAsync:87–109` and `ScpProcessor:61–63`. The previous secure
graph remains installed. There is no reconfiguration or reauthentication. Successful re-SELECT, an
unchanged host auth flag, and a live native connection establish different facts. A failure after the
RESET send can follow an already completed destructive mutation.

GlobalPlatform SCP03 **1.1.2 Public Release**, §5.6, identifies Application Session termination (for
example new Application selection) among Secure Channel termination events. This is a card/SD SHALL
condition in that specification; the exact YubiKey HSM applet/profile behavior after this reset/reselect
still needs vendor/device evidence. It justifies preserving uncertainty, not claiming a measured HSM
reset failure or automatically prescribing OATH's different terminal-reset policy. Dispose/recreate
with appropriate externally owned parameters for a fresh channel rather than advertising re-SELECT as
reauthentication. No live HSM/SCP reset ran here.

HSM reaches B6's empty-command concerns at LIST, GET_MANAGEMENT_KEY_RETRIES and RESET, and protected
continuations when a long LIST/other response actually chains. Asymmetric CALCULATE data can also reach
short-command fragmentation: with a 64-byte label, 130-byte context, 65-byte public key, 16-byte cryptogram
and padded password, the encoded data is 302 bytes. Firmware/transport/configuration choose the route;
ordinary recording tests use short APDUs and short labels, without independent protected fragmentation
or error/R-MAC validation. B6-C004/C005/C011/C012/C015 remain their original mechanism groups.

## Version provenance and actual gates

| Reading / policy | Source / effect | Qualification |
| --- | --- | --- |
| Cached/live Management firmware | Discovery metadata / explicit Management read | HSM factory does not use it as its initial applet feature policy. Shared test filters use a separate fixture metadata snapshot. |
| Raw HSM SELECT | First tag 79 value found, exactly three bytes | Empty/missing/wrong-width version falls back to 5.4.3; malformed TLV framing can throw. The fallback is not a catch-all parser recovery. |
| Caller override | `firmwareVersion ?? initializationFirmwareVersion` | SELECT still runs. Override controls protocol configuration and HSM gates, not actual applet instruction availability. |
| Major-zero values | Core `Feature` accepts every major-zero version optimistically; `PcscProtocol` takes development configuration branch | 0.0.0 can mean unknown. Physical release/beta identity and command support require independent evidence. |
| Reset version | Read and discard re-SELECT version | Original resolved policy remains, even if a different value is returned. |
| Historical serial 103 | SDK/direct Python 0.0.1; ykman display 5.8.0; Management 5.8.0.beta.0 | Different actors and host normalization; preserve all observations. Python patches exactly 0.0.1, unlike C#'s general major-zero policy. |

Base methods depend on successful SELECT and perform no `EnsureSupports(FeatureHsmAuth)` gate. Asymmetric
import/generate/get-public-key/calculate use 5.6.0; GET_CHALLENGE uses 5.6.0 and adds a supplied password only
under 5.7.1/major-zero; password changes use 5.8.0. A supplied challenge password on 5.6.0–5.7.0 is ignored,
including its length validation. The obsolete `FeatureGetChallengeNoPassword` aliases the 5.7.1 field.
The byte-level reset test checks retained override and `IsSupported`; it supplies no SCP or physical gate
qualification. A's raw/effective policy findings are reused without a duplicate firmware defect.

## Cryptographic actor and result-format boundary

| Operation / value | Actual cryptographic or validation actor | Source-backed limit |
| --- | --- | --- |
| Enrollment-derived K-ENC/K-MAC | Host BCL static PBKDF2; salt `Yubico`, 10,000 iterations, SHA-256, 32 bytes | C# test recomputes same primitive; Python has fixed ASCII/Greek expected pairs. Neither executed here. |
| Symmetric CALCULATE | YubiKey credential/app | HSM C# builds a 16-byte context and optional tag 78; no Core CMAC KDF is called for these HSM result keys. |
| Asymmetric CALCULATE | YubiKey/app, externally trusted HSM public key and actual HSM handshake inputs | C# validates the 130-byte context only; key/cryptogram encoding, on-curve/trust checks are partly applet/integration-owned. Core EC constructors are not reached by this path. |
| Imported asymmetric scalar | C# checks 32-byte width; YubiKey processes scalar | No local mathematical scalar check. On-device generation sends empty private-key tag. |
| GET_PUBLIC_KEY | YubiKey returns credential PK-OCE; C# checks 65-byte response length | Prefix/on-curve validation stays outside this method; Python explicitly decodes a P-256 point. |
| GET_CHALLENGE | YubiKey returns host challenge or EPK-OCE | C# returns unvalidated response shape. Check the actual algorithm/profile before context assembly. |
| Core SCP03/11 | Core initializer/provider over host↔YubiKey channel | Separate from HSM credential/password auth and external host↔HSM secure session. Generic crypto agreement cannot identify applet CALCULATE tail bytes. |
| Returned `SessionKeys` | Strict 48-byte parser copies S-ENC/S-MAC/S-RMAC, each 16 bytes | Supplied symmetric cryptogram 56-byte response rejection remains HSM-IMP-008. No accepted tail interpretation, production boundary or completed live HSM handshake. |

The technical manual explicitly distinguishes HSM secure messages from APDUs and two long-lived HSM keys
from Core SCP03's three static keys. Similar S-key names supply no object interchange or authority to
apply every GP APDU rule to the HSM connector. The host/HSM integration must enroll matching material,
authenticate its peer, finish host authentication and manage protected command state. Omission-only
three-key equality proves isolated applet calculation for laboratory input, leaving that live task open.

## Qualification and late-delta map

The prior YubiHSM run's 13 integration methods and four final scoped probes retain their exact historical
scope. Serial 103/Linux/USB SmartCard success, the original 56-byte parser failure, the failed cancellation
expectation and subsequent measured 6982/Failed plus fresh-session recovery remain distinct outcomes.
Neither the 14-second drain nor a caller cancel timestamp identifies native interruption or a touch
timeout cause. Older firmware, NFC, other OS, Native AOT runtime and genuine HSM/connector authentication
remain unqualified. No device or connector command ran in this boundary.

On the user's ready Windows CCID/HID handoff, pin its revision and revisit: PC/SC native admission and
borrowed-buffer terminal publication; open/partial failure; transaction/disconnect/context release;
known-member ownership and selector discovery; HSM creation/failure/disposal; LIST→CALCULATE and
RESET→SELECT gaps; presence callback/terminal outcomes; response-copy cleanup. HID changes matter only
through mixed discovery/shared infrastructure here: HSM operation transport remains SmartCard. Preserve
earlier evidence rather than relabeling it current Windows acceptance. The unfinished work was not
inspected. PIV physical deferrals remain unchanged.
