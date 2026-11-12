# B2 expansion and separate replacement original units

**No content verdicts.** Source `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Each excerpt is one exact original unit; read its named enclosing source context as needed.

## `core-001:unit:9d8102989bf52740`
MATERIAL-MISS-FULL-ORIGINAL-XML-QUOTA; `src/Core/src/Transports/Hid/Linux/LinuxHidEventSource.cs:236–257`; original rank 18.

````csharp


    /// <inheritdoc />
    public LinuxHidUdevEvent? ReceiveEvent()
    {
        if (_monitorHandle is null || _monitorHandle.IsInvalid)
        {
            return null;
        }

        using var device = UdevNativeMethods.udev_monitor_receive_device(_monitorHandle);
        if (device.IsInvalid)
        {
            return null;
        }

        var action = PtrToString(UdevNativeMethods.udev_device_get_action(device));
        var stableIdentity = GetStableUdevIdentity(device);
        var devNode = PtrToString(UdevNativeMethods.udev_device_get_devnode(device));

        return new LinuxHidUdevEvent(action, stableIdentity, devNode);
    }
````

## `core-001:unit:f00b1d2526f62fe3`
MATERIAL-MISS-FULL-ORIGINAL-XML-QUOTA; `src/Core/src/Transports/Hid/Linux/LinuxFidoHidNativeOwner.cs:230–272`; original rank 19.

````csharp


    /// <summary>Admits one raw send and copies its payload before returning the actual operation task.</summary>
    /// <param name="report">Caller-owned payload (0..16383 bytes), not required to match metadata; typed callers enforce 64 bytes.</param>
    /// <param name="token">Checked at entry and worker dispatch; after dispatch it cannot detach or abort output.</param>
    /// <returns>A task terminal only after dispatched output returns and the owned copy is cleared, or pre-dispatch cancellation/failure.</returns>
    /// <remarks>
    /// Admission takes the owner lock and rejects overlap. The caller retains and clears its source;
    /// the worker borrows only the owned copy. Native failure is not replayed or proof of no mutation.
    /// Successful output may succeed despite cancellation requested after dispatch.
    /// </remarks>
    internal Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        // Bound the escaping copy too, not only L1's temporary native framing.
        ArgumentOutOfRangeException.ThrowIfGreaterThan(report.Length, 16383);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_sync)
        {
            Admit();
            // Escapes to the native worker; never borrow caller storage after publication.
            byte[] copy;
            try { copy = report.ToArray(); }
            catch { _active = false; throw; }
            try
            {
                _workCompletion = completion;
                _outputCopy = copy;
                _work = () => Send(copy, token, completion);
                _ = _signal.Set();
            }
            catch
            {
                CryptographicOperations.ZeroMemory(copy);
                _work = null;
                _workCompletion = null;
                _outputCopy = null;
                _active = false;
                throw;
            }
            return completion.Task;
        }
    }
````

## `core-001:unit:e5b5533e4df67ea7`
MATERIAL-MISS-FULL-ORIGINAL-XML-QUOTA; `src/Core/src/Transports/Hid/Linux/ILinuxHidReportApi.cs:46–49`; original rank 20.

````csharp

    /// <summary>Sets one caller-length payload (0..16382 bytes) using zero-ID framing and a length-encoded feature ioctl.</summary>
    /// <remarks>Requires the complete payload-plus-one native count. Does not retry or impose typed OTP length.</remarks>
    void SetFeature(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload);
````

## `core-001:unit:727ad82dd732b613`
MATERIAL-MISS-FULL-ORIGINAL-XML-QUOTA; `src/Core/src/Transports/Hid/Linux/LinuxHidReadReactor.cs:435–437`; original rank 21.

````csharp

        /// <summary>Gets the eventfd number used in this run's poll snapshots.</summary>
        internal int Descriptor { get; private set; }
````

## `core-001:unit:1dcf2e836efee1d5`
MATERIAL-MISS-FULL-ORIGINAL-XML-QUOTA; `src/Core/src/Transports/Hid/Linux/LinuxHidEventSource.cs:92–98`; original rank 22.

````csharp


    /// <summary>
    /// Returns true when the hidraw device node exists and can be opened, indicating the node
    /// is ready for enumeration by discovery rescans.
    /// </summary>
    bool IsHidrawReady(string? devNode);
````

## `core-001:unit:9e1f0228fabdcb5d`
MATERIAL-MISS-FULL-ORIGINAL-XML-QUOTA; `src/Core/src/Transports/Hid/Linux/LinuxFidoHidNativeOwner.cs:291–324`; original rank 23.

````csharp


    /// <summary>Admits one reader for the next exact-length owned report, consuming queued input when available.</summary>
    /// <param name="typed">Whether delivery must contain exactly 64 bytes; a mismatch clears the report and terminalizes input.</param>
    /// <param name="token">Checked at entry; cancellation of a pending reader detaches only that reader, not native input.</param>
    /// <returns>The reader's actual completion task, yielding caller-owned array storage through a read-only view.</returns>
    /// <remarks>
    /// Raw input preserves actual read length rather than padding to descriptor capacity. Delivery
    /// races cancellation exactly once; undelivered reports remain available for a successor. The
    /// owner clears temporary, rejected and abandoned queued reports, but never a successfully returned
    /// array, including during shutdown. The view offers no disposal/zeroing API; read-only does not
    /// mean immutable or automatically erased. Sensitive successful output requires caller-managed
    /// backing-storage cleanup; copying it does not erase the original. Continuations run asynchronously.
    /// </remarks>
    internal Task<ReadOnlyMemory<byte>> ReceiveAsync(bool typed, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        lock (_sync)
        {
            Admit();
            var reader = new TaskCompletionSource<ReadOnlyMemory<byte>>(TaskCreationOptions.RunContinuationsAsynchronously);
            _reader = reader;
            _typedReader = typed;
            if (_reports.TryDequeue(out var report)) CompleteReadLocked(report);
            else
            {
                // May invoke immediately; identity check detaches only this reader. No
                // async wrapper: this TCS task IS the public completion/drain barrier.
                _readerCancellation = token.UnsafeRegister(_ => CancelRead(reader, token), null);
                if (_reader is null) _ = _readerCancellation.Unregister();
            }
            return reader.Task;
        }
    }
````

## `core-001:unit:f82d55d29d5e7e42`
MATERIAL-MISS-FULL-ORIGINAL-XML-QUOTA; `src/Core/src/Transports/Hid/Linux/LinuxHidIOReportConnection.cs:62–69`; original rank 24.

````csharp


    /// <summary>Gets the descriptor-derived output payload size in bytes, excluding the native zero report ID.</summary>
    /// <remarks>
    ///     The native seam falls back to 64 when descriptor queries are unavailable. This is metadata, not an
    ///     exact-length requirement on <see cref="SetReport" />; direct output preserves supported caller lengths.
    /// </remarks>
    public int OutputReportSize => _owner.OutputReportSize;
````

## `core-001:unit:fb2be78aaf8f6f25`
MATERIAL-MISS-FULL-ORIGINAL-XML-QUOTA; `src/Core/src/Transports/Hid/Linux/ILinuxHidReportApi.cs:28–31`; original rank 25.

````csharp

    /// <summary>Opens a caller-owned descriptor with close-on-exec, or throws on native failure.</summary>
    /// <remarks>The owner must drain all borrowers and inspect <see cref="CloseOnce"/> before releasing its physical claim.</remarks>
    LinuxFileSafeHandle Open(string path, NativeMethods.OpenFlags flags);
````

## `core-001:unit:e9d68bf588ab00e4`
MATERIAL-MISS-FULL-ORIGINAL-XML-QUOTA; `src/Core/src/Transports/Hid/Linux/ILinuxHidReadReactor.cs:58–60`; original rank 26.

````csharp

    /// <summary>Gets the immutable connection generation supplied at registration.</summary>
    long Generation { get; }
````

## `core-001:unit:82f001f6db22d507`
MATERIAL-MISS-FULL-ORIGINAL-XML-QUOTA; `src/Core/src/Transports/Hid/Linux/LinuxHidEventSource.cs:79–86`; original rank 27.

````csharp


    /// <summary>
    /// Receives one pending udev event. Returns null when the receive fails — which is most
    /// likely during event storms (for example ENOBUFS after the kernel dropped notifications),
    /// exactly when a removal may have been lost.
    /// </summary>
    LinuxHidUdevEvent? ReceiveEvent();
````

## `core-001:unit:cf330b6a639970cd`
MATERIAL-MISS-FULL-ORIGINAL-XML-QUOTA; `src/Core/src/Transports/Hid/Linux/LinuxHidDeviceListener.cs:1–34`; original rank 28.

````csharp
// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

/// <summary>
/// Linux implementation of HID device listener using udev_monitor with poll().
/// </summary>
/// <remarks>
/// <para>
/// The listener does not auto-start. Call <see cref="Start"/> after setting up
/// <see cref="HidDeviceListener.Changed"/> callback.
/// </para>
/// <para>
/// Each <see cref="Start"/> creates an independent session that owns its native resources
/// (udev context, monitor, shutdown event fd). The session's listener thread disposes those
/// resources when its loop exits. If <see cref="Stop"/> times out waiting for a wedged thread,
/// the cancelled session is abandoned rather than torn down, so a late-exiting thread can never
/// touch recycled file descriptors or state belonging to a newer session.
/// </para>
/// </remarks>

````

## `core-001:unit:1f646b31d8d5f48a`
MATERIAL-MISS-FULL-ORIGINAL-XML-QUOTA; `src/Core/src/Transports/Hid/Linux/LinuxHidEventSource.cs:46–54`; original rank 29.

````csharp


/// <summary>
/// A udev event received from the Linux HID event source, reduced to the fields the
/// listener needs to construct a <see cref="HidDeviceRescanHint"/>.
/// </summary>
/// <param name="Action">The udev action string ("add", "remove", ...), or null when unavailable.</param>
/// <param name="StableIdentity">The stable udev/sysfs identity (parent HID device syspath), or null.</param>
/// <param name="DevNode">The /dev/hidrawN node path, diagnostic only, or null.</param>

````

## `core-001:unit:dd179ffbb8315a43`
MATERIAL-MISS-FULL-ORIGINAL-XML-QUOTA; `src/Core/src/Transports/Hid/Linux/LinuxHidIOReportConnection.cs:130–133`; original rank 30.

````csharp


    /// <summary>Gets the generic HID type retained by this untyped report facade.</summary>
    public ConnectionType Type { get; } = ConnectionType.Hid;
````

## `core-001:unit:d5ffab3651d9f944`
MATERIAL-MISS-FULL-ORIGINAL-XML-QUOTA; `src/Core/src/Transports/Hid/Linux/LinuxHidReadReactor.cs:492–494`; original rank 31.

````csharp

        /// <summary>Gets the nonfaulting actual snapshot/ready/terminal-callback drain producer, not close evidence.</summary>
        internal TaskCompletionSource Quiesced { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
````

## `core-001:unit:e954bf0af6c49fba`
MATERIAL-MISS-FULL-ORIGINAL-XML-QUOTA; `src/Core/src/Transports/Hid/Linux/LinuxHidIOReportConnection.cs:33–39`; original rank 32.

````csharp


    /// <summary>Synchronously opens hidraw using the production report seam and shared readiness reactor.</summary>
    /// <param name="devNode">The hidraw device-node path.</param>
    public LinuxHidIOReportConnection(string devNode) : this(devNode, LinuxHidReportApi.Instance)
    {
    }
````

## `core-001:unit:fb09fe35ad8505f1`
MATERIAL-MISS-FULL-ORIGINAL-XML-QUOTA; `src/Core/src/Transports/Hid/Linux/LinuxHidReadReactor.cs:439–441`; original rank 33.

````csharp

        /// <summary>Gets the asynchronously continued outcome of run retirement, independent of owner device release.</summary>
        internal TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
````

## `core-001:unit:418fbc28a73c4bd5`
MATERIAL-MISS-FULL-ORIGINAL-XML-QUOTA; `src/Core/src/Transports/Hid/Linux/LinuxHidEventSource.cs:116–186`; original rank 34.

````csharp


    /// <inheritdoc />
    public bool Initialize()
    {
        _udevHandle = UdevNativeMethods.udev_new();
        if (_udevHandle.IsInvalid)
        {
            Logger.LogWarning("Failed to create udev context");
            return false;
        }

        _monitorHandle = UdevNativeMethods.udev_monitor_new_from_netlink(_udevHandle, UdevNativeMethods.UdevMonitorName);
        if (_monitorHandle.IsInvalid)
        {
            Logger.LogWarning("Failed to create udev monitor");
            return false;
        }

        var filterResult = UdevNativeMethods.udev_monitor_filter_add_match_subsystem_devtype(
            _monitorHandle,
            UdevNativeMethods.UdevSubsystemName,
            null);
        if (filterResult < 0)
        {
            Logger.LogWarning("Failed to add udev filter: {Result}", filterResult);
            return false;
        }

        var enableResult = UdevNativeMethods.udev_monitor_enable_receiving(_monitorHandle);
        if (enableResult < 0)
        {
            Logger.LogWarning("Failed to enable udev receiving: {Result}", enableResult);
            return false;
        }

        _shutdownEventHandle = LibcNativeMethods.EventFd(
            0,
            LibcNativeMethods.EFD_CLOEXEC | LibcNativeMethods.EFD_NONBLOCK);
        if (_shutdownEventHandle.IsInvalid)
        {
            Logger.LogWarning(
                "Failed to create Linux HID listener shutdown event fd: {Error}",
                Marshal.GetLastPInvokeError());
            return false;
        }

        var monitorFd = UdevNativeMethods.udev_monitor_get_fd(_monitorHandle);
        if (!IsValidFileDescriptor(monitorFd))
        {
            Logger.LogWarning("Failed to get udev monitor fd");
            return false;
        }

        using var shutdownLease = new LinuxDescriptorLease(_shutdownEventHandle);
        var shutdownFd = shutdownLease.Descriptor;
        if (shutdownFd < 0)
        {
            Logger.LogWarning("Invalid Linux HID listener shutdown fd");
            return false;
        }

        var pollFds = new LibcNativeMethods.PollFd[2];
        pollFds[0].fd = monitorFd;
        pollFds[0].events = (short)(LibcNativeMethods.POLLIN | LibcNativeMethods.POLLERR | LibcNativeMethods.POLLHUP);
        pollFds[1].fd = shutdownFd;
        pollFds[1].events = (short)(LibcNativeMethods.POLLIN | LibcNativeMethods.POLLERR | LibcNativeMethods.POLLHUP);
        _pollFds = pollFds;

        return true;
    }
````

## `core-001:unit:9b2517c252b41c26`
INELIGIBILITY-REPLACEMENT-NOT-MATERIAL-EXPANSION; `src/Core/src/Devices/DisposalGate.cs:26–30`; original rank 25.

````csharp
internal sealed class DisposalGate(
    IDisposable? lease = null,
    bool releaseLeaseOnFailure = true,
    Action<Exception>? onFailure = null)
{
````

## `core-001:unit:21cfb83cdc2b4263`
INELIGIBILITY-REPLACEMENT-NOT-MATERIAL-EXPANSION; `src/Core/src/Devices/DeviceConnectionRegistry.cs:165–166`; original rank 26.

````csharp

        private Exception? _unrecoveredFailure;
````

## `core-001:unit:1dc08e7d03f341ef`
INELIGIBILITY-REPLACEMENT-NOT-MATERIAL-EXPANSION; `src/Core/src/Native/Linux/Libc/LinuxFileSafeHandle.cs:24–25`; original rank 15.

````csharp

    private int _releaseRecorded;
````

## `core-001:unit:4c48c7d8d8d2e37a`
CONDITIONAL-HISTORY-ELIGIBILITY-REPLACEMENT; COORDINATOR-ADJUDICATION-REQUIRED; `src/Core/CLAUDE.md:600–603`; original rank 2.

````markdown
### ConnectionType Semantics

`ConnectionType` is a `[Flags]` enum with explicit values. `HidFido`, `HidOtp`, and `SmartCard` represent concrete discovered device interfaces. `Hid` is a group filter that includes both HID FIDO and HID OTP interfaces when used with discovery/cache filtering APIs. `Unknown` matches no devices.


````

## `core-001:unit:4c955b22a2de68df`
CONDITIONAL-HISTORY-ELIGIBILITY-REPLACEMENT; COORDINATOR-ADJUDICATION-REQUIRED; `docs/research/linux-native-acceptance.md:247–306`; original rank 7.

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

## `core-001:unit:0b2135f2f301e2c1`
CONDITIONAL-HISTORY-ELIGIBILITY-REPLACEMENT; COORDINATOR-ADJUDICATION-REQUIRED; `docs/research/linux-read-reactor.md:99–112`; original rank 8.

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

## `core-001:unit:d8f176c067aa0fd3`
CONDITIONAL-HISTORY-ELIGIBILITY-REPLACEMENT; COORDINATOR-ADJUDICATION-REQUIRED; `docs/research/linux-fido-async-lifetime.md:57–77`; original rank 9.

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

## `core-001:unit:b45b5794aa92008c`
CONDITIONAL-HISTORY-ELIGIBILITY-REPLACEMENT; COORDINATOR-ADJUDICATION-REQUIRED; `docs/research/linux-read-reactor.md:69–98`; original rank 10.

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

## `core-001:unit:4330e60d07cd5417`
CONDITIONAL-HISTORY-ELIGIBILITY-REPLACEMENT; COORDINATOR-ADJUDICATION-REQUIRED; `docs/research/linux-native-acceptance.md:101–202`; original rank 11.

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

## `core-001:unit:457a4879daaf1b55`
CONDITIONAL-HISTORY-ELIGIBILITY-REPLACEMENT; COORDINATOR-ADJUDICATION-REQUIRED; `docs/research/linux-otp-async-lifetime.md:50–69`; original rank 12.

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

