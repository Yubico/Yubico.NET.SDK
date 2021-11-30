---
uid: OtpStaticPassword
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

# Static passwords

The OTP application [slots](xref:OtpSlots) on the YubiKey are capable of storing static passwords in place of other configurations. As the name implies, a static password is an unchanging string of characters, much like the passwords you create for various online accounts. When a slot containing a static password is touch-activated, the password characters are sent to the host device as keyboard input (more specifically, as [USB HID reports](xref:OtpHID)).

> [!NOTE]
> Because static password characters are stored on the YubiKey as their corresponding HID usage IDs, sometimes referred to as "scan codes," static passwords can only be communicated correctly when the YubiKey is connected to a host device over USB or Lightning. NFC-enabled YubiKeys use the [NDEF](xref:OtpNdef) communication protocol to submit passwords wirelessly to host devices, and HID usage IDs are not compatible with NDEF.

As you can imagine, static passwords are not as secure as other configurations, such as [Yuibco OTPs](xref:OtpYubicoOtp), but their length and complexity still make them resistant to guessing. For this reason, we do NOT recommend using static passwords unless they are required for use with legacy systems for which other configurations would not be compatible.

## Static password configuration

Static passwords can be either randomly generated or manually set by a user. Both options require configuration via the API's [ConfigureStaticPassword()](xref:Yubico.YubiKey.Otp.OtpSession.ConfigureStaticPassword(Yubico.YubiKey.Otp.Slot)) method. Please see [How to program a slot with a static password](xref:OtpProgramStaticPassword) for examples.

> [!NOTE]
> Each OTP application slot may store one generated or user-defined password. If you try to configure a slot with both, you will receive a `System.InvalidOperationException`.

### Generated passwords

The [GeneratePassword()](xref:Yubico.YubiKey.Otp.Operations.ConfigureStaticPassword.GeneratePassword(System.Memory<System.Char>)) method allows you to generate a random password of a specified length (up to 38 characters) when configuring a slot with `ConfigureStaticPassword()`. Generated passwords use the [ModHex](xref:OtpModhex) character set, meaning that each character of the static password will be one of the 16 ModHex characters. This ensures that the generated password will be interpreted correctly by host devices, regardless of which keyboard layout they are configured with (e.g. English, German, etc).

If a slot has already been configured with a generated static password, the password may be updated to a new randomly generated password without having to use client software (as long as the [AllowManualUpdate()](xref:Yubico.YubiKey.Otp.Operations.ConfigureStaticPassword.AllowManualUpdate(System.Boolean)) boolean is set to `True`). For more information, see [How to manually update a generated static password](xref:OtpUpdateStaticPassword).

### Set passwords

The [SetPassword()](xref:Yubico.YubiKey.Otp.Operations.ConfigureStaticPassword.SetPassword(System.ReadOnlyMemory<System.Char>)) method allows you to set the static password to anything of your choosing (up to 38 characters in length).

Unlike `GeneratePassword()`, any key may be used as part of the password (including uppercase letters or other modified characters). However, you must specify the host device's [keyboard layout](xref:Yubico.Core.Devices.Hid.KeyboardLayout), as that determines which HID usage IDs will be stored on the YubiKey (HID usage IDs for some characters can vary across different keyboard layouts). If your password contains characters that are not present in your chosen keyboard layout, a `System.InvalidOperationException` will be thrown.
