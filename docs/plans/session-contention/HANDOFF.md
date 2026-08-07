# Handoff — yubikit-session-contention

**Date:** 2026-08-06 (Windows session; macOS pull-back note appended same day)
**Branch:** `yubikit-session-contention`
**Last commit:** `2364910b` — docs: refresh session-contention handoff for the Windows session
**Written for:** resuming on any platform; the remaining hardware item wants an operator-coordinated hotplug

> Committed to the repo rather than `Plans/handoff.md` on purpose: `Plans/` is untracked, so a handoff
> written there never reaches another machine. This file travels with the branch.

## ⚠️ Verification status of the Windows work — READ FIRST

The Phase 11 Windows results and the `HidDDevice` fix (`6289c774`) are recorded as reported by the Windows
session. They have **not been independently verified**. Specifically:

- The operator confirms only that authorized YubiKeys were reachable during those runs; the operator did
  **not** mechanically re-verify the results.
- ~~On macOS after pulling, only `build` was run.~~ **RESOLVED in Phase 12:** the full macOS gate set has
  now run green against the merged Windows commits — discovery 5/5, YubiOtp 10/10, FIDO sharing 3/3, unit
  12/12 projects, resilience passed, PIV contention 7/7. No macOS regression from `6289c774`.
- `6289c774` changes production native interop (`src/Core/src/Native/Windows/HidD/HidDDevice.cs`). It is
  Windows-only by file, so macOS/Linux behaviour should be unaffected, but that is reasoning, not a
  measurement.

**Treat Phase 11 as reported-but-unreviewed.** It is queued as the first item in the deferred cross-vendor
review, alongside the Phase 3–4 review that is already marked "do not merge without it". Do not merge on
the strength of the Phase 11 numbers alone.

---

## Session Summary

Closed the last two register rows, **F2 and F3, on Windows 11 hardware** (firmware 5.8.0, two same-PID keys,
serials 103 and 125). The verification did what it was supposed to — it found a real Windows defect that no
other platform could surface. The register now has **zero open rows and zero platform gaps**.

Three outcomes worth carrying forward:

1. **F5 — a real Windows OTP HID defect, found and fixed.** OTP HID could not be opened on Windows at all,
   even elevated, because the feature-report connection opened the keyboard collection read/write. Fixed to
   open with zero access. This is the branch's central "OTP HID is exclusive" contract — it was untestable
   on Windows before this.
2. **F4 is cross-platform, not macOS-specific.** The handoff hypothesis was that Windows might demultiplex
   two FIDO handles. It does not — `SendOnFirst_ReceiveOnSecond_RevealsReportMisrouting` passes on Windows
   too. Drive CTAP over one FIDO connection at a time on every platform.
3. **A Windows platform characterization, not a defect:** the CCID-held Management fallback routes through
   FIDO HID, which Windows admits only to an elevated process, so that specific fallback requires
   Administrator on Windows. Recorded, not worked around.

---

## Current State

### Committed this session

| Commit | What |
|---|---|
| `6289c774` | **fix(core): open Windows OTP HID feature reports with zero access** |
| `1031890b` | docs: close F2/F3 on Windows and record the OTP HID feature-open fix |

The fix is one file: `src/Core/src/Native/Windows/HidD/HidDDevice.cs`. `OpenIOConnection` (FIDO
input/output reports) keeps `GENERIC_READ | GENERIC_WRITE`; `OpenFeatureConnection` (OTP feature reports)
now opens with `DESIRED_ACCESS.NONE`, matching the legacy Yubico .NET SDK.

### Uncommitted changes

None in tracked files. Three untracked paths that must **never** be staged: `.claude/worktrees/`,
`.playwright-mcp/`, `Plans/`.

### Build & test status (Windows 11, elevated, serials 103 + 125)

| Gate | Result |
|---|---|
| `toolchain.cs build` | 0 errors |
| Core `CompositeDiscoveryIntegrationTests` | 5/5 (was 4/5 before the fix) |
| Piv `PivSessionContentionTests` (F3) | 5/5 |
| Core `FidoHidSharingIntegrationTests` (F2) | 3/3 |
| YubiOtp integration `--smoke` | 10/10, incl. `CalculateHmacSha1_WithKnownKey...` over **HidOtp** |
| `toolchain.cs -- resilience --fast` | 69/69 |
| full Core unit suite | 740/740 (2 skipped) |
| `docs-qa` | 55 files |
| `dotnet format whitespace \| analyzers --verify-no-changes --severity error` | clean |

**Windows requires an elevated shell for the HID hardware tests.** Non-elevated, FIDO HID opens fail with
`UnauthorizedAccessException` (Windows restricts read/write on the FIDO top-level collection to admins), so
the F3 fallback tests and the F2 tests fail for a platform reason, not a code reason. Run the integration
tests from an Administrator terminal.

`dotnet format style --verify-no-changes --severity error` exits **2**, but only on **pre-existing** native
P/Invoke naming (`IDE1006` on `kern_return_t`, `udev_device_get_parent`, `udev_device_get_syspath` in
`Native/MacOS` and `Native/Linux`) — not this branch's file. Use the split subcommands; whitespace and
analyzers are clean.

### Worktree / parallel agent state

Carried over from the prior handoff, still true unless changed: one extra worktree unrelated to this branch
under `.claude/worktrees/agent-aa7ba443d8eec3e9e` (FIDO2 ARKG-P256 work). Do not merge into it. It carries
its own `MacOSHidIOReportConnection.cs` edits that conflict with `619a4bf5`.

---

## Readiness Assessment

**Target:** .NET developers integrating YubiKey hardware, who need concurrent applet sessions and device
discovery to coexist without silently destroying each other's state.

| Need | Status | Notes |
|---|---|---|
| A PIV session survives an unrelated `GetDeviceInfoAsync` | ✅ Working | Pinned on macOS, Linux, and Windows (F3) |
| Exclusive interfaces refuse a second connection clearly | ✅ Working | CCID + OTP HID, named-interface diagnostics |
| Shared FIDO HID admits a second connection | ✅ Working | macOS, Linux, Windows |
| Concurrent CTAP over two FIDO handles | ⚠️ Bounded | Not demultiplexed on **any** platform; drive one handle at a time (F4) |
| Sessions on two different keys stay independent | ✅ Working | Incl. RSA-4096 cross-key liveness |
| Hotplug does not strand an interface lease | ✅ Working | Closed on macOS (D3) |
| Correct behaviour on **Windows** | ✅ Working | PC/SC + HID verified; one OTP HID defect fixed this session (F5) |
| CCID-held Management fallback on Windows | ⚠️ Needs elevation | Routes through FIDO HID; Windows admits that only to an admin process |
| Cross-process contention | ❌ Out of scope | In-process by contract |

**Overall:** 🟢 **Production** on macOS, Linux, and Windows for the target user's primary workflows. The
register has zero open rows and zero platform gaps.

---

## What's Next (Prioritized)

### Remaining hardware item (operator-coordinated)

1. **E1/E2 — DONE on Windows (Phase 12) and macOS (Phase 14).** The macOS run exercised the serial↔PID tier
   flip that the Windows topology-tier rig structurally could not: 7 physical actions → 7 events, zero
   phantom incumbent/survivor events, final-removal DeviceId correlation confirmed, and the published-object
   retention contract demonstrated on hardware. **A Linux run is nice-to-have, not required** — Linux also
   has no Container ID, so it exercises the same degraded tiers macOS just covered.

### Documentation gap — DeviceId / serial identity contract — **DONE (2026-08-06)**

All six defects closed, docs-only, no code or API change:

1. `IYubiKey.DeviceId` now has full XML documentation — session-scoped, not durable, not parseable, with
   the measured evidence that one key can present two identifiers.
2. New "Device identity" section in `docs/architecture/device-discovery-guarantees.md` — tier→shape table
   per platform, the Phase 14 flip, live-vs-fresh divergence, and the serial-as-durable-key guidance.
3. Terminology disambiguated: interface identifier (`hid:*`, `pcsc:*`) vs physical identifier
   (`ykphysical:*`), with the old "stable interface `DeviceId`" line corrected and cross-linked.
4. Serial semantics documented, including the access cost (a connection plus a Management exchange) and
   null-serial devices.
5. Phase 15 Q2 corrected in the ISA: firmware version is a tie-breaker in canonical, not a match key, and
   is explicitly excluded from this SDK's identity.
6. Follow-up recorded below.

**No new unit tests were added, deliberately.** The behaviour and all three identifier shapes are already
pinned across `CompositeDeviceMergerTests`, `CompositeDeviceMergerVectorTests`,
`YubiKeyDeviceRepositoryCompositeTests`, `FindYubiKeysPidMergeTests` and `FindYubiKeysFaultInjectionTests`,
plus hardware runs in Phases 12 and 14. A further test would restate existing assertions and give false
confidence, since the real risk here is prose drifting from code — which no unit test detects.

### Future work item — hoist DeviceInfo properties onto IYubiKey (separate branch/issue)

Out of scope for this branch, agreed with the operator. The identity documentation has to tell consumers
"use `DeviceInfo.SerialNumber` as your durable key", while that property is not reachable from `IYubiKey`
and costs a connection plus a Management exchange to obtain. That is a real usability tension the docs can
only describe, not fix.

Scope for the future effort: surface selected `DeviceInfo` properties (serial first) on `IYubiKey`,
particularly for the composite device, where the operator notes the Rust composite device model is more
comprehensive than ours. Worth comparing against Rust `LocalYubiKeyDevice`, which carries `info` (serial,
version) directly on the device record rather than behind a separate read.

### Canonical-verification queue (no hardware needed)

Use skill `_YUBIKIT_CANONICAL_SOURCE`. Rust `ykrust-auto` @ `9fe08d9a` (macOS path
`/Users/Dennis.Dyall/Code/y/yubikey-manager-rust-auto`).

1. ~~OTP HID exclusivity vs canonical.~~ **DONE (Phase 15): it is NOT canonical — it is our own
   strengthening.** Neither Rust nor Python enforces in-process OTP exclusivity. Behaviour stands (the
   interleaving hazard is real and our public API admits it, unlike theirs); only the provenance was wrong,
   and `src/Core/CLAUDE.md` now says so. Residual accepted risk: a caller wanting two OTP handles is refused
   here and would not be by canonical; no such use case is known.
2. CCID per-interface exclusivity vs canonical.
3. F4 — does canonical support concurrent CTAP over two FIDO handles, or one-at-a-time like us? (Now known
   to be one-at-a-time on all three of our platforms.)
4. Management transport fallback order `SmartCard -> HidFido -> HidOtp` vs canonical. Note the Windows twist:
   `HidFido` needs elevation, so the practical Windows fallback from a held CCID is elevation-gated. Worth
   checking whether canonical prefers OTP HID over FIDO HID on Windows, which would avoid the elevation
   requirement entirely — a possible future improvement, not a defect.
5. ~~Composite grouping / DeviceId tier model vs Rust `device.rs`.~~ **DONE (Phase 15): canonical mints no
   DeviceId at all.** It matches physical devices by **serial + firmware version**
   (`platform/device.rs:694-695`, and removal-wait compares both). Our tiered `ykphysical:*` id is an SDK
   construct with no canonical counterpart — legitimate for .NET consumers who need a stable object key, but
   its stability properties are ours alone to define. This confirms the planned doc guidance ("use the
   serial as the durable key") is canonically supported, and raises the open question of whether we should
   also carry firmware version as canonical does.

### Evidence and process debt (carried over)

6. ~~Reconcile contradictory formatting rows.~~ **DONE 2026-08-06.** Both rows claiming
   `dotnet format --verify-no-changes` = `exit 0` are corrected. The unqualified command exits **2** on
   pre-existing `IL2026`/`IL3050` trim-AOT warnings in `src/Tests.TestProject/Program.cs`, a file added
   2026-04-02 and therefore present when those phases ran — so the rows were inaccurate when written, not
   merely outdated. The real gate is the split severity-scoped form, which is clean. There is no `format`
   target in `toolchain.cs`; these are manual invocations.
6b. **Cross-vendor review of the Phase 11 Windows work — a merge gate.** Review `6289c774`
   (`HidDDevice.OpenFeatureConnection` → `DESIRED_ACCESS.NONE`) and the Phase 11 evidence in `1031890b`.
   It is production native interop, it was authored in a session whose results nobody re-verified, and the
   operator has explicitly deferred it into the cross-vendor review queue. Worth checking specifically:
   whether a zero-access handle is sufficient for *every* feature-report path (not just the ones exercised),
   and whether `OpenIOConnection` keeping `GENERIC_READ | GENERIC_WRITE` is right for FIDO given F4.
   Canonical comparison is cheap here — the commit claims parity with the legacy Yubico .NET SDK, which is
   a checkable assertion.
7. **Record an explicit Phase 3–4 cross-vendor review verdict.** Marked as a blocking merge item but no
   verdict is recorded.
8. **Re-run Cato** on `docs/plans/session-contention/ISA.md`. Standing verdict is `fail` (round 2); its
   CRITICAL finding is resolved and its WARNING is now item 6 above, so a clean verdict should be reachable.
   `bun ~/.claude/skills/Cato/Tools/CatoRun.ts <artifact> --current-vendor openai`
9. ~~Verify/retire the stale `Xunit.SkippableFact` record.~~ **DONE 2026-08-06.** Resolved on this branch by
   the Phase 8 repair, re-verified on macOS: YubiHsm integration **11/11**, YubiOtp **10/10**, no
   `FileNotFoundException`. The "34 commits behind" clause no longer applies to that item.

### Dropped by operator decision (2026-08-06)

Recorded so they are not silently reopened:

- **Linux E1/E2** — redundant; Linux has no Container ID either, so it exercises the same degraded tiers
  macOS covered in Phase 14.
- **Capture the D3 removal-time exception type** — another coordinated unplug for a detail the test does
  not assert on.
- **Chase the 2 transient Fido2 failures** — unreproducible across 3×29/29 and already recorded honestly as
  "observed-good, not proven".
- **Dirty worktree `agent-aa7ba443d8eec3e9e`** — a different effort; the future conflict with `619a4bf5` is
  already noted.
- **143 firmware-gated `[Theory]` tests** — pre-existing and not this branch. The operator believes it is
  already resolved; worth a one-command confirmation, then delete the entry.

### Later

11. Review and merge consolidation.
12. Base reconciliation — behind `yubikit`, conflicts, contradictory `IProtocol` ownership docs. Parked
    deliberately; do not merge/rebase `yubikit` without a decision.

---

## Blockers & Known Issues

- **Windows FIDO HID needs elevation.** Not a defect. Windows restricts read/write on the FIDO HID
  top-level collection to elevated processes, so any path that opens a FIDO HID connection — the CCID-held
  Management fallback, and the F2/F3 integration tests — must run as Administrator on Windows. FIDO2 over
  its own HID transport for an app is subject to the same OS rule.
- **macOS OTP HID fault — unresolved, environment-level, not an SDK defect.** (Carried over from the macOS
  session, unchanged.) Every macOS OTP HID open failed with `IOHIDDeviceOpen = 0xE00002E2`
  (`kIOReturnNotPermitted`) while CCID worked; leading unconfirmed hypothesis is macOS **Input Monitoring**
  against the terminal. **Does not reproduce on Windows** — Windows OTP HID works after the F5 fix. If OTP
  HID misbehaves again on macOS, first check `ykman --device 103 otp info` for `WARNING: Failed opening
  device` (the warning line is the discriminator, not the exit status).
  **RESOLVED (Phase 12): a restart clears it.** The probe now returns clean with no warning line, and all
  OTP-dependent gates pass. This also **falsifies the Input Monitoring hypothesis** — TCC is persistent
  policy and would have survived the reboot. The condition lived in transient kernel/daemon HID state that
  survives USB re-enumeration but not a restart, which is why three replugs and quitting Wispr Flow all
  failed. Cause still unidentified; no further attribution offered. **Operator remedy: restart, not replug.**
  **Phase 11 raises the prior on the Input Monitoring hypothesis.** F5 established that on Windows the OTP
  interface is a *keyboard top-level collection* and the OS refuses read/write on it even when elevated —
  an explicit anti-keylogger restriction. macOS protects the same class of device through Input Monitoring
  (`kTCCServiceListenEvent`), and `kIOReturnNotPermitted` is a permission-class status. Two different
  operating systems restricting the same keyboard collection is corroboration, not proof. Note the shapes
  differ: Windows was fixed by asking for *less* access, whereas macOS already opens with the minimum
  (`kIOHIDOptionsTypeNone`), so no equivalent code-side lever is known — which is why the leading
  hypothesis remains environmental rather than a code defect.

---

## Key File References

| File | Purpose |
|---|---|
| `docs/plans/session-contention/ISA.md` | The evidence ledger. **Phase 11** is this Windows session |
| `docs/plans/session-contention/edge-case-register.md` | 23 rows, zero open, zero gaps. F2/F3 covered, F4 cross-platform, **F5 new** |
| `src/Core/src/Native/Windows/HidD/HidDDevice.cs` | The F5 fix — `OpenFeatureConnection` opens with `DESIRED_ACCESS.NONE` |
| `src/Core/tests/.../Devices/FidoHidSharingIntegrationTests.cs` | F2/F4 hardware pins incl. the misrouting diagnostic |
| `src/Core/tests/.../Devices/CompositeDiscoveryIntegrationTests.cs` | Typed-transport connect; pins F5 (was RED on Windows) |
| `src/Piv/tests/.../PivSessionContentionTests.cs` | F3 pins; the `pcsc:` identity assertion |
| `src/Core/src/Devices/DeviceConnectionRegistry.cs` | Where exclusive vs shared is enforced |
| `src/Tests.Shared/appsettings.json` | The allow list (includes 103 and 125) |
| `src/Core/CLAUDE.md` | Concurrency model + shared-FIDO wording |

---

## Quick Start for New Agent

```bash
git checkout yubikit-session-contention && git pull

# NEVER dotnet build / dotnet test directly. Long options need the `--` separator.
dotnet toolchain.cs build
dotnet toolchain.cs test

# Windows hardware work — RUN FROM AN ADMINISTRATOR TERMINAL, keys plugged in BEFORE the runner starts:
dotnet toolchain.cs -- test --integration --project Piv  --smoke --filter "FullyQualifiedName~PivSessionContentionTests"
dotnet toolchain.cs -- test --integration --project Core --smoke --filter "FullyQualifiedName~FidoHidSharingIntegrationTests"
dotnet toolchain.cs -- test --integration --project Core --smoke --filter "FullyQualifiedName~CompositeDiscoveryIntegrationTests"
dotnet toolchain.cs -- test --integration --project YubiOtp --smoke

# Formatting: use the split subcommands, not the unqualified one
dotnet format whitespace --verify-no-changes
dotnet format style      --verify-no-changes --severity error   # exits 2 on PRE-EXISTING native naming, not this branch
dotnet format analyzers  --verify-no-changes --severity error
```

**Prerequisites:** authorized serials must be in `src/Tests.Shared/appsettings.json` (includes 103 and 125).
Devices must be connected *before* the test runner starts. On Windows, run the HID integration tests
elevated. Do not run touch/insert/remove tests unless a human is coordinating.

**Working rules:** stage only files you changed explicitly; never `git add .`/`-A`/`-a`. Do not merge or
rebase `yubikit`. Do not weaken an assertion to make a hardware test pass — record the failure instead.
</content>
</invoke>
