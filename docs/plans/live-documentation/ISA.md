# Live V2 Documentation System — Program ISA

<!--
Program ISA (E4). Long-lived source of truth for the "living documentation" effort.
Phase 1 = self-maintained visual architecture documentation.
Later phases = broader documentation governance (README, docs/*, staleness reconciliation).
This file lives under docs/plans/ and is intentionally excluded from docs-qa.
-->

---
task: "Establish a self-maintaining live documentation system for the v2 SDK, starting with visual architecture documentation"
slug: live-documentation-system
project: Yubico.NET.SDK (yubikit v2)
effort: E4
effort_source: explicit
phase: observe
mode: interactive
started: 2026-07-02
authoritative_branch: yubikit
working_branch: yubikit-live-docs-automation
---

## Problem

The v2 SDK (`yubikit` branch) already has one working "living documentation" lane: the
v1-to-v2 migration docs automation (merged via PR #513). It uses a proven, boring-where-it-matters
shape — a deterministic Bash context builder (`scripts/migration/build-migration-context.sh`)
feeds evidence to an AI action, which opens documentation-only PRs and advances a `.state.yml`
watermark so the same commits are never re-analyzed.

Everything else the SDK ships as documentation is **static and drifts**:

- The new layered architecture diagrams (`docs/architecture/sdk-architecture-diagrams.md` + rendered
  SVG/PNG) are hand-authored. They are accurate today but have no mechanism to detect drift when
  Core/session/backend/transport types change. The file even hardcodes `Branch: yubikit-consolidation`
  in its header — already stale the moment it merged to `yubikit`.
- `README.md`, `docs/*` active docs (`docs/architecture/**`, `docs/usage/**`,
  `docs/troubleshooting/**`, top-level `docs/*.md`, module `README.md`/`CLAUDE.md`) are validated
  only for **hygiene** by `docs-qa` (balanced fences, resolvable links, no known-stale trait
  examples). `docs-qa` does not detect **semantic** staleness — a doc can pass every check while
  describing an API that no longer exists.
- There is no inventory of which active docs exist, when they were last touched relative to the code
  they describe, or which are likely stale. "Clean out the closet" is currently a manual guess.

Without a system, architecture diagrams will rot exactly like every other static doc, and the
migration-docs automation will remain a lonely island rather than one lane of a coherent doc-governance
model. The lead developer (also the presenter of this SDK) needs the docs to stay true with minimal
manual authoring.

## Vision

A maintainer merges a change to `yubikit` that adds a new FIDO2 backend. Within one CI run, an
automation PR appears that says: "L2 and L3b architecture diagrams are likely affected — `IFidoBackend`
gained an implementation; here is the evidence and a proposed diagram edit," with the Mermaid re-rendered
to SVG/PNG and `docs-qa` green. The maintainer reviews a small, evidence-backed diff instead of
remembering to hand-edit diagrams. A monthly synthesis PR keeps the diagrams pedagogically clean
(progressive zoom preserved, one question per diagram) rather than a pile of patches. A quarterly (or
on-demand) documentation-inventory report flags which static docs have drifted from the code they
describe, so "cleaning the closet" is a ranked worklist, not a guess. The migration-docs lane and the
architecture lane share one mental model, one context-builder pattern, and one governance doc.

## Out of Scope

- **No mass rewrite of existing docs in the first implementation PR.** The documentation inventory and
  staleness report are a *Phase 2* deliverable and are report-only when they land; actual doc cleanup is
  later, focused, human-reviewed work. Phase 1 ships architecture-diagram automation only.
- **No fully autonomous doc merges.** All automation opens PRs; nothing pushes directly to `yubikit`.
  Humans review every documentation change.
- **No new documentation website, hosting, or portal.** This is repo-resident Markdown + rendered
  images + CI, matching the migration-docs precedent.
- **No replacement of `docs-qa`.** The existing hygiene gate stays; this system adds semantic/drift
  and render/verify lanes beside it, not instead of it.
- **No AI-generated architecture prose from scratch.** Deterministic tooling extracts architecture
  facts; AI edits the teaching layer and diagrams against that evidence. The AI must not "notice
  architecture" from intuition.
- **No API-reference doc generation** (DocFX/XML-doc site). The published API reference at
  docs.yubico.com is a separate concern.
- **No changes to the migration-docs automation's behavior** beyond, at most, refactoring shared
  helpers if a shared context-builder library emerges — and only if it does not alter migration output.
- **No CLI/test/example project diagrams.** Architecture docs stay focused on shipped assemblies.

## Principles

- **Deterministic scripts find evidence; AI writes prose.** Correctness-bearing extraction (project
  graph, type presence, changed public surface) is mechanical and reproducible. AI is confined to the
  teaching/diagram layer, grounded in that evidence.
- **Documentation lanes are one system, not many islands.** Migration docs, architecture diagrams, and
  active-doc staleness are lanes of a single governance model with shared patterns (context builder →
  AI → docs-only PR → `.state.yml` watermark).
- **Boring where correctness matters, intelligent where prose helps.** Copied verbatim from the
  migration-docs operating principle; it is the reason that lane works.
- **Every automated claim carries evidence.** A diagram-drift claim names the changed type/file/project
  ref that triggered it. Unsupported findings degrade to `manual-review`, never silent edits.
- **Diagrams are pedagogy, not exhaustive class dumps.** Progressive zoom and "one question per
  diagram" are protected properties; automation must not append every new class just because it exists.
- **The maintainer reviews diffs, not memory.** Success is measured by how little the maintainer must
  remember, not by how much automation runs.

## Constraints

- **Authoritative v2 branch is `yubikit`.** `develop` is the v1 baseline only (relevant to migration
  docs, not architecture docs). Architecture/live-doc automation triggers on `yubikit`.
- **GitHub cron runs only from the repository default branch.** The migration-docs lane already solves
  this with `migration-docs-scheduler-wrapper.yml`; any scheduled architecture/inventory job must use
  the same default-branch wrapper pattern, not assume cron works from `yubikit`.
- **Context builders are Bash** (portable across macOS, Linux, GitHub Ubuntu runners), matching
  `scripts/migration/build-migration-context.sh`. No PowerShell.
- **Diagram rendering uses `@mermaid-js/mermaid-cli` (`mmdc`), pinned**, installed as a CI step (CI has
  no preinstalled `mmdc`). Rendering must be reproducible.
- **Reuse the existing toolchain pattern.** New validation belongs in a bounded `toolchain.cs` target
  (mirroring `docs-qa`), invoked by CI — not duplicated as inline YAML logic.
- **All automation opens PRs via `peter-evans/create-pull-request` (pinned)** and uses
  `anthropics/claude-code-action` (pinned) exactly as the migration lane does. Documentation-only
  `add-paths`.
- **State is watermarked** in a `.state.yml` per lane so ranges are analyzed once (migration pattern).
- **Docs stay ASCII-only** in generated content, matching the migration prompt contract.
- **`docs-qa` must stay green.** `docs/architecture/**` is an active-docs root; any generated
  architecture doc must pass balanced-fence, link-resolution, and known-stale-pattern checks.
- **Stage only intended files.** Never `git add .`/`-A`; never push to `yubikit` without explicit human
  action. Signing/build discipline (`dotnet toolchain.cs`, not raw `dotnet`) unchanged.

## Goal

Stand up a repository-resident live documentation system for the v2 SDK. **Phase 1 (the current
implementation slice)** keeps the layered architecture diagrams true to the code via a deterministic
architecture-context builder, a diagram-to-evidence map, a pinned Mermaid render/verify toolchain
target, and preview/post-merge/monthly GitHub workflows modeled on the merged migration-docs
automation. **Phase 2+ (roadmap)** adds a report-only documentation inventory + staleness signal across
`README.md` and active `docs/*` and a unified governance doc. Every lane opens documentation-only PRs,
advances a per-lane `.state.yml` watermark under explicit success/failure/manual-review semantics, keeps
`docs-qa` and `docs-architecture` green, and never auto-merges or mass-rewrites docs.

## Criteria

Legend for positive ISCs: `[ ]` open, `[x]` verified.
Legend for **anti-criteria** (ISC-25+): `[ ]` = the forbidden behavior has NOT yet been proven absent;
`[x]` = a probe confirms the forbidden behavior does not occur (i.e., `[x]` means "verified safe"). An
anti-criterion is only checked once its probe demonstrates absence of the bad behavior.
Phase-1 ISCs are the current implementation slice; Phase-2+ ISCs are the program roadmap (kept here
because this is the long-lived program ISA).

### Phase 1 — Self-maintained visual architecture documentation

- [ ] ISC-1: `docs/architecture/sdk-architecture-diagrams.md` header no longer hardcodes a stale branch; it states the authoritative branch/source-of-truth accurately.
- [ ] ISC-2: `docs/architecture/sdk-architecture-map.yml` exists and maps every diagram (L0–L4, L3b) to its source evidence (project refs and/or grep-able type symbols).
- [ ] ISC-3: Every evidence symbol/path referenced in the map resolves in the current tree (probe: a checker script exits 0).
- [ ] ISC-4: `scripts/architecture/build-architecture-context.sh` exists, is `bash -n` clean, and accepts `--v2-branch`, `--base-range`, `--output-dir` (mirrors migration builder arg shape).
- [ ] ISC-5: The context builder emits, under `.architecture-context/`, at minimum: `context-summary.md`, `changed-files.txt`, `diff.patch`, `project-graph.txt`, `architecture-symbols.txt`, `affected-diagrams.txt`, `state.txt`.
- [ ] ISC-6: `affected-diagrams.txt` is derived by intersecting changed files/symbols against `sdk-architecture-map.yml` (probe: a fixture change to a mapped type lists the correct diagram; an unmapped change lists none).
- [ ] ISC-7: `docs/architecture/.state.yml` exists with `format_version`, `v2_branch: yubikit`, and `last_analyzed_commit`.
- [ ] ISC-7.1: Watermark advance semantics are defined and enforced in the post-merge prompt: advance `last_analyzed_commit` to analyzed HEAD ONLY when (a) no diagram was affected, or (b) affected diagrams were successfully edited AND re-rendered AND `docs-architecture` is green. Do NOT advance when render fails or a finding degraded to `manual-review`; instead leave an unresolved marker so the range is re-analyzed (probe: prompt text encodes all three cases).
- [ ] ISC-7.2: When a finding is `manual-review` or render fails, the workflow records it (changelog/PR body) and does not silently advance the watermark (probe: prompt + workflow inspected; no unconditional advance step).
- [ ] ISC-8: `toolchain.cs` exposes a bounded `docs-architecture` target that (a) verifies every Mermaid block renders under pinned `mmdc`, (b) verifies every image referenced in the diagrams file exists, (c) verifies the rendered-image count matches the diagrams table, (d) verifies the evidence map resolves, (e) proves image freshness (see ISC-29 mechanism).
- [ ] ISC-9: `dotnet toolchain.cs -- docs-architecture` exits 0 on the current tree.
- [ ] ISC-10: A pinned `@mermaid-js/mermaid-cli` version is recorded (workflow install step and/or a pinned manifest) so renders are reproducible.
- [ ] ISC-11: `.github/prompts/architecture-docs-pr-preview.md` exists and instructs comment-only, evidence-grounded, no-file-edit behavior (mirrors migration preview prompt).
- [ ] ISC-12: `.github/prompts/architecture-docs-post-merge.md` exists and constrains edits to `docs/architecture/**`, requires evidence, forbids invented architecture, and advances `.state.yml`.
- [ ] ISC-13: `.github/prompts/architecture-docs-monthly-synthesis.md` exists and protects progressive-zoom/one-question-per-diagram pedagogy while collapsing duplication.
- [ ] ISC-14: `.github/workflows/architecture-docs-preview.yml` triggers on PRs to `yubikit`, builds architecture context, and runs comment-only preview (pinned actions).
- [ ] ISC-15: `.github/workflows/architecture-docs-update.yml` triggers on push to `yubikit`, builds context, renders diagrams, and opens a docs-only PR limited to `docs/architecture/**` (pinned `peter-evans/create-pull-request`).
- [ ] ISC-16: `.github/workflows/architecture-docs-monthly-synthesis.yml` exists for monthly/on-demand synthesis, opening docs-only PRs.
- [ ] ISC-17: Scheduled architecture jobs use the default-branch scheduler-wrapper pattern (documented and/or a wrapper workflow), not a bare cron on `yubikit`.
- [ ] ISC-18: The post-merge workflow re-renders SVG + PNG for changed diagrams and stages the regenerated images alongside the Markdown edit.
- [ ] ISC-19: A short operator/handoff doc explains the architecture lane end-to-end (trigger, context, prompts, state, render), mirroring `migration-docs-automation-handoff`.

### Phase 2 — Documentation inventory & staleness (report-only first)

- [ ] ISC-20: `scripts/docs/build-docs-inventory.sh` enumerates active docs (root `*.md`, `docs/*.md`, `docs/usage/**`, `docs/troubleshooting/**`, `docs/architecture/**`, module `README.md`/`CLAUDE.md`) using the same boundary as `docs-qa`.
- [ ] ISC-20.1: The inventory's active-doc set is verified to equal the set `docs-qa` validates (probe: a check compares the inventory enumeration against `toolchain.cs` `DiscoverActiveDocumentationFiles()` / `IsArchivedOrPlanningDoc()` output; symmetric difference is empty). If the boundary is duplicated in Bash, it is asserted equal, not assumed.
- [ ] ISC-21: The inventory records, per doc: path, last-commit date, and a drift signal (e.g., referenced-symbol resolution rate against the current tree).
- [ ] ISC-22: A staleness report ranks likely-stale active docs with evidence; it is report-only (no edits) in Phase 2.
- [ ] ISC-23: `README.md` "Documentation" section is reconciled to reality (verified links; no references to removed structure) as a focused, human-reviewed PR — not part of the automation's first run.
- [ ] ISC-24: A governance doc (`docs/architecture/` or `docs/`) describes the unified living-documentation model covering the migration, architecture, and inventory lanes and how they share the context→AI→PR→state pattern.

### Anti-criteria (all phases)

- [ ] ISC-25: Anti: any live-docs workflow pushes directly to `yubikit` (probe: every workflow uses PR creation, no direct push to the protected branch).
- [ ] ISC-26: Anti: the first architecture-automation PR rewrites more than `docs/architecture/**` (probe: `add-paths` scoped to architecture; no `README`/other-docs edits in the automation PR).
- [ ] ISC-27: Anti: an architecture diagram edit lands without a corresponding evidence entry (probe: post-merge prompt requires evidence; unsupported → manual-review).
- [ ] ISC-28: Anti: `docs-architecture` or `docs-qa` is red after any generated change (probe: both targets exit 0 in CI before PR is opened).
- [ ] ISC-29: Anti: rendered images drift from Mermaid source. Mechanism: `docs-architecture` renders each Mermaid block to a temp file and compares against the committed image by content hash (normalized to ignore nondeterministic mmdc metadata such as embedded ids/timestamps), OR embeds a source-hash provenance comment in each image and verifies it matches the current block hash. Existence/count checks alone are insufficient (probe: mutate a Mermaid block without re-rendering → `docs-architecture` exits non-zero).
- [ ] ISC-30: Anti: the diagrams collapse into an exhaustive class dump (probe: monthly-synthesis prompt forbids per-class expansion; diagram count/zoom-levels remain bounded).
- [ ] ISC-31: Anti: a scheduled job is assumed to run from `yubikit` without a default-branch wrapper (probe: scheduling documented via wrapper pattern).

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | content | Diagrams header states correct authoritative branch/source | no stale branch string | Read/Grep |
| ISC-2 | file | Map file exists, one entry per diagram | L0,L1,L2,L3,L3b,L4 covered | Read |
| ISC-3 | script | Evidence resolves in tree | exit 0 | Bash (checker) |
| ISC-4 | script | Builder exists + `bash -n` + arg parsing | exit 0 | Bash |
| ISC-5 | script | Context files emitted | all required present | Bash + ls |
| ISC-6 | fixture | Mapped change → correct affected diagrams | exact match | Bash (fixture) |
| ISC-7 | file | `.state.yml` fields present | 3 keys | Read |
| ISC-7.1 | prompt | Watermark advance/hold/manual-review cases encoded | 3 cases present | Read/Grep |
| ISC-7.2 | prompt+YAML | No unconditional watermark advance | absent | Read |
| ISC-8 | toolchain | `docs-architecture` performs 5 checks incl. freshness | present | Read `toolchain.cs` |
| ISC-9 | command | `dotnet toolchain.cs -- docs-architecture` | exit 0 | Bash |
| ISC-10 | pin | Mermaid CLI version pinned | version recorded | Read |
| ISC-11..13 | file | Prompts exist with required constraints | present + constraints | Read/Grep |
| ISC-14..16 | YAML | Workflows exist, correct triggers, pinned actions | source-inspected valid | Read |
| ISC-17 | design | Scheduler wrapper pattern used | documented/wrapper present | Read |
| ISC-18 | workflow | SVG+PNG regenerated + staged | render step present | Read |
| ISC-19 | file | Architecture-lane handoff doc | present | Read |
| ISC-20..22 | script | Inventory + staleness report | runs, report-only | Bash |
| ISC-20.1 | check | Inventory set == docs-qa active set | empty symmetric diff | Bash vs toolchain |
| ISC-23 | PR | README reconciled (focused PR) | links resolve | Read/docs-qa |
| ISC-24 | file | Governance doc | present | Read |
| ISC-25..31 | anti | See probes in each ISC | as stated | Read/Bash/CI |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Diagram evidence map | `sdk-architecture-map.yml` + header fix + resolver checker | ISC-1, ISC-2, ISC-3 | — | false |
| Architecture context builder | Bash builder + `.state.yml` + affected-diagram derivation | ISC-4, ISC-5, ISC-6, ISC-7 | Diagram evidence map | false |
| Render/verify target | `docs-architecture` toolchain target + pinned mmdc | ISC-8, ISC-9, ISC-10, ISC-29 | Diagram evidence map | true |
| Architecture prompts | preview / post-merge / monthly prompt files | ISC-11, ISC-12, ISC-13, ISC-27, ISC-30 | — | true |
| Architecture workflows | preview / update / monthly / scheduler-wrapper | ISC-14, ISC-15, ISC-16, ISC-17, ISC-18, ISC-25, ISC-26, ISC-28, ISC-31 | context builder, render/verify target, prompts | false |
| Architecture handoff doc | operator doc for the lane | ISC-19 | Architecture workflows | true |
| Docs inventory (Phase 2) | inventory + staleness report (report-only) | ISC-20, ISC-21, ISC-22 | — | true |
| README reconcile (Phase 2) | focused human-reviewed README fix | ISC-23 | Docs inventory | true |
| Governance doc (Phase 2) | unified living-docs model | ISC-24 | Architecture workflows | true |

## Decisions

- 2026-07-02: **Program ISA, not a single-task ISA.** The effort spans architecture diagrams, migration
  docs, and active-doc staleness; framing it as a program lets Phase 1 (architecture) ship first while
  keeping the broader lanes visible. Rationale: Dennis explicitly reframed the ask as "keeping all our
  documentation live," starting with cleaning the closet.
- 2026-07-02: **Model the architecture lane directly on the merged migration-docs automation** (context
  builder → claude-code-action → peter-evans PR → `.state.yml`). It is proven, in-repo, same-branch,
  and reviewer-familiar. Rationale: lowest-risk path; reuse over reinvention.
- 2026-07-02: **Diagram-to-evidence map is the anti-hallucination cornerstone.** Automation must map
  changed types/project-refs to specific diagrams rather than "sensing" architecture. Rationale: the
  migration lane's determinism is exactly what keeps it trustworthy; architecture needs the same anchor.
- 2026-07-02: **Phase 1 does not touch README or other docs.** First automation PR is scoped to
  `docs/architecture/**`; inventory is report-only. Rationale: avoid the "AI rewrote everything" failure
  mode; keep review diffs small (Anti ISC-26).
- 2026-07-02: **Merge resolution** — the 3 file-location conflicts (migration handoff/plan, previewsign
  ISA) were resolved into `docs/archive/plans/`, consistent with the manual `docs/plans → docs/archive/plans`
  rename on consolidation. Rationale: honor the intent of the rename Dennis performed.
- 2026-07-02: **Cato (GPT-5.5) audit → `concerns`, no criticals; all 5 findings accepted and fixed.**
  (1) Phase-1/Phase-2 boundary contradiction resolved — inventory is explicitly Phase 2 in Out of Scope
  and Goal. (2) Added ISC-7.1/7.2 defining watermark advance vs hold-on-failure/manual-review semantics.
  (3) ISC-29 now specifies a concrete freshness mechanism (content-hash / provenance), and ISC-8 gate (e)
  references it. (4) Added ISC-20.1 asserting the inventory boundary equals the `docs-qa` active set.
  (5) Added an explicit anti-criteria legend so `[x]` means "verified safe." Rationale: opposite-vendor
  audit caught the exact drift/hollow-check failure modes that would undermine a self-maintaining system.

## Changelog

<!-- Deutsch error-correction trail. Four-piece C/R/L entries only, added at LEARN. -->

## Verification

<!-- Populated at VERIFY with quoted command output / file evidence per ISC. Empty until then. -->
