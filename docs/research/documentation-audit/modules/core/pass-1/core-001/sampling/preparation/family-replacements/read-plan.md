# Core family sampling replacements — no defect expansion

**Proposed reading only.** All original 983 selections, prior additions/replacements, source-disposition ledgers and coordinator verdicts remain byte-preserved.

Astra has read all **802 original Core-family samples**. The remaining 181 original selected units belong to boundary owners and remain unread according to this handoff. Added/replacement reads are separate.
C: all 62 source contents are supported pending final mapping, with original RFC/OID/vector checks retained in Astra’s verdict record. No new C exclusion or extra source/provider/runtime acceptance is inferred.

| Owner/role | Original population | Working population | Original quota / working minimum | Valid original after specific withholding | Replacements |
| --- | ---: | ---: | ---: | ---: | ---: |
| B5 tests-and-helpers | 503 | 501 | 51 / 51 | 49 | 2 |
| B6 XML-contracts | 104 | 104 | 11 / 11 | 6 | 5 |
| B6 tests-and-helpers | 128 | 128 | 13 / 13 | 12 | 1 |

## Coordinator reading order
[original-units.md](original-units.md) supplies all eight complete pinned units; exact source/range hashes and pending verdicts are in `replacement-selection.json`.
- `core-001:unit:0052beaaca7e3aab` — `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/RawOtpHidSessionTests.cs:124–126`; B5 tests-and-helpers; rank 52; **UNREAD / zero credit**.
- `core-001:unit:891bea911fc43528` — `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Otp/Hid/OtpHidProtocolTests.cs:69–78`; B5 tests-and-helpers; rank 53; **UNREAD / zero credit**.
- `core-001:unit:f96355d3db0dfcd7` — `src/Core/src/Protocols/SmartCard/Scp/ScpProcessor.cs:34–39`; B6 XML-contracts; rank 12; **UNREAD / zero credit**.
- `core-001:unit:3ad8b9f956a4e672` — `src/Core/src/Protocols/SmartCard/Apdu/SWConstants.cs:52–55`; B6 XML-contracts; rank 13; **UNREAD / zero credit** — same SWConstants path, inherited source-gap qualification still requires exact review.
- `core-001:unit:aa9d6d49701b178d` — `src/Core/src/Protocols/SmartCard/Apdu/SWConstants.cs:142–145`; B6 XML-contracts; rank 14; **UNREAD / zero credit** — same SWConstants path, inherited source-gap qualification still requires exact review.
- `core-001:unit:a3d37dc1aed22b2f` — `src/Core/src/Protocols/SmartCard/Apdu/SWConstants.cs:151–154`; B6 XML-contracts; rank 15; **UNREAD / zero credit** — same SWConstants path, inherited source-gap qualification still requires exact review.
- `core-001:unit:f39183b2d438d21d` — `src/Core/src/Protocols/SmartCard/Apdu/SWConstants.cs:71–74`; B6 XML-contracts; rank 16; **UNREAD / zero credit** — same SWConstants path, inherited source-gap qualification still requires exact review.
- `core-001:unit:edaef2dcf282acc6` — `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreScpAuditReproTests.cs:171–186`; B6 tests-and-helpers; rank 14; **UNREAD / zero credit**.

## Separate exclusions and qualifications
B5 c7319c1c1182a4be and 5bcf834655fef594 are specific pure-preamble exclusions, not documentation misses. Their role population is reduced only by those two adjudicated IDs (503 → 501); its 10% minimum remains 51, requiring two replacements.
B6 five exact SWConstants sample IDs retain SOURCE-QUALIFIED-NORMATIVE-GAP. Their declaration meanings were read; exact original ISO clauses/revision remain unavailable. They have zero final clean credit, and no unknown clause is declared verified.
B6 59c33b8c7c8f294c retains REFERENCE-DATA-QUALIFIED: the complete CAVS response file was read as reference data; original NIST archive provenance gap remains and no crypto output was recomputed. It has zero final clean-contract credit.
B6 broad XML/test role populations stay **104 / 128**, with original quotas **11 / 13**. Five XML plus one same-role test unit are proposed replacements for six withheld sample slots, without shrinking the entire SWConstants class or broad denominator. These are qualification replacements, not new defect/category expansion; clean credit still requires actual reading and source acceptance.
Replacement selection is deterministic in the original fixed-seed rank, skipping all prior selected IDs and exact known accepted changed spans. No unrelated code-only/private/internal/test exclusions or other-stratum reductions are introduced.
A new same-class replacement may retain its own original-source gap. Same-role selection is not automatic content/source acceptance; Astra must read and adjudicate each exact claim. Current source mappings and the final canonical denominator remain separate pending gates.
Known accepted current-product records include the integrated B4 two-guide patch. Final combined validation/sampling reconciliation remains pending; no source-content verdict is supplied by this preparer.
Records-only rank/hash/index work. No source audit, product edits, SDK/build/test/native/device/formatter/crypto execution, Git mutation or subagent. Finish Core Pass 1 publication, then stop.
