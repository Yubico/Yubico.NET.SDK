## Transports and interfaces

![h:520](assets/L4-connection.svg)

---

## Command invocation: smart card / APDU

![w:1120](assets/L3-apdu-sequence.svg)

PIV · OATH · OpenPGP · Management · SecurityDomain · YubiHSM Auth

---

## Command invocation: FIDO HID / CTAP

![w:1000](assets/L3b-fido2-ctap-sequence.svg)

FIDO2 · WebAuthn. YubiOTP rides OTP-HID, a third framing.

---

## Applets are not one-transport-each

`[Flags] ConnectionType`: `Unknown=0` · `Hid=1` · `HidFido=2` · `HidOtp=4` · `SmartCard=8` · `All`

| Applet | Default transport order |
|---|---|
| **Management** | `SmartCard` → `HidFido` → `HidOtp` — **three** |
| **FIDO2** | `HidFido` → `SmartCard` |
| **WebAuthn** | inherits FIDO2's order |
| **YubiOTP** | `SmartCard` → `HidOtp` |
| PIV · OATH · OpenPGP · SecurityDomain · YubiHSM | `SmartCard` only |

- FIDO2's SmartCard path is **NFC, or USB-CCID on firmware 5.8.0+**.
- **YubiOTP prefers SmartCard**, not HID — on a CCID-enabled key the YubiOTP snippet
  later in this deck runs over APDU.
- Supplying `ScpKeyParameters` **forces SmartCard** when no preference is given.

```csharp
await using var mgmt = await key.CreateManagementSessionAsync(
    new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidFido });
```

<!-- Anchors: Management src/Management/src/IYubiKeyExtensions.cs:143-144;
     Fido2 src/Fido2/src/IYubiKeyExtensions.cs:189-190, FW5.8 note :95;
     YubiOtp src/YubiOtp/src/IYubiKeyExtensions.cs:144-145;
     SCP-forces-SmartCard Management:109, Fido2:132, YubiOtp:110;
     single-transport Piv:46 Oath:47 OpenPgp:45 SecurityDomain:53 YubiHsm:49;
     ConnectionType src/Core/src/Devices/ConnectionType.cs:19-25 -->
