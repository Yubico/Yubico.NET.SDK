# Linux native / AOT / read-only acceptance (L6b)

Recorded 2026-11-11. **Read-only physical acceptance passed in JIT and AOT;
operator-assisted acceptance and budgets remain unexecuted/unaccepted.** No master
ISA/status criteria were edited. No runtime fix, commit, push, driver rebind,
configuration change, service start, credential/PIN operation or provisioning was performed.

## Current PR-preparation checkpoint — `20261111T123927Z`

Fresh registered publish and full read-only capture passed **after the final code
commit**, with **Recorded HEAD `adf3a911582c3664f3c02c13a3eb668706b9471b`**
(runtime commit `f32848f62`, verification commit `adf3a9115`; baseline remains
`3de42bb54c5e8c986364f0f04398d4e85a66a596`). This checkpoint supersedes earlier
physical evidence for PR preparation; it does not promote pre-format execution
to proof of the final code. No shipping or verification source was edited here.

- **SOURCEHASH:** `d5bb1304912aff955f41c759cfa10447c910296698174ad44522dfb896eaa4dc`
  (1,447 source/build/config/asset files).
- **INPUTHASH:** `34fd574f5a74f84307fb7f968df95c94e63045ac0e014f43b25e45f972d27917`.
- **Publish-manifest SHA-256:**
  `3289f44ef58ed8fd259c629d3983f614808aeb4bd270967eb11644ebd774b293`.
- Registered publish run `20261111-133907-085-linux-hid-verification-publish`
  passed with **0 warnings**, stable before/after inputs, **223 artifacts** and
  **17 content-hashed packages**. Binding was checked before capture and before/after
  every child. No full-solution restore/build/test followed this publish.
- Both runtimes independently discovered exactly one key and agreed positive
  **serial 103**, with `HidFido, HidOtp, SmartCard`. USB `3-1.3`, `1050:0407`,
  had readable/writable OTP `/dev/hidraw0` and FIDO `/dev/hidraw5`, both bound to
  `usbhid`. CCID `1.2` had no kernel driver; the actual PC/SC route nevertheless
  passed. No driver, service or system configuration was changed.
- Management reports **5.8.0** across typed OTP, direct feature and PC/SC;
  FIDO session / CTAP GetInfo reports **0.0.1**, five advertised versions.
  These development-fixture reports remain distinct, not normalized.

| Route | JIT named checks | AOT named checks |
|---|---:|---:|
| `--native` | 38 | 38 |
| `--list` | 1 | 1 |
| `--fido` | 12 | 12 |
| `--otp` | 9 | 9 |
| `--raw` | 3 | 3 |
| `--expert-io` | 9 | 9 |
| `--expert-feature` | 9 | 9 |
| `--smartcard` | 1 | 1 |
| **Six physical modes** | **43** | **43** |

Total: **20 child runs**, **16 positive exits 0**, four exact intended negative
exits 1, **164 positive named checks**, four negative cleanup checks, and **zero
expired watchdogs**. All six physical modes ran in both runtimes. Coverage includes
three cycles per route, borrowed FIDO idle reuse, pending typed read disposal/reopen,
direct indefinite read disposal wake/reopen, and PC/SC begin/select/read/end/dispose.
PC/SC's one named check aggregates three cycles and two reopens.

Representative single-run SDK timings (milliseconds, **caller return / completion**):

| Operation | JIT | AOT |
|---|---:|---:|
| First typed FIDO open | 16.5915 / 70.4139 | 0.1233 / 50.9830 |
| First FIDO GetInfo after initialization | 0.0898 / 25.8554 | 0.0074 / 26.0137 |
| Pending typed FIDO read disposal | 0.4682 / 2.9907 | 0.0056 / 2.3049 |
| First OTP feature GET8 | 0.5158 / 2.0768 | 0.0011 / 0.2488 |
| First OTP Management info | 0.0887 / 28.1124 | 0.0053 / 29.5763 |
| Direct I/O disposal wake | 2.8233 / 6.8950 | 0.0161 / 2.5604 |
| First direct I/O INIT/GetInfo | 38.6668 / 38.7686 | 32.5042 / 32.5046 |

These are managed/native/protocol samples, not kernel-only timings or accepted
budgets; direct compatibility calls are synchronous. PC/SC open invocation-only
samples were 10.358/0.321/0.342ms (JIT), 0.154/0.141/0.109ms (AOT), all pending
at return. Direct feature has no per-call timings. No new descriptor-only probe
was run; the supplemental probe below belongs only to its historical capture.

All **20 result contracts** and **44 raw-file hash-index entries** were independently
revalidated; **16/16 gate/provenance regression tests** passed. Final publish
verification and whitespace validation passed. Raw evidence remains local/ignored:
`artifacts/linux-hid-verification/evidence/20261111T123927Z/`.
The derived JSON preserves source, publish, binary, provenance, summary and index
identities without copying raw environment paths or payloads.

The final full-repository gate **preceded** this publish:
`20261111-133305-894-build+test+docs-qa+native-aot-contract-qa+complexity` passed
build, tests, docs QA, native AOT contract QA and complexity: **4,078 executed,
0 failed, 5 skipped, 0 warnings; all 183 changed shipping methods within limits**.
Its result and the complete fresh publish logs were archived under
`artifacts/linux-hid-verification/validation/`, outside toolchain log rotation.
Regression log: `artifacts/linux-hid-verification/validation/pr-prep-gate-regressions-20261111T123927Z.log`.

The earlier `20261111T123621Z` native-only capture used the same SOURCEHASH but a
pre-commit HEAD/input binding; it is historical, not evidence for the recorded HEAD
above. The `20261111T115147Z` physical capture and `20261111T040627Z` blocked capture
remain preserved below. **A later documentation-only commit can change HEAD without
changing any captured source-file bytes.** The strict input binding includes HEAD,
so this checkpoint does not attest that future HEAD: live verification after such a
commit will require registered republish/fresh capture, not relabeling these records.

Only the two evidence documents were refreshed by this checkpoint. No commit/push,
operator Selection/touch/cancel/removal, provisioning, credential action or master
ISA/status/architecture edit was performed. Hosted CI, operator cases, isolated
hardware-native timing, representative telemetry, a pre-change physical baseline
and approved budgets remain outside this acceptance.

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

## Historical blocked checkpoint — `20261111T040627Z`

The remainder preserves the earlier blocked capture and its measurements. References
to “current” or “final” below are scoped to that historical checkpoint; the physical
checkpoint above supersedes its hardware gap, not its full-repository-gate history.

**Final full-repository-gate refresh:** the current evidence is the source-bound
capture at `20261111T040627Z`, after registered republish following the successful
full-repository gate. The source hash is unchanged. The earlier valid
`20261111T034838Z` capture is historical, not the current publish record: the full
solution's RIDless restore changed only `dependencies.resolvedClosure` and correctly
made publish verification fail closed until republish. No code or gate was changed
to ignore that drift. The earlier `20261111T032852Z` capture (and other pre-binding
captures) is **superseded, not current source-bound proof**: its environment hash
was only a fresh working-tree label, and its gate did not enforce runtime identity.
Those historical execution observations must not attest the current publish.

## Source and artifacts

- Baseline/HEAD: `3de42bb54c5e8c986364f0f04398d4e85a66a596`.
- Working-tree **SOURCEHASH**:
  `36b75d0310e6ae896975189f8be0138ba8ae48a52127474029de22d577d278aa`.
  This hashes the sorted file-digest manifest for 1,447 git-visible non-Markdown source/build/config/asset
  inputs, including uncommitted L0–L6a and this host. Documentation and agent
  instructions are excluded to avoid circular result hashes. The definition and
  complete file manifest are emitted by `publish_contract.py` and copied by capture;
  this is not a git tree hash. Uncommon resource/config suffixes are included too.
- Environment: Omarchy 4.5.0, Linux `7.2.15-1-omarchy`, x86_64; SDK `10.0.404`,
  runtime `10.0.15`; glibc `2.44+r50+g1848099f063e-1`; pcsclite `2.5.3-1`.
- SDK package version `2.0.0-alpha.2`; NativeShims pin **and resolved assets**
  `1.18.1`. Existing package targets supply Linux static AOT + PC/SC linking;
  this host adds no linker workaround. AOT `ldd` resolves `libpcsclite.so.1`,
  `libm.so.6`, `libc.so.6` and the loader, with no missing dependency.
- Durable scrubbed counts, measurements and SHA-256 identities are in
  [linux-native-acceptance-results.json](linux-native-acceptance-results.json).
  These identify locally built/cached bytes, not a new package-producer attestation.

Raw evidence is local/ignored, **not linked as if it were checked-in data**:
`artifacts/linux-hid-verification/evidence/20261111T040627Z/` contains eight raw
run JSON files, eight logs, `publish-manifest.json`, `source-manifest.json`, `provenance.json`, `summary.json`
and `sha256.json`. The durable derived record retains their important identities.
No device/secret payload bytes are logged. Native test payloads are uniform synthetic
markers only. The capture script reads no prior logs or credential configuration.

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

## Host and safety boundary

`verification/LinuxHidRouteVerification` is a standalone .NET 10 / C# 14
`PublishAot` console host referencing Core, Ctap2 and Management. It is intentionally
outside the solution, so explicit publish **and** execution are required.
Core grants only this host an additional `InternalsVisibleTo`. The macOS host and
its platform guard remain unchanged. The platform-neutral `SmartCardScenario.cs`
is linked unchanged; no macOS listener-drain source is linked.

`--native` uses:

1. Real libc `open`, errno, `eventfd`, read/write and checked close, including a real
   descriptor-zero case. Only the bounded host child closes its own stdin for that case.
2. The **production** `LinuxHidReadReactor`, with real eventfd/poll syscalls. A counting
   observer delegates every call to `LinuxHidReadinessNative`; it never simulates poll.
3. Real AF_UNIX datagram socketpairs with nonblocking/close-on-exec descriptors.
   `DescriptorReportApi` transfers one socket descriptor on Open and supplies
   **synthetic FIDO64 metadata** instead of hidraw descriptor ioctls. Read and output
   framing delegate to the production `LinuxHidReportApi`/libc. This is a synthetic
   descriptor fixture, **not a HID device or firmware emulator**.
4. The actual `LinuxFidoHidConnection`/native owner. A deliberately held synchronous
   output seam establishes worker/input independence. The hold itself is a managed
   assertion barrier, not a claim that a kernel USB write was blocked.

No application callback is installed on the reactor. The held internal callback is
a test assertion barrier, released and truly drained in `finally` even when the
negative control fails. The watchdog can kill only its own verification child;
expiry exits **3/failure**, never drain proof or a passing timeout.

## Executed native evidence

| Path | JIT | Native AOT |
|---|---:|---:|
| Named libc checks, including negative fd/errno and fd 0 | 6 passed | 6 passed |
| Reactor/readiness/quiescence and same-integer fd reuse checks | 17 passed | 17 passed |
| Linux FIDO owner over synthetic descriptors | 15 passed | 15 passed |
| **Total positive checks** | **38, exit 0** | **38, exit 0** |
| Blocking report-entry sensitivity | intended assertion, exit 1 | intended assertion, exit 1 |
| False-quiescence sensitivity | intended assertion, exit 1 | intended assertion, exit 1 |
| Physical-device enumeration | zero keys, blocked/exit 2 | zero keys, blocked/exit 2 |

Every success rejects zero checks. AOT reports `IsDynamicCodeSupported=false`;
JIT reports `true`. Each native run opens/closes three production FIDO owners over
real socket descriptors and retains/verifies a burst of **40 owned input buffers**
without loss, reordering or overwrite. It also checks pending read cancellation and
same-owner recovery, pending read disposal, and fresh-owner reopen/read/checked close.

The readiness control probe records **4 real poll calls, 3 eventfd writes and 3
eventfd reads** per runtime. Explicit signal does not manufacture device data.
Removal and shutdown wake an indefinite poll; actual token Quiescence precedes
synchronous reference release on a designated owner thread and checked close.
While a callback is held, Quiescence stays pending and early ReleaseReference throws.
After checked close, the kernel actually reuses the same integer fd; stale predecessor
removal/release cannot affect the successor's registration identity/generation.
This does not inject a physically impossible callback after proven quiescence.

The output probe records one accepted/pending output at its barrier, an off-caller
worker, and an incomplete returned Task. Another production owner's input progresses
through the same real reactor before that output is released. The original output
worker performs checked close (JIT thread 20; AOT thread 18). The recorded aggregate
read count is 45 (JIT) / 48 (AOT) and output count 1 before the final reopen probe; these are call
counts, not hardware report counts or a global worker budget.

The blocking sensitivity control executes the **real retained `FidoHidConnection`
compatibility adapter** over a held synchronous report fixture, then uses the same
off-caller/pending-task gate as the positive owner probe. The false-quiescence
control substitutes `Task.CompletedTask` for an actual still-held registration's
acknowledgement; the gate rejects it and cleanup subsequently waits for true drain.
CI requires the exact failure message and exit 1; crashes/watchdogs cannot satisfy it.

## Measurements, not budgets

Single cold diagnostic samples, milliseconds. Held output duration includes the
test barrier/other-input assertions; it is not device latency. JIT versus AOT is
not a pre-change versus post-change comparison or a performance claim.

| Operation | JIT caller return / total | AOT caller return / total |
|---|---:|---:|
| Synthetic owner open | 2.3406 / 3.4911 | 0.0272 / 0.1223 |
| Held output | 0.4373 / 3.5704 | 0.0016 / 0.0410 |
| Pending-read disposal | 0.8662 / 1.3656 | 0.0058 / 0.0149 |
| Reactor shutdown | 1.8407 / 2.1586 | 0.0121 / 0.0369 |

The host-injected report observer separately times the synchronous production L1
Write call **after** releasing the artificial hold: JIT `0.522905ms`, AOT
`0.005327ms`. This isolates the report-call boundary beneath the native owner,
including framing, marshalling and possible cold JIT overhead. It is **not** a
kernel-only syscall/USB duration and is not end-to-end Send timing relabeled native-only.
Device-native read/write/ioctl/open/close durations remain **pending**.

With one live synthetic owner/reactor, idle whole-process CPU was `1.901ms` over
`251.1572ms` (JIT), `0.080ms` over `250.5524ms` (AOT); observed thread delta 0 in
both. This short process-wide sample is not a long idle/device budget.
`GC.GetTotalAllocatedBytes()` observations are approximate process-wide deltas,
include instrumentation/background work, and do not attribute native allocation:
open `16,144` bytes in each runtime, idle `24,600` / `16,400` bytes. A reported zero
delta is not a zero-allocation guarantee; unmeasured held-output allocation is null.

L0's [unchanged baseline](linux-async-boundaries-baseline.md) supports only a
qualitative held-report responsiveness comparison. No pre-change native or hardware
timing baseline exists. No isolated hardware baseline worktree was created because
hardware is still unavailable. Nothing here retroactively supplies that measurement.
**No global latency/allocation/CPU/worker budget has been accepted.**

## Hardware and operator gap

The final read-only sysfs snapshot finds Yubico `1050:0407`, product
`YubiKey OTP+FIDO+CCID`, USB `3-1.3`, with interfaces `1.0`, `1.1`, `1.2` all bound
to **usbfs**. `/dev/hidraw0` and `/dev/hidraw5` are absent again; existing hidraw1–4
are inaccessible to this user and were not selected/opened arbitrarily. This differs
from L0's earlier usbhid snapshot. JIT and AOT SDK discovery both return zero keys.
**No positive serial, firmware, physical transport path, hardware operation, or
hardware reopen is verified (physical path checks = 0).** No system remediation
was attempted.

Built, **not executed**, serial-selected read-only modes:

| Mode | Intended physical evidence, still pending |
|---|---|
| `--fido` | Public typed factory → actual Linux FIDO owner; three CTAP INIT/GetInfo/open/close cycles; two sequential borrowed sessions with idle reuse; pending raw read/dispose/reopen |
| `--otp` | Actual Linux OTP owner; three typed feature GET8 / Management device-info / close/reopen cycles |
| `--raw` | Public selected-device raw CTAP session factory, GetInfo and reopen over three cycles |
| `--expert-io` | Uniquely associated public interface → Linux direct I/O facade; indefinite pending GetReport woken by disposal from another thread; descriptor metadata and INIT/GetInfo/reopen |
| `--expert-feature` | Linux direct feature facade; unchanged 8-byte metadata, GET8 and selected Management info, three cycles |
| `--smartcard` | Unchanged shared scenario: context/connection open, BeginTransaction, select/read Management info, EndTransaction, dispose/reopen, three cycles |

Direct modes refuse an ambiguous association when multiple keys/interfaces exist.
Linux typed routes assert the real Linux owner type; direct modes deliberately use
the retained synchronous compatibility adapter over the actual Linux facade, not
macOS classes or substitute typed-route evidence. No six-second macOS input timeout
or macOS-specific terminal reason is assumed.

`--touch`, `--active-cancel` and `--removal` are also compiled but **unexecuted**.
They refuse to run unless `LINUX_VERIFY_OPERATOR_COORDINATED=1` is deliberately set
after separate coordination. Touch/cancel use read-only CTAP Selection, paired
presence/recovery/GetInfo, never provisioning. Removal requires terminal input
**before disposal**, observed physical absence and a fresh serial-matched generation
before reopen/GetInfo. Merely timing out cannot pass. The capture script never runs
these modes, even if the environment flag is set.

## Commands and validation

```bash
dotnet toolchain.cs linux-hid-verification-publish
python3 verification/LinuxHidRouteVerification/capture.py
python3 verification/LinuxHidRouteVerification/test_evidence_gates.py artifacts/linux-hid-verification/evidence/20261111T040627Z
python3 verification/LinuxHidRouteVerification/publish_contract.py verify
```

The capture command exits **2** for this record: native checks/expected negative
controls passed, hardware blocked. It stores raw JSON/log hashes, file manifest,
native package/archive/binary identities, runtime/kernel and sysfs routes. Only
when both runtimes independently enumerate exactly one key with the same positive
serial does it execute the six read-only physical modes. Otherwise it selects none.
An individual explicit mode can be invoked with `--serial POSITIVE_SERIAL` and
`--output FILE`; do not infer a serial from an unrelated interface or prior log.

The full-repository gate was completed **before** the final standalone republish:

```bash
dotnet toolchain.cs -- build test docs-qa native-aot-contract-qa complexity
```

- Full solution and explicit JIT/AOT publish: **0 warnings, 0 errors**.
- Final full-repository gate: **4,078 executed, 0 failed, 5 skipped, 0 warnings**;
  build, tests, docs QA, all-library AOT contract QA and shipping complexity passed.
  This includes the previously focused managed paths, but is not hardware proof.
- Review regression script: **16 passed**, with real-runtime substitution and
  isolated filesystem stale/missing-binding negatives as described above.
- Standard complexity: all 172 changed shipping methods within thresholds. That
  tool excludes verification hosts; it was **not** counted as host coverage.
  A temporary script using the repository's exact MethodExtractor/complexity visitors
  separately measured **63 host methods**, zero findings. The extractor does not
  model top-level Program CLI statements; those were manually reviewed.
- Previously, scoped `dotnet format` apply and `--verify-no-changes` passed for all nine new C#
  host files. Workspace loading reports pre-existing duplicate PublicAPI additional
  files in the three referenced projects; those are not compiler/publish warnings.
  Formatting never targeted earlier L0–L6a source or the shared macOS scenario.
- Final full gate log:
  `20261111-050336-551-build+test+docs-qa+native-aot-contract-qa+complexity`;
  final bound publish log: `20261111-050610-341-linux-hid-verification-publish`.
  Copies survive toolchain's five-run rotation under the local
  `artifacts/linux-hid-verification/validation/` directory, alongside the separate
  host complexity script/result and `final-gate-regressions-20261111T040627Z.log`. These local
  artifact paths are not repository links.

Initial full-run attempts had two diagnostic failures, preserved as caveats rather
than claimed fixed by the passing rerun. The orchestrator independently proved a
PIV use-after-return test bug on baseline. A Core PC/SC finalizer-watchdog failure
also reproduced alone; the production/probe bytes were unchanged versus baseline,
but its root cause remains unresolved. The orchestrator byte-restored all diagnostic
source edits; **no runtime fix** was retained. The final gate's result JSON reports
the later passing run above; its SHA-256 is recorded in the derived result file.

After that full gate, RIDless Core assets caused expected dependency-closure drift
against the older standalone publish stamp. The registered publish target restored
and republished both modes with stable before/after inputs, without weakening any
binding check. No full-solution build/test was run after this final republish, so
the assets binding remains valid. Only excluded evidence Markdown/JSON was refreshed;
the 16 gate tests and final publish verification were then checked without changing
shipping/verification source or dependency assets. All verification commands are
foreground and completed; no verification child/command is left running.

`.github/workflows/native-aot.yml` keeps the existing three-OS/all-library gate and
adds Linux-only JIT/AOT publish + real-native no-hardware checks and exact negative
controls. This workflow edit is locally verified code, **not a newly observed hosted
CI run**. Hardware, operator actions, device-native measurements, baseline comparison
and approved budgets require follow-up review/evidence; L6b must not be marked globally
accepted on the basis of this record alone.
