# Management Pass 1 — cycle management-001

## Goal and authority

Help SDK consumers understand Management and complete device-information/configuration tasks using
accurate public contracts, examples, internal explanations and references. Apply the canonical
[workflow](../../../../workflow.md) and [style](../../../../records/style.md).

User approval: execute Management Pass 1 with Sol 6.1 investigation/editing/independent review and Astra
coordination, consequential adjudication, clean-unit sampling, validation and integration. Complete the
campaign's first sweep across remaining applets before any second-sweep implementation return.

## Baseline and scope

- Baseline: `da61d2e6d754b4c155cbb2579202e910986f4b3a`; branch `yubikit-docs-audit`.
- Tracked tree clean at start. Preserve untracked `docs/research/audit-v2-closeout-review.md`,
  `docs/research/audit-v2-execution.md`, `docs/research/audit-v2-jira-traceability.md`, and `mise.toml`.
- Initial inventory: 55 tracked module files, 42 C# files, five public types, 47 non-header API snapshot
  entries (including generated members). Reconcile final semantic coverage against these files.
- Include public XML, full guides/package metadata, all internal explanatory comments, complete
  examples, test explanations, TODOs/unfinished-work markers and substantive specification claims.
- Inspect shared documentation and narrow Core dependencies used by Management. Core-wide discovery,
  protocol and SCP investigations remain separately deferred.
- Pass 1 permits documentation/comment/descriptive metadata changes only. Executable sample/test fixes,
  assertions, API shapes and runtime fixes become deferred findings. Markdown/XML examples may be corrected.
- No device operation is planned for this documentation run. Record device/platform evidence gaps explicitly.

## Phases and exit evidence

1. Baseline/inventory: enumerate module files, public families and external reference candidates; define
   semantic units and owners, separating generated/historical exclusions.
2. Investigation: read every owned semantic unit and record clean/findings dispositions. Trace contracts
   through source and assertions; inspect complete examples rather than only module-name matches.
3. Citation verification: retain exact URLs, revision/status/section, actor and applicability with focused
   primary-source evidence. Sibling implementations corroborate behavior, not normative requirements.
4. Triage: coordinator accepts documentation corrections and assigns stable deferred implementation or
   cross-module IDs with evidence and affected-documentation dependencies. Escalate consequential ambiguity
   after initial investigation and one focused follow-up.
5. Editing: fresh Sol editors, isolated worktrees, one owner per file. Preserve useful rationale and precise
   conditions. Classify examples and prepare meaningful compile-only checks.
6. Independent review: fresh Sol reviewers check original source/specification evidence and full changed
   sections; apply required corrections and re-review. Review all campaign/run records too.
7. Sampling/reconciliation: Astra samples >=10% of clean semantic units, minimum three where available,
   across public XML, narrative/examples and internal/test comments. Expand any category with a material miss.
8. Validation/closeout: required build/unit-test/docs/architecture/complexity/scoped-format checks,
   executable-token/directive and API preservation, compiled examples, reviewed-file hashes, complete
   coverage, final handoff, commit/push and verified remote checkpoint. Serialize toolchain use per checkout.

## Investigation assignments (task packets)

All agents read root/module/test instructions as applicable and the workflow/style. Investigations
write only their assigned run reports. No build/test/hardware, source edit, commit or recursive delegation.
Use Context7 for framework/library questions and primary Yubico references for protocol/device claims.

### A — session, transports and test evidence

Owner model: `github-copilot/gpt-6.1-sol`. Files: `src/Management/src/{IManagementSession,ManagementSession,
IYubiKeyExtensions}.cs`, `src/Management/src/Backend/*.cs`, all module test C# files. Inspect narrow Core
dependencies as required. Cover initialization, transport/SCP choices, borrowed/owned resources,
firmware/reset, cancellation/errors and test-assertion evidence. Output `investigation-a.md` with stable
`A-*` candidates, source/reference evidence, every owned file disposition and clean semantic units.

### B — configuration and wire claims

Owner model: `github-copilot/gpt-6.1-sol`. Files: `src/Management/src/{DeviceConfig,SetDeviceConfigOptions}.cs`.
Read related tests/source as evidence (A owns their edits). Cover nullable omission, masks, ranges, flags,
NFC restriction, lock/unlock semantics, ownership/zeroing and TLV claims. Verify official references and
firmware/family conditions. Output `investigation-b.md` with `B-*` candidates and per-unit coverage.

### C — consumer journeys and external references

Owner model: `github-copilot/gpt-6.1-sol`. Files: module/package/test Markdown, all example files,
descriptive package metadata, API/project support inventory and active shared documentation references.
Cover complete examples, ManagementTool explanatory comments, migration/architecture/usage consistency,
stale development residue and actionable task flow. Read source to verify each claim. Output
`investigation-c.md` with `C-*` candidates, complete module/support dispositions, external coverage and
example classifications. Preserve immutable historical audit records and label historical exclusions.

## Durable output and Pass 2 handoff

Keep phase progression, task packets, findings/triage, exact evidence, decisions, edit/review/correction
records, coverage, experiments/compiled-example sources, validation logs and closeout in this run.
Stable accepted IDs use `MGMT-DOC-*`, deferred implementation/test IDs `MGMT-IMP-*`, and cross-module
dependencies `MGMT-XREF-*`. Map each deferred item to implementation paths and exact documentation
re-review triggers. Track code, docs and device/platform evidence independently. Completion is not a
standards-conformance or hardware-qualification claim.
