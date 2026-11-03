# SDK documentation and implementation cycles

This directory is the durable record of the module-by-module documentation campaign and the
implementation work it uncovers. Keep it in version control and push completed checkpoints. Local
agent sessions and temporary workspaces are working copies, not the only handoff.

The campaign has two distinct passes:

1. **Documentation pass:** audit and improve public documentation and internal explanatory comments;
   verify claims, examples and citations; record implementation defects without changing behavior.
2. **Implementation pass:** select and fix recorded defects or behavioral gaps, verify the resulting
   contracts, and independently re-review every affected documentation claim.

Use the [workflow](workflow.md) for future modules. The approved [writing style](records/style.md)
remains the editorial reference. Repository and module instructions still govern execution.

## Module status

| Module | Pass 1: documentation | Pass 2: implementation | Start here |
| --- | --- | --- | --- |
| OATH | Complete; pilot followed by documentation-impact reviews | Explicitly approved fix waves complete within recorded scope; additional contract/device questions remain deferred | [OATH handoff](oath.md) |
| WebAuthn | Complete, committed/pushed at `9399b94b` | Not started; 21 implementation/test follow-ups and one CTAP2 documentation dependency recorded | [WebAuthn handoff](webauthn.md) |
| Management, PIV, Security Domain and other applets | Not started by this campaign | Not scheduled by this campaign | Apply the workflow when a module is selected |
| Core | Deferred; only narrow dependencies and a WebAuthn example were touched | Discovery/SCP follow-ups retain their existing separate scope | Existing module checkpoints; no Core-wide audit is implied |

## Retained records

[`records/`](records/) is a byte-preserving snapshot of the retained local campaign workspace taken
after WebAuthn closeout on 2026-11-03. It contains the available plans, role/task packets, progression
and pause records, findings, coverage, evidence, edits, decisions, reviews, validation logs, source/diff
snapshots, implementation-wave records and closeouts for both completed modules.

[`record-manifest.json`](record-manifest.json) lists every retained file with its original relative path,
byte count and SHA-256. The temporary WebAuthn compiler-checker workspace (generated binaries, build
intermediates and duplicated staged source) is omitted; byte-identical checker source/project copies,
the experiment and compilation evidence are retained. This is an
archive of records that actually exist, not a reconstruction of missing conversations or unrecorded work.

**Historical authority:** preserve the original records, including failed runs and superseded plans.
Old `PAUSED`, “pending,” or “in progress” text describes that checkpoint, not current module status.
Start with the module handoff and latest closeout, then follow findings and evidence backward. The
archived plans and role prompts are historical records, not new operational instructions. Some contain
machine-local paths or links to commits/worktrees; those paths identify the original execution context.

The new workflow and module handoffs here supersede archived operational direction when describing
future campaign work. Source/specification evidence and the original meaning of historical records
remain unchanged. Future runs write directly into tracked per-module/pass directories described in
the workflow, rather than maintaining a second local-only authority.

## Reading order for a return visit

1. Read the module handoff: completed scope, current source checkpoint, outstanding items and limits.
2. Read the authoritative findings/triage ledger and its implementation-to-documentation dependency map.
3. Check the latest review and validation records; distinguish source inspection, compiled examples,
   executed unit tests, and actual device/platform observations.
4. Compare the current source with the recorded baseline before assigning fixes. A finding can already
   be fixed, changed, or invalidated by intervening work.
5. Create a new pass-2 run with explicit item IDs and scope; retain the pass-1 evidence unchanged.
