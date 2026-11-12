# YubiOTP/Core boundary investigation

**Outcome:** complete source/assertion investigation of the exact **324 assigned
semantic units**, with **269 read-clean and 55 read-candidate**, zero unread.
The worker read all **62 disposition paths / 10,388 lines** in complete local
caller/helper/assertion context. Nine local groups, `CORE-OTP-X001` through
`CORE-OTP-X009`, are ready for coordinator triage. No accepted Core IDs were allocated.
Source investigation completion is separate from fresh editing, independent review,
sampling, compilation, tests and platform/device qualification.

Source remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Initial metadata HEAD
was `9d72160375862dfd9d09793538f64a7d4f96e398`; record-only advances were observed
while this boundary ran. `coverage-summary.json` records the serialization HEAD.
Every assigned raw source/range hash matches inventory; every disposition path
matches pinned Git content with BOM/newline normalization explicit.

**Inventory count qualification:** the packet's literal `paths` array has 61 entries,
`disposition_paths` has 62, and `file_count` says 62. The extra disposition path is
`src/YubiOtp/src/PublicAPI.Shipped.txt`, containing only `#nullable enable` and
having no assigned semantic unit. This return follows the exact 324 IDs and both
literal path arrays without editing the central inventory or inventing another unit.

## Durable records

All records are under [boundaries/YubiOtp](boundaries/YubiOtp/).

| Record | Purpose and limit |
| --- | --- |
| [Unit dispositions](boundaries/YubiOtp/unit-dispositions.json) | Every original ID, full character/line coordinates, raw/range hashes, localized candidate association and pending review/validation |
| [File identities](boundaries/YubiOtp/file-identities.json) / [file dispositions](boundaries/YubiOtp/file-dispositions.json) | All 62 paths, Git/raw/normalized identities, complete-file observations and explicit unchanged reasons |
| [Authored semantic notes](boundaries/YubiOtp/semantic-notes.json) | Distinct full implementation/assertion/context reasoning; record assembly does not infer a read from hashes |
| [Candidates](boundaries/YubiOtp/candidates.md) / [index](boundaries/YubiOtp/candidate-index.json) | X001–X009 with separate documentation/runtime/decision/qualification facets and exact original anchors |
| [Unchanged paths](boundaries/YubiOtp/unchanged-paths.json) | Every candidate file and dependency path retained with a reason, future owner and reopening trigger |
| [Call flow and verification](boundaries/YubiOtp/flow-and-verification.md) | Factory/OS/initialization/failure/cancel/version/buffer tables, actual assertion scopes and nine meaningful unexecuted proposals |
| [Inherited dependencies](boundaries/YubiOtp/inherited-dispositions.json) | All 14 routed source-run keys in four nonadditive groups; authoritative states/conclusions preserved |
| [Applet findings](boundaries/YubiOtp/applet-findings-dispositions.json) | OTP-IMP-001–012 keep original applet scope and precise boundary implications |
| [Sources](boundaries/YubiOtp/sources.md) / [source identities](boundaries/YubiOtp/source-identities.json) | Independently read pinned Python/vendor originals, Context7/current Microsoft reference, inherited authority limits and bounded gaps |
| [Clean nominees](boundaries/YubiOtp/clean-nominees.json) | Substantive eligible units for later independent sampling; nominations are not sampling completion |
| [Coverage](boundaries/YubiOtp/coverage-summary.json) / [preservation](boundaries/YubiOtp/preservation-verification.json) | Exact populations and static identity checks; zero SDK/native/device/crypto executions |
| [Verification receipt](boundaries/YubiOtp/verification-receipt.json) | Fresh separate record checker result, not an independent semantic review |

## Current supported journey

The device extension selects SmartCard then OTP HID among exposed interfaces, or
one explicit supported preference. SCP without a preference forces SmartCard. A
selected open or initialization failure propagates without trying another transport.
The extension owns its hidden connection after successful creation; a direct factory
borrows the caller's connection. Core binds before wire work and disposes/detaches
failed initialization. Backends borrow the effective protocol; no second disposable
backend lifetime is needed.

OTP HID initialization uses a first report for protocol firmware, optional NEO
invalid-map refresh, and a second report for cached status. SmartCard probes
Management on USB and NFC, selects OTP, then chooses the effective detected version.
An override is applied after discovery. Protocol configuration and optional SCP
precede backend construction and full readiness publication. Raw status, detected
version, effective override and metadata version retain separate provenance.

Built-in Linux/macOS typed HID paths use their dedicated OTP native owners and
accept original claims before native acquisition. Pinned Windows/custom paths use
the synchronous compatibility adapter and registered wrapper. Direct expert report
opens remain synchronous and ungrouped; each Linux/macOS facade has its own owner
instance. Actual native return, drain and checked release determine resource lifetime.
A canceled managed wait proves none of those states by itself.

Core guards a complete HID frame or APDU exchange. The applet owns CRC/expected
prefix interpretation, SmartCard sequence rules, cached status publication, prompt
terminal classification and complete task sequencing. A short SmartCard write can
need a second guarded status exchange; the guard is no whole-operation/configuration
lease. Callers await each public operation before disposal and keep configurations,
borrowed memory and prompt services alive through actual use.

## Triage priorities

| Group | Boundary outcome |
| --- | --- |
| X001 | Qualify process/known-member ownership and per-exchange versus workflow lifetime; retain correct hidden/direct factory scopes. Reuse B1/B5 mechanisms. |
| X002 | Trace first/second HID reports, custom-provider size assumptions, NEO broad catch, local polling and reset classification into actual initialization. Reuse B5 C007. |
| X003 | Follow sensitive incoming HID reports, Windows nested report-ID staging and SmartCard raw/response/prefix owners through HMAC/AES callers. Preserve B4/B5/B6/C ownership nonadditively. |
| X004 | Preserve the one-shot HMAC result until hidden-session teardown succeeds: teardown failure currently faults the outer task after backend transfer and can strand output. Extend OTP-IMP-003 with this source-derived outer producer boundary. |
| X005 | Retain invalid-code/config/parser cleanup gaps; refer unified CLI's config-before-code-parse lifetime without undoing its correct encoding fix. |
| X006 | Separate raw/effective firmware, command capability and SmartCard prompt heuristic; retain applet sequence/gate/transport findings and historical fixture limits. |
| X007 | Distinguish standalone UTF-8-as-scan-code defect from working public/unified CLI translation, Core reader responsibilities and locale/output qualification. |
| X008 | Resolve active migration relocation uncertainty by named surface: device-info exists through Management/Core; typed FIPS-mode query and old legacy mutator remain absent at this pin. |
| X009 | Preserve assertion-accurate test descriptions, actual retained-array observers and historical KAT scope; refer failure-path drain/cleanup and independent-oracle gaps separately. |

## Inherited dispositions and boundaries

- `yubiotp-001:OTP-DEP-001` now has actual selection, hidden/borrowed connection,
  initialization failure and per-platform teardown applicability. Physical grouping,
  release and full-workflow limits remain qualified.
- `yubiotp-001:OTP-DEP-002` now has actual status/framing/CRC/sequence/touch/reset and
  failed-reset refusal traces. Core report copies/polling stay shared findings;
  SmartCard validation remains applet-owned OTP-IMP-005.
- `yubiotp-001:OTP-DEP-003` now has admitted SmartCard cancellation, serialized/source/
  raw/response ownership and resolution-before-transfer traces. X004 identifies the
  later one-shot teardown boundary; B6 retains APDU/SCP copy and recovery ownership.
- `yubiotp-001:OTP-DEP-004` now has raw first/second report, Management/OTP and effective
  override provenance. Major-zero acceptance remains optimistic SDK policy rather
  than hardware support. Prompt/flag/legacy firmware decisions stay applet-owned.
- `yubiotp-001:OTP-DEP-005` has a completed current typed-surface inventory. Active
  migration can now separate relocated device-info from absent typed FIPS query and
  legacy configuration mutation. Raw command access is a separate expert tier.

The other nine routed PIV/PGP/SD/HSM/OATH keys receive only this shared mechanism's
YubiOTP applicability. Their applet-specific resets, policy floors, credentials and
physical gaps retain their own boundary owners. The old 100-row/70-group registry
and separate PGP documentation labels remain unchanged.

`audit-v2-20261022-20261031:N3` remains the unified CLI encoding/redirected-reader
history. Standalone OtpTool's OTP-IMP-009 defect is still present. OTP-IMP-001–012,
including HMAC marker, NDEF, scan-map, update/protection, transport capability and
legacy constraints, retain their exact prior scopes. Generic Core crypto metadata,
Feature or SCP policies grant no new OTP firmware/device support.

## Verification and next action

The record verifier checks exact IDs, source/range/pin hashes, original-source
captures and unchanged-path accounting. Its success is static preservation evidence,
separate from content acceptance. Complete task examples remain uncompiled in this
packet; the coordinator can reuse the existing validation foundation and the bounded
proposals rather than start another generic harness.

Prior serial 103/Linux results remain historical: raw/effective OTP 0.0.1 versus
metadata 5.8.0.beta.0, twelve HMAC vectors and scoped HID AES/touch/cancel/timeout
outcomes, USB CCID HMAC 6985 rejection, and empty-slot cleanup rather than total
device restoration. Older firmware, NFC read-back, keyboard output and all-platform
support remain qualified. Historical serial 9681620's failure is still unexplained.

Only `boundaries/YubiOtp/*` and this report were written. Product code/XML, assertions,
API snapshots, executable help/output, central ledgers, prior archives and user files
were left untouched. No build/test, formatter, native/device/crypto operation,
reset/ykman/touch, index/ref update, commit/push or subagent ran. Static Python/Git
reads and bounded original-document retrieval were the only execution; temporary
environment paths were set to the home-backed core-001 runtime.

Next action is coordinator triage followed by fresh editing and independent review.
The user's new Windows CCID/HID work requires its later ready handoff/delta; pinned
B4 does not review it. PIV physical deferrals remain in force. This return requests
no new device action, runtime change or phase transition.
