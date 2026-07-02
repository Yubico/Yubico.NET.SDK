# Architecture Documentation Monthly Synthesis Prompt

You are polishing the layered architecture diagrams for teaching/presentation quality.
You may edit ONLY `docs/architecture/**`. The workflow opens a documentation PR.

## Purpose

Incremental post-merge updates keep diagrams accurate but can erode their pedagogy over
time (drifting labels, duplicated teaching notes, boxes accreting into clutter). Monthly
synthesis restores clarity WITHOUT inventing architecture.

## Required Inputs

Read:

- `docs/architecture/sdk-architecture-diagrams.md`
- `docs/architecture/sdk-architecture-map.yml`
- `docs/architecture/.state.yml`
- `.architecture-context/context-summary.md` (if present)
- `.architecture-context/project-graph.txt` (if present)
- `.architecture-context/architecture-symbols.txt` (if present)

## Protected Properties (do NOT violate)

- **Progressive zoom.** L0 -> L1 -> L2 -> L3/L3b -> L4 each zoom one step. Keep this order
  and the "one question per diagram" framing.
- **One question per diagram.** Each diagram answers a single question; do not merge diagrams
  or overload one with multiple concerns.
- **Not an exhaustive class dump.** Diagrams are teaching tools. Do not add a box for every
  type just because it exists. If anything, prune boxes that no longer earn their place.
- **Every box that is a real type stays grep-able and mapped.** If you rename/relabel a box,
  update `sdk-architecture-map.yml` accordingly.

## Allowed Work

- Reword teaching notes for clarity; collapse duplicated notes into one canonical statement.
- Improve labels and layout for slide readability.
- Prune clutter boxes that do not change the story (and remove their now-unused map symbols).
- Fix accurate-but-stale wording spotted against the current source (project-graph/symbols).

## Forbidden Work

- Do not invent types, edges, or relationships not in the source.
- Do not add exhaustive detail or new diagrams that break progressive zoom.
- Do not edit outside `docs/architecture/**`.
- Do not edit or advance `docs/architecture/.state.yml`; monthly synthesis is not the
  watermark owner.

## After Editing

Regenerate images with `scripts/architecture/render-architecture.sh` and confirm
`dotnet toolchain.cs -- docs-architecture` passes before the PR is opened. If it cannot
pass, do not claim completion.

## Output (PR body)

1. What was clarified/deduplicated/pruned.
2. Which diagrams changed and why (pedagogy, not new architecture).
3. Confirmation that progressive zoom + one-question-per-diagram are preserved.
4. Whether images were re-rendered and `docs-architecture` passed.
5. Any map updates made to keep evidence honest.
