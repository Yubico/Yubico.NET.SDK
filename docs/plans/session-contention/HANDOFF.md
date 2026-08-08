# Handoff — yubikit-session-contention

**Date:** 2026-08-07
**Branch:** `yubikit-session-contention`
**HEAD:** `3edfbbfb` — docs: record Phase 21, base reconciliation executed
**Position:** **63 commits ahead of `origin/yubikit`, 0 behind.** Everything is pushed.
**Written for:** a cold-start agent on any platform. The remaining hardware items need a Windows box.

> Committed to the repo rather than `Plans/handoff.md` on purpose: `Plans/` is untracked, so a handoff
> written there never reaches another machine. This file travels with the branch.

---

## ⚠️ Read this before acting on any older document

**The previous handoff said "Do not merge or rebase `yubikit`". That instruction is obsolete.** The merge
was executed on 2026-08-07 (`d34eef08`) and upstream is fully absorbed — the branch is 0 behind. If you
find that instruction quoted anywhere else, it is stale.

Two related traps:

- The ISA is a **2,600-line append-only ledger**. Reading only its first 100 lines, or letting a tool cap
  the read at 50 KB, will give you a picture that is five phases out of date. The current-state section is
  near the top; the current *evidence* is in Phases 17–21 at the bottom. Read both.
- Several sections are explicitly marked **SUPERSEDED** and retained as history. Check for that banner
  before treating a section as a to-do list.

---

## State in one screen

| | |
|---|---|
| Blocking gates | **G4 — review and merge consolidation. The only one left.** G1, G2, G3, G5 all discharged |
| ISCs | All eight pass |
| Edge-case register | 23 rows, zero open, zero platform gaps |
| Base reconciliation | **Done** (Phase 21). 63 ahead / 0 behind |
| Working tree | Clean. Untracked and **never to be staged**: `.claude/worktrees/`, `.opencode/`, `Plans/` |
| Last full gate run | `3edfbbfb`, macOS, serials 103 + 125 — all green (see below) |

---

## What changed since the last handoff (Phases 17–21)

| Phase | Commits | Outcome |
|---|---|---|
| 17 | `8ef09522`, `cb260ede` | **G5 fixed.** Session binding moved out of the constructor into `ApplicationSession.Construct`, making the stranded-guard state unrepresentable rather than cleaned up afterwards. Two cleanup designs were rejected as unsafe first. All 8 factories converted |
| 18 | `9250e161` | Canonical answers to the four open questions (Rust + Python). CCID exclusivity **is** canonically motivated; OTP HID exclusivity is **ours alone**; concurrent CTAP over two FIDO handles is not supported anywhere; our Management fallback order has no canonical counterpart |
| 19 | `888daf6b` | Base reconciliation investigation; local paths scrubbed from `docs/plans/**` |
| 20 | `d464b9e8`, `561e896b` | **Fable deep audit.** Fixed 4, including two defects *inside the G5 change itself* — one a regression G5 introduced that the suite could not catch. 11 findings recorded unfixed |
| 21 | `f5cce73c`, `d34eef08`, `e03d01bb`, `3edfbbfb` | **The merge.** Ownership pins banked first, then `origin/yubikit` absorbed. `PivSession` constructor made `internal` |

Also: `5db10542`, `4b7e1cc4`, `eb4fe12c` corrected three ISA claims — the FIDO `SelectAsync` merge estimate,
the Windows `DESIRED_ACCESS` question (**`NONE` is confirmed correct**, not to be switched to
`GENERIC_WRITE`), and why `HidDDevice` needs no hidapi-style retry.

---

## Remaining work, in priority order

### 1. G4 — review and merge consolidation *(the only open gate)*

Everything it needs is in place. The merge itself was reviewed cross-vendor by `github-copilot/gpt-5.5`
(DevTeam) with verdict **`pass`, zero findings**.

Decide during G4: **what happens to `docs/plans/**` at merge.** Upstream `75a1a04b` deletes the directory
as internal working material before the public v2 alpha. This ISA lives there. Local-path leaks were
already scrubbed in `888daf6b`, so the remaining question is editorial, not hygiene.

### 2. Owed to the Windows machine *(cannot be run from macOS)*

| Item | Why it matters |
|---|---|
| FIDO `InitializeAsync` + `FirmwareVersion` equivalence with the old SELECT parse | **The higher-value of the two.** It touches the CCID-held Management fallback, which routes over FIDO HID |
| HID constructor-leak re-verify (`bbf07e8e`) | Confirms the G1 fix on the platform it was written for |

Windows HID integration tests **require an elevated shell** — Windows admits read/write on the FIDO HID
top-level collection only to an administrator. Non-elevated runs fail for a platform reason, not a code
reason.

### 3. Known-open defects — real, verified, none caused by this effort

Re-verified against the post-merge tree on 2026-08-07. The operator's decision was to keep this branch's
scope closed and file these as follow-up.

| # | Finding | Verified state |
|---|---|---|
| 5 | Post-dispose divergence across the eight sessions | `Oath` uses `ThrowIfDisposed` (12 sites); `Piv`/`Fido2` rely on `EnsureInitialized`; **`Management` has neither** |
| 6 | macOS CF objects never released | `CFRelease` appears **zero** times in *both* `MacOSHidIOReportConnection.cs` and `MacOSHidFeatureReportConnection.cs` |
| 7 | macOS constructor not failure-safe | `MacOSHidFeatureReportConnection` calls `SetupConnection()` bare in the ctor. **This is the macOS twin of the Windows bug fixed in G1** |
| 8–11 | Monitor duplicate retirement · `StartMonitoring` interval silently ignored · inverted merger evidence ledger · `DeviceId` uniqueness prose-only | Not re-verified post-merge |

**Paths moved in the merge:** `src/Core/src/Native/MacOS/` → `src/Core/src/Transports/Hid/MacOS/`. Cite the
new paths in any follow-up issue.

Findings #1, #2 and #4 were closed *by the merge itself* — #4 by upstream `16c3fe47`, not by us. #3 was
closed by `e03d01bb`.

### 4. Deferred / dropped by operator decision

Recorded so they are not silently reopened:

- **Linux E1/E2** — Linux has no Container ID either, so Phase 14's macOS run exercises the same degraded
  tiers. **Contested**: the G3 auditor argued an identical tier *model* does not imply identical platform
  behaviour (udev path stability, permissions, timing all differ). Re-opening is a live option.
- **macOS removal-wake plumbing** — the `RemovalCallback` was emptied rather than corrected in Phase 20.
  Waking a blocked read promptly needs the scheduled run loop plumbed through as callback context, plus
  unplug testing. Not changed blind with no operator present.
- **Inverted metadata fallback** (`NONE` → `READ|WRITE`) — `NONE` is confirmed correct; see `4b7e1cc4`.
- **D3 removal-time exception type** — the test does not assert on it.
- **2 transient Fido2 failures** — unreproducible across 3×29/29; recorded as "observed-good, not proven".
- **Hoist `DeviceInfo.SerialNumber` onto `IYubiKey`** — separate branch/issue. The identity docs must tell
  consumers "use the serial as your durable key" while that property is not reachable from `IYubiKey`.
- **143 firmware-gated `[Theory]` tests** — pre-existing; believed resolved by the Phase 8 repair. Needs one
  confirming run, then delete the entry.
- **Worktree `agent-aa7ba443d8eec3e9e`** — a different effort (FIDO2 ARKG-P256). Do not merge into it.

---

## Landmines

Things that will bite an agent who changes code here without knowing them.

**A protocol never owns its connection.** The interface lease belongs to the *connection*, so a protocol
disposing one it was handed releases that lease out from under its owner. Upstream still calls
`_connection.Dispose()` in all three protocols; this branch deliberately removed it. This survived the merge
only because `ProtocolConnectionOwnershipTests` was written and committed **before** merging (`f5cce73c`) —
`FidoHidProtocol`'s upstream side is a +16/−110 rewrite where "take theirs" was the low-friction move and
would have silently reintroduced the defect. **Do not weaken those pins.**

**`DisposalGate` makes a failed teardown terminal and shared**, so a later dispose replays the exception.
Upstream's equivalent test can use `using`; ours cannot. This interacts with
`DisposeAfterInitializationFailure`, which suppresses cleanup failures to preserve the primary exception.
The reason is recorded at the call site.

**`ManagementSession.Transport` exists only on this branch.** Upstream has no `Transport` concept. It was
silently lost during merge resolution and **only the PIV hardware contention tests caught it** — no unit
test covers it, and reading the diff did not reveal it. If you restructure `ManagementSession`, run PIV
integration.

**Never pipe `ykman otp info` through `head`** when diagnosing macOS OTP HID. It lets the command fall back
to CCID and exit 0 while HID is still broken. The discriminator is the presence of
`WARNING: Failed opening device`. If macOS OTP HID misbehaves: **restart, not replug** (Phase 13).

---

## Verification recipe

```bash
git checkout yubikit-session-contention && git pull

# NEVER dotnet build / dotnet test directly. Script long options need the `--` separator.
dotnet toolchain.cs build
dotnet toolchain.cs test
dotnet toolchain.cs -- resilience --fast     # when touching Core loops/lifecycle
dotnet toolchain.cs docs-qa                  # 54 active files

# Formatting: the split severity-scoped form IS the gate.
# Unqualified `dotnet format --verify-no-changes` exits 2 on PRE-EXISTING IL2026/IL3050
# trim-AOT warnings in src/Tests.TestProject/Program.cs. It is not your regression.
dotnet format whitespace --verify-no-changes
dotnet format style      --verify-no-changes --severity error
dotnet format analyzers  --verify-no-changes --severity error

# Hardware (macOS, serials 103 + 125). Keys must be plugged in BEFORE the runner starts.
dotnet toolchain.cs -- test --integration --project Core       --smoke
dotnet toolchain.cs -- test --integration --project Piv        --smoke
dotnet toolchain.cs -- test --integration --project YubiOtp    --smoke
dotnet toolchain.cs -- test --integration --project Management --smoke
```

**Last full run, `3edfbbfb`, macOS, serials 103 + 125:** build 0 errors · unit 12/12 projects · resilience ·
formatting clean · docs-qa 54 · Core integration **25/25** (incl. all five discovery invariants) · Piv
**76/76** (incl. PIN-clobber and refusal) · YubiOtp **10/10** · Management **40/40** · ownership pins 7/7 ·
G5 construction pins 5/5.

**Preconditions:** authorized serials must be in `src/Tests.Shared/appsettings.json` (103 and 125 are
present; an empty allow list hard-exits `-1`). `--smoke` skips `Slow` and `RequiresUserPresence`. Do not run
touch/insert/remove tests unless a human is coordinating.

**Working rules:** stage only files you changed explicitly — never `git add .` / `-A` / `-a`. Do not weaken
an assertion to make a hardware test pass; record the failure instead.

---

## Key files

| File | Purpose |
|---|---|
| `docs/plans/session-contention/ISA.md` | The evidence ledger, 21 phases. Current state at the top, current evidence in Phases 17–21 |
| `docs/plans/session-contention/edge-case-register.md` | 23 rows, zero open, zero platform gaps |
| `src/Core/src/Sessions/ApplicationSession.cs` | `Construct` (the G5 fix), the AC6 bind check, our disposal machinery, upstream's `InitializeProtocolAsync` |
| `src/Core/src/Sessions/ConnectionSessionGuard.cs` | One live session per connection. `Detach` is owner-aware |
| `src/Core/src/Devices/DeviceConnectionRegistry.cs` | Where exclusive vs shared is enforced |
| `src/Core/tests/.../Protocols/ProtocolConnectionOwnershipTests.cs` | The pins that guarded the merge. Do not weaken |
| `src/Core/tests/.../Devices/ConnectionOwnershipContractTests.cs` | The acquisition-time contract |
| `src/Piv/tests/.../PivSessionContentionTests.cs` | ISC-1 on hardware; the `pcsc:` identity assertion |
| `src/Core/src/Transports/Hid/MacOS/` | Fable #6/#7 live here (moved in the merge) |
| `src/Core/src/Native/Windows/HidD/HidDDevice.cs` | The F5 access split; `eb4fe12c` explains why no retry |
| `src/Core/CLAUDE.md` | Concurrency model; CCID-vs-OTP exclusivity provenance |
| `src/Tests.Shared/appsettings.json` | The hardware allow list |
