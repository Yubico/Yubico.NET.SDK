# Security Domain triage and deferred-work handoff

2026-11-04; Astra coordinator. Baseline `b73efe3813b429b4b8eb2a7e819a6ad7be699d47`.
Original observations and precise source anchors remain in [investigation](investigation.md) and
[source evidence](evidence/sources.md). These 13 overlapping candidate groups are not 13 confirmed bugs.

## Documentation decisions

| Candidate | Decision for fresh editing | Evidence / limit |
| --- | --- | --- |
| SD-C001 | Accepted: current contributor/test examples, correct links, scopes and present-tense explanations | Module-local signatures and cleanup. Replace obsolete copied implementations with useful current flow descriptions; avoid unverified Core logging recipes. |
| SD-C002 | Accepted: bounded destructive reset attempts, partial mutation, unauthenticated entry, no verified factory-state guarantee | Coordinator independently read complete reset/raw-send/reinit flow. Vendor SCP03 restoration statement does not prove every mixed-key firmware state. Do not turn silent exhaustion into intended success policy. SD-IMP-001/005. |
| SD-C003 | Accepted: deletion is a KID/KVN filter; SCP03 normalizes to KVN-only; document caller's nonzero version and last-key consequences | Coordinator independently read whole normalization/encoding helper. Empty normalized filter is an implementation edge, not an endorsed delete-all feature. SD-IMP-002. |
| SD-C004 | Accepted: distinguish first import replacing factory set, adding custom versions, explicit replacement; verify fresh authentication before deleting retained old keys | Living Yubico manual E1, with SCP03-specific applicability. Tests are unexecuted hardware evidence at this stage. |
| SD-C005 | Accepted: distinguish channel establishment, host authority and certificate loading/trust; explain roles without an incomplete SCP11a/c provisioning tutorial | Module test asserts SCP11b management rejection; E2 is an explicitly identified public-review draft, not final v1.3. Inherited semantics remain SD-DEP-001/002. |
| SD-C006 | Accepted: authored public families/member contracts, interface/concrete agreement, data shapes and selected exception wrapping | Coordinator read factory/initialization: selection precedes catch; only selected ConfigureProtocolAsync errors wrap with supplied parameters. Keep inherited guarantees within local evidence. |
| SD-C007 | Accepted: malformed-hex FormatException, CA-direct serial scope with source qualification, clear removes restriction, no unsupported SCP11c recipe | E2/E3 and local encoding/tests. Draft tag92/counter applicability remains SD-IMP-003; do not claim a proven YubiKey defect or final-profile conformance. |
| SD-C008 | Accepted: selected-SD raw GET/STORE scope, caller payload responsibility, nested recognition value result | Local APDU/parsing code. Integer validation question joins SD-IMP-006. Do not replace an unverified normative citation with a false conformance assertion. |
| SD-C009 | Accepted: assertion-based test commentary and current rationale; preserve useful comments | Method renaming, assertions and fixture behavior stay unchanged. SD-IMP-007/008 preserve actual deficiencies. |
| SD-C010 | Deferred engineering: SD-IMP-004; qualify relevant result/parser contracts where useful | Source-observed malformed parsing paths; no executed reproduction in investigation. |
| SD-C011 | Deferred engineering: SD-IMP-005; document current post-reset option limitation and fresh-session workaround | Coordinator confirmed both reinit callbacks omit supplied configuration/version. Effective inherited Core state remains SD-DEP-003. |
| SD-C012 | Deferred engineering: SD-IMP-006; distinguish required caller ranges from actual host enforcement | No automatic range/API redesign in a documentation pass. |
| SD-C013 | Deferred engineering: SD-IMP-008; avoid unsupported cleanup assurances | Sensitive-data classification and exact owned/borrowed lifetimes need scoped follow-up. No runtime/test cleanup changes. |

## Stable implementation/verification findings

All rows are **deferred-pass-2**; none is implemented or hardware verified by this cycle. Owner at return:
implementation coordinator, after re-baselining source and agreeing the desired contract. This is a
breaking release; backward API compatibility does not constrain the eventual decision.

| ID / provenance | Observed issue or question | Next action / missing evidence | Documentation reopening trigger |
| --- | --- | --- | --- |
| SD-IMP-001 / C002 | Reset can exhaust attempts or continue transmission failures and still reselect/return; no post-reset factory-state verification; empty list skips | Decide incomplete-result/error contract; exercise exhaustion, failures and empty listing with meaningful fakes; later authorized device/firmware/transport matrix | ResetAsync/BlockKeyAsync/TryGetResetParameters/raw send or vendor reset behavior changes → both Reset XML, README reset/cancellation, contributor/test reset flow, AI-DOCS-GUIDE example |
| SD-IMP-002 / C003 | `(SCP03 component, KVN=0)` passes initial validation then normalizes to empty DELETE filter | Decide validation after normalization; exact-wire tests for component/zero and wildcard combinations; establish relevant device outcome only in authorized hardware work | NormalizeDeletePolicy/EncodeDeleteFilter/DeleteKey behavior changes → both Delete XML, rotation/deletion guidance, lifecycle test explanations |
| SD-IMP-003 / C007 | Empty individual serial allowed; no tag92/counter support in allowlist writes; SCP11c final/vendor applicability unsettled | Obtain final/profile clause; decide entry-validation/counter support; meaningful wire and acceptance/rejection tests | Store/ClearAllowList validation/encoding/profile changes → both XML contracts, README role/allowlist flow, audit-repro and integration comments |
| SD-IMP-004 / C010 | Key-info parser ignores trailing component byte; CA parser lacks tag/pair/reference-length checks and raw listing escape hatch | Decide malformed versus future-valid response contract; add malformed-response cases | ListKeyInformation/GetCaIdentifiers parser, error or DTO shape changes → both XML surfaces, KeyInfo/CaIdentifier docs and parser test commentary |
| SD-IMP-005 / C011 | Reset reinitialization omits caller's configuration/version and clears explicit-version indicator | Preserve or intentionally redefine reset options; fake tests; Core effective state remains separately deferred | Reset/Initialize option retention changes → README firmware/reset, factory/reset XML, contributor initialization, affected shared architecture/migration version claims |
| SD-IMP-006 / C008/C012 | replaceKvn and GET DATA identifiers truncate to bytes; Generate has no local SCP11 KID check; KCV validation does not bind response version and permits extra data | Decide ranges/open-value/response policy against supported profile; test actual encoding and response mismatch | Import/GetData/Generate/KCV validation changes → caller range/exception docs, rotation guidance and corresponding test descriptions |
| SD-IMP-007 / C009 | Some tests assert broad exceptions; generated-key authentication comment exceeds callback; convenience disposal is not observed; fixture is not a full protocol verifier | Strengthen intended behavioral assertions and fixture coverage in a scoped test follow-up | Listed test setups/assertions/fixture checks change → test descriptions/README coverage; no comment-only correction closes this engineering item |
| SD-IMP-008 / C013 | Local temporary/fixture/test sensitive resource cleanup incomplete; inherited ownership unanswered | Classify each buffer/object, establish ownership via SD-DEP-002, then scoped cleanup and meaningful lifetime tests | SecurityDomainKeyMaterial, fake connection, ScpCertificates or integration cleanup changes → security notes, contributor ownership examples and affected local comments |

## Core dependencies — record only

Core is the final target, with per-applet boundary reviews then. No Core implementation or document
investigation/editing is assigned here. Names below are known from module references, not inspected Core
implementations. Keep existing Core/OATH/Management findings' original provenance when reconciling later.

| ID | Question / module anchor | Later documentation dependency |
| --- | --- | --- |
| SD-DEP-001 | ApplicationSession IsAuthenticated and SCP preparation/configuration error semantics; SD InitializeAsync | Auth/creation/exception contracts and migration subsection |
| SD-DEP-002 | StaticKeys, Scp03/11KeyParameters, ECPrivateKey/ECPublicKey and certificate clone/borrow/ownership rules; SD `_scpKeyParams` and import/export/test creation | Ownership/security/setup examples, private-import explanation, SD-IMP-008 |
| SD-DEP-003 | ApplicationSession/Feature effective firmware/configuration after reset; local omitted arguments and explicit-version flag | Feature/reset/version contracts, SD-IMP-005 |
| SD-DEP-004 | Session/connection lease, cancellation, response-memory lifetime and APDU Le semantics; factory/OwnConnection/GetData call sites | Factory/connection and raw-data contracts |
| SD-DEP-005 | Current global logging configuration and payload logging policy; obsolete loggerFactory sample | Contributor diagnostics and sensitive-logging advice |

## Editing/review packet

Fresh Sol editor owns only documentation/comments under `src/SecurityDomain/`, plus exact accepted SD
sections in `docs/AI-DOCS-GUIDE.md`, `docs/migration/v1-to-v2.md`, and the `securitydomain-exception` scalar
in `docs/migration/v1-to-v2-map.yml`. Preserve the pre-existing unrelated YAML parse defect DOC-002.
No other-module/Core edits or boundary review. No executable C#/assertion/snapshot/config changes.
Use patch, preserve EOL, and leave formatter to coordinator. Save `edit-report.md` with actual paths,
candidate mapping, example classification and final content hashes. Independent reviewer then owns only
`review.md` and focused citation evidence, with required corrections re-reviewed. Scope/voice follow plan.
