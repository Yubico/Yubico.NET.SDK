# Record-preservation checkpoint — 2026-11-03

## Request and scope

After the second module cycle, the user confirmed that each module needs a documentation-only first
pass and a later implementation/fix pass followed by documentation re-review. The workflow, progress,
completed/outstanding work, verification limits and reports must survive for that return visit.

The completed WebAuthn source checkpoint is `9399b94b`. Before this record-preservation step, detailed
OATH/WebAuthn artifacts existed in the local campaign workspace, while GitHub held only compact
research checkpoints. This step closes that storage gap; it does not rerun either module audit or
start implementation/hardware work.

## Changes

- Added the canonical [two-pass workflow](workflow.md), including stage progression, evidence rules,
  independent review, code-to-documentation dependencies and durable completion requirements.
- Added current [OATH](oath.md) and [WebAuthn](webauthn.md) handoffs distinguishing completed scope,
  deferred implementation, and missing device/platform evidence.
- Preserved all **784 retained text records**, **4,804,723 bytes**, from the local campaign workspace
  in `records/`, with original paths and byte-for-byte SHA-256 checks in `record-manifest.json`.
- Excluded 59 temporary compiler-checker workspace files, including generated outputs and duplicated
  staged source; the byte-identical checker source/project, isolated experiment, compiled-example
  source and recorded results remain in the snapshot.
- Marked the snapshot as historical evidence. Current handoffs and the new workflow supersede old
  operational directions; old failed/pending/superseded states remain available for provenance.
- Linked both published module checkpoints to these durable handoffs.

No SDK behavior, API, test assertion or hardware state changed. Archived source files are historical
evidence, not additions to shipping SDK projects. The four pre-existing untracked user files are excluded.

## Verification and handoff

Every archived file is verified against its manifest and original snapshot bytes. New navigation links
are checked independently of the repository's active-doc boundary; archived historical links retain
their original execution context rather than being silently rewritten. The
[independent handoff review](record-preservation-review.md) passed with no required corrections.

Final command: `dotnet toolchain.cs -- build test docs-qa docs-architecture complexity`.
It passed with **3,998 unit tests**, five existing skips, zero failures and zero compiler/analyzer
warnings. The full solution compiled, 73 active documents and 10 package READMEs passed hygiene checks,
and architecture evidence/images passed. Complexity found no changed shipping C# methods, as expected
for this records-only change. Archived source snapshots are not formatted or treated as shipping code;
their original bytes and whitespace are deliberately preserved.

The [run log](maintenance/2026-11-03-record-preservation/validation/run.log),
[machine-readable result](maintenance/2026-11-03-record-preservation/validation/result.json), and all
17 files from that validation run are retained alongside this report. Hash/byte/count validation covers
the 784-file historical snapshot separately from these new maintenance records. The completion commit
on `yubikit-docs-audit` includes this workflow, both module handoffs, the complete retained snapshot and
review/validation records; the remote commit is verified after pushing.

The next module is not selected by this change. Management is a reasonable next documentation target
to exercise the refined workflow on device configuration and application-selection guidance; its actual
inventory must establish the scope. PIV and Security Domain can use the same workflow when selected.
WebAuthn pass 2 remains outstanding, including the planned human-coordinated UP/UV ceremony verification.
