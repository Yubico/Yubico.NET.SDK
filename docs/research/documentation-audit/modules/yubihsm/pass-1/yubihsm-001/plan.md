# YubiHSM Pass 1 execution plan

**Goal:** Help consumers provision and use the YubiKey YubiHSM Auth applet with accurate contracts,
complete task guidance, verified references and durable implementation follow-ups.

**Approach:** One whole-module investigator, fresh editor, independent content/citation reviewer and
fresh final evidence reviewer using Sol 6.1; Astra coordinates, adjudicates, samples and executes gates.
Independent tooling preparation may run alongside investigation. Match record volume to this module.

**Stack:** .NET 10 / C# 14, SmartCard YubiHSM Auth, HsmAuthTool, xUnit v3 unit/v2 integration tests.

## Authority and baseline

User approved this plan and execution, explicitly requesting subagents. Baseline is
`a74792facb657413e98223e059876d8b213ffff3`, branch `yubikit-docs-audit`. Initial tracked tree is clean;
four original untracked files remain user-owned. Prior 2,317 campaign artifact identities are preserved.
Planning scout `ses_e59ae7b1effel7yAYo2KUWeQ9Q` supplied preliminary 56 paths/eight public types/83 API
entries; execution independently reconciles these counts. Root/module/test instructions and campaign
workflow.md govern. Complete campaign Pass 1 before Pass 2; PIV late, Core last with boundary reviews.

User selected **YubiKey-only hardware verification**. Existing authorization for tests on the connected
dedicated test key carries forward. Actual HSM/connector interoperability stays unverified. Reidentify
device/applet version and readable state before mutation, coordinate physical touch, serialize all
toolchain/device operations, and perform explicit final applet cleanup/readback. Existing integration
helper resets before each body and has no post-test reset: inspect all setup/body/cleanup effects.

## Editorial acceptance

Every investigator/editor/reviewer must read records/style.md. Write direct, calm, precise, approachable
explanations, preserving useful rationale and conditions. **Prefer affirmative, task-oriented wording:**
explain what happens and name the responsible actor. Use a negative statement only for a consequential
limitation or likely misconception; pair it with the supported action where possible. Avoid repeated
“does not”, “cannot”, emphatic “NOT” and rhetorical “X, not Y” framing. This approved supplement applies
to API documentation, guides, source/example/test comments and review acceptance.

## Phases

1. **Baseline — Astra.** Hash inventory, original user work and prior manifests; reconcile all 56 paths:
   10 library C#, 22 example C#, 12 test/helper C#, four Markdown, four projects, two API/two runner files.
   Effective-public inventory accounts for generated record members, extensions and interface duplication.
2. **Investigation — Sol.** Read complete semantic units and relevant active shared-document sections.
   Cover provisioning/enrollment, host/HSM actor/context/cryptogram responsibilities, password encoding,
   retries/recovery, applet/device/development versions, ownership, presence/cancellation, parsing and
   real example/test effects. Nominate clean units; classify historical/generated material explicitly.
3. **Sources and triage — Sol/Astra.** Verify original title/revision/status/section/actor/strength and
   applicability. Pin vendor implementations as corroboration rather than normative authority. Initial
   research plus one focused follow-up, then explicit gap/decision. Stable HSM-IMP-* and HSM-DEP-* IDs
   bind observations to affected docs and precise reopening triggers. Runtime and assertion fixes wait
   for Pass 2; Core dependencies remain record-only.
4. **Fresh editing — Sol.** One owner for module docs/comments and assigned shared YubiHSM sections.
   Improve reader tasks and XML together, with complete enrollment/resource scopes, typed prerequisites,
   truthful limitations and example classifications. Preserve executable bodies/help strings/assertions/
   API snapshots. Read docs-module before changing module README/CLAUDE files.
5. **Independent review — fresh Sol.** Review complete affected sections, assertions and consequential
   original sources. Required factual/usefulness/style corrections versus optional preference; editor
   corrects and reviewer rechecks exact hashes. Reconcile late hardware implications across dependencies.
6. **Sampling — Astra.** At least 10% of final eligible clean semantic units, minimum three where available,
   spanning API/narrative-example/internal-test. Remove edited overlaps; material miss expands category.
7. **Validation — Astra.** Compile meaningful full documentation groups and entire 22-source example tool
   with genuine references/analyzers and actual Csc provenance. Use collision-free tooling identities;
   capture sample metadata late with normal sample outputs. Scope formatter to actual edited solution
   files; prove token/directive preservation with exact harmless import accounting; compare all APIs.
   Clean solution build/unit/docs-package/architecture/complexity gates, zero compiler/analyzer warnings.
   Inspect/classify all 13 integration methods; execute applicable YubiKey-only cases and bounded useful
   probes (independent key calculation, password transitions, actual CALCULATE presence/cancellation).
   Preserve rejected/skipped/blocked/not-run separately; final cleanup/readback verifies its actual scope.
8. **Final record review — fresh Sol.** Reconcile scope/findings/acceptance hashes, sampling, raw compiler/
   test/device evidence, failures, prior preservation and authoritative current status. Compact evidence
   review rather than repeated whole-module investigation.
9. **Publication — Astra.** Resolve required corrections, freeze manifest, explicitly stage owned paths,
   commit/push branch and verify remote HEAD. External handoff records publication after archive freeze.

## Records and boundaries

Run directory: modules/yubihsm/pass-1/yubihsm-001; external handoff: documentation-audit/yubihsm.md.
Preserve plan/progression/ownership, complete coverage, findings+dependencies, edits/reviews/corrections,
original sources/failures, validation/hardware results and closeout. Related records may share files.
Capture logs before toolchain rotation; retain unique source PDFs if needed with explicit identities;
exclude generated binaries/caches. Prior finalized runs remain immutable. Target existence checks and
genuine build references suffice for unrelated modules; dedicated Core investigation/editing is deferred.
Documentation completion, implementation completion and hardware qualification stay separate.
