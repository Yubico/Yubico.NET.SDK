---
uid: OtpSlots
summary: *content
---

<!-- Copyright 2021 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# Slots

The OTP application on the YubiKey contains two configurable slots: the "long press" slot and the "short press" slot. Each slot may be programmed with one of the following configurations:

- [Yubico OTP](xref:OtpYubicoOtp)
- [Initiative for Open Authentication HMAC-based OTP (OATH HOTP)](xref:OtpHotp)
- [Static password](xref:OtpStaticPassword)
- [Challenge-response (using the HMAC-SHA1 or Yubico OTP algorithms)](xref:OtpChallengeResponse)

## Slot Activation

The slots are activated during authentication. Activation results in the generation and/or submission of a password or challenge-response code from the YubiKey to the authenticating party. Only one slot may be activated at a time.

Slots configured with a Yubico OTP, OATH HOTP, or static password are activated by touching the YubiKey during authentication. The duration of touch determines which slot is used. The first slot ([ShortPress](xref:Yubico.YubiKey.Otp.Slot.ShortPress) slot) is activated when the YubiKey is touched for 1 - 2.5 seconds. The second slot ([LongPress](xref:Yubico.YubiKey.Otp.Slot.LongPress) slot) is activated when the YubiKey is touched for 3 - 5 seconds.

If a challenge-response configuration is programmed to require a user to touch the YubiKey, the touch will activate the appropriate slot regardless of duration. If touch is not required, activation will occur automatically.

NFC-compatible YubiKeys also contain an [NDEF](xref:OtpNdef) tag that can be configured to point to one of the slots. When the YubiKey is scanned by an NFC reader, the slot that is pointed to by the NDEF tag will activate, causing the generation of an OTP.

> [!NOTE]
> NDEF tags only work with Yubico OTPs and OATH HOTPs.

## Slot Properties

The OTP application slots have the following properties:

- Each slot may only be programmed with one configuration.
- Only one slot may be activated at a time.
- Slots can be [pointed to by an NDEF tag](xref:OtpConfigureNDEF).
- No data is shared between slots.
- Slots can be protected with an access code.
- Slot configurations can be [deleted](xref:OtpDeleteSlotConfig).
- Slot states are [retrievable](xref:OtpRetrieveSlotStatus).
- Slot configurations can be [swapped](xref:OtpSwapSlot).
- Slot settings that aren't related to encryption can be [updated](xref:OtpUpdateSlot) without performing complete slot reconfiguration.
