# Independent B1–B4 content and citation review

## Decision

**BOUNDED MANDATORY CORRECTIONS — three documentation-only corrections.** The reviewed
patch is not yet a content PASS. No runtime, assertion, declaration, import, default, attribute,
directive, output-string or API change is requested. Do not reopen the deferred implementation
campaign to resolve these comments.

Reviewed checkout: `/home/rupicola/Code/y/yubikit-core-docs-edit-transports`.
Base/observed HEAD: `da66f5d01ba594993974ddd26723e8c300c0a454`.
Product source pin: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.
Reviewed patch SHA-256: `86b0c6ed78d5d1c3cf2cd68ec4b529ad8903e9b0ad3258dfa8b2bd7cb1656ef6`.
These are accepted **input identities**, not compiler, runtime or whole-campaign acceptance.

## Mandatory corrections

### M1 — Remove the already-open prerequisite from both integer-property parameters

- **Path/current lines:** `src/Core/src/Transports/Hid/MacOS/IOKitHelpers.cs:29–31,59–61`.
- **Candidate/proposal:** `CORE-B3-C003` / `FPT-042`.
- **Original units:** `core-001:unit:cef6b843926775c3` (original 25–54) and
  `core-001:unit:549db4976f21f028` (original 54–105).

Both `device` parameters still say “The previously opened device”. The candidate explicitly
included the pre-open-query distinction, but the edit corrects only the exception and return
description. `MacOSHidInterface.GetList:65–85` obtains manager/device-set references and calls
`GetNullableIntPropertyValue` without opening each device. The helper itself queries
`IOHIDDeviceGetProperty` at 80–85; neither helper opens a device.

Describe a valid `IOHIDDeviceRef` whose lifetime the caller retains during the query, without
requiring a report connection to have been opened. Keep the existing absence, mismatched-type
and conversion-result distinctions. This is completion of an already released documentation
facet, not a request to repair the separately held null-set or CFNumber ABI findings.

**Source anchors:** `investigation/B3/candidates.md`, C003; retained Apple IOKitUser
`323ead896d04424f87184d8f6ff0cce811aab106`, `IOHIDDevice.h:87–102` distinguishes opening before
state-changing commands. The decisive current-use evidence is the complete enumeration/helper
control flow above. No native pre-open probe was run.

### M2 — Name the metadata probe's actual access fallback

- **Path/current lines:** `src/Core/src/Native/Windows/HidD/WindowsHidReportAccess.cs:86–90`.
- **Candidate/proposal:** `CORE-B4-C004` / `FPT-051`.
- **Original units:** `core-001:unit:97f3c89d0819e76e` (original 67–109), with implementation
  dependency `core-001:unit:3ac34ce64d139f54` (original 240–272).

The new explanation says the constructor's metadata probe “checks open with zero access and
capability retrieval”. That omits a consequential current branch: `OpenHandleForMetadata:232–262`
first tries zero desired access, but an `UnauthorizedAccessException` from either open or
capability retrieval enters the read/write retry. `RequiresReadWriteMetadataHandle:275–276`
selects that exception; `OpenReadWriteHandle:271–273` requests both generic access bits.
Constructor success therefore does not establish that the zero-access probe succeeded.

Say that metadata retrieval first tries zero access and retries read/write on that exception.
Retain the useful conclusion that neither route exercises feature GET/SET. Keep
`OpenFeatureConnection`'s separate zero-access policy, environmental conditions and historical
qualification; change no access flags or executable guidance.

**Source anchors:** the local control flow above; `investigation/B4/candidates.md`, C004;
retained Microsoft `createfile.html`, `dwDesiredAccess`/`dwShareMode`, and
`hidd-getfeature.html`, Parameters/Remarks. These are native API contracts, not an observation
that either access route works on a particular deployment. No additional external lookup is
needed to describe the existing retry.

### M3 — Finish the listener release qualification in the remaining local prose

- **Paths/current lines:** `src/Core/src/Transports/Hid/HidDeviceListener.cs:69–71,127–129`;
  `src/Core/src/Transports/Hid/Windows/WindowsHidDeviceListener.cs:162–164`.
- **Candidate/proposal:** `CORE-B4-C002` / `FPT-049`; macOS retention evidence also remains
  `CORE-B3-C005` / `FPT-044`, not a new finding.
- **Original units:** `core-001:unit:6f60e2b189737226` (Stop, original 67–84),
  `core-001:unit:c40476271e99068b` (Dispose(bool), original 124–144), and
  `core-001:unit:8aa16b126b5c3c3b` (Windows callback, original 149–178).

The Stop summary still says it releases monitoring resources, and Dispose(bool)'s summary
still says it releases **all** resources. Those unqualified summaries conflict with the new
shutdown remarks and the already qualified `IDeviceChangeListener.Stop`. On macOS,
`StartGeneration:85–98` can retain an ambiguously registered generation without a started
thread; `Stop:107–113` then returns without cleanup. On Windows, Stop and finalizer disposal
discard `CM_Unregister_Notification` status before clearing callback state
(`WindowsHidDeviceListener:138–147,294–306`). Its callback's remaining “the finalizer will
unregister” sentence repeats the success assurance removed at 77–79.

Make the summaries describe shutdown/cleanup requests or attempts, with the existing
platform-outcome qualification nearby. Change the callback sentence to an unregister attempt,
not guaranteed successful unregister. Preserve the retained-state safety rationale and all
runtime paths. This is not a request to fix failed unregister, restart, root retention or
callback exception containment.

**Source anchors:** `investigation/B4/candidates.md`, C002;
`investigation/B3/candidates.md`, C005; retained Microsoft `cm-unregister.html`, Return value
and Remarks: CR_SUCCESS versus CR errors, pending-callback wait, and callback-thread deadlock
restriction. Actual successful unregister is distinct from merely invoking the API.

For all three corrections, the editor should regenerate its patch and affected identity/example
records as applicable. Preserve these review inputs; a focused successor review must bind the
replacement hash rather than silently treating this hash as approved.

## Accepted content and authority bounds

The other reviewed changes need no optional style rewrite. The prose generally names the actor,
task, local condition and resource rather than substituting a broad safety slogan. In particular:

- Portable registry wording correctly distinguishes sequential sorted member acquisition, an earlier
  discovery wait, rollback and overlapping known IDs. Shared disposal outcome is not release proof;
  SmartCard failure retention differs from Windows/custom HID compatibility release-after-attempt.
  The PC/SC slot comment accurately leaves post-open logging cleanup as an implementation finding.
- Transaction commentary matches the actual held-transmit and end-failure assertions. The test does
  not hold native EndTransaction itself. Scope diagnostics, later connection-disposal reporting,
  native cleanup and physical transaction outcome remain separate. The PC/SC integration disposal
  test actually awaits its task; it is correctly not grouped with HID's race-only oracle.
- Finder cancellation describes the actual worker boundary, not a native interrupt. UNAWARE wording
  says maximum wait rather than quiet-loop delay. Neither allocation-before-try listener cleanup nor
  enumeration's early context exits are presented as fixed. The finalizer test commentary correctly
  limits its oracle to the calling thread, not finalizer entry or native release.
- Linux's current registration-long SafeHandle reference is supported by reactor construction,
  quiescence and explicit owner-worker ReleaseReference. Checked close and claim release stay
  separate; Linux EINTR/EIO classification is not generalized to POSIX, macOS or Windows.
  Private eventfd lifetime and bounded wake attempts supply no universal elapsed-time bound.
  The libudev parent is borrowed, monitor unref returns NULL, and native-text copying preserves the
  owner-lifetime requirement. Ubuntu noble amd64 package lists support only the named unversioned
  `.so` prerequisite, not a deployed-load result or a universal distribution claim.
- macOS callback context remains rooted across registration/drain, with active native-owned input
  storage distinguished from the dormant array seam. Normal late drain is not the no-worker startup
  path. Fixed 64-byte FIDO normalization, optional leading zero report ID, six-second reader detachment,
  expert eight-byte feature storage and typed short-result rejection match their owners/facades.
  IOReturn enum gaps and CFNumberType's LP64 parameter-width discrepancy remain implementation
  findings. No constants, signatures, corrected runtime values or intended ABI guarantees were added.
- Windows documentation preserves synchronous normal-handle/null-OVERLAPPED execution and temporary
  SafeHandle borrows. Managed Dispose/registry release is not actual native-borrow drain. Separate
  report-ID staging is not erased by clearing an adapter copy. BOOLEAN/BOOL, PULONG, LPGUID and signed
  caps carriers remain declaration findings, not measured cross-architecture corruption. Dormant
  LoadLibraryEx support is distinguished from active NativeShims resolution.
- PCI commentary is appropriately conditional: the managed carrier is eight bytes; the published
  1.18.1 pointer-forwarding path plus selected Linux LP64 two-unsigned-long headers gives a concrete
  conditional discrepancy. Windows two-DWORD and separately selected macOS framework routes are not
  pronounced mismatched. Published source/build recipes are not deployed-producer/header proof.
- The NIST SP 800-73-5 Part 2 July 2024 final §2.4.2 retrieval remains Exa-attributed text after direct
  PDF HTTP 403, not independently reviewed PDF bytes. The separately retained PC/SC Part 5 revision
  2.01.01 PDF and text derivative are not conflated with that gap. Its §3.2.5.2 desired-disposition and
  if-possible wording supports request/status language, not observed physical power-off or PIN-state
  clearing. Revised normative PIV wording and the physical lane remain held.
- Historical `.8` macOS, stable 1.18.1 source, Linux native-only versus physical populations and the
  unresolved serial-9681620 cause retain distinct identities. The user's future Windows concurrency
  delta is unready/unreviewed; no unfinished branch was inspected.

Original-source review used the retained source registers, exact cited passages and local code/test
assertions rather than current web summaries. The revision/status/actor/strength/applicability limits
in those registers remain necessary. None of the accepted passages closes a native, deployment,
device, cross-process, permission, producer or historical-cause qualification.

## Examples, dispositions and preservation

The current three XML code bodies exactly match `example-bodies.json` by static extraction. They are
one complete top-level startup policy/discovery snippet and two composable methods with typed
prerequisites, not omitted-helper fragments. The generic result returns the actual connection to an
enclosing `await using` scope. Both method examples dispose the borrowed raw session before the
connection, including exceptional exit. The startup snippet owns no disposable discovered result.
API names/imports were source-checked; **compilation remains VALID-NOT-CHECKED and all examples are
NOT-EXECUTED**. The two original bodies remain recorded. Native C signatures are reference declarations,
and the commented-out Linux scan remains inactive history, not a runnable consumer example.

Independent static reconciliation found 213 unique eligible paths, with 62 EDIT, 135 RETAIN and 16 HOLD;
122 have candidate associations. All 37 local IDs map to the 36 original proposals FPT-022–FPT-057,
including the nonadditive ambient-context group. Every eligible path has a nonempty reason, and all
edited paths have candidate/proposal mappings. Retain/hold reasons and shared/external handoffs do not
silently convert implementation or evidence gaps into documentation fixes. M1 and M3 are remaining
documentation omissions within those mapped scopes, not missing candidate identities.

All 213 current raw hashes, base hashes and source-pin hashes matched the file-disposition record;
all 22 retained-original hashes in `source-citations.json` matched. Independent comparison of verbatim
nonblank lines excluding only full-line `//` comments, after explicit BOM/newline handling, found no
noncomment changes. Inline comments and block comments remain in that comparison; BOM and final
newline state were preserved. This bounded text check protects executable code, assertions, imports,
directives, declarations, defaults, attributes and strings for this patch; it is not a C# parser or
compiler proof. The observed cached diff was empty. No index/ref mutation was performed.

This review did not run SDK build/tests, formatting, native libraries, devices, cryptographic SDK
operations, child hosts or subagents. Static Python/Git reads and hashes only were used. It writes
only this report and `review-identities.json`; product source, editor records and originals are untouched.
Coordinator-owned bound-checking, compilation and example validation remain **pending**, not passed
by this review. Stratified clean-unit sampling is not claimed by the 213-file identity check.

Resolve the three bounded corrections, review their replacement identities, complete the coordinator's
existing gates, publish Core Pass 1 and deliver the deferred finding handoff, then stop. No Pass 2
implementation, new native/device campaign or additional planning project is authorized here.
