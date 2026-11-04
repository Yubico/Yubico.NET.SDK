# WebAuthn campaign handoff

## Current state

**Pass 1 is complete**, committed and pushed as
`9399b94bdc6fe2786903b1b6e58808537aa8c94e` on `yubikit-docs-audit`.
**Pass 2 has not started.** The 21 implementation/test follow-ups remain a scoped backlog, not implemented
fixes or approval of limitations as intended contracts. The original CTAP2 documentation dependency
**WA-XREF-001** is satisfied by the independently reviewed CTAP2 Pass-1 correction; see the
[fresh boundary review](modules/ctap2/pass-1/ctap2-001/boundary-review-final.md).

The [CTAP2 cycle](ctap2.md) also reconciles narrowly affected WebAuthn requirements, requested-versus-issued
permission wording, registration unsigned-output key0x06, and inner previewSign policy flags. No WebAuthn
runtime fix or full new WebAuthn audit is implied. Original WA findings/errata remain unchanged under records/;
WA-IMP-010 descriptor recovery, other engineering items and the native selected-user privacy decision remain open.

Coverage: 131 module files, 44 public types, 542 API snapshot entries including generated members,
and 30 external-reference files. The 51 initial investigator findings overlap; they are not 51
independent defects. All accepted documentation changes and required review corrections were reviewed.

## Reading map

- [Published checkpoint](../webauthn-documentation-audit-checkpoint.md): outcomes and the complete deferred table.
- [Cycle plan](records/webauthn-plan.md), [progress/status](records/runs/webauthn-001/status.md), and
  [closeout/retrospective](records/runs/webauthn-001/closeout.md).
- [Triage and pass-2 dependency ledger](records/runs/webauthn-001/triage.md): authoritative dispositions,
  review corrections, implementation owners and documentation re-review triggers.
- [Client investigation](records/runs/webauthn-001/investigation-a.md),
  [extension investigation](records/runs/webauthn-001/investigation-b.md), and
  [guide/external-reference investigation](records/runs/webauthn-001/investigation-c.md): original
  claims, primary citations, source traces and per-file dispositions. The leading A erratum corrects
  its original CTAP descriptor-omission version attribution; do not reuse the superseded attribution.
- [Final coverage manifest](records/runs/webauthn-001/coverage-final.json) and
  [reviewed-file hashes](records/runs/webauthn-001/reviewed-files.json).
- [Edit A](records/runs/webauthn-001/edit-a.md), [edit B](records/runs/webauthn-001/edit-b.md), and
  [edit C](records/runs/webauthn-001/edit-c.md): exact finding-to-file mappings and snippet classifications.
- [Review A](records/runs/webauthn-001/review-a.md) and [accepted corrections](records/runs/webauthn-001/review-a-followup.md).
- [Review B](records/runs/webauthn-001/review-b.md), [test-summary acceptance](records/runs/webauthn-001/review-b-followup.md),
  [blob follow-up](records/runs/webauthn-001/review-b-blob-followup.md), and [final acceptance](records/runs/webauthn-001/review-b-blob-final.md).
- [Review C](records/runs/webauthn-001/review-c.md) and [accepted corrections](records/runs/webauthn-001/review-c-followup.md).
- [Astra sampling](records/runs/webauthn-001/sampling.md) and [expanded example review](records/runs/webauthn-001/sample-expansion.md).
- [Validation record](records/runs/webauthn-001/validation.md), its archived logs, example sources, compiler
  checker source and isolated nullable-memory experiment.

## Pass-2 priorities to assess

Re-baseline and select coherent groups from the ledger rather than assigning this entire list blindly:

- **Policy/enforcement:** verified WebAuthn PRF use; credential-protection enforcement; enterprise
  attestation behavior; required blob support; original allow-list cardinality; minimum-PIN input handling.
- **Ownership and data contracts:** PRF/blob result cleanup and intermediate buffers; decoder snapshots;
  default AAGUID semantics; missing credential-ID recovery; absent-versus-empty blob reporting.
- **Compatibility/errors:** protocol negotiation and built-in-UV capability checks; timeout input/return
  policy; consistent error isolation; exact client-data serialization; previewSign handle/argument boundaries.
- **Test evidence:** portable UV assumptions, retained-view zeroing checks, minimal signing-attestation
  fixtures and explicit returned-ID membership assertions.

The item IDs, source/spec evidence and exact re-review dependencies are in the triage ledger. A code
fix reopens its related guide/XML/internal comments, optional outputs, examples and migration claims.
Do not simply remove a limitation after merging code without checking the complete affected contract.

## Verification completed and still outstanding

Pass 1 completed the full solution build and 3,998 unit tests, including 371 WebAuthn cases; five
pre-existing skips, zero failures and zero compiler/analyzer warnings. Documentation, architecture,
complexity and scoped formatting checks passed. Three current example groups compiled. Runtime tokens
and API shapes were preserved apart from a namespace-import ordering correction.

No live WebAuthn ceremony, PIN setup, credential mutation, reset or cross-platform execution was run.
The isolated .NET check establishes the nullable-memory conversion only, not storage/device behavior.
Those omissions do not invalidate the documentation pass; they remain outstanding implementation and
hardware-verification dimensions.

The future pass-2 plan must include the full selected WebAuthn registration/authentication ceremony
coverage with user presence and user verification, applicable PIN/prompt/retry/cancellation paths,
credential selection and affected extension flows. Specify devices, firmware and transports, required
state, supported built-in UV, cleanup and exact commands. Obtain the repository-required human approval
for interactive or state-changing operations. Record blocked and skipped paths honestly and do not
generalize one device or Linux run to all supported configurations.
