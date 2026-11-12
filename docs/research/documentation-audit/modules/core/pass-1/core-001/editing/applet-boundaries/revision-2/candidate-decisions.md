# R2 successor: CORE-SD-X008 and related X004

This bounded record supersedes the first editor's X008 statement that only the c-test comment needed
correction, and the two affected files' blanket retain reasons. It does not rewrite
[the original decisions](../decisions-and-referrals.md), source ledgers or
[the independent review](../review.md).

| Candidate | Successor documentation decision | Engineering/policy/evidence status |
| --- | --- | --- |
| CORE-SD-X008 | Keep the accepted c-test oracle correction. Additionally edit the two SCP03 finally comments to describe an attempted default-key precondition and no asserted restored contents; rotation replaces the earlier custom set | Existing `security-domain-001:SD-IMP-007/008` test ownership, tokens, cleanup and positive-control debt remain held. No assertion, lifecycle, token or helper change and no new finding |
| CORE-SD-X004, related facet | Apply the same reset request-versus-restoration distinction at these actual callers, retaining why later tests/suites need public default-key authentication | Existing SD-IMP-001/005, SD-DEP-004 and Core B5-C006 reset/admission/state/recovery facets remain held. Track teardown is another attempt, not a restored-state oracle |

The two path decisions transition **RETAIN → EDIT** in
[R2-path-decisions.json](R2-path-decisions.json). Their full current source hashes, before/reviewed
hashes, original unit IDs and exact changed ranges are also in the current 233-path ledger. The other
231 path identities are unchanged from the reviewed ledger, including all 37 previously edited paths.

All 33 groups keep their original identities and documentation/engineering/policy/qualification
separation. Other accepted content, the 107 example bodies, outside-owner referrals and original
evidence states are unchanged. The review correction adds no new engineering defect or runtime remedy.
