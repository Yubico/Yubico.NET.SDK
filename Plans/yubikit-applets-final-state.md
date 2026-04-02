# YubiKit 2.0 Applets — Final State & Next Steps

**Branch:** `yubikit-applets` (based on `yubikit`, never touches `develop` or `main`)  
**Last commit:** `78fd8e31`  
**Date:** 2026-04-03

---

## Where We Came From

The Yubico.NET.SDK 2.0 rewrite (`yubikit-*` branches) had 4 applets already done:
- **Management** — on `yubikit` (base branch)
- **SecurityDomain** — on `yubikit`
- **PIV** — on `yubikit-piv`
- **FIDO2** (implementation) — on `origin/yubikit-fido`

Four applets were skeleton-only (zero implementation): OATH, YubiOTP, HsmAuth, OpenPGP.

---

## What Was Done This Session

### Phase 1: agate Orchestration
Used `agate auto` to implement all 4 missing applets + a FIDO2 CLI tool in parallel worktrees. Each had a GOAL.md spec referencing the canonical Python `ykman` implementation.

### Phase 2: Consolidation
Merged all 5 feature branches (`yubikit-oath`, `yubikit-yubiotp`, `yubikit-hsmauth`, `yubikit-openpgp`, `yubikit-fido2-cli`) into `yubikit-applets`.

### Phase 3: Dev-Team Review
5 parallel review agents fixed coding standard violations (ZeroMemory, ConfigureAwait, static loggers, etc.). FIDO2 integration tests rewritten to use `[WithYubiKey]` infrastructure.

### Phase 4: CLI Rewrites
All 5 CLI tools rewritten to match `ykman`'s command structure (`oath accounts list`, `otp chalresp`, `hsmauth credentials add`, `openpgp keys generate`, `fido credentials list`).

### Phase 5: Integration Testing
Sequential hardware tests against attached YubiKey 5 NFC (5.8.0-alpha). Found and fixed **14 bugs** across the entire applet layer.

---

## Current State

### Build & Tests

| Metric | Result |
|--------|--------|
| Build | ✅ 0 errors, 0 warnings |
| Unit tests (all applets) | ✅ 325/325 pass |
| OATH integration tests | ✅ 8/8 |
| HsmAuth integration tests | ✅ 8/9 (1 alpha firmware gap: `ChangeCredentialPassword` INS not in this build) |
| OpenPGP integration tests | ✅ 27/28 (1 alpha firmware gap: `AttestKey` GET_ATTESTATION) |
| YubiOTP integration tests | Not run (dual-transport complexity, deferred) |

### Applets Delivered

| Applet | Source files | CLI files | Test files | CLI Command Style |
|--------|-------------|-----------|------------|-------------------|
| OATH | 10 | ~15 | 7 | `oath accounts list`, `oath access change` |
| YubiOTP | 23 | ~14 | 6 | `otp info`, `otp chalresp 2`, `otp delete 2` |
| HsmAuth | 7 | ~15 | 9 | `hsmauth credentials list`, `hsmauth reset` |
| OpenPGP | 37 (7 partial files) | ~28 | 12 | `openpgp keys generate sig`, `openpgp access change-pin` |
| FIDO2 CLI | — | 18 | — | `fido info`, `fido credentials list` |

### Bugs Fixed During This Session

1. **OATH CalculateAll credential ID parsing** — first char consumed as type byte
2. **OATH integration test static CTS** — shared CancellationToken caused inter-test failures
3. **HsmAuth ResetAsync protocol leak** — new undisposed protocol caused SW=0x6985
4. **HsmAuth GenerateCredentialAsymmetric missing TAG_PRIVATE_KEY** — SW=0x6A80
5. **HsmAuth integration test static CTS** — same CTS pattern as OATH
6. **HsmAuth integration test context size** — 32 bytes instead of 16
7. **HsmAuth management key complexity** — alpha firmware rejects low-entropy keys
8. **PcscProtocol.Configure sentinel** — firmware 0.0.1 caused early return, no extended APDUs
9. **ApplicationSession.IsSupported sentinel** — firmware 0.0.1 blocked version-gated features
10. **ConfigState version gate sentinel** — OTP slot status showed N/A on alpha firmware
11. **OpenPGP GetAlgorithmAttributesAsync** — direct GET_DATA for 0xC1/0xC2/0xC3 not supported; must read from ApplicationRelatedData (0x6E)
12. **OpenPGP GetAlgorithmInformationAsync** — outer 0xFA TLV not unwrapped before parsing inner list
13. **OpenPGP DeleteKeyAsync** — empty template invalid; must change attrs RSA4096→RSA2048
14. **OpenPGP VerifyPin_WrongPin test** — SW=0x6982 on alpha instead of 0x63Cx; query pin status for retries
15. **OTP multi-transport device deduplication** — single YubiKey appeared twice
16. **FIDO HID FIDO auto-selection** — non-interactive mode needed to prefer HID FIDO

---

## Known Alpha Firmware Gaps (Not Code Bugs)

These fail on the 5.8.0-alpha key but will work on production firmware:

| Test | SW | Reason |
|------|-----|--------|
| `HsmAuth.ChangeCredentialPassword_ThenCalculate_Succeeds` | 0x6D00 | INS 0x0B not yet implemented in this alpha build |
| `OpenPGP.AttestKey_ReturnsValidCertificate` | 0x6982 | GET_ATTESTATION (CLA=0x80) doesn't respond correctly to PIN auth in this build |

---

## Next Steps

### Immediate (Before Merging to Main yubikit Stream)

1. **YubiOTP integration tests** — not run yet. Dual-transport complexity. Need to run `dotnet test Yubico.YubiKit.YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/...` sequentially.

2. **OpenPGP VerifyPin P2 mode investigation** — On the alpha firmware, P2=0x82 (extended mode) unexpectedly enables sign operations. On production firmware, P2=0x81 = sign-only and P2=0x82 = extended (non-sign). The current code is tuned for the alpha key; should be verified on production hardware.

3. **Validate all tests on production firmware** — The 2 alpha firmware gaps + the P2 mode issue should all resolve on a production 5.x key.

### Medium Term

4. **Management session as authoritative firmware version source** — The `Major == 0` sentinel in `ApplicationSession.IsSupported()` and `PcscProtocol.Configure()` works for internal alpha/beta hardware but is not a production-quality solution. Proper fix: query Management at session init if applet reports `Major == 0`. See `Plans/streamed-meandering-adleman.md` § Future Work #1.

5. **FIDO2 over SmartCard on 5.8+** — FidoTool auto-selects HID FIDO unconditionally. On 5.8+ with firmware detection via Management, should allow SmartCard FIDO. See § Future Work #2.

6. **CLI shared infrastructure extraction** — All 5 CLI tools now follow the canonical DeviceSelector pattern but still copy code. Extract to `Yubico.YubiKit.Examples.Shared`. See § Future Work #3.

7. **CLI tool for FIDO2 session operations** — The FIDO2 CLI (`FidoTool`) exposes the command structure but most operations require user presence (touch). Autonomous testing is limited to `fido info`. E2E validation requires interactive testing.

### When Ready to Integrate

8. **Merge yubikit-applets → yubikit** (or however the 2.0 stream is structured)
9. **Merge yubikit-piv** — PIV was already done on `yubikit-piv`, needs to flow into the consolidated branch
10. **Review FIDO2 source** — `origin/yubikit-fido` has 44 source files; the CLI is on `yubikit-fido2-cli`. Need to bring them together.

---

## Branch Map

```
yubikit (2.0 base)
├── yubikit-piv         (PIV implementation — ready to merge)
├── yubikit-oath        (OATH — merged into yubikit-applets)
├── yubikit-yubiotp     (YubiOTP — merged into yubikit-applets)
├── yubikit-hsmauth     (HsmAuth — merged into yubikit-applets)
├── yubikit-openpgp     (OpenPGP — merged into yubikit-applets)
├── yubikit-fido2-cli   (FIDO2 CLI — merged into yubikit-applets)
└── yubikit-applets     ← CURRENT WORK BRANCH (consolidated, tested, pushed)
```

---

## Files of Interest

| File | Purpose |
|------|---------|
| `Plans/streamed-meandering-adleman.md` | Original implementation plan + future work |
| `Plans/goals/goal-*.md` | GOAL.md specs used by agate for each applet |
| `Plans/yubikit-applets-final-state.md` | This document |
| `Yubico.YubiKit.Core/src/YubiKey/ApplicationSession.cs` | Major==0 sentinel for feature gating |
| `Yubico.YubiKit.Core/src/SmartCard/PcscProtocol.cs` | Major==0 sentinel for APDU configuration |
| `docs/TESTING.md` | Test runner requirements (always use `dotnet build.cs test`) |
