# Coordinator clean-unit sampling

2026-11-04; Astra. Population: 12 eligible investigator-clean semantic units in `coverage.json`.
Required: max(three, ceil(10% × 12)) = three. Selected three (25%), one per required category.
These are bounded semantic samples, not an independent whole-module or Core audit.

| Unit | Read and checked | Result / limits |
| --- | --- | --- |
| SD-CL01 — API | Entire CaIdentifierType enum/XML; module GetCaIdentifiersAsync validation/group dispatch and local tag names | PASS. None=0, Kloc=1, Klcc=2 and flags semantics agree; None has no group and caller method rejects it. KLOC then KLCC dispatch agrees. No promise that None is a valid retrieval request, or that enum validates firmware applicability. |
| SD-CL05 — narrative | Entire PACKAGE_README; module project metadata/direct Core project reference; published module method inventory and campaign scope | PASS. Package purpose corresponds to available operations; own-application reset scope is clear. Package links use explicit v2 branch and existing module destination; canonical alpha banner retained. Installation commands were inspected, not executed; no feed availability, cross-applet interoperability or hardware conformance inferred. |
| SD-CL09 — test explanation | Entire Scp03HandshakeFakeConnectionTests source, all four inputs/expected outputs and actual AesCmac assertion; independently fetched original RFC4493.txt | PASS. Key/message prefixes and MACs exactly match RFC 4493 §4. The comment scopes the independent check to AES-128-CMAC, not all fixture handshake behavior. This sample does not execute the tests or establish SCP03 conformance. |

No material miss was found in the selected units; category expansion is not required. These clean
files are checked at baseline content, with identities retained in `validation/sampled-files.json`;
final reconciliation must verify their unchanged hashes or reopen the affected sample.

## Post-edit sampling reconciliation

The editor changed the package synopsis and overlapping exception/certificate contract passages.
Conservatively exclude SD-CL02, SD-CL03 and SD-CL05 from the final clean population; their new text is
covered by authored-content review. Nine eligible clean units remain. The earlier SD-CL05 check is
retained as a supplemental pre-edit sample, not counted toward final clean acceptance.

Replace it with **SD-CL07**, the complete default SCP03 authentication fragment: parameter `using`
encloses session `await using`; typed caller-selected `IYubiKey device`; ListKeyInformation result stays
inside that scope. The block is byte-identical to baseline and compiled in the real consumer context.
This validates the example's scope/binding, not deep Core ownership or hardware authorization.

Final counted set: **SD-CL01, SD-CL07, SD-CL09 — 3/9 (33.3%)**, one per required category. Minimum remains
three. No material semantic miss prompted this adjustment; edited-unit eligibility was the reason.
`validation/final-sampling.json` records final population/exclusions and exact baseline/current fragment
identity. Enum and CMAC-vector file hashes must also remain unchanged at closeout.

## Independent original reference

Fetched `https://www.rfc-editor.org/rfc/rfc4493.txt` through webfetch on 2026-11-04. Title **The AES-CMAC
Algorithm**, June 2006, **Informational**; status explicitly says it does not specify an Internet standard.
Section 4 defines key `2b7e151628aed2a6abf7158809cf4f3c` and the four input-prefix lengths 0, 16, 40, 64
with outputs `bb1d6929e95937287fa37d129b756746`, `070a16b46b4d4144f79bdd9dd04a287c`,
`dfa66747de9ae63030ca32611497c827`, `51f0bebf7e3b9d92fc49741779363cfe`. Compared each against source,
including all 64 message bytes and the slicing/hex equality assertion. No claimed final-GP evidence
depends on this reference.
