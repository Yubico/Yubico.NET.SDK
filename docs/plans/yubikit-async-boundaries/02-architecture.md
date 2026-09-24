# Architecture: one key's asynchronous lifecycle

Status: revised single-key Gate 2 approved by the user on 2026-09-22; independent design review passed.
The first smart-card lifetime slice is committed at `db7a1bf6`; macOS typed/direct
reports and listener teardown have since been verified at bounded grades in the
[master verification](../../../2026-09-21-yubikit-async-boundaries-ISA.md#verification).
Full epic acceptance remains pending (18/72 checked, including bounded ISC-19).
At current source checkpoint `fdedd61c` plus uncommitted continuation and
registry work, the Core-only inventory classifies 205 outstanding sites:
134 native imports, 3 native exports, 24 waits, 15 scheduling sites,
21 pre-task-return gaps, 6 callback registrations, 0 delegate conversions
and 2 unmanaged callback addresses. The test-link registry now has 23 required
operation rows/45 profiles (typed Mac FIDO 4, OTP 4, portable PC/SC 5, direct
input 4, direct feature 4, listener 2). It includes direct raw and listener
paths but does not prove complete public/platform coverage or check ISC-4.
Design baseline: `a7f2cae8c32ad6e0ada55e404f85442f6a266f6c`, which matched fetched `origin/yubikit`.
Reconciled 2026-09-23: approved A1/A5 policy remains unchanged; D30 delegates in-scope
route-local implementation without repeated microapprovals. Public raw invariants remain.

This is the architecture view of the [master plan](../../../2026-09-21-yubikit-async-boundaries-ISA.md).
The [earlier architecture](addenda/earlier-multi-key-architecture.md) and
[earlier program design](addenda/earlier-multi-key-program-design.md) remain references. Their shared pools,
capacity credits and multi-key guarantees are deferred in the
[addendum](addenda/multi-key-capacity.md), not prerequisites for this iteration.

Delivery update: the user subsequently requested incremental implementation after
reviewing first-slice pseudocode (now condensed in the
[program design](03-program-design.md)), without whole-effort upfront
program/slice specifications. Affected-route checkpoints retain the former detailed-gate
responsibilities without requiring the entire epic's design before each slice.

## Fit

### A1 — retain the public tiers and existing responsibilities

Keep device discovery/identity, applet sessions and their domain contracts, public raw
sessions, typed raw connection operations, and external connection implementations.
Equivalent public concepts keep consistent creation, cancellation, ownership and
disposal conventions. Breaking changes and internalization remain permitted; material
public-surface scope changes still require user escalation under D30.

The lower-level synchronous `IHidConnection` interface remains public and
unchanged for direct report access. Built-in macOS typed routes use internal
asynchronous owners, and direct report facades share those owners. Keep local
validation, encoding, parsing and bounded cryptographic computation synchronous.

**Implemented sequence, not renewed architecture approval:** the internal macOS
asynchronous seam coexists with synchronous public direct report methods.
Wholesale interface migration remains a separate ISC-50 consumer decision.

Reuse the existing ownership roles:

| Owner | Responsibility retained |
|---|---|
| Applet/protocol code and ExchangeGuard | Visible framing, chaining, prompts, cancellation/recovery and refusal of overlapping logical exchanges. |
| ConnectionSessionGuard | One live session on a connection, including caller-provided connections. |
| DeviceConnectionRegistry | Existing physical-interface leases on managed factory/discovery paths; safe release and same-key quarantine. |
| DisposalGate | One shared teardown outcome; a fault is not authority to release native ownership. |
| Platform connection/native operation | Handles, report buffers, callback context, accepted operations, generation and terminal native completion. |

The reviewed uncommitted PC/SC fix marks interrupted plain command/response
chains recovery-required. After protected state advances, first transport,
response-MAC or intermediate-fragment failures also latch refusal on the same
protocol, retaining the original exception without replay. Wrapped plain
SELECT continuation reports once through the secure hook; authenticated
terminal application error permits reuse. One independent-card command-MAC
profile holds a protected continuation through caller cancellation, then
validates the next protected command (accepted ISC-19 at managed grade).
Creating a new protocol on the borrowed raw connection bypasses per-protocol
refusal, so the raw caller must reopen after uncertainty. Latest full Core
1,492 passed/3 skipped, secure filter 159 passed/2 skipped; the latest
selected-key smart-card hardware attempts were blocked before open by
`SCARD_E_SHARING_VIOLATION`. ISC-37 still needs the global monitor/discovery
and custom-fallback lifecycle matrix; five connection rows are not its closure.

No actor framework, command hierarchy, universal connection-state base class, new
shipping project, process-global single-key lock, or global reservation framework.
The library continues enumerating other keys. This iteration verifies one selected
key at a time and makes no new cross-key fairness/scale guarantee.

### A2 — connection-owned execution for blocking native work

Create the transport's lifetime owner **before native opening**, including partial
construction and direct built-in raw factory paths. It owns native resources until
checked release; it is not created anew for each operation.

For classified blocking native calls, give that owner one lazily created, background
worker thread. Reuse it for open, ordinary native work and close. Accept at most one
ordinary native operation at a time, with no backlog of ordinary operations. Reject
overlap; do not quietly serialize competing public operations. Jobs are synchronous
native work, not asynchronous delegates or managed sync-over-async waits.

Disposal registers one coalesced shutdown request rather than competing for an ordinary
operation slot. After accepted work and platform quiescence, checked blocking cleanup
runs on the same owner. Concurrent disposal callers share completion. Shutdown does
not need global cleanup credits, a spare worker, or a second queue of release jobs.
Successfully released owners terminate their worker; failed-safe opens terminate it
as well. DisposeAsync returns before native waiting or thread joining occurs.

An owned transaction scope also needs a **bounded release intent**: ending a transaction
is not necessarily closing the connection. Accept and coalesce that scope's end request
even while an ordinary call occupies the slot, close admission to further ordinary work
for that scope, then execute end/disposition on the same owner after the accepted call
terminates. Never execute end concurrently with an unresolved transmit or reject/discard
the release merely because the worker is busy. Connection shutdown and scope disposal
share exactly-once end state. At most one active transaction's end intent and one
connection shutdown intent are retained; this is not a general control-job queue.
If a cancelled begin later succeeds, return an owned scope or finish its end before
publishing cancellation/failure. Exact precedence and public scope signatures are settled
in the affected-route walkthrough.

If a native call never returns, retain its thread/state and unresolved ownership.
Do not create a replacement thread, free the handle, or report successful disposal.
An asynchronous disposal can remain pending and its synchronous facade can block
indefinitely; this is an explicit safety/liveness limit. A faulted teardown can report
an error with ownership retained. Positive late completion can still permit cleanup.

This bounds work **per connection lifetime**. Registry-managed attempts against a held
or quarantined key must be refused before creating another owner/worker. Direct report
factories and external implementations retain their documented concurrency responsibilities;
this proposal does not claim a global bound on arbitrarily many explicit direct opens.
Process-wide admission limits belong to the deferred iteration.
Refusing overlapping calls on a built-in raw connection is an intentional observable
behavior within its existing caller-exclusion contract. Do not require a new global
scheduler or new simultaneous-operation capability from external implementations.

A gate over Task.Run would be shorter and could still track operation ownership; it
is not incapable of retaining state. The dedicated worker is preferred here because
blocking driver waits then occupy an attributable resource outside the shared managed
pool. That does **not** make all managed teardown continuations pool-independent.
The affected-route checkpoint must trace the disposal wait graph and test context/pool-
pressure behavior without claiming progress when an application blocks every available
execution resource.

### A3 — preserve native completion and the explicit macOS expansion

The worker adapts blocking calls; it does not replace genuine native completion or
readiness. Platform event delivery is a separate responsibility and must not need the
blocking worker to become free merely to observe completion or removal.

This six-row table is the authoritative architecture view of the platform/transport
combinations. HID direction entries are deliberately separate: FIDO input/output are
not OTP feature GET/SET. Common cancellation, recovery and release rules follow the
table rather than being repeated in every cell.

| Platform/transport | Classified execution and direction | Implementation/evidence status |
|---|---|---|
| Windows HID | FIDO input and output use owned overlapped `ReadFile`/`WriteFile` completion; immediate-success notification rules prevent double completion. `CancelIoEx` requests cancellation, while terminal completion controls storage release. Preserve OTP zero-desired-access opening; classify feature GET and SET independently, using the connection's blocking owner unless that direction is separately verified for overlapped execution. | Approved design only for this effort. Existing HID code/tests remain, but no Windows HID route has migrated and native-runtime/hardware evidence is pending. |
| macOS HID | FIDO input uses persistent IOKit event delivery and bounded owned reports instead of per-call caller-run-loop pumping. FIDO output and OTP feature GET and SET use connection-owned workers, not unverified callback APIs. | Typed FIDO/OTP and direct IO/feature paths have selected-key `.8` read-only native-AOT evidence. Real listener matching, removal and late-drain probes support bounded ISC-33 teardown acceptance. Public direct calls remain synchronous; other-host permission and universal coverage remain open. |
| Linux HID | FIDO input uses nonblocking read plus `poll`/`eventfd` readiness and wake signaling. FIDO output uses the connection's blocking owner when it may block. OTP feature GET and SET ioctls are classified independently and never run on the read-readiness loop. | Approved design only for this effort. Existing HID code/tests remain, but no Linux HID route has migrated and native-runtime/hardware evidence is pending. |
| Windows smart card | Apply the portable smart-card policy: adapt classified blocking context/connect/transmit/transaction/end/disconnect/release calls through the connection owner; keep status monitoring's context/lifetime independent. Platform release and monitor semantics require their own evidence. | Portable owner slice implemented and managed-tested. Windows native-runtime/hardware evidence remains pending. |
| macOS smart card | Apply the same portable smart-card policy without inferring that one platform's release/monitor result proves another's. | Portable owner and built-in async acquisition passed managed withheld-native probes; selected-key async transaction/read/reopen passed after earlier sharing contention. Other platforms and interrupted native release remain pending. |
| Linux smart card | Apply the same portable smart-card policy. Interpret release results by established platform evidence without changing the approved cleanup policy. | Portable owner slice implemented and managed-tested. Linux native-runtime/hardware evidence remains pending. |

These rows map to the existing parameterized ISC-4, ISC-8–20, ISC-26–40, ISC-44–46,
ISC-54 and ISC-57–64 obligations as applicable. They add no criterion, checkmark,
platform semantic, reservation number or route approval.

Cancellation capability is classified per route, not invented for the single worker:
Windows overlapped requests have CancelIoEx; Linux readiness waits have explicit wake
signaling; macOS input has event cancellation/quiescence. These do not cancel unrelated
blocking feature calls, writes, or smart-card operations. Portable smart-card transmit
and transaction begin have no assumed bounded timeout or abort; SCardCancel is used
only for its independently owned monitoring/status context. Those calls must drain,
possibly indefinitely, before scope release/connection shutdown can execute. Callback
report timeout behavior must be verified per direction before it supplies a deadline.
No caller cancellation or ordinary slowness alone is classified as a permanent fault.
The baseline used Task.Run(...).GetAwaiter().GetResult() for transaction begin.
The built-in connection now provides awaitable `BeginTransactionAsync`, verified
with native begin withheld; external implementations retain a synchronous
default fallback. No new device-side abort is claimed.

There is no blanket HID native-cancel promise after caller cancellation. A protocol
cancellation token can require continued recovery reads, including FIDO's retained
between-read/keepalive cancellation point, while native shutdown cancellation separately
stops or quiesces the platform event owner during teardown. Neither action by itself is
protocol recovery or proof of safe release.

#### macOS expansion and report lifetime

The macOS input change is an explicit expansion of existing IOKit usage, not merely
moving the old caller-run-loop pump onto an incidental pool thread. An owned native
run-loop event owner remains a documented fallback if the dispatch lifecycle cannot
be used for a demonstrated technical reason; it is not a fallback solely for preserving
an older binary target. Account for that event owner separately from the blocking worker; there is
no claim that a connection consumes only one operating-system thread in total.

Report completion and event quiescence are separate. For callback report operations,
stop new submissions, drain accepted GET/SET requests, then cancel dispatch delivery,
observe its quiescence, close and release. Do not stop delivery while it is still
needed to observe a pending report's completion. Root contexts and buffers throughout.
Copy input callback memory before native buffer reuse. Bound report reception and
fault on overflow; the old 256-report proposal is not automatically selected.

A minimal native bridge may own dispatch blocks/queue lifetime without exposing their
layout to managed code. Exact exports, timeout conversion, native-source/package
provenance and direction-specific runtime probes remain affected-route prerequisites.
The known report-timeout documentation/source discrepancy remains unresolved.
Device matching and discovery identity policy are not expanded by this change.

### A4 — release proof and same-key recovery remain mandatory

Keep caller cancellation, native terminal completion, protocol recovery and physical
release as distinct events. Before dispatch, cancellation that wins prevents native
submission. After dispatch, retain operation storage until native completion. Public
terminal completion means no more access to that operation's borrowed input.
The protocol/caller owns borrowed input; the transport/native-operation owner borrows
and pins it only through its actual native use, or copies into operation-owned storage.
A submitted transport task must not complete early on cancellation while native code
still reads borrowed storage. Consequently, the protocol's await/finally zeroing occurs
after the transport relinquishes that borrow. Operation-owned copies are zeroed by
their native owner after last use. Detached discovery must own its data separately;
abandoning an await is not the transport operation's terminal completion.

An exchange retains admission through response draining/recovery. Do not replay an
uncertain mutation. Retain FIDO's between-read/keepalive cancellation point and at most
one legal cancel frame; do not require simultaneous raw send/read from external
implementations. Cancellation while a blocking native call/read is pending may have
to wait for its permitted completion boundary. No immediate device-side abort is promised.

Managed/public completion, native terminal completion, protocol recovery/reuse
classification and physical-claim release are four distinct observations. They may become
known at different times; uncertainty can retain admission, resources or quarantine the
claim, but never authorizes mutation replay.

The physical lease may release only after the relevant native work and callbacks are
finished and release is established. A task fault, cancellation, removal notification
or SafeHandle.Dispose return alone is not proof. Keep unresolved native state rooted
independently of a caller retaining the public wrapper. A finalizer may request safe
shutdown, never impersonate native completion or free active callback storage.

Use the existing registry's ownership entry for a generation-scoped unrecovered state,
not a new process-wide quarantine scheduler. Failed teardown and subsequent acquisition
must make that state distinguishable from ordinary live-session contention. Late release
can clear only its original claim; replug alone cannot authorize cleanup of old state.

Preserve the distinction between successful physical release and a cleanup error:
platforms such as Linux can report a close error after releasing a descriptor. Report
the error, apply the platform's release evidence, and never retry close on a reused
descriptor. The affected-route checkpoint must specify that outcome and its tests.

Synchronous disposal invoked **from** an owned native worker/event callback must not
wait for that same worker/event path to make progress. A separate caller thread may
use the documented blocking facade, potentially indefinitely. Likewise, do not drain
an exchange from the callback whose return that exchange requires. Keep application callbacks and
continuations off native callback/worker stacks. Completion sources use asynchronous
continuations; exact reentrancy checks are affected-route work, not a reason to recreate the
deferred shared execution-context framework wholesale.

### A5 — keep discovery integration focused on this key

Retain existing discovery/monitor owners and their current admission policies rather
than replacing them with global work classes or reservation credits. Their prior
source findings remain recorded. In particular, an async LongRunning delegate does
not provide dedicated-thread execution after an await; do not claim it does.
Retaining those owners does not exempt listener delivery from callback isolation.
The affected-route checkpoint must review its selected route's callback and removal
delivery behavior even when discovery and public visibility remain unchanged. The proposed
sequencing default leaves `FindHidInterfaces`/`IFindHidInterfaces`/`IHidInterface` visibility
unchanged. A broader visibility migration requires a separate walkthrough and user
approval. Current `HidDeviceListener`/`ISmartCardDeviceListener`/
`DesktopSmartCardDeviceListener` callbacks cannot simply be declared safe.
Preserve public manager enumeration/events and the retained public raw-access use cases.

Adapt connection opening and release to the new lifetime owner. Preserve current
discovery/connection priority and generation/epoch rules. If a discovery read budget
expires or teardown fails while native work still owns this key, quarantine that held
claim and wake connection waiters with the appropriate failure. Do not release the
physical lease simply to wake them. Late native release can permit a fresh probe;
quarantine must not be reported as ordinary transient contention on every scan.

The lifetime owner also brackets short-lived enumeration acquisitions where classified
blocking work requires it; an example of an existing synchronous owned job is
FindPcscDevices.FindAllAsync's LongRunning synchronous enumeration delegate
(`src/Core/src/Transports/SmartCard/FindPcscDevices.cs:35-62`), not ProtocolDeviceInfo's
async delegate. Retain such a job only where its release/ownership contract qualifies.
Do not create one global single-key executor for
enumeration or move applet logic into a discovery thread.

### A6 — prove a production route before adding shared machinery

Use narrow internal seams below actual production adapters to withhold native work
and assert that the entry point returns its task first, including open and DisposeAsync.
Protocol-only fake connections do not prove that a platform adapter is nonblocking.
Prove one complete route's open/operation/failure/teardown path before widening changes.
The orchestrator proposes route order and presents each bounded route to the user for
approval before implementation.

Retain the source inventory and evidence distinctions. Implement only the contract
harness and verification support needed by the selected slices initially; map their
coverage explicitly. Whole-epic semantic inventory, cross-key capacity and production
closure criteria remain outstanding until independently satisfied, never silently waived.
Native runtime, ahead-of-time execution, hardware and managed tests stay separate
evidence levels. One-key evidence cannot establish multi-key fairness or isolation.

## Endpoints

None. This is a local library change, not a network service.

## Data

Runtime state is scoped to the connection/operation: native resources, one ordinary
operation, one shutdown request, generation/release evidence, and an input-report
buffer where the native platform produces reports persistently. No global worker
budgets, cleanup-credit store, generic job queue, new persistent data store or public
executor configuration is introduced in this iteration.

The master retains all 72 epic criteria. The revised slice plan will register the
single-key rows being exercised and leave broader obligations pending. K1–K3 remain
possible fixtures; choose one initially and identify it before hardware work.

## Flow

1. **Open:** acquire existing physical ownership on managed factory paths; create the
   lifetime owner before native acquisition; perform open asynchronously; initialize
   explicitly; publish only a ready connection/session. Failed partial open performs
   checked cleanup on the same owner or retains its unsafe state/claim.
2. **Operate:** admit the logical exchange; run local protocol steps synchronously;
   await the platform completion path or the owner's synchronous native job. Raw
   callers retain their framing, exclusion and recovery responsibilities.
3. **Cancel/recover:** record intent, observe actual native completion, and finish the
   protocol's recovery/disposition before giving up the exchange. No ambiguous replay.
4. **Dispose:** close admission and record shared scope-end/shutdown intents; wait asynchronously for
   accepted work and platform quiescence; do checked cleanup on that owner's worker
   where it can block; release the registry claim only on proof; terminate the worker.
   Repeated disposal uses the same outcome and creates no new worker or cleanup queue.
5. **Removal/late completion:** keep old resources/generation until safe; wake pending
   readers appropriately; late completion acts only on its original owner. New calls
   cannot reuse a still-held identity simply because disposal reported a fault.

External implementations remain supported. The library documents their required
completion/lifetime contracts but cannot establish their native behavior on their
behalf. Session binding remains applicable to custom connections; physical registry
guarantees remain scoped to paths that acquire those leases.

## External

Existing desktop stacks and NativeShims remain the dependencies. No application
dispatcher or supplied executor is required. The current pin is private-published
`1.18.1-async.8` with five new macOS exports, produced by native `71a23cd0` in
[workflow 35955737172](https://github.com/Yubico/Yubico.NET.SDK/actions/runs/35955737172),
package SHA-256 `c85c56f7a41c6999b48b1a5fdcd82c56fbfb3e5ee6ff18ccc64a41144f760403`.
Fresh private-feed restore and seven packaged consumers passed, not Windows/Linux
device execution. The earlier `.2` preview lacked its dirty-file hash manifest.
Original 1.18.0 macOS artifacts
target minimum 12, while the .NET 10 upstream
support matrix currently starts at macOS 14 and the approved dispatch APIs are available
from 10.15. These facts are distinct; D28 selects modern dispatch across the current
upstream-supported systems and does not require an old-API fallback solely for the binary
minimum. Other platform/reader fixtures, exact firmware, callback behavior and baseline
measurements remain explicit prerequisites.

| Earlier signed 1.18.0 package evidence | Architectural meaning |
|---|---|
| Restore and managed/local native-crypto tests pass; macOS arm64 Native AOT publish links the static shim and PCSC.framework. | Confirms dependency consumption and local cryptographic execution, not device driver, PC/SC or HID hardware behavior. The published host was not executed. |
| macOS x64/arm64 retain all 11 `Native_SCard` exports and unchanged PC/SC ABI/dependencies; all packaged native platforms add static runtime archives. | No observed smart-card ABI regression, but Windows/Linux/Intel runtime and hardware remain unverified. |
| Tag `1.18.0` (`cb5275e…`) is inspected source; package metadata has no producer commit. | Local `.2` preview builds from this tag; the cited dirty-file hash manifest is missing, and the tag does not bind the original signed package to its producer. |

The `.2` preview's read-only and pending-receive probes are historical. Later
`.8` selected-key typed/direct reports and listener removal have separate evidence
grades in master Verification. Earlier `.3` private-feed access failure is not a
current restore blocker. Managed seams on macOS cannot prove the proposed
Windows FIDO overlapped route on Windows hardware.
Alternative smart-card backends remain later exploration work.

## Approval boundary and evidence

This revised Gate 2 selects connection-local execution and lifetime ownership, the
retained native platform directions, and focused same-key integration/testing. It
does not approve exact new types, signatures, buffer limits, files or slice order.
Each affected-route checkpoint must reuse proven findings without copying the earlier
shared-pool program design. A need for cross-key reservations or a global scheduler
reopens scope.

At historical `db7a1bf6`, synchronous HID opening and caller-run-loop pumping
still existed *before* the macOS migration; neither describes the current direct
report implementation. Historical discovery and smart-card anchors remain in the
earlier documents. Newer evidence is in the master, not inferred for other platforms.

FIDO — Fast Identity Online; HID — human interface device; OTP — one-time password.
IOKit is Apple's device-driver framework. These identify the existing transport routes.
