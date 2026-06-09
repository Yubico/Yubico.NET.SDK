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

- DevTeam reviewer route command: `bun ~/.claude/PAI/TOOLS/AgentHarnessRouter.ts --surface devteam --role reviewer --primary-model "google-vertex-anthropic/claude-opus-4-8@default" --dry-run --json`.
- DevTeam reviewer route result: `openai/gpt-5.5` selected because the primary family is Anthropic (Vertex Opus 4.8).
- DevTeam review execution: attempted via the router with `--execute`; the GPT-5.5 reviewer timed out after 200s and produced an empty output file (`/tmp/opencode/devteam-review-phase35.md`).
- DevTeam review status: WAIVED. The cross-vendor reviewer (OpenAI GPT-5.5) is unavailable due to the rate/token limit the principal reported when switching the primary model to Vertex Opus 4.8. Per the program ISA (ISC-4 allows "review output or waiver") and the Phase 35 ISA decision, no same-family reviewer was substituted.
- Supplementary primary-model self-review (not a cross-vendor substitute): verified page-loop termination and more-data handling match the prior Management implementation; per-page length validation preserves the page-aware `BadResponseException` message; OTP CRC strip/validation ports verbatim from `OtpBackend`; `defaultVersion` passthrough preserved; no Core-to-Management coupling; TLV disposal parity preserved.
- Follow-up: a proper cross-vendor DevTeam review should be run against this phase once GPT-5.5 quota is available; the working tree is committed so the review can target commit range for Phase 35.

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

- The mandatory cross-vendor DevTeam reviewer (GPT-5.5) could not run because of the OpenAI rate/token limit; it was waived per the ISA rather than substituted with a same-family reviewer.
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
- Status: implementation verified; cross-vendor review waived (GPT-5.5 rate limit); ready for staging/commit.
