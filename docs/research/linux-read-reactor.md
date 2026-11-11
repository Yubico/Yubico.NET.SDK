# Linux read reactor — L2

Internal readiness infrastructure only. Existing Linux report connections/factories remain
synchronous. L3 owns typed FIDO/native-owner integration; L4 OTP; L5 registry/discovery wiring;
L6 platform/device acceptance. No adapter-registry rows or L1 seam/import changes are introduced.

## Two-phase consumer contract

```csharp
internal interface ILinuxHidReadReactor : IAsyncDisposable
{
    ILinuxHidReadRegistration Register(LinuxFileSafeHandle handle, long generation,
        Action<long, short> onReady, Action<Exception> onTerminal);
}
internal interface ILinuxHidReadRegistration
{
    long Generation { get; }
    Task<Exception?> Activation { get; }
    Task Quiescence { get; }
    Exception? RequestRemoval();
    void ReleaseReference();
    void Signal();
}
```

Register runs on the native owner worker, not a reactor. **Every published admission returns
its token**, including when its wake fails. Exceptions from Register occur only before
publication. `Activation` completes with an immutable result: null when applied by the poller,
or the initial failure/removal-before-activation error. The task itself does not fault.
Readiness may start immediately after activation; construct the owner before registering.

`RequestRemoval` publishes removal before wake and returns its stable first wake-error outcome
(null means no error, **not drain**). `Signal` is an explicit control wake; failure throws
IOException. Neither these outcomes, Activation, onTerminal, nor failed reactor DisposeAsync
prove native quiescence. The separate, nonfaulting `Quiescence` task completes only when the
reactor can no longer touch that identity through a snapshot, read callback or terminal callback.
A permanently broken wake leaves it pending; a later genuine poll return can complete the
**same token's** task despite the earlier error. No timeout or cross-thread close manufactures
an acknowledgement. The reactor roots the token and owner-capturing callbacks until release.

Only after Quiescence may the connection's **existing native owner worker** synchronously call
`ReleaseReference`. Early, reactor-thread (including a sibling callback), and concurrent calls
throw. The retained SafeHandle addref is released exactly once; repeated completed release is
a no-op, failed release cannot be retried and remains rooted. There is no implicit scheduling,
per-registration task, or native job pool. Even if an owner disposed its handle early and the
last addref executes device close, that close occurs on this explicit caller, never the poller.
Duplicate descriptor registrations are rejected until reference release finishes.

L3 pseudocode (not implemented here):

```text
owner.token = reactor.Register(owner.handle, generation, owner.OnReady, owner.OnTerminal)
activationError = await owner.token.Activation  // error does not lose owner.token
... mark terminal if activationError; keep the same native owner and physical claim ...
wakeError = owner.token.RequestRemoval()
await owner.token.Quiescence                   // still observe this after wake/shutdown failure
await owner.existingNativeWorker.Invoke(() => {
    owner.token.ReleaseReference()             // synchronous, guarded, exactly once
    outcome = reportApi.CloseOnce(owner.handle)
    if outcome.Released: release the same physical claim
    else: retain/quarantine owner and claim
})
```

Quiescence proves reference-use drain, **not device-close proof**. L1 `CloseOnce` evidence is
still required. While Quiescence is pending the owner/claim cannot be released. The failed
reactor remains terminal even if late drain lets its individual native owners clean up safely.

## One poller, including notification-fd retirement

`LinuxHidReadReactor.Shared` is lazy; an instance allocates a thread/eventfd only on admission.
One poller serves live tokens, using poll(-1), POLLIN only, bounded 32-callback rotating batches,
and control checks between callbacks and every EINTR retry. The native owner performs bounded
nonblocking TryRead in readiness mechanics; open/report write/ioctl/device close and application
code never run there. Consumer TCS continuations must run asynchronously.

When idle, the poller marks its run retiring and closes **only its private local eventfd** after
poll exits. Admissions arriving during this close receive retained tokens with pending Activation;
they do not signal the retiring fd or create replacement resources/jobs. After proven close, the
same thread creates at most one replacement wake fd and activates pending admissions. Otherwise
it exits; a later admission can start a fresh run. Unproven wake close retains the run/resource,
faults shutdown, and prevents replacements. Reactor disposal awaits quiescence and run retirement,
not owner-worker device release; failed disposal leaves actual token quiescence observable.

Source basis for in-poller **eventfd-only** retirement: Linux v6.12
[`fs/eventfd.c`](https://github.com/torvalds/linux/blob/v6.12/fs/eventfd.c), read 2026-11-11.
`eventfd_fops` has no flush/driver operation; `eventfd_release` performs wake_up_poll(EPOLLHUP),
eventfd_ctx_put/kref_put, and local ida_free/kfree. It has no wait_event/device wait. The fd is
private and no poll/wake syscall is live at retirement. This is a local notification-resource
classification, **not** a hard wall-clock syscall bound or permission to close hidraw on the
reactor. A held synthetic release still cannot spawn replacement threads/fds or pool jobs.

Eventfd transfers require exactly 8 bytes, EAGAIN is benign/already readable, and EINTR is bounded
to four attempts; one non-semaphore read drains the counter. Persistent poll errors/four EINTR,
success without flags, or wake errors exit terminally, never spin. POLLERR/HUP/NVAL precede input
and remove that registration; callback faults also remove it, with contained terminal notification.
Callbacks may request removal but must not synchronously wait for Quiescence.

## Evidence and bounds

`LinuxHidReadReactorTests` covers recoverable failed admission through the consumer interface,
late actual quiescence and cleanup of the same real registry claim on a test native worker,
held owner release, held eventfd retirement with pending admissions/removals, misuse guards,
stale identity/generation and actual fd reuse, callback/terminal drain, bounded faults/batches,
and sequential lifetime/resource bounds. Real Linux/glibc eventfd/poll tests reuse the L0
WithheldNativeProbe and owned F_DUPFD_CLOEXEC duplicates; no stdio/device nodes are closed.

The exact source inventory adds only poller startup: **205 sites / 132 imports / 25 waits /
16 scheduling sites**. No cleanup Task.Run, new imports/waits, global cross-key scheduler,
L3 owner, factory migration, physical hidraw, musl, native-AOT execution or universal Linux
acceptance is claimed. Slow/hung internal callbacks or a permanently failed wake remain liveness
bounds; the observable pending Quiescence and rooted native owner are deliberate, not teardown.
