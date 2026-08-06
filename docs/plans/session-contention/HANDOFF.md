# Handoff — yubikit-session-contention

**Date:** 2026-08-06 (Windows session)
**Branch:** `yubikit-session-contention`
**Last commit:** `1031890b` — docs: close F2/F3 on Windows and record the OTP HID feature-open fix
**Written for:** resuming on any platform; the remaining hardware item wants an operator-coordinated hotplug

> Committed to the repo rather than `Plans/handoff.md` on purpose: `Plans/` is untracked, so a handoff
> written there never reaches another machine. This file travels with the branch.

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

1. **E1/E2 — physical DeviceId tier flip.** Needs two same-PID keys inserted/removed. Currently pinned by
   repository unit tests only; ISA records it as "planned, not run". The Windows rig has two same-PID
   firmware-5.8.0 keys (103/125) plugged in now, which is a clean place to do it. Requires a human to
   coordinate the insert/remove.

### Canonical-verification queue (no hardware needed)

Use skill `_YUBIKIT_CANONICAL_SOURCE`. Rust `ykrust-auto` @ `9fe08d9a` (macOS path
`/Users/Dennis.Dyall/Code/y/yubikey-manager-rust-auto`).

1. **OTP HID exclusivity vs canonical — highest value.** The branch's central contract; still rests on our
   own reasoning.
2. CCID per-interface exclusivity vs canonical.
3. F4 — does canonical support concurrent CTAP over two FIDO handles, or one-at-a-time like us? (Now known
   to be one-at-a-time on all three of our platforms.)
4. Management transport fallback order `SmartCard -> HidFido -> HidOtp` vs canonical. Note the Windows twist:
   `HidFido` needs elevation, so the practical Windows fallback from a held CCID is elevation-gated. Worth
   checking whether canonical prefers OTP HID over FIDO HID on Windows, which would avoid the elevation
   requirement entirely — a possible future improvement, not a defect.
5. Composite grouping / DeviceId tier model vs Rust `device.rs`.

### Evidence and process debt (carried over)

6. **Reconcile contradictory formatting rows.** Earlier ISA rows recorded unqualified
   `dotnet format --verify-no-changes` as "0 errors"; it actually exits **2** on pre-existing native
   naming. Cato flagged this.
7. **Record an explicit Phase 3–4 cross-vendor review verdict.** Marked as a blocking merge item but no
   verdict is recorded.
8. **Re-run Cato** on `docs/plans/session-contention/ISA.md`.
   `bun ~/.claude/skills/Cato/Tools/CatoRun.ts <artifact> --current-vendor openai`
9. Identify the 2 transient `Fido2` integration failures seen once on macOS after the seize change.
10. Capture the removal-time exception type in D3 (needs one more coordinated unplug).

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
