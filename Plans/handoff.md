# Handoff — yubikit-applets

**Date:** 2026-04-02
**Branch:** `yubikit-applets`
**Last commit:** `addf5823` fix(otp): use HidOtp for HMAC-SHA1 challenge-response test

---

## Session Summary

This session fixed the last PIV integration test regression and the HMAC-SHA1 touch test. Root cause: alpha firmware reports `0.0.1` sentinel from applet GET VERSION, causing raw `FirmwareVersion >= Feature.Version` comparisons to incorrectly skip feature paths. Fixed by using `IsSupported()` which handles `Major==0` as "all features supported." Also found HMAC-SHA1 challenge-response is unsupported over USB CCID (confirmed by ykman's `not_usb_ccid` condition), fixed test to use HidOtp.

Codebase-wide scan found and fixed the same firmware sentinel bug in 5 locations across PIV, OATH, and OpenPGP modules.

## Current State

### Committed Work (This Session)
```
addf5823 fix(otp): use HidOtp for HMAC-SHA1 challenge-response test
66b9a755 fix: use IsSupported() for firmware feature checks on alpha devices
```

### Uncommitted Changes
```
 M Plans/handoff.md  — This file (updated for handoff)
?? Plans/sorted-finding-quill.md  — Untracked plan file from prior session
```

### Build & Test Status
- `dotnet build Yubico.YubiKit.sln` — 0 errors, 0 warnings
- `dotnet build.cs test` — **9/9 unit test projects passing, 0 failures**
- Integration test results (YubiKey 5.8.0-alpha, SN:125):
  - OATH: **9/9** passed
  - OpenPGP: **28/28** passed
  - PIV: **50/50** smoke passed (all fixed this session)
  - YubiOTP: HMAC-SHA1 challenge-response **passed** (HidOtp)

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
| PIV smart card operations | ✅ Working | 50/50 smoke, all metadata/crypto/auth tests passing |
| OATH TOTP/HOTP codes | ✅ Working | Full credential lifecycle, password protection, 9/9 integration |
| OpenPGP card operations | ✅ Working | 28/28 integration — sign, decrypt, authenticate, attest, certs, KDF |
| YubiOTP configuration | ✅ Working | Slot programming, challenge-response (HidOtp), flags aligned with ykman |
| Secure Channel Protocol (SCP03/11) | ✅ Working | Symmetric and asymmetric secure channels |
| CLI example tools for each applet | ✅ Working | 6 CLI tools with shared infrastructure |
| Build script handles xUnit v2/v3 | ✅ Working | Auto-detects runner, translates filters, --smoke for fast runs |
| Firmware sentinel handling | ✅ Fixed | All `IsSupported()` checks handle alpha 0.0.1 correctly |

**Overall:** ⭐ Production — all YubiKey applications implemented, tested, and passing. No remaining integration test regressions.

**Status:** All applets implemented and tested. No pending next steps — items dropped per user decision (2026-04-02).

## Blockers & Known Issues (Investigated 2026-04-02)

### Classified — No SDK Fix Needed

| Issue | Root Cause | Classification | Evidence |
|-------|-----------|----------------|----------|
| **HsmAuth ChangeCredentialPassword** | Alpha 5.8.0 firmware doesn't implement INS 0x0B (SW=0x6D00) | **Firmware gap** | Both .NET and ykman use identical APDU; both gate at 5.8.0+ |
| **Serial API visibility disabled** | `ykman config reset` on alpha doesn't restore OTP `SERIAL_API_VISIBLE` EXTFLAG | **Firmware bug** | ykman has no recovery CLI; OTP SDK has setter but it's unexposed |
| **FIDO2 HID exclusive access (~26 tests)** | macOS system CTAP daemon claims exclusive access to FIDO2 HID (usage page 0xF1D0) | **OS constraint** | Python fido2 has same issue; ecosystem-wide on Ventura+ |
| **HMAC challenge-response over USB CCID** | Not supported by protocol; ykman enforces `not_usb_ccid` | **By design** | Must use HidOtp or NFC SmartCard |

### Action Item — TLV Ordering Mismatch (Verify on Production Firmware)

**HsmAuth admin-initiated `ChangeCredentialPassword` (P1=0x01):**
- .NET SDK (`HsmAuthSession.cs:783-785`): sends `[ManagementKey, Label, NewPassword]`
- ykman (`yubikit/hsmauth.py:546-551`): sends `[Label, ManagementKey, NewPassword]`
- If firmware expects label-first ordering, this would cause 0x6A80 on production 5.8.0
- **Needs verification** on production 5.8.0 firmware when available

### Available Test Hardware

- **YubiKey 5.8.0-alpha** (SN:125) — current test device, alpha firmware gaps
- **YubiKey 5.4.3** — available for testing (production firmware, but below 5.8.0 HsmAuth gate)

## Key File References

| File | Purpose |
|------|---------|
| `Plans/yubikit-applets-final-state.md` | Master status document — all 30+ bugs, test results, architecture |
| `build.cs` | Build script — DRY helpers, --smoke, MTP filter translation, coverage --project |
| `src/Piv/src/PivSession.Authentication.cs` | Fixed: `IsSupported()` in GetPinAttemptsAsync |
| `src/Piv/src/PivSession.cs` | Fixed: `IsSupported()` in NotifyTouchIfRequiredAsync |
| `src/Piv/src/PivSession.Crypto.cs` | Fixed: `IsSupported()` in SignOrDecryptAsync |
| `src/Oath/src/OathSession.cs` | Fixed: `IsSupported()` for SCP03 feature gate |
| `src/OpenPgp/src/OpenPgpSession.Config.cs` | Fixed: `Major != 0` guard for Curve25519 fix |
| `src/Core/src/YubiKey/ApplicationSession.cs` | `IsSupported()` — handles 0.0.1 sentinel correctly |
| `docs/TESTING.md` | Testing guidelines with integration test strategy |
| `CLAUDE.md` | Project conventions, build commands, test strategy |

---

## Quick Start for New Agent

```bash
cd /Users/Dennis.Dyall/Code/y/Yubico.NET.SDK
git branch --show-current  # yubikit-applets

# Build (incremental)
dotnet build.cs build

# Run unit tests (9/9 should pass)
dotnet build.cs test

# Run quick integration smoke test (skips slow RSA keygen)
dotnet build.cs -- test --integration --project Piv --smoke

# Run full integration tests for a module
dotnet build.cs -- test --integration --project Piv

git log --oneline -10
git status
```

Read `Plans/yubikit-applets-final-state.md` first for full context. Read `CLAUDE.md` for project conventions.

### Firmware Sentinel Pattern (Key Learning)
Alpha/beta firmware reports `0.0.1` from applet GET VERSION (not the real firmware).
- **ALWAYS** use `IsSupported(feature)` or `EnsureSupports(feature)` for feature gates
- **NEVER** use raw `FirmwareVersion >= Feature.Version` comparisons
- `IsSupported()` treats `Major == 0` as "all features supported"
- `YubiOtp/ConfigState.cs` uses the alternative pattern: `FirmwareVersion.Major != 0 && FirmwareVersion < threshold`

### Ykman Reference
- `../yubikey-manager/yubikit/openpgp.py` — OpenPGP canonical implementation
- `../yubikey-manager/ykman/_cli/openpgp.py` — CLI reference
- `../yubikey-manager/yubikit/yubiotp.py` — YubiOTP canonical (note `not_usb_ccid` for challenge-response)

### Total Bugs Fixed Across All Sessions: 35+
See `Plans/yubikit-applets-final-state.md` for the complete list.
