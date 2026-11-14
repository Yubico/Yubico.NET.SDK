# Pass 2 applet master: integration review inputs

**2026-11-13. Integration editor receipt; independent ledger-level review and a targeted semantic/source
sample recorded, not full semantic certification. R1's bounded correction awaits reviewer closure.**

The active review entry is [pass-2-applet-master.md](pass-2-applet-master.md). Its tracker records
**176 local review cards plus three displayed referrals**, not unique confirmed bugs. All user
review is unstarted; all implementation is deferred; all proposed code is unapplied and uncompiled.
This companion records inputs and limits, not a second backlog or acceptance ledger.

The [independent bounded review](pass-2-applet-master-review.md) found no missing known finding IDs at
ledger level and requested R1's impact/severity correction. The [correction receipt](pass-2-applet-master-correction-r1.md)
records that edit without altering the original review. Remaining per-applet review groups are lazy
human-review followups: load the selected applet's unsampled facets when needed, not all nine deep reviews
as a new completion gate. Ledger presence and the targeted sample do not certify every card semantically.

## Identity and bounded checks

- C# baseline: `6ce355468047e8004fd51cf1425808f6e8242ed3`.
- Python comparator: `/home/rupicola/Code/y/yubikey-manager`,
  `4ca60f706af930459138d8dc0f0f953480e1c7a4`.
- Editorial authority: [approved voice](records/style.md). Section authors' source-window receipts
  are retained as historical checks; integration did not re-run their full BEFORE excerpt matching.
- Static integration checks cover every master/companion Markdown link target and heading anchor,
  fence pairing, named card-ID uniqueness, per-applet heading counts and preservation of all fenced
  code/procedure blocks. No URL reachability, compiler, SDK test, native, crypto-provider or device
  result is implied. The master currently contains only local Markdown navigation links.
- Original product-ID sets are compared with the named triage/findings inputs, using the section
  ledgers for compressed ranges and intentional gaps. CTAP-IMP-017 stays rejected; OATH IMP-001–014
  stay resolved. Original Core boundary groups have local ledger entries: 6/12/3/23/8/9/7/8/6 in
  applet order. No Core candidate inventory was expanded or edited.

## Frozen inherited identity inputs

Only identity fields/row counts from these existing registers are needed for integration. They contain
**150 routed occurrences**, with overlap between applets; this is neither a distinct-ID nor bug total.
The master retains local facet ledgers and a shared canonical referral index. Identity presence does
not prove that every original facet and reopening condition is adequately explained.

| Applet | Inherited register | Routed rows | Local review cards / extra displays |
| --- | --- | ---: | --- |
| OATH | [Oath inherited dispositions](modules/core/pass-1/core-001/boundaries/Oath/inherited-dispositions.json) | 13 | 8 / 0 |
| WebAuthn | [WebAuthn inherited dispositions](modules/core/pass-1/core-001/boundaries/WebAuthn/inherited-dispositions.json) | 31 | 23 / 1 CTAP referral |
| Management | [Management inherited dispositions](modules/core/pass-1/core-001/boundaries/Management/inherited-dispositions.json) | 9 | 19 / 2 Core referrals |
| CTAP2 | [Ctap2 inherited dispositions](modules/core/pass-1/core-001/boundaries/Ctap2/inherited-dispositions.json) | 26 | 50 / 0 |
| Security Domain | [SecurityDomain inherited dispositions](modules/core/pass-1/core-001/boundaries/SecurityDomain/inherited-dispositions.json) | 16 | 14 / 0 |
| YubiOTP | [YubiOtp inherited dispositions](modules/core/pass-1/core-001/boundaries/YubiOtp/inherited-dispositions.json) | 14 | 14 / 0 |
| YubiHSM Auth | [YubiHsm inherited dispositions](modules/core/pass-1/core-001/boundaries/YubiHsm/inherited-dispositions.json) | 15 | 13 / 0 |
| OpenPGP | [OpenPgp inherited dispositions](modules/core/pass-1/core-001/boundaries/OpenPgp/inherited-dispositions.json) | 15 | 18 / 0 |
| PIV | [Piv inherited dispositions](modules/core/pass-1/core-001/boundaries/Piv/inherited-dispositions.json) | 11 | 17 / 0 |

The [Windows successor](integrations/windows-upstream-20261113/deferred-findings-successor.md) and
the [frozen Core deferred handoff](modules/core/pass-1/core-001/deferred-findings.md) remain the authority
for scoped supersession and shared owners. SmartCard-only applets do not inherit the old Windows HID
mechanism as a new bug; independent reports, workflows, provider/native/profile and physical limits
are not closed by software-owner delivery.

## Questions for the independent reviewer

These integration questions remain scope guidance. The independent report answers them at ledger level
and for its named sample; unsampled semantic work is explicitly retained, not a missing-known-ID finding.

1. **Facet completeness, not just ID presence.** WebAuthn summarizes 31 inherited keys, CTAP2 summarizes
   26, and several other applets refer cross-applet keys collectively. Their original IDs occur in the
   master at canonical owners or exclusions, but the integration scan cannot prove every compound
   facet or exact reopening map survives. Compare those registers with the selected local ledger and
   shared index, one applet at a time. Return any missing facet by exact original key; do not invent closure.
2. **Required fields can be in the local map.** OATH, Management, Security Domain and OpenPGP supply
   some severity/certainty/no-brainer judgments in their per-applet map rather than repeating them in
   each card. A body-only keyword scan therefore reports false missing-field warnings. Review map and
   card together for essential context, explicit bug-versus-qualification status, Python precedent or
   its limitation, source identity, BEFORE/AFTER applicability and meaningful acceptance. No required
   field is declared missing solely from that scan; editorial sufficiency remains an independent judgment.
3. **Card totals are nonadditive.** Check 176 local / 179 displayed, including qualification and shared
   workflow cards. WebAuthn's native User display is CTAP-owned; Management's two metadata displays are
   Core-owned. Its support note is not another card. PIV split groups and OpenPGP's intentional numeric
   gaps are not omissions or additional bugs. SAMPLE-A004 denotes separate PIV/OpenPGP caller evidence.
4. **Code plausibility is not compilation.** Unchanged author-checked BEFORE excerpts were not
   re-authored. AFTER blocks, including literal C#, remain proposed and uncompiled. Schematic names
   are not APIs. If the reviewer changes any excerpt, recheck that exact window rather than rerunning
   all applets or claiming a test/build result.
5. **Scope and evidence.** Preserve OATH's 14 IMP resolutions, rejected CTAP-IMP-017, the retracted HSM
   retry-message subclaim, resolved User/descriptor prose, resolved SD production certificate partial
   cleanup and PIV's exact post-PR745 fixes/response-only rollback. The master must retain the distinct
   remaining policy, tool, secret-owner, original-failure and physical/profile questions without treating
   historical successes, skipped paths or managed tests as broader proof.

## Section edit manifest

- Introduction/overview/conventions: integrated nine-app status, review entry/navigation, evidence
  receipt limits and explicit unstarted/deferred/uncompiled state. Existing original-group inventory
  and authority links are retained.
- Final reconciliation: card-count/state tracker, narrow canonical shared referral index, legacy alias
  routing, per-applet acceptance workflow and independent-review handoff replace the unfinished placeholder.
- Applet narrative: only obsolete dispatch labels, incomplete-draft references and author-time
  preservation sentences are mechanically updated. OATH's referral instruction now points to the
  shared index. No card content, snippets, local coverage dispositions or frozen records are changed.
- Companion: this file only. No implementation, build/test/hardware operation, commit, push or
  subagent dispatch is part of integration. The parent coordinator owns the next independent review.

The manifest above describes the original integration edit, not R1's later correction. R1 changes only
CTAP-IMP-007's impact/severity prose and the master/companion review-status and scope wording; all other
card bodies, proposals and frozen records remain unchanged. The parent owns bounded reviewer closure.
