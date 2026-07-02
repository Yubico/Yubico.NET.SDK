# Migration Documentation Automation Handoff

## Status

Implemented on branch `yubikit-migration-docs-automation` and opened as PR #513:

https://github.com/Yubico/Yubico.NET.SDK/pull/513

The work seeds v1-to-v2 migration documentation and adds automation intended to keep that documentation current as v2 evolves on `yubikit`.

## Goal

Reduce the lead developer's documentation burden to reviewing generated migration-documentation PRs. The system should accumulate migration guidance over the next several months so that, near v2 finalization, the migration guide is already close to release-ready.

## Added Files

Migration documentation:

- `docs/migration/v1-to-v2.md`
- `docs/migration/v1-to-v2-map.yml`
- `docs/migration/v1-to-v2-changelog.md`
- `docs/migration/.state.yml`

Planning and handoff:

- `docs/plans/migration-docs-automation-plan.md`
- `docs/plans/migration-docs-automation-handoff.md`

Claude prompts:

- `.github/prompts/migration-docs-pr-preview.md`
- `.github/prompts/migration-docs-post-merge.md`
- `.github/prompts/migration-docs-monthly-synthesis.md`

GitHub workflows:

- `.github/workflows/migration-docs-preview.yml`
- `.github/workflows/migration-docs-update.yml`
- `.github/workflows/migration-docs-scheduler-wrapper.yml`
- `.github/workflows/migration-docs-monthly-synthesis.yml`

Context builder:

- `scripts/migration/build-migration-context.sh`

PowerShell was intentionally removed from this automation path; Bash is the canonical scripting language for portability across macOS, Linux, and GitHub-hosted Ubuntu runners.

## How It Works

### PR Preview

When a PR targets `yubikit`, `.github/workflows/migration-docs-preview.yml` runs.

It:

1. Checks out the PR with full history.
2. Fetches `origin/develop` and `origin/yubikit`.
3. Builds `.migration-context/` using `scripts/migration/build-migration-context.sh`.
4. Invokes `anthropics/claude-code-action` pinned to the commit behind `beta` with `.github/prompts/migration-docs-pr-preview.md`.
5. Comments on migration impact only; it should not edit files.

### Post-Merge Update

When changes land on `yubikit`, `.github/workflows/migration-docs-update.yml` runs.

It:

1. Checks out `yubikit`.
2. Reads `docs/migration/.state.yml`.
3. Analyzes `last_analyzed_commit..HEAD`.
4. Invokes Claude with `.github/prompts/migration-docs-post-merge.md`.
5. Opens a migration-doc update PR back into `yubikit` using `peter-evans/create-pull-request` pinned to the `v7` commit.

The prompt tells Claude to advance `.state.yml` after successful analysis, even for no-impact ranges, so old commits do not get reanalyzed forever.

### Weekly Reconciliation

GitHub cron only runs workflows from the repository default branch. The repository default branch is currently `develop`, while the migration docs live on `yubikit`.

`.github/workflows/migration-docs-scheduler-wrapper.yml` is therefore a wrapper intended to exist on the default branch. It checks out `yubikit`, scans the full `origin/develop..HEAD` v1-to-v2 tree delta for reconciliation, and opens PRs back into `yubikit`.

Until this wrapper is present on the default branch, weekly reconciliation will not run automatically. Push-triggered and manually dispatched runs on `yubikit` still work.

### Monthly Synthesis

`.github/workflows/migration-docs-monthly-synthesis.yml` is intended to keep the guide from becoming a pile of chronological updates.

It asks Claude to:

- collapse duplicate guidance,
- improve structure,
- preserve uncertainty,
- report readiness by applet/area,
- avoid inventing mappings.

Like weekly scheduling, automatic monthly cron requires the workflow to exist on the default branch. Manual dispatch can be used from `yubikit`.

## Deterministic Context Bundle

`scripts/migration/build-migration-context.sh` produces these files under `.migration-context/`:

- `context-summary.md`
- `changed-files.txt`
- `diff.patch`
- `public-api-candidates.txt`
- `api-added.txt`
- `api-removed.txt`
- `package-changes.txt`
- `namespace-changes.txt`
- `session-patterns.txt`
- `existing-map-hits.txt`
- `docs-coverage-hits.txt`
- `v1-symbol-candidates.txt`
- `migration-state.txt`

This is meant to keep AI inference grounded in deterministic evidence. The model should classify and write from these files, not guess from branch names or broad context.

## Dry Run Performed

The Bash context builder was dry-run locally against:

```text
origin/yubikit...HEAD
```

Result:

- Context generation succeeded.
- 13 changed files were detected.
- Diff size was 54,726 bytes.
- Diff was not truncated.
- No C# public API candidates were found, as expected for a docs/workflow PR.
- The repo remained clean after the dry run.

Local WSL required a temporary `safe.directory` override due to Windows-owned repo paths. No global Git config was changed.

## Verification Performed

- `bash -n ./scripts/migration/build-migration-context.sh`
- Local deterministic dry run of the Bash context builder.
- DevTeam reviewer pass after implementation.
- DevTeam reviewer re-review after fixes: no blockers remained.

## Cato / Opus Audit Status

Cato was attempted multiple times but did not produce an audit result:

- `CatoRun.ts` returned `status: error` with an empty Claude audit failure.
- Direct `claude -p --bare --model claude-opus-4-8` fallback was blocked by Vertex quota (`429 RESOURCE_EXHAUSTED`).

The implementation proceeded with DevTeam review instead.

## Important Dependencies

- `ANTHROPIC_API_KEY` repository secret, unless the workflows are changed to Vertex/Bedrock/OIDC auth.
- GitHub Actions must allow workflows to create pull requests.
- `peter-evans/create-pull-request` must be permitted to push automation branches and open PRs.
- `anthropics/claude-code-action` behavior should be validated on the first real workflow run.
- Scheduler/monthly cron automation requires wrapper workflows on the repository default branch.

## Expected First Behavior After Merge To `yubikit`

After PR #513 merges into `yubikit`, the push workflow may open a first migration-doc PR analyzing the automation merge itself. That first PR may be meta/noisy. It should either:

- record a no-impact or automation-added changelog entry and advance `.state.yml`, or
- make small automation-note adjustments.

After that state advancement, later SDK feature merges should produce more useful migration-specific documentation PRs.

## Next Steps

1. Merge PR #513 into `yubikit`.
2. Watch the first `Migration Docs Update` workflow run.
3. Verify the Claude action receives context and can either comment or modify docs as intended.
4. Verify `peter-evans/create-pull-request` opens PRs with only `docs/migration/**` changes.
5. If PR creation fails, check repository Actions settings and token permissions.
6. Mirror `.github/workflows/migration-docs-scheduler-wrapper.yml` to the default branch (`develop`) if automatic weekly reconciliation is required immediately.
7. Optionally mirror `.github/workflows/migration-docs-monthly-synthesis.yml` to the default branch if automatic monthly synthesis is required immediately.
8. After a few real runs, tune prompts for noise level and confidence calibration.

## What To Watch For

- Claude action input compatibility with `prompt` and `claude_args`.
- Excessively noisy PR comments.
- State file not advancing.
- Generated docs claiming more confidence than evidence supports.
- Scheduler not running because it exists only on `yubikit`.
- Automation PRs piling up unmerged, causing docs to lag.

## Operating Principle

The system should be boring where correctness matters and intelligent where prose helps:

- deterministic scripts find changed evidence,
- AI classifies migration impact and writes documentation,
- uncertainty becomes manual-review guidance,
- humans only review generated documentation PRs.
