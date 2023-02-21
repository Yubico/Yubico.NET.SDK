---
uid: OtpAppConcepts
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

# OTP application concepts

The goal of this section is to cover the critical properties of the OTP application that apply to most or all of the [configurations](xref:OtpConfigConcepts). These properties include the following:

- [Slots](xref:OtpSlots)

   Slots are the foundation of the OTP application; each slot can be programmed with one configuration. This article covers slot properties and activation with each configuration type.

- [YubiKey-host communication](xref:OtpHID)

   Let's say you have programmed a slot with a Yubico OTP configuration. When you activate that slot, the key generates an OTP. But how is that OTP communicated to a host device during authentication? This article covers the HID protocol, which the YubiKey uses when connected to a host over USB or Lightning, as well as the NDEF protocol, which the YubiKey uses to communicate wirelessley over NFC.

- [ModHex (modified hexadecimal encoding)](xref:OtpModhex)

   When a Yubico OTP or OATH HOTP is generated, the encrypted passcode is a byte string, but when these passwords are sent to a host, they appear as a character string on screen. ModHex is an encoding scheme developed by Yubico to translate the raw bits of OTPs/HOTPs into ASCII/UTF characters in a manner that ensures correct communication and interpretation, regardless of the communication protocol used by the YubiKey or the host's keyboard language configuration. This article covers what's included in the ModHex character set, how it works, and why it's important, as well as how ModHex can be used when configuring static passwords.
