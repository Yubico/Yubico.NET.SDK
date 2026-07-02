# V1 to V2 Migration Documentation Automation Plan

## Problem

The v2 SDK will continue evolving for roughly six months before migration guidance must be ready for external developers. The lead developer does not want to manually write, maintain, or remember migration documentation while feature work is ongoing. Without automation, migration guidance will drift, important API deltas will be forgotten, and the final documentation push will require reconstructing months of context.

## Goal

Make v1-to-v2 migration documentation mostly self-maintaining as v2 changes land in the `yubikit` integration branch. By release readiness, developers migrating from v1 should have accurate, current, practical migration documentation and enough structured mapping data to support future migration tooling.

## Branch Model

- `develop` is the v1 baseline.
- `yubikit` is the authoritative v2 integration branch.
- `yubikit-*` branches are feature branches or stacked feature branches.
- Migration docs are authoritative only on `yubikit`.
- Feature branches do not directly maintain migration docs.
- Pull requests targeting `yubikit` get migration-impact preview comments.
- Pushes to `yubikit` produce chronological migration documentation updates.

## Artifacts

- `docs/migration/v1-to-v2.md`: human-readable migration guide.
- `docs/migration/v1-to-v2-map.yml`: structured v1-to-v2 package, namespace, type, member, and behavior mapping.
- `docs/migration/v1-to-v2-changelog.md`: chronological migration-impact ledger.
- `docs/migration/.state.yml`: migration automation state containing `v1_baseline`, `v2_branch`, and `last_analyzed_commit`.

## Initial Snapshot

Create the first migration snapshot on a branch based on `origin/yubikit`. The snapshot explicitly records its scope:

> This document reflects the v2 SDK state on branch `yubikit` as of commit `<sha>`. Later pull requests targeting `yubikit` update this guide incrementally.

The initial snapshot covers the high-confidence migration areas already known:

- Package and namespace split from `Yubico.YubiKey.*` / `Yubico.Core` to `Yubico.YubiKit.*` packages.
- Device discovery model changes.
- Connection and transport selection changes.
- Session construction and async disposal changes.
- Application-specific migration sections for PIV, FIDO2, OATH, OTP, Security Domain, and HSM Auth.
- Manual migration guidance for low-level command/APDU users.

## PR Preview Workflow

For pull requests targeting `yubikit`:

- Fetch `origin/develop` and `origin/yubikit`.
- Analyze only `origin/yubikit...HEAD`.
- Cross-reference changed v2 public surface against v1 source on `origin/develop`.
- Read the existing migration guide and mapping file.
- Comment on the PR with migration impact and suggested documentation changes.
- Do not edit files.
- If the diff is too large, produce only an affected-module summary and manual-review checklist.

## Post-Merge Documentation Workflow

On push to `yubikit`:

- Fetch `origin/develop`.
- Read `docs/migration/.state.yml`.
- Analyze only `last_analyzed_commit..HEAD`.
- Cross-reference changed v2 public surface against v1 source on `origin/develop`.
- Update migration artifacts conservatively.
- Open a documentation PR back into `yubikit`; do not push directly to `yubikit`.
- Advance `last_analyzed_commit` only in the documentation PR and only after the analyzed range has been handled. If the range has no migration-relevant changes, add a short no-impact changelog entry and advance the state file so the same commits are not analyzed repeatedly.

## Scheduled Reconciliation Workflow

Run weekly on `yubikit`:

- Compare the current migration guide and map against the current `develop` and `yubikit` trees.
- Detect migration-relevant public API changes missed by incremental runs.
- Detect stale mappings and unresolved manual-review items.
- Open a documentation PR if reconciliation finds gaps.

GitHub scheduled workflows run from the repository default branch. Because this repository currently uses `develop` as the remote default branch, weekly reconciliation requires either mirroring the update workflow onto the default branch or changing the repository default branch to `yubikit`. Push and manual dispatch runs on `yubikit` remain the reliable initial automation path.

The preferred no-manual-work model is a small default-branch scheduler wrapper. The wrapper lives on the default branch, checks out `yubikit`, builds migration context from `develop` and `yubikit`, and opens documentation pull requests back into `yubikit`. This preserves `yubikit` as the authoritative documentation branch without requiring the repository default branch to change.

## Monthly Synthesis Workflow

Run monthly or manually:

- Read accumulated guide, map, changelog, and context evidence.
- Collapse duplicate incremental notes into canonical guide sections.
- Improve release-readiness structure without inventing mappings.
- Produce or update readiness guidance by area: Core/device, PIV, FIDO2, OATH, YubiOTP, OpenPGP, Security Domain, YubiHSM, and low-level commands.
- Open a documentation PR back into `yubikit`.

Monthly synthesis is the quality ratchet that prevents the guide from becoming a chronological pile of small updates.

## AI Context Contract

Each AI run receives deterministic context before inference:

- V1 baseline ref: `origin/develop`.
- V2 integration ref: `origin/yubikit`.
- Current analyzed range: either `origin/yubikit...HEAD` for PR preview or `last_analyzed_commit..HEAD` for post-merge updates.
- Changed files.
- Changed public API candidates.
- Added and removed public/protected API candidates.
- Package, namespace, session, device, connection, and transport evidence files.
- Existing migration guide.
- Existing migration mapping file.
- Existing chronological changelog.
- Existing map and documentation coverage hits for diff-derived symbols.
- Relevant v1 source snippets discovered by targeted symbol searches.
- Relevant v2 source snippets from the changed range.

The AI must not infer what changed from branch names or intuition. It classifies only the supplied changed range and uses `develop` only as the v1 reference baseline.

The context builder is implemented in Bash for portability across macOS, Linux, and GitHub-hosted Ubuntu runners. A PowerShell implementation may remain temporarily as a fallback while the Bash path is validated, but workflows should prefer Bash.

## AI Prompt Rules

The AI must:

- Document only migration-relevant changes introduced in the analyzed range.
- Use `develop` as the v1 source of truth.
- Use `yubikit` as the v2 source of truth.
- Preserve existing document structure.
- Prefer appending changelog entries over broad rewrites.
- Avoid inventing mappings.
- Mark uncertain mappings as `manual-review`.
- Assign confidence levels to mappings.
- Treat internal-only changes as `none`.
- Never advance `last_analyzed_commit` unless migration artifacts were updated successfully.
- Never claim an automatic migration when behavior or intent is ambiguous.

## Structured Mapping States

Mappings in `v1-to-v2-map.yml` use these statuses:

- `automatic`: safe mechanical package, namespace, type, or member mapping.
- `assisted`: partially mechanical, but requires developer review.
- `manual`: requires human migration due to behavior, lifecycle, transport, or protocol changes.
- `removed`: v1 API has no v2 equivalent.
- `unknown`: possible relationship needs review.

Mappings also include:

- `confidence`: `high`, `medium`, or `low`.
- `migration_kind`: package, namespace, type, member, construction, async lifecycle, behavior, transport, command, or removal.
- `evidence`: source paths or symbols supporting the mapping.
- `last_reviewed_commit`.

## Guardrails

- PR mode comments only; it does not edit files.
- Post-merge mode opens PRs; it does not directly push to `yubikit`.
- The workflow reports the analyzed refs and range in every comment or documentation PR.
- Large diffs degrade to summary mode instead of attempting exhaustive prose.
- Diff context is size-bounded before it reaches the model; oversized ranges include truncation metadata and must be handled as summary/manual-review cases.
- Low-confidence findings become manual-review entries.
- Weekly reconciliation catches missed incremental changes.
- Mapping and changelog are append-friendly to reduce merge conflicts and AI rewrite churn.
- The guide remains human-readable; the map remains machine-readable.

## Success Criteria

At month six:

- The migration guide reflects current `yubikit` behavior.
- The mapping file covers major v1 package, namespace, applet, session, and device APIs.
- The changelog explains migration-impacting changes chronologically.
- PR comments surfaced migration impact during development.
- The lead developer did not need to manually remember or author most migration documentation.
- External developers can identify required v2 packages, namespaces, session creation patterns, device discovery changes, transport changes, async lifecycle changes, and manual migration cases.

## Desired Developer Experience

The system should feel like a memory assistant for the lead developer. Each migration-doc PR should include a short summary such as:

> Migration impact since last update:
> - PIV session creation now uses async factory methods.
> - FIDO2 transport selection defaults changed.
> - Direct v1 `IYubiKeyDevice` usage maps to v2 `IYubiKey`.

This summary should make it easy to review migration documentation without rereading the full guide.

## Cato Audit Questions

Audit this plan for:

- What did we miss?
- What could make this fail over six months?
- How do we make this successful for a lead developer who does not want to manually write or remember migration docs?
- How do we make the outcome exceed expectations for developers migrating from v1 to v2?
- Are the branch model, state tracking, PR workflow, and AI prompt contract sufficient?
- What guardrails are needed to prevent stale, misleading, or hallucinated migration docs?
