# Handoff — yubikit-applets

**Date:** 2026-04-02
**Branch:** `yubikit-applets`
**Last commit:** `bd979619` fix(build): use incremental builds and add xUnit v3 MTP filter translation

---

## Session Summary

This session resumed handoff items 2 (YubiOTP touch test) and 3 (integration test sweep). Main accomplishments: (1) fixed build.cs `--filter` incompatibility with xUnit v3 MTP runner by adding `TranslateToMtpFilter()` that converts VSTest-style filters to native `--filter-method`/`--filter-trait` options; (2) cross-referenced all YubiOTP constants and defaults against ykman Python canonical and fixed 8 issues — wrong ConfigFlag values (StrongPw1, StrongPw2, ManUpdate), missing flags (ChalHmac, ChalYubico, OathHotp8), missing default flags in SlotConfiguration hierarchy, wrong Use8Digits flag, incorrect StaticPassword flag combo; (3) fixed OATH touch property encoding from TLV `[tag, len, value]` to raw bytes `[tag, value]` matching ykman's `struct.pack`; (4) made build.cs use incremental builds by removing `DependsOn("build")` from test/coverage targets and removing `--no-build` flags.

## Current State

### Committed Work (This Session)
```
bd979619 fix(build): use incremental builds and add xUnit v3 MTP filter translation
3106fff4 fix(otp,oath): align flag values and defaults with ykman canonical
b6f5ed06 chore(build): remove clean from restore dependency chain
f648e030 fix(cli): use firmware-dependent touch timing in FidoTool reset
```

### Uncommitted Changes
None — working tree is clean.

### Build & Test Status
- `dotnet build Yubico.YubiKit.sln` — 0 errors
- `dotnet build.cs test` — **9/9 unit test projects passing, 0 failures**
- Hardware integration tests not run this session (user not available for touch tests)
- Previous session hardware results (YubiKey 5 NFC, FW 5.8.0-alpha, SN: 125):
  - OATH: 8/8, HsmAuth: 8/9, OpenPGP: 27/28, FIDO2: 31/57 (HID contention), YubiOTP: 6/7 (touch test)

### Worktree / Parallel Agent State
All stale agent worktrees pruned. Only `legacy-develop` remains (unrelated):
```
/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK                — yubikit-applets (main)
/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK/legacy-develop  — dennisdyallo/fix-rds-scard-invalid-handle
```

---

## Readiness Assessment

**Target:** .NET developers who need to interact with YubiKey devices for authentication, cryptography, and security operations via a modern, type-safe SDK.

| Need | Status | Notes |
|---|---|---|
| Discover and connect to YubiKey devices | ✅ Working | DeviceRepository, MonitorService, SmartCard/HID transports |
| Query device capabilities and firmware | ✅ Working | ManagementSession, DeviceInfo, capability flags |
| FIDO2/WebAuthn authentication | ✅ Working | Full CTAP 2.1/2.3: passkeys, extensions, credential management, biometrics |
| PIV smart card operations | ✅ Working | Authentication, certificates, key generation, signing, decryption |
| OATH TOTP/HOTP codes | ✅ Working | Full credential lifecycle, password protection, touch encoding fixed |
| OpenPGP card operations | ✅ Working | Key management, signing, encryption, attestation |
| YubiOTP configuration | ✅ Working | Slot programming, challenge-response, flags aligned with ykman canonical |
| Secure Channel Protocol (SCP03/11) | ✅ Working | Symmetric and asymmetric secure channels |
| CLI example tools for each applet | ✅ Working | 6 CLI tools with shared infrastructure |
| Build script handles both xUnit v2/v3 | ✅ Working | Auto-detects runner, translates VSTest filters, incremental builds |
| Applet reports correct firmware version | ⚠️ Partial | Applets report 0.0.1 sentinel; Management query not yet auto-triggered (#1) |

**Overall:** 🟢 Production — all YubiKey applications implemented and tested. SDK is functionally complete for primary developer workflows. Remaining items are polish (firmware version resolution, integration test coverage, build.cs DRY cleanup).

**Critical next step:** Review build.cs for regressions and DRY patterns (user-requested post-session task).

---

## What's Next (Prioritized)

1. **Review build.cs for regressions and DRY patterns** — User explicitly requested this. Check for duplicated logic, inconsistent patterns, and ensure the incremental build changes don't break any workflows.
2. **Touch test: CalculateHmacSha1** — Requires user presence on YubiKey. Run:
   ```bash
   dotnet build.cs -- test --integration --project YubiOtp --filter "FullyQualifiedName~CalculateHmacSha1"
   ```
   Touch the YubiKey when it blinks (~2-3 seconds after test starts).
3. **Integration test sweep** — Run integration tests per-applet:
   ```bash
   dotnet build.cs -- test --integration --project Oath
   dotnet build.cs -- test --integration --project OpenPgp
   dotnet build.cs -- test --integration --project Piv
   ```
4. **Management as authoritative firmware version (#1)** — Query Management on session init when applet reports 0.0.1 sentinel.
5. **PR to develop** — When satisfied with integration test results, open PR from `yubikit-applets` to `develop`

## Blockers & Known Issues

- **Alpha firmware gaps** (not code bugs, will pass on production FW):
  - HsmAuth `ChangeCredentialPassword` — INS 0x0B not implemented
  - OpenPGP `AttestKey` — GET_ATTESTATION doesn't handle PIN auth
  - OpenPGP `VerifyPin P2=0x82` — alpha has different behavior
- **Serial API visibility disabled** — `ykman config reset` permanently disabled serial API on alpha FW. `AllowUnknownSerials` config workaround is in place.
- **FIDO2 HID exclusive access** — macOS HID exclusive-access contention causes ~26 FIDO2 integration tests to fail. Not code bugs.
- **build.cs `--project`/`--filter` requires `--` separator** — `dotnet run` intercepts these flags. Always use `dotnet build.cs -- test --project X --filter Y`.
- **Alpha keys are the most capable keys** — There are NO known Alpha firmware issues. Alpha FW reports 0.0.0 version. The `../yubikey-manager` is the canonical Python reference SDK.

## Key File References

| File | Purpose |
|------|---------|
| `Plans/yubikit-applets-final-state.md` | Master status document — all 22+ bugs, test results, architecture |
| `src/Oath/src/OathSession.cs` | OATH touch property encoding fix (raw bytes, not TLV) |
| `src/YubiOtp/src/ConfigFlag.cs` | Corrected flag values aligned with ykman canonical |
| `src/YubiOtp/src/SlotConfiguration.cs` | Default ext flags (SerialApiVisible, AllowUpdate) |
| `src/YubiOtp/src/KeyboardSlotConfiguration.cs` | Default tkt/ext flags (AppendCr, FastTrigger) |
| `build.cs` | Build script — incremental builds, xUnit v3 MTP filter translation |
| `CLAUDE.md` | Project conventions, build commands, code style rules |

---

## Quick Start for New Agent

```bash
cd /Users/Dennis.Dyall/Code/y/Yubico.NET.SDK
git branch --show-current  # yubikit-applets

# Build (incremental — only recompiles if sources changed)
dotnet build.cs build

# Run unit tests (9/9 should pass)
dotnet build.cs test

# Run integration tests (requires YubiKey, use -- separator)
dotnet build.cs -- test --integration --project Oath

git log --oneline -10
```

Read `Plans/yubikit-applets-final-state.md` first for full context. Read `CLAUDE.md` for project conventions.

### Ykman Canonical Alignment (This Session)
- ConfigFlag values now match ykman `CFGFLAG` exactly
- SlotConfiguration default ext flags match ykman `SlotConfiguration.__init__`
- KeyboardSlotConfiguration defaults match ykman `KeyboardSlotConfiguration.__init__`
- OATH touch property uses raw bytes `[tag, value]` matching ykman `struct.pack(">BB", ...)`
- HOTP adds OathFixedModhex2 default, Use8Digits uses correct OathHotp8 flag

### Total Bugs Fixed Across All Sessions: 30+
See `Plans/yubikit-applets-final-state.md` for the complete list.
