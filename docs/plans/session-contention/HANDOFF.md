# Handoff — yubikit-session-contention

**Date:** 2026-08-06
**Branch:** `yubikit-session-contention` (pushed; local == `origin`)
**Last commit:** `cebc4037` — docs(isa): correct the OTP probe methodology and record the root test
**Written for:** resuming on a **Windows** machine

> Committed to the repo rather than `Plans/handoff.md` on purpose: `Plans/` is untracked, so a handoff
> written there never reaches another machine. This file travels with the branch.

---

## Session Summary

Closed the two remaining hardware items in the session-contention edge-case register using authorized macOS
hardware with the operator present. **F1 (macOS FIDO double-open) turned out to be a real product defect,
not the accepted platform gap it had been recorded as**, and was fixed against canonical Rust and Python.
**D3 (hotplug during an open session) was closed** and proved the exclusive CCID lease is not stranded when
a key is pulled. The register now has **zero open rows**; only the Windows rows remain, which is precisely
what the next machine can do.

A long-running macOS OTP HID fault also ran through the session. It is **not an SDK defect** and is
**unresolved**; three separate attributions were made and all three were wrong. That story is written up
honestly in the ISA because the reasoning error is instructive.

---

## Current State

### Committed this session

| Commit | What |
|---|---|
| `8ed7905a` | fix(core): harden session and interface ownership |
| `e977b532` | docs: reconcile session contention contracts |
| `d82218bf` | test(yubiotp): dispose caller-created OTP HID connections |
| `0bfcf669` | docs(isa): record ISC-4 pass and correct the OTP HID root cause |
| `619a4bf5` | **fix(core): open macOS FIDO HID non-seizing so shared FIDO is true** |
| `1021fcf5` | docs: correct the shared-FIDO contract and record F1 closure |
| `2a4fdb02` | test(piv): add D3 hotplug pin that the CCID lease is not stranded |
| `9a698160` | docs: close D3 on hardware and correct the Phase 9 OTP root cause |
| `32b0c61c` | style(piv): fix final newline in D3 hotplug test |
| `805af38c` | docs(isa): withdraw the Wispr Flow attribution; OTP cause unresolved |
| `cebc4037` | docs(isa): correct the OTP probe methodology and record the root test |

### Uncommitted changes

None in tracked files. Three untracked paths that must **never** be staged: `.claude/worktrees/`,
`.playwright-mcp/`, `Plans/`.

### Build & test status (macOS, serials 103 + 125)

| Gate | Result |
|---|---|
| `toolchain.cs build` | 0 errors (1 pre-existing `CA2254` in `src/Cli.Shared/src/Logging/StaticLoggerExtensions.cs`) |
| `toolchain.cs test` (unit) | 12/12 projects, 0 failed |
| `toolchain.cs -- resilience --fast` | passed |
| Core `FidoHidSharingIntegrationTests` | 3/3 |
| Fido2 integration `--smoke` | 29/29, three consecutive runs |
| Piv `PivSessionContentionTests` + `PivMultiKeyContentionTests` | 7/7 smoke |
| Piv `PivHotplugContentionTests` (D3) | 1/1, 1m47s, operator-coordinated |
| `dotnet format whitespace \| style \| analyzers --severity error` | clean |
| `docs-qa` | 55 files |

Unqualified `dotnet format --verify-no-changes` exits **2** on pre-existing `IL2026`/`IL3050` in
`src/Tests.TestProject/Program.cs:21` — not this branch's file. Use the three split subcommands.

### Worktree / parallel agent state

One extra worktree, **unrelated to this branch — do not merge into it**:

- Path: `.claude/worktrees/agent-aa7ba443d8eec3e9e`
- Branch: `worktree-agent-aa7ba443d8eec3e9e` @ `6988dc1d`
- Content: FIDO2 ARKG-P256 / previewSign port work, ~391 insertions
- **Dirty**: `CoseArkgP256SeedKey.cs`, `FidoPreviewSignTests.cs` have uncommitted edits
- Note: it contains its own copy of `MacOSHidIOReportConnection.cs` changes — expect a conflict with
  `619a4bf5` if these branches ever meet

---

## Readiness Assessment

**Target:** .NET developers integrating YubiKey hardware, who need concurrent applet sessions and device
discovery to coexist without silently destroying each other's state.

| Need | Status | Notes |
|---|---|---|
| A PIV session survives an unrelated `GetDeviceInfoAsync` | ✅ Working | The motivating footgun; pinned on hardware |
| Exclusive interfaces refuse a second connection clearly | ✅ Working | CCID + OTP HID, named-interface diagnostics |
| Shared FIDO HID admits a second connection | ✅ Working | Fixed this session; was broken on macOS |
| Concurrent CTAP over two FIDO handles | ⚠️ Bounded | Reports are not demultiplexed; drive CTAP one handle at a time (row F4) |
| Sessions on two different keys stay independent | ✅ Working | Incl. RSA-4096 cross-key liveness |
| Hotplug does not strand an interface lease | ✅ Working | Closed this session (D3) |
| Correct behaviour on **Windows** | ❌ Unverified | PC/SC sharing + HID open semantics never tested (F2, F3) |
| Cross-process contention | ❌ Out of scope | In-process by contract |

**Overall:** 🟢 **Production** on macOS and Linux for the target user's primary workflows; **unverified on
Windows**, which is the one material gap remaining.

**Critical next step:** Close **F2 and F3** on the Windows machine — Windows PC/SC sharing semantics under
contention, and Windows HID open behaviour. Everything else on this branch is evidence-complete.

---

## What's Next (Prioritized)

### On Windows — the reason this handoff exists

1. **F3 — Windows PC/SC sharing semantics under contention.** Run
   `dotnet toolchain.cs -- test --integration --project Piv --smoke --filter "FullyQualifiedName~PivSessionContentionTests"`.
   The in-process seam is platform-independent, but native PC/SC behaviour is not. Expect the interesting
   case to be `ConnectAsync_SecondSmartCardConnection_WhilePivSessionOpen_IsRefused`, whose message asserts
   a `pcsc:` identity.
2. **F2 — Windows HID sharing semantics.** Run the three
   `Core --filter "FullyQualifiedName~FidoHidSharingIntegrationTests"` tests. On macOS the second FIDO open
   required removing `kIOHIDOptionsTypeSeizeDevice`; Windows uses a different HID stack, so
   `SendOnFirst_ReceiveOnSecond_RevealsReportMisrouting` may legitimately **fail** there — if it does, that
   is a *good* result meaning Windows demultiplexes, and row F4 becomes macOS-specific. Record it, do not
   "fix" the test to keep it green.
3. **Windows topology Tier 2**, carried over from the composite-merge effort.
4. **E1/E2 — physical DeviceId tier flip.** Needs two same-PID keys inserted/removed. Currently pinned by
   repository unit tests only; ISA:485-489 records it as "planned, not run". Deferred on macOS because the
   OTP fault distorts the discovered topology; a healthy Windows rig is a clean place to do it.

### Canonical-verification queue (no hardware needed)

Use skill `_YUBIKIT_CANONICAL_SOURCE`. Rust `ykrust-auto` @ `9fe08d9a` at
`/Users/Dennis.Dyall/Code/y/yubikey-manager-rust-auto`.

1. **OTP HID exclusivity — highest value.** It is this branch's central contract and currently rests on our
   own reasoning, not canonical. F1 showed how badly that can go.
2. CCID per-interface exclusivity vs canonical.
3. F4 — does canonical support concurrent CTAP over two FIDO handles, or one-at-a-time like us?
4. Management transport fallback order `SmartCard -> HidFido -> HidOtp` vs canonical.
5. Composite grouping / DeviceId tier model vs Rust `device.rs`.
6. Does canonical document macOS keyboard-grabber / Input Monitoring contention on OTP?

### Evidence and process debt

7. **Reconcile contradictory formatting rows.** ISA:444 and ISA:521 record unqualified
   `dotnet format --verify-no-changes` as "0 errors"; measured today it exits **2**. Cato flagged this.
8. **Record an explicit Phase 3–4 cross-vendor review verdict.** ISA:483-484 and ISA:645 mark it as *the*
   blocking merge item ("Do not merge without it"); ISA:772 asserts it happened but no verdict is recorded.
9. **Re-run Cato** on `docs/plans/session-contention/ISA.md`. Standing verdict is **fail (round 2)**. Its
   CRITICAL finding — attributing the OTP failure without ruling out this branch's own rework — is now
   resolved. Its WARNING (item 7) is not.
   `bun ~/.claude/skills/Cato/Tools/CatoRun.ts <artifact> --current-vendor openai`
10. Identify the 2 transient `Fido2` integration failures seen once immediately after the seize change;
    3×29/29 followed but those two were never captured.
11. Capture the removal-time exception type in D3 (needs one more coordinated unplug).

### Later

12. Review and merge consolidation.
13. Base reconciliation — 34 commits behind, 14 conflicts, contradictory `IProtocol` ownership docs. Parked
    deliberately; do not merge/rebase `yubikit` without a decision.
14. Verify/retire the stale defect record at ISA:623-625 (`Xunit.SkippableFact` — YubiOtp ran 10/10 today,
    so it is at least partly obsolete).
15. Pre-existing: 143 firmware-gated tests use plain `[Theory]` so they fail instead of skipping on older
    firmware. Not this branch.

---

## Blockers & Known Issues

- **macOS OTP HID fault — unresolved, environment-level, not an SDK defect.** Every OTP HID open fails with
  `IOHIDDeviceOpen = 0xE00002E2` (`kIOReturnNotPermitted`) while CCID works. Excluded by direct evidence:
  this branch's lease registry, orphaned testhosts, Wispr Flow (quit entirely — no process, no IORegistry
  client, still fails), Karabiner daemons, and USB re-enumeration (three replugs). `sudo` does not fix it,
  but `sudo` does not bypass TCC either, so that test is inconclusive rather than negative. Leading
  unconfirmed hypothesis: macOS **Input Monitoring** (`kTCCServiceListenEvent`) against the terminal.
  Unexplained contradiction on record: OTP worked earlier in the same session under the same process tree.
  A machine restart was pending at handoff — first check afterwards is whether
  `ykman --device 103 otp info` still prints `WARNING: Failed opening device`.
- **Probe caveat:** `ykman otp info` falls back to CCID, so it can exit successfully while HID is broken.
  The discriminator is the warning line, not the exit status. Do not pipe it through `head`.
- **ISC-4 precondition:** the recorded discovery 5/5 requires an openable OTP interface. Under the fault it
  reverts to 2 passed / 3 failed. Green, but conditional.
- **Operator hypothesis worth remembering:** `~/.gnupg/scdaemon.conf` sets `disable-ccid`, routing scdaemon
  through **PC/SC** — the same channel CCID tests use — and `gpg-agent.conf` sets `enable-ssh-support`. In
  any repo with an **SSH remote or signed commits**, a git operation can wake scdaemon and contend for the
  card. Measured as not firing in this repo (`commit.gpgsign=false`, HTTPS remote), but very plausibly real
  elsewhere. **On Windows this does not apply** in the same form; if integration tests start failing after
  git operations there, suspect Windows Hello / WebAuthn platform authenticator instead.

---

## Key File References

| File | Purpose |
|---|---|
| `docs/plans/session-contention/ISA.md` | The evidence ledger. Phases 9, 10 and both addenda are this session |
| `docs/plans/session-contention/edge-case-register.md` | 22 rows, zero open. F1 covered, F4 new bounded row, D3 covered |
| `src/Core/src/Transports/Hid/MacOS/MacOSHidIOReportConnection.cs` | The F1 fix — `IOHIDDeviceOpen(handle, 0)`, must stay non-seizing |
| `src/Core/tests/.../Devices/FidoHidSharingIntegrationTests.cs` | F1 + F4 hardware pins, incl. the misrouting diagnostic |
| `src/Piv/tests/.../PivHotplugContentionTests.cs` | D3 pin; self-fails if no removal occurs |
| `src/Core/src/Devices/DeviceConnectionRegistry.cs` | Where exclusive vs shared is enforced |
| `src/Core/src/Sessions/ApplicationSession.cs`, `src/Core/src/Devices/DisposalGate.cs` | Disposal / lease release |
| `src/Tests.Shared/appsettings.json` | The allow list. Core's own empty copy was removed this session |
| `src/Core/CLAUDE.md` | Concurrency model + the corrected shared-FIDO wording |

---

## Quick Start for New Agent

```bash
git checkout yubikit-session-contention && git pull

# NEVER dotnet build / dotnet test directly. Long options need the `--` separator.
dotnet toolchain.cs build
dotnet toolchain.cs test

# Windows hardware work (needs authorized keys plugged in BEFORE the runner starts):
dotnet toolchain.cs -- test --integration --project Piv  --smoke --filter "FullyQualifiedName~PivSessionContentionTests"
dotnet toolchain.cs -- test --integration --project Core --smoke --filter "FullyQualifiedName~FidoHidSharingIntegrationTests"
dotnet toolchain.cs -- test --integration --project Core --smoke --filter "FullyQualifiedName~CompositeDiscoveryIntegrationTests"

# Formatting: use the split subcommands, not the unqualified one
dotnet format whitespace --verify-no-changes
dotnet format style      --verify-no-changes --severity error
dotnet format analyzers  --verify-no-changes --severity error
```

**Windows prerequisites:** authorized serials must be in `src/Tests.Shared/appsettings.json` (currently
includes 103 and 125 — the Windows rig's keys may differ and an empty/mismatched list hard-exits with
`Environment.Exit(-1)`). Devices must be connected *before* the test runner starts. Do not run
touch/insert/remove tests unless a human is coordinating.

**Working rules:** stage only files you changed explicitly; never `git add .`/`-A`/`-a`. Do not merge or
rebase `yubikit`. Do not weaken an assertion to make a hardware test pass — record the failure instead.
