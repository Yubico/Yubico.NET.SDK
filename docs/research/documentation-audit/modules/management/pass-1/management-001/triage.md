# Management triage and deferred-work ledger

## Authority and progression

Coordinator: Astra. Baseline `da61d2e6`; documentation-only Pass 1. This ledger assigns accepted
documentation work and defers implementation/test changes to the campaign's second sweep. Investigation
IDs retain their original evidence; overlapping findings are not counted as separate bugs.

2026-11-03: A/B/C investigations received and adjudicated. IDs below reconcile overlapping reports;
documentation acceptance is not independent-review completion. All 41 initial candidate groups
(A14/B8/C19) have a disposition here, including clean/optional B-08; they are not 41 independent bugs.

## Accepted documentation groups

| ID | Evidence candidates | Accepted correction | Owner / state |
| --- | --- | --- | --- |
| MGMT-DOC-001 | B-01, B-05 | Explain full replacement masks, null omission and actual USB-None validation; distinguish supported-device responsibility from SDK enforcement. | B models; A session; C guides — accepted |
| MGMT-DOC-002 | B-02, B-07 | Explain per-command authorization, code replacement/removal with sixteen zero bytes, borrowed buffer lifetime, reboot request versus reset. | B options; A operations; C procedures — accepted |
| MGMT-DOC-003 | B-03, B-04 | Correct restricted-NFC firmware/family, USB-power recovery and NDEF qualifications. Describe false as an encoded value without promising that it restores NFC; use documented USB-power recovery. | B property; A tests; C guide — accepted, false behavior deferred |
| MGMT-DOC-004 | B-06 | Explain timeout units/conditions/zero semantics, assignment-time validation and 3600/60 as SDK limits, not firmware maxima. Independent reviewer must recheck zero semantics against originals. | B models; C examples — accepted |
| MGMT-DOC-005 | B-08 | Retain accurate local TLV order/cleanup rationale; do not promote sibling ordering to protocol requirement or copy vendor NFC-tag typo. | B internal; A test; C guide — accepted |
| MGMT-DOC-006 | A-01, A-03, A-09 | Complete session/interface/extension contracts: direct borrowing vs convenience ownership, initialization/override, metadata reads, sequencing, transport-specific cancellation, errors and supported vs enforced firmware. | A API; C guides — accepted |
| MGMT-DOC-007 | A-02 | Identify Bio Multi-protocol Edition reset, narrow erasure claims and remove unsupported configuration-lock recovery promise; distinguish firmware check from product support. | A reset XML; C prose/sample comments — accepted |
| MGMT-DOC-008 | A-04, A-05, A-06, A-07, A-08, A-10 | Correct test explanations to actual row binding, cached metadata, transport pinning, assertions and restoration/cleanup attempts. | A test C#; C module/shared guidance — accepted |
| MGMT-DOC-009 | A-11, A-12 | Correct OTP CRC attribution/length framing; replace obsolete phase/builder history and placeholder narration with current invariants; preserve genuine unfinished coverage in ledger. | A internals/tests; C contributor prose — accepted |
| MGMT-DOC-010 | A-13, A-14 | Qualify reliance on current FIPS/form-factor metadata, without redefining wrong decoding as a valid contract or claiming certification. | C consumer/contributor qualification; Core dependency recorded — accepted |
| MGMT-DOC-011 | C-02..08, C-15..18 | Repair complete current guide/XML examples and narrow shared references: actual signatures, scoped lifetimes, helper rows/cache, current architecture, process/group/finalizer bounds. Preserve historical snapshots. | C guides/shared explanatory comments — accepted |
| MGMT-DOC-012 | C-09..14, C-16, C-18 | Document actual ManagementTool tasks/commands and current limits; correct sample XML/comments and result request semantics; qualify string-input cleanup and SmartCard-only active selection. Executable help/output remains deferred. | C example guide/comments — accepted |

## Deferred implementation / test findings

All rows: implementation not started, hardware verification not run, affected Pass-1 documentation
reviewed with required corrections re-reviewed. These statuses are independent; truthful documentation
does not close the underlying implementation or device-verification work.

| ID | Evidence / observed condition | Future decision or verification | Documentation re-review trigger |
| --- | --- | --- | --- |
| MGMT-IMP-001 | B-03; SetDeviceConfig gates the operation at 5.0 but not NFC restriction at 5.7/NFC capability. Active migration already flags field-specific gating. | Decide and implement field-specific firmware/device preflight policy; verify applicable device families/transports. | DeviceConfig.NfcRestricted, session/interface XML, README requirements and restriction guidance, CLAUDE, migration gaps, capability test comments and ManagementTool. |
| MGMT-IMP-002 | B-04; encoder emits tag 17 byte zero for false; primary byte-level semantics and restoration timing unresolved. | Obtain firmware definition or scoped true/false/null observations with USB power/reboot boundaries; choose contract before code change. | Restriction property, guides, test restoration explanations and sample UI. Remove qualifications only after evidence supports them. |
| MGMT-IMP-003 | B-05; Validate rejects exactly USB None; nonzero out-of-wire-width values may narrow to zero, and actual supported applications are not checked. | Decide request extensibility, width validation and device-aware supported-mask policy. Source-derived risk, not observed hardware lockout. | DeviceConfig type/property/Validate XML, session contract, masks/examples, README/CLAUDE, audit-test claims and migration coverage. |
| MGMT-IMP-004 | A-07/B supporting evidence; lock-code test zeroing follows fallible restoration; simple configuration tests restore only after successful assertions; several restorations are not read back. C review also exposed undisposed caller-owned default SCP parameter objects in two executable tests (see correction note below). | Make secret zeroing independent of hardware cleanup, dispose owned SCP inputs, restore after failed assertions, preserve failure provenance and assert restored state. NFC false semantics additionally depend on MGMT-IMP-002. | Lock-code/simple/capability/SCP test comments, test guidance and any claims of verified restoration/cleanup. |
| MGMT-IMP-005 | A-07; production encoded-buffer cleanup is in finally but test covers successful backend write only. | Meaningfully verify retained encoded views on failure/cancellation, without inventing a production leak. | Zeroing test summary, session/options ownership and security guidance. |
| MGMT-IMP-006 | A-05; some transport-filtered one-shot/helper tests use unpinned default session selection. | Pin and assert actual operation transport for intended interface coverage; record device/firmware/transport boundaries. | Simple/advanced test summaries, helper/transport guidance and verification reports. |
| MGMT-IMP-007 | A-06; wrong-SCP and missing-lock tests accept any exception, permitting unrelated failure to satisfy the assertion. | Assert precise device/error provenance in meaningful failure cases. | Test explanations and authentication/configuration-error guidance. |
| MGMT-IMP-008 | A-09; 4.1 FeatureDeviceInfo is declared but read/factory paths do not enforce it. | Settle legacy-firmware read and initialization-probe policy, including override/development behavior, then regression-test it. | GetDeviceInfo API, README requirements/feature table, initialization guide and legacy tests. |
| MGMT-IMP-009 | A-10; test MinFirmware="5.0" is unparsable by three-component parser and silently ignored by shared filter. | Correct executable criterion; decide strict malformed-filter handling in Tests.Shared. | Test summary, filtering examples, shared attribute/helper XML and coverage claims. |
| MGMT-IMP-010 | A-02; reset has generic firmware/transport gating but no family gate or reset-success invalidation. Management configuration/lock erasure and post-reset session behavior are unresolved. | Agree product/preflight/lifecycle contract with authoritative evidence; verify supported Bio MPE reset effects and connection/session boundaries. Do not copy OATH lifecycle policy automatically. | Reset API/example, README features/reset/security, contributor guide, ManagementTool and migration comparisons. |
| MGMT-IMP-011 | A-06/A-12; Bio form-factor/firmware checks are not biometric-operation tests; broader property/FIPS claims lack assertions. | Select meaningful missing feature/metadata tests in second sweep; do not create placeholder tests or infer certification. | Bio/FIPS/property test explanations and module coverage claims. |
| MGMT-IMP-012 | A per-file SmartCardBackend disposition; dormant internal SetMode uses P1=0 whereas pinned sibling uses 0x11. No current public SetMode surface. | Re-baseline legacy-operation intent and wire evidence if selecting legacy support; not a current public regression claim. | Internal backend summary, migration legacy-mode gap and any future public mode documentation. |
| MGMT-IMP-013 | C-09; active ManagementTool static selector permits SmartCard only while unused ManagementDeviceSelector permits all three. | Decide routing and test HID-only/composite selection without hardware first. | Module/example README, selector comments, transport capabilities and no-device guidance. |
| MGMT-IMP-014 | C-10 corrected by C-MOD-R4: Program recognizes info/config data commands and top-level -h/--help/help, but ignores trailing arguments such as info --help; no NFC-restriction action despite helper. | Decide command-specific argument/help and reachable-feature contract; test dispatch and update executable help. | Tool task/command guide, feature-helper examples and menu/CLI descriptions. |
| MGMT-IMP-015 | C-11; string-based lock prompts retain immutable secret copies; confirmation prompt can throw before first byte array enters cleanup. | Adopt bounded secret-input design and unconditional exceptional cleanup; verify failure paths. | Prompt XML, sample security section, caller ownership examples and lock procedures. |
| MGMT-IMP-016 | C-12; menu callbacks discard console token; wrappers/metadata selector catch cancellation as generic failure. Shared/sibling pattern is a dependency, not SDK session behavior. | Forward and preserve cancellation through menus/wrappers/selectors; scope shared changes separately and verify cleanup/error distinction. | Sample cancellation/error/ownership guidance and related selector comments; shared CLI and later sibling audits. |
| MGMT-IMP-017 | C-13/C-16; ConfigResult.RebootRequired carries request flag, not observed need; output labels IsFips as mode, omits touch-eject conditions, and test-host strings name retired ManagementSessionSimple. | Decide reboot-result contract and correct executable output/diagnostic descriptions. Shared reset eligibility/transport lies under MGMT-IMP-010. | ConfigResult XML, tool result/security/flags guide, shared command summary and test-host explanation. |

## Cross-module dependencies

| ID | Evidence / condition | Deferred work and documentation trigger |
| --- | --- | --- |
| MGMT-XREF-001 | A-13; Core DeviceInfo FIPS tags use application-bit decoder despite distinct local/sibling FIPS mapping; unused FromFips has an additional width concern. | Separately scoped Core/Pass-2 decoding and filter work needs authoritative fixtures and width tests. Re-review Core metadata XML, Management FIPS use/test comments and Tests.Shared filtering. Not hardware-qualified or a certification finding. |
| MGMT-XREF-002 | A-14; Core casts raw flagged form-factor byte to non-flags enum, so e.g. vendor 0x81 differs from UsbAKeychain. | Decide normalized/raw forward-compatible representation and verify flagged fixtures. Re-review Management display/filter guidance and Core/Tests.Shared contracts. Core runtime remains deferred. |
| MGMT-XREF-003 | A-04; shared test-helper/CLAUDE claims about per-device rows and fresh metadata disagreed with actual one-placeholder/first-match/cached binding. | Narrowly relevant explanatory claims corrected and independently reviewed in Pass 1; runtime row policy unchanged. Documentation dependency resolved for this source checkpoint; reopen after binding/cache changes. |

## C investigation reconciliation

| Candidates | Disposition |
| --- | --- |
| C-01 | MGMT-DOC-007; MGMT-IMP-010 also includes sample firmware-only IsResetSupported/UI and shared reset command's advertised HID transports. |
| C-02, C-04, C-05 | MGMT-DOC-008/011; related IMP-004/006/007/009/011 and XREF-003. |
| C-03, C-06, C-07, C-08 | MGMT-DOC-006/009/011; current signatures, architecture and full-block lifetime corrections. |
| C-09 | MGMT-DOC-012 and MGMT-IMP-013. |
| C-10 | MGMT-DOC-012 and MGMT-IMP-014. |
| C-11 | MGMT-DOC-012 and MGMT-IMP-015. |
| C-12 | MGMT-DOC-012 and MGMT-IMP-016. |
| C-13 | MGMT-DOC-010/012 and MGMT-IMP-017; Core interpretation dependencies XREF-001/002. |
| C-14 | MGMT-DOC-001..005; corresponding B implementation dependencies unchanged. |
| C-15 | MGMT-DOC-006/011; qualify grouping/process-local registry and deterministic disposal versus actual best-effort connection finalizer. |
| C-16 | MGMT-DOC-011/012 and MGMT-IMP-017; snapshot/one-shot query and error explanation. |
| C-17 | MGMT-DOC-011; repair active sibling resolution example, leave original historical plans immutable. |
| C-18 | MGMT-DOC-009/011/012; useful editorial corrections, preserve rationale. |
| C-19 | MGMT-DOC-010 and XREF-001/002; duplicate dependency, not another Core bug count. |

The example's old Spectre HintPath is a build-portability concern observed by C, not a confirmed build
failure; retain it as an unselected support observation for Pass 2 re-baselining. The unrelated obsolete
module path in the API-design skill is instruction-maintenance follow-up, not a consumer-doc correction
or authorization to modify skills in this run. Historical scan/performance results remain attributed
historical evidence, never this run's hardware results.

## Decisions and evidence limits

- B-06: preserve current 3600/60 SDK timeout limits. Investigation quotes a pre-existing maintainer record
  in an untracked user file; it is historical intent, not firmware authority. No range expansion selected.
- B-04: do not infer false-byte behavior from Python omitting it or v1 .NET documenting it. Public prose
  should explain the supported true/recovery task and accurately describe the encoded false value without
  promising restoration. Keep uncertainty actionable in MGMT-IMP-002, not as permanent audit narration.
- B-08: vendor Config Reference has a contradictory NFC write tag. Retain the implementation's 0x0E;
  sibling code corroborates it but does not repair the vendor specification or establish full conformance.
- All future contract decisions follow the breaking-release direction; backward API compatibility is not
  a goal. This does not authorize API changes in Pass 1.
- A-02: Astra independently fetched both official Bio MPE manual and DeviceResetCommand reference on
  2026-11-03 (same URLs as investigation A R2/R3). They establish Bio MPE, simultaneous FIDO/PIV reset,
  shared PIN/fingerprints/credential destruction and retained F9 attestation slot, not generic Management
  lock/configuration erasure. Accept that narrower description; preserve lifecycle/erasure unknowns in
  MGMT-IMP-010. The individual FIDO ResetBlocked condition is not transferred to Management reset.
- A-13/A-14: use precise qualifications where consumers interpret metadata. Do not re-audit or repair Core
  in this run; preserve original decoder evidence and cross-module dependencies for later selection.
- C source-authority correction: `docs/migration/v1-to-v2-gaps.md` explicitly declares a point-in-time
  baseline. Preserve its historical Builder/legacy/gating statements; B's proposed active rewrite is not
  accepted. Put current qualifications in the module and current migration guide where relevant.
- C-15: correct stale "no connection finalizer" absolutes where directly explaining Management lifetime.
  Deterministic caller disposal remains required; best-effort native cleanup is not a lifetime guarantee.
- C-MOD-R4 erratum: original C-10's "only two command tokens" claim was incomplete. Source recognizes
  top-level help tokens too; only trailing command arguments are ignored. Preserve original investigation
  with this correction and the independent review; qualify MGMT-IMP-014 rather than discarding its real gap.
- C-MOD-R1 dependency expansion: inline `Scp03KeyParameters.Default` creates caller-owned disposable input.
  The documentation example is corrected in Pass 1. Coordinator found two analogous executable test uses:
  `ManagementSessionAdvancedTests` SCP03 helper argument and `IYubiKeyExtensionsTransportTests` SCP/HID
  rejection options. Their disposal is deferred under MGMT-IMP-004. These are public default keys, not
  observed private-key disclosure or a production session-key leak. The simple integration test already
  uses a local `using` scope; do not claim every SCP example leaks inputs.

## Source evidence

[Investigation B](investigation-b.md) retains exact source locations, focused primary quotations,
URLs/access date/revision status, all owned-unit dispositions and later documentation dependencies.

[Investigation A](investigation-a.md) covers 21 files, 14 overlapping candidates and 20 named clean units;
its primary-source cache and narrow Core traces remain the original evidence for A dispositions.

[Investigation C](investigation-c.md) covers all 32 remaining module/support paths, the external-reference
manifest, 49 baseline module Markdown blocks and five C-owned XML snippets. Its historical exclusions
govern classification of the migration-gap snapshot and original plans.
