# First implementation slice: smart-card connection lifetime

Status: first slice implemented and verified on 2026-09-22; independent review passed
with low-priority notes. This is smart-card slice evidence, not completion of the epic.
The user approved the single-key product and architecture and replaced whole-effort
upfront specification with incremental implementation on 2026-09-22. Following
pseudocode acceptance, implement, test and review each slice; resolve non-blocking
details live rather than requiring a complete epic program design or slice catalogue.

The [master plan](../../../2026-09-21-yubikit-async-boundaries-ISA.md) remains the
acceptance record. The [earlier detailed program design](addenda/earlier-multi-key-program-design.md)
is preserved as research/reference, not a prerequisite or an implementation instruction.

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

## Deferred to implementation effort

| Detail | When to resolve |
|---|---|
| Internal helper/seam names, final method signatures and exact file list | Deferred to implementation effort — choose against the first failing tests and surrounding code. |
| Precise task, lock, signal and finalizer plumbing | Deferred to implementation effort — demonstrate the safety rules above before considering the slice complete. |
| Exact new public transaction signature and consumer-example updates | Deferred to implementation effort — discuss when that surface is reached; preserve existing synchronous expert behavior meanwhile. |
| Per-connection report-buffer size and platform operation-record shapes | Deferred to implementation effort — before their respective adapter changes. |
| Single-key measurement runner details and numerical budgets | Deferred to implementation effort — record the unchanged baseline before altering production behavior and agree relevant thresholds. |
| macOS bridge exports, callback-timeout interpretation and supported system floors | Deferred to implementation effort — resolve before enabling the affected native path. The approved IOKit expansion remains planned. |
| Native package-producing revision and new-package compatibility | Deferred to implementation effort — remains a hard prerequisite for native package changes; not declared resolved by managed tests. |
| Windows/Linux/macOS adapter order, broader inventory automation and subsequent slices | Deferred to implementation effort — select the next route from first-slice evidence. Full epic obligations remain in the master. |

These entries postpone up-front design, not required correctness checks. Surface a
material public-contract, architecture or safety trade-off before implementing it.
Routine choices within approved conventions can be made and reviewed during the slice.
Multi-key capacity and scheduling remain in the separate addendum rather than leaking
back into this iteration. No full-epic completion is claimed by a first working route.

## Slice result

The built-in smart-card path now uses `PcscConnectionNativeState` and a narrow
`ISCardConnectionApi` seam. Both direct-slot and published-device discovery routes
transfer their claims to checked native cleanup. `UnrecoveredConnectionException`
distinguishes retained ownership. Existing synchronous transaction entry points remain;
their native calls are owned and scope reuse/teardown races are covered.

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
