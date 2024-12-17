---
uid: OtpHID
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

# YubiKey-host device communication

YubiKeys can, depending on their components, communicate with host devices over USB, NFC, and/or Lightning interfaces.

## USB communication

In order for the OTP application on the YubiKey to submit
passwords ([Yubico OTPs](xref:OtpYubicoOtp), [OATH HOTPs](xref:OtpHotp), and [static passwords](xref:OtpStaticPassword))
through a host device over USB, it must utilize the [USB HID (Human Interface Device)](https://www.usb.org/hid)
communication protocol. The HID standard allows compliant hosts and USB peripherals, like keyboards and mice, to
communicate without the need for specialized drivers.

The YubiKey essentially emulates a HID keyboard; each key on a keyboard is represented by a
HID [usage ID](https://www.usb.org/sites/default/files/documents/hut1_12v2.pdf#page=53) (in decimal and hexadecimal),
which is collected into a HID usage report (sometimes referred to as a message). The YubiKey generates these usage
reports to simulate keystrokes, and the usage reports are decoded by the host into the characters of a password.

### HID reports

A HID report consists of eight bytes: the first byte represents a set of modifier key flags, the second byte is unused,
and the final six bytes represent keys that are currently being pressed, sorted in the order they were pressed.

With HID, modifier key flags (e.g. the left-shift button) are used to, you guessed it, modify the keys included in the
final six bytes of the HID report. If modifier key flags are not included in the report, the keys will be sent in their
default format (letter keys are lowercase by default).

The following tables represent the bytes of a HID report and the bits in the modifier key flags byte:

<table>
<tr><th>HID Report Bytes</th><th></th><th>Modifier Key Flags</th></tr>
<tr><td>

| Byte | Description        |
|------|--------------------|
| 0    | Modifier Key Flags |
| 1    | Reserved Byte      |
| 2    | Keypress #1        |
| 3    | Keypress #2        |
| 4    | Keypress #3        |
| 5    | Keypress #4        |
| 6    | Keypress #5        |
| 7    | Keypress #6        |

</td><td>&nbsp;&nbsp;&nbsp;&nbsp;</td><td>

| Bit | Description           |
|-----|-----------------------|
| 0   | Left [Ctrl]           |
| 1   | Left [Shift]          |
| 2   | Left [Alt]            |
| 3   | Left GUI<sup>*</sup>  |
| 4   | Right [Ctrl]          |
| 5   | Right [Shift]         |
| 6   | Right [Alt]           |
| 7   | Right GUI<sup>*</sup> |

</table>

<sup>*</sup> On Windows systems, this is the Windows key.

To send an uppercase "A" to a host device, the YubiKey must send the following usage report:

| Byte  | Value | Description                                          |
|-------|-------|------------------------------------------------------|
| 0     | 0x02  | The left shift (modifier key flag bit 1) is pressed. |
| 1     | 0x00  | (reserved)                                           |
| 2     | 0xe1  | Usage ID for the left shift key.                     |
| 3     | 0x04  | Usage ID for the "A" key.                            |
| 4 - 7 | 0x00  | Unused bytes (no more keys pressed).                 |

### HID keyboard layout challenges

A major challenge is that HID usage IDs correspond to physical locations on a keyboard, not the characters themselves.
For example, on an English language keyboard, the top row of keys spells QWERTY, and on a German language keyboard,
those same keys spell QWERTZ. However, the "Y" key on the English keyboard and the "Z" key on the German keyboard are
represented by the same HID usage ID of 28 (0x1c).

Therefore, when [programming a YubiKey slot with a static password](xref:OtpProgramStaticPassword), the SDK must be told
which [keyboard layout](xref:Yubico.Core.Devices.Hid.KeyboardLayout) the host device is configured with in order to send
the correct HID usage IDs for your static password characters.

> [!NOTE]
> You can configure your keyboard layout in Windows regardless of the actual keyboard you have.

When specifying a keyboard layout, you must be absolutely sure that every host device your key is plugged into be set to
the same keyboard layout. Going back to our example, if you program your YubiKey using an English layout, and someone
plugs the key into a computer configured with a German layout, then all of the “Y” characters in your static password
will be interpreted as “Z”.

But what should you do if you can't guarantee that the host device's keyboard layout will always be the same? And where
does this leave Yubico OTPs and OATH HOTPs, which are generated by algorithms instead of manually configured by a user?
To address these challenges, Yubico invented ModHex (modified hexadecimal), which is both a keyboard layout
configuration and an encoding scheme.

For more details, see the article on [ModHex](xref:OtpModhex).

## Lightning communication

The Apple Lightning connector on the YubiKey 5Ci uses the iAP communication protocol (iPod Accessory Protocol). However,
iAP also has support for HID through a tunneling mechanism, which allows the OTP application on the YubiKey to send and
receive HID messages when connected over Lightning.

## NFC communication

NFC-enabled YubiKeys communicate with host devices through close (wireless) contact with a host's NFC-reader via
the [NDEF](xref:OtpNdef) (NFC Data Exchange Format) protocol.

Unlike HID communication, where passwords are sent as bytes that represent HID usage reports, NDEF sends text. When a
YubiKey is scanned by an NFC reader, the key emits a URL containing the web address of an OTP validation server followed
by the OTP.

> [!NOTE]
> NFC is only compatible with Yubico OTPs and OATH HOTPs--static passwords can only be communicated through HID usage
> reports.
