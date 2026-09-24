# First implementation slice: smart-card connection lifetime

Status: first slice implemented and verified on 2026-09-22; its original independent
review finished PASS WITH NOTES, and the subsequent structural refactor has separately
recorded managed verification. This is smart-card slice evidence, not completion of the epic.
The commit/worktree and route evidence in the sections below are historical snapshots;
the [current status](00-status.md) supersedes their package and hardware-gap claims.
The first smart-card slice was committed at `db7a1bf6`. The subsequent native fix
at `71a23cd0269c968c1d9420eddb2e2fec71e0cc29` produced the private-published
`.8` package in [run 35955737172](https://github.com/Yubico/Yubico.NET.SDK/actions/runs/35955737172).
The unsigned `.7` was built before that commit and is historical, not the current pin.
The user approved the single-key product and architecture and replaced whole-effort
upfront specification with incremental implementation on 2026-09-22. For subsequent
slices, walk through bounded pseudocode, then implement, test and review; resolve
non-blocking details live rather than requiring a complete epic program design or catalogue.

The [master plan](../../../2026-09-21-yubikit-async-boundaries-ISA.md) remains the
acceptance record. The [earlier detailed program design](addenda/earlier-multi-key-program-design.md)
is preserved as research/reference, not a prerequisite or an implementation instruction.

## Subsequent bounded portable checkpoint — 2026-09-24 (published `.8`)

The pinned `.8` (attested package SHA-256
`c85c56f7a41c6999b48b1a5fdcd82c56fbfb3e5ee6ff18ccc64a41144f760403`)
passed seven packaged consumers, native AOT and private publish in the workflow above.
Fresh private-feed restore succeeded in an empty cache after renaming the repo source
key from `YubicoInternal` to `Yubico_GH` to match an existing credential name at the
same URL, without changing credentials; normal parent restore passed 43 projects.
This is not Windows HID driver proof. On selected serial 31683481, `.8` native-AOT
five normal scenarios, active cancellation and three read-only OTP info queries passed;
no new touch or unplug was run. Earlier `.3` touch and `.6` unplug remain historical
version-specific evidence. Windows/Linux native testing is deferred pending user direction.

The built-in transaction-begin withheld-native probe returns a pending task before
release (ISC-51); custom `BeginTransactionAsync` defaults to a synchronous fallback.
The migrated-route registry has 13 required operation rows with 26 named runnable
Fact/Theory profile links; three registry plus 13 Core scanner tests passed (16).
The registry covers macOS FIDO/OTP and portable PC/SC only, not universal ISC-4.
OTP partial-send/read faults attempt one abort after attempted write, never replay
the original command; successful abort permits reuse, while failed abort faults the
protocol preserving the original failure. 35 focused protocol tests and one scripted
Mac OTP test passed, not physical mid-frame failure. PublicApi 22, Fido2 471,
YubiOtp 180 and resilience-fast 77 passed; the latest full Core tally appears below.
The earlier `dotnet toolchain.cs complexity` run passed six changed shipping
methods at cyclomatic ≤10/cognitive ≤20; the later 21-method result is below.

Independent review accepted ISC-38/39/43–47/60 at this earlier checkpoint;
the later ISC-31 and ISC-32 slices bring the master to **16/72 checked, 56 pending**.
ISC-38's source audit finds `SCardCancel` only in monitoring,
while controlled cancelled transmit retains its native borrow; it is not a gate
against future callsites. ISC-39's `PcscContextIsolationTests` holds a transmit
through production listener disposal and compares distinct fake-native context
addresses: only monitor A is cancelled/released, connection B after its own work.
This is not Windows/Linux operating-system native evidence. Six managed
`ResponsivenessProbeTests` make actual legacy synchronous macOS feature open and
OTP GET/SET fail the thread-identity responsiveness gate; built-in macOS FIDO
open and OTP open/receive also return before withheld native release (ISC-60,
not all-platform coverage).

The five new macOS `Native_HidInput*` exports have scoped ISC-44–46 evidence:
source and Apple callback contract retain input buffer/device/context until
callback return or cancellation acknowledgment and accepted-delivery drain;
synthetic destroy-BUSY, close-attempt and drain tests passed (23 Release at
producer `71a23cd0` in continuous integration; 23 AddressSanitizer locally on
identical runtime source before that commit, not rerun at its SHA). Real `.6`
unplug after acknowledgment/drain and `.8` normal runs are version-specific,
not a new `.8` unplug. Independent review accepted these as a substitution for
the **unimplemented** proposed native retained-buffer counters. ISC-43 now
has an explicit `hidinput/owner.h` contract, committed at unpushed comment/test-only
native revision `760d0416`, for **one persistent registration**
per input owner, not a new submission for each report: Create never calls back;
Start registers before activation, which can trigger callbacks on another
queue before Start returns (source-modeled, not observed); one Start can produce
zero or many ordered, once-delivered reports; terminal is at most once after
accepted reports drain; Cancel is a request and WaitShutdown acknowledges and
drains. Existing lifecycle tests added two reports after one Start and repeat-
Start rejection; 23 native Release tests passed without changing runtime code
from the `.8` producer `71a23cd0`. Independent review checked ISC-43 on this
scope, **not** on a per-report callback-count fixture or any future operation
without its own contract. ISC-47 is checked by actual `.8` release-artifact
LLVM inspection of all 14 shared/static files on seven RIDs (36 canonical
`Native_*` exports except 41 on macOS; no test-only helpers) and five checker
tests including compiled Mach-O negative controls. Checker-only native commit
`541cfb09` is not pushed or wired into continuous integration, and did not
rebuild `.8`; this is a one-time artifact check.

Six diagnostics cases inventory logging in three files and exercise real
PC/SC/OTP sentinel full payloads and five-byte prefixes to catch framing
leaks; external exception payloads and other migration logs remain outside
this proof, so ISC-56 stays unchecked. At this earlier checkpoint full Core
passed **1,423/3 skipped**, including these cases. After
scoped formatting, a targeted `BoundaryInventory` run passed **28** tests:
13 scanner, three registry, six responsiveness and six diagnostics. The
focused OTP protocol run passed 35; PublicApi 22, Fido2 471, YubiOtp 180 and
resilience-fast 77 passed. Complexity checked six changed shipping methods
at cyclomatic ≤10/cognitive ≤20; manually split verification methods are
excluded, not tool-certified. The latest full-suite and complexity counts
are in the next slice below.

### Subsequent direct report input slice on macOS — committed at `4f361504`

The typed macOS FIDO owner now detaches a cancelled expected reader under lock,
without cancelling native input or producing a terminal; late reports queue
and a stale token cannot detach its replacement. The public direct report connection
`MacOSHidIOReportConnection` delegates to this persistent native input owner,
removing its caller-run-loop pump and legacy callback-handle cleanup.
`IHidConnection` remains public with unchanged shape: direct open, GetReport,
SetReport and dispose stay synchronous. `GetReport` retains the six-second
`PlatformApiException` timeout followed by same-connection retry. Direct report
output accepts any report length for native validation while typed FIDO still
requires 64 bytes. Input accepts 64 bytes or 65 with zero report ID;
malformed input becomes terminal, without a claim of exact legacy failure
parity. Ordinary constructor failures retain `PlatformApiException`, while
unproven release propagates `UnrecoveredConnectionException`. Native SET
failure now surfaces status-bearing `PlatformApiException` instead of the
typed path's `InvalidOperationException`; this exception difference is
intentional.

Five pending-read cancellation, eight direct IO compatibility and three
facade tests passed (16); six old legacy tests were removed after meaningful
cases moved. On selected serial 31683481 with pinned `.8`, native-AOT
`--expert-io` observed a 6,011 ms timeout, then same-connection INIT/getInfo
and dispose/reopen/getInfo passed without operator interaction. Independent
cross-vendor review: PASS. This checks **ISC-31** for macOS HID input waits:
typed and public direct report input both use the persistent event owner. OTP feature
GET/SET is not an input callback. ISC-33 still lacks discovery-manager
callback-quiescence proof; ISC-53 is wider than this synchronous report
surface. The 13-operation registry does not include direct raw IO, so
universal ISC-4 remains open.

After this slice, full Core **1,434 passed/3 skipped**, PublicApi 22, Fido2 471
and resilience-fast 77 passed. YubiOtp 180 and 35 focused OTP protocol tests
passed at the prior checkpoint. `dotnet toolchain.cs complexity` passed 21
changed shipping methods at cyclomatic ≤10/cognitive ≤20; manually split
verification methods are excluded, not tool-certified. The Core inventory
still has **198 documented/outstanding** sites after removing the legacy input
path: 130 native imports, 21 waits, 16 scheduling, 21 pre-task-return gaps,
six callback registrations, two delegate conversions, two unmanaged callback
addresses. The older [`.8` current-profile dataset](../../../artifacts/measurements/current-profile-20260924T051049865Z.json)
predates this facade; it does not measure its performance. No new physical
touch/unplug or cross-platform native execution is inferred.

`docs/architecture/raw-access-tiers.md` and `src/Core/README.md` describe
direct-connection limits. The feature-report ownership gap described at this
earlier checkpoint is superseded by the next slice; discovery-manager callback
quiescence and global synchronous-wait inventory remain open.

### Subsequent direct feature-report slice on macOS — committed at `db378ffc`

The public `MacOSHidFeatureReportConnection` now delegates to the typed OTP
connection-owned worker for native open, descriptor metadata, feature GET/SET
and checked close. Typed FIDO output plus direct IO SET use the FIDO worker;
typed OTP GET/SET plus direct feature GET/SET use the OTP worker: explicit
blocking-executor fallbacks rather than claims of native callback GET/SET.
Direct report open/Get/Set/dispose still block synchronously; each connection
has one worker/one admitted operation, **not** process-global bounded capacity.
Direct feature GET returns an owned eight-byte array: short native responses of
0–8 bytes remain zero-padded; more than eight bytes fails and zeros the
buffer. Typed send still requires eight bytes; direct feature SET accepts arbitrary
length for native validation. Accepted calls drain before checked close.
Both IO and feature public `GetReport` return the owned whole array via
`MemoryMarshal.TryGetArray`, with no second uncleared copy; tests pin array
identity. The public `IHidConnection` shape is unchanged.

Independent review: **PASS WITH NOTES** after the copy fix. Eleven feature
compatibility and nine IO compatibility tests passed post-fix. The Native-AOT
host built from this worktree with pinned `.8` passed
`--expert-feature --serial 31683481`: 3/3 eight-byte feature GETs and
read-only Management device info via feature SET/GET with dispose/reopen and
serial match. Separate `--otp-info` typed queries passed 3/3 on that key.
This checks **ISC-32** only for identified macOS output/feature directions;
it does not prove a native callback feature API, all-platform runtime, or
ISC-33 listener/manager callback quiescence. ISC-53 still covers remaining
public synchronous waits.

After this fix full Core **1,451 passed/3 skipped**, PublicApi 22, YubiOtp 180
and resilience-fast 77 passed. Fido2 471 and 35 focused OTP protocol tests
were earlier results, not post-fix claims. `dotnet toolchain.cs complexity`
passed 21 changed shipping methods at cyclomatic ≤10/cognitive ≤20;
verification methods excluded by that tool remain manually split, not
tool-certified. The current Core inventory asserts **200 outstanding sites**:
130 native imports, 24 waits, 15 scheduling, 21 pre-task-return gaps,
six callback registrations, two delegate conversions and two callback
addresses. The older 198-site count belongs to the input checkpoint.
The 13-operation registry omits direct raw IO/feature routes; ISC-4 stays open.
The feature slice is now committed. The subsequent listener-generation work
below does not yet prove actual native callback quiescence.

### Subsequent macOS listener-generation slice — uncommitted over `db378ffc`

`MacOSHidDeviceListener` now roots manager/run-loop/callback resources per
generation. On the listener thread, `finally` unschedules only after `Run`
returns, then releases mode, loop, manager and root. Stop signals and waits
outside the lock; concurrent Stop/Dispose share one monotonic deadline. A
timeout retains the generation without repeated eight-second waits; fresh
Start is refused until prior cleanup succeeds. Callback self-stop does not
join its own thread, and callback/logger exceptions cannot escape into the
native callback. This is the existing owned IOHIDManager run loop with 100 ms
poll, **not** a new native dispatch bridge or hardware topology change.

Independent review: PASS after the shared-stop fix. Six controlled
`MacOSHidListenerLifetimeTests` cover held callback/drain, self-stop,
concurrent Stop/Dispose, timeout/restart and failed cleanup. Four existing
on-host `HidDeviceListenerIntegrationTests` passed on macOS/.NET 10.0.12 for
Start, no-change, Dispose and platform type; none observed actual native
arrival/removal callback drain. **ISC-33 remains unchecked** pending that
evidence and the broader teardown audit. Full Core **1,457 passed/3 skipped**,
PublicApi 22, resilience-fast 83 (six more than the prior 77); YubiOtp 180
passed at the feature checkpoint, with Fido2 471 and 35 OTP protocol tests
from earlier runs. Complexity passed 33 changed shipping methods at
cyclomatic ≤10/cognitive ≤20; verification/test methods remain outside its
certification. Current scanner: **198 outstanding** sites (130 imports, 24
waits, 15 scheduling, 21 pre-task-return gaps, six callback registrations,
zero delegate conversions, two callback addresses), not verified-safe sites.
No new operator touch/unplug is claimed. Parent owns the later SDK commit;
next bounded work can inspect remaining listener quiescence and inventory
without claiming whole-macOS or epic acceptance.

The pre-compatibility-facade `.8` [current profile](../../../artifacts/measurements/current-profile-20260924T051049865Z.json)
(schema 2; SHA-256 `9bbd8837afab35e1143385bc6e383a9baf330a5c5ce20de5892b2090b7ba1f65`)
completed two warmups, ten normal fresh-child samples and one idle sample. Normal
medians: caller return 5.00675 ms, operation complete 24.84525 ms, disposal
2.90555 ms, allocation 60,004 bytes. Idle CPU was 1.945 ms with zero idle
allocations over 1,001.7302 ms. Native-only duration and pending ordinary count
are null with explicit instrumentation-unavailable reasons. This does not
measure either report compatibility facade and is not comparable to the historical `.3`
pair; ISC-62 is pending. Actual release-artifact
inspection now checks ISC-47 as scoped above, not route or epic acceptance.

### Historical `.7` checkpoint

This is not a replacement for the historical first-slice pseudocode below. The
selected macOS normal-use FIDO path passed on one key, including `.3` touch/cancel,
`.6` unplug/replug after earlier close failures and `.7` normal native-AOT execution;
`.7` did not receive a new unplug run. The failed-reset OTP path now latches the
current protocol unusable while retaining the original cancellation or timeout;
28 focused tests and one scripted real-protocol abort/reuse test passed. A new session
borrowing the same raw connection can bypass that per-protocol latch, and physical
mid-frame failure remains unverified. The unplug/dispose edge stays quarantined.
Correctness review finished PASS WITH NOTES: this bounded implementation increment
stops without claiming universal route or platform acceptance.

The Core-only semantic scanner records 198 exact source sites, all
documented/outstanding, and 13 passing targeted tests, including the cross-file
source-order regression. Its unknown-native fixture and exact
gate establish only ISC-5; ISC-6 is not established by flagging an interface call.
Source scanning covers Core and the current .NET 10 preprocessor configuration only,
not native exports or every applet/helper path. Two-pass collection fixed source-order
dependence; callback forwarder/nontransitive helper-native-graph coverage remains a
deferred review note. The final full Core run passed 1,400 tests with 3 skipped,
including the thirteenth inventory test. The same-host `.3`
ten-before/ten-after measurements are descriptive with
unmeasured native duration, allocation and idle activity and worse after-tail;
the comparison runner's six self-tests are pinned to `.3`, not the current `.7`
checkout. ISC-62 remains open. ISC-5/49/52 are the only master checkmarks (3/72).
The [current status](00-status.md) supersedes this `.7` package blocker; no push
or release is part of this documentation update.

## First slice

Use the existing built-in smart-card route for one selected YubiKey. Establish an
owned native lifetime covering open, a non-destructive request/response, and dispose,
including failed opening, cancellation and retained ownership after unsafe cleanup.
This uses the current native package; it does not require a new bridge/package to
prove the first execution model. Public raw access and applet/session ownership remain.

One connection owns one lazy blocking worker. At most one ordinary native operation
is admitted; there is no ordinary backlog. Existing transaction begin/end calls must
also use that owner so they cannot race new teardown, even if the new public async
transaction surface is delivered in a following step.

The initial implementation area is `UsbSmartCardConnection`, its native seam and
checked handle/context release, and the factory/registered-connection lease path.
Change shared guards only where the slice requires it; do not widen into other applets,
all platform adapters or global scheduling merely to complete a file inventory.

## Pseudocode

```text
open(selected key, cancellation):
    acquire the existing physical-interface claim
    create native state and its lifetime owner before acquiring native resources
    attach release observation before dispatch
    try:
        await owner.run(open native connection, cancellation)
        return the existing typed raw connection bound to that owner
    on failure:
        request the same owner's checked cleanup of any partial opening
        release the claim only after positive release evidence
        otherwise retain the native state and claim, and report the failure

owner.run(native operation, cancellation):
    refuse overlap or closed admission; reserve the single operation slot
    if cancellation wins before dispatch:
        complete cancelled without a native call
    otherwise:
        execute the synchronous native call on the owned worker
        keep its task and borrowed storage alive until the native call returns
        copy/transfer the result; release pins and zero owned scratch after last use
        free the operation slot; publish the actual result or error
        service any pending release/shutdown intent

end transaction scope:
    record one coalesced end request, even if an operation is still running
    prevent further ordinary work for that ending scope
    after the accepted operation ends, perform native end exactly once
    share that end outcome with connection shutdown; never run end concurrently

dispose connection:
    close admission and record one shared shutdown request
    return an awaitable without waiting on the caller thread
    after accepted work finishes:
        settle transaction end once if needed
        perform checked disconnect/context release on the same owner
        if native release is proven:
            release the physical claim and stop the worker
            report any cleanup error separately; do not silently swallow it
        otherwise:
            retain/root native state and the claim; report unrecovered state
            do not retry an uncertain native action or create a replacement worker
```

This is the registry-managed built-in path. Direct raw construction still owns its
native lifetime without silently acquiring registry semantics. External factories and
connections keep their documented responsibilities; they are not claimed to have
built-in native proof.

## Rules that are not deferred

- A cancelled wait is not native completion. After dispatch, do not complete a
  transport task while native code still uses its borrowed input. Protocol finally/
  zeroing therefore occurs only after that borrow ends.
- A returned native call, a faulted task, or SafeHandle.Dispose returning is not by
  itself release proof. Interpret the actual close/release result; never assume every
  platform's error implies either successful release or a still-live handle.
- Partial opening needs the same owner and cleanup rules as a published connection.
  The absence of a published connection is not evidence that nothing was acquired.
- Transaction release and shutdown remain bounded intents, not a general work queue.
  A failed transaction end must not prevent safe connection cleanup or be retried blindly.
- If native work never returns, retain its state/ownership and leave disposal pending;
  do not promise an abort the portable smart-card interface does not provide.
- Share disposal completion, keep consumer continuations off native worker stacks,
  and reject self-drain. Keep existing logical exchange/session guards in charge.
- Same-key discovery expiry must wake waiting callers with the unrecovered-state
  failure while retaining unsafe ownership; late proof can release only the old claim.
- Use existing naming, Task conventions, static logging and sensitive-buffer rules.
  No payload logging, actor hierarchy, shared pools, credits, or process-global one-key lock.

## Essential verification

Write controlled tests below the real production adapter, not only against fake
public connections. First make the relevant old behavior fail, then implement:

1. Hold native open/transmit/close: the asynchronous entry returns before release.
2. Cancel before dispatch: zero submissions. Cancel after dispatch: task/storage
   remain protected until actual native return.
3. Attempt overlap: refusal and no queued native call.
4. Fail after partial acquisition: checked cleanup or retained ownership; no false reuse.
5. Request transaction end and disposal during a held call: one ordered end/close,
   shared completion, no additional worker and no concurrent release of the handle.
6. Inject cleanup failure/late completion: claim and native state stay alive until
   release is proven; old completion cannot affect a replacement.
7. Drop caller references and repeat safe open/close: no premature finalization,
   stranded safe worker, retained borrowed buffer or unobserved native failure.

The current smart-card open/transmit paths already use Task.Run, so their responsiveness
checks may start green. The first red proof should expose an actual missing lifetime/
admission guarantee, such as release-on-fault or disposal during a withheld operation;
do not treat an absent helper type or every newly named test as a behavioral failure.

Run affected Core ownership/protocol/raw-connection regressions and public-surface
checks. Then exercise a selected key's non-destructive request/response and reopening,
with exact device/environment evidence. Choose and verify that fixture before device
operations; earlier bus descriptors are not firmware proof. Independent code review
and a focused consistency review close the slice before extending the model.

## Bounded slice finish line

Master D26 is canonical. Before implementation, agree one route outcome, owner, bounded
files/responsibilities and non-goals, applicable existing criterion IDs, finite named
probes and exact relevant commands, and required evidence grades. A slice finishes when
all agreed behavior passes with no correctness/safety/regression blocker, the settled
shape receives one independent correctness review plus targeted fix review, and the
master/status/plan record evidence, limits, deferred items and next action.

Stop at that line: no speculative shared abstraction, future-route generalization or
redesign for taste. Existing passing evidence remains reusable until meaningful changes
or new risks invalidate it; do not rerun the whole suite without either. Missing required
native/hardware/platform/performance evidence blocks route acceptance, although a
pre-agreed investigation or managed-only milestone may finish at its honest limited grade.
Use one implementation pass plus at most one optional bounded readability cleanup, and no
more than two review/fix cycles before presenting remaining blockers to the user. New
safety findings require explicit re-scope; materially new scope requires user approval.

## Deferred to implementation effort

| Detail | When to resolve |
|---|---|
| Internal helper/seam names, final method signatures and exact file list | Deferred to implementation effort — choose against the first failing tests and surrounding code. |
| Precise task, lock, signal and finalizer plumbing | Deferred to implementation effort — demonstrate the safety rules above before considering the slice complete. |
| Exact new public transaction signature and consumer-example updates | Deferred to implementation effort — discuss when that surface is reached; preserve existing synchronous compatibility behavior meanwhile. |
| Per-connection report-buffer size and platform operation-record shapes | Deferred to implementation effort — before their respective adapter changes. |
| Single-key measurement runner details and numerical budgets | Deferred to implementation effort — record the unchanged baseline before altering production behavior and agree relevant thresholds. |
| macOS bridge exports, callback-timeout interpretation and deployment evidence | Deferred to implementation effort — D28 settles the modern API direction; verify the concrete bridge contract and current .NET 10 environment before enabling the affected native path. |
| Native package-producing revision and new-package compatibility | Deferred to implementation effort — remains a hard prerequisite for native package changes; not declared resolved by managed tests. |
| Windows/Linux/macOS adapter order, broader inventory automation and subsequent slices | Deferred to implementation effort — select the next route from first-slice evidence. Full epic obligations remain in the master. |
| HID interface migration | Proposal only — public `IHidConnection` connects discovery, both protocol wrappers and six platform implementations. Keep it unchanged during a route-local seam experiment unless a separate compatible migration walkthrough approves broader change. No exact new interface is approved. |

These entries postpone up-front design, not required correctness checks. Surface a
material signature, visibility, seam, native-platform, architecture or safety trade-off
to the user for approval before implementing it. D14 remains binding.
Routine choices within approved conventions can be made and reviewed during the slice.
Multi-key capacity and scheduling remain in the separate addendum rather than leaking
back into this iteration. No full-epic completion is claimed by a first working route.

## Slice result

The built-in smart-card path now uses `PcscConnectionNativeState` and a narrow
`ISCardConnectionApi` seam. Both direct-slot and published-device discovery routes
transfer their claims to checked native cleanup. `UnrecoveredConnectionException`
distinguishes retained ownership. Existing synchronous transaction entry points remain;
their native calls are owned and scope reuse/teardown races are covered.

The transferable result is owner-before-open, one lifetime authority distinct from its
native-resources helper, no ordinary backlog, borrowed-input release at public task
termination, and checked release or quarantine. Apply that result by route: do not
pre-extract a generic scheduler or assume a blocking worker fits native completion,
readiness or callback delivery.

The active focused commands are separate filters for `PcscConnectionLifetimeTests`,
`SmartCardConnectionFactoryTests`, and `PcscDiscoveryLifetimeTests`; they cover 17, 13,
and 8 cases respectively, 38 total. The original pre-split `CoreAsyncBoundaryTests`
command and its 34-case result remain historical evidence in the master plan, not a
current gate. Core passes 1,335 tests with three existing skips; PublicApi passes 22 and
resilience passes 77. Isolated cleanup-failure and finalizer probes assert explicit
completion markers and nonzero child counts. Restoring the old published-device routing
defect made its regression test fail; restoring the fix passed again. A read-only hardware
test on the selected 5.7.4 USB key passed three connection cycles with two transaction/read
cycles each. macOS arm64 native ahead-of-time publish and the existing discovery host also
passed; that host is not a full native-boundary test harness. Detailed commands, limits
and the observed non-selected-key discovery warning are recorded in the master verification.
Those native publish/discovery and hardware results predate the structural refactor and
the NativeShims 1.18.0 upgrade; they were not rerun and are not 1.18.0 hardware evidence.

The Dependency Engineer's one-line 1.18.0 declaration passed restore with no 1.16.1 in
assets, Core 1,335 passed/3 skipped, PublicApi 22, focused native-crypto/PreviewSign tests,
and macOS arm64 Native AOT publish. The published host was not executed and no device was
used. Native OpenSSL execution covers local cryptography, not PC/SC or HID hardware. The
master records package identity, signatures, artifact/export comparison and limitations.

## Historical macOS FIDO `.2` route checkpoint: persistent input

**D30 production code implemented, selected normal and pending-read shutdown paths verified, route acceptance pending:** the built-in macOS FIDO
open/init/send/receive/cancel-recovery/concurrency/shutdown/reopen route is available for
user review, not delivery of a harness alone. D29's synthetic evidence remains limited. Internal managed bridge,
terminal wake, slot/registry/discovery claim transfer and awaited initialization are now
implemented; public raw contracts remain. The native owner has five macOS exports consumed
via locally built unsigned/unpublished `1.18.1-async.2` (tag 1.18.0 base plus recorded dirty
hash manifest). At that implementation checkpoint neither original 1.18.0 producer
binding nor actual device behavior was inferred. Parent correctness review was underway;
no independent code review is claimed.
The lifecycle below is the approved route-local model, not a new gate for each helper.

### Route-local lifecycle

```text
entry/open (registry-managed built-in macOS FIDO only):
    acquire the existing physical-interface claim; root one generation owner
    dispatch blocking create/open on its worker; register input/removal with event owner
    activate persistent delivery; only then publish the typed raw FIDO connection
    on partial failure, run checked shutdown or retain owner plus physical claim
initialize:
    retain existing FIDO initialization semantics and ExchangeGuard; send CTAPHID_INIT through
    the typed raw route and receive from persistent delivery
    do not publish an initialized applet/session before that existing exchange completes
input/read:
    admit one receive or refuse overlap immediately; keep no public-operation backlog
    await a bounded receiver populated independently by the native event callback
    callback validates generation/result/length, copies the reusable native report into
    one owned packet, attempts nonblocking enqueue/signal, and returns immediately
    complete managed waiters later with asynchronous continuations, never on callback stack
    on receiver overflow, mark the generation unusable and terminally wake pending/future work
output:
    admit one send or refuse overlap immediately; do not overlap it with an admitted receive
    copy caller data into operation-owned storage; run blocking SET report on the worker
    retain and zero that copy only after native output returns
close/removal:
    close ordinary admission and terminally wake a pending receive
    drain accepted output and any operation-owned borrow, then request native event cancel
    await native cancellation/quiescence acknowledgment before blocking close/release
    release the claim only after operation drain and positive native release
    removal is a terminal signal, not release proof; it follows the same drain/ack path
    finalization may only request this asynchronous cleanup while rooted state continues it
```

The native event owner and persistent input report buffer live for the generation; a receive
does not lend that buffer to native code. Output instead lends an operation-owned copy to one
blocking call. Callback code takes no managed lock that can wait on callback, worker or disposal,
performs no close, and invokes no application continuation. The worker separately
owns create/open, output and close; event-owner and worker self-waits are rejected.

Ordinary send/receive admission is one slot with immediate overlap refusal and no queue;
persistent callbacks still enqueue within the report bound. Preserve between-read/keepalive
FIDO cancellation and at most one legal cancel frame; impose no simultaneous raw send/read
capability on external `IFidoHidConnection` implementations. For synchronous protocol shutdown
with a built-in macOS receive waiting, a narrow internal terminal-wake control marks
that route unusable and wakes it before drain, adding neither public API nor custom duplex duty.

The native bridge implements cancellation acknowledgment and accepted-delivery drain;
the later selected-key pending-read shutdown exercises normal IOKit callback acknowledgment,
not interrupted callback unregister or quiescence after physical removal. Retain context/buffer/claim until positive native
evidence, never equate unregister return with quiescence, and keep late callbacks
generation-scoped so they cannot complete a replacement.

**D29 historical partial experiment:** a standalone `hidinput` dylib in the detached 1.18.0-tag
native worktree implements a bounded copied-report owner with opaque create/start/cancel/
wait/destroy, separate acknowledgment and delivery drain, owner isolation, self-wait
refusal and retained roots on checked close failure. Close is synchronous on the destroy
caller, so production would need its blocking worker. The actual IOKit backend compiles
but was not device-exercised; its cancel handler posts acknowledgment after returning
on the same serial queue. Eight synthetic libdispatch C tests passed Debug, Release and
AddressSanitizer; a close-failure red probe preceded the targeted fix. Final Release used
an explicit macOS-12 deployment target (minos inspected as 12). The main-worktree
`verification/MacOSHidInputVerification` host published **and ran** three watchdog-isolated
synthetic-backed arm64 Native AOT probes (borrow/root/drain, late owner, overflow), not
real IOKit callback quiescence or hardware. Its rooted `GCHandle` is not such proof.
The experimental C ABI has no managed terminal-error/removal read path; integrate that
and settle initialization/control before any production route. No shipping native
package/export or Core transport was changed **at D29**. D30 adds preview exports and Core
integration; master D29 records the original experiment's native/AOT commands.

### Route-local seam and bounded responsibility

The implemented seam is internal and asynchronous only on the built-in typed raw FIDO
path. `HidConnectionSlot` opens the macOS FIDO route asynchronously; terminal wake and
claim-transfer paths follow the connection owner. Public `IHidConnection`,
`IHidInterface`, `IFidoHidConnection`, OTP and other-platform signatures stay unchanged. No
shared Core scheduler or all-route HID migration is included.

Managed implementation includes `MacOSFidoHidConnection.cs`, `MacOSHidInterface.cs`,
`HidConnectionSlot.cs`, terminal-wake and initialization in `FidoHidProtocol.cs`/
`ApplicationSession.cs`, and claim-transfer updates in `YubiKeyDevice.cs`,
`RegisteredConnections.cs` and `ProtocolDeviceInfo.cs`. The new native exports bind the
tagged-base owner with at-most-once terminal delivery, report type/ID checks and checked
close-failure retention. Internal correctness fixes remain part of this milestone.
Escalate only a genuine material scope change or unresolvable safety blocker, not routine
internal seam/packaging choices under D30. Tests remain route-scoped.
GET/SET callbacks, OTP, discovery matching, public API changes, other platforms and cross-key
scheduling were non-goals **of this FIDO slice**. Later macOS OTP and public smart-card
transaction increments are tracked separately below.

### Finite proposed probes and commands

Lifecycle/producer: (1) held open/activation proves asynchronous entry and partial-failure
claim retention; (2) copied reports arrive without caller-run-loop pumping; (3) overflow
faults pending/future work and ignores a late generation. Concurrency/cancellation: (4)
send/receive overlap is refused with no backlog while callbacks continue; (5) cancellation
before dispatch prevents submission, accepted output retains storage until return, and one
between-read cancel frame drains without external duplex. Shutdown/ownership: (6) removal wakes receive,
but held output and cancel acknowledgment delay release; (7) synchronous protocol shutdown
wakes its pending built-in receive and self-wait is rejected; (8) finalization retains context
and claim through late quiescence or failed close, refusing unsafe reopen.

After tests are written red, the proposed managed commands are:

```text
# New filter; require a nonzero discovered count before accepting its result.
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~MacOSHidFidoRouteLifetimeTests"
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~MacOSHidConnectionLifetimeTests"
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~HidConnectionSlotTests"
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~FidoHidProtocol"
dotnet toolchain.cs -- build --project Core
```

At the initial pre-fixture checkpoint, `verification/MacOSHidRouteVerification`
published with real Core/Fido2 and the `.2`
package; its Native AOT binary statically links all 41 native exports, without a native
shared-shim dependency. `--list` executed discovery but found zero YubiKeys (exit 2),
corroborated by `system_profiler SPUSBDataType`. `--probe --serial 0` rejected invalid
input. No valid-serial route call occurred at that checkpoint; the subsequent selected-key
normal-path probe is recorded below. Synthetic Native AOT execution cannot substitute for
device evidence. Applicable criteria are ISC-9–15, ISC-20, ISC-26–28, ISC-31–33,
ISC-50, ISC-52–56
and ISC-60–63, plus ISC-41–48 and ISC-58 when native/package work is authorized.
ISC-49/52 are verified at managed-contract grade in the master; other mapped criteria
remain unchecked. At that checkpoint `.2` preview-package runs: Core 1,355 passed/3
skipped, PublicApi
22, Fido2 471, Management 86 and resilience-fast 77. The later queue-allocation fix was
followed by 17 focused macOS FIDO route passes, not a rerun of every suite. Native 11 and
synthetic AOT 5 passed per Engineer reports. Parent correctness review/fixes are not an
independent review; the user can inspect production code now. The later fixture removes
the availability blocker but not the touch/removal or comparison requirements.

### Selected-key checkpoint and bounded fit

On macOS 15.7.7 arm64 / .NET 10.0.0, the real-Core/Fido2 native-AOT host using local
`1.18.1-async.2` passed an explicit-serial read-only normal path on key 31683481
(firmware 5.7.4): three open/init/getInfo/dispose/reopen cycles. The same fixture passed
one read-only `PcscLifetimeIntegrationTests` case with three connection cycles and two
transaction/device-info reads per connection. Benchmark-tooling artifacts linked in
master Verification record three completed lifecycle samples, and a no-input sample
censored by the watchdog invocation (exit 1; child exit code unrecorded) at
`phase=invocation_returned`, return 79.9447 ms.
No shutdown request was recorded in the censored sample: it neither fails nor proves
dispose. No touch or unplug/removal probe, BEFORE dataset, agreed budget or performance
improvement exists. The selected normal path does not check ISC-31/33/58 or change the
2/72 master count.

After the latest `.2` restore, a supplied later checkpoint published the real-Core/Fido2
host with
`RestoreConfigFile=/var/folders/gn/mh64zz5969j89_f5dffnvnb80000kt/T/opencode/yubikit-async-native.nuget.config dotnet publish verification/MacOSHidRouteVerification/MacOSHidRouteVerification.csproj -c Release -r osx-arm64 --self-contained -p:PublishAot=true`
and ran
`./verification/MacOSHidRouteVerification/bin/Release/net10.0/osx-arm64/publish/MacOSHidRouteVerification --probe --serial 31683481`.
The probe passed three read-only cycles and pending raw receive → `DisposeAsync` → read
terminal → dispose complete → reopen/getInfo on the selected 5.7.4 key. Seventeen focused
post-craftsmanship managed tests also passed with the preview. This is real IOKit callback
acknowledgment during normal pending-read shutdown, **not** touch, unplug/removal or
interrupted callback shutdown; ISC-33 and all-platform ISC-58 remain unchecked. The
earlier empty-host result and earlier no-input sample censored before shutdown request
remain historical results, not counterevidence to the later probe. No BEFORE dataset or
performance improvement is claimed.

D30's bounded craftsmanship pass is local clarity, not a new architectural gate:
the separate Code Engineer completed named return codes, per-owner report capacity, a format
quarantine helper and trimmed comments; bounded review returned PASS WITH NOTES, and 17
focused post-cleanup managed tests passed at the historical 1.18.0 pin. Parent owns
evidence/docs; the later preview checks and actual-key pending-receive dispose/reopen
result are recorded above, not credited to the craftsmanship pass. Keep the nested
connection/native owner and public raw contracts. A two-turn cross-vendor Fable
consultation retained that overall shape and withdrew a shared slot interface. Value 2 /
cost 1 for local return/capacity clarity; value 1 / cost 1 for comments/docs. Keeping
the current shape unchanged leaves ambiguous names and stale claims; a shared scheduler
or slot interface increases scope without a second route, so defer it. Do not credit
new tests or hardware acceptance to this pass before they actually run.

### B1 — finite unchanged-route baseline assignment

The measurement Engineer owns only `benchmarks/Yubico.YubiKit.PerformanceBenchmarks/**`;
outputs go under `artifacts/measurements/`. No production/native changes are part of baseline
tooling. Record `db7a1bf6` as the unchanged transport-source anchor and the tooling revision.
For the comparable baseline use old transport source at `db7a1bf6` plus the **same**
locally built preview package `1.18.1-async.2` on both sides; record the package SHA-256
`514f3804201c26447273a48fef89fa31f1153eadf12ab9cbd6ea2c5266c60d1f`, source diff
and tooling revision. An old 1.18.0 versus new `.2` comparison would confound package and
transport effects. Choose one read-only fixture and record its identity before collection.

Implemented hardware modes, now run on the selected key: (1) read-only
open/initialize/getInfo/dispose/reopen, three completed samples; (2) raw receive with no
request sent, one sample censored after caller return and before any shutdown request.
Previously proposed (3) keepalive cancel/recovery and (4) overlap during a held exchange
remain deferred until a faithful fixture exists; they are not implemented modes.
Freeze repetitions and watchdogs before collection. Isolate each potentially blocking sample
in a child process, not many six-second reads behind a shorter aggregate watchdog. A killed
sample is blocked/censored evidence, not proof of recovery or native cleanup.

Collect caller-return and terminal duration, recovery/disposal duration, allocations,
thread counts and idle activity. Native durations, callback counts, queue depths and wakeups
are recorded only where observable; mark unavailable values instead of inferring them from
managed timing. Instrumented fake results and real-device/native results remain distinct.
Use existing benchmark tooling; do not introduce a general benchmark framework. The exit is
one raw dataset with revisions, environment, native package/source evidence, fixture firmware,
scenario, samples and missing evidence rows, plus proposed comparison budgets for approval.
Budgets constrain agreed recoverable scenarios and measured overhead; they do not invent
bounded cancellation/disposal of an arbitrarily hung native call or absent keepalive.

The earlier B1 tooling-only checkpoint ran no device operation; the later selected-key
samples above are AFTER-only, not a comparable baseline. No budgets are agreed. The
opt-in tooling
passed eight self-tests; dry-run touched no device; default listing preserved 12 existing
benchmarks. Exact executed commands (not baseline measurements):

```text
dotnet toolchain.cs -- benchmark --benchmark-args "--async-boundary-self-test"
dotnet toolchain.cs -- benchmark --benchmark-args "--async-boundary-baseline --dry-run"
dotnet toolchain.cs -- benchmark --benchmark-args "--list flat"
```

Child watchdog/progress classifies blocked synchronous prefixes, returned pending tasks,
synchronous/task faults and censored unknown cleanup. Native duration/report count are
null when unavailable; firmware is null with “not read” reason. Dataset provenance records
source commit/shipping diff hash (unavailable with untracked files), tooling hashes,
executing `.2` manifest and package/
deployed asset digests, architecture/runtime and selected serial. Cached archive native
asset matching deployed binary is not proof that a particular dynamic library loaded.
Next collect the unchanged-source BEFORE dataset with the same `.2` package before
interpreting overhead or improvement. The package declaration now reads
`1.18.1-async.3`; direct private-feed restore returned 403, while restore using the
downloaded identical workflow artifact succeeded. The earlier `.2` pending-read probe
followed a temporary local-feed restore. Replaying that preview
also needs tagged source plus dirty-file hashes, native macOS package scripts/inputs,
and a temporary local feed in the explicit `RestoreConfigFile` NuGet configuration for
restore/build/publish. Neither a configuration path alone nor this unsigned local build
is release proof.

## Subsequent bounded increments — historical `.3` evidence

These historical `.3` increments do not change the first-slice finish line or the
master 2/72 count. The [current status](00-status.md) supersedes the old hardware and
package gaps; parent verification and any requested commit follow separately.

| Milestone | Implemented and tested | Blocker and next evidence |
|---|---|---|
| S4 smart-card transaction | Additive public `ISmartCardConnection.BeginTransactionAsync`; built-in awaitable worker and documented synchronous fallback for external implementations. Eight focused managed tests passed; pre-OTP Core 1,363 and PublicApi 22 passed. | Selected PC/SC hardware integration test failed with a sharing violation; no selected-key transaction proof. Resolve contention and rerun, then cover context isolation and required platforms before S4 closure. |
| S2 macOS OTP GET/SET | Worker-owned feature GET/SET candidate; eight focused unit tests passed. After the change Core 1,371 passed/3 skipped, PublicApi 22, YubiOtp 180 and resilience-fast 77 passed. | No selected-key actual OTP device operation or touch; validate both directions and recovery with native/package and hardware evidence. This is not S5 completion or universal acceptance. |
| S2 macOS FIDO package transition | NativeShims `1.18.1-async.3` workflow [35888947280](https://github.com/Yubico/Yubico.NET.SDK/actions/runs/35888947280) completed SUCCESS with macOS/Linux/Windows builds, AOT consumers and private publish. Workflow artifact SHA-256 `b6df35457da06409f5bfd6643dd7dbc9da8b0e7e8404bb5fa99fcdde076f4a3c` restored locally. | Direct private-feed access returned 403; do not claim fresh feed consumption. Earlier normal/pending-read live FIDO probes used `.2`, not `.3`. No physical touch/removal/interrupted callback probe or scientifically valid BEFORE/AFTER comparison; `.3` selected-key route remains unverified. |

S5 still requires Windows OTP zero-access feature reports, cross-platform recovery and
remaining factory/raw/monitor/disposal boundaries. Native AOT publish/build evidence
does not substitute for their actual device execution. The selected-key sharing failure
and absent OTP/touch fixtures are evidence gaps, not proof that the user is unavailable.
