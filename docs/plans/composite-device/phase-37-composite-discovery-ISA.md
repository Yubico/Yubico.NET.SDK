# Phase 37 ISA: Composite Discovery And Repository Semantics

This phase merges the separately-enumerated per-interface SDK devices (PC/SC CCID, FIDO HID, OTP HID) of one physical USB YubiKey into a single logical `IYubiKey`, updates connection-type filtering to operate over merged capability sets, and keys the device repository's add/remove events by physical-device identity instead of per-interface identity. It builds directly on the Phase 36 physical-device shape (`AvailableConnections`, `SupportsConnection`, typed and ambiguity-safe `ConnectAsync`).

Read this together with:

- `docs/plans/composite-device/ISA.md`
- `docs/plans/composite-device/phase-36-physical-yubikey-model-learnings.md`
- `docs/plans/composite-device/phase-35-core-device-info-reader-learnings.md`
- `src/Core/CLAUDE.md`
- `../yubikey-manager` on branch `experiment/rust` (`crates/yubikit/src/platform/device.rs`)

## Problem

After Phase 36, `IYubiKey` describes a physical key, but discovery still returns one `IYubiKey` per interface: `FindYubiKeys.FindAllAsync` concatenates one `PcscYubiKey` per PC/SC reader and one `HidYubiKey` per HID interface, with no grouping. A normal USB YubiKey that exposes CCID + FIDO HID + OTP HID therefore appears as three independent `IYubiKey` instances, each with a single-bit `AvailableConnections`. The repository (`YubiKeyDeviceRepository`) caches and raises add/remove events keyed by per-interface `DeviceId` (`pcsc:{reader}`, `hid:{reader}:{usage}`), so plugging in one physical key raises three "added" events and removing it raises three "removed" events.

This blocks the composite-device vision: callers cannot select a physical key once and open whichever transport they need, and physical-device add/remove semantics are wrong.

The .NET environment diverges from the Rust reference in one decisive way: **PC/SC readers expose no USB Product ID and no USB topology** (`IPcscDevice` carries only `ReaderName`, `Atr`, `Kind`). Rust's primary merge shortcut ("a PID seen exactly twice is the same key") cannot bridge CCID to HID in .NET because the CCID side has no PID. The only identity that is reliably comparable across all three transports is the **application serial number**, which Core can already read over any transport with the Phase 35 `DeviceInfoReader`.

## Vision

`YubiKeyManager.FindAllAsync(ConnectionType.All)` returns one logical `IYubiKey` per physical USB YubiKey. That object reports the union of its available connections (`SmartCard | HidFido | HidOtp` for a full key) and routes typed `ConnectAsync<TConnection>()` to the correct underlying interface. `ConnectionType` filters return the set of physical devices capable of the requested connection, never duplicate per-interface rows. NFC PC/SC devices are never merged with USB devices. Two same-model keys are only collapsed when identity evidence (a shared serial) proves they are the same physical key. The repository raises one `Added`/`Removed` event per physical device.

## Out of Scope

- No applet-extension smart-default policy table, explicit per-call transport-override parameters, or the full default+override unit-test matrix (master ISA ISC-21..ISC-24). Phase 37 includes only the **minimum merge-safety gating** of the two parameterless `ConnectAsync()` consumers required by the master ISA sequencing rule (see Decisions). The documented, justified, and fully tested smart-default/override design remains Phase 38.
- No public `IYubiKey.DeviceInfo`/`FirmwareVersion` interface member. The identity-read metadata is cached internally on the composite in Phase 37; promoting it to a public `IYubiKey` member (with consistent population and the broad implementer/test-double migration it requires) is deferred to a later phase. (Resolves interim-Cato finding #5: a "sometimes populated" nullable public property is a foot-gun.)
- No CLI command-family redesign; only compile/behavior migration required by the discovery/repository change.
- No mechanical Rust port. Rust is the edge-case and behavior reference only.
- No mutating Management operations move into Core; no new Core dependency on Management.
- No new public people-facing discovery API surface beyond what merge/identity requires (the public `YubiKeyManager.FindAllAsync` signatures are unchanged).
- No reset/reconnect/`reinsert`-style device-handle reacquisition API (Rust `reinsert_*`); deferred.

## Principles

- One physical USB YubiKey is one `IYubiKey`. CCID + FIDO HID + OTP HID interfaces of the same key merge into one logical device.
- Merge only on strong, comparable identity evidence. In .NET that is the application serial number read over a transport; absent or unreadable serial means **do not collapse** (conservative no-merge), never a guess.
- NFC is physically separate and is never merged into a USB device.
- The merge algorithm is a pure, deterministic function over per-interface descriptors so it is unit-testable over fake inventories without hardware or live connections (the master ISA test strategy for ISC-16..ISC-20, ISC-27 is "unit tests over fake device inventories").
- Discovery must not regress single-interface behavior, and must bound the new cost of reading identity: identity reads happen only when merging is actually possible (more than one USB interface present) and are cached so repeated monitor rescans do not reopen already-known interfaces.
- The repository's unit of identity is the physical device. Add/remove events fire per physical device; a physical device whose interface set changes is modeled as remove + add (the existing `DeviceAction` has only `Added`/`Removed`).
- Respect the package boundary: Core does not reference Management; identity reads use the Core-owned `DeviceInfoReader`.

## Constraints

- Execute on branch `yubikit-composite-device-new`.
- This is a broad behavioral/API-boundary phase: a Cato review of this ISA is required before source edits. While GPT-5.5 is rate-limited, run the interim opposite-family Cato review via `scripts/interim-cross-vendor-review.sh` (GPT-5.4, high reasoning) and queue the GPT-5.5/Cato review.
- Use the `/DevTeam` review/fix workflow after implementation; interim GPT-5.4 reviewer is acceptable with the GPT-5.5 review queued. Record review output or waiver before commit.
- Use `dotnet toolchain.cs`; never raw `dotnet build` or `dotnet test`. Integration runs require `--project` and use `--smoke` (or explicit category filters) to skip `Slow`/`RequiresUserPresence`.
- Safe hardware smoke for this phase uses the connected 5.8 beta composite key, serial **103** (OTP+FIDO+CCID). Add serial 103 to the Core integration project's allow-list **locally only**; revert that change before commit (it is environment-specific test config, not a Phase 37 deliverable).
- No unattended UP/UV/touch/insert-removal ceremony tests.
- Commit only intended files after the learning note and verification are complete; never `git add .`/`-A`/`commit -a`.
- Do not introduce a `Core` reference to `Management`.

## Goal

Implement deterministic serial-based composite discovery so Core returns one physical `IYubiKey` per USB YubiKey (CCID/FIDO/OTP merged), keeps NFC separate, conservatively refuses to collapse keys without shared-serial evidence, filters by merged capability set, and keys repository add/remove events by physical-device identity; read and internally cache the merged device's read-only `DeviceInfo`/`FirmwareVersion` during discovery (no public `IYubiKey` member in this phase); apply the minimum merge-safety gating to the two parameterless `ConnectAsync()` consumers (Management, YubiOtp) so the merged-device cutover does not break them; cover all merge/filter/event behavior with unit tests over fake inventories; verify the full solution; run a safe hardware smoke against serial 103; run interim Cato (ISA) and DevTeam (implementation) reviews with GPT-5.5 queued; write a learning note; and commit Phase 37 only.

## Criteria

### Governance

- [ ] ISC-1: Branch check shows `## yubikit-composite-device-new` before source edits, review delegation, build/test, or commit.
- [ ] ISC-2: `../yubikey-manager` reference branch is confirmed as `experiment/rust` before citing Rust composite-device behavior.
- [ ] ISC-3: This ISA records the explicit merge-identity strategy, NFC and no-collapse rules, and the Phase 37/38 sequencing resolution before source edits.
- [ ] ISC-4: Interim Cato review of this ISA runs before source edits and returns pass or all concerns are resolved; the GPT-5.5 Cato review is queued.

### Merge Model And Algorithm

- [ ] ISC-5: A `CompositeYubiKey` (or equivalent) internal `IYubiKey` aggregates ordered member interface devices, reports `AvailableConnections` as the bitwise union of its members' concrete connect bits, and routes typed `ConnectAsync<TConnection>()` to the member that supports the requested connection (throwing `NotSupportedException` naming the type when unsupported). Its `DeviceId` is a stable physical-identity string derived from the serial (e.g. `ykphysical:{serial}`).
- [ ] ISC-6: Ownership model fits the existing type system. `IYubiKey` is not `IDisposable`, and neither `PcscYubiKey` nor `HidYubiKey` owns a long-lived connection; `CompositeYubiKey` likewise owns no long-lived connection and holds only references to its member interface devices. Identity-read connections opened during discovery are disposed inside the read (try/finally). `ConnectAsync<TConnection>()` returns an independently-owned connection the caller disposes; a failed connect leaks nothing. (Resolves interim-Cato finding #6.)
- [ ] ISC-7: The merge is implemented as a deterministic, side-effect-free function over per-interface descriptors (interface connect bit, transport USB/NFC, nullable serial, and the underlying per-interface `IYubiKey`), returning the merged device list. It is unit-tested directly without hardware or live connections.
- [ ] ISC-8: Merge identity is the application serial number. USB interfaces sharing the same non-null serial merge into one `CompositeYubiKey`; the merge does not use USB Product ID or USB topology as a cross-transport key (recorded rationale: PC/SC exposes neither in .NET).
- [ ] ISC-9: Conservative no-collapse: a USB interface whose serial is null/unreadable is never merged with any other interface; it is returned as its own single-interface device. Two interfaces only merge when both report the same non-null serial. (Satisfies master ISC-19.)
- [ ] ISC-10: A lone single USB interface, and any device set where merging is impossible, passes through unchanged as the existing per-interface `IYubiKey` (no `CompositeYubiKey` wrapper for a single member), preserving Phase 36 single-interface behavior.

### Discovery, Identity Read, And Cost

- [ ] ISC-11: `FindYubiKeys.FindAllAsync(ConnectionType.All)` returns exactly one logical device for a physical USB key exposing CCID + FIDO HID + OTP HID, with `AvailableConnections == SmartCard | HidFido | HidOtp`. (Master ISC-16.)
- [ ] ISC-12: Identity (serial) is read via the Core `DeviceInfoReader` over each USB interface's connection during discovery, and only when more than one USB interface is present (merging is otherwise impossible). When exactly one USB interface (and any number of NFC readers) is present, no identity read is performed. Per-interface identity reads are cached keyed by the interface's cheap stable pre-key (PC/SC reader name / HID device path) so repeated monitor rescans do not reopen already-known interfaces. A failed identity read is treated as null serial (ISC-9 no-collapse), not an error that aborts discovery.
- [ ] ISC-12.1: Identity-cache invalidation is explicit (resolves interim-Cato finding #2): an entry is evicted when its pre-key is absent from the latest inventory, and a re-read is forced when a pre-key reappears, so a cached serial is never reused across an absence/reinsert (defends against a different physical key reappearing under a recycled reader name / HID path).
- [ ] ISC-13: The merged `CompositeYubiKey` caches the `DeviceInfo`/`FirmwareVersion`/serial obtained during the identity read as an INTERNAL member, reused to avoid re-reads and available to Core test-support. Phase 37 does NOT add a public `DeviceInfo`/`FirmwareVersion` member to the `IYubiKey` interface (deferred; see Out of Scope). The Management `GetDeviceInfoAsync()` extension remains the public device-info path. The identity read uses the Core `DeviceInfoReader`, so no Core→Management coupling is introduced. (Resolves interim-Cato finding #5.)

### Filtering, NFC, Repository

- [ ] ISC-14: `ConnectionType` filters return merged physical devices capable of the requested connection, never duplicate per-interface rows. Filtering is applied to the merged `AvailableConnections` set via the Phase 36 `Matches` helper in both `FindYubiKeys` and `YubiKeyDeviceRepository.GetAll`. (Master ISC-17.) The current asymmetry (HID filtered post-factory, CCID unfiltered) is removed: filtering applies uniformly to merged devices.
- [ ] ISC-15: NFC PC/SC devices (`PscsConnectionKind.Nfc`) are never merged with USB HID or USB CCID devices; each NFC reader is its own standalone device. USB-vs-NFC kind is surfaced from the PC/SC layer to the merge layer (internal). A PC/SC reader whose kind is `Unknown` is treated as non-mergeable standalone (never assumed USB). (Master ISC-18; resolves interim-Cato finding #7.)
- [ ] ISC-16: `YubiKeyDeviceRepository` caches and diffs by physical-device identity (the merged `DeviceId`), so add/remove events fire once per physical device, not once per interface. (Master ISC-20.)
- [ ] ISC-17: A physical device whose interface set changes between rescans (e.g. an interface appears or disappears while the key stays plugged) produces a coherent event sequence (modeled as `Removed` then `Added` of the physical device, since `DeviceAction` has only `Added`/`Removed`); no silent capability change without an event, and no spurious churn for an unchanged device. This explicitly includes the same-`DeviceId` case where the physical identity is stable but `AvailableConnections` changed: today the repository overwrites such entries silently (`YubiKeyDeviceRepository` lines ~92-95); Phase 37 must emit remove+add instead of a silent overwrite. (Resolves interim-Cato finding #9.)

### Sequencing Gate (master ISA line 247)

- [ ] ISC-18: The two production parameterless `ConnectAsync()` consumers are made merge-safe so a merged multi-connection device does not throw the Phase 36 ambiguity exception through them: `Management/src/IYubiKeyExtensions.cs` (`CreateManagementSessionAsync`) resolves a concrete transport in preference order SmartCard → HidFido → HidOtp; `YubiOtp/src/IYubiKeyExtensions.cs` (`CreateYubiOtpSessionAsync`) resolves in preference order **SmartCard → HidOtp** (SmartCard-first, matching the shipped `OtpTool` example which "prefers SmartCard for richer protocol support"; YubiOtp cannot run over FIDO HID). Resolution uses a single internal Core helper over `AvailableConnections`; for a single-interface device the resolved transport equals the only available one (behavior parity). The full smart-default policy, override parameters, and master ISA ISC-21..24 test matrix remain Phase 38. (Resolves interim-Cato finding #4.)
- [ ] ISC-19: The other applet entry points are confirmed already merge-safe (Oath, OpenPgp, SecurityDomain, YubiHsm use `ConnectAsync<ISmartCardConnection>()`; FIDO2 uses its Phase 36 typed selection) and require no Phase 37 change; this is verified, not assumed.
- [ ] ISC-19.1: `Tests.Shared` is migrated to be merge-correct (resolves interim-Cato finding #1, BLOCKER): `YubiKeyTestInfrastructure` device filtering uses the set-correct `Matches`/`SupportsConnection` helpers instead of scalar `d.ConnectionType == criteria.ConnectionType` equality, and `YubiKeyTestState` separates the **requested** transport (the `[WithYubiKey(ConnectionType = ...)]` filter / the transport a test will open) from the device's **available** transports, so a merged multi-connection device still matches `[WithYubiKey(ConnectionType = X)]` when it supports `X`. The cache key is reconciled with physical identity.
- [ ] ISC-19.2: `[WithYubiKey]`-based integration tests that call the parameterless `state.Device.ConnectAsync()` are migrated to a merge-safe form (resolves interim-Cato finding #3, HIGH). At minimum `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/YubiOtpSessionIntegrationTests.cs` (lines ~28,41,54,80,109,125) is migrated to the gated `CreateYubiOtpSessionAsync()` helper or a typed connect. A repo scan confirms no other production-or-test parameterless `ConnectAsync()` consumer is left that would throw on a merged device.

### Tests And Verification

- [ ] ISC-20: Unit tests over fake inventories cover: (a) single-PID/full-key three-interface merge into one device with unioned `AvailableConnections`; (b) two same-model keys with different serials staying as two devices, each pairing its own interfaces; (c) a USB interface with null/unreadable serial not collapsing; (d) NFC reader never merged and kept standalone; (e) merged filter semantics (`SmartCard`, `Hid`, `All`, single concrete) over merged sets; (f) repository event diffs keyed by physical identity (one Added/Removed per physical device) including the interface-change remove+add case. (Master ISC-27.)
- [ ] ISC-21: `CompositeYubiKey` behavior unit tests cover typed connect routing to each member, unsupported-type failure, the ambiguity-safe default `ConnectAsync()` throwing on the merged multi-connection device, and member disposal.
- [ ] ISC-22: The merge-safety resolver (ISC-18) is unit-tested: Management resolves SmartCard→HidFido→HidOtp and YubiOtp resolves SmartCard→HidOtp over representative `AvailableConnections` sets, including the full merged set and single-interface parity.
- [ ] ISC-23: Focused Core build and tests pass; full solution build passes with `dotnet toolchain.cs build`.
- [ ] ISC-24: Active documentation validates with `dotnet toolchain.cs -- docs-qa`; changed-file formatting verifies clean; `git diff --check` is clean; Core has no reference to `Yubico.YubiKit.Management`.
- [ ] ISC-25: Safe hardware smoke against serial 103 passes (or records an explicit skip rationale): `FindAllAsync(ConnectionType.All)` returns exactly one device for serial 103 with `AvailableConnections == SmartCard | HidFido | HidOtp`; `FindAllAsync(ConnectionType.SmartCard)`, `(ConnectionType.HidFido)`, and `(ConnectionType.HidOtp)` each return that one device; and a typed `ConnectAsync<ISmartCardConnection>()` (and at least one HID typed connect) on the merged device succeeds. No UP/UV/touch required. The local allow-list edit is reverted before commit.
- [ ] ISC-26: DevTeam review (interim GPT-5.4 acceptable) returns pass or all findings fixed; GPT-5.5 review queued; review output or waiver recorded.
- [ ] ISC-27: Phase 37 learning note exists and records the merge algorithm, identity strategy, NFC/no-collapse rules, sequencing resolution, source changes, review status, verification (incl. hardware smoke evidence), deferred candidates, and Phase 38 inputs.

### Anti-Criteria

- [ ] ISC-28: Anti: Phase 37 ships the merged multi-connection cutover without making the two parameterless `ConnectAsync()` consumers merge-safe (would break Management/YubiOtp on merged devices).
- [ ] ISC-29: Anti: Phase 37 implements the full Phase 38 smart-default policy, per-call override parameters, or the master ISA ISC-21..24 default+override test matrix beyond the minimal merge-safety gating.
- [ ] ISC-30: Anti: Phase 37 collapses devices on weak evidence (same PID/model without shared serial, or any USB↔NFC merge).
- [ ] ISC-31: Anti: Phase 37 introduces a Core reference to Management, or reads identity through anything other than the Core-owned `DeviceInfoReader`.
- [ ] ISC-32: Anti: the local integration allow-list edit (serial 103) is committed.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | branch | Active branch | `## yubikit-composite-device-new` | `git status --short --branch` |
| ISC-2 | branch | Sibling repo branch | `## experiment/rust` | `git -C ../yubikey-manager status --short --branch` |
| ISC-3 | design | Strategy recorded | decisions present before edits | Read |
| ISC-4 | review | Interim Cato ISA review | pass or resolved | `scripts/interim-cross-vendor-review.sh` output |
| ISC-5 to ISC-10 | source/unit | Composite type + pure merge fn | merge/route/no-collapse correct | Read + `dotnet toolchain.cs -- test --project Core --filter ...` |
| ISC-11 to ISC-13 | source/unit | Discovery merge + cost gate + cache + internal identity | one device for full key; reads gated/cached/evicted; identity cached internally (no public member) | Read + unit tests |
| ISC-14 to ISC-17 | unit | Filter/NFC/repository event semantics | merged filtering; NFC/Unknown standalone; per-physical events incl. same-id change | `dotnet toolchain.cs -- test --project Core --filter ...` |
| ISC-18 to ISC-19.2 | source/unit | Merge-safety gating + applet audit + Tests.Shared/test migration | two consumers gated; others safe; Tests.Shared set-correct; parameterless-connect tests migrated | Read + unit tests + full build |
| ISC-20 to ISC-22 | unit | Fake-inventory merge/filter/event + composite + resolver tests | focused tests pass | `dotnet toolchain.cs -- test --project Core --filter ...` |
| ISC-23 | build/test | Core + full build/tests | exit 0 | `dotnet toolchain.cs build` / `-- test --project Core` |
| ISC-24 | docs/format/dep | Docs QA, changed-file format, whitespace, no Core->Mgmt | exit 0 / clean | `dotnet toolchain.cs -- docs-qa`; `dotnet format --verify-no-changes --include <changed>`; `git diff --check`; grep |
| ISC-25 | integration | Hardware smoke serial 103 | one merged device; typed connect OK; allow-list reverted | `dotnet toolchain.cs -- test --project Core --integration --filter "...RequiresHardware..."` |
| ISC-26 | review | DevTeam review | pass/fixed/waiver | review output |
| ISC-27 | file | Learning note | present | Read |
| ISC-28 to ISC-32 | scope/dependency | Sequencing + evidence + coupling guards | gating present; no weak merge; no Core->Mgmt; allow-list reverted | Read / grep / git diff |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Phase 37 ISA + interim Cato | Write this ISA; record merge/identity/NFC/sequencing decisions; run interim Cato before edits. | ISC-1, ISC-2, ISC-3, ISC-4 | Phase 36 | false |
| Identity plumbing | Surface USB/NFC/Unknown kind to the merge layer; add discovery-time serial read via `DeviceInfoReader` with multi-interface gate + invalidating cache; cache identity (`DeviceInfo`/serial) internally on the composite (no public `IYubiKey` member). | ISC-12, ISC-12.1, ISC-13, ISC-15 | interim Cato pass | false |
| Composite model + merge fn | `CompositeYubiKey` (union caps, typed routing, no-long-lived-connection ownership) + pure deterministic merge function over descriptors. | ISC-5, ISC-6, ISC-7, ISC-8, ISC-9, ISC-10 | identity plumbing | false |
| Discovery + filtering integration | Wire merge into `FindYubiKeys`; unify filtering on merged sets; keep NFC/Unknown standalone. | ISC-11, ISC-14, ISC-15 | composite model | false |
| Repository physical-identity events | Key cache/diff by physical `DeviceId`; per-physical Added/Removed; interface-change + same-id capability-change remove+add. | ISC-16, ISC-17 | composite model | false |
| Merge-safety gating | Internal preferred-transport resolver; gate Management + YubiOtp parameterless consumers; audit others. | ISC-18, ISC-19 | composite model | true |
| Tests.Shared + test migration | Migrate `Tests.Shared` filtering/state to set-correct matching (requested vs available transport); migrate `[WithYubiKey]` parameterless-connect integration tests to the gated helper. | ISC-19.1, ISC-19.2 | merge-safety gating | true |
| Tests | Fake-inventory merge/filter/event tests; composite behavior tests; resolver tests. | ISC-20, ISC-21, ISC-22 | all source | true |
| Verify, smoke, review, learn, commit | Full build, docs/format/dep, hardware smoke (serial 103), interim DevTeam, learning note, commit. | ISC-23, ISC-24, ISC-25, ISC-26, ISC-27, ISC-28..32 | implementation complete | false |

## Decisions

- 2026-06-10: **Merge identity = application serial number.** USB interfaces sharing the same non-null serial merge into one physical device. The Rust "PID seen exactly twice = same key" shortcut is NOT adopted as the cross-transport key because .NET PC/SC (`IPcscDevice`) exposes neither USB Product ID nor USB topology, so PID cannot bridge CCID to HID. Serial is read over any transport with the Phase 35 Core `DeviceInfoReader`, which exists for exactly this purpose.
- 2026-06-10: **Conservative no-collapse.** Interfaces with null/unreadable serial are never merged (returned as their own single-interface devices). This satisfies master ISC-19 (no same-PID collapse without strong evidence) and matches Rust's no-match fallback, adapted to serial-only evidence in .NET.
- 2026-06-10: **NFC is never merged.** USB-vs-NFC kind (`PscsConnectionKind`, already ATR-detected at PC/SC discovery) is surfaced to the merge layer; NFC readers are always standalone devices. Matches Rust Phase-3 NFC handling.
- 2026-06-10: **Merge is a pure function over per-interface descriptors** so ISC-20/27 are unit-testable over fake inventories (descriptors carry serial/kind directly; no live connection needed in tests). Live discovery builds descriptors by reading serial; tests build them directly.
- 2026-06-10: **Cost gate + cache.** Serial reads happen only when more than one USB interface is present (merging otherwise impossible) and are cached per cheap stable interface pre-key, so the throttled monitor rescan loop does not reopen connections to already-known interfaces. A single lone USB interface incurs no identity read and passes through as its Phase 36 single-interface device. A failed read = null serial (no-collapse), never a discovery abort.
- 2026-06-10: **`DeviceInfo`/`FirmwareVersion` stay internal in Phase 37** (revised after interim-Cato finding #5). The discovery-time identity read is cached on the composite as an internal member (reuse + Core test-support), but is NOT promoted to a public `IYubiKey` interface property in this phase. A "sometimes-populated" nullable public property is a foot-gun and would force another broad implementer/test-double migration. The public device-info path remains the Management `GetDeviceInfoAsync()` extension; a consistent public `IYubiKey` device-info member is deferred to a later phase. (The Phase 36 ISA's "deferred to Phase 37" note is superseded here for the public member; identity-read-and-cache still lands in Phase 37.)
- 2026-06-10: **Identity-cache invalidation** (after interim-Cato finding #2): cache entries are keyed by a cheap stable per-interface pre-key (PC/SC reader name / HID path) but are evicted when the pre-key leaves the inventory and re-read on reappearance, so a recycled reader name / HID path cannot reuse a stale serial for a different physical key. PC/SC `Unknown` kind is non-mergeable.
- 2026-06-10: **Ownership** (after interim-Cato finding #6): `IYubiKey` is not `IDisposable`; the composite owns no long-lived connection, only references to member interface devices. Discovery identity-read connections are disposed in try/finally; `ConnectAsync<T>()` returns caller-owned connections.
- 2026-06-10: **`Tests.Shared` migration is in Phase 37 scope** (after interim-Cato finding #1, BLOCKER): filtering moves to set-correct `Matches`/`SupportsConnection`; `YubiKeyTestState` separates requested vs available transport so merged devices still satisfy `[WithYubiKey(ConnectionType = X)]`. `[WithYubiKey]` integration tests calling parameterless `ConnectAsync()` (YubiOtp integration tests) are migrated to the gated session helper.
- 2026-06-10: **Repository keys by physical identity.** The cache key becomes the merged `DeviceId` (`ykphysical:{serial}` for merged devices; the existing per-interface `DeviceId` for unmerged/passthrough devices). Add/remove diffs operate on physical identity. An interface-set change on a still-present physical device is modeled as `Removed` then `Added` because `DeviceAction` has only `Added`/`Removed`; adding a `Changed` action is deferred (avoids a public enum change in this phase).
- 2026-06-10: **Sequencing resolution (master ISA line 247).** Phase 37 lands the merged cutover together with the minimum Phase-38 gating: only the two parameterless `ConnectAsync()` consumers (Management, YubiOtp) are made merge-safe via an internal preferred-transport resolver (Management: SmartCard→HidFido→HidOtp; YubiOtp: SmartCard→HidOtp, matching the shipped OtpTool example's SmartCard-first preference — revised after interim-Cato finding #4). This is the smallest change that keeps the build/tests green and existing single-interface behavior intact once discovery returns merged devices. The full Phase 38 work — documented smart-default policy, explicit override parameters, and the master ISA ISC-21..24 default+override test matrix across all applets — remains a separate phase.
- 2026-06-10: **Hardware smoke uses serial 103** (connected 5.8 beta OTP+FIDO+CCID key). The Core integration allow-list edit to include 103 is local-only and reverted before commit (environment-specific test config, not a deliverable).

## Changelog

- conjectured: Phase 37 could reuse the Rust PID-count merge shortcut to group interfaces cheaply without opening connections.
  refuted by: .NET `IPcscDevice` exposes no USB Product ID or topology, so PID cannot correlate the CCID interface with the HID interfaces of the same key.
  learned: Use the application serial read via the Core `DeviceInfoReader` as the cross-transport merge key, gated to the multi-interface case and cached to bound cost.
  criterion now: ISC-8, ISC-12 govern serial-based identity and the read gate.
- conjectured: Phase 37 can return merged multi-connection devices without touching any applet extension.
  refuted by: Management and YubiOtp call the parameterless `IYubiKey.ConnectAsync()`, which (Phase 36) throws on a multi-connection device; a merged cutover would break them at runtime.
  learned: Land the minimum merge-safety gating for those two consumers together with the cutover (master ISA line 247), deferring the full Phase 38 policy/overrides/tests.
  criterion now: ISC-18, ISC-19, ISC-28 govern the gating; ISC-29 guards against over-reaching into Phase 38.
- conjectured: production-only call-site auditing and adding a nullable public `IYubiKey.DeviceInfo` were sufficient, and `Tests.Shared`/integration tests would just work.
  refuted by: interim Cato (GPT-5.4, round 1, BLOCKED) — `Tests.Shared` still filters transport by scalar equality and would stop matching merged devices (BLOCKER); the identity cache lacked an eviction/presence rule allowing stale-serial reuse (BLOCKER); YubiOtp integration tests call parameterless `ConnectAsync()` directly (HIGH); the temporary YubiOtp HidOtp-first order conflicted with the shipped SmartCard-first OtpTool example (HIGH); a sometimes-populated public `DeviceInfo?` is a foot-gun forcing another broad migration (HIGH); and ISC-6's disposal model didn't fit non-disposable `IYubiKey` (MEDIUM).
  learned: pull `Tests.Shared` + parameterless-connect test migration into Phase 37; specify cache eviction + `Unknown`-kind non-merge; keep `DeviceInfo` internal; align YubiOtp to SmartCard-first; rewrite the ownership criterion around no-long-lived-connection devices.
  criterion now: ISC-6, ISC-12.1, ISC-13, ISC-15, ISC-18, ISC-19.1, ISC-19.2 capture the fixes.

## Verification

Verification is populated in the Phase 37 learning note before commit.
