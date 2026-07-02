# Architecture Documentation Post-Merge Prompt

You are updating the layered architecture diagrams after changes landed on `yubikit`.
You may edit ONLY `docs/architecture/**`. The workflow opens a documentation PR; you do
not push to `yubikit` directly.

## Branch and Range Rules

- Authoritative v2 branch: `yubikit`.
- Analyze only the supplied post-merge range from `docs/architecture/.state.yml`
  `last_analyzed_commit` to current `HEAD` (normally `last_analyzed_commit..HEAD`).
- Use ONLY the deterministic context in `.architecture-context/` plus the diagrams file
  and evidence map. Do not "notice" architecture changes not represented in the context.

## Required Inputs

Read:

- `.architecture-context/context-summary.md`
- `.architecture-context/changed-files.txt`
- `.architecture-context/affected-diagrams.txt`
- `.architecture-context/architecture-symbols.txt`
- `.architecture-context/project-graph.txt`
- `.architecture-context/diff.patch`
- `.architecture-context/state.txt`
- `docs/architecture/sdk-architecture-diagrams.md`
- `docs/architecture/sdk-architecture-map.yml`
- `docs/architecture/.state.yml`

## Allowed Edits (only `docs/architecture/**`)

- `docs/architecture/sdk-architecture-diagrams.md` — update the affected Mermaid blocks and
  their teaching notes to match the changed source. Preserve the progressive-zoom structure
  and the "one question per diagram" pedagogy. Do NOT expand a diagram into an exhaustive
  class dump; add a box only when it materially changes the story.
- `docs/architecture/sdk-architecture-map.yml` — when you add/rename a real box, update its
  symbol/project/edge evidence so the resolver stays honest.
- `docs/architecture/manual-review.md` — create or update only when an item needs human
  review before the watermark can advance.

After editing Mermaid, you MUST regenerate images by running
`scripts/architecture/render-architecture.sh` (the workflow provides mmdc). Never hand-edit
rendered SVG/PNG. If you cannot render, record MANUAL-REVIEW.

## Watermark Semantics (critical)

Do NOT edit `docs/architecture/.state.yml`. The workflow owns watermark advancement after
your edits, rendering, validation, and manual-review detection.

Create or update `docs/architecture/manual-review.md` when:

- rendering fails,
- a map gap is found,
- an affected diagram cannot be confidently updated, or
- any ambiguity should block watermark advancement.

Use the literal marker `MANUAL-REVIEW` for each open item. The workflow will leave
`last_analyzed_commit` unchanged when that marker appears anywhere under `docs/architecture/`
so the range is re-analyzed. Do not rely on PR-body-only manual-review notes.

## Anti-Hallucination Rules

- Edit a diagram only for changes supported by the supplied context.
- Ground each edit in a changed symbol/edge/file; cite it in the PR body.
- Never invent types, edges, or relationships. If unsure, mark MANUAL-REVIEW and do not edit.
- Preserve existing diagram structure; make the smallest change that restores accuracy.
- Keep generated content ASCII only.
- Do not touch files outside `docs/architecture/**`.

## Output (PR body)

1. Analyzed range (from `state.txt`).
2. Impact classification: none | low | medium | high.
3. Diagrams edited and why (evidence per diagram).
4. Whether images were re-rendered and `docs-architecture` passed.
5. Manual-review items remaining (map gaps, ambiguous changes).
6. Whether any `MANUAL-REVIEW` items remain. The workflow, not you, advances
   `last_analyzed_commit` when no marker remains and validation passes.
