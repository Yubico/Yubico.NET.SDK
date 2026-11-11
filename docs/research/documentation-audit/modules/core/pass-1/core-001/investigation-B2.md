# B2 Linux lifetime investigation

**Outcome:** complete reading/disposition of all **63 primary files / 11,868 lines**, **1,148 primary
semantic units** and **71 assigned shared units**. The authoritative packet JSON supplies these counts.
Six local candidate groups are ready for coordinator triage; accepted finding IDs remain unassigned.
This return preserves shipping code, public APIs, native declarations, test assertions, console output,
central inventory/coverage, original registries and historical archives. No SDK build, test, formatter,
native probe, device operation, index/ref write, commit, push or subagent ran in B2.

Source baseline: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Observed record-only HEAD:
`01a114d4e251e0f52b92f73393fbe1dca5d5c24e`. All assigned source and range identities match the inventory;
every selected path matches the pin with explicit LF normalization. This records preservation after the
manual semantic reads; hashes alone supplied no clean disposition. Source retrieval began November 11,
2026 UTC, and final record assembly continued November 12.

## Records

| Record | Purpose |
| --- | --- |
| [Unit dispositions](investigation/B2/unit-dispositions.json) | Every original primary/shared ID, complete range, raw/range SHA-256, read status, reason, candidate and later review/validation state |
| [File identities](investigation/B2/file-identities.json) | Raw and pinned Git identities, LF equality and exact complete/shared read scope |
| [Coverage summary](investigation/B2/coverage-summary.json) | 1,219 units: 1,187 read-clean, 31 read-candidate, one read-exclusion, zero unread |
| [Complete file evidence](investigation/B2/file-evidence.md) | Distinct implementation/assertion/lifetime context for all 63 files and all shared documents |
| [Local candidates](investigation/B2/candidates.md) | CORE-B2-C001–C006, separate documentation/runtime/decision/qualification facets and unchanged-path reopening maps |
| [Candidate index](investigation/B2/candidate-index.json) | Exact affected unit IDs and candidate-associated unchanged paths awaiting explicit future editor/reviewer disposition |
| [Ownership and qualification](investigation/B2/ownership-and-qualification.md) | Native state/resource/task owners, discovery/A/B1 handoffs, inherited IDs and exact evidence chronology |
| [Validation proposal](investigation/B2/verification-proposal.md) | Linux release/native/physical/packaging matrix, unexecuted bounded experiments, complete example and B3/B4/B5/PIV handoff |
| [Original sources](investigation/B2/sources/register.md) | Exact Linux/glibc/.NET/systemd/Python/package passages, actor, strength and applicability |
| [Fetch manifest](investigation/B2/sources/manifest.json) | Retained originals' exact URLs, UTC times, byte counts and hashes |
| [Preservation verification](investigation/B2/preservation-verification.json) | Exact ID sets, inventory source/range identities, pinned-source equality and zero new SDK executions |

READ-CLEAN is a source/documentation disposition in the stated implementation/assertion context. It is
neither implementation acceptance nor fresh compiler/platform execution. Independent review, meaningful
consumer compilation and selected current-source/native/device lanes remain pending. The historical
exclusion is `core-001:unit:a9c5253178fb280d`, complete commented-out `LinuxUdevScan.cs`, preserved as
inactive history; active enumeration uses LinuxHidInterface.GetList. No assigned platform unit was
excluded to a later stage. macOS/Windows dependency source units retain B3/B4 ownership explicitly.

B2 read root AGENTS/CLAUDE, full Core CLAUDE/README, nested Core test instructions, testing guidance,
campaign workflow/style/SDK house style, plan/inventory/dispatch/source ownership, B1 report/ownership/
candidates/file evidence and relevant inherited routing/registry entries. There are no additional nested
AGENTS/CLAUDE files under the selected verification host. A's completed report and exact C003/C017/C021
finding definitions were consumed with narrow current source reads; their units retain A ownership.
No other worker's unfinished checkout or Windows branch was read.

## Current Linux contract

The built-in typed factories route directly to separate Linux FIDO and OTP owners. Each owner roots
itself before native acquisition, accepts the exact transferred physical/discovery token and uses one
context-suppressed dedicated worker. There is one ordinary operation slot and no ordinary backlog,
per-operation Task.Run, replacement worker or native-close retry. Synchronous direct report facades
share the respective owner **type**, while each instance owns its own native lifetime. Their public
construction/Get/Set still block, and direct opens carry no grouped registry claim.

FIDO's shared reactor retains a registration-long SafeHandle reference and routes by token identity plus
generation. Its poller performs bounded nonblocking reads only. Activation, removal/wake outcome, actual
quiescence, owner-worker reference release and checked descriptor close are separate observations.
Shutdown closes admission and terminalizes the actual pending reader, while accepted output finishes on
the original worker. That worker waits actual quiescence, releases the retained reference and checks close
before disposing the original claim. Private eventfd retirement is serialized with pending admissions;
it closes a local notification resource after poll exit, rather than a live hidraw descriptor.

OTP uses no read reactor. Its worker owns feature GET/SET and close. Entry/pre-dispatch cancellation
prevents I/O; after native dispatch the actual task/operation slot stay pending until native return.
FIDO pending-receive cancellation instead detaches that one reader, leaving native input active. A late
report can reach its successor. A canceled operation or outer discovery WaitAsync supplies no release
proof. A permanently broken wake or hung open/write/ioctl/close can leave cleanup pending indefinitely.
Finite attempt counts and harness watchdogs supply no blanket elapsed-time guarantee.

libc descriptors are int, including valid fd zero; the default pointer-width SafeHandle marshaller is
avoided for int-returning open/eventfd. Temporary native calls retain SafeHandle references. CloseOnce
observes the cached actual release callback result: pending borrowers or an unexecuted callback give no
proof. Native zero and Linux -1/EINTR/EIO permit claim release, with diagnostics reported separately.
EBADF/unexpected/unknown release or failed reference/claim disposal retain and quarantine the original
root/token. This policy is source-grounded Linux behavior, not a portable POSIX/macOS/Windows rule or a
physical induced-EINTR/EIO result.

Built-in sends own and clear copies before actual public-task completion; native framing is scoped and
cleared in finally. Successful exact input arrays transfer to the caller, including through read-only
Memory views, and connection disposal leaves them intact. Clearing a separate copy leaves the original
unchanged. Descriptor/status/firmware data and secret-derived outputs need their own semantic cleanup
classification. Raw CTAPHID/OTP wire sequencing, full operation recovery and terminal presence outcome
belong to B5 and the later applet boundary owners. B2 supplies transport lifetime evidence, not an applet
credential or protocol-conformance guarantee.

## Candidate priorities

| Local ID | Candidate and next action |
| --- | --- |
| CORE-B2-C001 | Linux setup leads with world-writable HID and USB rules; propose scoped permissions with explicit desktop versus service prerequisites and distribution-rule verification. Documentation/security guidance, no system edit. |
| CORE-B2-C002 | Active native comments still describe a future reactor; L5 ownership attribution predates SharedDeviceInfoRead split. Preserve stage records/history and add current actor/successor context; maintain normal spacing. |
| CORE-B2-C003 | Udev comments claim ASCII strings and a void C monitor-unref return. Original API returns pointer/NULL; current managed return is ignored. Correct rationale while preserving executable declarations. |
| CORE-B2-C004 | Unversioned libudev.so import is absent from Ubuntu noble runtime-only libudev1 file list; libudev-dev supplies it. Qualify deployment prerequisites; stable-soname/loading policy is later implementation work. |
| CORE-B2-C005 | Some FIDO/reactor/routing tests drain only successful-path ownership. A failed assertion can skip disposal of a late opened result or actual late-work drain. Preserve existing oracles and propose explicit test-scope cleanup work. |
| CORE-B2-C006 | Direct Linux factory XML should name caller ownership/synchronous waiting/no grouped claim; usage parser comments should identify first-pair extraction and downstream classification. Detailed facade lifetime XML is already useful. |

The candidate record names current source lines, actual complete test oracles, original authority,
affected public XML/guides/internal/native comments/examples/architecture/migration and precise reopening
triggers. Candidate-associated unchanged paths require explicit later editor/reviewer disposition. The
future SHARED editor alone integrates shared-document changes. No runtime fix or test-assertion change
is bundled with this investigation.

## Dependencies and inherited identities

- **A:** SharedDeviceInfoRead owns discovery scheduling/connection/lease transfer. Native release does
  not establish independent ownership of parsed metadata. CORE-A-C003's TLV-backed timeout/hash concern,
  C017's metadata-cache PID qualification and C021's successful serial-less cache policy remain A
  candidates, with exact original unit IDs in the ownership handoff. HID ProductId means enumerated USB
  product ID in the finder; it is unrelated to a process ID. No duplicate accepted B2 bug is allocated.
- **B1:** reuse portable known-member/process bounds, shared disposal outcomes, borrowed-session and
  actual-terminal-task vocabulary. C002 enumeration cleanup, C003/C004 listener cadence/partial lifetime,
  C009 post-open slot logging and C005 Windows/custom release policy remain their original candidates.
  They can affect mixed discovery but grant no early-release permission for a Linux owner.
- **B3/B4:** retain their native callbacks/checked release/access/ABI responsibilities. C004 adds a
  Linux loader/package deployment dependency for B4. Linux evidence gives no macOS/Windows acceptance.
- **B5/C/B6:** complete wire operations, applet terminal presence, secret services/TLV owners and pinned
  Python history retain their owners. No duplicate Python repository clone or package import occurred.
- **`fixes-wave-001:CORE-001`:** old Linux hidraw7 serial 9681620/key 125 failure remains unexplained.
  Modern fake routing, native-only descriptors and serial 103 physical success differ from the original
  two-key scenario. Preserve original logs, changed architecture and unresolved physical cause separately.
- **`audit-v2-20261022-20261031:PCSC-stall-finalizer-flake`:** current Linux worker/finalizer source and
  fake tests do not diagnose the earlier PC/SC stall/watchdog. Preserve this run-qualified identity and
  B1/B3 source handoffs; no broad historical closure is asserted.

## Exact execution envelopes

The latest upstream Linux native-acceptance checkpoint is `20261111T123927Z`, recorded HEAD
`adf3a911582c3664f3c02c13a3eb668706b9471b`, source hash
`d5bb1304912aff955f41c759cfa10447c910296698174ad44522dfb896eaa4dc`. It records passing read-only JIT/AOT
routes on serial 103/1050:0407, Management 5.8.0 and FIDO/CTAP 0.0.1 kept distinct. Each runtime has 38 native
checks plus 43 physical checks over six physical modes; the four intended controls are negative outcomes,
not extra physical success. Its original blocked sections/JSON fields are historical, explicitly superseded
within that document. Operator touch/active-cancel/removal, representative telemetry and budgets remain
pending. Selected GetInfo/Management/status/reopen paths do not establish full FIDO ceremonies or PIV.

The later reviewed integration's **native-only** source hash
`90b0e9ab7582316099d37d9bc8e93beca1dd64fe8ce9c44e7edb32042b7533aa` records 38 checks/runtime, four intended
controls and 31 gate tests, with zero physical operations. Its bound/restored compiler inputs and
ArchiveSourceRef qualification belong to the frozen integration record. B2 reads the verifier product
contracts/oracles without revalidating that framework or claiming new execution. Current compiler work
is coordinator-owned and was left undisturbed.

Current native implementation source is managed C#; this checkout's product C/C++ lane is empty.
Downloaded original kernel/libc source lives only in B2's source-evidence directory. It supplies cited
mechanics, not new SDK product/native-shim implementation or a deployed binary-source attestation.

## Clean sampling nominees

These are complete, substantive contexts for later independent sampling. The coordinator retains the
campaign's stratified 10%/minimum-three requirement and removes any later edited overlaps from the clean
denominator; the nominations alone do not satisfy that sampling gate.

| Exact original unit ID | Nominee / reason |
| --- | --- |
| `core-001:unit:e24e8d1f599d535f` | LinuxFileSafeHandle.CloseOnce: cached callback publication versus managed Dispose return; inspect ReleaseHandle alongside it. |
| `core-001:unit:1eaa6e2f9b74bb86` | FIDO owner Close: true quiescence, original-worker reference release, diagnostic close versus quarantine/claim handling. |
| `core-001:unit:5fa328452e6ee35d` | Reactor Release: exactly-once state, non-reactor/post-drain guard, synchronous last-reference lifetime. |
| `core-001:unit:ac52eb68cc0075a2` | Report SendFramedReport: caller-length framing, sized ioctl, stack/pool finally zeroing and no retry. |
| `core-001:unit:3b526c935ee07e8b` | GetReportBytes: widened multiply/rounding and meaningful 4096 bound; inspect complete parser limits. |
| `core-001:unit:f33ae15adbb39ca7` | LastBorrowerRelease errno test: actual injected deferred-close callback and independent operation error preservation. |
| `core-001:unit:64c366f588428113` | FeatureDisposeWhileHeld helper: synchronous witness/negative control and complete two-caller finally drain. |
| `core-001:unit:78df928471d3832e` | Discovery AssertHeldQuiescence: PID lease through failed wake, actual drain and held close, independent cleanup. |
| `core-001:unit:fcfe703561725672` | OTP partial GET clearing: fake captures actual production staging, not its own second copy. |
| `core-001:unit:9b046c9e9ac12924` | OTP cancellation/reuse: held GET, dummy abort/reset order, correlated CRC and enclosing scopes; wire primary B5. |
| `core-001:unit:06501a0884e6bec9` | NATIVE-AOT Linux section: exact 38 native result gate, real descriptor/synthetic metadata versus selected physical lanes. |

The proposed matrix and unexecuted experiments provide the next validation scope. B2 requests no new
execution, hardware privilege or phase transition. Documentation triage/edit/review, implementation
selection, compiler verification, native resources, selected-key qualification and broader platform
acceptance retain their separate statuses.
