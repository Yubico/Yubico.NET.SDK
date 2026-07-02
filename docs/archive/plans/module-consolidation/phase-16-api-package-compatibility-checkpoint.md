# Phase 16: API And Package Compatibility Checkpoint

This checkpoint records package-facing and public-API risk after the source-risk consolidation phases. It is intentionally narrow: inspect, verify packaging, and classify next actions.

## Branch And Scope

- Branch: `yubikit-consolidation`
- Source changes in this phase: none intended
- Hardware/integration tests: not applicable
- Baseline: `docs/MODULE-CONSOLIDATION-ASSESSMENT.md` remains unchanged

## Packable Project Inventory

Current packable projects are the module projects under `src/*/src/*.csproj` that are not explicitly marked `<IsPackable>false</IsPackable>`. The toolchain still uses the broader source-project discovery set for `build --project`; only pack/publish package targets are filtered to packable projects.

| Project | Package posture |
| --- | --- |
| `Yubico.YubiKit.Core` | Packable SDK library |
| `Yubico.YubiKit.Management` | Packable SDK library |
| `Yubico.YubiKit.Piv` | Packable SDK library |
| `Yubico.YubiKit.Fido2` | Packable SDK library |
| `Yubico.YubiKit.WebAuthn` | Packable SDK library |
| `Yubico.YubiKit.Oath` | Packable SDK library |
| `Yubico.YubiKit.YubiOtp` | Packable SDK library |
| `Yubico.YubiKit.OpenPgp` | Packable SDK library |
| `Yubico.YubiKit.SecurityDomain` | Packable SDK library |
| `Yubico.YubiKit.YubiHsm` | Packable SDK library |
| CLI support/tool projects | Explicitly non-packable; excluded from pack target after Phase 16 |
| Test/support projects | Explicitly non-packable; excluded from pack target after Phase 16 |

## Package Metadata And Dependency Posture

- `Directory.Build.props` centralizes `Version`, target framework, package metadata, repository metadata, Source Link, symbol packages, and documentation generation.
- `Directory.Packages.props` enables central package management and sets `YubiKitVersion` to `2.0.0-preview.1`.
- Microsoft dependencies are centralized at `10.0.2`, matching the repo's .NET 10 target.
- Third-party runtime dependencies are centralized: `System.Formats.Cbor`, `System.Reactive`, and `Yubico.NativeShims`.
- Test, CLI, and build dependencies are centralized and mostly isolated to test/tool/example projects.
- No package-validation baseline, API compatibility baseline, package lock, or `EnablePackageValidation` gate is currently configured.

## Public API Risk Classification

### Breaking-Risk But Approved In Preview

- `ConnectionType` numeric semantics changed in Phase 12 so `[Flags]` values are no longer overlapping. This is a real public enum compatibility risk for serialized or persisted values, but it was intentionally accepted during v2 preview consolidation to repair broken flag semantics.

### Additive Or Low-Risk Changes

- Phase 13 added `Feature.IsSupportedByFirmware(FirmwareVersion)`. This is additive and keeps firmware-only support policy centralized.
- Phase 13 broadened `FirmwareVersion.IsAlphaOrBeta` to `Major == 0`. This is behaviorally significant but aligns existing beta/sentinel feature-gate behavior behind one source of truth.
- Phase 14 added PC/SC SmartCard transport provenance types/helpers. These support FIDO2 transport decisions and do not require consumers to adopt a new public construction pattern.
- Phase 15 kept the public OATH CLI helper signature and changed internal password-byte handling and CLI warning behavior only.

### Documentation/API Drift Risks

- Active docs and module dependency-injection XML comments still reference `AddYubiKeyManagerCore()` in several places, while the current static API direction and source evidence do not consistently support that entry point.
- This is a package-facing documentation risk, not a source compatibility fix for Phase 16. It should be resolved in a docs/API alignment pass before release.

### Enforcement Gaps

- No package compatibility gate is configured.
- No API compatibility baseline artifact is present.
- No previous-package validation is configured for the preview branch.
- Package validation should not be enabled blindly without deciding the baseline package/version and acceptable v2 preview breaks.

### Toolchain Pack Discovery Gap

The first Phase 16 pack run succeeded but revealed that `toolchain.cs pack` attempted non-packable CLI/test projects before producing only the 10 SDK packages. This created noisy output and obscured the real package surface.

Phase 16 fixed that discovery gap by filtering pack targets through project-file `<IsPackable>false</IsPackable>` markers. The filter is whitespace and attribute tolerant, and `build --project` continues to use the broader source-project discovery set. The second pack run attempted only the 10 SDK package projects and created 10 packages.

## Phase 16 Decision

Do not add package-validation enforcement in this phase. The branch is still in v2 preview consolidation and lacks an approved baseline package for compatibility comparison. Enabling a package-validation gate now risks either false confidence or noisy failures without a release policy.

Instead:

- Verify that all packable projects can build and pack through `dotnet toolchain.cs pack`.
- Keep `toolchain.cs pack` scoped to actually packable projects.
- Treat `ConnectionType` numeric changes as an accepted preview break that must be called out in release notes.
- Treat `AddYubiKeyManagerCore()` documentation drift as a high-priority docs/API alignment follow-up.
- Add a future release-readiness gate that chooses a baseline package and enables API/package validation deliberately.

## Verification Plan

- Branch check: `git status --short --branch`
- Package verification: `dotnet toolchain.cs pack --package-version 2.0.0-preview.phase16`
- Whitespace: `git diff --check`
- Review: DevTeam cross-vendor artifact review

## Next Recommended Targets

- Add release notes for approved v2 preview public enum changes.
- Decide whether `AddYubiKeyManagerCore()` docs should be restored through source or removed from active docs.
- Add an explicit API/package compatibility baseline before a stable release candidate.
- Keep Phase 17 focused on test-runner and hardware coordination; do not mix package compatibility into it.
