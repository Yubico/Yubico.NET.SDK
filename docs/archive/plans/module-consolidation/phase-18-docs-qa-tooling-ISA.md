# Phase 18 ISA: Docs QA Tooling

## Problem

The consolidation program repaired active documentation in earlier phases, but there was no executable guard for basic active-doc hygiene. Stale links, unclosed code fences, and old FIDO2 User Presence trait examples could reappear without a focused check.

## Vision

The repository has a small, fast, bounded docs QA target that validates active documentation without sweeping archived plans, research, or historical artifacts into release blocking scope.

## Out Of Scope

- No archived `docs/plans`, `docs/archive`, `docs/research`, `docs/reviews`, `docs/specs`, `docs/templates`, or completed-doc cleanup.
- No README snippet compilation.
- No markdown style/lint framework adoption.
- No CI workflow changes.
- No applet behavior or hardware testing.

## Constraints

- Branch must be `yubikit-consolidation`.
- Use `dotnet toolchain.cs -- docs-qa` for the new docs QA target.
- Keep validation deterministic and dependency-free.
- Active-doc scope must be explicit and documented.
- README/example snippet limits must be documented if not compiled.

## Goal

Add a standalone `docs-qa` toolchain target, document its scope, repair active-doc failures it exposes, and record the exact limits of Phase 18 validation.

## Criteria

- `ISC-18.1`: Branch check records `## yubikit-consolidation` before Phase 18 edits and verification.
- `ISC-18.2`: `toolchain.cs` exposes a `docs-qa` target.
- `ISC-18.3`: `docs-qa` validates only bounded active docs and excludes archived/planning/research/spec material.
- `ISC-18.4`: `docs-qa` validates balanced fenced code blocks.
- `ISC-18.5`: `docs-qa` validates local markdown links in active docs.
- `ISC-18.6`: `docs-qa` rejects known stale FIDO2 User Presence trait examples.
- `ISC-18.7`: `TOOLCHAIN.md` documents the target, scan boundary, checks, and snippet-compilation non-goal.
- `ISC-18.8`: Any active-doc failures discovered by the new target are repaired or explicitly deferred with rationale.
- `ISC-18.9`: `dotnet toolchain.cs -- docs-qa` exits 0.
- `ISC-18.10`: DevTeam cross-vendor review is run on Phase 18 changes, or a human-approved waiver is recorded.

## Test Strategy

Planned verification commands:

```bash
git status --short --branch
dotnet toolchain.cs -- docs-qa
dotnet format --verify-no-changes --include "toolchain.cs"
git diff --check
```

Integration tests are skipped because Phase 18 changes only docs and docs tooling.

## Promotion Candidates

- `Tests.Shared`: no code promotion in scope.
- `Core`: no code promotion in scope.
- `Cli.Shared`: no code promotion in scope.
- Toolchain: add standalone `docs-qa` target and helper functions.
