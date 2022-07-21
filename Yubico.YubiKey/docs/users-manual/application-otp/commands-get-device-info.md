---
uid: OtpCommandGetDeviceInfo
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

# Get device information

Reads configuration and metadata information about the YubiKey. Similar commands exist in other
applications. The Command APDU may be different, however the data in the Response APDU will be
of identical format.

## Available

YubiKey firmware 4.1 and later.

## Command APDU info

|  CLA  |  INS  |  P1   |  P2   |    Lc    |   Data   |
| :---: | :---: | :---: | :---: | :------: | :------: |
| 0x00  | 0x01  | 0x13  | 0x00  | (absent) | (absent) |

## Response APDU info

|    Lr    |    Data     |  SW1  |  SW2  |
| :------: | :---------: | :---: | :---: |
| (Varies) | (See Below) | 0x90  | 0x00  |

The device information is encoded in Tag-Length-Value (TLV) format. The following table describes the
possible entries (tags).

| Name                         | Value | Description                                                                                           |
| :--------------------------- | :---: | :---------------------------------------------------------------------------------------------------- |
| Available capabilities (USB) | 0x01  | USB Applications and capabilities that are available for use on this YubiKey.                         |
| Serial number                | 0x02  | Returns the serial number of the YubiKey (if present and visible).                                    |
| Enabled capabilities (USB)   | 0x03  | Applications that are currently enabled over USB on this YubiKey.                                     |
| Form factor                  | 0x04  | Specifies the form factor of the YubiKey (USB-A, USB-C, Nano, etc.)                                   |
| Firmware version             | 0x05  | The Major.Minor.Patch version number of the firmware running on the YubiKey.                          |
| Auto-eject timeout           | 0x06  | Timeout in (ms?) before the YubiKey automatically "ejects" itself.                                    |
| Challenge-response timeout   | 0x07  | The period of time (in seconds) after which the OTP challenge-response command should timeout.        |
| Device flags                 | 0x08  | Device flags that can control device-global behavior.                                                 |
| Configuration lock           | 0x0A  | Indicates whether or not the YubiKey's configuration has been locked by the user.                     |
| Available capabilities (NFC) | 0x0D  | NFC Applications and capabilities that are available for use on this YubiKey.                         |
| Enabled capabilities (NFC)   | 0x0E  | Applications that are currently enabled over USB on this YubiKey.                                     |

## Examples

YubiKey 4.3.7

```shell
$ opensc-tool.exe -c default -r 1 -s 00:a4:04:00:08:a0:00:00:05:27:20:01:01 -s 00:01:13:00
Sending: 00 A4 04 00 08 A0 00 00 05 27 20 01 01
Received (SW1=0x90, SW2=0x00):
04 03 07 03 07 00 06 0F 00 00 ..........
Sending: 00 01 13 00
Received (SW1=0x90, SW2=0x00):
0C 01 01 FF 02 04 00 6B 95 6A 03 01 3F .......k.j..?

0C                      // Overall bytes
    01 01 FF            // Available capabilities (USB), length = 1, All capabilities available
    02 04 00 6B 95 6A   // Serial number, length = 4, value = 7060602
    03 01 3F            // Enabled capabilities (USB), length = 1, All capabilities enabled
```

YubiKey 5.2.3

```shell
$ opensc-tool.exe -c default -r 0 -s 00:a4:04:00:08:a0:00:00:05:27:20:01:01 -s 00:01:13:00
Sending: 00 A4 04 00 08 A0 00 00 05 27 20 01 01
Received (SW1=0x90, SW2=0x00):
AF 5C 62 50 C3 A8 .\bP..
Sending: 00 01 13 00
Received (SW1=0x90, SW2=0x00):
2E 01 02 02 3F 03 02 00 3C 02 04 00 A8 57 9B 04 ....?...<....W..
01 01 05 03 05 02 03 06 02 00 0A 07 01 0F 08 01 ................
80 0D 02 02 3F 0E 02 02 3B 0A 01 00 0F 01 00    ....?...;......

2B                      // Overall bytes
    01 02 02 3F         // Available capabilities (USB), length = 2, All capabilities
    03 02 00 3C         // Enabled capabilities (USB), length = 2, U2F and CCID unused
    02 04 00 AB 57 9B   // Serial number, length = 4, value = 11229083
    04 01 01            // Form factor, length = 1, 5A Keychain
    05 03 05 02 03      // Firmware version, length = 3, value = 5.2.3
    06 02 00 0A         // Auto eject timeout, length = 2, value = 40,960
    07 01 0F            // Challenge response timeout, length = 1, value = 15
    08 01 80            // Device flags, length = 1, Touch eject enabled
    0D 02 02 3F         // Available capabilities (NFC), length = 2
    0E 02 02 3B         // Enabled capabilities (NFC), length = 2
    0A 01 00            // Configuration log, length = 1, Unlocked
```
