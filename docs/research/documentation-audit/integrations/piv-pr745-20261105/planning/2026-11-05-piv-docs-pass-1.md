# PIV Documentation Pass 1 Implementation Plan

**Goal:** Complete the established whole-module documentation audit against PIV after PR #745 has merged into `yubikit`, with accurate consumer tasks and durable implementation follow-ups.

**Architecture:** A merge gate and explicit user release precede any audit execution. Fresh Sol 6.1 sessions investigate, edit and independently review; Astra coordinates, adjudicates, samples, validates and publishes. Documentation completion, implementation completion and hardware qualification remain separate outcomes.

**Tech Stack:** .NET SDK repository, PIV SmartCard application, repository toolchain, pinned original specifications and Python YubiKey Manager evidence.

---

## Current status — 2026-11-05

**APPROVED / BLOCKED ON PR #745. No PIV Pass-1 investigation, editing, tests or hardware work has started.**

**Approval update:** The user subsequently switched to execution mode and twice confirmed: “Once the plan and the wait for #745 is ready, you are approved to go.” This supplies the release authorization below. Begin after verifying merge/containment and establishing the integrated baseline; no second approval is required. Physical-touch timing remains coordinated.

This planning packet lives in `/home/rupicola/.opencode/plan`. Repository campaign records will be created after the upstream gate clears. The companion watch plan and activation receipt record the scheduler's actual status.

Read-only observation from GitHub:

- Repository: `Yubico/Yubico.NET.SDK`.
- PR: <https://github.com/Yubico/Yubico.NET.SDK/pull/745>.
- Title: `refactor(piv)!: unify key vocabulary and session operation boundaries`.
- State: `OPEN`; draft: false; base: `yubikit`.
- Head branch: `yubikit-piv-session-consistency`.
- Observed head: `b0c7c638ed3b7addf6d52f034a8345c71c02643b`.
- `mergedAt` and `mergeCommit`: null; PR `updatedAt`: `2026-11-04T21:41:20Z`.

These are a dated observation, not a pinned future audit baseline. The other agent may push more changes. Do not review the present PIV source as the final audit target or use the present PR head as the eventual merge identity.

Campaign checkout observed at `0c301252d7e8abfd1e84cb0cbc0ce17925f0b963`, branch `yubikit-docs-audit`, with a clean tracked tree. Four original untracked files remain:

- `docs/research/audit-v2-closeout-review.md`
- `docs/research/audit-v2-execution.md`
- `docs/research/audit-v2-jira-traceability.md`
- `mise.toml`

## Companion plans

- [Delegation packets](2026-11-05-piv-docs-delegation.md)
- [PR gate and recurring-check setup](2026-11-05-piv-pr745-watch.md)

Repository paths below are relative to `/home/rupicola/Code/y/Yubico.Yubikit.NET`.

## Authority and required reading

Re-read the versions present in the **post-merge audit checkout**, since PR #745 may update instructions and vocabulary:

1. `AGENTS.md` and canonical root `CLAUDE.md`.
2. `src/Piv/CLAUDE.md`, `src/Piv/README.md`, and any applicable nested/test instructions.
3. `docs/research/documentation-audit/workflow.md` — canonical two-pass workflow, evidence, sampling and publication.
4. `docs/research/documentation-audit/records/style.md` — approved prose voice and examples.
5. `docs/SDK-HOUSE-STYLE.md` — SDK design/contract conventions; use alongside the prose reference.
6. `docs/TESTING.md`, `docs/COMMIT_GUIDELINES.md`, and applicable build/test/verification skills when their phases begin.
7. `docs/research/documentation-audit/openpgp.md` and the frozen OpenPGP closeout, validation, example-validation and corrections records for transferable lessons.

Every assignment must explicitly require **affirmative, task-oriented wording**: lead with behavior, name the actor, preserve useful rationale and nearby firmware/transport/lifetime conditions. Use negatives for consequential limitations or likely misconceptions, paired with a supported action. Avoid repeated “does not,” “cannot,” emphatic “NOT,” and rhetorical “X, not Y.” Optimize comprehension rather than minimum word count. Keep public prose free of private audit chronology.

## Release gates

Audit execution requires **all** of the following:

1. A fresh successful GitHub query confirms PR #745 is `MERGED`, with non-null merge time/commit, and its base is `yubikit`.
2. The recorded merge commit is in the current `yubikit` history. A closed-unmerged PR, merge queue entry, approval or green CI does not satisfy this gate.
3. User release and execution mode are present. **Satisfied by the subsequent 2026-11-05 approval above.** A later user cancellation or changed constraint takes precedence.
4. A preserved, post-merge audit checkout contains both the campaign history and the merged PIV changes. Establish its actual HEAD, source state and GitHub backup before investigation.

Until the merge and checkout gates pass, activity is limited to planning and read-only PR monitoring. A verified merge wakes Astra to recheck the gate and establish the post-merge baseline, then execute this approved plan.

## Phase 0 — reconcile the post-merge baseline

**Owner: Astra.**

1. Capture current PR JSON, final head, merge commit/time, `origin/yubikit` and campaign HEADs. Fetch only during authorized execution; the watcher itself stays read-only.
2. Coordinate checkout/worktree ownership with the agent working on #745. Preserve unfamiliar changes and all original user files. Avoid branch switching, resets or cleanup in another agent's active checkout.
3. Inspect ancestry and choose a non-destructive integration into `yubikit-docs-audit` or an isolated audit worktree. Prefer preserving published campaign history. Do not force-push or blindly replay commits already present upstream.
4. Verify the resulting audit HEAD contains the merged PR commit and campaign baseline. Review merge resolutions, especially shared docs/API vocabulary. Keep integration changes distinct from the documentation-only diff.
5. Re-verify every prior finalized manifest, including OpenPGP's 607 artifacts as well as the earlier 2,794 identities; derive the authoritative current total from manifests. Preserve their bytes and original user-file hashes.
6. Re-read merged instructions and build a fresh inventory. Do not inherit pre-merge PIV symbol counts, method names or test counts from this plan or the previous module.
7. Record the branch/worktree, integration evidence and chosen post-merge SHA. Establish a GitHub backup checkpoint under the applicable repository gates.

Future repository records, after release and collision checks:

- `docs/research/documentation-audit/piv.md`
- `docs/research/documentation-audit/modules/piv/pass-1/piv-001/`
- Scoped campaign-index update in `docs/research/documentation-audit/README.md`.

If those paths already contain work, inspect and preserve it; choose the next run identity rather than overwrite it. Import this planning packet and monitor history into the execution records so local plan storage is not the final evidence repository.

## Phase 1 — inventory and investigation

**Owner: fresh Sol investigator; Astra adjudicates.**

Read the whole merged PIV module: effective public APIs, internal explanations, guides/package metadata, all example and test commentary, assertions, unfinished work and substantive specification claims. Reconcile partial/extension/generated/accessibility representation and API snapshots. Review #745's merged diff as an orientation map, then the whole resulting module; its prior review does not replace the documentation audit.

Provisional task families to reconcile against the merged surface:

- Discovery, session/connection ownership, management-key authentication versus PIN/PUK and protocol authentication state.
- Credential encoding/limits, retry identity, change/unblock/reset, management-key algorithms/defaults, PIN-only/protected/derived key storage and transition/recovery semantics.
- Slots, algorithms, policies, generation/import/exportability, move/delete, metadata and partial persistent mutation.
- Certificates/data objects, compression/size/encoding, storage ownership, attestation evidence and trust-chain responsibility.
- Signing, RSA padding, hashing/signature formats, decryption/key agreement, sensitive result ownership and cleanup.
- Touch/PIN policies, notification lifecycle, cancellation, drain and fresh-session recovery.
- Bio/temporary-PIN/user-verification capabilities where represented by the merged module; device applicability remains explicit.
- Firmware/family/FIPS/transport distinctions, beta applet-version observations and unsupported-capability behavior.
- Full PivTool tasks, implicit shared callback examples, migration/comparison/package claims and test-oracle accuracy.

These are investigation questions, not assertions about implemented behavior. Keep the cycle PIV-focused; record precise Core dependencies for the final Core cycle instead of launching a cross-module boundary review.

## Phase 2 — source authority and triage

Verify applicable original PIV specifications and vendor extensions. Candidate families include FIPS 201 and NIST SP 800-73/800-78; determine the exact relevant revision, publication status, part, section, actor, strength and applicability before making claims. A newer publication does not automatically describe this SDK or a particular device profile.

Pin Python YubiKey Manager source, tests and relevant commit history for uncertain behavior and firmware-check origins. Distinguish sibling implementation evidence, vendor product/profile statements and normative requirements. Retain original URLs, access dates, revision hashes and focused passages; independently recheck consequential citations during review.

Use initial research plus one focused follow-up, then record a precise evidence gap or decision. Assign stable `PIV-C-*`, `PIV-IMP-*`, and `PIV-CORE-*` identities as appropriate. Maintain finding → implementation paths → dependent public/shared/internal/test prose → exact reopening trigger. Separate documentation dependencies from engineering-group counts. Reconcile historical PIV findings against #745 before carrying them forward; preserve resolved/superseded provenance.

## Phase 3 — fresh editing and independent review

1. Astra accepts the documentation scope and deferred findings.
2. Fresh Sol editor owns the module prose and explicitly assigned PIV shared sections. Improve task flow, contracts, complete examples, internal rationale and assertion-accurate test comments together.
3. Preserve runtime bodies, assertions, help strings, directives and API snapshots. Documentation and descriptive metadata changes remain distinct from runtime fixes. A breaking next release removes compatibility as a design constraint; it does not authorize implementation changes in this sweep.
4. Fresh Sol reviewer reads full affected units and consequential original sources. Require factual/usefulness/style corrections, preserve optional preferences separately, and re-review exact corrected hashes.
5. Astra samples at least 10% of eligible clean semantic units, minimum three where available, spanning API, narrative/examples and internal/test explanations. Expand the affected category after a material miss. Reconcile edited overlaps before the final denominator.
6. Reopen all dependent claims after late hardware or citation evidence. Record current authoritative status while retaining earlier reports as history.

## Phase 4 — validation and device work

**Astra owns execution; preparation can run independently under the delegation plan.**

- Serialize toolchain and hardware jobs. Use `domain-build` and `domain-test` skills with the repository toolchain; recheck current commands rather than calling `dotnet build/test` directly.
- Compile complete meaningful examples and the entire actual PivTool with real references, analyzers, typed dependencies and full resource scopes. Retain actual Csc/source/config/assets evidence immediately after each real compilation. Use unique audit project names and ordinary tool bin/obj layout.
- Carry forward OpenPGP's exact analyzer-package lesson: verify actual analyzer participation and pinned imports; a disabled duplicate SDK analyzer selector alone is not skipped analysis. Preserve failures and corrections without weakening warning/no-skip/membership checks.
- Scope formatting to edited C# paths. Verify executable tokens/directives/imports and every API snapshot against the **post-745 baseline**. Account explicitly for any harmless formatting/import change and Git line-ending normalization.
- Run final clean solution/unit/documentation-package/architecture/complexity gates and require zero compiler/analyzer warnings or failures. Record actual counts and existing skips from the new baseline.
- Establish device identity and PIV state afresh before mutation. Serial 103 and its beta metadata are historical context, not a future selection guarantee. Record PIN/PUK attempts, management-key mode/available knowledge, slot keys/certificates, policies, attestation identity and applicable metadata without logging secrets.
- Existing dedicated-key authorization covers routine resets/configuration/tests/ad hoc tools. Coordinate physical touch or insertion/removal timing. Inspect every integration method's setup/body/cleanup and early returns; distinguish attempted, reached, passed, failed, skipped and blocked.
- Inspect reset/recovery effects and preserve existing attestation material. Avoid irreversible policy choices or attestation-key replacement merely to increase coverage. Identify any non-restorable initial state before changing it, and record its actual recovery scope.
- Use bounded supplemental probes only for consequential claims: independent host signatures/plaintext/agreement, certificate/key binding, error/retry identity, partial mutation and cancellation/recovery. Run same-key vendor controls before cleanup when necessary to preserve comparability.
- Wait for operations to finish before cleanup. Record cancellation request and terminal timings separately; verify fresh-session recovery before reset where that is the claim. Device/OS evidence qualifies only its measured scope.
- Finish with independent cleanup and fresh public-state readback. Restore reversible settings outside reset's scope explicitly. Report exact initial/final differences and private-state observability limits rather than equating a reset return with restoration.

## Phase 5 — closeout and publication

1. Reconcile whole-module coverage, exact accepted hashes, clean sampling, complete-example membership, current implementation limitations and device state.
2. Preserve full logs before toolchain rotation, original failed attempts, source evidence, compiler inputs and current live dependency checks. Keep generated binaries/caches outside the frozen record set; register any retained primary-source binary with exact identity.
3. Fresh independent Sol evidence reviewer checks the final package and raw outcomes, separate from the content reviewer.
4. Resolve required corrections, run final record/link/manifest/preservation checks, then freeze the run.
5. Explicitly stage only owned paths; verify index bytes, with narrowly documented repository-driven normalization where needed. Commit/push the campaign branch and verify remote HEAD.
6. Update the external handoff/publication checkpoint after freeze. Preserve all prior manifests and user work. Pass 2 remains deferred until the first sweep finishes; Core remains the final target.

## Acceptance and next action

PIV Pass 1 is complete only after every in-scope unit has a disposition, accepted edits are independently reviewed, applicable examples/citations/gates have evidence, implementation issues have durable reopening records, device outcomes are scoped honestly, and the records are committed and backed up.

**Next action now:** arm the read-only recurring watcher and wait for #745. On verified merge, stop the watcher, recheck current approval/merge evidence, establish the post-merge baseline and proceed under the user's standing approval.
