---
uid: OtpConfigureNDEF
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

# How to configure NDEF to use a slot to generate an OTP

The [NDEF](xref:OtpNdef) (NFC Data Exchange Format) configuration for the OTP application is a special case. The NDEF configuration is always active. If you present the YubiKey to an NFC reader and issue an NDEF read command, the YubiKey will always emit something.

When you configure NDEF functionality, you are setting two things: some text and which OTP configuration [slot](xref:OtpSlots) to use to generate a challenge. The text can be either a URI or just static text.

Unlike other configuration operations that take a slot identifier, configuring NDEF does not alter the configuration of the OTP application slot. It only sets which slot to activate after sending the text.

In its default state, the YubiKey has NDEF configured to emit https://my.yubico.com/neo/? and then activate slot 1 (the [short press](xref:Yubico.YubiKey.Otp.Slot.ShortPress) slot), which is configured for Yubico OTP. The result looks something like this:  https://my.yubico.com/neo/?vvccccnnjfhbtdgbflcbfcegkkdvttldvlcvvfinvvdu.

The most likely use case for this is to configure the YubiKey with a specific Yubico OTP credential and a URL to a validation server.

NDEF should only be configured to work with a [Yubico OTP](xref:OtpYubicoOtp) or [HOTP](xref:OtpHotp) slot. Nothing will prevent you from configuring NDEF to use a slot with any other configuration, but it will not emit anything useful.

For example, if a slot is configured for [challenge-response](xref:OtpChallengeResponse), presenting the YubiKey to an NFC reader and issuing a NDEF read command will result in the static text or URI with nothing after. If a slot is configured with a [static password](xref:OtpStaticPassword), the password will come through NDEF as the raw [HID](xref:OtpHID) bytes, which are not recognizable as characters.
(Static passwords need to be communicated through a USB port using HID messages.)

## ConfigureNdef example

In this example, we will configure the [long-press](xref:Yubico.YubiKey.Otp.Slot.LongPress) slot to emit an HOTP token, and we will configure NDEF to emit an identifier for an example user.

To execute the code below, the YubiKey needs to either be inserted into a USB port or be on an NFC reader when the command is run.

```
using (OtpSession otp = new OtpSession(yKey))
{
  otp.ConfigureHotp(Slot.LongPress)
    .UseInitialMovingFactor(4096)
    .Use8Digits()
    .UseKey(_key)
    .Execute();
  otp.ConfigureNdef(Slot.LongPress)
    .AsText("AgentSmith:")
    .Execute();
}
```

## Next steps

After configuring a slot with NDEF, learn [how to read from the NDEF tag](xref:OtpReadNDEF).
