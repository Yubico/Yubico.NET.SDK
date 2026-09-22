# Architecture: one key's asynchronous lifecycle

Status: revised single-key Gate 2 approved by the user on 2026-09-22; independent design review passed.
The revised single-key-first product gate is approved. No implementation is authorized.
Source base: `a7f2cae8c32ad6e0ada55e404f85442f6a266f6c`, matching fetched `origin/yubikit`.

This is the architecture view of the [master plan](../../../2026-09-21-yubikit-async-boundaries-ISA.md).
The [earlier architecture](addenda/earlier-multi-key-architecture.md) and
[earlier program design](addenda/earlier-multi-key-program-design.md) remain references. Their shared pools,
capacity credits and multi-key guarantees are deferred in the
[addendum](addenda/multi-key-capacity.md), not prerequisites for this iteration.

Delivery update: the user subsequently requested incremental implementation after
reviewing [first-slice pseudocode](03-program-design.md), without whole-effort upfront
program/slice specifications. Later-gate references below describe responsibilities,
not a requirement to finish the entire epic's design before the first slice.

## Fit

### A1 — retain the public tiers and existing responsibilities

Keep device discovery/identity, applet sessions and their domain contracts, public raw
sessions, typed raw connection operations, and external connection implementations.
Equivalent public concepts keep consistent creation, cancellation, ownership and
disposal conventions. Breaking changes and internalization remain permitted; exact
signatures and visibility closure are revised Gate 3 decisions.

The lower-level synchronous report interface becomes an internal asynchronous boundary.
It is distinct from the retained public typed raw connections. Keep local validation,
encoding, parsing and bounded cryptographic computation synchronous.

Reuse the existing ownership roles:

| Owner | Responsibility retained |
|---|---|
| Applet/protocol code and ExchangeGuard | Visible framing, chaining, prompts, cancellation/recovery and refusal of overlapping logical exchanges. |
| ConnectionSessionGuard | One live session on a connection, including caller-provided connections. |
| DeviceConnectionRegistry | Existing physical-interface leases on managed factory/discovery paths; safe release and same-key quarantine. |
| DisposalGate | One shared teardown outcome; a fault is not authority to release native ownership. |
| Platform connection/native operation | Handles, report buffers, callback context, accepted operations, generation and terminal native completion. |

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
publishing cancellation/failure. Exact precedence and public scope signatures are Gate 3.

If a native call never returns, retain its thread/state and unresolved ownership.
Do not create a replacement thread, free the handle, or report successful disposal.
An asynchronous disposal can remain pending and its synchronous facade can block
indefinitely; this is an explicit safety/liveness limit. A faulted teardown can report
an error with ownership retained. Positive late completion can still permit cleanup.

This bounds work **per connection lifetime**. Registry-managed attempts against a held
or quarantined key must be refused before creating another owner/worker. Direct expert
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
Gate 3 must trace the disposal wait graph and test context/pool-pressure behavior without
claiming progress when an application blocks every available execution resource.

### A3 — preserve native completion and the explicit macOS expansion

The worker adapts blocking calls; it does not replace genuine native completion or
readiness. Platform event delivery is a separate responsibility and must not need the
blocking worker to become free merely to observe completion or removal.

| Route | Single-key direction retained |
|---|---|
| Windows FIDO reports | Owned overlapped ReadFile/WriteFile completion. Immediate-success notification rules must prevent double completion. CancelIoEx requests cancellation; terminal completion controls storage release. |
| Windows OTP feature reports | Preserve zero-desired-access opening. Keep feature calls on the connection's blocking owner unless GET/SET directions are separately verified for overlapped execution. |
| macOS input | Persistent IOKit device event delivery and bounded owned reports, replacing per-call caller-run-loop pumping. Prefer IOHIDDeviceSetDispatchQueue, IOHIDDeviceActivate, IOHIDDeviceCancel and IOHIDDeviceSetCancelHandler where the agreed deployment floor permits. |
| macOS output/feature reports | Evaluate IOHIDDeviceSetReportWithCallback/GetReportWithCallback independently by direction; use the connection's blocking owner for an unverified direction. |
| Linux input | Nonblocking read plus readiness/wake signaling using poll/eventfd. Readiness ownership is independent from blocking writes/control calls; descriptor generation and deregistration remain required. |
| Linux output/feature reports | Run potentially blocking writes/ioctls on the connection's blocking owner, never the read-readiness loop. |
| Smart-card routes | Adapt blocking context/connect/transmit/transaction/end/disconnect/release calls through their connection owner. Keep status monitoring's independent context/lifetime. |

Cancellation capability is classified per route, not invented for the single worker:
Windows overlapped requests have CancelIoEx; Linux readiness waits have explicit wake
signaling; macOS input has event cancellation/quiescence. These do not cancel unrelated
blocking feature calls, writes, or smart-card operations. Portable smart-card transmit
and transaction begin have no assumed bounded timeout or abort; SCardCancel is used
only for its independently owned monitoring/status context. Those calls must drain,
possibly indefinitely, before scope release/connection shutdown can execute. Callback
report timeout behavior must be verified per direction before it supplies a deadline.
No caller cancellation or ordinary slowness alone is classified as a permanent fault.
The current Task.Run(...).GetAwaiter().GetResult() begin blocks its caller; the new
awaitable improves caller responsiveness without claiming a new device-side abort.

#### macOS expansion and report lifetime

The macOS input change is an explicit expansion of existing IOKit usage, not merely
moving the old caller-run-loop pump onto an incidental pool thread. An owned native
run-loop event owner remains a documented fallback if the dispatch lifecycle cannot
be used. Account for that event owner separately from the blocking worker; there is
no claim that a connection consumes only one operating-system thread in total.

Report completion and event quiescence are separate. For callback report operations,
stop new submissions, drain accepted GET/SET requests, then cancel dispatch delivery,
observe its quiescence, close and release. Do not stop delivery while it is still
needed to observe a pending report's completion. Root contexts and buffers throughout.
Copy input callback memory before native buffer reuse. Bound report reception and
fault on overflow; the old 256-report proposal is not automatically selected.

A minimal native bridge may own dispatch blocks/queue lifetime without exposing their
layout to managed code. Exact exports, timeout conversion, native-source/package
provenance and direction-specific runtime probes remain Gate 3/prerequisite work.
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
descriptor. Revised Gate 3 must specify that outcome and its tests.

Synchronous disposal invoked **from** an owned native worker/event callback must not
wait for that same worker/event path to make progress. A separate caller thread may
use the documented blocking facade, potentially indefinitely. Likewise, do not drain
an exchange from the callback whose return that exchange requires. Keep application callbacks and
continuations off native callback/worker stacks. Completion sources use asynchronous
continuations; exact reentrancy checks are Gate 3 work, not a reason to recreate the
deferred shared execution-context framework wholesale.

### A5 — keep discovery integration focused on this key

Retain existing discovery/monitor owners and their current admission policies rather
than replacing them with global work classes or reservation credits. Their prior
source findings remain recorded. In particular, an async LongRunning delegate does
not provide dedicated-thread execution after an await; do not claim it does.
Retaining those owners does not exempt listener delivery from callback isolation.
Revised Gate 3 must explicitly settle the visibility or delivery-thread behavior of
FindHidDevices/IFindHidDevices/IHidDevice and HidDeviceListener/ISmartCardDeviceListener/
DesktopSmartCardDeviceListener; their current callbacks cannot simply be declared safe.
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
The exact route and slice order belong to Gate 4.

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
dispatcher or supplied executor is required. The macOS bridge needs a verified native
base/package and deployment contract before implementation. Do not assume the package's
observed macOS floor settles product support. Other platform/reader fixtures, exact
firmware, callback behavior and baseline measurements remain explicit prerequisites.
Alternative smart-card backends remain later exploration work.

## Approval boundary and evidence

This revised Gate 2 selects connection-local execution and lifetime ownership, the
retained native platform directions, and focused same-key integration/testing. It
does not approve exact new types, signatures, buffer limits, files or slice order.
The reduced Gate 3 must reuse proven findings without copying the earlier shared-pool
program design. A need for cross-key reservations or a global scheduler reopens scope.

Source anchors at the recorded base: `HidConnectionSlot.cs:48-71` performs synchronous
open inside an async-looking wrapper; `UsbSmartCardConnection.cs:98-102` blocks for
transaction begin; `DisposalGate.cs:59-64` releases a lease in finally after faulted
teardown; `MacOSHidIOReportConnection.cs:100-117` pumps the caller's run loop;
`ProtocolDeviceInfo.cs:399-426` and `:477-480` contain the existing discovery scheduling
and admission. Full paths and source/native references remain in the earlier documents.
No new implementation, benchmark, native-runtime or hardware evidence is claimed here.

FIDO — Fast Identity Online; HID — human interface device; OTP — one-time password.
IOKit is Apple's device-driver framework. These identify the existing transport routes.
