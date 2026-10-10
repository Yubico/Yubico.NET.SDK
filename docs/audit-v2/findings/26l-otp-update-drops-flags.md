# 26l OTP update configuration drops AllowUpdate and ProtectSlot2 (YESDK-1634)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed, with a correction. The silent drop is right for the update path. The SDK also assigns the wrong bit to `ProtectSlot2`, which neither the audit nor the verification report identified. |
| Our severity | MED for AllowUpdate on the update path. MED for the wrong `ProtectSlot2` bit on the standard path (new; the verification report rated the update path LOW). |
| Root cause | SDK only |
| Fix group | B (small API decision: reject `ProtectSlot2` on update configurations). The bit correction is a constant change, but it should be confirmed on a device first. |
| Evidence | unit test; comparison with the vendor header, yubikey-manager and the Android SDK. No device test. |
| Since the audit | `SlotConfiguration.cs`, `UpdateConfiguration.cs`, `ExtendedFlag.cs`, `TicketFlag.cs` and the tests are unchanged at current yubikit. |

## What the audit says

The update builder accepts AllowUpdate and ProtectSlot2, but the update mask removes both before serialization. The setters therefore change nothing on the device, and nothing reports it.

> "...silently accepting unsupported security settings makes it easy to believe protection was changed when it was not."

Abbreviations: OTP is one-time password. Here it means the YubiOTP application, which programs slots 1 and 2. AllowUpdate is the extended flag that permits later updates to a slot. ProtectSlot2 is the ticket flag that blocks changes to slot 2. EXTFLAG and TKTFLAG are the device's extended and ticket flag bytes. SDK is software development kit. API is application programming interface.

## What is right

- **The update path masks flags.** `UpdateConfiguration` overrides the three effective-flag methods to apply the update masks ([UpdateConfiguration.cs:133-140](../../../src/YubiOtp/src/UpdateConfiguration.cs#L133-L140)). The class documentation says so: "Only flags within the defined update masks are written to the device." ([UpdateConfiguration.cs:19-29](../../../src/YubiOtp/src/UpdateConfiguration.cs#L19-L29)).
- **AllowUpdate is outside the extended update mask.** `ExtendedFlagMasks.UpdateMask` lists seven flags and leaves out `AllowUpdate` (`0x20`) ([ExtendedFlag.cs:39-46](../../../src/YubiOtp/src/ExtendedFlag.cs#L39-L46)). `AllowUpdate(true)` and `AllowUpdate(false)` therefore both serialize byte 45 as `0x04`. The unit test fails today: "Values are equal, Expected: Not 4, Actual: 4".
- **ProtectSlot2 is outside the ticket update mask.** `TicketFlagMasks.UpdateMask` does not include `ProtectSlot2` ([TicketFlag.cs:38-46](../../../src/YubiOtp/src/TicketFlag.cs#L38-L46)). The unit test that expects a rejection fails today: no exception is thrown.
- **Python behaves correctly on the update path.** `ALLOW_UPDATE` is in the update mask. `protect_slot2` on an update configuration raises `ValueError`, and unsupported flags are rejected in `_build_update`. See the Python section.

## What is wrong or imprecise

- **The SDK assigns the wrong bit to ProtectSlot2 (new).** `TicketFlag.ProtectSlot2` is defined as `0x40` ([TicketFlag.cs:30](../../../src/YubiOtp/src/TicketFlag.cs#L30)). The protect bit is `0x80`:
  - yubikey-manager: `TKTFLAG.PROTECT_CFG2 = 0x80` ([yubiotp.py:110](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/yubiotp.py#L110)), used by `protect_slot2` ([yubiotp.py:365-367](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/yubiotp.py#L365-L367)).
  - Vendor header: `TKTFLAG_PROTECT_CFG2` is `0x80` (see Specification).
  - Android SDK: `TKTFLAG_PROTECT_CFG2 = 0x80` ([SlotConfiguration.java:41-46](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/yubiotp/src/main/java/com/yubico/yubikit/yubiotp/SlotConfiguration.java#L41-L46)).
  - The value `0x40` is the challenge-response bit (`CHAL_RESP` in yubikey-manager, [yubiotp.py:117](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/yubiotp.py#L117)) and the OATH HOTP bit (`OATH_HOTP`, [yubiotp.py:114](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/yubiotp.py#L114)). The SDK already uses `0x40` for `OathHotp` ([HotpSlotConfiguration.cs:60](../../../src/YubiOtp/src/HotpSlotConfiguration.cs#L60)). The SDK's own enum therefore has three names on `0x40`: `ProtectSlot2`, `OathHotp` and `ChalResp` ([TicketFlag.cs:30-32](../../../src/YubiOtp/src/TicketFlag.cs#L30-L32)).
- **On a standard configuration, ProtectSlot2 is not discarded; it sets the wrong bit.** `SlotConfiguration.ProtectSlot2()` sets `0x40` on any configuration ([SlotConfiguration.cs:122-126](../../../src/YubiOtp/src/SlotConfiguration.cs#L122-L126)). The protect bit is never set. The effect of a stray `0x40` on a Yubico OTP or static slot was not tested on a device.
- **`ProtectSlot2(false)` clears a mode bit on challenge-response and HOTP configurations.** `HmacSha1SlotConfiguration` sets `TicketFlag.ChalResp` (`0x40`) in its constructor ([HmacSha1SlotConfiguration.cs:46](../../../src/YubiOtp/src/HmacSha1SlotConfiguration.cs#L46)), and `HotpSlotConfiguration` sets `TicketFlag.OathHotp` (`0x40`) ([HotpSlotConfiguration.cs:60](../../../src/YubiOtp/src/HotpSlotConfiguration.cs#L60)). `SetTktFlag` clears the bit it is given ([SlotConfiguration.cs:262-270](../../../src/YubiOtp/src/SlotConfiguration.cs#L262-L270)). Because `ProtectSlot2` uses `0x40`, `ProtectSlot2(false)` on those configurations clears the mode bit. This follows from the code; it was not run on a device.
- **The `ProtectSlot2` documentation describes the wrong mechanism.** [SlotConfiguration.cs:120](../../../src/YubiOtp/src/SlotConfiguration.cs#L120) says the call "requires slot 1 touch before slot 2 activates". The vendor header defines the bit as blocking updates to config 2 unless config 2 is configured and has the bit set (see Specification). The documentation needs correcting in the same change as the bit.
- **A passing test encodes the bug.** `Base_ProtectSlot2_SetsTktFlag` ([SlotConfigurationTests.cs:918-929](../../../src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/SlotConfigurationTests.cs#L918-L929)) compares the configuration byte with `(byte)TicketFlag.ProtectSlot2`, which is the wrong value. It passes today (run for this document: 1 of 1 succeeded), so it cannot detect the error.
- **The verification report's mechanism needs correcting.** The report says ProtectSlot2 "silently clears bit 0x40". `0x40` is the bit the SDK wrongly assigns to ProtectSlot2. The update mask drops it, which hides the error on update configurations only.
- **The enum value is part of the unshipped public API.** `TicketFlag.ProtectSlot2 = 64` is listed in [PublicAPI.Unshipped.txt:145](../../../src/YubiOtp/src/PublicAPI.Unshipped.txt#L145). It can be corrected before release.

## Why it matters

- **Who triggers it.** Any application that calls `ProtectSlot2()`, on a standard or an update configuration. An application that uses it to stop accidental reconfiguration of slot 2 gets no protection.
- **Impact on the update path.** The call is accepted, nothing is written, and no error is raised. The caller believes slot 2 is protected or unprotected when it is not.
- **Impact on the standard path.** The slot is written with the challenge-response or OATH HOTP bit, and the protection is absent. On a challenge-response or HOTP slot, `ProtectSlot2(false)` also clears the mode bit. The device's response to the stray bit on a Yubico OTP or static slot is untested here.
- **Preconditions.** A call to the setter. No access code is needed beyond the normal write path.

## Specification

The configuration flags are not in the specification cache. The vendor's C header is the closest primary source. It is not pinned, so treat it as a reference for the bit values, not as a stable link.

> `#define EXTFLAG_ALLOW_UPDATE 0x20 /* Allow update of existing configuration (selected flags + access code) */`

> `#define TKTFLAG_PROTECT_CFG2 0x80 /* Block update of config 2 unless config 2 is configured and has this bit set */`

> `#define TKTFLAG_CHAL_RESP 0x40 /* Challenge-response enabled (both must be set) */`

Source: [yubikey-personalization, `ykcore/ykdef.h`](https://github.com/Yubico/yubikey-personalization/blob/master/ykcore/ykdef.h), master branch at the time of writing.

| Flag | Vendor header | yubikey-manager | Android SDK | SDK | Notes |
| --- | --- | --- | --- | --- | --- |
| `ALLOW_UPDATE` (EXT) | `0x20` | `0x20` ([yubiotp.py:159](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/yubiotp.py#L159)) | not checked | `0x20` ([ExtendedFlag.cs:29](../../../src/YubiOtp/src/ExtendedFlag.cs#L29)) | The update mask should include it |
| `PROTECT_CFG2` (TKT) | `0x80` | `0x80` | `0x80` | `0x40` ([TicketFlag.cs:30](../../../src/YubiOtp/src/TicketFlag.cs#L30)) | Wrong in the SDK |
| `CHAL_RESP` (TKT) | `0x40` | `0x40` | not checked | `0x40` (`ChalResp`, [TicketFlag.cs:32](../../../src/YubiOtp/src/TicketFlag.cs#L32)) | Value correct; collides with ProtectSlot2 |
| `OATH_HOTP` (TKT) | `0x40` | `0x40` | not checked | `0x40` (`OathHotp`, [TicketFlag.cs:31](../../../src/YubiOtp/src/TicketFlag.cs#L31)) | Value correct; collides with ProtectSlot2 |

## Canonical Python reference

- **Python is correct here.** `ALLOW_UPDATE` is in `EXTFLAG_UPDATE_MASK` ([yubiotp.py:177-184](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/yubiotp.py#L177-L184), line 183). `PROTECT_CFG2` is not in `TKTFLAG_UPDATE_MASK` ([yubiotp.py:168-175](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/yubiotp.py#L168-L175)). `UpdateConfiguration.protect_slot2` raises `ValueError` ([yubiotp.py:577-578](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/yubiotp.py#L577-L578)) instead of dropping the flag.
- Unsupported TKT and CFG flags are rejected in the update builder ([yubiotp.py:567-575](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/yubiotp.py#L567-L575)). Note the comment in that method, "NB: All EXT flags are allowed", which matches the masks.

## Sibling SDKs (context only)

- The Android SDK defines `PROTECT_CFG2` as `0x80` ([SlotConfiguration.java:41-46](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/yubiotp/src/main/java/com/yubico/yubikit/yubiotp/SlotConfiguration.java#L41-L46)). Its `UpdateConfiguration.protectSlot2` throws `IllegalArgumentException` ([UpdateConfiguration.java:78-79](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/yubiotp/src/main/java/com/yubico/yubikit/yubiotp/UpdateConfiguration.java#L78-L79)), the same behaviour as Python.

## Reproduction

- Unit: [YESDK1634_UpdateDoesNotSilentlyDiscardAllowUpdate](../../../src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/AuditV2/YubiOtpAuditReproTests.cs#L5-L17). Asserts that the AllowUpdate bit differs between enable and disable. **Result today (2026-10-10): fails, "Values are equal", both `4`.**
- Unit: [YESDK1634_UpdateRejectsUnsupportedProtectSlot2](../../../src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/AuditV2/YubiOtpAuditReproTests.cs#L19-L25). Asserts `ArgumentException` from `ProtectSlot2()` on an update configuration. **Result today: fails, "No exception was thrown".**
- Unit (existing, not an AuditV2 test): [Base_ProtectSlot2_SetsTktFlag](../../../src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/SlotConfigurationTests.cs#L918-L929). **Result today: passes.** It passes because it asserts the wrong constant.
- Hardware: none.

## Proposed fix

- **Recommendation.**
  1. Add `ExtendedFlag.AllowUpdate` to `ExtendedFlagMasks.UpdateMask` ([ExtendedFlag.cs:39-46](../../../src/YubiOtp/src/ExtendedFlag.cs#L39-L46)).
  2. Change `TicketFlag.ProtectSlot2` to `0x80` ([TicketFlag.cs:30](../../../src/YubiOtp/src/TicketFlag.cs#L30)). Keep `OathHotp` and `ChalResp` at `0x40`. Update the unshipped API entry ([PublicAPI.Unshipped.txt:145](../../../src/YubiOtp/src/PublicAPI.Unshipped.txt#L145)).
  3. Reject `ProtectSlot2` on update configurations. Make `SlotConfiguration.ProtectSlot2` virtual, and override it in `UpdateConfiguration` to throw `ArgumentException`. Hiding it with `new` (as `Dormant` is hidden at [UpdateConfiguration.cs:91-95](../../../src/YubiOtp/src/UpdateConfiguration.cs#L91-L95)) does not help a caller who holds the reference as `SlotConfiguration`.
  4. Change `Base_ProtectSlot2_SetsTktFlag` to assert the literal `0x80` and not the enum, and add a test that the `0x40` bit stays clear after `ProtectSlot2()`.
  5. Correct the `ProtectSlot2` documentation ([SlotConfiguration.cs:119-121](../../../src/YubiOtp/src/SlotConfiguration.cs#L119-L121)) to describe the vendor's configuration-update protection rule, not a touch prerequisite for activation.
  6. Add regression tests proving that toggling protection changes only `0x80` and preserves `0x40` where the configured mode requires it: challenge-response (`HmacSha1SlotConfiguration`) and OATH HOTP (`HotpSlotConfiguration`).
- **Options considered (for the update rejection).**
  - Option 1 (recommended): virtual override that throws `ArgumentException`. The error appears at the call, matching Python and Android.
  - Option 2: throw from `GetConfig` when the protect bit is set on an update configuration. Later, and harder to diagnose.
  - Option 3: keep the silent mask and document it. Not recommended, since it is the audit's concern.
- **API impact.** `TicketFlag.ProtectSlot2` changes value, but only in the unshipped API. `ProtectSlot2` on an update configuration now throws. `AllowUpdate` on an update configuration now changes the transmitted byte.
- **Proving tests.** The two AuditV2 tests. The corrected `Base_ProtectSlot2_SetsTktFlag`. A test that a `SlotConfiguration`-typed reference to an update configuration rejects `ProtectSlot2`. The regression tests in step 6.
- **Depends on / interacts with.** Independent of the other findings. The bit change should be confirmed on a device before it is merged. A test key with slot 2 unused is enough for that check.
- **Open questions for the maintainer.** Confirm the `0x80` bit on a device before the change. Decide whether the wrong bit is tracked as its own finding (the verification report does not list it).

## Check it yourself

From the worktree root:

```bash
dotnet toolchain.cs -- test --project YubiOtp --filter "FullyQualifiedName~YESDK1634_Update|FullyQualifiedName~Base_ProtectSlot2"
```

Read in this order:

- [TicketFlag.cs:21-46](../../../src/YubiOtp/src/TicketFlag.cs#L21-L46) (the bit values and the update mask) and [ExtendedFlag.cs:37-46](../../../src/YubiOtp/src/ExtendedFlag.cs#L37-L46).
- [SlotConfiguration.cs:122-126](../../../src/YubiOtp/src/SlotConfiguration.cs#L122-L126) (`ProtectSlot2`) and [UpdateConfiguration.cs:133-140](../../../src/YubiOtp/src/UpdateConfiguration.cs#L133-L140) (the masks).
- `yubikit/yubiotp.py` lines 110, 114, 117, 159, 168-184 and 577-578 in the Python reference, and `ykcore/ykdef.h` for the vendor values.
