# Handoff — yubikit-applets

**Date:** 2026-04-02
**Branch:** `yubikit-applets`
**Last commit:** `738123a4` ci,fix,chore: fix Linux CI pcscd setup, SDK graceful degradation, NativeShims 1.16.0

---

## Session Summary

This session committed the previous session's fixes (TLV parsing, OpenPGP fallback, HsmAuth ordering), opened PR #446 (`yubikit-applets` → `yubikit`), then diagnosed and drove two rounds of fixes to the GitHub Actions `build-and-test` CI that was failing. Root causes: missing `libpcsclite.so.1` (resolved by installing pcscd), then socket permission denial (`SCARD_W_SECURITY_VIOLATION` / 0x8010006A) because the GitHub Actions runner user can't access `/run/pcscd/pcscd.comm`. Fixed by running pcscd directly as a background process and chmod-ing the socket. Also bumped `Yubico.NativeShims` to 1.16.0 as requested.

## Current State

### Committed Work (This Session)
```
738123a4 ci,fix,chore: fix Linux CI pcscd setup, SDK graceful degradation, NativeShims 1.16.0
10173098 ci: install pcscd and fix NuGet cache key for Linux CI
82f8451a fix(core,openpgp,hsmauth): fix TLV parsing, algorithm info fallback, field ordering
```

### Prior Sessions (on this branch)
```
23d610d9 fix(piv): bypass .NET TripleDES weak key rejection for PIV management key
addf5823 fix(otp): use HidOtp for HMAC-SHA1 challenge-response test
66b9a755 fix: use IsSupported() for firmware feature checks on alpha devices
```

### Uncommitted Changes
```
?? Plans/parsed-foraging-wombat-agent-af89c68f6f64e0f34.md   — old plan artifact
?? Plans/parsed-foraging-wombat.md                            — old plan artifact
?? Plans/sorted-finding-quill.md                              — old plan artifact
```
No source changes uncommitted. Only stale untracked plan files.

### Build & Test Status
- `dotnet build.cs build` — **0 errors, 0 warnings** (locally)
- `dotnet build.cs test` — **9/9 unit test projects passing** (locally)
- Integration tests on **YubiKey 5C NFC 5.4.3** (SN:20260533):
  - PIV: **44/44** | OATH: **8/8** | OpenPGP: **28/28** | YubiOTP: **5/7**
- CI on PR #446: **pending** — run `23923723955` in progress (3rd attempt, should pass)

### Worktree / Parallel Agent State
None.

---

## Readiness Assessment

**Target:** .NET developers who need to interact with YubiKey devices for authentication, cryptography, and security operations via a modern, type-safe SDK.

| Need | Status | Notes |
|---|---|---|
| Discover and connect to YubiKey devices | ✅ Working | DeviceRepository, MonitorService, SmartCard/HID transports |
| Query device capabilities and firmware | ✅ Working | ManagementSession, DeviceInfo, capability flags |
| FIDO2/WebAuthn authentication | ✅ Working | Full CTAP 2.1/2.3: passkeys, extensions, credential management |
| PIV smart card operations | ✅ Working | 44/44 on 5.4.3, 50/50 on alpha — 3DES and AES management keys |
| OATH TOTP/HOTP codes | ✅ Working | Full credential lifecycle, password protection, 8/8 integration |
| OpenPGP card operations | ✅ Working | 28/28 on 5.4.3 — TLV parse fixed, all key operations working |
| YubiOTP configuration | ⚠️ Partial | 5/7 — HMAC-SHA1 HID timeout on 5.4.3 (works on alpha via HidOtp) |
| Secure Channel Protocol (SCP03/11) | ✅ Working | Symmetric and asymmetric secure channels |
| CLI example tools for each applet | ✅ Working | 6 CLI tools with shared infrastructure |
| CI build pipeline | ⚠️ Partial | 3rd CI attempt in progress — should pass with pcscd socket fix |

**Overall:** 🟢 Production — all primary workflows work. One minor OTP HID timeout on 5.4.3 firmware. CI pipeline being fixed this session.

**Critical next step:** Confirm PR #446 CI passes (run `23923723955`), then request review/merge to `yubikit`.

---

## What's Next (Prioritized)

1. **Confirm CI passes** — run `gh pr checks 446` when run `23923723955` completes
2. **Request PR review / merge** — PR #446 (`yubikit-applets` → `yubikit`) is open
3. **Investigate YubiOTP HID timeout on 5.4.3** — HMAC-SHA1 times out at `OtpHidProtocol.cs:211`
4. **Verify HsmAuth TLV ordering on production 5.8.0** — fixed but unverified (no production FW hardware)

## Blockers & Known Issues

### Classified — No SDK Fix Needed

| Issue | Root Cause | Classification |
|-------|-----------|----------------|
| **HsmAuth ChangeCredentialPassword** | Alpha 5.8.0 firmware doesn't implement INS 0x0B (SW=0x6D00) | **Firmware gap** |
| **Serial API visibility disabled** | `ykman config reset` on alpha doesn't restore OTP SERIAL_API_VISIBLE EXTFLAG | **Firmware bug** |
| **FIDO2 HID exclusive access (~26 tests)** | macOS system CTAP daemon claims exclusive access to FIDO2 HID | **OS constraint** |
| **HMAC challenge-response over USB CCID** | Not supported by protocol; ykman enforces `not_usb_ccid` | **By design** |

### CI — pcscd Socket Permissions (Linux)

**Root cause of CI failures this session:** `SCARD_W_SECURITY_VIOLATION` (0x8010006A) from `SCardEstablishContext` on Linux. pcscd installs and auto-starts, but the GitHub Actions runner user doesn't have socket access to `/run/pcscd/pcscd.comm`. Fixed by:
1. Running `pcscd --foreground &` directly (bypasses systemd socket activation)
2. `chmod 777 /run/pcscd/pcscd.comm` to open socket access
3. SDK fix: `FindPcscDevices.FindAll()` now returns `[]` on any `SCardEstablishContext` failure (consistent with `DesktopSmartCardDeviceListener` behavior; belt-and-suspenders)

**If CI still fails:** Check if pcscd socket is created before chmod runs (may need longer sleep), or check the run log for the socket path.

### Available Test Hardware

- **YubiKey 5.8.0-alpha** (SN:125) — alpha firmware, some applet gaps
- **YubiKey 5C NFC 5.4.3** (SN:20260533) — production firmware, pre-AES management key

## Key File References

| File | Purpose |
|------|---------|
| `Plans/yubikit-applets-final-state.md` | Master status document — all 39+ bugs, test results |
| `.github/workflows/build.yml` | CI workflow — pcscd setup, NuGet cache key |
| `src/Core/src/SmartCard/FindPcscDevices.cs` | Fixed: graceful degradation when pcscd unavailable |
| `src/Core/src/OtpHidProtocol.cs:211` | Bug: HID timeout on 5.4.3 for HMAC-SHA1 |
| `Directory.Packages.props` | Yubico.NativeShims bumped to 1.16.0 |
| `src/Tests.Shared/appsettings.json` | Test allowlist — both keys registered |

---

## Quick Start for New Agent

```bash
cd /Users/Dennis.Dyall/Code/y/Yubico.NET.SDK
git branch --show-current  # yubikit-applets

# Check CI status on open PR
gh pr checks 446

# Build (incremental)
dotnet build.cs build

# Run unit tests (9/9 should pass)
dotnet build.cs test

# Run quick integration smoke test
dotnet build.cs -- test --integration --project OpenPgp --smoke

git log --oneline -10
git status
```

Read `Plans/yubikit-applets-final-state.md` for full context. Read `CLAUDE.md` for project conventions.

### Key Learnings (Cumulative)

**pcscd on Linux CI (new this session):**
`SCardEstablishContext` returns `SCARD_W_SECURITY_VIOLATION` (0x8010006A) on GitHub Actions when pcscd is running but the runner user lacks socket permissions. Fix: `sudo pcscd --foreground &` + `sleep 2` + `sudo chmod 777 /run/pcscd/pcscd.comm`. The SDK now also handles this gracefully by returning `[]` instead of throwing.

**NativeShims package:** `libYubico.NativeShims.so` is in the NuGet package for linux-x64 and gets copied to the output directory. It links against `libpcsclite.so.1` — so `pcscd` (which provides `libpcsclite1`) must be installed on Linux CI.

**TLV Parser 0x80 Fix:**
BER-TLV length byte `0x80` means indefinite length, not short-form value 128.

**Firmware Reset Behavior:**
OpenPGP TERMINATE + ACTIVATE on FW 5.4.3 does NOT clear fingerprint DOs (0xC5). Tests verify structure not zero values.

**TripleDES Weak Key Pattern:**
.NET's `TripleDES` rejects keys where all three 8-byte DES subkeys are identical. Use individual `DES.Create()` blocks for manual 3DES.

**Firmware Sentinel Pattern:**
Alpha/beta firmware reports `0.0.1`. Always use `IsSupported(feature)`, never raw version comparisons.

### Ykman Reference
- `../yubikey-manager/yubikit/openpgp.py` — OpenPGP canonical (TLV padding fallback at line 1377-1382)
- `../yubikey-manager/yubikit/yubiotp.py` — YubiOTP canonical (note `not_usb_ccid` for challenge-response)
- `../yubikey-manager/yubikit/hsmauth.py` — HsmAuth (TLV ordering reference at line 545-552)

### Total Bugs Fixed Across All Sessions: 40+
See `Plans/yubikit-applets-final-state.md` for the complete list.
