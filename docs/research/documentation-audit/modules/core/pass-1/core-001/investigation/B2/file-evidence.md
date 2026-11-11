# Complete file read evidence

Every primary file was read in full, including helper implementations, complete tests, barriers and cleanup. These are source/assertion observations, with zero new executions. Unit IDs/ranges/hashes and exact candidate intersections are in unit-dispositions.json.

## `src/Core/src/Native/Linux/Libc/Libc.Interop.cs`

Reviewed every import/wrapper, flag and pollfd field: int descriptors, nint ssize_t, nuint count/request, preallocated SafeHandle for open/eventfd, CLOEXEC, scoped descriptor leases, pinning and immediate errno capture. These describe the glibc ABI used here; they supply no all-architecture or musl attestation.

## `src/Core/src/Native/Linux/Libc/LibcFcntlConstants.cs`

Reviewed public numeric F_GETFL/F_SETFL/O_NONBLOCK constants in their actual fcntl call context; no native execution follows from constant equality.

## `src/Core/src/Native/Linux/Libc/LibcHelpers.cs`

Reviewed last-PInvoke-error message overloads: capture immediately on failure or pass an already captured code. Error-message lookup owns no descriptor and establishes no close outcome.

## `src/Core/src/Native/Linux/Libc/LinuxDescriptorLease.cs`

Reviewed both addref constructors, int extraction versus libudev-owned monitor fd, exactly scoped DangerousRelease and errno restoration. Current reactor independently holds a registration-long reference; future-reactor wording is stale (C002).

## `src/Core/src/Native/Linux/Libc/LinuxEventFdSafeHandle.cs`

Reviewed inheritance: eventfd uses the same fd-zero-valid checked close implementation, rather than a second release policy.

## `src/Core/src/Native/Linux/Libc/LinuxFileSafeHandle.cs`

Reviewed -1 invalid sentinel, fd zero, preallocation/Initialize, cached volatile release outcome, deferred borrower case, injected/actual native close, EINTR/EIO released diagnostics and conservative unknown result. SafeHandle closed state alone supplies no actual close proof; no retry occurs.

## `src/Core/src/Native/Linux/Linux.Libraries.cs`

Reviewed libc.so.6 and libudev.so literals. libc is glibc-specific; unversioned libudev name requires an available loader entry and creates the Ubuntu runtime-package gap C004.

## `src/Core/src/Native/Linux/LinuxUnmanagedDynamicLibrary.cs`

Reviewed lazy dlopen failure and dlsym/delegate conversion. Base loader owns SafeLibraryHandle; symbol lookup neither roots an arbitrary consumer delegate after owner disposal nor proves device support. General native-loading support remains B4.

## `src/Core/src/Native/Linux/Udev/LinuxUdev.cs`

Reviewed context constructor, invalid-handle/negative-result translation and idempotent explicit dispose. Current enumeration uses direct SafeHandles; the historical commented scan supplies no active subclass. Typographical rationale is optional cleanup, not new lifetime machinery.

## `src/Core/src/Native/Linux/Udev/LinuxUdevDeviceSafeHandle.cs`

Reviewed pointer-zero invalidity and device unref ownership: one owned reference; parent pointers remain borrowed from child scope.

## `src/Core/src/Native/Linux/Udev/LinuxUdevEnumerateSafeHandle.cs`

Reviewed pointer-zero invalidity and enumerate unref; list-entry pointers remain borrowed until enumeration owner disposal.

## `src/Core/src/Native/Linux/Udev/LinuxUdevMonitorSafeHandle.cs`

Reviewed pointer-zero invalidity and monitor unref. Monitor owns its socket; the listener retains monitor while polling. C003 corrects the import's stated C return signature without changing executable declarations.

## `src/Core/src/Native/Linux/Udev/LinuxUdevSafeHandle.cs`

Reviewed udev context unref on pointer handle; libc int descriptor rules are deliberately separate.

## `src/Core/src/Native/Linux/Udev/LinuxUdevScan.cs`

Read complete commented-out implementation, including borrowed list traversal and nested device scopes. Historical exclusion: contributes no compiled class, public API or present enumeration guarantee. Preserve bytes; active enumeration is LinuxHidInterface.GetList.

## `src/Core/src/Native/Linux/Udev/Udev.Interop.cs`

Reviewed all context/enumerate/list/device/parent/monitor declarations and borrowed versus owned comments. get_fd returns int, parent returns borrowed IntPtr, new/receive return owned SafeHandle. C003: ASCII claims and void monitor-unref C signature need correction; current callers copy strings inside owner scopes.

## `src/Core/src/Transports/Hid/Linux/ILinuxHidReadReactor.cs`

Reviewed complete two-phase token contract: published failed activation retains identity; removal error differs from nonfaulting actual quiescence; reference release is explicit after drain on owner worker; internal bounded callbacks and asynchronous consumer continuations.

## `src/Core/src/Transports/Hid/Linux/ILinuxHidReportApi.cs`

Reviewed every seam/result/disposition contract: synchronous borrowed spans, actual counts, zero-ID framing, typed versus expert size bounds, exact feature counts, mutation uncertainty and cached close evidence. The seam schedules no worker and owns no caller memory.

## `src/Core/src/Transports/Hid/Linux/LinuxFidoHidConnection.cs`

Reviewed factory failure cleanup, 64-byte send copy/dispatch semantics, 64-byte receive transferred array and explicit clearing recipe, terminal-wake routing, sync versus async disposal and nonwaiting finalizer. Actual operation tasks and native release remain separate.

## `src/Core/src/Transports/Hid/Linux/LinuxFidoHidNativeOwner.cs`

Reviewed complete rooted owner: pre-open claim, context-suppressed single worker, activation wait, one ordinary slot, copied/cleared output, identity-specific reader cancellation, 32 reads/four EINTR, 160-report queue, terminal removal, original-worker quiescence/reference release/checked close, loop-error cleanup and unknown-release quarantine. Lock acquisition, allocation and native calls carry no hard wall-clock guarantee.

## `src/Core/src/Transports/Hid/Linux/LinuxHidDeviceListener.cs`

Reviewed complete per-Start session ownership, initialization/failure disposal, eight-second join with independent abandoned session, payload-less hints, 250 ms delayed fallback, error exit and finalizer request. Active thread delegate retains listener; finalization is a backstop, not normal abandoned-running-listener recovery. No source-level reuse of old native handles on restart.

## `src/Core/src/Transports/Hid/Linux/LinuxHidEventSource.cs`

Reviewed partial initialization ownership, monitor/shutdown retained refs during poll, EINTR retry result, shutdown-first readiness, bounded four-write wake policy, concurrent disposed wake tolerance, copied event fields, borrowed parent scope, descriptor-ready open and source disposal order. Temporary descriptor readiness close is not registered connection claim release. C002 stale future-reactor text.

## `src/Core/src/Transports/Hid/Linux/LinuxHidFeatureReportConnection.cs`

Reviewed synchronous expert facade over separate OTP owner: GET/metadata8, arbitrary bounded SET, array transfer, owned send copy, blocking construction/Get/Set/Dispose and async checked drain. Direct open carries no grouped registration.

## `src/Core/src/Transports/Hid/Linux/LinuxHidIOReportConnection.cs`

Reviewed synchronous expert facade over separate FIDO owner, descriptor metadata versus actual read count, indefinite read and EOF terminal behavior, caller-length SET, transferred arrays, parser delegation and checked shutdown. No macOS six-second timeout applies.

## `src/Core/src/Transports/Hid/Linux/LinuxHidInterface.cs`

Reviewed complete udev enumeration/parent sysfs walk, descriptor open RDWR then RDONLY, unmanaged allocation/finally cleanup, usage classification and direct synchronous factories. Descriptor enumeration is separate from typed owner lifetime. C006 clarifies factory ownership/blocking and first-pair extraction versus downstream supported-pair classification.

## `src/Core/src/Transports/Hid/Linux/LinuxHidNativeApi.cs`

Reviewed all wrappers: production libc borrows/pins spans through synchronous syscall; captures count and errno together before later calls; no scheduling or hidden retries.

## `src/Core/src/Transports/Hid/Linux/LinuxHidReadReactor.cs`

Reviewed complete registration/publication, unique fd and generation identity, one lazy poller/private eventfd, immutable activation, bounded/rotated callbacks, terminal containment, actual snapshot/terminal drain, owner-thread reference release, retained failed wake and serialized retirement/restart. Release guard checks non-reactor/post-quiescence, while production callers enforce original-owner-worker identity.

## `src/Core/src/Transports/Hid/Linux/LinuxHidReadinessNative.cs`

Reviewed production eventfd nonblocking/CLOEXEC creation, poll(-1), eight-byte host-endian wake increments/drain, and immediate errno. Native calls are synchronous and carry no clock bound.

## `src/Core/src/Transports/Hid/Linux/LinuxHidReportApi.cs`

Reviewed complete descriptor fallback/bounds, widened parser route, positive/EOF/EAGAIN/EINTR/error counts, stack/pool zero-ID framing, GET8/native9 exact count, sized SET and no suffix retry. Metadata pool contents are nonsecret descriptor bytes; successful received payload ownership is separate.

## `src/Core/src/Transports/Hid/Linux/LinuxHidReportSizes.cs`

Reviewed every parser branch: last nonzero input/output item, 64-byte default, global size/count persistence, report-ID rejection, widened rounded arithmetic and 4096-byte bound. Truncation/default and absent PUSH/POP/aggregation are explicitly scoped YubiKey behavior.

## `src/Core/src/Transports/Hid/Linux/LinuxOtpHidConnection.cs`

Reviewed factory cleanup, typed8 send/receive, successful array transfer/clearing guidance, pre-dispatch cancellation versus held ioctl, checked shared disposal and request-only finalizer. No reactor participates.

## `src/Core/src/Transports/Hid/Linux/LinuxOtpHidNativeOwner.cs`

Reviewed complete rooted separate owner and context-suppressed worker; invalid-handle rejection, copied/cleared SET, worker-owned GET8 and failed-output clearing, one operation/no backlog, cancellation dispatch point, terminal task before close, loop failure, event retirement and retained uncertain claim.

## `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/LinuxHidRoutingTests.cs`

Read every theory row, helper, synthetic metadata/CRC response and cleanup path. Actual interfaces/slots/device/registry/owners are used beneath fake report/reactor seams. Oracles cover all known claims, refusal, pre-cancel/open/close/quiescence, late evidence, same-PID grouping, raw borrowed reuse and exact error shape. C005: some red-path finally blocks signal gates without guaranteeing returned-owner or late-work drain.

## `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxDescriptorCloseTests.cs`

Read all result rows and callbacks: synthetic close classification/deferred refs/throw/cached count and errno preservation; separate real Linux /dev/null reuse test only proves replacement usability, while isolated ABI test proves exact fd-zero reuse. No physical EIO/EINTR injection.

## `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxDirectFacadeAcceptanceTests.cs`

Read all held stages and nested synchronous witness. Tests distinguish invocation start, actual WaitSleepJoin inside Dispose, 200 ms nonreturn under held gate, shared close/ref count, and immediate-return negative surrogate. Finally releases gate and drains both callers uncancelled before fake disposal. Durations bound harness observation, not SDK latency.

## `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxDiscoveryAdmissionTests.cs`

Read complete isolated-child held-open/close and held-quiescence paths for direct/composite FIDO/OTP; sibling same-PID admission waits until exact lease release; EINTR/EIO are supplied fake outcomes. Unknown close permanently retains PID while unrelated PID progresses. Finally retires scope, releases all barriers and drains recovery/admission with finite uncancelled cleanup.

## `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxFidoHidConnectionTests.cs`

Read every test and all fake implementations, private field accesses, queue and cancellation helpers. Most tests use an invalid/inert fake SafeHandle plus synthetic report close outcome; reactor fake invokes production callback directly and models drain independently. Positive source oracles cover copy isolation, actual task terminality, overflow, generation, late drain, quarantine and isolated facade collection. C005 identifies failed-assertion cleanup holes; passing names supply no exhaustive race proof.

## `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxFidoWorkerFailureTests.cs`

Read actual Thread.Interrupt placement, locked queued delegate/copy, before-open reflection setup and independent retirement helper, held quiescence double interrupt, aggregate causes, weak facade finalizer child and finally gates. Tests establish injected managed worker-failure response; native failure or physical close remains a different lane.

## `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxHidReadReactorTests.Native.cs`

Read full fake poll/eventfd scripts, PollCall lifetime, ObservedRealNative passthrough/held return, synthetic descriptor and test OwnerWorker. Fake default ten-second wake behavior is a harness watchdog, not production timeout. Real adapter uses actual libc but no hidraw key.

## `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxHidReadReactorTests.cs`

Read complete setup/rows/oracles/teardown for activation/removal/wake failures, true callbacks drain, stale snapshots, held retirement/reference release, rotating 32-callback batches, sequential resource bounds and real eventfd/fcntl reuse. Specific fake tests release on normal path rather than enclosing every setup assertion (C005); whole race coverage is unclaimed.

## `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxHidReportApiTests.cs`

Read all framing/count/parser rows and NativeFake. Tests check exact zero prefix, ioctl length field, caller unchanged, no retry and failure destination unchanged; fake captures a copy before production finally, so these assertions alone do not inspect cleared stack/pool framing. Direct parser tests characterize YubiKey scope.

## `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxNativeAbiTests.cs`

Read actual reflection signatures and guarded native cases: failed open/eventfd, eventfd read/write/poll/fcntl, isolated fd0 and regular-file/ioctl error. Source signature oracle catches widths that small successful calls would miss. RealFile hardcodes existing /tmp/opencode when present; coordinator must prepare a home-backed variant before new runs under current quota constraint.

## `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxOtpHidConnectionTests.cs`

Read every GET/SET/open/close held case, reflection staging capture, pre-dispatch locked cancellation, source-copy clearing on success/failure, invalid open, claim-release failure and synthetic close code rows. Successful output contents/call counts and retained real registry tokens are independent of physical USB behavior.

## `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxOtpRecoveryTests.cs`

Read complete status/ten-frame/touch-wait script and CRC construction. Cancellation waits held GET, abort dummy SET precedes correlated CRC-valid reuse and completion reset. Frame sequence, command and CRC asserted; neither provisioning nor real user presence executed. Full framing policy belongs B5.

## `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxOtpWorkerFailureTests.cs`

Read idle and queued GET/SET interrupt paths, before-open setup/drain, held checked close, finalizer child and strong/weak owner separation. Root survives unknown fake close after facade collection; worker identity/error preservation asserted. Synthetic EBADF supplies classification, not actual kernel-release failure.

## `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxReportConnectionTests.cs`

Read report fake and operation arrays: direct IO returns three actual bytes with capacity64; feature GET8 and SET8 preserve compatibility. Uses fake handle with injected successful release; annotations describe production platform, not native execution.

## `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxWorkerFailureTestSetup.cs`

Read complete reflected startup delegate swap, retained actual completion, withheld wake and Interrupt, plus shutdown/independent join finally. Preserves primary assertion in Exception.Data when cleanup also fails; join separately proves worker event retirement after task completion.

## `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/LinuxHidDescriptorParsingTests.cs`

Read every descriptor byte vector, expected tuple and theory data: truncation preserves prior fields, long item progress, four-byte little-endian sizes, narrowed usage, first pair, default versus last-size globals and rounding. Characterization is explicitly distinct from general HID conformance.

## `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/LinuxHidDeviceListenerFaultInjectionTests.cs`

Read all event/receive/failure/restart tests and blocking fake. One failed poll exits/disposes once; receive/null action emits hints; ignored change then add supplies one hint; unready add supplies delayed hint. Stop responsiveness is fake-wake timing. No held-past-eight-seconds restart/native-libudev or real finalizer oracle.

## `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/LinuxUdevHidEventSourceTests.cs`

Read all scripted write/error steps and descriptor rows. Full eight-byte success reads no errno, EINTR retries four total, EAGAIN means already signaled, short/fatal transfers report once. get_fd delegate type checks int managed shape only; zero-extension case is synthetic arithmetic.

## `verification/LinuxHidRouteVerification/BlockingReportConnection.cs`

Read held synchronous GetReport fixture, entered/thread/release and idempotent disposal. Negative control deliberately wraps retained compatibility adapter, rather than production typed Linux route. Caller is drained before fixture gates are disposed.

## `verification/LinuxHidRouteVerification/DescriptorReportApi.cs`

Read real-socket transfer, synthetic 64-byte metadata, production report reads/framing, held managed output gate, separate native write timing and cached actual close. Native seam hold is not a blocked USB syscall; fixture is borrowed until owner disposal.

## `verification/LinuxHidRouteVerification/Evidence.cs`

Read result model, named checks, caller/total measure boundaries, process-wide allocation deltas, source environment label and generated JSON save. Baseline is provenance, SourceHash alone is a label, timing is wrapper-inclusive and allocation is process-wide.

## `verification/LinuxHidRouteVerification/HardwareScenarios.cs`

Read all serial selection/mode branches and enclosing cleanup. FIDO 3 cycles, borrowed idle sessions, pending read dispose/reopen; OTP GET8/Management serial; raw status/nonempty response; direct unique-interface prerequisite and pending synchronous reader. Checks qualify those exact read-only paths, not all CTAP/PIV capabilities or native error branches.

## `verification/LinuxHidRouteVerification/LinuxHidRouteVerification.csproj`

Read complete host properties/project references and linked SmartCardScenario. Standalone net10/C#14 AOT-capable host needs explicit publish and execution; referenced projects and linked source are separate compile inputs. No verifier ran here.

## `verification/LinuxHidRouteVerification/NativeDescriptors.cs`

Read AF_UNIX datagram nonblocking/CLOEXEC socketpair imports and handle transfer, marker send and disposal. Real file descriptors with synthetic FIDO data, not hidraw. Fixture allocation has narrower exceptional-OOM protection than shipping preallocated fd acquisition; no production fix inferred.

## `verification/LinuxHidRouteVerification/NativeScenarios.cs`

Read all 38-check construction, actual eventfd/poll/socket operations, held callback finally, fd reuse, output/input independence, copied65-byte output, 40 retained arrays, cancellation/dispose/reopen, idle sample and exact negative controls. Reference release runs designated test threads, not production lifetime workers in isolated reactor-only scenarios. Child kill remains failure, not native drain.

## `verification/LinuxHidRouteVerification/ObservedReadinessNative.cs`

Read complete counters/passthrough: every poll/wake invokes production native adapter; counts observe calls and contain no readiness simulation.

## `verification/LinuxHidRouteVerification/OperatorScenarios.cs`

Read complete separately gated Selection/touch/cancel/removal flows, exact paired presence identity/outcome, scheduled cancellation task, observed absence/fresh discovery and reopen. Removal snapshot is one-shot discovery, not monitor identity-generation evidence. Compilation or environment flag supplies no attended execution.

## `verification/LinuxHidRouteVerification/Program.cs`

Read complete arguments/platform guard/serial validation and bounded own child host: streams drain, timeout kills own tree and exits3, result requires named checks, exceptions retain failed evidence. SmartCard linked scenario's one marker aggregates 3 cycles. Physical and native modes retain separate selected scopes.

## `verification/LinuxHidRouteVerification/capture.py`

Read all provenance/capture/selection/control flow. Verifies binding before device snapshot/child and around each child, exact native/negative gate, positive unique JIT/AOT serial agreement before six physical modes; native-only executes none. No operator mode. Records package content from bound restore, not old default obj paths.

## `verification/LinuxHidRouteVerification/evidence_gate.py`

Read full independent 38-name set and strict runtime/mode/source-label/outcome/unique-check/negative-prefix/cleanup checks. Other positive physical modes require nonempty successful checks, rather than an independently enumerated physical-check set; actual scenario assertions supply their scope. No binary attestation follows from result JSON alone.

## `verification/LinuxHidRouteVerification/publish_contract.py`

Read complete source/input/restore/package/import/artifact/seal/verify logic. Conservative unsigned local stable-input relationship includes HEAD and artifacts, two actual restore contexts and referenced SDK property routing. Before/after equality leaves concurrent adversarial mutation outside claim. Current compilation hooks remain later validation's responsibility.

## `verification/LinuxHidRouteVerification/test_evidence_gates.py`

Read all setup/mocks/temp fixtures/assertions/finally restore across runtime, publish, evaluated imports, restored package graph and capture package provenance. Some tests invoke real MSBuild despite Python unittest label. Synthetic executables stay unexecuted; 31 retained integration tests are old scoped evidence and no tests ran in B2. Hardcoded /tmp/opencode temp directories need home-backed adaptation before future quota-constrained runs.

## Shared exact-range context

- `docs/NATIVE-AOT.md`: Read complete Linux native-contract section against actual host/publish/gate source: native-only real descriptors with synthetic FIDO64 differ from serial-selected physical and attended modes.
- `docs/linux-setup.md`: Read full configuration journey and nearby PC/SC, hidraw/group and troubleshooting conditions; permission and loader-prerequisite candidates remain separate from any executed deployment.
- `docs/plans/yubikit-async-boundaries/00-status.md`: Read exact historical Linux stage checkpoint: L0-L6b statuses/counts belong to its own revision and remain distinct from current release qualification.
- `docs/research/linux-async-boundaries-baseline.md`: Read full L0 record, fake helper contract, negative controls and timings. Baseline compatibility-adapter behavior is historical; invocation return, actual operation drain and hardware measurements remain separate.
- `docs/research/linux-fido-async-lifetime.md`: Read full L3 record against current owner and complete tests. Historical stage exclusions remain historical; original-worker drain, finite attempts and pending native lifetime are accurately separated from elapsed-time guarantees.
- `docs/research/linux-hid-routing.md`: Read full L5 record against actual slot and current SharedDeviceInfoRead/borrowed ProtocolDeviceInfo split. Exact source owner attribution needs C002; recorded execution and fake scope remain historical.
- `docs/research/linux-managed-acceptance.md`: Read full L6a and witnessed synchronous-disposal follow-up. Runnable registry links, successful-path managed assertions, actual witness negative controls and pending native/device lanes are distinct. Historical 24-entry count is not current registry's 23.
- `docs/research/linux-native-acceptance-results.json`: Read complete nested/current/historical JSON, not only top-level blocked fields. Latest prPreparationCheckpoint supersedes historical physical/blocked checkpoints within its source/serial envelope; integration native-only capture is separate.
- `docs/research/linux-native-acceptance.md`: Read full chronological record and every assigned section. Latest serial 103 six read-only routes passed at recorded d5bb source; old blocked fields are historical. Operator actions, telemetry/budgets, other devices/OS and current merged source execution remain separate.
- `docs/research/linux-native-report-access.md`: Read full L1 original-source/report/count/ABI record against actual seam. Slice scope is historical; fd-int, fd0, borrowed spans, metadata versus actual counts and checked close remain current. Exact deployed kernel/libc attestation remains qualified.
- `docs/research/linux-otp-async-lifetime.md`: Read full L4 record against current separate OTP owner and complete tests. Stage routing exclusions are historical, while copied SET, transferred GET, dispatch cancellation and checked release remain current mechanisms.
- `docs/research/linux-read-reactor.md`: Read full L2 token/poller/retirement record against production reactor and complete real/fake tests. Historical integration exclusions remain stage scope; actual quiescence/reference release/device close stay distinct.
- `src/Core/CLAUDE.md`: Read full Core instructions with exact assigned Linux L1-L4/L6a sections against production implementation/assertions. Contract vocabulary is useful; section's 23 public-sync entries is current source, not historical L6a24. Cross-platform/native verification stays bounded.
