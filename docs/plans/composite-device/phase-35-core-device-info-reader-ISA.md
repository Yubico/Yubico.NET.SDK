# Phase 35 ISA: Core Device Info Reader

This phase gives `Core` its own read-only device-info reader over SmartCard, FIDO HID, and OTP HID so later composite-device discovery can read a physical YubiKey's `DeviceInfo` without depending on `Management`. It builds on Phase 34, which moved the read-only metadata types into `Yubico.YubiKit.Core.YubiKey`.

Read this together with:

- `docs/plans/composite-device/ISA.md`
- `docs/plans/composite-device/phase-34-metadata-promotion-learnings.md`
- `src/Core/CLAUDE.md`
- `src/Management/CLAUDE.md`
- `../yubikey-manager` on branch `experiment/rust`

## Problem

After Phase 34, `Core` owns `DeviceInfo` and `DeviceInfo.CreateFromTlvs`, but the logic that actually reads the device-info TLV pages off the key still lives in `Management`. The per-page read command is protocol-specific (SmartCard APDU `0x1D`, FIDO CTAP vendor `0xC2`, OTP slot `0x13` plus CRC), and the page-iteration, length validation, and TLV decode loop live in `ManagementSession`. Composite-device discovery in later phases needs to read `DeviceInfo` from a freshly discovered physical key before any `Management` session exists, and `Core` cannot reference `Management`.

The read building blocks are already Core-owned: the three protocol interfaces (`ISmartCardProtocol`, `IFidoHidProtocol`, `IOtpHidProtocol`), `ApduCommand`, `TlvHelper`, `DisposableTlvList`, `Tlv`, `ChecksumUtils`, `OtpConstants`, `BadResponseException`, and `DeviceInfo`. Only the orchestration currently sits in `Management`.

## Vision

`Core` can read a physical YubiKey's read-only `DeviceInfo` over any of the three transports using only Core types. `Management` keeps owning mutating operations (config write, set mode, reset, lock codes, reboot) and its session lifecycle, but delegates the read-info orchestration to the Core reader instead of duplicating it. The dependency direction stays `Management -> Core`; `Core` never references `Management` and never consumes a `Management`-owned helper.

## Out of Scope

- No physical/composite `IYubiKey` shape changes in Phase 35 (that is Phase 36).
- No discovery merge or repository event behavior in Phase 35 (that is Phase 37).
- No scalar `IYubiKey.ConnectionType` disposition in Phase 35 (that is Phase 36).
- No extension-method smart-default changes (that is Phase 38).
- No new public API surface; the reader is internal to Core and shared with Management via `InternalsVisibleTo`.
- No SCP/authentication redesign; Management keeps establishing SCP and reads through the same protocol instance it already holds.
- No mutating Management operation moves into Core.

## Principles

- Read-only device-info orchestration belongs in Core because discovery, applet extensions, and tests need it before a Management session exists.
- Mutating Management concepts stay in Management.
- Prefer sharing over duplication: Management delegates the read loop to Core rather than keeping a second copy.
- Keep the new Core reader internal in Phase 35; the public physical-device surface is Phase 36's decision, so Phase 35 does not change public API and is not a broad public API-boundary phase.
- Behavior parity matters: the Core reader must reproduce the existing page sequencing, length validation, OTP CRC handling, and `BadResponseException` messages.
- The dependency direction is the hard invariant: Core must not reference Management and must not consume a Management-owned helper.

## Constraints

- Execute on branch `yubikit-composite-device-new`.
- Use `/DevTeam` review/fix workflow after implementation; record review output or a waiver before commit.
- Use `dotnet toolchain.cs`; never raw `dotnet build` or `dotnet test`.
- Commit only intended files after the learning note and verification are complete.
- Do not introduce a `Core` project reference to `Management`.
- Do not introduce a Core dependency on a Management-owned helper (direct, indirect, or via shared type).
- Preserve `ManagementSession` public behavior, including SCP-authenticated reads and the Select-header firmware-version fallback.

## Goal

Add an internal Core-owned device-info reader that reads `DeviceInfo` over SmartCard, FIDO HID, and OTP HID using only Core types; make `ManagementSession` delegate its device-info read to that reader while keeping all mutating operations in Management; verify no Core-to-Management coupling is introduced; cover the Core reader with fake-protocol unit tests; run DevTeam review; write a learning note; and commit Phase 35 only.

## Criteria

- [ ] ISC-1: Branch check shows `## yubikit-composite-device-new` before source edits, review delegation, build/test, or commit.
- [ ] ISC-2: `../yubikey-manager` reference branch is confirmed as `experiment/rust` before citing Rust read-info behavior.
- [ ] ISC-3: Core owns an internal device-info reader under `Yubico.YubiKit.Core.YubiKey` that returns a Core `DeviceInfo`.
- [ ] ISC-4: The Core reader reads device-info pages over `ISmartCardProtocol` using the GetDeviceInfo command path.
- [ ] ISC-5: The Core reader reads device-info pages over `IFidoHidProtocol` using the read-config vendor command path.
- [ ] ISC-6: The Core reader reads device-info pages over `IOtpHidProtocol` using the read-capabilities command path with CRC validation.
- [ ] ISC-7: The Core reader reproduces multi-page sequencing using the more-data indicator and aggregates TLVs across pages.
- [ ] ISC-8: The Core reader reproduces per-page length validation and throws `BadResponseException` with page-aware context on malformed length.
- [ ] ISC-9: `ManagementSession.GetDeviceInfoAsync` delegates device-info reading to the Core reader and preserves existing behavior, including the `defaultVersion` fallback semantics.
- [ ] ISC-10: Management retains ownership of config write, set mode, reset, lock codes, and reboot; no mutating operation moves to Core.
- [ ] ISC-11: Core production project still has no `ProjectReference` to Management.
- [ ] ISC-12: Core does not consume any Management-owned helper for read-info; the shared read logic is Core-owned and Management depends on Core.
- [ ] ISC-13: `Management` accesses the internal Core reader only through an explicit, intentional `InternalsVisibleTo` grant, and Management still references Core.
- [ ] ISC-14: Core unit tests cover the Core reader over a fake SmartCard protocol path.
- [ ] ISC-15: Core unit tests cover the Core reader over a fake FIDO HID protocol path.
- [ ] ISC-16: Core unit tests cover the Core reader over a fake OTP HID protocol path, including CRC handling.
- [ ] ISC-17: Core unit tests cover multi-page aggregation and malformed-length `BadResponseException` behavior.
- [ ] ISC-18: Existing Management device-info sequencing tests either continue to pass against the delegated path or are migrated to Core with equivalent coverage.
- [ ] ISC-19: Focused Core build passes.
- [ ] ISC-20: Focused Management build and tests pass.
- [ ] ISC-21: Full solution build passes with `dotnet toolchain.cs build`.
- [ ] ISC-22: Active documentation validates with `dotnet toolchain.cs -- docs-qa` and changed-file formatting verifies clean.
- [ ] ISC-23: DevTeam cross-vendor review returns pass or all findings are fixed, or a review waiver is recorded with reason if the cross-vendor reviewer is unavailable.
- [ ] ISC-24: Phase 35 learning note exists and records source changes, review/waiver, verification, deferred candidates, and next phase inputs.
- [ ] ISC-25: Anti: Phase 35 changes `IYubiKey` physical-device semantics or scalar `ConnectionType` disposition.
- [ ] ISC-26: Anti: Phase 35 adds discovery merge/repository event behavior or extension smart defaults.
- [ ] ISC-27: Anti: Phase 35 introduces a Core reference to Management or a Core dependency on a Management-owned helper.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | branch | Check active branch | `## yubikit-composite-device-new` | `git status --short --branch` |
| ISC-2 | branch | Check sibling repo branch | `## experiment/rust` | `git -C ../yubikey-manager status --short --branch` |
| ISC-3 to ISC-8 | source | Inspect Core reader implementation | reader in Core namespace, all three transports + sequencing + validation | Grep/Read |
| ISC-9 to ISC-10 | source | Inspect Management delegation and ownership | session delegates read, keeps mutating ops | Grep/Read/tests |
| ISC-11 to ISC-13 | dependency | Verify project refs and InternalsVisibleTo | no Core->Management ref; Management uses Core internals via grant | `.csproj` grep |
| ISC-14 to ISC-17 | unit | Core reader fake-protocol tests | focused tests pass | `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~DeviceInfoReader"` |
| ISC-18 | unit | Management/Core device-info sequencing tests | focused tests pass | `dotnet toolchain.cs -- test --project Management --filter "FullyQualifiedName~ManagementSessionTests"` |
| ISC-19 | build | Core build | exit 0 | `dotnet toolchain.cs -- build --project Core` |
| ISC-20 | build/test | Management build/tests | exit 0 | `dotnet toolchain.cs -- build --project Management`; focused test |
| ISC-21 | build | Full solution build | exit 0 | `dotnet toolchain.cs build` |
| ISC-22 | docs/format | Docs QA and changed-file format | exit 0 | `dotnet toolchain.cs -- docs-qa`; `dotnet format --verify-no-changes --include <changed>` |
| ISC-23 | review | DevTeam review | pass, fixed, or recorded waiver | review output or waiver note |
| ISC-24 | file | Learning note exists | title and evidence present | Read |
| ISC-25 to ISC-27 | scope/dependency | Scope and coupling guard | no physical-model/discovery/default work; no Core->Management coupling | Git diff / grep |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Phase 35 ISA | Write this ISA before source edits. | ISC-1, ISC-2 | Phase 34 | false |
| Core device-info reader | Add internal Core reader with per-protocol page read, multi-page sequencing, length validation, and CRC handling. | ISC-3, ISC-4, ISC-5, ISC-6, ISC-7, ISC-8 | Phase 35 ISA | false |
| Management delegation | Delegate `ManagementSession.GetDeviceInfoAsync` to the Core reader and grant `InternalsVisibleTo`, keeping mutating ops in Management. | ISC-9, ISC-10, ISC-11, ISC-12, ISC-13, ISC-18, ISC-20 | Core device-info reader | false |
| Core reader tests | Add Core fake-protocol tests for SmartCard, FIDO, OTP, sequencing, and malformed length. | ISC-14, ISC-15, ISC-16, ISC-17, ISC-19 | Core device-info reader | true |
| Verify, review, learn, commit | Full build, docs/format, DevTeam review or waiver, learning note, commit. | ISC-21, ISC-22, ISC-23, ISC-24, ISC-25, ISC-26, ISC-27 | implementation complete | false |

## Decisions

- 2026-06-09: The Core device-info reader is internal in Phase 35; the public physical-device surface is deferred to Phase 36, so Phase 35 is not a broad public API-boundary phase and does not require Cato (DevTeam review still applies).
- 2026-06-09: Management accesses the internal Core reader via a new `InternalsVisibleTo Yubico.YubiKit.Management` grant in Core, consistent with the existing Core grant to Fido2; this keeps the dependency direction Management -> Core.
- 2026-06-09: Management delegates only the device-info read path to Core; `IManagementBackend` keeps mutating operations (write config, set mode, reset). The read-only `ReadConfig` responsibility moves out of the Management backend so the backend is mutating-only.
- 2026-06-09: Device-info read-behavior tests move to Core because the read logic now lives in Core; Management retains config-write zeroing and disposability tests.
- 2026-06-09: The Core reader passes `defaultVersion` through to `DeviceInfo.CreateFromTlvs` so Management can keep supplying its Select-header firmware version, and discovery can pass null to rely on the firmware-version TLV.
- 2026-06-09: If the DevTeam cross-vendor reviewer (OpenAI GPT-5.5, selected because the primary is Vertex Opus 4.8) is unavailable due to rate limits, Phase 35 records an explicit review waiver per the program ISA's allowance rather than substituting a same-family reviewer.

## Changelog

- conjectured: Core could read device info by calling the existing Management backends.
  refuted by: Management backends live in `Yubico.YubiKit.Management`, and Core must not depend on Management.
  learned: The read orchestration and per-protocol read commands must be Core-owned, with Management delegating to Core.
  criterion now: ISC-3 through ISC-13 govern Core ownership and the dependency direction.

## Verification

Verification is populated in the Phase 35 learning note before commit.
