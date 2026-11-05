# Coordinator clean-unit sampling

**Final result: 64/385 eligible regions, minimum 39, across 18 source/shared paths.** Categories are
25 API, 10 example, 23 internal/test and six narrative units. The earlier selection checkpoints below
preserve progression; sampling-final.json is the authoritative current identity/population record.

Astra independently read **51 unchanged semantic regions** across13 paths, with whole-file context
for the module files and bounded PIV sections for shared guides. Current categories:25 API,10 example,
10 internal/test and6 narrative units. The post-editor provisional denominator is385 (minimum39);
final edited-overlap/hash reconciliation follows required review corrections and formatting.
Exact selected region IDs, source hashes and bodies are retained in sampling-accepted.json.

| Sample family | Units selected | Independent check |
| --- | ---: | --- |
| Slot | 8 | Full enum read; four standard roles, retired endpoints andF9 qualification agree with original NIST Part1 namespaces and Yubico slot/reset descriptions. Homogeneous intermediate retired entries were read as context rather than counted to fill the sample quota. |
| DataObject | 6 | Capability/CHUID/authentication/history IDs match SP800-73-4 Part1 AppendixA Table7; F9/Admin extension identifiers match pinned vendor implementation. Key references remain distinct from certificate-object IDs. |
| TouchPolicy | 5 | Full enum read; Never/Always/Cached values and15-second vendor cache statement checked against original technical-manual policy section. This is specification/source checking, not a touch ceremony. |
| UserVerification | 4 | Full enum plus complete BioProtocol source read; empty/tag03/tag02 request shapes and temporary-PIN ownership match actual branches. No Bio-device qualification is inferred. |
| IYubiKeyExtensions | 2 | Full factory read; options precede cancellation, SmartCard resolution and OwnConnection occur before return. Caller disposal follows documented ownership; inherited Core internals remain deferred. |
| DeviceInfoResult | 3 | Success, retry-only and failure factories assign the documented nullable fields; retry-only explicitly permits missing DeviceInfo. |
| SigningResult | 4 | Success/signature/timing/factory semantics checked with full result class and actual Signing helper. Transport/helper success remains distinct from signature validity; PivTool framing limitations are documented separately. |
| VerificationResult | 3 | Valid/Invalid factories distinguish completed verification from validity; IsValid is meaningful only on Success. Whole result class checked. |
| PinOnlyProtocolTests | 7 | All seven method bodies/assertions read with full production PinOnlyProtocol context: absent/wrong-shape/flags/salt and actual working-copy clearing/extracted-key distinction. No live recovery is attributed to these fakes. |
| KeyProtocolTests | 3 | All helper tests and real import/appending context read: complete expected concatenation, previous-array zeroing and both chained intermediates are actually asserted. No blanket all-buffer cleanup claim inferred. |
| Package and shared guide sections | 6 | Whole package page, root PIV row and both PIV-relevant highlights regions checked against current package/API/task context. Existing macOS-only deeper Native AOT evidence remains bounded; no qualification upgrade. |

No additional material miss was found in these selections. Existing accepted C001–011 corrections
and deferred implementation groups remain separate from clean sample outcomes. Every selected file
matched the recorded post-745 baseline bytes at selection. Final review may remove a sample if a
later edit overlaps it; replace/expand selections as necessary to retain required category coverage.

## Expansion after independent review

PIV-REV-003 found residual test-oracle overclaims in two already edited crypto-test files. Astra read
the complete355-line SessionConsistencyTests and104-line SlotMetadataExtensionsTests, selecting ten
additional unchanged internal/test regions. They cover MATCH capability versus enrollment, real retry
metadata, unknown-algorithm retention and extraction refusal, explicit wire bytes, malformed-metadata
fallback refusal, physical-default versus compatibility override, and three mismatched host-key extractors.
Each assertion/explanatory scope was checked; fake traffic/state evidence stays distinct from live
hardware qualification. No further material miss was found in this expansion.

Exact identities are in sampling-expansion.json. The combined selection is now **61 regions**:
25 API,10 example,20 internal/test and6 narrative, across15 source/shared paths. Final eligibility and
accepted-region hashes still require reconciliation after correction re-review and formatting.

## Final internal-prose expansion

Final candidate closure identified the inaccurate RSA re-wrap probability rationale. Three further
unchanged internal explanatory units were read in full context: KeyReference, KeyTypeEncoding and
TypedDataObjectProtocol. Their reference/algorithm distinctions and typed-object delegation match
the implementation. The comment-free IBackend/SmartCardBackend contexts were also read but are not
counted as extra prose samples. No new material miss was found.

`sampling-internal-expansion.json` binds those three identities. Final reconciliation passed all
64 selected identities, all 385 eligible regions and the 39-region minimum. The corrected late files
were already candidate-associated, so they add no clean denominator overlap. Original review misses,
their corrections and category expansions remain visible.
