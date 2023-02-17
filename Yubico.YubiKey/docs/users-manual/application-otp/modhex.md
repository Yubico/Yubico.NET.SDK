---
uid: OtpModhex
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


# Modified hexadecimal encoding (ModHex)

As detailed in the section on [USB device communication](xref:OtpHID#usb-communication) via the [HID (Human Interface Device)](https://www.usb.org/hid) communication protocol, in order to submit a password ([Yubico OTP](xref:OtpYubicoOtp), [OATH-HOTP](xref:OtpHotp), or [static password](xref:OtpStaticPassword)) from the YubiKey to a host device over USB (or Lightning), the characters of the password must be sent as HID usage IDs so they can be handled as keyboard input by the host device. Unfortunately, these usage IDs represent physical locations on a keyboard, not the keys themselves. This can become a problem when sending HID usage IDs to host devices that are configured with different keyboard layouts.

For example, on an English language keyboard, the top row of keys spells QWERTY, and on a German language keyboard, those same keys spell QWERTZ. However, the "Y" key on the English keyboard and the "Z" key on the German keyboard are represented by the same HID usage ID of 28 (0x1c).

If the YubiKey sends the letter "Y" to a host device as part of a password, and it assumes the host device is configured with the English keyboard layout when it is actually configured with the German layout, the usage ID will be incorrectly interpreted as a "Z" by the host device. An incorrect password will then be sent for authentication, which will fail.

To address this and other discrepancies between HID usage IDs of characters across different keyboard layouts, Yubico invented the ModHex (modified hexadecimal) encoding scheme. ModHex only uses characters that are located in the same place on virtually all Latin alphabet keyboards: b, c, d, e, f, g, h, i, j, k, l, n, r, t, u, and v. Because there are exactly sixteen characters in this layout, it can be used to relay binary data as a modified hexadecimal code.

| ModHex Letter: | c | b | d | e | f | g | h | i | j | k | l | n | r | t | u | v |
|----------------|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Hexadecimal: | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | a | b | c | d | e | f |
| Decimal: | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13 | 14 | 15 |
| Binary: | 0000 | 0001 | 0010 | 0011 | 0100 | 0101 | 0110 | 0111 | 1000 | 1001 | 1010 | 1011 | 1100 | 1101 | 1110 | 1111 |

For Yubico OTPs and OATH-HOTPs, the ciphertext generated post-password encryption is binary. So, instead of encoding each 8-bit chunk of ciphertext (e.g. 0110 0011) as an [ASCII character](https://theasciicode.com.ar/) (ASCII includes all letters of the alphabet), each 4-bit chunk of ciphertext (e.g. 1011) can be encoded as one of the 16 ModHex characters.

To send the password to a host device over USB/Lightning, the ModHex characters are then translated into their corresponding HID usage IDs so they can be handled by the host device as keyboard input.

Yubico OTPs use ModHex encoding by default. OATH-HOTPs can be configured so that the [first byte](xref:Yubico.YubiKey.Otp.OtpSettings`1.OathFixedModhex1%28System.Boolean%29), [first two bytes](xref:Yubico.YubiKey.Otp.OtpSettings`1.UseOathFixedModhex2%28System.Boolean%29), or [all bytes](xref:Yubico.YubiKey.Otp.OtpSettings`1.UseOathFixedModhex%28System.Boolean%29) of the token identifier use ModHex encoding.

## ModHex as a static password keyboard layout

When [configuring an OTP application slot with a static password](xref:OtpProgramStaticPassword), you have two options:

1. [Generate](xref:Yubico.YubiKey.Otp.Operations.ConfigureStaticPassword.GeneratePassword%28System.Memory%7BSystem.Char%7D%29) a random password of a specified length to be used as the static password.

1. [Set](xref:Yubico.YubiKey.Otp.Operations.ConfigureStaticPassword.SetPassword%28System.ReadOnlyMemory%7BSystem.Char%7D%29) the static password to something of your choosing.

For static passwords, the [keyboard layout](xref:Yubico.Core.Devices.Hid.KeyboardLayout) determines which HID usage IDs are used to represent the password characters. For example, if the keyboard layout is set to English, the SDK will use the HID usage IDs corresponding to the location of the password characters on an English keyboard.

Additionally, for generated passwords, the keyboard layout determines which characters are used during generation (e.g. if the keyboard layout is set to English, the password will only contain characters that are included on the English keyboard). For set passwords, the keyboard layout acts as a filter; if the user-defined password contains characters that are not found in the keyboard layout, a `System.InvalidOperationException` will be thrown.

For both types of static passwords, the keyboard layout is set to ModHex by default. This means that generated passwords will only contain ModHex characters, and a `System.InvalidOperationException` will be thrown for set passwords that contain non-ModHex characters.

If you can’t be certain which keyboard layout will be configured on all devices that the YubiKey will be used with, you will want to create a password that only contains ModHex characters. As a best practice, we recommend explicitly setting the keyboard layout to ModHex even though it is the default layout.

## ModHex character casing

ModHex characters, like standard hexadecimal characters, are case-insensitive. This means that uppercase and lowercase versions of the ModHex characters have the same decimal and binary representations. For example, ModHex  "c" and "C" both represent decimal "0" and binary "0000". This is unlike ASCII characters, where uppercase and lowercase versions of a character have different decimal and binary representations.

For passwords that must be decrypted, like Yubico OTPs, the ModHex characters of the password are simply a representation of the binary ciphertext. By representing the ciphertext as characters, the YubiKey can easily communicate the ciphertext to a host device by pretending the characters are keyboard input, which a host device receives through HID usage reports. When a validation server receives the OTP as ModHex characters, it converts them back to their binary forms before decrypting. For example, the OTPs "cbd" and "CbD" will both be converted to "0000 0001 0010".

In practice, Yubico OTPs only contain lowercase ModHex characters. Although uppercase ModHex characters would still be interpreted correctly by a validation server, the YubiKey would have to do extra work to communicate uppercase characters to a host device. In order to send an uppercase character via a HID usage report, the modifier key flag has to be activated to show that a **Shift** key is being pressed with the character key. The YubiKey simply omits this unnecessary step.

When it comes to static passwords, however, ModHex characters behave more like ASCII characters. Static passwords do not represent binary ciphertext; they are meant to be interpreted as-is. If the system that is validating static passwords is case-sensitive, then it does matter whether the static password contains uppercase or lowercase ModHex characters. If a case-sensitive system expects a password to be "cbd," then sending "CbD" will not authenticate a user.

## ModHex encoding example

The Yubico OTP is 44 ModHex characters in length. The first 12 characters of a Yubico OTP string represent the public ID of the YubiKey that generated the OTP--this ID remains constant across all OTPs generated by that individual key. The last 32 characters of the string is the unique passcode, which is generated and encrypted by the YubiKey.

The unencrypted passcode consists of a 128-bit long string of fields unique to the key, including the key's private ID (48 bits), a usage counter (16 bits), the timestamp (24 bits), a session usage counter (8 bits), a random number (16 bits), and a checksum (16 bits). This 128-bit string is encrypted with a 128-bit AES key, resulting in a 128-bit encrypted binary string (the ciphertext).

For this example, let's say we touched our YubiKey, activating the short-press slot, which happens to be configured to generate a Yubico OTP. The YubiKey encrypts the 128-bit string of our key's unique fields with our secret AES key, resulting in the following binary ciphertext:

0000 0000 0000 0000 0101 1000 1100 0101 0111 0011 0000 0110 0010 1100 1011 0111 0011 1110 0110 1010 0100 0001 1101 0111 1111 0010 0011 0100 0111 1110 0010 1000 0010 0100 0000 1001 1000 0100 0010 0110 1110 0100 0011 0010

Because ModHex is base 16, each 4-bit chunk of the encrypted binary string can be represented by one of the 16 ModHex characters.

> [!NOTE]
> Remember, a bit can represent two unique states (0 or 1), so a string of four bits can represent sixteen unique states (2 x 2 x 2 x 2 = 2^4 = 16).

Thus, the 128-bit encrypted binary string can be represented by 32 ModHex characters (128 / 4 = 32).

Therefore, our binary ciphertext above will be represented by the following 44-character ModHex string:

ccccgjrgiechdrnieuhlfbtivdefiudjdfckjfdhufed

This 44-character string includes the 12-character public ID (ccccgjrgiech) and the 32-character encrypted passcode (drnieuhlfbtivdefiudjdfckjfdhufed).

To send this 44-character OTP to a host device over USB/lightning, the YubiKey will send the characters' HID usage IDs via a series of HID usage reports. The host device receives these usage reports and converts them into characters. If your cursor is inside an area that accepts text input, you will see the OTP (in ModHex characters) appear on your screen.

When the OTP (as a character string) is sent to a validation server through the host device, it must be converted from ModHex characters back to binary before it can be decrypted.

For our example, this means that our OTP will be converted from ccccgjrgiechdrnieuhlfbtivdefiudjdfckjfdhufed back to the binary string shown above.
