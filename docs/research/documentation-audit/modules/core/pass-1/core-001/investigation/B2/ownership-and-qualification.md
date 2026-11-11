# Linux ownership and qualification handoff

## Current source state machine

The source pin is `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. This extends B1's vocabulary and
uses the implementation's actual resources/tasks rather than treating “canceled” as one state.

| Observation | FIDO hidraw input/output | OTP feature reports |
| --- | --- | --- |
| Claim admitted | Public typed connect obtains every known member ID; discovery obtains one interface lease carrying PID admission. Slot transfers the original token before calling owner open. | Same public/discovery transfer, to a different owner. Direct IHidInterface facades take no grouped registry claim. |
| Native owner acquired | GCHandle roots owner before native acquisition. One context-suppressed dedicated worker opens O_RDWR/NONBLOCK/CLOEXEC, validates metadata and stores reactor token before waiting for Activation. | Separate rooted/context-suppressed worker opens O_RDWR/CLOEXEC. No reactor or descriptor-size query is needed for fixed feature GET8. |
| Open wait canceled | Token requests shutdown; failed/canceled factory waits for the original worker's native return and cleanup. Activation failure retains token through actual quiescence. | Token checked before and after open; late acquisition is checked-closed on original worker before failure escapes. Invalid injected handle owns no descriptor. |
| Ordinary operation admitted | Send and receive share one slot; overlap throws, no ordinary backlog. Send owns a copy before publication. Receive installs one actual TCS or consumes an owned queued report. | GET and SET share one slot; SET owns a copy before publication, GET allocates eight bytes on the worker. No ordinary queue, replay, replacement worker or feature reactor. |
| Operation wait canceled | Before send dispatch, output is skipped and owned copy cleared. After dispatch, task waits for native output. Pending reader cancellation detaches only the matching reader; native registration continues, late report can reach successor. | Pre-dispatch token prevents GET/SET. Once dispatched, cancellation keeps task and slot pending through native return; a successful call may still return success. |
| Native pending | Output borrows owner copy on worker; bounded nonblocking input on poller borrows staging. Thirty-two attempts/four EINTR bound attempts, not elapsed time. | Synchronous feature ioctl borrows worker-owned staging/copy. The native call can remain pending indefinitely. |
| Task terminal | Actual send TCS is terminal after native borrow and zeroing. Actual receive TCS is terminal under lock on delivery/cancel/terminal wake. Successful received array transfers to caller. | Actual operation TCS becomes terminal after native borrow, with failed/partial GET cleared or successful array transferred. SET copy is cleared first. |
| Terminal input | Queue is cleared; pending reader faults; removal is requested. EOF, bad typed length, overflow and terminal native status stop further input. Existing accepted send is not aborted. | Shutdown/worker fault closes admission. Ordinary operation errors alone do not automatically dispose a still-healthy connection; protocol recovery remains B5/applet responsibility. |
| Drained | Original worker waits exact token Quiescence, then synchronously ReleaseReference. Snapshot/ready callback/terminal callback return are included; Activation, failed wake or terminal task are insufficient. | Admitted GET/SET has returned and published its task outcome before worker enters close. |
| Release proven | L1 cached CloseOnce result says Released after actual close callback. Native0 or Linux -1/EINTR/EIO permit original claim disposal. Diagnostic error can still fault Dispose after release. | Same L1 evidence classification on separate owner. No native-close retry or new worker. |
| Uncertain claim retained | Reference release failure, unexecuted/pending/unknown close or claim-disposal failure retains root and original registration and marks it unrecovered when possible. | Unknown native/claim release similarly retains rooted owner/token. |
| Reopen | Known overlapping member acquisition is allowed only after original claim release; stale token/generation or repeated Dispose cannot release a successor. Disjoint view IDs remain independent per B1. | Same process-local known-member limit. A completed failed disposal is not generic reopen permission. |

The shared reactor has its own lifecycle. It holds a SafeHandle reference from registration publication
through explicit owner release. One poller runs while needed; idle retirement closes only the private
notification eventfd after poll exits. Admissions arriving during retirement retain pending Activation
on that same run, without overlapping replacement threads/fds. A failed wake terminalizes the reactor
but may leave actual Quiescence pending until a genuine later poll return. Owners continue observing the
same token. A quiescent token still retains its descriptor reference until explicit release. Failed
eventfd retirement prevents replacement; it grants no permission to close a live device snapshot.

Thread markers and identity differ: the reactor routes by registration object and immutable generation;
FIDO's callback also rejects a mismatched generation. Production owner calls release on its original
worker. The registration guard enforces post-quiescence/non-reactor/exactly-once use, rather than storing
and checking one arbitrary caller thread ID. No application callback executes on the production poller.
Read completions use asynchronous TCS continuations. A fake directly invoking OnReady demonstrates
owner logic, while real poll tests separately exercise native readiness and actual fd reuse.

## Buffers and finalization

- Raw public caller input remains caller-owned. Built-in Linux sends copy it synchronously before
  worker publication; callers keep input stable through the call and clear secret input after actual
  operation terminality. This conservative public rule also works with custom providers that borrow.
- Owner SET/output copies and L1 native zero-ID stack/pool frames are cleared in finally before
  completion/pool return. Native feature GET copies to caller destination only after exact native9.
- FIDO staging and abandoned/rejected/overflowed queued reports are cleared. Delivered exact arrays
  transfer to the caller; connection disposal leaves them intact. Built-in array-backed read-only
  views can be cleared through MemoryMarshal.TryGetArray and the actual segment. A cleared second copy
  leaves the original unchanged. Custom providers need their own result-ownership contract.
- Device status/descriptor/firmware metadata is ordinary nonsecret data. Authentication material,
  challenge-response secrets and secret-derived outputs require their semantic owner to clear them.
  Expected report capacity or a modeled length is not an observed transfer count or a clock budget.
- Facade finalizers request shutdown and catch failures; they neither wait for native drain nor prove
  release. RequestShutdown takes the admission lock, so “nonblocking” here means no native/drain wait,
  not lock-free or deadline-guaranteed invocation. The owner root excludes facade references, enabling
  the isolated facade-finalizer tests. The listener is different: its live thread delegate captures
  the listener, so ordinary running abandonment need not make that listener finalizable.
- Unexpected worker-loop failures fault actual pending completions, stop admission and clear owned
  copies. The still-running original worker attempts safe cleanup; first error survives proven release.
  An interrupted quiescence wait quarantines uncertainty rather than resuming cleanup on another thread.

## Discovery and A/B1 boundary dependencies

SharedDeviceInfoRead owns PID admission, worker admission, interface lease, connection and read pipeline.
DiscoveryDeviceInfoReader's budget ends an outer wait. Once transferred, PID admission follows the native
lease through close, including quarantine. Before registration, the admission holder disposes untransferred
admission; after transfer the native owner alone releases. Retirement prevents queued obsolete opens;
already-admitted work retains its original lifetime. ProtocolDeviceInfo borrows the connection, builds and
disposes the protocol, and supplies no independent discovery scheduling. Direct and published composite
provider resolution reach the same native owner. The exact current actor split is C002's doc dependency.

The new A handoff is an input, not a second metadata audit:

| A candidate / original unit IDs | Narrow B2 source check and consequence | Owner / reopening trigger |
| --- | --- | --- |
| CORE-A-C003; `core-001:unit:a6f48f999e63cf51`, `core-001:unit:922f5a4c9bacb503` | DeviceInfo.CreateFromTlvs stores tag07's ReadOnlyMemory view as ChallengeResponseTimeout; content hash reads that span. Native owner release does not confer independent ownership of parsed TLV-backed metadata. | A/C retain TLV/scalar decision; reopen metadata/cache stability and Management examples before adding decoded-list disposal. B2 supplies no secret-zeroing diagnosis or source edit. |
| CORE-A-C017; `core-001:unit:765edbf523cfd3fc`, `core-001:unit:35a8b4441222950f` | Identity cache checks cached.Pid; metadata cache restores Info by PhysicalIdentityKey without a PID comparison. HID PID comes from DescriptorInfo.ProductId, the enumerated USB product ID; known positive values are admitted. It is not a process ID or serial number. | A retains conditional stable-interface-ID/PID-change candidate and test. Reopen Linux discovery cached-evidence claims when validity is decided; no failure of native close follows from cache reuse. |
| CORE-A-C021; `core-001:unit:35a8b4441222950f` | Any non-null successful DeviceInfo is cached, including null serial. Same-PID cache hit differs from timeout/exception retry; fresh one-shot finder evidence differs from retained monitor/finder evidence. | A owns retry/expiry prose and policy. Reopen discovery convergence/serial-less troubleshooting after omitted-serial success vector and invalidation controls. |

The exact A finding definitions remain in `investigation/A/findings.json`; their accepted IDs remain
unassigned. Their units keep A ownership and enter no B2 denominator. Product source/range hashes remain
the original inventory authority. A-C008 owns discovery budget arithmetic/retry/history comments;
B2-C002 only records the Linux current-actor dependency.

B1-C002/C003/C004 cover PC/SC enumeration/listener partial lifetimes and cadence; B1-C009 covers post-open
PC/SC slot logging. Those can affect a mixed scan/monitor or borrowed pipeline, but they are neither new
Linux owner defects nor grounds to release a Linux claim early. B1-C005 retains Windows/custom HID failure
release behavior; B1-C007 retains the active Linux terminal-wake narrative correction; B1-C010 retains the
separate PC/SC ambient-context decision. B5 owns full CTAPHID/OTP framing, terminal presence and recovery.
C owns prompts/credential services and the shared Python history register; B2 acquires no prompt or PIN
service and makes no credential-request guarantee for arbitrary downstream applet calls.

## Inherited observations and platform handoff

`fixes-wave-001:CORE-001` remains changed/unresolved physical, with A primary and OATH boundary dependency.
The original path was Linux `hid:/dev/hidraw7:0001`, serial 9681620 (5.4.3) with key 125 also attached.
The single-key OATH success and modern owner architecture do not identify why that old read stayed active.
Current serial 103 read-only acceptance is a different fixture and topology. Preserve original IDs/log
availability and request a matching separately authorized two-key trace rather than declaring closure.

`audit-v2-20261022-20261031:PCSC-stall-finalizer-flake` retains its undiagnosed historical state. B2's source
inspection and fake worker/finalizer cases do not diagnose the earlier PC/SC stall/watchdog. The original
native-stall routing names macOS source paths with B3 and PC/SC paths with B1; B4 owns generic loader/shim
ABI qualification. These are explicit dependency handoffs, not exclusions hiding B2 unread units. B2 has
one exclusion only: the fully read, commented-out LinuxUdevScan historical unit, recorded by exact ID in
unit-dispositions.json. No assigned platform unit is excluded to B3/B4.

## Evidence chronology and release envelope

| Evidence | Exact scope retained | B2 conclusion |
| --- | --- | --- |
| Upstream L6b latest `20261111T123927Z`, recorded HEAD adf3a911582c3664f3c02c13a3eb668706b9471b, source d5bb1304912aff955f41c759cfa10447c910296698174ad44522dfb896eaa4dc | Omarchy x64; serial 103, 1050:0407; Management 5.8.0 and FIDO/CTAP 0.0.1 kept distinct. 38 native checks/runtime plus six read-only physical modes/43 checks/runtime, four intended controls, no operator work. | Real selected-key read-only historical acceptance exists. The older blocked checkpoint does not describe this latest checkpoint. It remains scoped to those recorded source/inputs/fixture, not all Linux/current merged source. |
| Reviewed integration source 90b0e9ab7582316099d37d9bc8e93beca1dd64fe8ce9c44e7edb32042b7533aa | JIT/AOT native-only 38/runtime, four exact intended controls, 31 gate tests; zero physical operations. Restored compile/property mapping and ArchiveSourceRef-qualified retained outputs belong to integration validation. | Separate source/native verification. B2 ran none and does not revalidate the integration checker framework or relabel its outputs. |
| Current B2 pinned source read | 63 complete primary files/1,148 primary units plus 71 shared; exact hashes and source/test oracles | Documentation investigation only; no compiler, build, test, formatter, native or device execution. |

The 38-check native mode uses real sockets, eventfd and poll through production report/reactor/FIDO code,
with synthetic 64-byte metadata and a managed held-output seam. It does not exercise hidraw feature GET/SET,
USB driver error returns or all registry claims. The actual close-success probes differ from fake
EINTR/EIO/EBADF callback outcomes. No test name or simulated failure establishes a physical kernel error.
Upstream selected-key physical modes cover read-only GetInfo/Management/status/reopen, not complete FIDO
registration/authentication, UV/PIN, PIV operations, reset/provisioning or all firmware. Operator Selection,
active-cancel and removal remain unexecuted in those retained checkpoints. Source-only limits, native
resource/quiescence evidence, physical selected-key success, consumer compilation and approved latency/
CPU/allocation/worker budgets remain separate dimensions. Current source contains managed native C#;
the C/C++ lane is empty in this checkout, and no archived/native-shim source was invented to fill it.
