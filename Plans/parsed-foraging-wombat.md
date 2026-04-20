# Plan: Fix OpenPGP TLV Parse, Fingerprint Test, HsmAuth TLV Ordering

## Context

Three bugs remain from the yubikit-applets integration testing sessions (handoff 2026-04-02). Items 1, 2, and 4 from the handoff's prioritized next steps. Item 3 (YubiOTP HID timeout) is deferred.

- **Bug 1:** `GetAlgorithmInformation` crashes with `ArgumentOutOfRangeException` on FW 5.4.3 due to TLV length byte `0x80` being mishandled
- **Bug 2:** `GetFingerprints_DefaultState_AllZero` fails when device has pre-existing keys — likely a cascading effect from Bug 1 or a test suite ordering issue
- **Bug 3:** `ChangeCredentialPasswordAdmin` sends TLV fields in wrong order vs ykman reference

Cross-referenced against ykman Python SDK (`yubikit/openpgp.py`, `yubikit/hsmauth.py`) for all fixes.

---

## Fix 1: OpenPGP TLV Parse Crash

### 1a. Fix `Tlv.ParseData()` boundary condition

**File:** `src/Core/src/Utils/Tlv.cs:221`

**Current:**
```csharp
if (length > 0x80)
```

**Fix:** Add explicit check for `== 0x80` before the long-form branch. Per BER-TLV (ISO 8825-1), `0x80` means indefinite length, which is invalid in determinate-length encoding. Throw `ArgumentException`.

```csharp
if (length == 0x80)
{
    throw new ArgumentException("Indefinite length encoding (0x80) is not supported");
}

if (length > 0x80)
{
    // existing long-form handling...
}
```

**Safety:** The `Encode` method encodes value 128 as `0x81 0x80` (long form), never bare `0x80`. No valid TLV data uses `0x80` as short-form. Other callers are unaffected.

### 1b. Unit tests

**File:** `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Utils/TlvTests.cs`

Add two tests:
- `ParseData_IndefiniteLength0x80_ThrowsArgumentException` — verify `0x80` length is rejected
- `ParseData_LongFormLength0x81_ParsesCorrectly` — regression: ensure `0x81 xx` still works

---

## Fix 2: OpenPGP Fingerprint Test

### Strategy: Fix Bug 1 first, then investigate

The test `GetFingerprints_DefaultState_AllZero` already uses `resetBeforeUse: true`. The reset sequence (block PINs → TERMINATE → ACTIVATE) should clear fingerprint DOs.

**Hypothesis:** Bug 1 may cause a cascading failure. If a prior test in the suite calls `GetAlgorithmInformationAsync` and crashes, it could leave the session or device in a bad state, causing subsequent tests (including the fingerprint test) to fail.

**Investigation steps after Fix 1:**
1. Run OpenPGP integration suite on 5.4.3: `dotnet toolchain.cs -- test --integration --project OpenPgp`
2. If fingerprint test passes → Bug 1 was the root cause (done)
3. If still fails → check:
   - Does `ResetAsync()` actually clear DO 0xC5 (fingerprint composite) on 5.4.3?
   - Is there a session cache issue after reset?
   - Is the test runner ordering causing state bleed?

**File (if needed):** `src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests/OpenPgpSessionTests.cs:410-424`

---

## Fix 3: HsmAuth Admin Password Change TLV Ordering

**File:** `src/YubiHsm/src/HsmAuthSession.cs:781-786`

**Current (wrong order):**
```csharp
var data = TlvHelper.EncodeList([
    new Tlv(TagManagementKey, managementKey.Span),  // 0x7B ← wrong position
    new Tlv(TagLabel, labelBytes),                   // 0x71
    new Tlv(TagCredentialPassword, newPwBytes)        // 0x73
]);
```

**Fix (match ykman `hsmauth.py:545-552`):**
```csharp
var data = TlvHelper.EncodeList([
    new Tlv(TagLabel, labelBytes),                   // 0x71 ← label first
    new Tlv(TagManagementKey, managementKey.Span),   // 0x7B
    new Tlv(TagCredentialPassword, newPwBytes)        // 0x73
]);
```

All other HsmAuth methods verified correct — only this one outlier.

---

## Verification

1. `dotnet build Yubico.YubiKit.sln` — 0 errors, 0 warnings
2. `dotnet toolchain.cs test` — 9/9 unit test projects passing
3. Integration (5.4.3): `dotnet toolchain.cs -- test --integration --project OpenPgp` — verify TLV parse fix + fingerprint
4. Integration (5.4.3): `dotnet toolchain.cs -- test --integration --project YubiHsm` — if available
5. HsmAuth TLV ordering can only be fully verified on production FW 5.8.0 (not currently available)

## Files Modified

| File | Change |
|------|--------|
| `src/Core/src/Utils/Tlv.cs` | Reject `0x80` indefinite length |
| `src/YubiHsm/src/HsmAuthSession.cs` | Swap TLV ordering in `ChangeCredentialPasswordAdminAsync` |
| `src/Core/tests/.../TlvTests.cs` | Two new unit tests for `0x80` handling |
