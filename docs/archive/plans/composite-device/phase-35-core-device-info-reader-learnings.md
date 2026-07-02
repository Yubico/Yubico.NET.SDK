# Phase 35 Learnings: Core Device Info Reader

Use this note as the handoff record for Phase 35 of the composite-device program.

## Phase Summary

- Branch: `yubikit-composite-device-new`.
- Scope: add a Core-owned read-only device-info reader over SmartCard, FIDO HID, and OTP HID, and have Management delegate its device-info read to it without introducing a Core-to-Management dependency.
- Phase ISA: `docs/plans/composite-device/phase-35-core-device-info-reader-ISA.md`.
- Source changed: Core (new reader, csproj grant), Management (delegation, mutating-only backends), Core/Management tests.
- Next phase after owner command: Phase 36, physical `IYubiKey` model and scalar `ConnectionType` disposition.

## What Changed

- Added `src/Core/src/YubiKey/DeviceInfoReader.cs` (internal): reads `DeviceInfo` over `ISmartCardProtocol` (APDU `0x1D`), `IFidoHidProtocol` (CTAP vendor `0xC2`), and `IOtpHidProtocol` (slot `0x13` + CRC), with multi-page sequencing (more-data tag `0x10`), per-page length validation, OTP CRC validation, and `defaultVersion` passthrough to `DeviceInfo.CreateFromTlvs`.
- Granted `InternalsVisibleTo Yubico.YubiKit.Management` in `src/Core/src/Yubico.YubiKit.Core.csproj`, consistent with the existing Fido2 grant.
- `ManagementSession.GetDeviceInfoAsync` now delegates to `DeviceInfoReader.ReadAsync(_protocol, _version, ct)`; removed the duplicated page loop, `ReadDeviceInfoPageAsync`, and the `TagMoreDeviceInfo` constant from `ManagementSession`.
- Removed `ReadConfigAsync` from `IManagementBackend` and from `SmartCardBackend`, `FidoHidBackend`, and `OtpBackend`. The backends are now mutating-only (write config, set mode, reset). The OTP CRC read handling moved into the Core reader.
- Moved device-info read-behavior tests from Management to Core as `DeviceInfoReaderTests` (fake SmartCard/FIDO/OTP protocols, multi-page aggregation, malformed length, CRC failure, unsupported protocol, null protocol). Management retained config-write zeroing and disposability tests.

## Review Evidence

- Primary DevTeam reviewer route: `openai/gpt-5.5` (selected because the primary family is Anthropic, Vertex Opus 4.8). The GPT-5.5 reviewer was unavailable: both the AgentHarnessRouter `--execute` attempt and a direct `opencode run -m openai/gpt-5.5` probe timed out (empty output / exit 124), consistent with the rate/token limit the principal reported.
- Interim cross-vendor review: ran the GitHub Copilot CLI as an interim OpenAI-family reviewer (`gpt-5.4`, high reasoning) via `scripts/interim-cross-vendor-review.sh` (read-only, `--deny-tool=write`). Output: `/tmp/opencode/copilot-review-phase35-output.md`. This is the documented GPT-5.5 throttling workaround now recorded in `ISA.md` ("Interim Cross-Vendor Review").
- Interim review verdict: PASS WITH NOTES.
- Interim review finding (LOW), fixed: `DeviceInfoReaderTests.ReadAsync_NullProtocol_ThrowsArgumentNull` was a validation-only test (against the repo's "Tests Worth Writing" policy). Replaced with `ReadAsync_DefaultVersionProvided_OverridesFirmwareVersionTlv`, a behavior regression proving the reader passes `defaultVersion` through to `DeviceInfo.CreateFromTlvs` (it overrides the firmware-version TLV). Core reader tests now 8/8 pass.
- Queued: a proper GPT-5.5 DevTeam review of commit `c36bec2a` must still be run when quota is restored; Phase 35 is not a broad public API-boundary phase so Cato is not required for it.

## Verification Evidence

- Branch check command: `git status --short --branch`; result: `## yubikit-composite-device-new`.
- Rust reference command: `git -C ../yubikey-manager status --short --branch`; result: `## experiment/rust...origin/experiment/rust`.
- Focused Core build: `dotnet toolchain.cs -- build --project Core`; passed.
- Focused Management build: `dotnet toolchain.cs -- build --project Management`; passed.
- Core reader tests: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~DeviceInfoReader"`; passed.
- Management session tests: `dotnet toolchain.cs -- test --project Management --filter "FullyQualifiedName~ManagementSessionTests"`; passed.
- Full solution build: `dotnet toolchain.cs build`; passed with 0 warnings and 0 errors.
- Docs QA: `dotnet toolchain.cs -- docs-qa`; passed; 54 active documentation files validated.
- Changed-file format: `dotnet format --verify-no-changes --include $(git diff --name-only --diff-filter=ACM -- '*.cs')`; clean.
- Whitespace: `git diff --check`; clean.
- Dependency-direction checks: Core csproj has no `ProjectReference` to Management (only an `InternalsVisibleTo` friend grant); Core source has no `using Yubico.YubiKit.Management` or `Management.`-qualified references; Management still references Core.

## What Did Not Work

- The mandatory GPT-5.5 cross-vendor reviewer could not run because of the OpenAI rate/token limit; rather than substituting a same-family reviewer, an interim GPT-5.4 Copilot review was run and the GPT-5.5 review was queued.
- Delegating the read path away from `IManagementBackend.ReadConfigAsync` broke the two Management read-behavior unit tests; they were moved to Core where the read logic now lives, which is the correct home for them.

## Reusable Patterns

- Extract read-only discovery logic into Core and let higher modules delegate via an `InternalsVisibleTo` friend grant rather than duplicating protocol commands.
- Keep mutating module operations (write/reset/mode) in the owning module's backend; move read-only operations to Core.
- When moving logic between layers, move its tests to the new owning layer instead of leaving them to fail against an indirected path.
- Port protocol byte-level behavior (OTP CRC, length framing) verbatim and cover it with fake-protocol tests to lock parity.

## Deferred Candidates

- Run a proper cross-vendor DevTeam (or Cato) review of Phase 35 once GPT-5.5 quota is restored.
- Consider whether discovery should read `DeviceInfo` eagerly or lazily when the physical `IYubiKey` model lands in Phase 36.
- Revisit the per-page `DisposableTlvList` wrapper disposal (carried over unchanged) during a future Core resource-handling pass.

## Next Phase Inputs

- Phase 36 introduces the physical `IYubiKey` shape (DeviceInfo, available connections, support predicates, typed connection routing) and decides the scalar `IYubiKey.ConnectionType` disposition; it is a broad API-boundary phase and requires Cato review.
- Phase 36 can consume `DeviceInfoReader` to populate physical-device metadata during discovery.
- Phase 36 must keep the Core-to-Management dependency direction intact and must not move mutating Management operations into Core.

## Compact Summary

- Goal: Core-owned device-info reader; Management delegates.
- Branch: `yubikit-composite-device-new`.
- Scope: Core reader + Management delegation + tests.
- Status: implementation verified; interim GPT-5.4 review PASS WITH NOTES (finding fixed); GPT-5.5 review queued.
