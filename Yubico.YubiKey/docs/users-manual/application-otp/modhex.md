---
uid: OtpModhex
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

Yubico OTPs use ModHex encoding by default. OATH-HOTPs can be configured so that the [first byte](xref:Yubico.YubiKey.Otp.OtpSettings-1.OathFixedModhex1), [first two bytes](xref:Yubico.YubiKey.Otp.OtpSettings-1.UseOathFixedModhex2), or [all bytes](xref:Yubico.YubiKey.Otp.OtpSettings-1.UseOathFixedModhex.html#Yubico_YubiKey_Otp_OtpSettings_1_UseOathFixedModhex_System_Boolean_) of the token identifier use ModHex encoding.

## ModHex as a static password keyboard layout

When [configuring an OTP application slot with a static password](xref:OtpProgramStaticPassword), you have two options:

1. Generate a random password of a specified length to be used as the static password. Or,

1. Manually set the static password to something of your choosing.

In either situation, the [keyboard layout](xref:Yubico.Core.Devices.Hid.KeyboardLayout) must be specified. 


When [configuring an OTP application slot with a static password](xref:OtpProgramStaticPassword), the YubiKey must be told which [keyboard layout](xref:Yubico.Core.Devices.Hid.KeyboardLayout) the host device is configured with (e.g. English, German, etc) so that it translates the characters of the password into the correct HID usage IDs.

If you can’t be certain which keyboard layout will be configured on all devices that the YubiKey will be used with, ModHex is the safest layout to use. The downside of using the ModHex layout is that you will be limited to passwords containing upper and lower case versions of the ModHex characters.

## ModHex encoding example

The Yubico OTP is 44 ModHex characters in length. The first 12 characters of a Yubico OTP string represent the public ID of the YubiKey that generated the OTP--this ID remains constant across all OTPs generated by that individual key. The last 32 characters of the string is the unique passcode, which is generated and encrypted by the YubiKey.

The unencrypted passcode consists of a 128-bit long string of fields unique to the key, including the key's private ID (48 bits), a usage counter (16 bits), the timestamp (24 bits), a session usage counter (8 bits), a random number (16 bits), and a checksum (16 bits). This 128-bit string is encrypted with a 128-bit AES key, resulting in a 128-bit encrypted binary string (the ciphertext).

Because ModHex is base 16, each 4-bit chunk of the encrypted binary string can be represented by one of the 16 ModHex characters.

> [!NOTE]
> Remember, a bit can represent two unique states (0 or 1), so a string of four bits can represent sixteen unique states (2 x 2 x 2 x 2 = 2^4 = 16).

Thus, the 128-bit encrypted binary string can be represented by 32 ModHex characters (128 / 4 = 32).

When the OTP (as a character string) is sent to a validation server through the host device, it must be converted from ModHex characters back to binary before it can be decrypted.
