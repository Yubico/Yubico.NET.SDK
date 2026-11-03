# Two-pass module workflow

## Purpose and authority

Give SDK consumers documentation that explains practical value, supports real tasks, and tells the
truth about contracts and limitations. Use the same evidence to make a later implementation pass
precise and reviewable. This workflow incorporates the completed OATH and WebAuthn cycles and the
user's explicit two-pass direction.

**Campaign sequencing:** complete Pass 1 across every remaining in-scope YubiKey applet/module before
starting the second sweep of Pass 2 returns. A module's documentation closeout does not start its
implementation pass. Preserve findings and documentation dependencies for that later sweep. OATH's
already completed implementation exception remains historical; Core's audit scope remains deferred.

Review a whole selected non-Core module in one cycle, with coherent internal assignments. Public APIs
and user documentation come first; internal explanatory comments, test/example comments, unfinished
work markers and substantive specification references are also in scope. Core remains deferred until
separately selected. Read dependencies narrowly and record cross-module findings explicitly.

The approved [style](records/style.md) applies to authors and reviewers: direct, calm, precise,
approachable, and optimized for understanding. Preserve useful rationale, conditions and local context.
Paragraphs explain; numbered steps guide procedures; tables compare real alternatives. Internal
comments explain present constraints and intent rather than an agent's development history.

Sol 6.1 remains the primary investigator/editor/independent reviewer; Astra coordinates, adjudicates
consequential uncertainty, samples, validates and integrates. Resolve exact available model IDs before
dispatch. Use fresh editor and reviewer sessions with explicit scope and evidence. The coordinator owns
phase transitions and records; ask the user about consequential intent, not every routine task.

## Pass 1 — documentation only

1. **Baseline and inventory.** Record branch, commit, worktree state, instructions and preserved user
   work. Inventory every public type/member family, active user-documentation reference, source/internal
   comment region, and example/test explanatory unit. Label generated/historical exclusions. Reconcile
   API snapshots with effective accessibility; do not invent prose for compiler-generated boilerplate.
2. **Read and investigate.** Read complete semantic units, not just keyword matches. Audit correctness,
   usefulness, reader responsibilities, examples, comments, TODOs and project-phase residue. Read all
   relevant example blocks, including implicit `client` calls with no module keyword. Record clean units
   as well as findings. Source and tests are evidence of implementation, not automatic proof of intent.
3. **Verify citations.** Identify specification title, version/status, section/anchor, actor/layer,
   normative strength and feature conditions. Read the actual passage. Do not carry an optimization or
   section number across versions, upgrade references automatically, or treat sibling agreement as
   conformance. Cache focused excerpts and URLs with an access date. Independent reviewers recheck
   consequential clauses from originals rather than inheriting the investigator's citation.
4. **Triage.** Accept supported documentation corrections. Give behavioral bugs, missing enforcement,
   contract choices, capability gaps and test deficiencies stable IDs and pass-2 dispositions. State
   real limitations where consumers need them without making defects intended contracts. Preserve
   genuine unfinished work in the ledger; zero TODOs is not a success metric. After initial research
   and one focused follow-up, escalate consequential ambiguity or record its precise evidence gap.
5. **Edit with fresh ownership.** Improve the entry-point task flow and related XML/comments together.
   Explain purpose, prerequisites, application/RP responsibilities, results, failure and resource lifetime.
   Classify snippets as complete examples, composable fragments, application-dependent sketches or
   historical before/after material. Compile meaningful current examples with explicit typed prerequisites.
   One editor owns each file; use isolated worktrees for concurrent edits.
6. **Review independently.** Check full affected sections against original source/spec evidence and the
   approved style. Separate required factual/usefulness corrections from optional preferences. Test
   comments describe what assertions establish, not desired security properties, method names or a
   hardware test that was never executed. Correct and re-review required findings.
7. **Sample and reconcile.** Astra samples at least 10% of clean semantic units (minimum three where
   available), spanning API XML, narrative/examples and internal comments. Define units in the manifest.
   A material miss expands that category. Recheck the whole claim-to-document dependency map after
   new evidence or corrections, including package pages, architecture, migration and shared examples.
8. **Validate and close out.** Run applicable repository gates on the final changes. Serialize toolchain
   runs within one checkout. Scope formatting to edited files and verify executable tokens/directives
   are preserved, recording harmless import-order changes separately. Record counts, limitations,
   review outcomes, final commit and verified remote hash. Push the workflow records with the changes.

Pass 1 changes documentation, examples/comments and descriptive package metadata. It does not silently
include runtime fixes, changed test assertions, API redesign, new lifecycle policy or device remediation.
If the user explicitly authorizes an exception, record that as a separately scoped implementation run
with its own checks and documentation-impact review. The historical OATH exception is not standing
permission for later modules.

Small, isolated language/library experiments may settle a documentation question without modifying
SDK behavior. Retain the experiment and its output; scope the conclusion to what it proves. Do not
publish “not investigated yet” narration as a permanent comment when a bounded check can settle it.

### Pass-1 completion means

- Every in-scope inventory unit has a semantic disposition; overlaps and exclusions are explicit.
- Every accepted documentation change has independent review and required corrections re-reviewed.
- Examples and important citations have the appropriate recorded checks.
- Known implementation gaps have truthful public qualifications and durable pass-2 records.
- The actual final diff passes applicable gates; reports and evidence are committed and backed up.

This is documentation completion. It is not a claim of implementation completeness, certification,
full specification conformance, all-platform execution or live hardware qualification.

## Pass 2 — implementation and documentation-impact re-review

1. **Re-baseline the selected items.** Read the module handoff and original evidence. Compare each finding
   with current source and intervening changes. Record whether it remains reproducible, changed, was
   already fixed, or needs a product decision. Keep the original finding ID and provenance.
2. **Agree the contract and scope.** Use `docs/SDK-HOUSE-STYLE.md` and repository instructions. Define the
   desired behavior, ownership, errors and compatibility/standards target; settle consequential policy
   with the maintainer. Select coherent fix groups rather than automatically implementing every finding.
3. **Implement and verify.** Use meaningful regression evidence and required build/test workflows.
   Keep code changes narrow and independently reviewable. Preserve failure/cancellation/cleanup behavior
   in the verification plan. A green existing suite does not replace a test that exercises the defect.
4. **Perform the required device work.** Define an explicit operation/device/firmware/transport matrix
   for affected claims. Coordinate exact commands and known device/PIN state under repository hardware
   rules. Record attempted, executed, passed, failed, blocked, skipped and not-run separately. A test
   blocked during setup did not execute its ceremony. Linux evidence does not establish other OS behavior.
5. **Re-review documentation against the new behavior.** Use the dependency map to reopen affected
   public XML, guides, examples, internal comments, test explanations, package/architecture and migration
   claims. Remove or update limitations only when the fix and evidence support that change. A fresh
   reviewer checks the resulting contract/prose together; sample neighboring claims for missed effects.
6. **Close each dimension explicitly.** Record implementation revision, code review, tests, device/platform
   evidence and documentation re-review separately. “Code merged” does not mean “hardware qualified,”
   and “documentation reviewed” does not mean the underlying defect is fixed. Unfinished dimensions
   remain outstanding with an owner, next action and missing evidence.
7. **Integrate and hand off.** Run the final applicable gates, commit code/docs/records coherently, verify
   the remote backup, and update the module handoff. Preserve superseded evidence and failed attempts.

For WebAuthn, the future pass must explicitly plan registration and authentication with user presence
and user verification, including applicable direct-PIN, prompted-PIN, retry/cancellation, credential
selection and extension cases. Built-in UV requires a capable device. Full selected-module ceremony
coverage and affected extension flows belong in that plan; current approval of this workflow does not
authorize unattended PIN/state-changing tests. Existing test assumptions may need correction before
their results can support broad device claims.

## Durable records and progression

Future runs live under:

```text
docs/research/documentation-audit/
  <module>.md                         current handoff and links
  modules/<module>/pass-1/<run-id>/    documentation-cycle records
  modules/<module>/pass-2/<run-id>/    implementation-cycle records
```

The OATH/WebAuthn historical workspace remains under `records/` with a byte/hash manifest. Do not move
or rewrite it merely to match the new layout. A new run references it and records the fresh baseline.

Required logical records may share files when that reduces duplication:

| Record | Required contents |
| --- | --- |
| Plan and task packets | Goal, scope, audience, pass, baseline, actor/model, required instructions, edit ownership, evidence dependencies and escalation bound |
| Progress/status | Stage, dated transitions, completed work, active assignments, blockers, decisions, next action and authoritative current checkpoint |
| Coverage | Files/symbols or semantic units, read/clean/findings/exclusion status, review/sample status and source revision |
| Findings/decisions | Stable IDs, observed/desired distinction, evidence, normative source where applicable, severity, disposition, unresolved intent and owner |
| Dependency map | Finding → implementation paths → affected documentation/examples/comments → precise re-review trigger |
| Edit/review records | Finding-to-change mapping, exact files/revision, independent verdict, required vs optional feedback, corrections and re-review outcome |
| Validation/evidence | Exact commands, outputs/logs, executed counts, warnings, device/firmware/transport/OS where relevant, and what the result does and does not prove |
| Closeout/handoff | Completed scope, outstanding items, rejected/superseded items with reasons, not-run/blocked work, verification limits, retrospective, commit and remote backup |

Use explicit statuses such as `candidate`, `accepted-doc-change`, `deferred-pass-2`, `needs-decision`,
`implemented`, `code-reviewed`, `docs-rereviewed`, and `verified-for-recorded-scope`. Keep implementation,
documentation and hardware state as separate fields; do not force them into one misleading “closed” flag.
Do not call overlapping report findings independent bugs or add confirmation of the same finding to its
count twice.

Save records as each phase completes and checkpoint before a pause. At completion, include the durable
reports in the reviewed commit and push the campaign branch. Record the resulting commit/remote hash in
the next progress entry when a file cannot contain its own eventual commit hash. Preserve evidence that
would otherwise disappear under toolchain log retention. Reports must remain usable after agent sessions,
temporary worktrees and local planning directories are gone.

## Lessons from completed cycles for the next target

- Inventory complete example blocks, not only lines mentioning the module.
- Verify the specific specification version independently, especially exceptions and optional fields.
- Read the assertions before trusting test commentary about cleanup, privacy or credential binding.
- Distinguish SDK behavior/policy from normative client/authenticator/RP requirements.
- Settle simple framework questions with isolated evidence; keep hardware/platform gaps separate.
- Preserve the approved voice while adding necessary qualifications; do not turn public guides into
  private audit transcripts or create editorial churn after factual acceptance.
- Reconcile every dependent claim after a late correction; use the map rather than memory.
- Make durable recordkeeping and the pass-2 handoff part of completion, not a later backup task.
- Review ownership of disposable inputs created inside examples, including convenience defaults, as
  carefully as session/connection scopes.
- Use actual project/reference/analyzer context for examples that depend on test or sample libraries;
  retain raw compiler diagnostics separately rather than hiding them to produce a green result.
- Mechanically verify local links and complete example blocks; a fresh reviewer can still inherit a
  mistaken path calculation or overlook an adjacent obsolete capability name.
