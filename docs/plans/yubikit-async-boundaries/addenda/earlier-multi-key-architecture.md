# Earlier architecture: multi-key execution and capacity

Status: historical architecture approved for the earlier broader scope on 2026-09-22,
including the explicit macOS IOKit expansion. Archived before replacing the active
Gate 2 with its single-key-first proposal. Shared capacity and cross-key isolation
remain deferred in the [multi-key addendum](multi-key-capacity.md).
Source base: `a7f2cae8c32ad6e0ada55e404f85442f6a266f6c`, freshly matched to `origin/yubikit`.

This is the architecture review view of the
[master acceptance plan](../../../../2026-09-21-yubikit-async-boundaries-ISA.md).
Its criteria and evidence remain authoritative. The architecture choices below record
the earlier approval; they are not blanket requirements for the reduced iteration.
The public tiers, macOS direction and native lifetime findings remain inputs to revalidation;
exact types, signatures, budgets, and native exports belong to Gate 3, and the
implementation sequence belongs to Gate 4. No implementation is authorized yet.

## Fit

### A1 — keep the existing layer responsibilities

| Existing area | Proposed responsibility |
|---|---|
| Public device factories, applet sessions/interfaces, and raw sessions | Retain the approved consumer experience and visible protocol flow. Coordinate affected public changes across applets; retain the shared options/cancellation/ownership grammar. |
| Public typed raw connections | Retain direct packet/transmit operations, caller-owned lifetime and external implementations. They are not a second public logical-exchange queue. |
| Core protocol owners | Keep framing, chaining, secure-channel state, prompt pairing, cancellation messages, and recovery here. Native adapters cannot infer applet success or replay uncertain commands. |
| Core transport/platform implementations | Own asynchronous open/report/lifecycle execution and native request lifetime. Keep platform details behind the public typed connection contracts. |
| Existing guards and registry | Keep logical admission, one-session binding, physical ownership and shared teardown as distinct responsibilities; adapt their interactions where native lifetime demands it. |
| NativeShims | Use only for native calling-interface/platform adaptation and a minimal callback-lifetime bridge where justified. It does not own protocol policy or a competing scheduler. |
| Existing test/build tooling | Own boundary discovery, contract profiles, measurements and evidence; add no shipping dependency on the scanner or manifest. |

No new shipping project, service, actor framework, operation-command hierarchy, or
universal native-operation base class is proposed. Shared code stays in Core and
remains mechanical. Applet wire flow and WebAuthn delegation through Fido2 remain visible.

### A2 — internal asynchronous report boundary, public typed raw access

Replace the synchronous implementation contract currently represented by
`IHidConnection` with an internal genuinely asynchronous report boundary. Make its
low-level opening contract and necessarily connected implementation types internal
as a coordinated change, rather than preserving a second public platform-report layer.
Exact members and visibility closure must be reviewed at Gate 3.
That review must name `FindHidInterfaces`, `IFindHidInterfaces`, `IHidInterface` and their
consumer replacements explicitly; raw-operation retention alone does not settle
every public enumeration use case. Include the public `HidDeviceListener`,
`ISmartCardDeviceListener` and `DesktopSmartCardDeviceListener` delegate surfaces:
their visibility or observed callback threading must be settled explicitly.

Keep `ISmartCardConnection`, `IFidoHidConnection`, `IOtpHidConnection`, the public
connection lifetime/composition capability, and all three raw-session families.
Consistent signature changes are allowed, but neither public raw-access tier is removed.
Public/protected factory and base-type dependencies must remain accessible.

This cut follows the approved surface-minimization policy. It does not mean all
synchronous methods are forbidden: local work and deliberately retained synchronous
compatibility and disposal boundaries remain classified contracts. A broader unrelated Core
visibility cleanup is not part of this proposal.

### A3 — native completion where available; one bounded blocking mechanism

| Route | Proposed execution direction |
|---|---|
| Windows FIDO reports | Owned overlapped input/output completion. Preserve access and report-ID/length rules; cancellation requests never substitute for terminal completion. |
| Windows OTP feature reports | Bounded blocking adaptation initially; use an overlapped direction only after separately verifying GET or SET. Preserve zero-desired-access opening. |
| macOS report input | Persistent native event delivery and bounded owned report reception; no caller-run-loop pumping. Prefer dispatch lifecycle if the agreed support floor permits it; an owned run-loop thread is the fallback within this architecture. |
| macOS output/feature reports | Verified report callbacks per direction, otherwise explicitly classified bounded blocking adaptation. Event-handler quiescence and report-operation completion are separate release conditions. |
| Linux report input | Nonblocking reads with readiness notification and explicit wake-up for registration, cancellation and shutdown. Start with the existing poll/eventfd vocabulary; retain generation identity across descriptor reuse. |
| Linux output/feature reports | Bounded blocking adaptation, separate from the read reactor. Readiness does not establish that write/control operations cannot block. |
| Smart-card connections on all desktop platforms | Bounded execution for blocking establish/connect/transmit/transaction/end/disconnect/release work. Monitoring retains its independent context and owned status-wait lifecycle. |
| Other opening, discovery and cleanup boundaries | Classify each operation. Potentially blocking work uses its approved execution owner; existing persistent monitoring/event owners are retained where appropriate, not forced into short-job workers. |

#### macOS: expanding the current IOKit usage

Current source already uses a real slice of the IOKit HID API: `IOHIDManagerCreate`,
device matching/arrival/removal callbacks, `IOHIDDeviceRegisterInputReportCallback`,
and direct `IOHIDDeviceGetReport`/`SetReport` for feature and output reports
(`src/Core/src/Transports/Hid/MacOS/MacOSHidDeviceListener.cs:83-108`,
`IIOKitDeviceLifetime.cs:165-168`, `MacOSHidIOReportConnection.cs:127-146`,
`MacOSHidFeatureReportConnection.cs:77-106`). "Expanding IOKit usage" means two
named, evidence-gated moves beyond that surface, not adopting IOKit for the first time:

1. Replace per-call caller-run-loop pumping in `MacOSHidIOReportConnection.GetReport`
   (`:100-124`, `CFRunLoopRunInMode` on the caller's own thread) with a persistent
   dispatch-queue lifecycle owner — `IOHIDDeviceSetDispatchQueue`, `Activate`,
   `Cancel`, `SetCancelHandler` — where the agreed deployment floor supports it
   (available from macOS 10.15; the floor itself is an open S0 decision, not assumed here).
2. Evaluate `IOHIDDeviceSetReportWithCallback`/`GetReportWithCallback` as a
   replacement for the current synchronous `IOHIDDeviceSetReport`/`GetReport` calls
   used for output and feature reports, verified independently per direction. An
   unverified direction keeps the classified bounded-blocking fallback; this does not
   block freezing the input-delivery change.

Both moves are already named in the master plan's macOS implementation section
(`2026-09-21-yubikit-async-boundaries-ISA.md:413-414`); this subsection makes them
explicit at Gate 2 instead of leaving them compressed into the two rows above.

**Not currently proposed:** native multi-criteria device matching
(`IOHIDManagerSetDeviceMatchingMultiple`, referenced but unused at
`Native/MacOS/IOKitFramework/IOKitHid.Interop.cs:70`) to replace the current
match-all-then-filter discovery approach (`MacOSHidDeviceListener.cs:92`,
`MacOSHidInterface.cs:65`). That is a discovery-shape question, not a blocking/async
boundary, and is out of scope here unless the user asks for it to be folded in.

These are implementation directions, not verified platform capabilities. Backend
selection happens before state-changing work, never as a replay after ambiguous failure.

The one internal blocking executor uses owned blocking-worker threads and accepts
synchronous native jobs only. It bounds
active workers and all pending admission, retains capacity until native return, and
keeps discovery saturation from consuming the configured interactive reservation.
Awaiting an asynchronous delegate is not a dedicated blocking worker.

Reserve bounded cleanup/control progress separately from new interactive/discovery
admission. Native completion and callback-quiescence observation must not require a
fresh job in the saturated executor. Subsequent blocking cleanup may require reserved
capacity; it cannot run on a handle whose native operation is still unresolved.
The approved capacity/failure budget must include a single stalled cleanup alongside
healthy connections: that one call cannot consume every progress reservation in the
supported scenario. Keep its occupied slot until it really returns; do not manufacture
capacity by abandoning calls or adding unbounded replacement threads. Gate 3 must
state the supported number of simultaneously stalled calls and what callers observe
when the finite reserve is exhausted. This is a finite isolation guarantee, not
recovery from unlimited hung drivers. The exact reservation topology, admission/exhaustion behavior and numeric budgets
must be presented at Gate 3 before implementation. They are not new public queueing policy.

### A4 — explicit ownership, using the current guards

| Responsibility | Existing owner to preserve / proposed extension |
|---|---|
| Logical exchange admission | `ExchangeGuard` refuses overlap and retains the admitted exchange through recovery. Worker admission does not replace it. |
| Session and physical ownership | `ConnectionSessionGuard` binds one session to a connection; `DeviceConnectionRegistry` owns factory-created physical-interface leases. Preserve discovery deferral to waiting connections. |
| Native request and connection lifetime | The platform connection retains its handle/generation; a platform-local operation owns request state, buffers, cancellation registration and exactly-once terminal cleanup. Share contract tests rather than inheritance. |
| Teardown | `DisposalGate` shares completion; the transport/protocol teardown owner establishes drain and native quiescence before physical ownership can be released. |

**A teardown exception is not proof of release.** The current `DisposalGate` releases
its lease in `finally` even on failure. Preserve shared completion but review/adjust
lease-release ownership for unresolved native work. Such work retains or quarantines
its handle, buffers and lease; the interface cannot be reopened merely because an
await failed. A managed/native lifetime owner must keep the quarantined request,
handles, buffers and callback context strongly reachable and retain the required native
references even after callers drop the connection; garbage collection/finalization
cannot release storage still in use. Gate 3 selects the rooting/handle-retention mechanism.

Quarantine must have a defined exit: late native completion/quiescence permits cleanup
and lease release once safe. Removal/replug does not itself prove old work complete;
define how old and replacement generations are distinguished without releasing a
still-live identity. If work never terminates, retaining ownership for the process
lifetime is possible. Gate 3 must expose that unrecovered state clearly to callers,
rather than reporting only the ordinary “another live session owns this key” case,
and specify late-completion, dropped-reference and replug tests.

### A5 — seams beneath production adapters and independent evidence

Use narrow internal native-operation seams where deterministic tests need control
over submission, completion, cancellation, removal and release. Exercise actual
production adapters through them, including the prefix before their first await.
Reuse existing seams where they fit; exact extensions versus sibling seams are Gate 3.

Current coverage is partial: `IWindowsHidReportAccess` is synchronous; `IIOKitDeviceLifetime`
deliberately excludes report execution; `ILinuxHidEventSource` covers monitoring;
`ISCardApi` covers discovery/status, not full connection/transaction lifetime.
Fake typed connections remain useful for protocol tests but cannot alone prove the
production adapter nonblocking. No public injection framework is proposed.

Keep a bounded semantic inventory check in test/build tooling: identify native
imports/dynamic lookup/callback boundaries, actual blocking members and known
direct/interface wrappers. Unknown or unresolved relevant sites require disposition;
empty input, stale records, missing profiles and zero-test execution fail the gate.
This is not whole-program proof and does not replace controlled responsiveness tests.
Native runtime, ahead-of-time execution, packaged consumers and hardware retain
separate evidence grades. Introduce platform execution checks during each migration;
the final closure phase reconciles evidence rather than introducing the first checks.

## Endpoints

None. This work changes a local library's device operations, not a network service.

## Data

No database or durable application-data migration. Runtime state is bounded and
connection/operation-owned: admission, handle generations, pending work and report
buffers where a persistent producer requires them. Callback data is copied or its
ownership transferred before native storage can be reused; overflow faults the
connection instead of losing reports silently. Responses retain their public lifetime.

The master plan remains the acceptance authority. A generated boundary manifest and
test/native/hardware/measurement artifacts supply evidence; no duplicate runtime
registry or per-agent acceptance system is added. Gate 3 settles formats and locations.

## Flow

### Opening and initialization

1. Validate locally and acquire managed physical ownership on the factory route.
2. Dispatch any potentially blocking open to its execution owner; return control to
   the caller while opening remains pending.
3. Construct/bind the requested session and await required protocol initialization
   explicitly, including FIDO channel setup. Configuration cannot synchronously wait
   for asynchronous initialization. Preserve safe lazy initialization on applicable raw routes.
4. Publish only a ready object. Failed or cancelled partial opening retains ownership
   until acquired native resources are safely released.

### Session operations and raw operations

1. Applet/raw sessions admit one logical exchange, validate/encode synchronously and
   perform visible protocol work. Typed raw operations enter at the connection layer;
   their caller owns framing, sequencing, exclusion and recovery as documented.
2. Native execution is submitted under its bounded worker or native-completion owner.
   Cancellation that wins before dispatch submits nothing. After dispatch, retain
   buffers, handles and worker capacity through actual native completion, then release
   them at their respective safe boundaries; protocol recovery may still own the exchange.
3. The protocol owner handles response draining/recovery, then parsing/outcome and
   sensitive-buffer cleanup. Public terminal completion means no further use of that
   operation's borrowed input. An uncertain mutation is never automatically replayed.

For FIDO cancellation, retain the existing between-read sequence: after a received
keepalive, the already-admitted exchange observes cancellation and may send one
protocol-permitted cancel before requesting the next report. The request is fully
transmitted first, and the terminal response is still drained. Do not cancel the read
needed for draining merely because the caller cancels. This preserves the current
public raw-connection sequencing obligation; it does not require simultaneous send/read
support from external implementations. Cancellation is not instantaneous and cannot
interrupt an indefinitely pending read through this protocol policy. Changing to a
cancel send while a read is pending would require an explicit public concurrency
contract, updated documentation/consumer tests, and renewed architecture review.
Exact cancellation-result races remain Gate 3 work and require protocol-source verification.
Register the no-further-keepalive case explicitly: the admitted exchange remains
pending until the read terminates or transport removal/fault supplies the terminal
disposition; caller cancellation alone does not prove recovery or release ownership.

Native callbacks, reactors and blocking workers deliver data/completion only; they
run no application callback or continuation inline. Prompt delivery stays in the
existing protocol/applet orchestration and preserves request/resolution pairing.
This is a target rule, not a claim about current monitoring: existing listener delegates
can run on the native callback stack. Inventory those delegates, distinguish internal
rescan signals from externally supplied callbacks, and change delivery or visibility
where needed. Keeping a listener's event owner does not exempt its callback path from
the isolation tests.

### Transactions

Acquire and release transactions asynchronously through the owned smart-card lifecycle.
After a submitted begin, observe native return before public completion; do not detach
the caller while leaving an unowned native transaction. If begin succeeded, either
return an owned scope or complete its end/release before publishing cancellation/failure.
Gate 3 decides the public scope/signatures and success-versus-cancellation precedence.

The current `Task.Run` followed by `GetAwaiter().GetResult()` blocks the caller; it
does not abandon an already-running begin when its scheduling token is cancelled.
The proposed drain obligation prevents a future regression rather than claiming an
existing late-success transaction leak. Portable transmit/transaction cancellation
does not assume that `SCardCancel` interrupts the operation.

### Removal and disposal

Close admission, retain the active exchange through recovery or an explicit fault
disposition, observe native terminal completion and callback quiescence, then perform
ordered native cleanup and release physical ownership. Removal wakes readers and
retires the generation; late completions still clean up their original resources
but cannot affect a replacement connection.

`DisposeAsync` must not enter a blocking native prefix. Retained synchronous disposal
is a documented blocking facade over the same release conditions. Existing synchronous
SafeHandle finalizer cleanup is an inventoried fallback requiring lifetime review,
not a substitute for orderly asynchronous teardown or permission to free live callbacks.
Synchronous disposal must not wait on the same owned worker/event-delivery thread
needed for its completion, nor run reentrantly from the operation being drained.
Gate 3 must trace and test this wait graph, including managed continuation progress
under shared-pool pressure and the absence of an application-context dependency.
Dedicated native workers alone do not prove that every async teardown continuation
is independent of the shared pool. No finite completion guarantee is made when an
application itself blocks every available execution resource.

### External connection implementations

Built-in adapters are the target of the native responsiveness/lifetime proof. External
implementations remain supported and must obey documented connection contracts;
the library cannot guarantee their native behavior on their behalf. Session binding
works by connection identity without requiring implementer state. Physical-interface
exclusivity is guaranteed on factory-created connections, not arbitrary custom handles.
Gate 3 must include consumer examples/fixtures demonstrating the retained public path
and document required send/read, cancellation, borrowed-memory and disposal behavior.

## External

- Existing desktop operating-system device stacks and the pinned NativeShims package
  remain the native dependencies. No application dispatcher, executor, new service,
  environment variable or secret is required by this architecture.
- A minimal macOS bridge is conditional on callback/lifetime and deployment evidence.
  Define any new native calling contract before consuming it. Verify a compatible
  native source/package pair and old/new consumer policy before native edits; the
  candidate 1.16.1 producing revision remains unproven. Do not use the unrelated
  static-linking branch as the implementation base.
- The observed macOS 14.0 binary floor is not the approved support floor. Exact
  platform versions, fixtures and budgets stay explicit prerequisites. Missing
  hardware evidence cannot be converted into managed-test success.
- Alternative smart-card backends remain the separately approved exploration scope;
  they do not enter default factories during this production migration.

## Alternatives and approval boundary

- A uniform blocking-worker solution for every platform would simplify submissions
  but discard available completion/readiness and still require callback/lifetime
  handling. Use it only for the classified blocking directions.
- Keeping incidental per-call `Task.Run`, or an unbounded thread per connection or
  timed-out call, cannot provide the required capacity/isolation accounting.
- A universal actor/state machine, command hierarchy or common native-operation
  inheritance tree duplicates existing ownership and obscures platform lifetime rules.
- Protocol-only mocks miss the blocking bodies under test. A general-purpose analyzer
  framework would exceed the scoped inventory problem; neither is the sole test strategy.

Gate 2 approval selects A1–A5 and the flow/ownership rules above. It does not approve
exact source changes, numerical budgets, native exports or the candidate slice graph.
Gate 3 specifies those contracts; Gate 4 schedules small end-to-end slices. The first
working adapter path must justify shared code before wider fan-out; platform-local
operation state and report reception are not mandatory upfront generic infrastructure.
The orchestrator remains the single acceptance/integration owner and engineers retain
exclusive file ownership plus serialized shared-contract updates.

If later evidence requires removing a retained public use case, changing ownership
semantics, adding an application-supplied executor, or replacing persistent/nonblocking
input with caller-thread waits, stop and re-open the affected approval gate. Selecting
one of the explicitly conditional platform fallbacks is not silent architecture drift;
record its reason and require the same behavioral/native evidence.

## Source anchors and evidence limits

All paths below are relative to the worktree, inspected at the source revision above:

- `src/Core/src/Devices/HidConnectionSlot.cs:48-70`: synchronous opening inside an
  asynchronous-looking wrapper; `src/Core/src/Protocols/Fido/Hid/FidoHidProtocol.cs:48-67`:
  synchronous configuration wait plus existing async initialization.
- `src/Core/src/Devices/DeviceConnectionRegistry.cs:155-215`: connection/discovery
  priority; `src/Core/src/Sessions/ConnectionSessionGuard.cs:38-64`: binding custom
  connection implementations by identity.
- `src/Core/src/Devices/DisposalGate.cs:40-64`: inline teardown prefix and unconditional
  lease release after teardown, including faults; native quiescence is not encoded here.
- `src/Core/src/Transports/SmartCard/UsbSmartCardConnection.cs:86-114,165-185,198-234`:
  blocking transaction acquisition and synchronous end/release paths.
- `src/Core/src/Protocols/Fido/Hid/FidoHidProtocol.cs:260-321`: caller intent is
  distinct from the drain token; existing cancel handling runs between received keepalives.
- `src/Core/src/Native/Desktop/SCard/SCardContext.cs:36` and `SCardCardHandle.cs:37-38`
  in the same directory: synchronous native SafeHandle release.
- `src/Core/src/Transports/Hid/MacOS/IIOKitDeviceLifetime.cs:22-39` and
  `src/Core/src/Transports/SmartCard/ISCardApi.cs:19-32`: deliberately partial native seams.
- `docs/architecture/raw-access-tiers.md:19-34,136-159` and
  `docs/architecture/applet-public-api.md:3-34`: current public access and applet grammar.

This gate used source inspection and read-only architecture review. Existing regression
results and package inspection are recorded in the master; no new responsiveness,
native runtime, hardware or performance evidence is claimed by this proposal.

Abbreviations: FIDO — Fast Identity Online; HID — human interface device;
OTP — one-time password. These name the existing authentication/report routes.
