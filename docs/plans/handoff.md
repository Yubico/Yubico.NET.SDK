# Handoff — yubikit-composite-merge

**Date:** 2026-07-29
**Branch:** `yubikit-composite-merge` (pushed, in sync with origin)
**Last commit:** `35fa29b1` docs(isa): close the composite-merge effort

---

## Session Summary

Two efforts completed back to back. First, a **concurrency-model simplification** on
`yubikit-concurrency-fixes` (PR #528): three DevTeam-identified defects were fixed not by adding
lifecycle coordination but by removing it — publication is now epoch-gated, connection disposal is
one-shot with shared completion, and SCP construction is closed to its factory. Second, a
**composite-merge remediation** on this branch: same-PID YubiKeys (two or more keys of the same
model, indistinguishable at the USB layer) grouped nondeterministically with orphaned interfaces,
and now group deterministically, with a per-platform guarantees document stating exactly what
discovery can and cannot promise.

Both efforts were plan-first, cross-vendor-audited before implementation, and executed under a
binding evidence rule: every change proved by a RED test that failed for its predicted reason, or
by a diagnostics delta.

---

## Current State

### Committed Work

**Composite-merge effort** (this branch, `abbc6f7f..35fa29b1`, +3224/−111 across 20 files — only
662 lines of it production code):

| Commit | What |
|---|---|
| `abbc6f7f` | Cross-vendor-audited plan (`docs/plans/composite-merge-remediation/PLAN.md`) |
| `481dda81` | Phase 1 RED harness: 3 defect vectors + 18 pins + generalized integration invariants |
| `bb3c5cd8` | Phase 2 core fix: generalized PID guard, pigeonhole deduction, admission wait, cause discriminator |
| `34d35965` | Phase 3: Windows USB-topology tier behind scripted native seams |
| `48190f7e` | Phase 4 Tier 1: hardware reconfiguration matrix results |
| `2e5e3242` | Phase 5: `docs/architecture/device-discovery-guarantees.md` + three entry-point links |
| `35fa29b1` | ISA closure |

**Concurrency effort** (inherited into this branch's history via `fd8a8027`; PR #528 open):
`2e3abb6a` epoch-gated publication · `c14d7333` one-shot disposal · `8a587e22` SCP closure ·
`98e50001` six subtractions · `3697523f` ISA/doc reconciliation · `b2b84894` format fix ·
`1acb3e59` simplification review · `822c404c` + `1ae79694` cross-vendor review fixes · `44bb7b7f`
ISA evidence.

### Uncommitted Changes

None in tracked files. `git status` shows only two long-standing untracked directories:
`.claude/worktrees/` and `.playwright-mcp/`. Neither belongs to these efforts; do not stage them.

### Build & Test Status

At `2e5e3242` (unchanged by the docs-only `35fa29b1`):

- Full solution build: **0 errors** (1 pre-existing CS7022 in the NuGet-generated `Tests.TestProject` entry point)
- Full unit suite: **12/12 projects green**; Core 713 total, 710 passed, 3 pre-existing platform skips, 0 failed
- `dotnet toolchain.cs -- resilience --fast`: **69/69**
- `dotnet format --verify-no-changes`: **0 errors** (non-zero exit is only the pre-existing IL2026/IL3050 trim warnings in untouched `Tests.TestProject`)
- `git diff --check`: clean
- Hardware (two allow-listed keys, 103 + 125, fw 5.8.0): all five `CompositeDiscoveryIntegrationTests` invariants green; `PivMultiKeyContentionTests` **runs and passes 2/2** instead of skipping

### Worktree / Parallel Agent State

| Worktree | Branch | State |
|---|---|---|
| `.claude/worktrees/agent-aa7ba443d8eec3e9e` (locked) | `worktree-agent-aa7ba443d8eec3e9e` @ `6988dc1d` | **Unrelated FIDO2 previewSign/ARKG work.** 4 files / +391/−22 vs `origin/yubikit`. Has uncommitted changes in `CoseArkgP256SeedKey.cs` and `FidoPreviewSignTests.cs`. Not touched this session — leave alone. |
| `.claude/worktrees/agent-af2d812e` | — | Stale directory, not a registered worktree. |

**Stashes — read before using `git stash`:** `stash@{0}` is *not* ours. It is a pre-existing
`webauthn/phase-9.2-rust-port` WIP that an agent accidentally popped this session; it was traced
via `git fsck --unreachable` and restored with a `(restored: accidentally popped 2026-07-29)`
annotation. `stash@{1}` is an old `develop` WIP. Both verified intact. **Do not `git stash pop`
blind in this repo.**

---

## Readiness Assessment

**Target:** .NET application developers integrating YubiKey hardware, who need device discovery to
return one `IYubiKey` per physical key — reliably, including when several same-model keys are
connected — and who need to know which guarantees hold on their platform.

| Need | Status | Notes |
|---|---|---|
| One device per physical key, single key attached | ✅ Working | Unchanged path; pinned by PID-class vectors |
| Same-PID keys grouped completely and deterministically | ✅ Working | 20/20 identical groupings on hardware; 69 → 0 degraded identity reads |
| Concurrent sessions across two keys | ✅ Working | `PivMultiKeyContentionTests` 2/2 (was skipping) |
| Discovery never disturbs an open session | ✅ Working | Unchanged by design; in-use interfaces skipped, attributed once idle (G8) |
| Monitor lifecycle safe under stop/restart/dispose races | ✅ Working | Epoch-gated publication; three-round cross-vendor review ended PASS |
| Connections disposed exactly once, disposal means disposed | ✅ Working | `DisposalGate`; RED-verified |
| Reconfigured keys (capabilities changed) group correctly | ✅ Working | Hardware matrix passed; config restored and verified |
| Serial-less multi-interface keys, macOS/Linux | ⚠️ Documented bound | Cannot be grouped — no supported reader→USB mapping on either platform. Windows solves it via Container ID |
| Windows topology tier on real Windows hardware | ⚠️ Unvalidated | Decision tree and all failure modes seam-proven; P/Invoke marshalling and real ContainerId matching untested |
| Partially enumerated same-PID keys during hotplug | ⚠️ Documented bound | Complementary partials can transiently share a composite (G2); heals on evidence, blast radius is metadata not connection misdelivery |

**Overall:** 🟢 **Production** for the primary workflows — the defects that opened both efforts are
fixed, proved on hardware, and gated by tests. The two remaining ⚠️ rows are a genuine platform
bound and a deferred hardware validation, both documented rather than silent.

**Critical next step:** Validate the Windows topology tier on a Windows machine — it is the only
shipped code path with no hardware evidence behind it, and it is the one that closes the
serial-less-keys gap.

---

## What's Next (Prioritized)

1. **Windows hardware validation of the topology tier** (Phase 4 Tier 2). Verify the
   `SCardGetReaderDeviceInstanceIdW` two-call marshalling, real SCARD_* codes for absent readers,
   and that `CmDevice.ContainerId` actually matches across one YubiKey's CCID and HID interfaces.
   Until then, treat every "with topology evidence" cell in the guarantees matrix as seam-proven only.
2. **Merge PR #528** (`yubikit-concurrency-fixes` → `yubikit`), then rebase/retarget this branch so
   its diff is composite-merge only. Owner has explicitly de-gated this; sequencing is convenience,
   not dependency.
3. **Open a PR for this branch** once #528 lands.
4. **Linux hardware smoke** (Tier 2) — single key, confirm no regression on the udev/pcsc paths.
5. **Answer pending:** do multi-interface *serial-less* Security Key configurations exist in the
   field? If yes, the deferred macOS/Linux HID-to-HID topology follow-up gains value; if no, the G4
   bound stands as written. (Owner was going to ask management.)
6. **Optional follow-up:** per-transport scan isolation when PC/SC enumeration throws — tracked in
   `docs/plans/monitor-polling-migration/ISA.md`, deliberately out of scope for both efforts.

## Blockers & Known Issues

- **No Windows or Linux hardware available** in this environment — the only thing blocking item 1.
- **`stash@{0}` belongs to another effort** (see Worktree section). Do not pop blind.
- **CCID is not independently switchable on firmware 5.8.0** — measured, not assumed: FIDO2/U2F are
  exposed over CCID too, so disabling the CCID-exclusive applications leaves the interface present.
  Consequence: the 0x0403 PID class is covered by unit vectors only. Recorded in the ISA and the
  guarantees doc.
- **Two documented bounds are not bugs** — G2 (complementary partial visibility) and G4
  (serial-less keys on macOS/Linux). Both have pinning tests that assert the *bounded* behavior. A
  future agent "fixing" them will break those pins; read the guarantees doc first.
- **PR #532** (`yubikit-protocol-refactor`) is CONFLICTING, unrelated to this work.

## Key File References

| File | Purpose |
|---|---|
| `docs/architecture/device-discovery-guarantees.md` | **Read first.** What discovery guarantees per platform, each row citing its pinning test |
| `docs/plans/composite-merge-remediation/PLAN.md` | Audited plan: evidence hierarchy, epistemic bound, platform research, rejected alternatives |
| `docs/plans/composite-merge-remediation/ISA.md` | Evidence ledger — every RED failure verbatim, every pin, every diagnostics delta |
| `docs/plans/concurrency-resiliency-remediation/ISA.md` | The concurrency effort's ledger (PR #528) |
| `src/Core/src/Devices/CompositeDeviceMerger.cs` | The five-tier merge; pure and static — unit-vector it directly |
| `src/Core/src/Devices/IDeviceTopologyResolver.cs` | Tier-1 seam + platform selection; `NullDeviceTopologyResolver` off Windows |
| `src/Core/src/Devices/WindowsTopologyNativeOps.cs` | The three native ops behind the seam (the unvalidated surface) |
| `src/Core/src/Devices/ProtocolDeviceInfo.cs` | Worker admission; identity reads now wait rather than skip |
| `src/Core/src/Devices/YubiKeyDeviceMonitorService.cs` | Epoch model — `MonitorGeneration`, `_publishGate`, `_publishLock` |
| `src/Core/tests/.../Devices/CompositeDeviceMergerVectorTests.cs` | 26 vectors; the pins encode the documented bounds |
| `/var/folders/.../opencode/merge-diag/` | Phase 0/2 diagnostics harness (scratch, not in repo) |
| `/var/folders/.../opencode/reconfig-matrix/` | Phase 4 reconfiguration harness with unconditional restore (scratch) |

---

## Quick Start for New Agent

```bash
cd /Users/Dennis.Dyall/Code/y/Yubico.NET.SDK
git checkout yubikit-composite-merge

# Orientation — read in this order
cat docs/architecture/device-discovery-guarantees.md
cat docs/plans/composite-merge-remediation/PLAN.md
cat docs/plans/composite-merge-remediation/ISA.md

# Verify the baseline (never raw `dotnet build` / `dotnet test`; `--` separator is mandatory)
dotnet toolchain.cs build
dotnet toolchain.cs -- test --project Core          # expect 713 total, 0 failed, 3 skips
dotnet toolchain.cs -- resilience --fast            # expect 69/69

# Hardware, with allow-listed test keys attached (mutation authorized on these)
dotnet toolchain.cs -- test --integration --project Core --filter "FullyQualifiedName~CompositeDiscoveryIntegrationTests"
dotnet toolchain.cs -- test --integration --project Piv  --filter "FullyQualifiedName~PivMultiKeyContentionTests"
```

**House rules that bit this session:** the `--` separator is required for every script long option
(`dotnet` steals `--project` otherwise); filters do not support `|` OR — run separate invocations;
the toolchain's `TEST SUMMARY` counts *projects*, not tests (read the inline `total:/succeeded:`
lines); the repo convention is files **without** a trailing final newline; stage only files you
changed, never `git add .`.

Pick up with `/resume-handoff`.
