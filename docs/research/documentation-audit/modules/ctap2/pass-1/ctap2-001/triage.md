# CTAP2 triage and second-sweep dependency ledger

## Authority and progression

Coordinator: Astra. Baseline `5b09a9b6`; documentation-only Pass 1. All A–G investigations received.
The 103 overlapping candidates (A16/B13/C15/D17/E19/F12/G11) are inputs, not 103 independent bugs.
Documentation corrections below are accepted for editing, not yet independently reviewed. All SDK
implementation, assertion/executable-sample changes and device qualification remain deferred.

## Accepted documentation groups

| ID | Candidate provenance | Accepted correction / editing owners |
| --- | --- | --- |
| CTAP-DOC-001 | A-003/004/005/015, G-010 | Actual session/factory/info contracts, supported versus advertised/configured/authorized/enforced conditions, firmware snapshot/override bounds, direct versus owned connections and scoped alternatives. A API; F guides; G boundary. |
| CTAP-DOC-002 | A-001/002/009/010, G-009 | Reset command receipt versus interaction clocks; product/revision conditions and side effects; presence notifications versus device/ceremony success; cooperative cancellation and exact error provenance. A API/backends; F journeys; B/D/E dependent state. |
| CTAP-DOC-003 | A-007/008/016 | Raw exchange and CBOR responsibility, buffer ownership, copied inputs/deferred callbacks, exact current-value null parsing, limits of canonical-mode validation. A; dependent callers B/C/D/E/F. |
| CTAP-DOC-004 | B-01/02/08 | V1 SHA256 versus V2 separate HKDF derivations, zero PIN padding versus no AES padding, full crypto input/output lengths and ownership, ClientPin's supplied-protocol disposal, bounded cleanup. B API; F/E/G related prose. |
| CTAP-DOC-005 | B-03/05/09, G-004 | Caller-selected PIN protocol versus legacy command fallback; permissions/RP scope, built-in UV interaction/capabilities and persistent pcmr-only conditions. B; D/E/G/F dependencies. |
| CTAP-DOC-006 | B-04/06/10, D-07/14 | Unicode/code-point/UTF-8 PIN obligations versus actual byte checks; retries absent/unknown/defaulted values; temporary versus exhausted PIN block and UV recovery; force-change policy meaning. B models; D config; A errors/helpers; F/G. |
| CTAP-DOC-007 | B-13, G-003 | Omit uv key when using pinUvAuthParam, distinguish built-in UV from PIN authorization and actual returned verification. C options; B token API; F Markdown/XML and executable-example caveats; G final comparison. |
| CTAP-DOC-008 | G-001/002 | Correct assertion User id versus identifying fields and version-specific descriptor omission; preserve WA-XREF-001/WA-IMP-010 provenance. C response XML; F/G related claims. |
| CTAP-DOC-009 | D-05/07/09/10/11/12/16 | Administrative helper/session/protocol/token ownership, eager stateful enumeration/totals/hash routing, delete/update meaning, config toggle/fresh state and minimum-PIN policy. D; A/B/E/F/G dependencies. |
| CTAP-DOC-010 | D-01/02/03/04/15 | Candid standard biometric request/response/status limitations, begin-ID retention, decoded count versus verified completion, actual notification and test proof. D API/comments; F guides/examples. Do not recast wrong wire values as valid standard behavior. |
| CTAP-DOC-011 | D-06/08/17, A-009 | Correct exact specification references/canonical ordering/status provenance and constant explanations, retaining current executable values and deferred defects. D/A; C/E dependencies. |
| CTAP-DOC-012 | A-011/012/013, B-10/11/12, D-13/14/15/16 | Test commentary follows actual assertions, setup side effects, known-PIN normalization, partial restoration, mock scope and cleanup; retain real unfinished work instead of phase/placeholder narration. A/B/D, later C/E; F test guides. |
| CTAP-DOC-013 | G-005/006/007/008; E pending | CTAP hmac-secret/WebAuthn PRF roles, ignored credProtect enforcement, actual largeBlob route versus standard direct alternative, minPinLength reporting rather than policy-setting. E after its full investigation; F/G. |
| CTAP-DOC-014 | All cross-guide dependencies; F pending | Complete reader tasks, prerequisites, meaningful examples/resource lifetimes, actual FidoTool behavior, exact current names/links and honest limitations. F with technical owners. |
| CTAP-DOC-015 | G-001..011 | Full CTAP/WebAuthn claim reconciliation, preserving legitimate higher-level policy/orchestration and original WA findings. G owns map; F owns shared narrative by default; WebAuthn source ownership assigned explicitly if needed. |
| CTAP-DOC-016 | C-001..010/012..014 | Credential/attestation/COSE binary and ownership contracts, parse versus verify, precise algorithms/none attestation, versioned unsigned outputs, actual experimental ARKG profile and PIN-reader units. C; A/F/G/E dependencies. |
| CTAP-DOC-017 | E-01..19 | Full extension/storage/metadata/previewSign contract reconciliation: real output layers, incomplete convenience decoders/owners, stateful shared storage, experimental profile/verification limits and exact test evidence. E; C/B/D/A/F/G dependencies. |
| CTAP-DOC-018 | F-01..12 | Complete protocol demonstrations versus RP integration, non-overlapping session examples, actual CLI subset/selection/help/cancellation/reset-target/secret-input behavior, real current guide APIs, mechanical links and narrow shared references. F, including explicit WebAuthn README requirement qualification and current campaign handoffs; no WebAuthn runtime edit. |

## Deferred implementation / verification groups

All items: code not changed, hardware not run, Pass-1 documentation review pending. Missing preflight
checks are labeled decisions when caller-versus-helper enforcement is not settled; not every raw-API
caller obligation implies a required SDK feature. Report-local dependencies retain exact file/member paths.

| ID | Observed condition / provenance | Second-sweep action and documentation re-review trigger |
| --- | --- | --- |
| CTAP-IMP-001 | A-006 and late COORD-001: GetInfo accepts missing required Versions/AAGUID, lacks explicit length/range/complete-envelope checks, and converts absent encrypted byte arrays to present empty nullable memory. | Settle decoder strictness and absence semantics preserving valid unknowns; verify required/optional and malformed inputs. Reopen AuthenticatorInfo XML, encrypted-field HasValue/capability claims, minimal-fixture comments, initialization/support/raw guidance and WebAuthn metadata assumptions. |
| CTAP-IMP-002 | A-003/004: initialization accepts default/sentinel or unsupported version advertisements; detected SmartCard gate differs from effective override. | Decide version/capability/sentinel/gate policy before implementation. Reopen factory/transport/firmware/support matrices in CTAP and WebAuthn. |
| CTAP-IMP-003 | A-007: typed size/depth/device max enforcement absent; raw caller boundary needs policy decision. | Decide typed versus raw responsibilities and meaningful boundary tests. Reopen builder/raw/session/credential request contracts and client orchestration. |
| CTAP-IMP-004 | A-008/B-08: builder retains secret-bearing clones; generic request/response ownership lacks end-to-end cleanup. | Design explicit byte lifetime/cleanup without assuming read-only views are owners. Reopen builder/raw API, PIN/admin/extension consumers and WA-IMP-013 dependencies. |
| CTAP-IMP-005 | B-08: failed decryption outputs, V1 full MAC stack buffer and post-ECDH allocation failure leave specific clearing gaps. | Verify exceptional paths and scoped clearing. Reopen protocol/helper XML, security notes and hmac-secret/PRF callers; no observed exploit/provider erasure claim. |
| CTAP-IMP-006 | B-04: PIN validation checks only 4–63 bytes, not UTF-8/NFC/code points/device minimum/final NUL. | Decide input-layer versus SDK enforcement without unsafe silent normalization; add meaningful byte/Unicode/state tests. Reopen PIN params/errors, prompts and both layer guides. |
| CTAP-IMP-007 | B-07: peer key agreement checks coordinates but not required kty/crv/alg labels; tests omit alg. | Enforce security-critical key structure and verify actual exception boundaries. Reopen helper/Encapsulate/COSE/forward-compatibility and hmac-secret/PRF claims together. |
| CTAP-IMP-008 | B-06/11: retry parsers collapse absent state/count to false/zero, accept unsupported UV power-cycle field and signed counts. | Decide presence/unknown semantics and strict response contract. Reopen retry models, parser/tests, blocked-state guidance and WebAuthn prompt mappings. |
| CTAP-IMP-009 | B-11: decrypted PIN-token length/response completion insufficiently validated; fake tests do not establish wire/crypto interoperability. | Validate protocol-specific response shapes and meaningful negative/vector tests. Reopen token returns, protocol lengths and dependent consumers. |
| CTAP-IMP-010 | B-03/05/09: capability/permission/RP/pcmr combination preflight is incomplete or caller-managed. | Decide enforcement and persistent-token representation; do not assume every unsupported combination needs new SDK policy. Reopen token/enum/admin/encrypted-metadata and WebAuthn negotiation claims. |
| CTAP-IMP-011 | B-13/G-003: request encoder and executable FidoTool can combine uv key with pinUvAuthParam. | Fix selected validation/sample paths with original-wire evidence; preserve distinction from WebAuthn's already-correct omission. Reopen options/PIN task/sample caveats and boundary map. |
| CTAP-IMP-012 | D-01: standard bio subcommand values, sensor request, modality authentication prefix and timeout placement mismatch §6.7. | Reconcile standard and separately evidenced preview wire formats; test exact bytes/transcripts. Reopen all bio API/constants/builders, simulated tests and sample journeys. |
| CTAP-IMP-013 | D-02: bio enumeration expects flat fields instead of template array and converts unsupported InvalidCommand to empty. | Correct schema and precise no-enrollment/error semantics; verify real fixtures. Reopen enumeration/template models, inventory guidance and tests. |
| CTAP-IMP-014 | D-03: missing sample fields default to completion; next response may lose begin template ID; other required fields default silently. | Decide operation-aware decoder/result identity contract. Reopen sample/sensor/model completion XML, enrollment loops and failure/continuation tests. |
| CTAP-IMP-015 | D-04: bio enum values/vendor-field claims conflict with standard or lack original vendor proof. | Resolve original definitions, then change code/API only in Pass 2. Reopen enum/model/XML/fixtures and product capability guidance. |
| CTAP-IMP-016 | D-05/06/07: config unused constants and omitted false/empty fields; capability preflight/expressiveness need decisions. | Preserve correct active first-three subcommands; resolve unused vendor codes and desired request expressiveness. Reopen config/minPIN/RP-list/toggle guidance and E extension claims. |
| CTAP-IMP-017 | **Rejected as a runtime defect.** D-08 inferred wire order from insertion order; the actual Ctap2Canonical writer sorts output. | Retain candidate provenance and [framework evidence](framework-adjudication.md). Correct false alphabetical/numeric-canonical comments only (C-008/D-08); no encoder limitation or runtime fix justified by insertion order. |
| CTAP-IMP-018 | D-09: CredentialManagement.Dispose marks a field no operation reads. | Decide and verify disposed-helper contract; no automatic token/session ownership transfer. Reopen helper Dispose/lifetime and all calling examples. |
| CTAP-IMP-019 | D-03/10/11: administrative required counts/totals/hash validation and whole-sequence enforcement are incomplete or unsettled. | Separate decoder validity from sequence/caller policy; test first/next/error cases. Reopen result totals/count estimates/hash routing/sequence guidance. |
| CTAP-IMP-020 | A-011/B-12/D-14: nominal read-only helpers default to PIN normalization, which can mutate or consume retries before callback. | Separate read-only setup from known-state PIN workflows and verify actual test lanes/traits. Reopen all helper/smoke/test-guide claims; no unattended hardware confirmation in Pass 1. |
| CTAP-IMP-021 | A-012/B-12/D-14/16: shared and owned tests discard secrets or restore only on success, including partial PIN/config recovery. | Fix meaningful teardown/zeroing/failure provenance and verify restored state. Reopen cleanup/test prerequisites and executable diagnostic caveats. |
| CTAP-IMP-022 | A-013/B-10/11/D-13/14/15: tests overstate deletion/crypto/feature/protocol proof or return normally without feature exercise. D-14's alleged wrong legacy force-change status is rejected; other lifecycle/cleanup gaps remain. | Select meaningful assertion/fixture improvements, not coverage-only tests. Reopen precise test explanations and verification reports after actual execution. |
| CTAP-IMP-023 | A-009: all SELECT APDU failures remapped as unsupported AID; empty response synthesized Other. | Decide error/provenance policy and verify specific causes. Reopen backend/exception/troubleshooting and WA-IMP-002 mappings. |
| CTAP-IMP-024 | D-16/B-08: admin helpers leave named payload/auth-param/request copies uncleared beyond the final-CM-request guarantee. | Review scoped owned buffers and exceptional paths; separate from builder-retained clones. Reopen admin security/ownership/internal comments and sample cleanup. |
| CTAP-IMP-025 | A-014 / framework probe: default Guid serialization is architecture-independent mixed-endian, while converter swaps only on little-endian hosts. | Code-inferred big-endian portability defect; current little-endian fixture passed, no big-endian execution. Correct serialization explanation now; defer conversion/test changes and avoid claiming unsupported platform qualification. Reopen AAGUID API/internal conversion and platform guidance. |

## Cross-module provenance and bounded decisions

- **CTAP-XREF-001 / WA-XREF-001:** assertion User documentation correction assigned C, with G fresh
  boundary re-review. Original WebAuthn A-002 and later errata remain immutable; do not close before review.
- **CTAP-XREF-002:** G reconfirmed existing WA-IMP-002/003/009/010/011/012/013/014–019 dependencies. Keep
  original IDs and code evidence in boundary-map.md, not duplicate them as new CTAP implementation fixes.
  WebAuthn implementation remains deferred; any documentation edits receive explicit ownership/review.
- **CTAP-DEC-001:** AAGUID byte-order uncertainty resolved by pinned framework source and bounded pure
  probe into code-inferred CTAP-IMP-025. No big-endian platform execution or SDK fix authorized.
- **CTAP-DEC-002:** G-011 selected native AuthenticationResponse display-field retention needs an actor/
  serializer/conformance-boundary decision; no automatic DTO stripping or new vulnerability claim.
- **CTAP-DEC-003:** exact SKU/firmware/prototype support, reset product-window gap (5.5.2/5.5.3), and vendor
  bio field provenance remain bounded evidence questions. Firmware constants are not qualified support.

## Coordinator decisions at this checkpoint

- Accept B's factual protocol/ownership/fallback/Unicode/retry corrections; do not change helper ownership
  or add using wrappers to WebAuthn's shared-protocol backend without a separately selected code decision.
- Accept D's concrete sequence/ownership/configuration guidance and candid standard-bio/encoder limits.
  Wrong wire values and fixtures stay deferred, not relabeled as normative or hidden through citation changes.
- Accept G's User/descriptor and UV-token boundary corrections, retaining exact revision conditions.
  Higher-level orchestration, prompting and supported-policy restrictions can legitimately differ from CTAP.
- Re-baselined historical makeCredential up=false and HID raced-success findings are already corrected in
  source per A. Do not reopen them as present defects or count them again from old reports.
- Live integration smoke is not part of this run; default PIN-normalizing setup precludes treating those
  inspected test paths as read-only merely because the final call is GetInfo.
- C/E/F documentation corrections accepted in their owned files, subject to original-source independent
  review. The supplemental ledger will assign remaining implementation/decision IDs without changing
  their deferred status. Existing WA engineering IDs stay authoritative, not duplicated per consumer.
- C-008 versus D-08: source insertion order is not emitted wire evidence. Actual System.Formats.Cbor10.0.2
  Ctap2Canonical writer emits text keys id/name/displayName and integer keys1/3/-1/-2/-3 regardless of
  the inspected insertion orders. D's code-defect inference is rejected; comment correction remains.
  The pure probe also shows ReadEndMap leaves trailing bytes: Finished does not prove complete input
  consumption. This supports narrow parser-gap reasoning, not execution of every SDK decoder. See
  framework-adjudication.md for exact commands, outputs/source, versions and limits.
- F inventory hash gap resolved mechanically: all four csproj checkout files differ only by CRLF versus
  baseline Git-blob LF, with empty source diff. Baseline inventory hashes are Git-blob bytes; this is not
  an implementation change or unexplained file mutation.
- F-06 policy: preserve the existing CTAP human-coordination requirements for interactive/PIN/reset paths.
  Correct what commands select and what setup does; do not silently relax module policy or invent a new
  approval gate for the GetInfo command itself. Actual normalization/lane policy is deferred under IMP-020.
- F shared ownership includes narrow active architecture/migration references and `src/WebAuthn/README.md`
  requirement qualification from G-010, plus current WebAuthn handoff updates after boundary review.
  Historical records/gaps remain immutable. No generalized FIDO→CTAP rename.
- [Supplemental reconciliation](triage-addendum.md) is accepted by Astra as the stable continuation:
  IMP-026–048 and narrow XREF-003, preserving original WA IDs and differentiating bugs, decisions,
  optional sample enhancements and evidence gaps. All remain deferred. Later review corrections below
  refine evidence without renumbering or losing original findings.
- D-14's legacy-status subclaim is withdrawn: original CTAP2.1 0x05 returns PIN_INVALID for forced
  change, while 0x09 permission issuance returns PIN_POLICY_VIOLATION. The test actually calls 0x05.
  B/D independent reviews verified this; no assertion change is justified by that alleged mismatch.
- RC-R1 original ARKG-10 explicitly defines seed placeholder -65700, agreeing with the SDK's marker.
  Outer kty, wrapped/direct nested maps and derived-algorithm model differences remain IMP-029;
  seed-placeholder disagreement does not. Expired draft status is not a production/conformance claim.
- RE-R2/3: LargeBlobData's Int64 restriction applies to origSize and integer map keys, not arbitrary
  unknown encoded values. Those unknown slices borrow serialized input. Keep this distinct from
  ExtensionOutput's lossy reconstructive integer decoder. IMP-037 and ownership documentation refined.
- COORD-001 is source-confirmed via nullable-array types/assignments and retained language evidence;
  A and F prose corrected, code unchanged. Targeted expansion checked nullable-array decoder exits:
  response largeBlobKey/packed ecdaaKeyId use explicit null guards; LargeBlobOutput locals are already
  nullable memory, and mandatory RP/user/key arrays are guarded. No further instance found in that
  bounded sweep; it is not an exhaustive whole-repository dataflow proof.

All exact source/primary-source passages, complete file dispositions, named clean units and individual
documentation dependencies remain in investigations A/B/D/G and their evidence files. C/E/F findings will
are now available; supplemental C/E/F implementation dispositions are being reconciled before closeout.
