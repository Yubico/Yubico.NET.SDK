# B3 additions and activated history-replacement whole units

**No content verdicts.** Pinned source `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; read enclosing owner/helper/assertion context as needed.

## New B3 XML expansion

### `core-001:unit:e3d8ada85fd182e2`
`src/Core/src/Transports/Hid/MacOS/MacOSHidInterface.cs:118–127`; **UNREAD / zero clean credit**.

````csharp


    /// <summary>
    ///     Establishes a connection capable of transmitting IO reports to a FIDO device.
    /// </summary>
    /// <returns>
    ///     An active connection object.
    /// </returns>
    public IHidConnection ConnectToIOReports() =>
        new MacOSHidIOReportConnection(_entryId);
````

### `core-001:unit:76b664da02352c87`
`src/Core/src/Transports/Hid/MacOS/IIOKitDeviceLifetime.cs:106–112`; **UNREAD / zero clean credit**.

````csharp


    /// <summary>
    ///     Registers, or with zero <paramref name="callback" /> and <paramref name="context" /> clears, the
    ///     device removal callback.
    /// </summary>
    void RegisterRemovalCallback(nint device, nint callback, nint context);
````

## Existing seven replacements now activated

### `core-001:unit:4c48c7d8d8d2e37a`
`src/Core/CLAUDE.md:600–603`; **UNREAD / zero clean credit**.

````markdown
### ConnectionType Semantics

`ConnectionType` is a `[Flags]` enum with explicit values. `HidFido`, `HidOtp`, and `SmartCard` represent concrete discovered device interfaces. `Hid` is a group filter that includes both HID FIDO and HID OTP interfaces when used with discovery/cache filtering APIs. `Unknown` matches no devices.


````

### `core-001:unit:4c955b22a2de68df`
`docs/research/linux-native-acceptance.md:247–306`; **UNREAD / zero clean credit**.

````markdown
## Publish binding and fail-closed result gates

The toolchain target restores the standalone host, invalidates the previous stamp,
and records inputs **before** the two publishes. Only equal before/after inputs
permit `publish-manifest.json` to be issued. The record binds:

- The source manifest and actual Git HEAD identity, the exact Release/linux-x64/self-contained publish
  options and SDK version.
- Evaluated JIT/AOT MSBuild properties (including constants, trimming, optimization,
  globalization and SDK paths), hashes of reported imported files, and explicit
  build/toolchain environment knobs. Launcher-injected `DOTNET_ROOT_X64` is not
  compared as an unset-versus-set string: effective SDK paths/version/import hashes
  describe the resolved SDK instead.
- The resolved dependency closure and contents of **17 packages**, including native
  archives, build targets and managed compiler dependencies—not just package labels.
- The complete file set and SHA-256 contents of **223 published artifacts**, covering
  apphosts, the JIT host and dependency assemblies, runtime/native dependencies and
  other published files. Added, removed or modified artifacts invalidate the binding.

Current publish **INPUTHASH**:
`dd147bacd10c7b10fdb041627fac325abb9698c620a0c57a4f19bda098ab57ad`.
Publish-manifest SHA-256:
`b83ec7ee30f29b082d09f5d31c4430762989d10aba6fd5553ce0921318d286bb`.

Capture independently checks that manifest against current inputs and every published
artifact **before starting any child/discovery/device operation**, and repeats the
check before/after each child. Missing manifests, changed source/effective inputs,
changed package content or stale binaries/assemblies fail with exit 1; there is no
fallback to labeling an old binary with a new hash. The environment `SourceHash`
remains **a label only**, derived from the verified publish record, not attestation.
This is a local unsigned stable-input build relationship, not producer signing or
protection against a malicious concurrent writer. Source changes during publish
that differ at the before/after snapshots prevent sealing. After this final capture,
only excluded documentation/result files were refreshed, avoiding self-reference.

CI and local capture now invoke the **same `evidence_gate.validate_result`** through
`capture.py --native-only` / normal capture. Both require `DynamicCode is true` for
JIT and `is false` for AOT (not truthiness), correct mode/source label, exit 0,
`Outcome=PASSED`, no failure and nonempty unique checks. Native mode independently
requires the exact **38 named checks**. Intended negative controls still require
exit 1, `Outcome=FAILED`, the exact assertion prefix and exact expected cleanup
checks; a crash, unrelated exception or watchdog cannot substitute.

`test_evidence_gates.py CAPTURE_DIRECTORY` runs **16 passing tests**, including real
JIT JSON substituted for AOT and the reverse, missing/duplicate/empty checks,
incorrect outcomes/runtime values, exact negative controls, stale source, stale
binary and JIT assembly, missing stamp, extra artifact, package/effective-build
changes, and source changes during publishing. Filesystem binding cases use isolated
temporary source/artifact fixtures, not modifications to checkout code or live
published binaries. The stale-source/binary/missing-stamp tests also execute capture's
real entry point and assert that neither a child nor the hardware snapshot starts.
The real-record runtime regressions were observed red against the old gate (three
failed rejection assertions), then green. The live CLI also rejected a missing
manifest and a test-source edit after publish before any operation.

The CI-equivalent native-only command was previously executed successfully (exit 0;
capture `20261111T034902Z`). The final refresh instead runs normal capture and the
same **16 gate tests against the fresh `20261111T040627Z` records**. This is not a
hosted workflow-run claim. Normal capture remains exit **2** for blocked hardware.


````

### `core-001:unit:0b2135f2f301e2c1`
`docs/research/linux-read-reactor.md:99–112`; **UNREAD / zero clean credit**.

````markdown
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

````

### `core-001:unit:d8f176c067aa0fd3`
`docs/research/linux-fido-async-lifetime.md:57–77`; **UNREAD / zero clean credit**.

````markdown
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


````

### `core-001:unit:b45b5794aa92008c`
`docs/research/linux-read-reactor.md:69–98`; **UNREAD / zero clean credit**.

````markdown
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


````

### `core-001:unit:4330e60d07cd5417`
`docs/research/linux-native-acceptance.md:101–202`; **UNREAD / zero clean credit**.

````markdown
## Historical physical checkpoint — `20261111T115147Z`

After the operator released HID from the VM, the existing source-bound capture
completed with **exit 0**, without republish or source changes. The earlier
`20261111T040627Z` blocked capture is preserved below as historical evidence, not
the current hardware status. The durable JSON preserves this checkpoint as
`currentHardwareCheckpoint` for compatibility; the newer `prPreparationCheckpoint`
is current for PR preparation. Historical pending fields do not describe this checkpoint.

- SOURCEHASH remains
  `36b75d0310e6ae896975189f8be0138ba8ae48a52127474029de22d577d278aa`.
- INPUTHASH remains
  `dd147bacd10c7b10fdb041627fac325abb9698c620a0c57a4f19bda098ab57ad`;
  publish-manifest SHA-256 remains
  `b83ec7ee30f29b082d09f5d31c4430762989d10aba6fd5553ce0921318d286bb`.
  All 223 published artifacts and effective/package inputs were checked before
  execution and before/after each child. No RIDless restore or build intervened.
- Both runtimes independently discovered exactly one key and agreed positive
  **serial 103**, exposing `HidFido, HidOtp, SmartCard`. No arbitrary fallback was used.
- Management device info reports **5.8.0** through typed OTP, direct feature and
  PC/SC, consistently across three cycles per route/runtime. CTAP GetInfo and the
  FIDO session report **0.0.1** with five advertised versions. These are distinct
  observed firmware reports from this development fixture, not normalized to one value.
- USB `3-1.3`, `1050:0407`: OTP `/dev/hidraw0` and FIDO `/dev/hidraw5` are
  readable/writable and bound to `usbhid`; CCID `1.2` has no kernel driver.
  **That is not a PC/SC blocker**: the actual PC/SC route passed. No service or
  binding was changed to obtain that result.

| Route | JIT named checks | AOT named checks | Physical result per runtime |
|---|---:|---:|---|
| `--list` | 1 | 1 | Exactly one key, same positive serial |
| `--fido` | 12 | 12 | Actual Linux owner; 3 cycles, borrowed idle reuse, pending read disposal and reopen |
| `--otp` | 9 | 9 | Actual Linux owner; 3 GET8 / selected Management-info / dispose cycles |
| `--raw` | 3 | 3 | Selected public raw CTAP factory, INIT/GetInfo/reopen over 3 cycles |
| `--expert-io` | 9 | 9 | Actual direct Linux facade; indefinite read disposal wake; 3 descriptor64 / INIT/GetInfo cycles |
| `--expert-feature` | 9 | 9 | Actual direct Linux feature facade; 3 metadata8 / GET8 / selected-info cycles |
| `--smartcard` | 1 | 1 | Aggregate check covers 3 open/begin/select/read/end/dispose cycles, 2 reopens |
| **Six physical modes** | **43** | **43** | **12 successful mode executions; 86 physical named checks total** |

Both native runs also passed their exact **38 checks**; all four intended negative
controls matched their exact failure/cleanup contracts. Total: **20 child runs**,
16 positive exits 0 and four intended exits 1; **164 positive named checks**
(76 native + 86 physical + 2 enumeration). The two quiescence negative controls
record two cleanup checks each, not additional positive acceptance. No watchdog expired.

Representative single-run SDK timings (milliseconds, **caller return / completion**):

| Operation | JIT | AOT |
|---|---:|---:|
| First typed FIDO open | 12.1704 / 66.8802 | 0.1437 / 51.2908 |
| First FIDO GetInfo after initialization | 0.0889 / 25.7251 | 0.0112 / 25.9580 |
| Pending typed FIDO read disposal | 0.3069 / 2.8782 | 0.0086 / 2.4982 |
| First OTP feature GET8 | 0.5265 / 1.9441 | 0.0013 / 0.2166 |
| First OTP Management info | 0.0926 / 29.4372 | 0.0055 / 27.3601 |
| Direct I/O disposal wake | 2.6247 / 6.6978 | 0.0427 / 2.6296 |
| First direct I/O INIT/GetInfo | 38.1859 / 38.2580 | 32.1260 / 32.1267 |

The direct compatibility path is deliberately synchronous: its caller-return time
can include the exchange. These samples include managed/native/protocol overhead,
not kernel-only durations or approved budgets. Process-wide allocation deltas are
recorded locally but are not per-operation attribution or a hardware idle budget.
PC/SC open invocation samples were 15.158/0.570/0.276ms (JIT) and
0.281/0.127/0.157ms (AOT), all pending at return; the shared scenario does not
measure total PC/SC operation latency. Direct feature has no per-call timings.

A separate post-capture **descriptor-only** probe on the same sysfs-associated
nodes performed no report read/write/GET/SET. `HIDIOCGRDESCSIZE` and `HIDIOCGRDESC`
both returned **0** on both nodes, with **71 bytes OTP / 34 bytes FIDO**, and both
open/close pairs succeeded. Thus direct FIDO's asserted 64/64 metadata is backed
by independently successful descriptor queries, not merely an assumed fallback;
the SDK capture itself does not instrument its ioctl return values. Probe ioctl
wall samples were OTP 0.005476/0.001675ms and FIDO 0.008767/0.002779ms; these are
Python `fcntl` wrapper-inclusive durations, **not SDK or kernel-only timings**.
Descriptor hashes and exact open/close samples are retained in the durable JSON.

Raw local evidence: `artifacts/linux-hid-verification/evidence/20261111T115147Z/`.
The original capture has 20 JSON results, 20 logs, source/publish manifests,
provenance, summary and a SHA index; those raw files were not edited. The later
`descriptor-probe.json` is supplemental and has its own durable hash. All raw
JSON/log identities are transitively bound by the preserved SHA-index hash.

Fresh validation: **16/16 gate/provenance tests passed** against this capture;
all 20 result contracts and the original raw-file hash index were revalidated,
and final publish verification and `git diff --check` passed. The diff check emits
an existing Core project LF/CRLF conversion warning, not a whitespace error.
Gate log: `artifacts/linux-hid-verification/validation/hardware-gate-regressions-20261111T115147Z.log`.
Commands:

```bash
python3 verification/LinuxHidRouteVerification/publish_contract.py verify
python3 verification/LinuxHidRouteVerification/capture.py
python3 verification/LinuxHidRouteVerification/test_evidence_gates.py artifacts/linux-hid-verification/evidence/20261111T115147Z
python3 verification/LinuxHidRouteVerification/publish_contract.py verify
git diff --check
```

**Remaining scope:** separately coordinated touch/active-cancel/removal; hosted CI
execution; isolated SDK hardware-native read/write/ioctl timing; representative
allocation/idle CPU/worker telemetry; pre-change physical baseline and approved
budgets. No global ISC/ISA acceptance checkbox was advanced. All commands finished;
no verifier child remains running.


````

### `core-001:unit:457a4879daaf1b55`
`docs/research/linux-otp-async-lifetime.md:50–69`; **UNREAD / zero clean credit**.

````markdown
## Shutdown, unexpected failure and finalization

Shutdown closes admission under the owner lock and returns one shared completion/outcome. An
accepted GET/SET runs to native return and public task terminality before checked close on the
original worker. Sync disposal drains that same outcome, refusing worker-thread self-drain.
No timeout or cross-thread close manufactures native abort or release proof.

Only L1 Released=true permits original-claim release/root removal. Linux EINTR/EIO prove descriptor
release while preserving a close diagnostic. EBADF, unknown evidence, close exceptions or claim
release failure retain/root/quarantine the original lifetime. Actual registry claims use the
existing TryMarkUnrecovered hook. Repeated disposal cannot retry native close or release a successor.

An unexpected loop error stops admission, faults pending actual open/send/receive tasks and clears
owned staging under the same lock. The surviving original worker performs checked cleanup; proven
release still releases the claim, but disposal retains the initial loop error, aggregated with
cleanup diagnostics. Failed/interrupted cleanup remains quarantined, with no replacement worker.
The event is retired under the setter/admission lock only after stopping is visible. A later
Dispose/finalizer request therefore never Sets a disposed event. Facade finalizers only request
shutdown with a nonthrowing backstop; collecting the facade cannot unroot unknown native ownership.


````

