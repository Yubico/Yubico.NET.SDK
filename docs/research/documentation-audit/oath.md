# OATH campaign handoff

## Current state

Pass 1 and the explicitly approved implementation follow-up are complete within their recorded scopes.
The implementation waves include shared Base32/input handling, protocol/CLI corrections, terminal reset,
timestamp bounds, authentication-error provenance, migrations and targeted documentation re-review.
This historical implementation exception does not change the documentation-only rule for future pass 1s.

Latest OATH checkpoint commit: `e353b49a`; final substantive reconciliation: `76a8ae52`.
The campaign later continued with WebAuthn at `9399b94b`.

## Reading map

- [Published OATH checkpoint](../oath-documentation-audit-checkpoint.md): compact outcomes and unresolved scope.
- [Pilot closeout and retrospective](records/pilot-closeout.md).
- [Coverage summary](records/coverage-summary.md) and [original per-file coverage](records/pilot-file-coverage.json).
- [Implementation findings and decisions](records/implementation-followups.md): original issue definitions,
  scoped resolutions, remaining questions and documentation dependencies. Later resolution states and
  closeouts supersede earlier “in progress” headings.
- [Implementation plan](records/implementation-plan.md) and [wave-one dispatch](records/runs/fixes-wave-001/dispatch.md).
- [Wave-one completion](records/runs/fixes-wave-001/completion.json).
- [Wave-two completion](records/runs/fixes-wave-002/completion.md) and [machine-readable result](records/runs/fixes-wave-002/completion.json).
- [Final documentation-impact review](records/runs/fixes-wave-002/documentation-impact-review.md).
- [Terminal-reset decision](records/runs/fixes-wave-002/reset-decision.md) and [review](records/runs/fixes-wave-002/terminal-reset-review.md).
- [SCP reference investigation](records/runs/fixes-wave-002/scp-reference-investigation.md).
- [Upstream reconciliation](records/runs/upstream-001/reconciliation.md).

The full retained run directories include the original OATH investigations, editing/review reports,
source/hash snapshots, test logs, device evidence, correction rounds and pause/recovery records. Use
the [record manifest](record-manifest.json) to locate and verify an artifact.

## Outstanding work and evidence limits

- All 14 numbered IMP findings have scoped resolutions. Broader DEC questions are not all closed.
- Broader authentication-failure session policy, raw-key scope, provisioning-URI extensions, wider
  HOTP counters, padding provenance, detailed HOTP operation semantics and earliest-firmware rationale
  retain their recorded deferred scope.
- Plain-channel terminal reset was exercised. Live SCP reset was **attempted but never reached**:
  serial 125 returned `6A88` during initialization. Repeat bounded initialization checks on additional
  SCP-capable devices before generalizing; do not describe offline SCP tests as live reset proof.
- Multi-key discovery issue `CORE-001` remains a separate Core follow-up. The older key worked alone;
  native discovery ownership during the multi-key failure was not proven released.
- Historical hardware results identify their actual device/firmware/transport; they are not a current
  inventory or Windows/macOS proof. Re-establish device/PIN/application state before new operations.
- Migration YAML issue `DOC-002` remains outside the completed OATH correction scope.

Any return visit uses a new pass-2 run under the [workflow](workflow.md). Revalidate unresolved items
against current source and reopen their named documentation dependencies after accepted fixes.
