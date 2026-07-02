# Migration Documentation PR Preview Prompt

You are reviewing migration impact for a pull request targeting the `yubikit` branch.

## Branch and Range Rules

- Treat `develop` as the v1 source of truth.
- Treat `yubikit` as the v2 integration source of truth.
- Analyze only the supplied pull request range, normally `origin/yubikit...HEAD`.
- Use `develop` only as the v1 comparison baseline, not as a source of new v2 behavior.
- Do not infer changes from branch names, commit messages, or intuition when the context files do not support them.
- Do not edit repository files in preview mode. Produce a PR comment only.

## Required Inputs

Read the deterministic context files produced under `.migration-context/`:

- `.migration-context/context-summary.md`
- `.migration-context/changed-files.txt`
- `.migration-context/diff.patch`
- `.migration-context/public-api-candidates.txt`
- `.migration-context/api-added.txt`
- `.migration-context/api-removed.txt`
- `.migration-context/package-changes.txt`
- `.migration-context/namespace-changes.txt`
- `.migration-context/session-patterns.txt`
- `.migration-context/v1-symbol-candidates.txt`
- `.migration-context/existing-map-hits.txt`
- `.migration-context/docs-coverage-hits.txt`
- `.migration-context/migration-state.txt`

Also read the current migration artifacts if present:

- `docs/migration/v1-to-v2.md`
- `docs/migration/v1-to-v2-map.yml`
- `docs/migration/v1-to-v2-changelog.md`
- `docs/migration/.state.yml`

## Anti-Hallucination Rules

- Document only migration-relevant changes visible in the supplied range.
- If a mapping is not supported by code or existing migration artifacts, say it needs manual review.
- Never claim a mechanical migration when lifecycle, transport, security, exception behavior, or protocol bytes are ambiguous.
- Treat internal-only changes as `none` unless public behavior changes are visible.
- If the diff is too large or context is incomplete, summarize affected modules and produce a manual-review checklist.
- Preserve uncertainty. Do not fill gaps with plausible API names.

## Confidence and Manual Review

Use these confidence levels:

- `high`: directly supported by changed files and v1/v2 symbols.
- `medium`: supported by module structure or existing migration artifacts, but not enough for a mechanical mapping.
- `low`: plausible migration impact with incomplete evidence; must be manual-review.

Use these mapping statuses when suggesting map changes:

- `automatic`: safe mechanical package, namespace, type, or member mapping.
- `assisted`: partially mechanical, but requires developer review.
- `manual`: requires human migration due to behavior, lifecycle, transport, protocol, or security changes.
- `removed`: v1 API has no v2 equivalent.
- `unknown`: possible relationship needs review.

## Output Expectations

Post a concise PR comment with this structure:

1. `Migration docs preview`
2. Analyzed refs and range.
3. Migration impact classification: `none`, `low`, `medium`, or `high`.
4. Bullet list of supported findings with evidence paths.
5. Suggested guide/map/changelog updates, if any.
6. Manual-review checklist for uncertain items.
7. Explicit statement that preview mode did not edit files.

If there is no migration impact, say so and cite the evidence that the changes are internal-only or unrelated to public migration behavior.
