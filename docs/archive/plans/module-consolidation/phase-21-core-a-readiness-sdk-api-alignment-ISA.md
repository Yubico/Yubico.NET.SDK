# Phase 21 ISA: Core A- Readiness And SDK-Family API Alignment Audit

This ISA governs Phase 21 of the module-consolidation quality-convergence program.

Read this together with:

- `docs/SDK-HOUSE-STYLE.md`
- `docs/MODULE-CONSOLIDATION-FINAL-REASSESSMENT.md`
- `docs/plans/module-consolidation/ISA.md`
- `docs/plans/module-consolidation/phase-20-quality-convergence-before-composite-yubikey-ISA.md`
- `docs/plans/module-consolidation/phase-20-quality-convergence-learnings.md`
- `src/Core/CLAUDE.md`
- `src/Core/README.md`

## Problem

Core is the highest-leverage next quality target because every applet module and the future composite YubiKey design depend on its public discovery, connection, logging, APDU, and utility primitives.

The Phase 19/20 handoff named three Core issues that should be resolved or bounded before applet-quality phases continue:

- active documentation and XML comments still referenced removed Core DI setup such as `AddYubiKeyManagerCore()`
- Core has duplicate CRC13239/checksum utility shapes in `Core.Utils.Crc13239` and `Core.Hid.Otp.ChecksumUtils`
- Core public discovery/device concepts need a source-backed SDK-family alignment audit before composite YubiKey design starts

## Vision

Phase 21 should leave Core closer to `A-` readiness by making current Core setup docs accurate, recording a concrete checksum consolidation decision, and mapping Core's public device/discovery surface against Swift, Python, and Android concepts without designing the composite YubiKey feature.

## Out Of Scope

- No composite YubiKey API design.
- No physical-device aggregation implementation.
- No package baseline freeze or `EnablePackageValidation` gate.
- No breaking public API removal for checksum helpers in this phase.
- No broad Core refactor unrelated to the named Phase 21 targets.
- No integration tests; Phase 21 touches docs/XML comments and audit artifacts only.

## Principles

- Prefer source reality over stale DI-era prose.
- Keep Core public API changes audit-only unless explicitly approved.
- Do not conflate SDK-family familiarity with exact API cloning.
- Treat current CLI and integration tests as the real compatibility surface.
- Preserve future optionality for composite YubiKey design by naming tensions, not resolving them prematurely.

## Constraints

- Execute on branch `yubikit-consolidation`.
- Use `dotnet toolchain.cs build --project Core` for source/XML-comment verification.
- Use `dotnet toolchain.cs -- docs-qa` for active docs verification.
- Use `git diff --check` before commit.
- Run DevTeam and/or Cato review for Phase 21 artifacts.
- Stage only intended Phase 21 files.

## Goal

Repair current Core DI documentation drift, produce a Core A- readiness audit for checksum and SDK-family API alignment, and commit the Phase 21 learning note with verification evidence.

## Criteria

- [ ] ISC-1: Phase 21 ISA exists and names the Core scope.
- [ ] ISC-2: Active source/docs no longer claim `AddYubiKeyManagerCore()` is a current Core registration prerequisite.
- [ ] ISC-3: Current Core logging docs point to `YubiKitLogging.Configure(...)` or static `YubiKeyManager`, not removed `IYubiKeyManager` DI resolution.
- [ ] ISC-4: Applet DI XML comments that mentioned Core DI setup now state they only register module session factories.
- [ ] ISC-5: Audit artifact names both CRC13239 implementations and explains why consolidation is deferred or implemented.
- [ ] ISC-6: Audit artifact compares current .NET Core device/discovery concepts with Swift, Python, and Android references.
- [ ] ISC-7: Audit artifact does not start composite YubiKey design and preserves the Phase 20 stop gate.
- [ ] ISC-8: Package compatibility remains audit-only.
- [ ] ISC-9: `dotnet toolchain.cs build --project Core` succeeds.
- [ ] ISC-10: `dotnet toolchain.cs -- docs-qa` succeeds.
- [ ] ISC-11: `git diff --check` succeeds.
- [ ] ISC-12: Review output is recorded and material findings are resolved or explicitly deferred.
- [ ] ISC-13: Learning note records changed files, review evidence, verification evidence, and next phase recommendation.
- [ ] ISC-14: Anti: Phase 21 removes public checksum APIs without an approved breaking-change decision.
- [ ] ISC-15: Anti: Phase 21 begins composite-device identity/cache-key design.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | file | read Phase 21 ISA | file exists with title | Read |
| ISC-2 | content | grep active `src/**` and current docs | no current Core DI prerequisite claim | Grep |
| ISC-3 | content | read `docs/LOGGING.md`, `src/Core/README.md`, `src/Core/CLAUDE.md` | static/logging setup reflects source | Read/Grep |
| ISC-4 | content | grep applet DI comments | no `AddYubiKeyManagerCore()` prerequisite in `src/**` | Grep |
| ISC-5 | audit | read audit artifact | both CRC utilities named with disposition | Read |
| ISC-6 | audit | read SDK-family section | Swift/Python/Android sampled concepts named | Read |
| ISC-7 | audit | read composite boundary | stop gate preserved | Read |
| ISC-8 | audit | read package section | audit-only language present | Read |
| ISC-9 | build | `dotnet toolchain.cs build --project Core` | exit 0 | bash |
| ISC-10 | docs | `dotnet toolchain.cs -- docs-qa` | exit 0 | bash |
| ISC-11 | whitespace | `git diff --check` | exit 0 | bash |
| ISC-12 | review | review output | pass or findings resolved | bash/read |
| ISC-13 | learning | read learning note | evidence recorded | Read |
| ISC-14 | git/source | inspect diff | no checksum API removal | bash/read |
| ISC-15 | audit | read artifact | no composite design decision | Read |

## Features

| Feature | Description | ISC |
| --- | --- | --- |
| DI drift repair | Update current setup docs and XML comments to match static Core discovery and explicit logging configuration. | ISC-2, ISC-3, ISC-4 |
| Checksum audit | Record duplicate CRC13239 utilities and defer consolidation until a public API policy exists. | ISC-5, ISC-14 |
| SDK-family audit | Compare public device/discovery concepts against Swift, Python, and Android without designing composite discovery. | ISC-6, ISC-7, ISC-15 |
| Learning handoff | Record evidence for Phase 22. | ISC-12, ISC-13 |

## Decisions

- 2026-06-07: Core DI setup is removed source reality; documentation should not tell users or agents to call `AddYubiKeyManagerCore()`.
- 2026-06-07: Applet DI extensions remain valid as module-local factory registrations, but they are not chained to a Core DI registration.
- 2026-06-07: `Core.Hid.Otp.ChecksumUtils` and `Core.Utils.Crc13239` duplicate CRC13239 logic, but Phase 21 will not remove public helpers without an approved compatibility decision.
- 2026-06-07: SDK-family API alignment audit may name composite-device tensions but must not choose an identity/cache-key design.

## Changelog

- conjectured: Phase 21 should enforce a package/API baseline before Core cleanup.
  refuted by: Phase 20 owner decision kept package validation audit-only until a real preview/release baseline exists.
  learned: Core API alignment should be a source-backed audit, not a freeze.
  criterion now: ISC-8.
- conjectured: DI drift was only a Core README problem.
  refuted by: Grep found current root/module docs and applet XML comments with stale Core DI prerequisite claims.
  learned: The repair must cover active guidance and XML comments, while historical specs/plans stay historical.
  criterion now: ISC-2 through ISC-4.

## Verification

Verification is populated in `docs/plans/module-consolidation/phase-21-core-a-readiness-sdk-api-alignment-learnings.md` as the phase executes.
