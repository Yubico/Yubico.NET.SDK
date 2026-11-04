# CTAP2 required corrections and adjudication progression

Original reports/reviews remain intact except explicit leading errata. Follow-ups identify accepted final
hashes; source corrections are documentation-only. Implementation/hardware work remains deferred.

| Group | Required correction / evidence | Current status |
| --- | --- | --- |
| Framework D-08/C-008 | Actual canonical writer auto-sorts; insertion order is not wire defect. | Runtime inference rejected, comments corrected; pure probe and originals retained. |
| Framework A-014 | Default Guid serialized byte convention is host-independent; conditional reversal has inferred big-endian limitation. | IMP-025 retained; no big-endian execution or code fix. |
| A-REV-001 / COORD-001 | Restrict null semantics; missing required AAGUID empty; parsed absent encrypted fields present empty nullable memory. | correction-a.md / review-a-followup.md PASS;26 exact reviewed files integrated. |
| RB-R1 / authorized RB-O1 | Name authenticator implementation actor and caller UV/token prerequisites; distinguish forced-change legacy0x05 from permission0x09 errors. | correction-b.md / review-b-followup.md PASS;13 exact files integrated. |
| D-14 subclaim | Test actually invokes legacy0x05; its PinInvalid expectation agrees with original CTAP2.1. | Wrong-status inference withdrawn; other cleanup/state gaps retained. |
| RC-R1/R2 / authorized O1/O2 | Seed-65700 explicitly agrees with draft; retain genuine profile differences; correct AA/AB and BB/BC fixture patterns, byte-array and modeled-field wording. | correction-c.md / review-c-followup.md PASS;27 exact files integrated. |
| RD-R1 / authorized O1 | Single fixture does not assert remaining count trend; clarify credentials-for-one-RP enumeration. | correction-d.md / review-d-followup.md PASS;20 exact files integrated. |
| RE-R1..R4 / authorized O1/O2 | Passive Written field, signed range scoped to known size/keys, borrowed unknown slices and actual decode error families; precise input/cache/auth lifetime. | correction-e.md / review-e-core-followup.md PASS;30 core files integrated. Separate16 crypto paths passed review-e-crypto.md and remain unchanged. |
| RF-R1..R4 / authorized O1..O3 | NFC versus USB gate, fixed V2 and verify-PIN scope prerequisites, actual AuthenticationTagLength name, L2/L3b flow reconciliation; useful task/voice refinements. | correction-f-review.md / review-f-followup.md PASS;30 exact paths integrated, with documented project EOL normalization. |

Final integrated BF-R1–R3 and authorized BF-O1 passed boundary-review-final.md; correction-boundary.md
retains explicit new path ownership and source evidence. All source-content corrections are accepted.

Supplemental ledger IMP-026–048 accepted by coordinator; rejected/profile/decoder refinements are recorded
there without duplicated WA issue counts. Targeted nullable-array decoder-exit expansion found no further
instance beyond the two GetInfo fields in its checked scope; it is not whole-repository dataflow proof.

New validation hosts are isolated from normal module-name discovery using AuditCt001 identifiers.
Original probe project/script/hash bytes are retained as snapshots, current harness paths are explicit,
and the older Management audit-host naming collision remains a separate recorded follow-up.
