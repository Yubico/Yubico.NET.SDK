# B5 local candidates and unchanged-path decisions

These are **proposed local IDs**, not accepted Core allocations or nine independent runtime defects.
All product paths remain unchanged. Documentation is `CANDIDATE`, implementation is `SOURCE-ONLY / NOT
FIXED` or `NEEDS-DECISION`, independent review is `PENDING`, and compilation/runtime/platform/device
validation is `NOT-RUN`. Citations use [the source register](sources/register.md); the machine index
retains exact original unit IDs and every associated unchanged path.

## CORE-B5-C001 — State and version provenance need a tier-specific contract

`IApplicationSession.cs:21–35` describes a detected/overridden effective firmware and initialized
application protocol. `RawCtapHidSession.CreateAsync:40–58` and `RawOtpHidSession.CreateAsync:43–61`
instead set `IsInitialized=true` with **no wire I/O**, and their only operation never updates inherited
`FirmwareVersion`. After a successful first exchange whose protocol learned 5.8.0, the raw session still
exposes 0.0.0. That default also passes every `Feature` threshold through major-zero policy. Simple
RawSmartCard likewise starts unknown; its explicit Configure or SCP overload records supplied firmware.
Security Domain uses supplied or assumed 5.3.0, rather than a detected version. These are source-visible
exceptions to the generic interface prose, not evidence that the underlying key has any particular
firmware or capability.

**Documentation:** describe effective configured versus protocol-discovered/raw versus assumed/unknown
version, and distinguish raw session attachment/readiness from lazy channel/status initialization. Explain
that `IsSupported` remains a firmware-only query after disposal; `ApplicationSessionDisposalTests:279–292`
deliberately asserts that behavior. Firmware and ConnectionType remain readable while initialized/auth
flags become false at disposal admission. `IsAuthenticated` means successful optional protocol setup,
including device-only SCP11b authentication; it grants no applet management/PIN/UV authorization.

**Decision:** choose whether raw HID should publish its discovered protocol version, or retain an
unknown/configured-only property with precise documentation. Do not repair it during this run. Raw
version propagation would need cancellation/disposal tests and a reviewed property contract.

`SessionCreationOptions.cs:25–39` needs the retained-parameter exception beside the general initialization
borrow: Security Domain stores `_scpKeyParams` for reset. Management and Security Domain ignore rather
than retain UserPresencePrompt. The carrier is init-only and configuration value-carried, but this is no
deep clone of key/private-key/certificate/prompt objects. The asynchronous initializer closure can retain
the options during creation; the intended no-retention claim is about the successfully returned session.
WithPreferred's test checks copied settings, not this whole lifecycle.

**Reopen:** IApplicationSession, ApplicationSession, all three raw session XML, SessionCreationOptions,
Core README/CLAUDE, `docs/SDK-HOUSE-STYLE.md` Session Shape, `docs/architecture/raw-access-tiers.md`,
`docs/migration/v1-to-v2.md`, `docs/v1-to-v2-comparison.md`, `docs/v2-highlights.md`. Later applet owners
reopen their factory/feature/reset XML when effective-version or retained-parameter policy changes.
Keep A-C003/C017/C021 attached: metadata timeout memory aliases, metadata-cache ProductId validity gap,
and cached successful serial-less reads prevent a new universal “stable fresh metadata” assurance.

## CORE-B5-C002 — CTAPHID cancellation and raw command grammar

`FidoHidProtocol.ReceiveTerminalPacketAsync:334–379` sends one CANCEL on any keepalive after caller
cancellation or prompt failure, then continues receiving with the guard's uncancellable token. Cancellation
alone while ReceiveAsync is pending sends nothing. A device that stops producing reports can keep that
operation pending indefinitely; disposal's optional terminal wake is separate from protocol CANCEL and
from native shutdown/release. A valid final response is fully assembled before callback/cancellation is
surfaced. A successful **CBOR** response wins a caller-cancel race. A prompt failure still wins over that
success after drain, so a failed callback is not rollback of an authenticator side effect.

CTAP 2.0 §8.1.9.1.5 and 2.1/2.3 §11.2.9.1.5 assign CANCEL to ongoing **CBOR** work, require its canceled
request's normal response to carry 2D, forbid a response to CANCEL itself, and require ignoring CANCEL
without active CBOR/on an inactive CID. The vendor-command fixtures at FidoHidProtocolTests:558–622 and
694–741 demonstrate managed drain/reuse under a script, not a normative vendor-command “cancel
acknowledgement.” Re-plug necessity and universal immediate usability claims exceed those assertions.
The original §KEEPALIVE prose itself says MSG while §CBOR also permits keepalives; preserve that source
wording discrepancy rather than silently treating every vendor command as a cancelable CTAP ceremony.

**Runtime/decision:** `IsCtapSuccess:384–387` compares command exactly to 10. Requests accept encoded
vendor bytes and normalize expected response at line 406; an encoded CBOR 90 request correlates successfully
but loses the same late-success treatment. Define accepted command grammar before changing this.
Also, `RawCtapHidSession.SendAndReceiveAsync(11, ...)` waits for a correlated response which a conforming
idle authenticator must never send. It is an application-agnostic **request/response** helper, not a
general control-command API. Reissuing INIT on its current CID has protocol resynchronization semantics;
do not describe arbitrary control commands as ordinary applet operations. Direct Tier 2 callers own those
special sequences. No API addition or universal command rejection is authorized here.

**Reopen:** RawCtapHidSession, IFidoHidProtocol, FidoHidProtocol Receive/IsCtapSuccess rationale,
FidoHidProtocolTests cancel summaries, Core README/CLAUDE cancellation sections, raw-access-tiers FIDO
and cancellation sections, and CTAP2/WebAuthn cancellation/deadline guidance through their boundary
editors. Trigger: accepted command normalization/control-command policy, new cancel scheduling, or new
exact device evidence. Test encoded 90 success/non-success and no-further-keepalive independently.

## CORE-B5-C003 — INIT/control validation and failed-exchange recovery

`AcquireCtapHidChannelAsync:154–178` checks length>=17 and nonce, correctly allowing future extension,
then accepts any assigned uint CID. The original framing reserves 0 and FFFFFFFF. Firmware bytes 13–15
are vendor-defined device version, distinct from protocol version byte 12 and capabilities byte 16. This
SDK neither stores capability flags/protocol version nor rejects reserved assigned channels. Treat
reserved-CID validation as an explicit robustness decision, not a fabricated host-MUST clause.

Outgoing payload-size validation also has a specific timing boundary: SendCommandAsync initializes a
missing channel before SendRequest checks the application's payload length. An oversized first request
can therefore perform INIT before rejection, although it sends no oversized application message. Keep
entry cancellation, argument rejection and absence of all wire I/O separate in any proposed test/prose.

`ReceiveTerminalPacketAsync` accepts keepalive BCNT 0 (deliberately tested) and any longer BCNT; a report
with BCNT > 7609 is rejected only in terminal-response validation, not keepalive. ERROR with BCNT 0 invents
7F, while the standard table specifies one error byte. Exact 64-byte/channel/init-bit and continuation sequence
validation are strong existing checks, but they do not justify a universal malformed-control rejection
claim. A malformed continuation or send/read failure releases ExchangeGuard without invalidating the
FIDO protocol/channel. There is no PC/SC-style recovery latch or OTP-style abort latch here. Queued tails
can therefore survive a failed partial FIDO exchange; current guidance correctly says dispose/reopen
after uncertainty. A later successful bind is bookkeeping, not recovery proof.

**Reopen:** FidoHidProtocol handshake/control validators/recovery rationale, IFidoHidProtocol and
RawCtapHidSession contracts, Fido tests, Core/raw-access troubleshooting. Trigger: selected malformed
control/CID acceptance policy or recovery implementation and explicit tail/reopen oracle. Keep applet
CBOR schema/status classification with CTAP2 and firmware interpretation with the appropriate applet.

## CORE-B5-C004 — Public memory and platform admission wording

`IFidoHidConnection.cs:23–27` says the physical interface admits exactly one live SDK connection and
native handle before native open. That is a scoped in-process known-member registry contract for
device-managed opens. Direct expert report opens have no grouped claim; external implementations can
have different admission/handle shapes. Existing method XML already carefully scopes macOS cancellation.
Retain that precision, add the Linux owner bounds when useful, and let B4 complete pinned Windows source
review. Do not flatten these three routes into a universal connection-interface implementation.

Raw send XML says outgoing SDK copies clear and caller input is unchanged, but should also state its
**borrow duration** and no-whole-request snapshot. FIDO data remains borrowed across lazy INIT and each
awaited packet send; continuation data is copied incrementally. OTP waits for lazy initialization before
copying into the padded 64-byte buffer; it is not copied at public method invocation. Keep source stable until the
actual operation task is terminal, even when an outer WaitAsync is canceled/times out. Early caller
clearing can alter unsent bytes. Successful raw HID result arrays survive session disposal and belong to
caller sensitive-data handling; clear actual returned storage, not a new ToArray copy.

**Reopen:** IFidoHidConnection type XML, RawCtapHidSession/RawOtpHidSession Send XML, raw-access-tier and
Core examples, IOtpHidConnection via B4/B1 owner, later applet public inputs/results. Trigger: native
route or input-copy policy change; explicitly supplied Windows delta requires fresh affected-path review
after user readiness. Compatibility adapters ignore tokens and perform synchronous native calls before
returning a completed task; native owner semantics cannot be inferred from those Task signatures.

## CORE-B5-C005 — Sensitive received report copies outlive cleared accumulators

FIDO `ReceiveResponse:282–325` clears failed partial **responseData**, while `ReadResponseDataAsync:
414–432` copies init/continuation payload from connection-returned report arrays and never clears those
arrays. OTP `ReadDataPacketsAsync:327–390` clears/returns its rented accumulation buffer and transfers
an independent successful response copy; the individual feature-report arrays are left intact. The
compatibility adapters return arrays directly; B2/B3 typed owners transfer successful arrays and do not
clear them later on connection disposal. This leaves extra copies of authentication/token/decrypted or
secret-derived response material beyond either applet response cleanup or the raw caller's result
cleanup. Public status/version/DeviceInfo metadata does not acquire a secret-zeroing requirement.

The retained-packet tests observe **outgoing** arrays, and the response factory/pool tests observe the
accumulator. They do not observe these source report arrays; clearing a later clone cannot establish
their erasure. B6's ApduResponse constructor/input staging cleanup is a separate implementation owner.

**Decision/runtime:** define ownership/clearing of received ReadOnlyMemory at the typed connection to
protocol boundary, including external implementations and aliasing. Do not cast arbitrary borrowed
read-only memory writable without that contract. Build deterministic retained-input-report vectors for
success, continuation failure, prompt failure and cancel-drain. Handle a status/report slice escaping
ReadStatus separately from consumed secret-bearing packets. No new blanket zeroing promise is proposed.

**Reopen:** Fido/Otp protocol receive and result rationale, connection return XML through B4/native
owners, raw result guidance, C's memory/security/interaction prose and CTAP2/YubiOTP backend/result
contracts. Trigger: chosen report owner and actual retained-buffer observations, not only passing old
outgoing-copy tests. This is separate from applet model-owned response clones and A-C003 metadata aliasing.

## CORE-B5-C006 — Exchange drain is not operation-lifetime serialization

`ApplicationSession:42–44` explicitly records no in-flight operation lease. Guards protect individual
protocol exchanges; a public workflow can finish one exchange, do local work or await an application
callback, and attempt another after disposal has started or a different public operation ran. Disposal
closes protocol admission and drains its admitted exchange, then detaches the session/optionally disposes
its connection. It does not await every public task, parser, post-exchange cache write, prompt/service
callback or detached callback-created task. Public state flags false at admission is useful but is not
proof those tasks have ceased accessing applet-owned buffers. Existing “sessions refuse overlapping
operations” prose should say logical exchanges and require callers to await complete public workflows.

ConfigureProtocolAsync checks holder and readiness but has no session ThrowIfDisposed check immediately
before publishing effective Protocol/version/auth after awaited SCP setup. The normal factory does not
return its session early, so callers cannot ordinarily race initial creation with disposal. Reset/reinit
and future friend callbacks need their own operation policy; do not claim a freely reproducible public
factory leak from this isolated interleaving. B6 owns SCP partial-state cleanup; applet boundary owners
own reinit and post-exchange state.

Reentrant disposal is another distinct wait graph. FIDO protocol detects its own exchange with AsyncLocal
for sync **and async** Dispose; OTP and ExchangeGuard do not. An OTP presence callback that awaits its
own session disposal waits on the exchange that is waiting for that callback. Calling sync disposal is
already warned against; explicitly keep callbacks non-reentrant and explain async self-wait too. Whether
to reject all operation self-disposal is a maintainer decision. ConfigureAwait(false) does not suppress
ExecutionContext/AsyncLocal flow; Linux worker SuppressFlow concerns a different native-owner boundary.

**Reopen:** ApplicationSession disposal/presence helper XML, ExchangeGuard rationale, IProtocol, Core
README/CLAUDE concurrency, raw-access-tiers, `docs/usage/user-interaction.md`; all nine boundary
workflow/callback/disposal descriptions. Trigger: whole-operation lease, self-disposal policy, callback
tracking or applet state-lifetime change. No new semaphore/framework or runtime fix is authorized.

## CORE-B5-C007 — OTP polling, NEO failure policy and response vocabulary

Readiness polls use a three-second Stopwatch loop with **no await delay** on unchanged status/busy
returns (`WaitForReadyToReadAsync:264–320`). Native GET duration is outside that deadline; synchronous
or immediately completed reads can spin and keep invocation on the caller thread. Write readiness uses
five-ms backoff up to the local one-second condition; touch uses 250 ms and 14 seconds, restarts readiness
clock afterward, and gives a same-poll completion priority over new cancellation. These are current SDK
policies, not exact device timeout guarantees. Source-source comparison with vendor C/Python shows
different waiting/rejection policies; agreement on bytes does not settle intended cadence.

NEO refresh catches every exception when no reset failure latched (`EnsureInitializedUnderGuardAsync:
101–125`), not just an expected command rejection. A programming-sequence read failure before wire or a
recovered transport failure can therefore be swallowed and `_initialized` become true. Pinned Python
catches CommandRejectedError only. Record a scoped failure-classification decision and meaningful
no-wire failure oracle, rather than general NEO support claims.

Comments name Java-style versus legacy C# phases inconsistently; explain actual flag/sequence logic.
`OtpConstants.StatusOffsetProgSeq` is an offset in the status **report**, not a byte inside a status byte.
`ResponseLengthOffset` (64) and `ResponseCrcOffset` (65) describe an assumed layout not used by the current inbound
parser: data response CRC position is command expectedLength. The outbound 70-byte frame has a 64-byte payload, command,
uncomplemented CRC16 of the padded payload in little-endian order, and three filler bytes. Ten reports each carry seven frame bytes plus control; this is neither
an eight-byte payload nor a universal 70-byte inbound response. CRC is corruption detection, not authentication.

**Reopen:** OtpHidProtocol polling/NEO/frame comments, OtpConstants offsets, IOtpHidProtocol/RawOtp
response contract, OTP protocol tests, raw-access guide and YubiOTP HID boundary prose. Trigger: chosen
cadence/rejection policy or verified changed firmware framing. Preserve OTP's existing one-abort/no-replay
and reset-failure refusal; broad catches are not permission to hide a reset failure.

## CORE-B5-C008 — Test explanations and failure-path ownership

FIDO concurrency Configure is local. Its first held send is lazy broadcast INIT, despite the larger
payload comment saying the application continuation is owed. Later wire-order/payload assertions remain
useful, but holding that INIT is not an observed midpoint in application-message framing. The test named
Configure_OverlapsFirstUseOperation_ThrowsWithoutSecondInitialization actually calls Configure successfully
and asserts no second INIT. Keep executable names/assertions frozen in this documentation run; correct
explanations and refer any stronger vector/rename to separately authorized test work.

Several held helpers release gates only after assertions. Examples: FIDO concurrency lines 49–142, OTP
concurrency lines 39–85, RawCtap lines 52–73, RawOtp lines 58–86, RawSmartCard lines 100–140/216–240, ExchangeGuard held tests,
ApplicationSessionDisposal lines 119–149, UserPresenceNotification lines 149–167 and concurrent construction lines 75–106.
On assertion/cancellation failure, using protocol/session disposal can wait indefinitely for an
uncancellable exchange whose fake release is skipped. Some extension tests release in finally but only
await exchange/disposal afterward; a failed assertion skips observing/draining those tasks. Full fake
helpers and true inner/public task identities matter. A watchdog bounds the host, not cooperative cleanup.

**Documentation:** describe the actual scripted oracles; preserve useful lifetime rationale while removing
blanket physical corruption/re-plug/all-schedule proof and old development history. RawSmartCardNative
uses actual managed production owners below a fake SCard API, not actual Windows/Linux resource-manager
calls; B2/B3 active native proofs stay separately qualified. Raw OTP CRC test recomputes expected with
the same ChecksumUtils helper; it is a consistency assertion, not an independent CRC known-answer test.

**Reopen:** listed test methods and complete helpers, plus raw-access/Core test evidence summaries.
Trigger: separately approved harness cleanup/stronger oracles, new native/device runs, or accepted doc
correction. Hold release and actual task drain in finally before disposing coordination primitives.

## CORE-B5-C009 — Current API grammar and old comparator/comment residue

Assigned `v1-to-v2-map.yml:258–275` calls the device extension the **only** public construction entry,
yet eight applet static CreateAsync factories remain public. Prefer it as the standard owned-connection
entry and state the direct borrowing alternative. `FactoryShapeTests.ValidateFactory:152–190` checks
return, three parameters and options/token; with receiverType null it never verifies the direct
connection parameter's type. The asserted grammar is not complete runtime lifetime coverage. Assigned
async allowlist includes protected EnsureSupports entries that the DeclaredOnly Public Instance search
does not visit. They are stale inventory context, not a public enforcement API regression.

The assigned July 6 changelog says repository-diffed Added/Removed truth; current SnapshotDiff publishes
DeviceUpdated as well and is monitor-owned. Route this small wording dependency through SHARED/A.
Keep dated public-access decisions and older AOT records historical, with RawFidoHidSession's historical
name and then-current native qualification intact. Do not rewrite history into current evidence.

`ConnectionSessionGuard:31–34` compares Rust taking a connection by value and Python Session storing
itself on a connection without repository/full pin/passages. C's verified current Python manager is a
different identified source; it supplies no automatic proof of that unnamed Session implementation.
Initial shared-source inspection and focused exact local-pin search leave this provenance gap; retain
the C# rationale and mark/delete unsupported sibling authority through a fresh editor, rather than
cloning a vendor forest. ProtocolConnectionOwnership and SessionConstructionGuard test comments also
carry historical gap/fix-story narration that can be replaced with current test purpose.

**Reopen:** those exact migration rows/changelog, ConnectionSessionGuard and affected test type comments,
PublicApi ValidateFactory and allowlist comments where useful, Core session-shape documentation. Trigger:
actual public factory grammar change or supplied original comparator/full revision. Do not count unknown
historical comparator provenance as a current C# session-guard runtime defect.
