# WebAuthn/Core dedicated boundary return

**Outcome:** source investigation is complete with explicit qualifications at
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. This return dispositions all **265 assigned semantic units**
across **95 selected paths** and the complete **129-path candidate-file population**, including
context-only paths with no assigned units. It is the dedicated direct/indirect WebAuthn → CTAP2 →
Core boundary review, not another whole-WebAuthn audit or closure of its 21 engineering findings.

Final investigator outcomes are **7 read-candidate, 80 read-related-candidate context, 177 read-clean
and 1 declaration-snapshot exclusion**, with zero unread assigned IDs. Read-clean is scoped source/
documentation judgment, not runtime acceptance. The snapshot receives explicit public declaration/
ownership inventory reconciliation rather than an invented prose-clean verdict. Fourteen substantive
clean nominees are available for later stratified sampling; boilerplate/header units are not substitutes.

Twelve local concern groups, `CORE-WA-X001`–`X012`, retain separate documentation, runtime, decision and
qualification facets. These are candidate groups, not accepted Core IDs or an additive independent-bug
count. All **31 routed source-run keys** preserve original definitions, aliases and authoritative states.

## Record map

| Record | Purpose |
| --- | --- |
| [Unit dispositions](boundaries/WebAuthn/unit-dispositions.json) | Exact ID/path/range/hash, completed read/exclusion, local outcome and later gates |
| [File dispositions](boundaries/WebAuthn/file-dispositions.json) | All 129 candidate paths, selected versus context-only scope, pin/current identities and unchanged outcome |
| [Call graph and traces](boundaries/WebAuthn/traces.md) | Direct Core use, indirect native route, ownership, creation/ceremony/failure/retry/cancel/reset/version/crypto traces |
| [Candidates and reopening map](boundaries/WebAuthn/candidates.md) | Twelve provisional groups, actual XML/guide/sample/migration/internal-test surfaces and precise triggers |
| [CTAP2 additive handoff](boundaries/WebAuthn/ctap2-handoff.md) | Each named WebAuthn-dependent question resolved or explicitly retained; source disagreements/actor bounds |
| [Inherited outcomes](boundaries/WebAuthn/inherited-dispositions.json) | All 31 original source-run keys; no automatic applet/Core closure |
| [Actual test oracles](boundaries/WebAuthn/test-oracles.md) | Assertion-accurate limits, meaningful unexecuted checks, hardware/Windows matrix |
| [Source authority](boundaries/WebAuthn/sources.md) / [identities](boundaries/WebAuthn/source-identities.json) | Exact original revision/status/section/actor/strength/application, retained bytes and bounded gaps |
| [Static summary](boundaries/WebAuthn/verification.json) / [checker receipt](boundaries/WebAuthn/verification-receipt.json) | Exact coverage/hash/preservation/link reconciliation, separate from independent content or runtime review |

## Current cross-layer agreement

The device factory delegates one transport selection/open/initialization to CTAP2. Core binds before
I/O, prepares the HID channel, or CTAP selects FIDO on SmartCard and obtains GetInfo. Detected firmware
gates precede effective override/configuration and optional SCP; the whole initializer precedes ready
publication. WebAuthn adds no transport fallback, applet SELECT, native owner, firmware gate or reset
reinitialization. It adopts a supplied CTAP session only on successful construction; that adoption does
not acquire ownership of a connection the direct session already borrowed from its caller.

WebAuthn owns ceremony validation, application RP authorization, deadline, PIN prompting/retry, token/
auth-tag owners and extension attempts/results. CTAP owns operation/status/schema; Core owns individual
exchange framing/drain and native ownership routes. The native owner decides actual completion/release.
Core per-exchange drain does not join a complete WebAuthn ceremony, post-exchange callback or blob output
workflow. Await/serialize the complete ceremony before disposing its client; keep borrowed inputs and
application prompt resources usable through actual remaining work.

The immediate active documentation proposals are the README's overly broad all-interface exclusion
headline, a whole-ceremony lifetime condition beside client ownership, and scoped fake/native test
descriptions/provenance. Existing accurate qualifications for timeout races, PIN context units, borrowed
attestation, sensitive-result copies, version sources and applet conformance gaps remain explicit
candidate-associated unchanged paths. The detailed reopening table distinguishes changes required now,
changes triggered by later Core repairs, and applet-owned engineering.

## CTAP2 handoff outcome

- **WA-XREF-001 / CTAP-XREF-001:** preserve documentation resolution for corrected User/descriptor prose.
- **WA-IMP-010:** source confirms final descriptor recovery remains missing. Probe may know a narrowed
  singleton, but matcher and final response independently read optional final descriptor and publish
  Empty. CTAP2.0 permits omission for that actual singleton; later tables require the field. No first-
  original-entry guessing or parser repair is authorized.
- **CTAP-DEC-002:** actual native output retains modeled User through cached selection. There is no SDK
  upload/response serializer. Actor trace is settled; maintainer/coordinator must still select the native
  DTO versus platform/WebAuthn/browser/RP disclosure contract before any filtering decision. The
  authenticator's no-identifying-fields-without-UV rule and platform picker's only-id return have distinct
  actors and conditions.
- **WA-IMP-006/007/013/017/019:** borrowed versus copied response/attestation storage, default Aaguid,
  PRF/blob lifetime, optional direct-blob support and empty signing handles retain distinct applet/CTAP/
  Core owners. A Core change alone supplies no fix or automatic closure.
- **B4/B6:** completed pinned mechanism follow-ups agree with the actual WebAuthn caller route, with all
  producer/native/API/ISO/GP/vendor/profile/runtime qualifications retained. CTAP's owner can consume the
  additive handoff without changing its original 590 units/23 concerns. Final independent cross-layer
  acceptance remains the coordinator/CTAP owner's gate.

## Evidence and completion limits

Initial research plus one focused follow-up reused the original CTAP/WebAuthn editions and current Core
family source registers, captured the existing dated W3C REC/vendor draft and pinned .NET source, and
left consequential gaps precise. Python policy is implementation context, not normative authority;
generic Core catalog applicability supplies no CTAP strict profile or provider/device qualification.

This worker changed only `boundaries/WebAuthn/*` and this report. Product source, APIs, assertions,
central ledgers/inventory, CTAP2 records, frozen archives and four user files remain untouched. Static
record scripts and HTTP/source reads ran; **no build, test, formatter, native/device/crypto experiment,
Git index/ref/commit/push or subagent ran**. Syntax/path failures during new record-tool preparation
were corrected before successful assembly; they were not SDK failures or validation evidence.

The existing 4,442 integration units / 197 overlapping reruns and Linux native-only/zero-physical
integration evidence provide no new WebAuthn ceremony qualification. UP/UV/cancellation/removal and
cross-OS/device/profile checks require their own later coordinated matrix. The user's PIV physical
deferral remains unchanged. New Windows CCID/HID work remains **UNREADY / UNREVIEWED** until the user's
ready handoff; no unfinished worktree was inspected. Independent review, triage, editing, sampling,
consumer compilation and selected current-source/platform/device verification remain separate gates.
