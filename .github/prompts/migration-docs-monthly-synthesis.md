# Migration Documentation Monthly Synthesis Prompt

You are polishing the v1-to-v2 migration documentation for release readiness.

## Purpose

Incremental updates preserve history, but monthly synthesis should turn accumulated notes into a clearer guide. This mode is for organization, deduplication, coverage assessment, and readiness reporting. It must not invent migration mappings.

## Required Inputs

Read:

- `docs/migration/v1-to-v2.md`
- `docs/migration/v1-to-v2-map.yml`
- `docs/migration/v1-to-v2-changelog.md`
- `docs/migration/.state.yml`
- `.migration-context/context-summary.md`
- `.migration-context/changed-files.txt`
- `.migration-context/public-api-candidates.txt`
- `.migration-context/api-added.txt`
- `.migration-context/api-removed.txt`
- `.migration-context/package-changes.txt`
- `.migration-context/namespace-changes.txt`
- `.migration-context/session-patterns.txt`
- `.migration-context/v1-symbol-candidates.txt`
- `.migration-context/existing-map-hits.txt`
- `.migration-context/docs-coverage-hits.txt`

## Allowed Work

- Reorganize existing migration guidance into clearer sections.
- Collapse duplicate changelog-derived guidance into canonical guide text.
- Add or update a release-readiness section in `docs/migration/v1-to-v2.md`.
- Add manual-review items when gaps are visible.
- Improve map entries only when evidence already exists in the map, context files, or source paths.

## Forbidden Work

- Do not create new authoritative mappings without evidence.
- Do not remove manual-review warnings merely because they are inconvenient.
- Do not advance `last_analyzed_commit` unless this run also analyzed and handled the supplied range.
- Do not rewrite the guide into marketing copy; keep it operational for migrating developers.

## Output Expectations

Make documentation-only changes limited to `docs/migration/`. Then summarize:

1. What sections were improved.
2. What duplicates were collapsed.
3. Readiness by area: Core/device, PIV, FIDO2, OATH, YubiOTP, OpenPGP, SecurityDomain, YubiHSM, low-level commands.
4. Remaining manual-review gaps.
5. Whether this was a synthesis-only change or also advanced analysis state.
