# YubiOTP triage and return-visit ledger

2026-11-04; Astra; baseline95e5ccaeda33954c21f2a38058d4f66b5f208787. Original18 candidates and precise
anchors remain in investigation.md. These overlapping groups are not18 independent confirmed defects.

## Documentation decisions

All OTP-C001–018 are accepted for supported documentation improvements/qualifications as described in
the investigation, subject to independent original-citation review and forthcoming hardware results.
Preserve correct existing HMAC three-layer explanations; improve reader comprehension rather than
minimizing words or converting public guidance into audit transcripts.

- C001/002: current/new access codes and full masked update values, defaults, pacing10/20ms and mode-bit
  aliases, legacy flag applicability. Device-level legacy rules require exact edition qualifications.
- C003: public ID is raw binary; modhex describes emitted characters.
- C004: copied versus borrowed inputs, immutable strings, GetConfig/output ownership; real preflight
  cleanup defects cannot be concealed by broad zeroing claims. Astra independently read complete copy/
  validation and WriteConfig cleanup ordering; defects are locally supported.
- C005/006: cached status/raw firmware versus effective session and transport-specific prompt behavior.
  Treat cached trigger as an implementation decision, not proven RequireTouch policy; hardware pending.
- C007/008: input/wire/hashed-message distinctions and variable AES ticket/verifier responsibility.
  Existing vector tests do not establish hardware results until executed; no padding implementation edits.
- C009/010: exact NDEF payload/UTF8 limits and null/empty intent, actual versus vendor firmware checks,
  NEO path and feature conditions. NFC readback and old-firmware applicability remain separate evidence.
- C011/012: nonzero HOTP IMF and38-versus45 scan map are implementation limitations, not desired protocol.
  Astra read actual IMF implementation; use default0 guidance and avoid a misleading full-map recipe.
- C013: exact backend checks/interval/CRC scope. Astra independently read short-status early return,
 5.0–<5.5 bypass and zero-sequence acceptance. Preserve strict desired malformed-input expectation in
  deferred finding while accurately qualifying existing behavior.
- C014/015: assertion-based comments, present-tense rationale, actual test effects and correct links.
- C016/017: describe demonstrative OtpTool limitations; Astra verified UTF8 bytes reach raw scan-code
  constructor and layout is unused. Runtime strings/help literals remain unchanged under token gate.
- C018: update only current YubiOTP migration sections, retaining valid feature gaps/history distinctions.

## Deferred engineering, verification and decisions

All rows are deferred-pass-2; observed facts do not authorize changes. Owner: implementation coordinator
after re-baselining. Hardware evidence may settle a question without closing a runtime defect. Breaking
release permits appropriate API correction; backward compatibility is not a goal.

| ID / candidates | Observation / next action | Exact documentation reopening trigger |
| --- | --- | --- |
| OTP-IMP-001 / C011 | HOTP IMF divides by65536 versus vendor16; local tests encode same error. Correct range/scaling and meaningful counter/wire tests. | Hotp constructor/SetHmacKey/counter semantics change → HOTP XML, README task, OtpTool IMF explanations, test comments, migration gaps |
| OTP-IMP-002 / C012 | SetScanMap enforces38 versus vendor45, with no firmware gate. Establish command contract/default reset and validate wire/device support. | Scan-map size/gate/API changes → SetScanMap XML, constants explanation, README constraints and migration gaps |
| OTP-IMP-003 / C004/C017 | Secret copies allocated before invalid-code checks/try; config lifecycle and output ownership gaps; sample parser/string/cleanup issues. Classify owned buffers and fix meaningful exceptional paths. | Allocation/validation/cleanup/response carrier or disposal changes → config/XML, README security/all affected examples, sample comments and migration recipe |
| OTP-IMP-004 / C005 | Cached trigger flag used as SmartCard RequireTouch policy though vendor distinguishes keyboard trigger. Probe status/notification matrix, then define accurate certainty/notification behavior. | ConfigState/state refresh/presence basis changes → state/session XML, both backends, README/CLAUDE, shared interaction/migration and prompt tests |
| OTP-IMP-005 / C013 | Malformed SmartCard write status can return successfully; sequence bypass interval/zero rules differ from vendor; prefix-length policy requires decision. | Backend length/CRC/progseq validation changes → exceptions/architecture comments, README/CLAUDE and backend tests |
| OTP-IMP-006 / C002/C010 | Per-flag/static-password-length/IMF firmware constraints not fully gated; effective versus raw firmware differs. Decide supported matrix and validation. | Gates/defaults/NEO normalization/override change → requirements/config/status XML, initialization guide, shared NEO path and firmware tests |
| OTP-IMP-007 / C001/C017 | Full-mask updates/new-code omission can drop state/protection; legacy4.3.2–4.3.5 code-change restriction missing. Decide explicit contract and enforce appropriate range. | Update defaults/masks/code retention/type checking/protection changes → update/put/base XML, README workflows, settings/provisioning sample comments/migration |
| OTP-IMP-008 / C009/C018 | Undefined NDEF enum/text/URI controls and UTF8 policy; language/UTF16/readback/generation gaps. Establish intended null/empty and NFC output contract. | NDEF format/input validation/disable/readback changes → NdefType/session XML, README/CLAUDE, NdefCommand and migration gaps |
| OTP-IMP-009 / C016 | OtpTool uses UTF8 password bytes as raw scan codes and ignores layout. Correct translation and command behavior tests. | StaticCommand/layout translation fixed → sample limitation, static comments, CLI option explanations and migration static-password passages |
| OTP-IMP-010 / C017 | CLI stdin/TOTP/swap-code/transport/help mismatches, JSON confirmation/output differences, parser cleanup and shutdown behavior. Scope coherent follow-ups rather than treat compatibility as established. | Parser/command/confirmation/output/session creation changes → OtpTool README guidance and affected command/XML/comments/migration entries |
| OTP-IMP-011 / C014/C015 | Weak length/configured-only assertions, non-finally cleanup, both-slot effects and self-referential CRC oracle. Improve meaningful assertions/cleanup without calling comment corrections runtime fixes. | Test setup/assertion/cleanup/fixtures change → named test explanations and module/test guides; related defect IDs retain provenance |
| OTP-IMP-012 / C006/C007/C008 | Actual transport challenge support, HMAC marker minimum and AES ticket verification coverage incomplete; possible mode-aware API policy separate. Record scoped probes, define supported contract. | Hardware evidence/padding/length/mode/capability/AES validation changes → both calculation APIs/configs, README examples, migration and known-answer commentary |

## Core dependencies — record only until Core

| ID / investigator origin | Precise deferred question | Reopen affected documentation when resolved |
| --- | --- | --- |
| OTP-DEP-001 / D001 | ApplicationSession/device helpers physical lease, initialization failure cleanup, selected transport/no fallback and disposal | Factory/extension/README ownership, OtpTool creation, migration and architecture |
| OTP-DEP-002 / D002 | OTP HID status-length invariant, protocol framing/CRC/progseq, cancellation/reset/drain/reuse | HID backend/initialization/cancellation/malformed-response explanations and IMP005 |
| OTP-DEP-003 / D003 | SmartCard admitted cancellation/drain, serialized-copy lifetime and notification exception/cleanup guarantees | Response/input ownership, README cancellation, backend comments/shared interaction and IMP003 |
| OTP-DEP-004 / D004 | FirmwareVersion alpha/beta/override, HID status normalization and raw/effective mismatch | Firmware/status/presence docs and IMP004/006 |
| OTP-DEP-005 / C018 | Missing YubiOTP FIPS/legacy configuration surfaces: relocation elsewhere unverified | Migration gap/relocation assertions after relevant module or Core boundary review |

## Editing packet

One fresh Sol editor owns documentation/comments under src/YubiOtp and accepted YubiOTP-only sections
of docs/usage/user-interaction.md, docs/migration/v1-to-v2.md, docs/migration/v1-to-v2-gaps.md,
docs/migration/v1-to-v2-map.yml, docs/architecture/device-discovery-guarantees.md. Other shared sections
need coordinator scope first. Preserve DOC-002 unrelated map parse defect. No Core/other-module document
target reads, source edits or dedicated boundary review. No executable source/help strings/assertions/
snapshot/config changes. Documentation corrections in sample comments are allowed; sample behavior fixes
remain deferred. All meaningful final examples require complete compilation context. Editor saves
edit-report.md mapping findings/paths/example classes/hashes; coordinator alone owns current status.

## Execution blocker, separate from module defects

Initial integration attempt selected15 methods but executed0: shared fixture failed exclusive SmartCard
initialization because another client holds the reader. Raw log records ConnectionInUseException, then
misleading generic no-authorized-device skips. Existing allow list already includes connected103.
No mutation occurred. Toolchain also translated negative!~ filter into positive MTP preflight; next
attempt selects exact IntegrationTests project to avoid unrelated unit filtering. Preserve both results;
do not edit Core or tooling during Pass1 to conceal them.
