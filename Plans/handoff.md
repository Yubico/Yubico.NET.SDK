# Handoff — yubikit-applets (cli-phase3-device-selector)

**Date:** 2026-04-02
**Branch:** `cli-phase3-device-selector` (based on `yubikit-applets`)
**Last commit:** `32733e32` feat(cli): extract shared CLI infrastructure into Yubico.YubiKit.Cli.Shared (#12)

---

## Session Summary

This session continued the YubiKit 2.0 applets branch. Primary work: (1) ran YubiOTP integration tests on alpha YubiKey 5 NFC hardware, found and fixed 4 bugs, added AllowUnknownSerials test infrastructure; (2) planned and executed CLI shared infrastructure extraction (#12) — created `Yubico.YubiKit.Cli.Shared/` project consolidating ~2600 LOC duplicated across 5 CLI tools, migrated all CLIs, applied reviewer fixes. Build passes clean (0 errors, 0 warnings).

## Current State

### Committed Work (This Session)
```
32733e32 feat(cli): extract shared CLI infrastructure into Yubico.YubiKit.Cli.Shared (#12)
b80e09ee docs: add CLI shared infrastructure extraction plan (#12)
364c8ae0 docs: update final state — YubiOTP tests done, AllowUnknownSerials documented
9931a0af fix(tests): add AllowUnknownSerials and pin YubiOTP tests to SmartCard
5b599f5c fix(yubiotp): fix 3 bugs found during integration testing on alpha firmware
```

### Uncommitted Changes
- `.claude/worktrees/` — leftover worktree directory from parallel agent work. Safe to clean up:
  ```bash
  git worktree remove .claude/worktrees/agent-a3e7c37b
  rm -rf .claude/worktrees/
  ```

### Build & Test Status
- `dotnet build Yubico.YubiKit.sln` — 0 errors, 0 warnings
- `dotnet build.cs test` — unit tests pass; integration tests require hardware
- Hardware test results (YubiKey 5 NFC, FW 5.8.0-alpha, SN: 125):
  - OATH: 8/8
  - HsmAuth: 8/9 (1 alpha firmware gap)
  - OpenPGP: 27/28 (1 alpha firmware gap)
  - FIDO2: 31/57 (HID exclusive-access contention, not code bugs)
  - YubiOTP: 6/7 (touch test needs user presence)

---

## What's Next (Prioritized)

1. **Merge branch back to `yubikit-applets`** — `cli-phase3-device-selector` needs merging into `yubikit-applets`
2. **Touch test: CalculateHmacSha1** — Requires user presence. Run:
   ```bash
   dotnet build.cs build
   dotnet test Yubico.YubiKit.YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/Yubico.YubiKit.YubiOtp.IntegrationTests.csproj \
     -c Release --no-build --logger "console;verbosity=normal" \
     --filter "FullyQualifiedName~CalculateHmacSha1"
   ```
   Touch the YubiKey when it blinks (~2-3 seconds after test starts).
3. **FidoTool reset (#30)** — Interactive: remove YubiKey, reinsert, hold touch 3-5 seconds:
   ```bash
   dotnet run --project Yubico.YubiKit.Fido2/examples/FidoTool/FidoTool.csproj -- reset --force
   ```
4. **Worktree cleanup** — Remove leftover agent worktrees
5. **Phase 4 (optional)** — InteractiveMenuBuilder + SessionHelper extraction (files exist in worktree but not wired up)

## Blockers & Known Issues

- **Alpha firmware gaps** (not code bugs, will pass on production FW):
  - HsmAuth `ChangeCredentialPassword` — INS 0x0B not implemented
  - OpenPGP `AttestKey` — GET_ATTESTATION doesn't handle PIN auth
  - OpenPGP `VerifyPin P2=0x82` — alpha has different behavior
- **Serial API visibility disabled** — `ykman config reset` permanently disabled serial API on alpha FW. `AllowUnknownSerials` config workaround is in place.
- **FIDO2 HID exclusive access** — macOS HID exclusive-access contention causes ~26 FIDO2 integration tests to fail. Not code bugs.
- **PIV branch** — lives on `yubikit-piv`, not yet merged into `yubikit-applets`

## Key File References

| File | Purpose |
|------|---------|
| `Plans/yubikit-applets-final-state.md` | Master status document — all bugs, test results, architecture |
| `Plans/cli-shared-infrastructure.md` | CLI extraction plan with phases and decision log |
| `Yubico.YubiKit.Cli.Shared/src/` | New shared CLI project (9 source files) |
| `docs/TESTING.md` | ALWAYS use `dotnet build.cs test`, never `dotnet test` directly |
| `Yubico.YubiKit.Tests.Shared/Infrastructure/` | AllowList, AllowUnknownSerials, WithYubiKeyAttribute |
| `Yubico.YubiKit.Core/src/YubiKey/ApplicationSession.cs` | IsSupported() with Major==0 sentinel |

---

## Quick Start for New Agent

```bash
cd /Users/Dennis.Dyall/Code/y/Yubico.NET.SDK

# Verify branch
git branch --show-current  # cli-phase3-device-selector

# Build
dotnet build.cs build

# Run unit tests
dotnet build.cs test

# Check status
git log --oneline -10
git status
```

Read `Plans/yubikit-applets-final-state.md` first for full context. Read `CLAUDE.md` for project conventions.

### Total Bugs Fixed Across All Sessions: 22
See `Plans/yubikit-applets-final-state.md` section "Bugs Fixed Across Entire Session History" for the complete list.
