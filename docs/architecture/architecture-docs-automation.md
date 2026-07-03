# Architecture Documentation Automation

This lane keeps `sdk-architecture-diagrams.md` and its rendered images aligned with
the v2 (`yubikit`) source tree. It mirrors the migration-docs automation pattern:
scripts collect deterministic evidence; the AI edits only grounded documentation.

## Files

- `sdk-architecture-diagrams.md` is the human-facing Mermaid source and teaching text.
- `sdk-architecture-map.yml` maps each diagram to source files, symbols, project edges,
  and structural paths that make the diagram honest.
- `.state.yml` stores the last analyzed commit for post-merge automation.
- `manual-review.md` is created only when a human must resolve an architecture-docs issue
  before the watermark can advance.
- `images/image-manifest.txt` stores the Mermaid CLI version, source-block hashes, and
  rendered SVG/PNG digests.

## Local Commands

Validate the evidence map and rendered-image freshness/integrity:

```bash
dotnet toolchain.cs -- docs-architecture
```

Regenerate images and the manifest after changing a Mermaid block:

```bash
scripts/architecture/render-architecture.sh
```

Preview which diagrams a range can affect:

```bash
bash scripts/architecture/build-architecture-context.sh \
  --mode preview \
  --v2-branch origin/yubikit \
  --base-range origin/yubikit...HEAD \
  --output-dir .architecture-context
```

## Workflow Contract

- Same-repository pull requests run `architecture-docs-preview.yml`, which comments only. It
  does not edit files. Fork PRs do not receive AI preview comments because repository secrets
  are unavailable to `pull_request` workflows.
- Pushes to `yubikit` run `architecture-docs-update.yml`. It builds deterministic context
  on every run but invokes AI and opens a docs PR only when mapped diagram evidence changed.
- `architecture-docs-scheduler-wrapper.yml` is for default-branch scheduling. It checks out
  `yubikit` and opens PRs back to `yubikit`.
- `architecture-docs-monthly-synthesis.yml` is a polish pass for pedagogy and clarity, not
  a license to invent architecture.

The update and synthesis workflows install the pinned Mermaid CLI, render images and
manifest together only when `sdk-architecture-diagrams.md` changed, then run
`dotnet toolchain.cs -- docs-architecture` before opening a PR. The update and wrapper
workflows, not the AI prompt, advance `.state.yml` only when no `MANUAL-REVIEW` marker
exists and the range had mapped architecture impact. No-impact ranges intentionally do not
open state-only PRs. The main build workflow runs `docs-architecture` in check-only mode,
so CI verifies committed images and manifest without re-rendering nondeterministic Mermaid
output.

## Watermark Rules

The update and wrapper workflows advance `.state.yml` `last_analyzed_commit` only after
rendering (when needed), `docs-architecture`, and the manual-review marker check pass.
Monthly synthesis never advances `.state.yml`.

Do not remove a `MANUAL-REVIEW` marker until the underlying issue is resolved. Leaving the
watermark unchanged makes the next run analyze the same range again.

## Manual Review Triggers

- A changed source type appears in a diagram but is not represented in `sdk-architecture-map.yml`.
- A source change affects architecture semantics but the context builder reports no affected diagram.
- Mermaid rendering changes unexpectedly beyond the edited diagram.
- `docs-architecture` reports stale source hashes, missing images, or image-integrity drift.
