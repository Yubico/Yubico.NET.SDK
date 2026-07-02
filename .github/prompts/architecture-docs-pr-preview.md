# Architecture Documentation PR Preview Prompt

You are previewing the impact of a pull request on the layered architecture diagrams
in `docs/architecture/sdk-architecture-diagrams.md`. You COMMENT ONLY. You do not edit files.

## Branch and Range Rules

- The authoritative v2 branch is `yubikit`. There is no v1 baseline for architecture docs.
- Analyze only the supplied range (normally `origin/yubikit...HEAD` for a PR).
- The deterministic context is in `.architecture-context/`. Use ONLY that context plus the
  diagrams file and the evidence map; do not infer architecture changes from intuition.

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

## What To Do

1. Read `affected-diagrams.txt`. This is the authoritative list of diagrams whose mapped
   evidence intersects the change set.
2. For each affected diagram, look at which mapped symbols/projects/paths changed
   (`architecture-symbols.txt`, `changed-files.txt`) and summarize the likely diagram impact
   in plain language (e.g. "L2/L3b: a FIDO2 backend type changed; the backend split box may
   need updating").
3. If `affected-diagrams.txt` is empty, say clearly that no mapped architecture evidence
   changed, so no diagram update appears necessary. Do not manufacture impact.
4. If you believe a diagram is affected but it is NOT in `affected-diagrams.txt`, this means
   a real diagram box is not mapped in `sdk-architecture-map.yml`. Call that out as a
   MANUAL-REVIEW item ("map gap: type X in diagram Ln is not tracked"), rather than editing.

## Anti-Hallucination Rules

- Comment only; never edit files in preview mode.
- Ground every claim in the context files; cite the changed file or symbol.
- Do not claim a diagram is wrong unless the evidence shows the underlying type/edge changed.
- Keep the comment concise and skimmable. Prefer a short table: diagram | evidence | suggested action.
- ASCII only.

## Output

Post a single PR comment:

1. Analyzed range (from `state.txt`).
2. Affected diagrams (from `affected-diagrams.txt`) with per-diagram evidence and suggested action.
3. Map gaps found (mappable boxes not in the evidence map), if any, as MANUAL-REVIEW.
4. If nothing is affected: a one-line "no architecture-diagram impact detected in this range."
