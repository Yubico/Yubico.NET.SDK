# CTAP2 campaign handoff

## Current state

Pass-1 content, independent source/boundary reviews and verification are complete in
[ctap2-001](modules/ctap2/pass-1/ctap2-001/closeout.md), based on
`5b09a9b6942ded3913b5c9340c3e126617e61a82` on `yubikit-docs-audit`.
[Final record/evidence review](modules/ctap2/pass-1/ctap2-001/review-closeout-followup.md) and archival
retention passed. Source commit and verified remote checkpoint:
**`3bc3e4091acc87e269bb371830899ef3ddac1af8`**, pushed to `origin/yubikit-docs-audit` on 2026-11-04.
`git push origin yubikit-docs-audit` succeeded; `git ls-remote origin refs/heads/yubikit-docs-audit`
matched local HEAD exactly. This publication entry is a later documentation-only checkpoint.
Read the [status](modules/ctap2/pass-1/ctap2-001/status.md) for phase progression.

The approved next sequence is CTAP2 → Security Domain → YubiOTP, with PIV and Core kept for last.
Core's detailed scope is still separately planned. Pass 2 waits until the first sweep is complete.

This cycle includes a dedicated CTAP/WebAuthn claim-consistency assignment and fresh final boundary
review. Existing `WA-XREF-001` and relevant WebAuthn deferred findings are re-baselining inputs, not
new implementation authorization. Public XML, guides, internal/test/example comments and substantive
specification claims all belong to Pass 1; runtime/API/test-assertion changes remain deferred.

No live CTAP/WebAuthn ceremony, PIN change, biometric enrollment, credential mutation or reset is
planned for this documentation run. Implementation, documentation and hardware status remain separate.

## Reading map

- [Plan/task packets](modules/ctap2/pass-1/ctap2-001/plan.md),
  [coverage](modules/ctap2/pass-1/ctap2-001/coverage.md) and
  [common claim/sequence matrix](modules/ctap2/pass-1/ctap2-001/claim-matrix.md).
- [Triage](modules/ctap2/pass-1/ctap2-001/triage.md) and
  [supplemental ledger](modules/ctap2/pass-1/ctap2-001/triage-addendum.md): stable IDs, rejected inferences,
  implementation/decision boundaries and precise documentation re-review triggers.
- [Final CTAP/WebAuthn boundary map](modules/ctap2/pass-1/ctap2-001/boundary-final.md) and
  [final independent boundary acceptance](modules/ctap2/pass-1/ctap2-001/boundary-review-final.md).
- [Sampling](modules/ctap2/pass-1/ctap2-001/sampling.md),
  [reviewed content hashes](modules/ctap2/pass-1/ctap2-001/reviewed-files.json), and original A–G
  investigations/edit/review/correction records in the run.
- [Repository validation](modules/ctap2/pass-1/ctap2-001/validation.md),
  [33-group example compilation](modules/ctap2/pass-1/ctap2-001/snippet-validation.md),
  [framework adjudication](modules/ctap2/pass-1/ctap2-001/framework-adjudication.md), and
  [closeout/retrospective](modules/ctap2/pass-1/ctap2-001/closeout.md).
- [Run record manifest](modules/ctap2/pass-1/ctap2-001/record-manifest.json):534 retained artifacts,
  12,964,683 bytes, with byte lengths/SHA-256 (533 text records and one pinned package-proof archive).
  The manifest excludes itself;101 generated runtime/cache duplicates have an explicit retention overlay.

## Completed scope and verification

All 183 module paths, 85 public families and 948 snapshot entries are reconciled, including generated
projections/historical removal entries. Current shared references and the CTAP/WebAuthn boundary were
reviewed with explicit read-depth/history exclusions. Nineteen eligible clean samples out of132 met
numeric/category targets; an additional behavior check is retained but not counted.

Clean solution build and 3,998 unit tests passed (492 CTAP2), with five existing skips, zero failures
and zero compiler/analyzer warnings. Documentation, architecture, complexity and scoped formatting passed.
Declaration/body tokens and preprocessor directives are preserved across 152 C# paths, with 31 exact
formatter-only unused namespace imports separately accounted; all 20 API snapshots remain unchanged.
All 33 selected example groups compiled in six real SDK contexts with zero warnings, without execution.

## Deferred second sweep

IMP-001–048 retain 47 deferred implementation/verification/decision groups and one rejected ordering
mechanism (017). They are not 48 confirmed bugs or permission to implement all groups. Areas include
decoder/absence/key/PIN validation, secret ownership, biometric wire/results, administrative/stateful
flows, extension/storage/experimental profile contracts, test evidence/cleanup and sample target/error
handling. Re-baseline selected items and settle consequential intent before code changes.

**WA-XREF-001 is resolved as documentation only.** WA-IMP-010 recovery and all other original WA engineering
items remain open. Native selected-response privacy/serializer scope (DEC-002) and bounded product/profile
questions remain decisions/evidence gaps. Core credential-reader dependencies retain their narrow scope.
Later hardware work must specify exact device/family/firmware/transport/current-state and human ceremony
requirements; existing unit/compile/source evidence does not establish those dimensions.

The next approved target is Security Domain, then YubiOTP. PIV and Core remain for last. The existing
YAML issue DOC-002 and older audit-host discovery naming collision are separately recorded, not silently
repaired. No SDK runtime/API/test-assertion or executable-sample behavior fix occurred in this pass.
