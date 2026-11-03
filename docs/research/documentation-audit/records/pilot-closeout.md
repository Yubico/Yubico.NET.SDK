# OATH documentation pilot closeout

**Status:** The pilot's scanning, accepted documentation edits, independent reviews, and final validation are complete. Implementation and contract closeout remain open; this is not a release sign-off.

**Subsequent scope decision:** The user approved OATH fixes as an explicit exception, then reaffirmed a documentation-first campaign. The hybrid scheduling recommendation below is historical, not current authorization: defer meaningful behavior/API/lifecycle changes in later audits to a separate fix pass and documentation-impact re-review. Small bug fixes must remain bounded. Cross-application reset consistency is specifically deferred. See `implementation-plan.md` and `implementation-followups.md` for current progress; counts and unresolved states below describe their recorded checkpoints.

## Coverage and outcome

- 38 tracked local Markdown/C# surfaces and 23 external files received scoped OATH semantic review. See [coverage index](coverage-summary.md) and per-run ranges.
- Four investigative units produced 45 distinct finding records, including coordinator additions. Findings are not all implementation defects: the ledgers distinguish incorrect prose, missing contracts, stale history, implementation concerns, and unresolved intent.
- Accepted documentation work proceeds independently of deferred engineering, but dependent release claims remain explicitly open.
- All three edit batches passed fresh independent reviews and repository gates. Final validation: full solution build, 3,496 executed unit tests with zero failures/five skips, documentation QA, architecture evidence/image checks, and scoped formatting passed with zero build/test warnings. Complexity reported only unchanged existing debt.
- At the documentation checkpoint, 21 tracked files changed: documentation/comments plus removal of two unused imports and EOF normalization in one edited file. That checkpoint left executable statements, public signatures, test assertions, and CLI runtime strings unchanged. Its hashes and diff are saved in `pilot-content.json` and `pilot-diff.patch`; subsequent upstream integration is recorded separately below.
- No hardware integration or consumer-package installation verification was performed. Passing current unit tests does not establish missing response/failure-path behavior.

## What worked

1. **Sol for bounded reasoning, not just pattern discovery.** It traced contracts to source and test evidence, found real protocol/ownership mismatches, and reported uncertainties rather than filling gaps with invented rationale.
2. **Fresh editing and review sessions.** Editors received accepted facts and local style examples; reviewers inspected originals. This kept investigation narratives out of user-facing prose and preserved unresolved boundaries.
3. **Astra on consequential claims and selective samples.** Coordinator checks added missed table/absolute-claim corrections and distinguished actual SDK gates from unsupported earliest-device claims.
4. **Separate implementation follow-ups.** Deferring code changes kept documentation edits coherent. It also exposed which documentation units will need another pass after engineering fixes.
5. **Explicit source-state records.** Per-batch snapshots, patches, hashes, and archived gate results let us distinguish substantive changes from EOF-only formatting corrections.

## Process changes for the next module

### Retrospective after the approved OATH follow-up

- **Cycle position:** OATH baseline review, documentation edits, independent review, validation, and targeted
  post-fix documentation re-review are complete. Open engineering/provenance questions have explicit deferred
  dispositions. We are at the handoff to the selected WebAuthn documentation/comment cycle, not an implementation wave.
- **Keep:** source-backed claims, calibrated prose examples, narrowly scoped Sol investigation/edit/review roles,
  Astra adjudication, and a written dependency trail. Fresh reviews caught meaningful omissions in both code and
  prose, including the final three cross-document contract mismatches.
- **Change:** enforce the documentation-first boundary at triage. OATH's authorized fix waves substantially
  extended the pilot; they are not the default template for subsequent modules. Meaningful implementation work
  should have its own later pass and trigger targeted documentation re-review when it lands.
- **Cadence:** complete one coherent reader task/API area, report documentation findings and deferred engineering
  separately, review and validate the prose, then move on. Avoid reopening a completed module to pursue every
  remaining protocol or device question.
- **Evidence economy:** reuse pinned references and recorded source dependencies. Hardware experiments belong
  only where a specific documentation question requires them; counts and model agreement do not prove contracts.
- **Next scope (updated by user steering):** WebAuthn is selected. Prioritize whole-module public APIs and user
  documentation, while also auditing internal explanatory comments, unfinished-work residue, and specification
  citations. Code remains evidence, not an automatic fix assignment. Core and its eventual scope, CORE-001, and
  cross-application reset changes are deferred. The canonical cycle plan is [webauthn-plan.md](webauthn-plan.md).

The branch and compact handoff are backed up on GitHub through `e353b49a`. Detailed logs and campaign records
remain in the local audit workspace. Model usage/cost data were not available, so no quantified efficiency claim
is made.

- Check table value associations against definitions and absolute claims against sibling-operation exceptions explicitly.
- Label claims as device requirements, SDK validation/policy, supported workflow, or intended engineering rules. Do not equate implementation checks with physical minimum firmware.
- Verify documented output layout against protocol-shaped test fixtures; fake responses can repeat the implementation's misconception.
- Record failure boundaries for multi-exchange operations and ownership across preprocessing helpers, not only the happy path.
- Give editors `.editorconfig` and scoped formatting checks up front. Distinguish newly introduced formatting from baseline hygiene in touched files.
- Reuse pinned source/specification evidence packets rather than repeatedly fetching broad pages. Retain the original URL/section and revision or access-date limitation.
- Keep role context selective, but preserve mandatory repository instructions. Larger coherent units can reduce repeated context loading when the same implementation/test files serve several closely related claims.
- Actual per-task model usage/cost was not returned by the dispatch tool. Obtain it where available in future cycles; do not infer savings or price totals from model names alone.

## Implementation scheduling recommendation

Use a **hybrid policy** for future audits:

- A source-confirmed implementation defect that blocks truthful documentation or has consequential behavior can move to an independently owned fix task after triage, while unrelated documentation review continues.
- Keep fixes in isolated worktrees/branches when they would overlap audit files. Integrate and revalidate the affected documentation unit after the fix; do not let an editor normalize the defect in the meantime.
- Defer nonblocking cleanup and unresolved product/firmware decisions to a bounded closeout queue.
- Reopen only claims whose source dependencies changed, plus a small independent sample; avoid rerunning the whole module audit for every fix.

For this pilot, keep the user's agreed deferral policy until the closeout discussion. [Implementation follow-ups](implementation-followups.md) contains 14 proposed implementation tasks, seven contract/provenance decisions, and one Linux setup-reference follow-up. Some are linked facets of the same root cause; this is not a claim of 22 independently reproduced runtime defects.

## Suggested fix order for discussion

1. Command help must not enter destructive reset, and option arity must not alter positional meaning (IMP-012/013).
2. Remaining failure-cleanup regression evidence and Base32 validity (IMP-004/005), followed by creation ID/numeric boundaries with the required policy decisions. IMP-003 was subsequently closed by the upstream ownership fix.
3. Full-HMAC return shape, required SELECT/LIST/aggregate response validation, and response cleanup (IMP-001/008/009/011/014).
4. Reset partial-success recovery and consistent sample cancellation (IMP-010/002).
5. Resolve remaining key-length, URI, timestamp, HOTP, padding, and secure-channel provenance questions; update dependent documentation after the decisions.

No new implementation task above was dispatched as part of this docs pilot. On the user's subsequent request, the documentation was committed as `7c4153f8` and upstream `089b2393` was imported in merge `361567eb`. That import closes IMP-003 and corrects IMP-004's code paths while leaving a failure-cleanup regression gap. All other implementation/contract dispositions remain open. No push was performed; the user's original untracked files are preserved.

See [upstream reconciliation](runs/upstream-001/reconciliation.md) before selecting further work. Post-merge validation passed with 3,684 executed unit tests (167 OATH), five skips, zero failures and zero warnings. The original pilot counts/source hashes above remain historical checkpoint evidence.
