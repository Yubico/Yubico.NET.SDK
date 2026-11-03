# Independent durable-checkpoint review

2026-11-03. Target: `docs/research/webauthn-documentation-audit-checkpoint.md` as read during the ongoing WebAuthn cycle. This is an evidence/handoff review, not a code-diff or implementation review.

## Outcome

The checkpoint faithfully carries the deferred engineering ledger into tracked documentation. **Two required coverage/count clarifications remain.** No additional substantive misstatement was found in the 20 deferred items or the CTAP2 documentation dependency when compared with the completed investigator reports and triage. The checkpoint correctly remains **in progress**; missing final gate results are not a defect at this stage.

Read root `CLAUDE.md`, the checkpoint, `triage.md`, all of `investigation-a.md`, `investigation-b.md`, `investigation-c.md`, `sampling.md`, and `sample-expansion.md`. Read `status.md` solely to corroborate the checkpoint's baseline `docs-qa` claim and inventory definitions. No editor/reviewer in-flight diffs or source edits were inspected. No repository edits, builds, tests, hardware operations, delegation, or new external research were performed. Only this report was written.

## Required factual corrections / count definitions

1. **Checkpoint lines 16–17: external coverage is now 30 files, not just 25 + 4.** Retain the useful categories: 25 original active-reference files + four supplemental migration files + the one directly dependent Core XML example in `src/Core/src/Credentials/UserPresenceIntent.cs`. Evidence: `triage.md:14–19`, `sampling.md:20–23`, `sample-expansion.md:66–85`. The companion is an additional external file, not a 132nd module file or a broad Core audit. Suggested replacement:

   > External coverage includes WebAuthn-specific passages in 25 active-reference files, four supplemental migration files, and one directly dependent Core XML example: 30 external files in total. Historical and generated material retains its original context.

2. **Checkpoint lines 19–20: explicitly define 51 as raw investigator-report findings before sample misses, not an accepted/unique-defect total.** The current overlap qualification is good but does not state the sample boundary. Evidence: `triage.md:7`, `sampling.md:13,18–23`, and `sample-expansion.md:7,13–74`. Suggested wording:

   > The three investigator reports contain 51 raw findings before quality-sampling follow-ups, with overlapping root causes; this is not a count of independent defects.

   Do not add WA-S-001 and WA-SAMPLE-EXPAND-001 as two new independent misses: they identify the same guide location. Expansion confirms that location and adds one companion XML location with the same argument-binding cause. Leave the 51-report count intact rather than silently presenting a new deduplicated total.

## Deferred-work evidence reconciliation

All IDs below are local audit ledger identifiers, not assertions that external issues were filed.

| Checkpoint ID | Original evidence | Assessment |
| --- | --- | --- |
| WA-IMP-001 | A-003 | Faithful: distinguishes pass-through attestation from sending an enterprise request, and retains missing Indirect anonymization. |
| WA-IMP-002 | A-005; B-007/009/012; C-D2 | Faithful: error boundaries and capability-gated previewSign omission remain unresolved policy/implementation work, not universal normalized errors. |
| WA-IMP-003 | A-006 | Faithful: cooperative cancellation, success race, and framework rejection of oversized positive delay are retained without a hard-deadline guarantee. |
| WA-IMP-004 | A-009 | Faithful: unguarded hardware assertion remains test applicability work; prose does not pretend capability guards were added. |
| WA-IMP-005 | A-012 | Faithful: exact CCDToString gap and low-level-versus-ceremony validation boundary survive; no claim that ordinary JSON validity proves exact serialization conformance. |
| WA-IMP-006 | A-013 | Faithful: borrowed CBOR/mutation discrepancy and ownership re-review are explicit; no invented snapshot guarantee. |
| WA-IMP-007 | A-014 | Faithful: default empty span and nonreflexive equality are identified as a defect, not intended default semantics. |
| WA-IMP-008 | A-018 | Faithful: accurately limits existing test evidence to disposal access rejection, not retained-view zeroing. It does not imply production zeroing is disproven. |
| WA-IMP-009 | A-021 | Faithful: protocol-2-only backend and built-in-UV capability review are retained without asserting that all non-PIN operations require protocol 2. |
| WA-IMP-010 | A-Q1 | Faithful: permitted final descriptor omission can publish an empty ID. The summary does not promise usable IDs or suggest guessing a multi-entry selection. |
| WA-IMP-011 | B-001; C-D1 | Faithful: verified-variant requirement is normative; current preference handling is a limitation. The separate paragraph at lines 80–83 makes Required a workaround, not a conformance fix. |
| WA-IMP-012 | B-002 | Faithful: the pre-creation enforcement promise is unmet; an ignored CTAP builder field is distinguished from a wire enforcement flag. Lines 82–83 correctly reject post-creation checking as equivalent prevention. No MUST is invented from the standard's SHOULD NOT. |
| WA-IMP-013 | B-003 | Faithful: missing output owner/cleanup API, PRF intermediate copies and temporary salt storage remain engineering work. No writable result span or client-disposal cleanup guarantee is invented. |
| WA-IMP-014 | B-005 | Faithful: Required can return a credential without blob support; this is not reframed as an intended weaker enum contract. |
| WA-IMP-015 | B-006 | Faithful: original allow-list cardinality matters; narrowing the device request later does not satisfy the WebAuthn input constraint. |
| WA-IMP-016 | B-007 | Faithful and appropriately explicit: null-array conversion outcome remains runtime-unverified. It is not conflated with independently source-established storage-error propagation under WA-IMP-002. |
| WA-IMP-017 | B-008 | Faithful: direct extension is outside the present SDK route and the token requirement is attributed to SDK writes, not unconditional CTAP policy. |
| WA-IMP-018 | B-010 | Faithful: input presence ignores Requested=false; this remains a limitation/input-handling decision, not a normative interpretation of false. |
| WA-IMP-019 | B-013 | Faithful: draft-permitted empty generated handles cannot currently round-trip through signing parameters. No claim that all draft signing is unsupported. |
| WA-IMP-020 | B-016 | Faithful: extra inner algorithm fixture field and missing minimal flags-only evidence are test follow-ups, not a claim that the decoder requires both. |
| WA-XREF-001 | A-002 | Faithful: CTAP2 User documentation is a later documentation-cycle dependency and handle conditions are separated from identifying display fields. |

The compact ledger does not reproduce every supporting condition; the original reports remain the detailed evidence. No omitted condition found here reverses a deferred finding or turns a current limitation into an intended normative contract.

## Verification, references, and confidence

- Checkpoint lines 38–41 preserve specification identities and distinguish client/RP/platform/authenticator actors. The report indexes support the stated L2/L3 and vendor-draft dates. Citation repair is explicitly not automatic standards-target migration.
- Lines 43–45 accurately describe the eight-unit process sample and the example-category expansion. These are not statistical certification or all-file independent re-review.
- Baseline `docs-qa` success at line 87 is corroborated by `status.md:31`: zero warnings and a recorded run-log path. This reviewer did not execute it or independently inspect that log. The checkpoint does not label it final verification or evidence of semantic correctness.
- Lines 88–90 appropriately deny hardware, cross-platform, certification, and comprehensive conformance evidence. No investigator report is converted into runtime evidence.
- `DOC-002` is already present in triage, C-D4 and run status context as the pre-existing migration-map parse problem; it is not an invented reference in this checkpoint. WA-IMP/WA-XREF IDs match triage exactly. No new issue, ticket, or upstream filing is asserted.

## Optional wording improvements (not factual blockers)

1. For WA-IMP-017, restore triage's explicit phrase “support decision, not automatically a required implementation.” The existing “if support expands” already points in this direction; an explicit distinction would further protect optional feature support from being mistaken for a mandatory conformance fix.
2. For the surface count sentence, add “textually/source-reconciled” if useful. Investigation C explicitly distinguishes 44 top-level public types / 542 snapshot entries from compiler/API-analyzer equivalence. Current wording does not claim compilation, so this is optional precision, not an identified false verification claim.
3. If making the baseline mechanical gate more durable is desired, name its recorded run identifier from status. A local artifact path is provenance, not a guarantee that the artifact ships with the repository backup.

## Finalization follow-ups (not current blockers)

- Preserve the module inventory definitions: 74 source C# + 47 test C# + three module Markdown + seven support files = 131 module files. API counts are 44 public types and 542 snapshot entries including generated members, not 542 independent documentation findings or workflows. External coverage becomes 30 as above, not 30 wholly audited external files.
- Reconcile the stale 29-external-file statements in local status/coverage summaries against the accepted expansion when finalizing campaign records. Do not edit historical investigator coverage claims to pretend the companion was in the original packet.
- Record completion of editor/reviewer correction loops, both sample-example locations, and any remaining open findings before changing the checkpoint to complete. The current “editing, independent review, and final verification are in progress” status is appropriate; no final results are required now. The supplied coordinator state says A review and B editing are active; this review did not inspect their work.
- Record actual final gate outcomes, warnings/errors, scope and any skipped/unavailable check. Do not convert a planned build/test/snippet compile, the baseline docs-qa pass, or source-only snapshot reconciliation into completed verification. Keep any compiled-example claim scoped to examples actually compiled.
- Keep normative/source-policy distinctions intact after editor integration, particularly verified PRF, pre-creation credProtect, original allow-list validation, error catch boundaries, and missing credential IDs. Do not close WA-IMP items merely because their limitation documentation has been repaired.
- Preserve the explicit unverified status of WA-IMP-016 unless a separately authorized targeted runtime check supplies evidence. This review supplies none.
- Retain the hardware/platform/conformance exclusion even if final compilation and unit tests succeed. Preserve DOC-002 and the separate Core/discovery/SCP deferrals unless a later authorized task actually resolves them.

No source re-audit or in-flight diff review is requested by this report. After the two count/coverage clarifications, no additional required evidence correction was identified in the checkpoint as reviewed.
