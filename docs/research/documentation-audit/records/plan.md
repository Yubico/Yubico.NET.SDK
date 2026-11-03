# YubiKit documentation audit plan

**Goal:** Make v2 documentation accurate, precise, readable, and compact through evidence-backed, independently reviewed changes.

**Architecture:** Pilot the complete cycle on OATH, refine the process from measured results, then expand by bounded module or reader task. Use deterministic checks for mechanical work, GPT-6 Sol 6.1 for investigation and editing, and GPT-6 Astra for difficult adjudication and selective quality checks.

**Tech stack:** C#/.NET SDK repository, existing documentation/toolchain checks, repository search, primary protocol specifications, and model-assisted review.

## Status and authority

**Next cycle selected: WebAuthn.** Prioritize its complete public API and user-facing documentation, and explicitly
audit internal comments for accuracy, unfinished-work residue, and correct specification references. Implementation
and tests supply evidence; meaningful behavior changes remain deferred. Core and its eventual audit scope are
deferred. The canonical next-cycle workflow is [webauthn-plan.md](webauthn-plan.md), which supersedes the earlier
public-only proposal. The complete cycle is committed and pushed at `9399b94b`; see
`runs/webauthn-001/closeout.md` and `status.md` for results and deferred work. Next module is unselected.

**Current user scope direction:** This campaign is documentation-first. The approved OATH implementation work, including terminal reset, is an explicit exception. Do not expand it into SDK-wide behavior changes. Small, bounded bugs may be corrected; meaningful behavior/API/lifecycle changes are deferred to a separate implementation pass, followed by targeted re-review of documentation implications. Cross-application reset/reopen consistency is specifically deferred. Record evidence, intended/observed distinctions, and affected documentation rather than changing behavior to make prose true.

- The user authorized the full WebAuthn cycle after leaving Plan mode on 2026-11-03.
- The user approved the two writing examples in `style.md`. Proposed pilot limits below are initial defaults to evaluate, not measured results.
- WebAuthn execution began from fresh commit `e353b49a`; current evidence is recorded per run.
- Repository: `/home/rupicola/Code/y/Yubico.Yubikit.NET`.
- v2 integration target: `yubikit`; never assume the repository default branch is the target.
- Read root `AGENTS.md` and canonical `CLAUDE.md`, then applicable module guidance. Repository instructions take precedence over this playbook.
- Do not overwrite or incorporate unrelated working-tree changes.

## Companion documents and context routing

- `style.md`: the editorial contract and user-approved examples.
- `roles.md`: dispatch envelope, role prompts, evidence formats, and tool guidance.
- `webauthn-plan.md`: selected module scope, comment/citation review, improved workflow, and completion criteria.
- The coordinator reads this plan and the role catalog. Workers receive only their selected role, required repository instructions, their task envelope, and relevant evidence.
- Do not send the full conversation or all role prompts to every worker.
- These are planning documents containing reusable prompts, not installed agent definitions. Creating executable agent files later requires the repository's agent-authoring workflow.

## Principles

1. Establish meaning before polishing disputed claims.
2. Read the entire assigned semantic scope; keyword searches alone do not constitute review.
3. Code shows implementation behavior, not necessarily intended behavior or specification compliance.
4. Do not normalize implementation defects by documenting them as intended contracts.
5. Improve readability, not simply word count. Preserve necessary rationale and qualifications.
6. API backward compatibility is not a v2 objective. Distinguish it from valid device, firmware, platform, and protocol compatibility.
7. Retain useful migration guidance without implying that removed interfaces remain supported.
8. Treat age, TODO markers, duplication, and confident wording as signals, not proof.
9. A deliberate deviation requires documented rationale or a maintainer decision; do not infer intent.
10. Record uncertainty and verification gaps explicitly. Model agreement is not evidence.

## Scope and existing infrastructure

Use `docs/live-documentation-governance.md` and the existing active-doc discovery mechanism. Do not independently redefine the active-doc boundary.

For subsequent non-Core cycles, supplement that boundary with public API XML, consumer examples/comments,
relevant current migration material, and internal explanatory comments across the selected module's source,
examples, and tests. Public documentation has priority; internal comments are an explicit semantic review target.
Implementation is traced to verify claims, not treated as permission for a design/refactoring pass. Required
agent guidance is read and reconciled where affected. Core scope is deferred.

Review generated documentation through its source inputs and prescribed generation workflow. Historical plans and research should be classified for relevance and active dependencies; do not rewrite historical records to describe the present.

Existing documented targets, to confirm against the current toolchain before execution:

- `dotnet toolchain.cs -- docs-list-active`: active documentation inventory input.
- `dotnet toolchain.cs -- docs-inventory`: generated review-signal report; may update repository files.
- `dotnet toolchain.cs -- docs-qa`: required documentation hygiene check.
- `dotnet toolchain.cs -- docs-architecture`: required for architecture changes.

These commands have not been run as part of this planning exercise. Use the build/test skills for execution and obey repository pre-commit gates. Never substitute direct `dotnet build` or `dotnet test` commands.

## OATH pilot

### Included material

- `src/Oath/src/**`: public XML documentation and internal comments, with implementation as evidence.
- `src/Oath/README.md`, `src/Oath/PACKAGE_README.md`, and `src/Oath/CLAUDE.md`.
- Documentation and explanatory comments under `src/Oath/examples/**` and `src/Oath/tests/**`, after reading any applicable nested instructions.
- OATH-specific claims elsewhere in active documentation and migration guides.
- Applicable specification sections and official vendor documentation.

Consult shared code and documentation when claims depend on them. Record cross-module findings and assign an owner instead of silently expanding the pilot to all of Core.

### Initial work sizing

- Start with one coherent reader task or API family per investigative assignment, roughly 5–10 related files where practical.
- Keep closely related declarations, implementations, tests, and prose together. Avoid arbitrary token-based splits.
- Run at most two independent investigators initially; adjust after the pilot.
- One editor owns a given file at a time. Parallel edits require isolated branches/worktrees and explicit integration ownership.
- Escalate after an initial investigation and one targeted follow-up fail to resolve a consequential question. This is an effort bound, not a requirement to stop a productive investigation early.
- Assign all remaining OATH material before calling pilot semantic coverage complete.

## Cycle

### 0. Baseline and editorial calibration

- Record branch, source commit, working-tree state, scope, and exclusions.
- Inventory tracked documentation and comment-bearing files; exclude build outputs and vendored material with recorded reasons.
- Use `style.md` as the approved baseline; do not spend a new model call renegotiating it per batch.
- Prepare task packets with expected outputs and bounded scope.

### 1. Mechanical discovery

- Identify broken local links, unresolved symbols/paths, malformed XML or fences, and stale-name candidates.
- Search unfinished-work language, temporal qualifiers, obsolete compatibility claims, repeated passages, specification references, and broad guarantees.
- Collect candidates without treating them as confirmed defects.
- Record exact commands, scope, and tool output locations. Avoid large reports copied into every packet.

### 2. Semantic investigation — Sol 6.1

- Read all assigned documentation/comments, including material without search hits.
- Identify factual, completeness, clarity, redundancy, and stale-history candidates.
- Verify substantive candidates against relevant implementation paths and available tests.
- Classify discrepancies: documentation error, implementation concern, intended-contract ambiguity, insufficient evidence, or correct claim.
- Follow cited specification claims to exact revisions/sections. Use primary sources; another SDK is corroboration, not proof.
- Return concise findings and explicit coverage. Do not edit.

### 3. Adjudication — Astra or maintainer when needed

- Escalate conflicting evidence, ambiguous protocol requirements, consequential ownership/security claims, cross-module dependencies, and unresolved intended behavior.
- Give Astra the precise question, evidence, and competing interpretations, with access to originals.
- Maintainer decisions establish intended behavior when evidence cannot. Record the decision separately from observed implementation behavior.
- Implementation defects become linked engineering work. Their documentation findings remain open or explicitly qualified until resolved.
- Deferred fixes must name the documentation surfaces to re-review when the later implementation lands. OATH authorization is not blanket permission for other modules or shared-Core redesign.
- Deduplicate shared root causes before authorizing edits.

### 4. Factual and editorial editing — fresh Sol 6.1 session

- Edit accepted findings only, within the assigned file ownership boundary.
- Correct facts and express them clearly together; remove obsolete history and needless repetition.
- Review surrounding paragraphs for coherence, not just individual replacement sentences.
- Keep necessary inline context for IDE users and local code comprehension.
- Route new factual questions back to investigation.

### 5. Independent review — fresh Sol 6.1 session

- Inspect the diff, original evidence, and full affected sections.
- Verify technical meaning, qualifiers, examples, links, and conformance statements.
- Review readability and reader-task fit against `style.md`.
- Identify lost information and unsupported new claims. Do not demand changes based only on personal stylistic preference.
- Keep substantive corrections separate from optional suggestions to avoid endless editorial churn.

### 6. Quality sampling — Astra

- Review consequential changes and a stratified sample of material marked clean.
- Start with 10% of clean semantic units, minimum three where available; include API reference, prose, and internal comments.
- Define units in the coverage ledger before selecting the sample. This is a practical check, not a statistical guarantee.
- A material miss triggers review of the affected category and adjustment of the prompt or model assignment.

### 7. Validation and integration

- Run relevant documentation checks and repository-required pre-commit/build/test gates.
- Compile runnable examples where practical. Label hardware behavior as unverified unless actually exercised or backed by identified existing evidence.
- Do not add trivial tests for prose edits. Meaningful new behavioral tests belong with the engineering work that needs them.
- Recheck actual final changes after review corrections; do not reuse stale validation results.
- Merge a narrow reviewed batch into `yubikit`; update evidence and coverage records.

### 8. Targeted follow-up and retrospective

- Revisit changed material, related claims, and the sample of clean material.
- Measure accepted/rejected findings, sampled misses, escalations, editing regressions, review effort, and actual model usage/cost when available.
- Record unavailable cost data as unknown; do not estimate prices without a source.
- Record a short process adjustment, then move to the next batch.
- Execute the selected WebAuthn cycle according to `webauthn-plan.md`: public-first, internal-comment-inclusive,
  with claim-level specification checks. Context-sized assignments do not narrow whole-module coverage. Core
  remains deferred without prescribing its scope now.

## Artifacts during execution

Agree one campaign tracking location before execution. Prefer compact machine-readable records and a short human summary; do not scatter narrative reports through published documentation.

Required logical artifacts:

- Coverage manifest: semantic units, files/symbols, source revision, stages completed, exclusions, and verification depth.
- Findings ledger: evidence, disposition, ownership, status, and dependency references.
- Validation record: commands, exit codes/results, source revision, and limitations.
- Brief retrospective: what to change in the next cycle.

The exact handoff schemas are in `roles.md`. Keep long logs and external-source extracts outside user-facing documentation. Avoid copying secrets or proprietary material into external documentation queries.

## Branch and iteration policy

- Suggested campaign branch: `yubikit-docs-audit`, based on `yubikit`.
- Submit narrow concern-based PRs and integrate frequently. Explicitly target `yubikit`.
- After merges, synchronize with the target and verify that the next diff contains only new work. After squash merges, a fresh cycle branch may be simpler.
- Follow repository stack/worktree skills if those workflows are used.
- Cache findings against source revisions and evidence dependencies. Changes to implementation, tests, specifications, or authoritative guidance invalidate affected conclusions.
- Do not rerun the whole campaign after every edit. Recheck affected dependencies and perform periodic cross-module consistency reviews.

## Completion criteria

Pilot completion requires full planned OATH semantic coverage, independently reviewed changes, applicable checks passing, and all consequential findings resolved or explicitly tracked with disposition and ownership.

Campaign completion means recorded coverage and evidence-backed claims, with no known consequential misinformation left in release documentation. Unresolved limitations must be accurately represented and owned; a ledger entry alone does not make misleading release text acceptable.

Word-count reduction is a secondary observation, never a success quota. Do not claim universal correctness or full specification conformance from sampled review or passing tests.
