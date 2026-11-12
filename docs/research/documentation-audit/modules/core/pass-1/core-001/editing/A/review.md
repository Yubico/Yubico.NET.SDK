# Packet A independent content and citation review

## Decision: REQUIRED CORRECTIONS

The reviewed batch is **not yet accepted as a whole**. Two bounded documentation corrections remain
below. Neither requires an executable change, a new experiment, or a change to the accepted grouping
policy. No executable change was found in the reviewed diff. Compiler/token/API/example qualification
is still a coordinator gate, not a result of this review.

Subject: checkout `/home/rupicola/Code/y/yubikit-core-docs-edit-discovery`, HEAD/base
`da66f5d01ba594993974ddd26723e8c300c0a454`, original source
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Exact product patch reviewed:
`28cfedee32cba340d041a90edadd79afff3d65ced04317a8863d43854890a517`.
This hash identifies the review subject; it is **not unconditional batch acceptance**.

## Required corrections

### R1 — Remove the remaining convergence guarantees

Candidates: **CORE-A-C010 / FPT-010**, **CORE-A-C011 / FPT-011**.

- `src/Core/src/Devices/CompositeDeviceMerger.cs:224–235`, especially **230–232**:
  the revised explanation still says “at worst with one scan of stale membership.”
  `ResolveContestedSerials` at 252–279 computes its answer from this scan's descriptors; it has no
  age, generation, expiry, or next-scan mechanism that imposes a one-scan bound. Repeating the same
  stale-complete/fresh-incomplete evidence can repeat the same winner. The adjacent new qualification
  about later distinguishing evidence does not make the one-scan assertion true.
- `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/CompositeDeviceMergerVectorTests.cs:256–265`
  and **281–289** still promise healing on the first subsequent scan with complete visibility,
  “serial evidence, or topology evidence.” Qualify this as evidence that actually changes the grouping
  decision, rather than promising that serial evidence alone necessarily heals it. In
  `CompositeDeviceMerger.Merge:108–114`, a PID-count-one complete group takes the PID shortcut unless
  a serial is contested **across PIDs**. `CanMergeByPidWithoutSerial:192–198` checks connection bits,
  not disagreement between serials in that same-PID group. The tests supply only one scan, and their
  new explicit “does not test convergence” sentences correctly describe that narrower oracle.

Required result: keep the useful heuristic rationale and accepted partial-evidence bound, but say that
stale membership can persist while the evidence persists and that later regrouping depends on evidence
the implemented tiers use to distinguish the members. Remove the one-scan/automatic-first-serial-scan
guarantees. Preserve method names, assertions, all runtime failure strings, and grouping code. This is
source reasoning, not a newly executed counterexample or a newly allocated implementation finding.

### R2 — The retained topology-test comment is not an accurate retain

Candidate: **CORE-A-C009 / FPT-009**.

`src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/WindowsDeviceTopologyResolverTests.cs:214–219`,
inside `NullResolver_AlwaysReportsUnknown_PinnedPlatformBound`, says:

> macOS/Linux platform bound: no reader/interface → USB device mapping exists, so topology
> evidence is never available and tiers 2..5 always decide.

The test calls `NullDeviceTopologyResolver.Instance` twice and checks false/null. That establishes
what this SDK's selected resolver does, not that those operating systems have no mapping APIs.
It contradicts the corrected `IDeviceTopologyResolver.cs:26–30,49–52` and the explicit investigation
qualification at `investigation-A.md:186–190`.

Required result: narrow these two comment lines to the current SDK no-topology resolver. Keep the
test name, native seams, inputs, assertions and platform selector unchanged. The editor return's
“accurate existing commentary” rationale for this retained path (`edit-A.md:91` and its path/candidate
disposition) must not be carried forward as accepted. A successor batch must truthfully account for
this path's documentation correction and its new hashes. Do not rewrite the immutable originals.

## Accepted documentation and limits

Apart from R1's two files, the documentation in the other **34 edited C# paths** is accepted at the
exact current hashes in `changed-path-sha256.json`. Of the **13 candidate-associated retained C#
paths**, the other **12** have a supported retain disposition; R2 is the exception. This is not a
fresh content audit of the other 41 non-candidate retained inventory paths. All 90 received the
identity/preservation checks described below.

- **C001–C006, C015–C016:** qualifier range is inclusive `0..uint.MaxValue` with the public `long`
  carrier preserved. Metadata defaults, units, effective/default/non-final version precedence, UTF-8
  fallback and component-version behavior match the complete decoder/helpers. `ChallengeResponseTimeout`
  stores the input TLV view; struct copies share it, and equality/hash consume current contents.
  `Tlv.Value` and `Tlv.Dispose` substantiate the lifetime qualification. Snapshot replacement preserves
  earlier scalar fields, not independent or disposal-stable timeout storage. The existing equality
  assertions occur before input-list disposal. Reader-list cleanup was not mechanically introduced.
- **FIPS/form factor/wide capabilities:** `IsFips` is a manufacturing-bit observation, not approval of
  an operation. Raw 0x81 remains distinct from base 0x01. The field XML does not settle tags 0x14/0x15
  mapping/width or promise certification. `ReadCapabilities` still calls ordinary `FromApp`; the unused
  `FromFips` vectors do not establish integration. Whole-integer wide decoding is not claimed: the
  existing wide all-zero vectors cannot establish nonzero-tail or signed-high-bit correctness. These
  remain the original implementation/representation/source-authority handoffs, not intended guarantees.
- **C007–C008, C017, C021:** monitor-retained references, changed-metadata updates, serial latching,
  contradictory-known-serial replacement and no-I/O cache access agree with `YubiKeyDevice`,
  `SnapshotDiff` and their complete relevant tests. Identity-cache PID validation is distinguished
  from the metadata cache's member-ID-only reuse. Successful serial-less metadata is cached, unlike
  failed/timed-out reads. The existing two-OTP PID-change test forces identity reads and is correctly
  not credited as a metadata-PID or successful-serial-less expiry test.
- **Monitor ownership and timing:** manager watch owns a private producer; monitor watch owns only
  its subscription. Creation awaits first publication, including empty success. First-move replay and
  registration share the feed gate; replay is outside channel capacity, and delivered devices remain
  mutable references. Full-buffer failure is subscriber-local, buffered history can drain, and healthy
  completion does not erase an earlier subscription error. Caller cancellation ends waits, not native
  work or ownership. Retirement/worker/native-owner transfer and teardown remain distinct. The budget
  sum is arithmetic, not a scan-wide deadline. No arbitrary-logger-failure cleanup guarantee is accepted.
- **C009–C012, C020:** the other topology text correctly distinguishes synchronous unbudgeted Windows
  calls, scripted resolver logic, OS PnP evidence and physical/native qualification. Count, shape,
  one-order conservation, replay versus insertion, direct-device authorization and sync-only fake
  disposal-counter descriptions match their full test bodies/helpers. The attended pump checks concern
  the successful path; failure-path drain remains held. Removed Rust comparisons are not silently
  verified with Python, and historical hardware anecdotes are not promoted to current watcher evidence.
- **C013–C014, C018–C019 and shared facets:** out-of-write-scope holds are truthful; this review does
  not accept a new authorization gate, repeated-switch runner semantics, reset policy, applet feature
  gate or shared-guide publication. Existing run-qualified inherited aliases remain nonadditive.

The accepted text uses the approved direct, calm engineering voice. It retains useful local conditions,
ownership rationale and resource scopes. No optional style rewrite is requested.

## Independent original-citation checks

Original bytes were read, not replaced by search summaries or current web pages. Capture hashes and
byte counts were independently recomputed against `investigation/A/sources/captures.json` (all five
matched). Exact identities are repeated in `review-identities.json`.

| Original/status | Independently checked section; actor/strength/applicability |
| --- | --- |
| YubiKey 5 Series Configuration Reference Guide; unversioned manufacturer reference; retained 2026-11-11 | Concepts → Form Factor, Auto-Eject Timeout, Challenge-Response Timeout, Device Flags; Reading Information → Device Information; timeout write-width passages. Descriptive device/configuration reference: seconds, 15-second documented default, inactivity conditional on MODE_FLAG_EJECT, manufacturing byte/table and ordinary field encoding. Advertised YubiKey 5+/Security Key scope and separately described YubiKey 4 capabilities compatibility; not an RFC MUST/SHOULD profile, modern FIPS-mask definition or future-width rule. |
| YubiKey Technical Manual → YubiKey 5 FIPS Series Features; unversioned manufacturer manual; retained 2026-11-11 | FIPS Form Factors/table: manufacturing-set one-byte values 0x81..0x85 since firmware 5.4. Corroborates flagged form-factor reporting only; not FIPS response-mask layout, operation approval or SDK certification. |
| Microsoft DEVPKEY_Device_ContainerId; content last updated 2023-02-28 | Remarks and Requirements: Windows 7+ PnP containers, matching GUIDs identify devnodes in the same container. OS/property-caller API contract, not universal hardware attestation or measured SDK ABI/matching success. |
| Microsoft SCardGetReaderDeviceInstanceIdW; content last updated 2024-11-20 | Parameters, Return value, Remarks, Requirements: established non-null context, null-output length probe succeeds, character count includes terminator, Remote Desktop SCARD_E_READER_UNAVAILABLE, Windows 8 desktop / Server 2012 desktop minimum. WinSCard caller contract, with no SDK completion-time bound. HTTP Last-Modified timestamps are separately recorded in the capture; they are not these content revision dates. |
| dotnet/runtime tag v10.0.0, BoundedChannel.cs | Complete TryComplete 305–361 and TryWrite 363–442: completed writes reject; full Wait-mode TryWrite returns false immediately; pending buffered items delay completion. Runtime implementation evidence plus Core feed code supports the subscriber-local policy, not a device standard or fresh executed concurrency result. |

No new library/API question required another Context7 query: consequential statements were checked
against these retained exact originals and local implementation. Unversioned orientation is not promoted
to pinned-runtime authority. B1's NIST original-PDF gap and C's draft/final/provider qualifications remain
outside this batch; none is closed by the A citations.

## Examples and static verification

All three current example bodies were read and their extraction boundaries/text/hashes recomputed:

- `ConnectionTypeExtensions.cs:32–34`:
  `9ff8535b320dc909b8670d626b05c18dc8e362ec80c1c1c5237b6a631c46811e`.
- `ConnectionTypeExtensions.cs:67–69`:
  `a6daf07849fb4729187f4c98a5d3b471a34ed7216a69276cd8f0350ec448fb20`.
- `YubiKeyManager.cs:17–36`:
  `d1620db72041e4a0b935b9fe662bca97261788f471db337171739bd7c66870bf`.

The first two are correctly classified as illustrative **internal** expressions. The third uses actual
manager/monitor/change-record names, explicit imports and a typed cancellation input; `await using`
encloses the whole enumeration and exception/break cleanup. It prints records rather than pretending
to maintain a projection. None acquires an undisposed connection or crosses TLV disposal with borrowed
metadata. All remain **VALID-NOT-CHECKED / NOT-RUN** for compilation/execution.

Fresh read-only checks independently confirmed:

1. All 90 current/base/source-pin hashes and the exact filtered non-comment lines match the manifests;
   source-pin and worktree-base product bytes agree. The 36 edits contain full-line comments/XML plus
   the recorded final-newline normalization. Actual hunks and surrounding code were inspected; no
   executable import/directive/token/signature/default/assertion/runtime-help/API snapshot change was found.
2. The actual `git diff --binary` with explicit `a/` and `b/` prefixes equals `product.patch` byte for byte.
   Local default mnemonic prefixes differ, so the explicit prefixes are necessary for the byte comparison.
3. All 21 candidate rows retain their FPT mapping and 151 unique cited original units. Each original
   full-file identity and exact character-range SHA-256 was checked using `git show fe342fb0:<path>`.
   Those are original-source ranges, not mislabeled current edited ranges.
4. Every input identity and every product/new-editor-record identity listed in the editor manifests
   matched, including the separately hashed output-identities file. All three body hashes matched.
5. 186 edited XML blocks parsed as well-formed XML; scoped `git diff --check` exited 0.

These are static/source checks, **not full compiler proof**. The coordinator's existing bound checker,
cref/API binding, compiled examples and final validation remain pending. No SDK build/test, formatter,
native/device/crypto execution, Git index/ref/commit/push operation, or subagent ran in this review.

## Handoff and stopping boundary

Return R1/R2 to the editor/coordinator as a bounded successor revision with new product/disposition
identities and focused re-review. Preserve the reviewed originals and this review subject's identities.
No new infrastructure or broad reinvestigation is needed.

FPX-01/A-E01 and A-E02 through A-E06 remain **unexecuted**. Do not infer post-disposal timeout/hash
stability, independent ownership, logger resilience, corrected FIPS/wide decoding, metadata PID expiry,
runner selection semantics or successful-serial-less expiry from this review. Native release/producer,
physical/profile, cross-process, historical failure and future Windows-delta qualifications remain with
their named owners. Shared narrative reconciliation stays in the existing handoff, not this worktree.

After bounded corrections and coordinator validation, finish Core Pass 1 and its agreed boundary
handoff, then stop. This review starts no Pass-2 plan or implementation.
