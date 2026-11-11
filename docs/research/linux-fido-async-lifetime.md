# Linux FIDO async lifetime — L3

Scope: one FIDO native owner and the direct synchronous expert facade over it. L0/L1/L2 remain
separate accepted layers. No OTP owner (L4), typed factory/discovery/registry transfer (L5), adapter
registry rows, physical hidraw execution, native-AOT or universal platform acceptance (L6).

## Exact seam and ownership

```csharp
internal static Task<LinuxFidoHidConnection> OpenAsync(
    string path, ILinuxHidReportApi reportApi, ILinuxHidReadReactor reactor,
    CancellationToken cancellationToken, IDisposable? physicalClaim = null);
```

`LinuxFidoHidConnection` implements `IFidoHidConnection` and `ITerminalWakeControl`. Its private
native lifetime is `LinuxFidoHidNativeOwner`, also used by `LinuxHidIOReportConnection`. The owner
roots itself with GCHandle before accepting the claim/acquiring native state. Facades are not
rooted by it: their finalizers can request nonblocking shutdown even while native state stays rooted.

One lazily started dedicated background worker owns open (`O_RDWR|O_NONBLOCK|O_CLOEXEC`), descriptor
ioctl, registration, activation wait, writes, actual quiescence wait, reference release and checked
close. There is one ordinary operation slot (send OR receive), no ordinary backlog, global
scheduler, command framework, replacement worker, operation Task.Run or cleanup pool job. The
opening caller's ExecutionContext does not flow to the worker. Cancellation during open waits for
native return and safe cleanup; a pre-canceled open never acquires native state.

The token is stored before Activation is observed, including failed published admissions. Failed
open cleanup preserves both the original and cleanup errors in an AggregateException. Every
published token stays attached to this same owner through real drain. An activation result,
terminal notification or failed removal wake never substitutes for Quiescence.

## Report and cancellation behavior

* Send checks entry cancellation and typed length64, admits once, copies to owned storage, checks
  cancellation on worker dispatch, invokes native write at most once and clears the copy before
  completing its public task. Cancellation during native output cannot detach that task or replay
  a possibly applied mutation. After dispatch, successful output may still succeed.
* Receive checks entry cancellation, admits one reader, then consumes an exact owned queued report
  or installs a unique pending reader. Typed Receive rejects non64 reports terminally; direct raw
  input preserves descriptor-sized capacity and exact actual length, including short reports.
* The reader's RunContinuationsAsynchronously TCS task **is** the public task. There is no async
  wrapper whose registration disposal could remain pending after only an internal TCS is canceled.
  Cancellation detaches only the matching reader under the owner lock; unregister is nonblocking.
  Delivery/cancellation completes exactly once, and late/undelivered reports remain for the next
  read. Cancellation of an old delivered read cannot detach a successor.
* OnReady accepts only the original immutable generation, performs at most32 nonblocking TryRead
  attempts per callback and at most four EINTR attempts per batch. EAGAIN returns; EOF, malformed
  counts and native errors fault terminally. No waits, ioctl, write, close or application
  continuations execute there. Temporary and rejected reports are cleared.
* The owned input queue holds160 reports, allowing the selected-key maximum129 FIDO message
  frames with headroom. It is not a universal overflow guarantee. Overflow faults rather than
  dropping, clears abandoned queued buffers and requests removal so unread level-triggered input
  cannot spin the poller. Accepted returned arrays transfer to callers and are not cleared later.
* RequestTerminalWake is terminal read wake only. Existing FidoHidProtocol decides when active
  protocol disposal needs it; idle borrowed-session disposal does not stop this connection's input.

## Shutdown and release evidence

All Dispose/DisposeAsync callers share the exact same owner completion/outcome. Shutdown stops
admissions and completes an admitted public reader before returning, requests token removal even
while write occupies the worker, then lets the original worker finish any accepted output. That
worker waits indefinitely for the same token's true Quiescence, calls ReleaseReference synchronously,
and only then calls L1 CloseOnce. Sync Dispose rejects owner-worker and reactor-thread self-drain
(including sibling reactor callbacks); LinuxHidReadReactor exposes its existing thread marker for
this guard only. No reactor contract or scheduling semantics changed.

Only Released=true permits original-claim disposal and root removal. EINTR/EIO close diagnostics
still report an error, but **do** release that original claim: no quarantine of an already released
fd. EBADF, unknown release and reference-release failure retain/root/quarantine the owner/claim
through DeviceConnectionRegistry's unrecovered-registration hook. Claim-disposal failure also
retains the root. Close and reference release are not retried by subsequent disposal calls.

A failed wake is preserved as a diagnostic while shutdown remains pending for real drain. A late
genuine Quiescence lets that same original worker release that same original claim; it cannot free
a successor's claim. Permanently broken wake or hung native calls may remain pending indefinitely.
Finalization only requests shutdown, never blocks or declares release by timeout.

### Unexpected worker-loop failure

The owner tracks accepted open/send completion and its owned output copy independently of the
dispatch delegate, including after that delegate is removed for dispatch. An outer loop failure
atomically closes admission, faults the actual pending public read and open/send tasks, clears
queued output, and requests reactor removal under the same owner lock. An already-terminal
operation is not retroactively changed. The still-running original worker then executes the same
true-Quiescence / ReleaseReference / checked-close path; no replacement or cleanup job is created.

If native cleanup succeeds, the original claim and root are released but disposal still faults
with the **original loop exception**. Additional wake, close or release errors preserve that cause.
Failed/interrupted cleanup quarantines uncertain ownership, without retry or cross-thread close.
The signal is retired under the admission lock after stopping is visible, serializing retirement
with every accepted setter. Subsequent disposal observes the same completion, not a disposed-event
exception. Finalizer RequestShutdown contains a nonthrowing backstop and cannot assert release.

## Direct compatibility and deliberate differences

Public IHidInterface/IHidConnection signatures remain unchanged. The direct constructor/Get/Set
are explicitly synchronous waits over the owner. Metadata remains descriptor-based (1..4096), raw
reads preserve actual payload length, and SET preserves supported arbitrary caller length rather
than requiring output metadata length or64. Original Linux indefinite reads are retained; no macOS
six-second timeout is imported. EOF now deliberately faults terminally instead of returning an empty
successful report, consistent with reactor/typed end-of-stream handling. Direct opens still do not
implicitly acquire a grouped physical registry claim. Typed factory routing is not changed here.

## L4 handoff / frozen sharing boundary

No shared worker utility was needed or introduced. `LinuxFidoHidNativeOwner` is FIDO/raw-IO-specific:
do not use it as an OTP operation/command framework. L4 retains L1's borrowed feature API and must
provide its own small native owner (or separately justify a mechanical extraction), preserving
pre-native rooting, one worker/no backlog, owned async buffers, shared checked shutdown and original
claim release evidence. L2's token contract remains unchanged; OTP feature ioctl must never run on
the read reactor. L5 is responsible for transferring real connection/discovery claims into owners.

## No-hardware evidence and inventory

`LinuxFidoHidConnectionTests` exercises the production owner with fake L1/L2 seams and L0's
WithheldNativeProbe: invocation return during held open/metadata/write/checked close, pre-cancel
and pre-dispatch cancellation, cancellation while native work is held, owned-copy isolation,
overlap refusal, delivery/cancellation races, stale generation and stale cancellation, public
reader terminal-before-disposal, terminal wake, overflow/batched input, EINTR/EOF/error removal,
metadata/register/activation/reference-release failures, real registry quarantine, late exact-claim
release and diagnostic released-close branches. A GC/finalizer test drops the facade while reactor
drain is held, then releases the same lifetime; it uses only fake seams in an isolated child via
the existing PcscIsolatedProbe harness. This is not arbitrary process-abandonment or hardware evidence.
L1 direct framing/descriptor assertions remain intact with the fake reactor now injected explicitly.

`LinuxFidoWorkerFailureTests` adds seven real-worker interruption cases: idle pending receive,
queued send and owned-copy clearing, queued open before native acquisition, released EIO and
unproven EBADF cleanup, interruption during actual quiescence (real registry quarantine without
retry), and an isolated finalizer while the original worker still drains. Tests capture the real
Thread through fake open; queued-work placement uses test-only private-state access to hold the
actual admission lock, not a production scheduler hook or substitute worker. The idle-reader
regression was observed failing with a watchdog timeout before the fix. All seven use fake native
and reactor seams, never device operations.

The reviewed source inventory is213 sites /132 imports /32 waits /17 scheduling sites. Relative to
L2, L3 removes one direct-facade Task.Run disposal site and adds four owner waits, three synchronous
facade waits and two mutually exclusive context-start sites for the single native worker. Entries
remain documented/outstanding, not verified or an adapter acceptance claim.
The worker-failure correction changes no source-site identities or counts; those exact entries
now document fault cleanup and signal retirement as well as ordinary worker parking/drain.
