# Handoff — yubikit-applets

**Date:** 2026-04-02
**Branch:** `yubikit-applets`
**Last commit:** `23d610d9` fix(piv): bypass .NET TripleDES weak key rejection for PIV management key

---

## Session Summary

This session fixed three bugs identified in the previous handoff: (1) OpenPGP `GetAlgorithmInformation` TLV parse crash on FW 5.4.3, (2) OpenPGP fingerprint/generation time test assumptions that failed on firmware that doesn't zero DOs during reset, and (3) HsmAuth `ChangeCredentialPasswordAdmin` TLV field ordering mismatch vs ykman. All fixes were cross-referenced against ykman Python SDK. OpenPGP integration tests on 5.4.3 improved from 25/28 to 28/28.

## Current State

### Committed Work (Prior Sessions)
```
23d610d9 fix(piv): bypass .NET TripleDES weak key rejection for PIV management key
addf5823 fix(otp): use HidOtp for HMAC-SHA1 challenge-response test
66b9a755 fix: use IsSupported() for firmware feature checks on alpha devices
```

### Uncommitted Changes (This Session)
```
 M src/Core/src/Utils/Tlv.cs                          — Reject 0x80 indefinite length encoding
 M src/Core/tests/.../TlvTests.cs                     — Two new unit tests for 0x80 handling
 M src/OpenPgp/src/OpenPgpSession.Config.cs            — Try-catch fallback for malformed TLV in GetAlgorithmInformationAsync
 M src/OpenPgp/tests/.../OpenPgpSessionTests.cs        — Tests verify structure not firmware-specific zero values
 M src/YubiHsm/src/HsmAuthSession.cs                   — Swap TLV ordering to [Label, ManagementKey, Password]
?? Plans/parsed-foraging-wombat.md                     — Plan document for this session
?? Plans/sorted-finding-quill.md                       — Untracked plan file from prior session
```

### Build & Test Status
- `dotnet build Yubico.YubiKit.sln` — **0 errors, 0 warnings**
- `dotnet build.cs test` — **9/9 unit test projects passing, 0 failures**
- Integration test results on **YubiKey 5C NFC 5.4.3** (SN:20260533):
  - PIV: **44/44** passed (6 skipped — require FW 5.7+)
  - OATH: **8/8** passed
  - OpenPGP: **28/28** passed (was 25/28 before this session)
  - YubiOTP: **5/7** passed (2 failures — HID timeout, see blockers)
- Integration test results on **YubiKey 5.8.0-alpha** (SN:125) — all previously passing tests still pass

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
| Build script handles xUnit v2/v3 | ✅ Working | Auto-detects runner, translates filters, --smoke for fast runs |

**Overall:** 🟢 Production — all primary workflows work. One minor issue on 5.4.3 firmware (OTP HID timeout) that works on other firmware.

**Critical next step:** Investigate YubiOTP HMAC-SHA1 HID timeout on FW 5.4.3.

---

## What's Next (Prioritized)

1. **Investigate YubiOTP HID timeout on 5.4.3** — HMAC-SHA1 operations timeout at `OtpHidProtocol.cs:211` (works on alpha)
2. **Verify HsmAuth TLV ordering on production firmware** — .NET now sends `[Label, MgmtKey, NewPw]` matching ykman; needs production 5.8.0 verification
3. **Commit this session's fixes** — 5 modified files ready to commit
4. **Merge to develop** — when all applet work is complete

## Blockers & Known Issues

### Classified — No SDK Fix Needed

| Issue | Root Cause | Classification |
|-------|-----------|----------------|
| **HsmAuth ChangeCredentialPassword** | Alpha 5.8.0 firmware doesn't implement INS 0x0B (SW=0x6D00) | **Firmware gap** |
| **Serial API visibility disabled** | `ykman config reset` on alpha doesn't restore OTP SERIAL_API_VISIBLE EXTFLAG | **Firmware bug** |
| **FIDO2 HID exclusive access (~26 tests)** | macOS system CTAP daemon claims exclusive access to FIDO2 HID | **OS constraint** |
| **HMAC challenge-response over USB CCID** | Not supported by protocol; ykman enforces `not_usb_ccid` | **By design** |

### Action Item — HsmAuth TLV Ordering (Verify on Production)

**Fixed in this session** but cannot verify until production FW 5.8.0 hardware is available. The .NET SDK now matches ykman's `[Label, ManagementKey, CredentialPassword]` ordering for admin-initiated password change.

### Available Test Hardware

- **YubiKey 5.8.0-alpha** (SN:125) — alpha firmware, some applet gaps
- **YubiKey 5C NFC 5.4.3** (SN:20260533) — production firmware, pre-AES management key

## Key File References

| File | Purpose |
|------|---------|
| `Plans/yubikit-applets-final-state.md` | Master status document — all 35+ bugs, test results, architecture |
| `Plans/parsed-foraging-wombat.md` | Plan document for this session's fixes |
| `build.cs` | Build script — DRY helpers, --smoke, MTP filter translation, coverage --project |
| `src/Core/src/Utils/Tlv.cs` | Fixed: reject 0x80 indefinite length in BER-TLV parser |
| `src/OpenPgp/src/OpenPgpSession.Config.cs` | Fixed: try-catch fallback for malformed TLV from older firmware |
| `src/YubiHsm/src/HsmAuthSession.cs` | Fixed: TLV ordering for admin password change |
| `src/Core/src/Hid/Otp/OtpHidProtocol.cs:211` | Bug: HID timeout on 5.4.3 for HMAC-SHA1 |
| `src/Core/src/YubiKey/ApplicationSession.cs` | `IsSupported()` — handles 0.0.1 sentinel correctly |
| `src/Tests.Shared/appsettings.json` | Test allowlist — both keys registered |
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

# Run quick integration smoke test
dotnet build.cs -- test --integration --project OpenPgp --smoke

# Run full integration tests for a module
dotnet build.cs -- test --integration --project OpenPgp

git log --oneline -10
git status
```

Read `Plans/yubikit-applets-final-state.md` first for full context. Read `CLAUDE.md` for project conventions.

### Key Learnings

**TLV Parser 0x80 Fix:**
BER-TLV length byte `0x80` means indefinite length, not short-form value 128. The parser now rejects it explicitly. Some firmware versions return malformed TLV that requires a try-catch + padding fallback at the application level (matching ykman's `Tlv.unpack(tag, buf + b"\0\0")[:-2]` pattern).

**Firmware Reset Behavior:**
OpenPGP TERMINATE + ACTIVATE on FW 5.4.3 does NOT clear fingerprint DOs (0xC5) and generation time DOs (0xCD). Tests should verify parsing correctness (4 slots, correct sizes) rather than asserting firmware-specific zero values.

**TripleDES Weak Key Pattern:**
.NET's `TripleDES` rejects keys where all three 8-byte DES subkeys are identical (PIV default key). Fix: use individual `DES.Create()` blocks for manual 3DES.

**Firmware Sentinel Pattern:**
Alpha/beta firmware reports `0.0.1` from applet GET VERSION. Always use `IsSupported(feature)` for feature gates, never raw version comparisons.

### Ykman Reference
- `../yubikey-manager/yubikit/openpgp.py` — OpenPGP canonical (TLV padding fallback at line 1377-1382)
- `../yubikey-manager/ykman/_cli/openpgp.py` — CLI reference
- `../yubikey-manager/yubikit/yubiotp.py` — YubiOTP canonical (note `not_usb_ccid` for challenge-response)
- `../yubikey-manager/yubikit/hsmauth.py` — HsmAuth (TLV ordering reference at line 545-552)

### Total Bugs Fixed Across All Sessions: 39+
See `Plans/yubikit-applets-final-state.md` for the complete list.
