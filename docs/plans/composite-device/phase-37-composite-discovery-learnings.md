# Phase 37 Learnings: Composite Discovery And Repository Semantics

Handoff record for Phase 37 of the composite-device program.

## Phase Summary

- Branch: `yubikit-composite-device-new`.
- Scope: merge the per-interface SDK devices (PC/SC CCID, FIDO HID, OTP HID) of one physical USB YubiKey into a single `IYubiKey` (`CompositeYubiKey`), key the repository by physical identity, filter by merged capability set, and gate the two parameterless `ConnectAsync()` consumers so the merged cutover is safe.
- Phase ISA: `docs/plans/composite-device/phase-37-composite-discovery-ISA.md`.
- Satisfies master ISA ISC-16, ISC-17, ISC-18, ISC-19, ISC-20, ISC-27.

## Merge Algorithm (as built)

1. `FindYubiKeys.FindAllAsync` always enumerates BOTH transports (PC/SC + HID) regardless of the requested filter, so interfaces can be grouped; the filter is applied to the merged capability set at the end.
2. Each per-interface device becomes a descriptor `{ IYubiKey, ConnectionType, IsUsb, serial?, DeviceInfo? }`. `IsUsb` = HID always true; PC/SC true only when `PscsConnectionKind.Usb` (NFC and `Unknown` are non-USB → never merge).
3. Identity (serial) is read via the Core `DeviceInfoReader` over a short-lived connection per USB interface, **only when more than one USB interface is present** (merge otherwise impossible), with retry on transient PC/SC sharing violations, and cached per interface pre-key (the interface `DeviceId`).
4. `CompositeDeviceMerger.Merge` (pure, deterministic, side-effect-free) groups USB interfaces by shared non-null serial into `CompositeYubiKey`; single-member groups and all non-USB / null-serial interfaces pass through unwrapped (conservative no-collapse).
5. `CompositeYubiKey` reports the union of member connections, routes typed `ConnectAsync<T>()` to the member exposing the requested connection, and owns no long-lived connection (`IYubiKey` is not `IDisposable`).

## Key Decisions

- **Serial is the cross-transport merge key.** .NET PC/SC exposes no USB Product ID or topology (`IPcscDevice` = `ReaderName`/`Atr`/`Kind`), so the Rust PID-count shortcut cannot bridge CCID↔HID. The Phase 35 `DeviceInfoReader` exists for exactly this.
- **Conservative no-collapse**: null/unreadable serial ⇒ the interface stays its own device.
- **NFC and `Unknown` PC/SC kind never merge.**
- **`DeviceInfo`/`FirmwareVersion` stay internal** to `CompositeYubiKey` in this phase (revised after interim Cato finding #5); no public `IYubiKey` member, no broad implementer/double migration, no foot-gun.
- **Repository keys by physical `DeviceId`**; a same-DeviceId capability change emits `Removed`+`Added` (no `Changed` enum value; avoids a public enum change).
- **Sequencing gate (master ISA line 247)**: only the two parameterless `ConnectAsync()` consumers were gated — Management (SmartCard→HidFido→HidOtp) and YubiOtp (SmartCard→HidOtp, matching the shipped OtpTool example) — via the new public `IYubiKey.ResolvePreferredConnection(params ConnectionType[])`. Full Phase 38 policy/overrides/tests remain deferred.

## Source Changes

New (Core): `CompositeYubiKey.cs`, `CompositeDeviceMerger.cs` (+ `DeviceInterfaceDescriptor`), `DiscoveryIdentityReader.cs`, `YubiKeyConnectionExtensions.cs` (`ResolvePreferredConnection`).
Modified (Core): `FindYubiKeys.cs` (scan-both + descriptors + gated/cached identity read + serialize + merge + filter), `YubiKeyDeviceRepository.cs` (remove+add on capability change).
Modified (applets): `Management/src/IYubiKeyExtensions.cs`, `YubiOtp/src/IYubiKeyExtensions.cs` (merge-safe transport resolution).
Modified (test infra): `Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs` (set-correct `SupportsConnection` filtering), `Tests.Shared/YubiKeyTestState.cs` (requested vs available transport split), `YubiOtp` integration tests (migrated to the gated `CreateYubiOtpSessionAsync`).
New tests: `CompositeDeviceMergerTests`, `CompositeYubiKeyTests`, `ResolvePreferredConnectionTests`, `YubiKeyDeviceRepositoryCompositeTests`, integration `CompositeDiscoveryIntegrationTests`.

## Review Evidence

- Interim Cato (ISA, pre-edit, broad API-boundary gate): GPT-5.4, round 1 **BLOCKED** (2 BLOCKER + 3 HIGH), round 2 **CONCERNS** (all substantive findings RESOLVED; 3 internal-consistency nits fixed). Drove real changes before coding: Tests.Shared scope, identity-cache eviction, internal-only DeviceInfo, YubiOtp SmartCard-first order, non-disposable ownership, `Unknown`-kind non-merge, same-id capability-change event. Outputs: `/tmp/opencode/cato-review-phase37-isa-output{,2}.md`.
- Interim DevTeam (implementation): GPT-5.4, round 1 **CHANGES REQUIRED** (2 HIGH + 1 LOW), round 2 **PASS** (all RESOLVED, no new defects). Fixes: requested-vs-available transport in `YubiKeyTestState`; cache only successful identity reads (transient failures retried); resolver returns concrete transports only. Outputs: `/tmp/opencode/devteam-review-phase37-output{,2}.md`.
- GPT-5.5 cross-vendor reviews (DevTeam + Cato) **QUEUED** for when quota returns, per the interim-review workaround.

## Verification Evidence

- Branch `## yubikit-composite-device-new`; Rust ref `## experiment/rust`.
- Full solution build: succeeded, 0 warnings, 0 errors.
- Core unit suite: 458 total, 0 failed (456 passed, 2 skipped) — includes the new merge/composite/resolver/repository tests.
- Hardware smoke (serial 103, FW 5.8.0 beta, OTP+FIDO+CCID): all 3 pass — `FindAllAsync(All)` returns ONE merged device (`SmartCard|HidFido|HidOtp`); `SmartCard`/`HidFido`/`HidOtp` filters each return that same physical device; typed SmartCard/FIDO/OTP connects succeed. No UP/UV/touch. The local allow-list edit (serial 103) was reverted before commit.
- Changed-file format clean; `git diff --check` clean; docs-qa validated 54 files; Core has no dependency on Management.

## Known Limitations (FLAGGED — candidates for a follow-up)

### 1. External exclusive CCID holders block the discovery serial read

On the test machine, GnuPG `scdaemon --multi-server` held the YubiKey CCID, so the discovery `ConnectAsync<ISmartCardConnection>()` failed with `SCARD_E_SHARING_VIOLATION` on every attempt (the retry loop exhausted, not a transient race). The code degrades correctly: the CCID interface gets a null serial → conservative no-collapse → it appears as a separate `PcscYubiKey` while the two HID interfaces still merge. The merged-all-three result only appears when the CCID is free (`gpgconf --kill scdaemon`).

This is **inherent to the serial-as-only-merge-key approach taken in Phase 37**: because we open every USB interface to read its serial and group by serial, any process holding the CCID exclusively (scdaemon/GnuPG, OpenSC/pkcs11, some browsers, Windows smart-card services) prevents the CCID interface from joining its physical device. The HID interfaces still merge; only CCID is left out. It is a correctness-degradation (extra device row), never a crash or wrong merge.

**How the Rust reference avoids this (verified in `crates/yubikit/src/platform/pcsc.rs` and `device.rs`):**
- It does NOT open every interface. For a single physical key, `open_single_usb` opens exactly ONE connection preferring CCID → OTP → FIDO and reads DeviceInfo over whichever succeeds; the other transports' paths are attached from the cheap enumeration by PID match. So a locked CCID is read over OTP/FIDO instead and the CCID path is still attached — it is never dropped.
- `PcscSmartCardConnection::open_inner` tries `ShareMode::Exclusive` then falls back to `ShareMode::Shared`, and on failure calls `kill_pcsc_blockers()` which runs `pkill -9 scdaemon` then `pkill -1 yubikey-agent`, sleeps 100 ms, and retries. There is an `is_sharing_violation()` helper and a `YKMAN_NO_EXLUSIVE` env override. `open` also retries USB "no card" up to 9× at 500 ms.

Recommended .NET remediation (NOT done in Phase 37; needs a Cato gate because it changes the merge model):
- **Adopt the Rust PID-from-reader-name model.** Our PC/SC reader is named `"Yubico YubiKey OTP+FIDO+CCID 00 00"`; derive the USB PID from the capability substring (Rust `pid_from_interfaces`: OTP+FIDO+CCID → 0x0407, etc.) and correlate CCID with the HID interfaces by PID WITHOUT opening the CCID. Read DeviceInfo over a single preferred transport with CCID → OTP → FIDO fallback. This removes both the open-every-interface cost and the exclusive-holder limitation.
- Optionally add `kill_pcsc_blockers`-style behavior for tooling/CLI contexts (not appropriate to kill a user's scdaemon silently inside an SDK library call; gate behind an explicit opt-in).
- USB parent/bus/port topology correlation is an alternative to reader-name PID, but is more platform-interop work and PC/SC cannot expose the parent on macOS/Win7.

### 2. Serial-less keys (SKY / Security Key series)

Some keys do not report a serial number — notably the **SKY (Security Key) series**, and any key with serial-number API visibility disabled. Coverage in Phase 37:

- **SKY single-interface (typical: FIDO HID only)** — handled correctly: a single serial-less USB interface needs no merge and passes through as exactly one device. Covered by `CompositeDeviceMergerTests.Merge_SeriallessSingleInterface_SkyStyle_PassesThroughAsOneDevice`. The `>1 USB interface` gate means no identity read is even attempted for a lone interface.
- **Serial-less key exposing multiple interfaces** — conservative no-collapse: each interface stands alone (we will not merge without serial evidence, to avoid wrongly collapsing two distinct same-model keys). Covered by `Merge_SeriallessMultiInterface_DoesNotMerge_ConservativeNoCollapse` and `Merge_UsbInterfacesWithoutSerial_DoNotCollapse`. This means a serial-less multi-interface key currently appears as several device rows.

**How the Rust reference handles serial-less keys (verified in `device.rs`):** Rust merges on **PID, not serial**, and obtains the CCID's PID from the reader name (`pid_from_reader_name`), so serial is not required to merge:
- Single key (`pid_count ≤ 1`) → `open_single_usb` attaches all transport paths by PID; no serial needed. SKY (FIDO-only) is one device; SKY is detected by PID (`is_sky_pid` == 0x0120) plus a firmware fixup (pre-5.2.8 + no serial + FIDO-only ⇒ `is_sky`).
- Multiple interfaces whose PID appears exactly twice (base+incoming) → Strategy 1 in `merge_devices` merges on PID alone, no serial.
- Serial is only a tiebreaker (Strategy 2 `(version, serial)`) when a PID appears 3+ times; `merge_from` prefers the side that has a serial.

Recommended .NET remediation (NOT done in Phase 37; same Cato-gated follow-up as limitation #1): adopt the Rust PID-from-reader-name + PID-count model. Both HID interfaces already expose a PID (`HidDescriptorInfo.ProductId`), and the CCID PID is derivable from the reader name, so a serial-less key (SKY-style or serial-disabled) could be merged on PID-uniqueness without any serial and without opening the CCID. This relaxes the merge-evidence rule, so it must go through a Cato gate (guard single-key vs multi-key carefully: PID alone cannot distinguish two same-model serial-less keys — Rust falls back to no-merge there too).

## What Did Not Work / Hazards Found
- **Concurrent discovery causes sharing violations.** The monitor's rescan and a caller's `forceRescan` both opening the same interface collided; fixed by serializing `FindYubiKeys.FindAllAsync` with a `SemaphoreSlim` and retrying transient failures.
- **Caching failed reads is wrong.** Initially failures were cached as null (permanent split after a transient failure); fixed to cache only successful reads.
- **Test namespace collision.** A test namespace `...UnitTests.YubiKey` shadowed `Core.YubiKey` and broke `YubiKey.FirmwareVersion` references elsewhere; use `...UnitTests.CoreYubiKey` (Phase 36 convention).

## Reusable Patterns

- Opening connections during discovery is fragile: serialize discovery, gate to when it's actually needed, cache successes, retry transient failures, and degrade to a safe default — never abort.
- A pure merge function over descriptors makes hardware-dependent discovery logic fully unit-testable on fakes.
- Protocols own and dispose their underlying connection on `Dispose()`; dispose the protocol, not the connection, to avoid leaks/double-dispose.
- For a public mechanism that must not encode policy, take the policy (preference order) as a parameter (`ResolvePreferredConnection(params ConnectionType[])`), and restrict outputs to concrete openable values.

## Deferred Candidates

- Phase 38: documented smart-default policy, explicit per-call transport overrides, and the master ISA ISC-21..24 default+override test matrix across all applets.
- A public `IYubiKey.DeviceInfo`/`FirmwareVersion` member (consistently populated) — deferred from Phase 37.
- **Adopt the Rust PID-from-reader-name merge model (addresses Known Limitations 1 and 2 together):** derive the USB PID from the PC/SC reader name capability string and correlate CCID with HID by PID, reading DeviceInfo over a single preferred transport (CCID → OTP → FIDO fallback) instead of opening every interface and grouping by serial. This removes both the exclusive-CCID-holder limitation and the serial-less/SKY limitation, and lowers discovery cost. Needs a Cato gate (relaxes the serial-only merge rule; must keep the conservative no-merge fallback for two same-model serial-less keys). USB parent/bus/port topology is an alternative correlation signal but is more platform-interop work.
- Reset/reconnect (`reinsert`) handle reacquisition (Rust `reinsert_*`).
- Queued: GPT-5.5 DevTeam + Cato reviews of Phase 37.

## Next Phase Inputs (Phase 38)

- The merge cutover is live: `FindAllAsync` returns merged multi-connection devices. Only Management and YubiOtp were gated with a minimal SmartCard-first resolver; Phase 38 must formalize and justify per-applet smart defaults, add explicit override parameters, and add the full default+override test matrix (master ISA ISC-21..24), replacing the inline resolvers where richer policy is needed.
- `IYubiKey.ResolvePreferredConnection(params ConnectionType[])` is available as the public mechanism for transport selection.
- Phase 38 should decide whether to promote `DeviceInfo` to a public `IYubiKey` member now that discovery populates it internally on the composite.

## Compact Summary

- Goal: one physical USB YubiKey = one merged `IYubiKey`, keyed by serial.
- Branch: `yubikit-composite-device-new`.
- Status: implemented, verified (build 0/0, Core 458 pass, hardware smoke 3/3 on serial 103); interim Cato (resolved) + DevTeam (PASS) done; GPT-5.5 queued. Known limit: external exclusive CCID holders prevent CCID merge (safe degrade).
