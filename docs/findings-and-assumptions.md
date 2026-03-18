# FIDO2 + SCP Support: Findings and Assumptions

**Branch:** `feature/fido2-scp-support`
**Date:** 2026-03-18 (updated)
**Status:** Resolved — FIDO2+SCP works over NFC (all firmware) and USB CCID (firmware 5.8+)

---

## Goal

Enable FIDO2 sessions over SCP03/SCP11 secure channels in the .NET SDK, with the end objective of supporting **Rust FFI interop** — allowing a Rust environment to call into a FIDO2 session and execute custom commands over an SCP-protected connection. The .NET SDK handles SCP channel establishment and encryption transparently, with Rust sending cleartext APDUs across the FFI boundary.

This requires two things:
1. **`Fido2Session` must accept `ScpKeyParameters`** — previously hard-coded to `null`, unlike `PivSession`, `OathSession`, `OtpSession`, and `YubiHsmAuthSession` which already support SCP
2. **The SCP connection pipeline must work with FIDO2** — the `ScpConnection` constructor flow must correctly establish a secure channel for the FIDO2 applet

This is a feature parity gap — the Android SDK (`yubikit-android`) already supports FIDO2 + SCP.

---

## Root Cause of Initial Failure

### The Error

```
Yubico.Core.Iso7816.ApduException : Failed to select the smart card application. 0x6A82
  at SmartCardConnection.SelectApplication()
  at SmartCardConnection.SetPipeline(IApduTransform)
  at ScpConnection..ctor(ISmartCardDevice, YubiKeyApplication, ScpKeyParameters)
```

`0x6A82` = ISO 7816 "file not found" — the FIDO2 AID could not be selected over USB CCID.

### Diagnosis

The initial tests used `Transport.UsbSmartCard` on **pre-5.8 firmware**. On firmware prior to 5.8, FIDO2 communicates via **HID** over USB, not CCID. The FIDO2 AID (`A0 00 00 06 47 2F 00 01`) is not selectable over the USB CCID (SmartCard) interface on those firmware versions.

**On firmware 5.8+**, Yubico added the FIDO2 applet to the USB CCID interface. The FIDO2 AID is selectable over both HID and CCID, making FIDO2+SCP possible over USB as well as NFC.

The equivalent PIV + SCP03 test passed on the same devices because PIV is always available over CCID.

### Key insight from `ConnectionFactory.cs`

`ConnectionFactory.CreateConnection()` for FIDO2 **prefers HID** (`ConnectionFactory.cs:123-130`) and only falls back to SmartCard if no HID device is available. Normal FIDO2 usage never goes through CCID. SCP connections always use SmartCard (`ConnectionFactory.cs:88-101`). On pre-5.8 firmware, this means FIDO2+SCP is inherently an **NFC use case**. On 5.8+, FIDO2+SCP works over both USB CCID and NFC.

---

## Assumptions — Verified

| # | Assumption | Result |
|---|-----------|--------|
| A1 | FIDO2 AID is selectable over USB CCID | **FALSE on pre-5.8** — `0x6A82`. **TRUE on 5.8+** — FIDO2 AID selectable over USB CCID. |
| A2 | `ResetSecurityDomainOnAllowedDevices()` interferes | **N/A** — irrelevant once correct transport/firmware used. |
| A3 | PIV+SCP works because PIV forwards GP commands to Security Domain | **TRUE** — confirmed by working PIV+SCP03 tests. |
| A4 | FIDO2 applet does NOT forward GP commands to Security Domain | **FALSE** — FIDO2+SCP03 and FIDO2+SCP11b both succeed over NFC (all firmware) and USB CCID (5.8+). The FIDO2 applet handles GP SCP commands correctly. |
| A5 | Android SDK's FIDO2+SCP relies on the applet supporting GP SCP | **TRUE** — Android tests run over NFC and succeed. Same flow works in .NET over NFC and over USB CCID on 5.8+. |
| A6 | `ScpConnection` needs restructuring for FIDO2 | **FALSE** — the existing `ScpConnection` flow is architecturally correct. Only the transport was wrong on pre-5.8 firmware. |

### Key conclusion

**Problem 2 from the initial analysis was wrong.** The FIDO2 applet *does* handle GlobalPlatform SCP commands (INITIALIZE UPDATE / EXTERNAL AUTHENTICATE). The `ScpConnection` constructor flow — SELECT app, then SCP handshake on that applet — works identically for FIDO2 as it does for PIV/OATH/OTP. No restructuring needed.

**Additionally**, on firmware 5.8+, the FIDO2 AID is registered on the USB CCID interface, so SCP works over USB — not just NFC. This was confirmed with MakeCredential (full PIN + touch + attestation) over SCP03 on a 5.8+ key connected via USB.

---

## Changes Made

### 1. `Fido2Session.cs` — Constructor updated

```csharp
// BEFORE
public Fido2Session(IYubiKeyDevice yubiKey, ReadOnlyMemory<byte>? persistentPinUvAuthToken = null)
    : base(Log.GetLogger<Fido2Session>(), yubiKey, YubiKeyApplication.Fido2, keyParameters: null)

// AFTER
public Fido2Session(
    IYubiKeyDevice yubiKey,
    ReadOnlyMemory<byte>? persistentPinUvAuthToken = null,
    ScpKeyParameters? keyParameters = null)
    : base(Log.GetLogger<Fido2Session>(), yubiKey, YubiKeyApplication.Fido2, keyParameters)
```

### 2. `YubiKeyFeatureExtensions.cs` — Feature gate

Added `YubiKeyCapabilities.Fido2` to the `YubiKeyFeature.Scp03` capability check so `ApplicationSession.GetConnection()` correctly validates FIDO2+SCP:

```csharp
YubiKeyFeature.Scp03 =>
    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V5_3_0
    && (HasApplication(yubiKeyDevice, YubiKeyCapabilities.Piv)
    || HasApplication(yubiKeyDevice, YubiKeyCapabilities.Oath)
    || HasApplication(yubiKeyDevice, YubiKeyCapabilities.OpenPgp)
    || HasApplication(yubiKeyDevice, YubiKeyCapabilities.Otp)
    || HasApplication(yubiKeyDevice, YubiKeyCapabilities.YubiHsmAuth)
    || HasApplication(yubiKeyDevice, YubiKeyCapabilities.Fido2)),  // NEW
```

### 3. `FidoIntegrationTestBase.cs` — Fixed caller

```csharp
// Named parameter to resolve ambiguity with new ScpKeyParameters parameter
new Fido2Session(testDevice, persistentPinUvAuthToken: persistentPinUvAuthToken)
```

### 4. `Scp03Tests.cs` — FIDO2 integration tests

- Added `[InlineData(StandardTestDevice.Fw5, Transport.UsbSmartCard)]` for 5.8+ USB CCID testing
- Added firmware 5.8+ skip condition for USB CCID tests
- Added `Scp03_Fido2Session_MakeCredential_Over_UsbCcid_Succeeds` — full MakeCredential with PIN + touch + attestation over SCP03
- Added `Scp03_Fido2Session_Pre58_UsbCcid_Skips_Gracefully` — validates correct behavior on both 5.8+ and pre-5.8 keys

### 5. `Scp11Tests.cs` — FIDO2 integration tests

- Changed from single NfcSmartCard parameter to theory with both NFC and USB transports
- Added `[InlineData(StandardTestDevice.Fw5, Transport.UsbSmartCard)]` with firmware 5.8+ skip condition

### Build & Test Status

- Solution builds with **0 errors, 0 warnings**
- All **3575 unit tests pass**
- Integration tests **pass over NFC** for both SCP03 and SCP11b (all firmware)
- Integration tests **pass over USB CCID** for SCP03 and SCP11b (firmware 5.8+)
- Full MakeCredential (PIN + touch + attestation) confirmed over SCP03 + USB CCID on 5.8+

---

## Connection Flows Explained

### How YubiKey exposes interfaces over USB vs NFC

Over **USB**, the YubiKey exposes multiple USB interfaces simultaneously:
- **HID FIDO** — for FIDO2/U2F (CTAP2 frames over HID reports)
- **HID Keyboard** — for OTP (keystroke injection)
- **CCID (SmartCard)** — for PIV, OATH, OTP, OpenPGP, YubiHSM Auth, Security Domain

On **pre-5.8 firmware**, the FIDO2 applet is **only registered on the HID FIDO interface** over USB. It is not registered on CCID. This is a firmware-level decision.

On **firmware 5.8+**, the FIDO2 applet is **registered on both HID FIDO and CCID** over USB. This means FIDO2 is selectable via SmartCard commands over USB, enabling SCP over USB.

Over **NFC**, there is only **one interface** — ISO 14443 (SmartCard). All applets are selectable through it, including FIDO2, on all firmware versions.

### The four FIDO2 connection flows

#### Flow 1: USB, no SCP — works (all firmware)

```
Fido2Session(yubiKey, keyParameters: null)
  → ConnectionFactory.CreateConnection(Fido2)
    → _hidFidoDevice != null → YES
    → return new FidoConnection(_hidFidoDevice)
```

Uses HID FIDO interface directly. No SELECT APDU. CTAP2 binary frames go over USB HID reports. This is the normal path — SmartCard/CCID is never involved.

#### Flow 2: NFC, no SCP — works (all firmware)

```
Fido2Session(yubiKey, keyParameters: null)
  → ConnectionFactory.CreateConnection(Fido2)
    → _hidFidoDevice is null (NFC has no HID)
    → _smartCardDevice != null → YES (NFC is always SmartCard)
    → return new SmartCardConnection(_smartCardDevice, Fido2)
      → SelectApplication() → SELECT FIDO2 AID → OK
```

Over NFC, there's no HID, so the SDK falls back to SmartCard. The FIDO2 AID is selectable because NFC exposes all applets. CTAP2 commands are wrapped in ISO 7816 APDUs.

#### Flow 3: USB, with SCP — firmware-dependent

**Firmware 5.8+: WORKS**

```
Fido2Session(yubiKey, keyParameters: scp03Key)
  → ConnectionFactory.CreateScpConnection(Fido2, scp03Key)
    → new ScpConnection(_smartCardDevice, Fido2, scp03Key)
      → SelectApplication() → SELECT FIDO2 AID over CCID → OK (5.8+ registers FIDO2 on CCID)
      → SCP handshake (INITIALIZE UPDATE / EXTERNAL AUTHENTICATE) → OK
      → All subsequent CTAP2 APDUs encrypted → OK
```

On 5.8+, the FIDO2 AID is registered on both HID and CCID. SCP is a SmartCard-layer protocol, so it routes through CCID. SELECT succeeds, SCP handshake succeeds, and all FIDO2 operations (including MakeCredential with PIN + touch) work through the encrypted channel.

**Pre-5.8 firmware: FAILS**

```
Fido2Session(yubiKey, keyParameters: scp03Key)
  → ConnectionFactory.CreateScpConnection(Fido2, scp03Key)
    → new ScpConnection(_smartCardDevice, Fido2, scp03Key)
      → SelectApplication() → SELECT FIDO2 AID over CCID → 0x6A82 FAIL
```

On pre-5.8, the firmware does not register the FIDO2 applet on CCID over USB. SELECT fails with "file not found." This is not an SDK issue — the card simply doesn't have the FIDO2 AID on CCID.

#### Flow 4: NFC, with SCP — works (all firmware)

```
Fido2Session(yubiKey, keyParameters: scp03Key)
  → ConnectionFactory.CreateScpConnection(Fido2, scp03Key)
    → new ScpConnection(_smartCardDevice, Fido2, scp03Key)
      → SelectApplication() → SELECT FIDO2 AID → OK (NFC exposes all applets)
      → SCP handshake (INITIALIZE UPDATE / EXTERNAL AUTHENTICATE) → OK
      → All subsequent CTAP2 APDUs encrypted → OK
```

Over NFC, SmartCard is the only interface, all applets are selectable, and the FIDO2 applet correctly handles GlobalPlatform SCP commands. This is the primary use case: mobile devices communicating with YubiKeys over NFC with SCP channel protection.

### Summary

| Flow | SDK transport | Firmware | FIDO2 available? | SCP possible? | Result |
|------|--------------|----------|------------------|---------------|--------|
| 1. USB, no SCP | HID FIDO | All | Yes (HID) | N/A | **Works** |
| 2. NFC, no SCP | SmartCard | All | Yes (NFC exposes all) | N/A | **Works** |
| 3. USB, with SCP | SmartCard (CCID) | Pre-5.8 | No (not on CCID) | Blocked | **Fails** |
| 3. USB, with SCP | SmartCard (CCID) | 5.8+ | Yes (on CCID) | Yes | **Works** |
| 4. NFC, with SCP | SmartCard (NFC) | All | Yes (NFC exposes all) | Yes | **Works** |

The constraint on pre-5.8: **SCP requires SmartCard. USB FIDO2 is HID-only. They are incompatible transports over USB.** Over NFC, both coexist on a single SmartCard interface.

On 5.8+: **FIDO2 is available on both HID and CCID over USB.** SCP routes through CCID and succeeds.

---

## Remaining Work

### Rust FFI path
With FIDO2+SCP validated over NFC (all firmware) and USB CCID (5.8+):
- Wrap `Fido2Session` with `ScpKeyParameters` in NativeAOT exports
- Expose `Connection.SendCommand()` for custom APDUs through the encrypted channel
- Create `[UnmanagedCallersOnly]` entry points for Rust consumption
- On 5.8+ firmware, USB connections are viable — no NFC reader required

---

## Reference Files

| File | Role |
|------|------|
| `Yubico.YubiKey/src/Yubico/YubiKey/Fido2/Fido2Session.cs` | Session constructor — accepts `ScpKeyParameters` |
| `Yubico.YubiKey/src/Yubico/YubiKey/YubiKeyFeatureExtensions.cs` | Feature gate — FIDO2 in SCP03 check |
| `Yubico.YubiKey/src/Yubico/YubiKey/Scp/ScpConnection.cs` | SCP connection — works as-is, no changes needed |
| `Yubico.YubiKey/src/Yubico/YubiKey/SmartCardConnection.cs` | Base class — `SelectApplication()` and `SetPipeline()` |
| `Yubico.YubiKey/src/Yubico/YubiKey/ConnectionFactory.cs` | Connection routing — FIDO2 prefers HID, SCP always uses SmartCard |
| `yubikit-android/.../fido/ctap/Ctap2Session.java:1650-1661` | Android reference — confirms FIDO2+SCP support |
