# CTAP2 Pass 1 — cycle ctap2-001

## Goal, approval and baseline

Improve the entire CTAP2 module's documentation so consumers understand its value, complete real
protocol tasks and understand their responsibilities. Apply the canonical [workflow](../../../../workflow.md)
and [approved voice](../../../../records/style.md). User approved the A–G plan, including the dedicated
CTAP/WebAuthn consistency assignment and the additional CTAP-specific checks below.

- Baseline: `5b09a9b6942ded3913b5c9340c3e126617e61a82`, branch `yubikit-docs-audit`, tracked tree clean.
- Preserve original untracked `docs/research/audit-v2-closeout-review.md`, `audit-v2-execution.md`,
  `audit-v2-jira-traceability.md`, and root `mise.toml`.
- Initial inventory: 183 tracked module files: 73 library C#, 77 test C#, 20 example C#, five Markdown
  and eight API/project/runner support files. API snapshot: 948 entries and 85 apparent type entries;
  reconcile effective accessibility/generated projections before final counts.
- Whole selected module in one cycle. Include public XML, guides/package descriptions, internal
  explanations, complete examples, test comments, TODO/phase residue, and substantive specification claims.
- Narrow shared/Core dependencies and affected WebAuthn claims are included; not a new whole-Core or
  whole-WebAuthn audit. Preserve immutable historical evidence; re-baseline earlier findings.
- Documentation-only: no runtime/API/test assertions/executable sample strings changed. Markdown/XML
  examples may be corrected. Behavioral defects receive durable second-sweep findings and dependencies.
- No device operation or live integration test is planned. Later hardware/UP/UV verification remains separate.

## Roles and phase progression

Primary model `github-copilot/gpt-6.1-sol`: investigators, fresh editors, fresh independent reviewers.
Astra: coordination, triage, consequential adjudication, clean-unit sampling, validation and integration.
Resolve model availability before dispatch. One editing owner per file, with isolated editor worktrees.
Reviewers independently read original source/spec evidence; they do not inherit an investigator's verdict.

1. Baseline/inventory and explicit ownership: inventory.json names every module path with baseline hash.
2. Read complete units and assertions; record findings and clean units, not only search hits.
3. Verify exact reference revision/publication status/section, actor, normative strength and conditions.
   Keep focused quotes/URLs/access dates. No automatic citation upgrade or weakening to hide a defect.
4. Triage: stable CTAP-DOC/IMP/XREF IDs, source-observed versus desired behavior, bounded unknowns and
   re-review dependencies. Initial research plus one focused follow-up, then adjudicate/escalate.
5. Fresh editing: coherent API/task/comment changes, approved direct/calm/precise voice, useful rationale.
6. Fresh independent review, required corrections and re-review; separate optional preferences.
7. Astra samples >=10% of clean semantic units, minimum three, spanning API XML/narrative/examples/
   internal/test comments. Expand a category after material misses; reconcile the complete dependency map.
8. Final build/unit/docs/architecture/complexity/scoped-format gates, meaningful example compilation,
   runtime-token/directive/API preservation, final cross-layer review, record review, commit/push and
   verified remote checkpoint. Serialize toolchain use per checkout; preserve outputs before retention expires.

## Common task packet for A–G

Read root/module/test instructions and this plan/workflow/style; load relevant skills. Use Context7 for
framework/library/CLI questions and original official specifications/product references for protocol claims.
Investigation writes only its named report. No builds/tests/hardware/commits/source edits/recursive delegation.
Read other owners' source only as dependencies; report cross-file findings to the owner rather than editing.
Tests are evidence of actual assertions, not proof that named security properties or hardware paths ran.
Each report includes exact owned-path coverage, meaningful clean-unit IDs, local candidate IDs, primary
evidence, source locations, doc versus deferred behavior, affected-documentation dependencies and limits.

### A — session and capabilities

Session/interface/extensions, AuthenticatorInfo, transport backends, CTAP commands/status/errors, general
CBOR builder/parser/AAGUID conversion and assigned session/transport/info/shared-helper test files.
Cover selection/initialization/SCP/firmware, ownership, cached versus fresh info, notifications,
sequencing/cancellation/error provenance, raw exchange contract, reset applicability/timing/side effects.
Report `investigation-a.md`, candidates A-*; claim-matrix contributions for capability and session behavior.

### B — PIN/UV

All Pin source and assigned PIN tests. Protocol1/2, key agreement, padding/encoding, token permissions,
RP binding, retries/blocked states, built-in UV versus PIN, ownership/zeroing and authorization lifetime.
Report `investigation-b.md`, B-*; include state sequence and binary-data contracts. No PIN/device operations.

### C — credential operations and data

Credentials, COSE, credential-request encoding/CoseKeyWriter and assigned make/get/attestation/algorithm/
request tests. Coordinate session-file findings with A. Check request/response semantics, UP/UV options,
next-assertion sequencing, user/descriptor omission, attestation/raw/unknown-data limits and ownership.
Report `investigation-c.md`, C-*. Re-baseline WA-XREF-001 and version-sensitive descriptor claims with G.

### D — administrative operations

CredentialManagement, BioEnrollment, Config and matching tests. Review enumeration state and totals,
delete/update, biometric sample sequences/cancellation, configuration permissions/capability conditions,
ownership and exact assertion evidence. Report `investigation-d.md`, D-* with state-sequence contracts.

### E — extensions, storage and cryptographic conveniences

Extensions, LargeBlobs, Arkg, Crypto and matching tests. Review hmac-secret, credProtect, credBlob,
minPinLength, large-blob chunk/storage lifecycle, previewSign/ARKG, encrypted metadata, raw/owned outputs
and request/enforcement/result distinctions. Label standardized/vendor/preview/experimental claims and
verify applicable publication status. Report `investigation-e.md`, E-*; coordinate shared request fields
with C and WebAuthn transformations with G. Experimental availability is not production guidance.

### F — consumer journeys and shared references

All module/example/test Markdown, all FidoTool files/comments, package/API/project support inventory,
active shared usage/architecture/migration/CLI references. Audit complete task examples and explanatory
units, including implicit variables and lifetimes. Classify every fence/XML example, typed prerequisites
and exclusions. Distinguish protocol demonstration from application integration: challenge/clientDataHash,
credential selection and RP verification responsibilities must not be hidden by compilable syntax.
Report `investigation-f.md`, F-*; external exact-path manifest and full snippet classifications required.

### G — CTAP ↔ WebAuthn consistency (dedicated report owner)

Read the current WebAuthn handoff/triage and relevant current public XML/guides/examples/architecture plus
actual boundary source. Produce `investigation-g.md` and `boundary-map.md`: CTAP claim → WebAuthn claim →
source/spec evidence → consistent/legitimate layer difference/contradiction/unresolved → owning editor →
Pass-2 trigger. Cover responsibilities, UP/UV/PIN, protocol negotiation, credential/user fields, extensions,
ownership/cancellation/errors and support claims. Preserve WA IDs/provenance; do not duplicate bug counts.
G does not overlap source edit ownership; explicit WebAuthn file owners are assigned after triage if needed.
A fresh final boundary reviewer must check both modules' resulting claims before closeout.

## Shared matrices and additional completion checks

Astra reconciles report contributions into `claim-matrix.md`, with feature/sequence/byte contracts:

- Standard revision/status versus advertised capability versus configured/current state versus request
  authorization versus actual SDK enforcement versus executed/inspected test evidence.
- Stateful sequences: assertions/next assertions, credential enumeration, biometric samples, blob chunks,
  PIN changes/reset/token lifetime, cached GetInfo. Explain actual completion/failure/recovery boundaries.
- Error provenance: CTAP status, APDU/transport, SDK validation, cancellation and callback faults. A failed
  call is not automatically proof the device did nothing; do not invent a rollback/session policy.
- Binary inputs/results: representation/length/encoding, absent versus empty, borrowed/owned copies/views,
  disposal and sensitive-data classification, forward-compatible unknowns versus strict malformed/security data.
- Standards/vendor/preview/experimental distinction, with exact clauses and scope. No conformance claim from
  sibling agreement or a green suite. No broad claim every YubiKey supports all CTAP2.1/2.3 features.

Use the Management cycle's lessons: verify disposable input ownership as well as sessions, complete examples
and mechanical local links, actual assertions/fakeable seams, and compiler hypotheses with evidence. For
test/sample-dependent examples use real project/reference/analyzer context from the start; keep raw compiler
diagnostics distinct. Preserve SDK warning policy/analyzers and do not manufacture a green result by stripping
attributes, calls, cleanup or tasks. Build appropriate offline examples, never execute a ceremony for syntax proof.

## Records and second-sweep handoff

Retain plan/task packets, inventory/coverage, dated progression, findings/decisions, matrices, citations,
edits, independent reviews and re-reviews, clean sampling, snippet classifications/inputs/exclusions,
failed attempts/diagnostics, actual validation logs, final hashes/patches and closeout in this run.
Implementation, documentation-review and hardware status are separate. Map every deferred item to source
paths and exact XML/guide/example/comment re-review triggers. New release is breaking; backward API
compatibility is not a goal, but no API redesign is authorized in Pass 1.

The approved next order is CTAP2 → Security Domain → YubiOTP, with PIV and Core saved for last.
Do not begin CTAP or WebAuthn Pass 2 on documentation closeout. Core's eventual scope is separately planned.
