# Migration Documentation Post-Merge Prompt

You are updating v1-to-v2 migration documentation after changes landed on the `yubikit` branch.

## Branch and Range Rules

- Treat `develop` as the v1 source of truth.
- Treat `yubikit` as the authoritative v2 integration branch.
- Analyze only the supplied post-merge range from `docs/migration/.state.yml` `last_analyzed_commit` to current `HEAD`, normally `last_analyzed_commit..HEAD`.
- Scheduled reconciliation may compare current `origin/develop` and `origin/yubikit`, but must still distinguish newly documented changes from existing guidance.
- Do not push directly to `yubikit`. The workflow opens a documentation PR.
- Advance `docs/migration/.state.yml` `last_analyzed_commit` after successful analysis of the range, even when there is no migration-relevant change. If no guide or map update is needed, add a concise no-impact changelog entry and update only the state file plus changelog.

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

Read and preserve the existing migration artifacts:

- `docs/migration/v1-to-v2.md`
- `docs/migration/v1-to-v2-map.yml`
- `docs/migration/v1-to-v2-changelog.md`
- `docs/migration/.state.yml`

## Anti-Hallucination Rules

- Update docs only for migration-relevant changes supported by the supplied range or reconciliation evidence.
- Preserve existing document structure and append rather than rewrite when practical.
- Prefer appending changelog entries over broad prose rewrites.
- Do not invent type, member, package, or behavior mappings.
- Mark uncertain mappings as manual-review with `status: manual` or `status: unknown` and `confidence: low` or `medium`.
- Never claim automatic migration for lifecycle, transport, security-sensitive, protocol, exception-contract, or behavior-dependent changes unless the evidence is direct and unambiguous.
- Treat internal-only changes as `none` and leave docs unchanged except for a workflow summary if needed.
- Keep all generated documentation ASCII-only.

## Confidence and Mapping Requirements

For every new or changed map entry include:

- `status`: `automatic`, `assisted`, `manual`, `removed`, or `unknown`.
- `confidence`: `high`, `medium`, or `low`.
- `migration_kind`: package, namespace, type, member, construction, async lifecycle, behavior, transport, command, or removal.
- `evidence`: source paths or symbols supporting the mapping.
- `last_reviewed_commit`: the current analyzed `HEAD` commit.

Use `manual` or `unknown` when evidence is incomplete.

## Output Expectations

Make the smallest documentation-only changes needed, limited to:

- `docs/migration/v1-to-v2.md`
- `docs/migration/v1-to-v2-map.yml`
- `docs/migration/v1-to-v2-changelog.md`
- `docs/migration/.state.yml`

Then provide a PR-ready summary with:

1. Analyzed refs and range.
2. Migration impact classification: `none`, `low`, `medium`, or `high`.
3. Files changed.
4. Supported findings and evidence.
5. Manual-review items that remain.
6. Whether `last_analyzed_commit` was advanced, and to which commit.

If no migration-relevant changes are present, do not rewrite the migration guide or map. Add a short no-impact changelog entry and advance `docs/migration/.state.yml` to the analyzed `HEAD` so the same range is not analyzed repeatedly.
