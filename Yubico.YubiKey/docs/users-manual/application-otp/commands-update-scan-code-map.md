---
uid: OtpCommandUpdateScanCodeMap
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

# Update scan-code map

Updates the scan-codes (or keyboard presses) that the YubiKey will use when typing out one-time passwords.

## Available

YubiKey firmware 3.0 and later.

## Command APDU info

|  CLA  |  INS  |  P1   |  P2   |  Lc   |    Data     |
| :---: | :---: | :---: | :---: | :---: | :---------: |
| 0x00  | 0x01  | 0x12  | 0x00  | 0x2D  | (see below) |

The data field is a simple 45-byte array that holds keyboard scan-codes for use during OTP keyboard
operations. The default set of characters is:
> "cbdefghijklnrtuvCBDEFGHIJKLNRTUV0123456789!\t\r"

This is represented by the following array of bytes:

```C
0x06, 0x05, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, // c-i
0x0d, 0x0e, 0x0f, 0x11, 0x15, 0x17, 0x18, 0x19, // j-v
0x86, 0x85, 0x87, 0x88, 0x89, 0x8a, 0x8b, 0x8c, // C-I
0x8d, 0x8e, 0x8f, 0x91, 0x95, 0x97, 0x98, 0x99, // J-V
0x27, 0x1e, 0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, // 0-7
0x25, 0x26,                                     // 8-9
0x9e, 0x2b, 0x28                                // !, \t, \r
```

## Response APDU info

|  Lr   |                 Data                  |  SW1  |  SW1  |
| :---: | :-----------------------------------: | :---: | :---: |
| 0x06  | [Status structure](xref:OtpCommands#status-structure) | 0x90  | 0x00  |

## Examples

Reprogram the YubiKey with the default scan-code map:

```shell
$ opensc-tool.exe -c default -r 1 -s 00:a4:04:00:08:a0:00:00:05:27:20:01:01 -s 00:01:12:00:2D:06:05:07:08:09:0A:0
B:0C:0D:0E:0F:11:15:17:18:19:86:85:87:88:89:8A:8B:8C:8D:8E:8F:91:95:97:98:99:27:1E:1F:20:21:22:23:24:25:26:9E:2B:28
Sending: 00 A4 04 00 08 A0 00 00 05 27 20 01 01
Received (SW1=0x90, SW2=0x00):
05 03 01 03 05 00 ......
Sending: 00 01 12 00 2D 06 05 07 08 09 0A 0B 0C 0D 0E 0F 11 15 17 18 19 86 85 87 88 89 8A 8B 8C 8D 8E 8F 91 95 97 98 99 27 1E 1F 20 21 22 23 24 25 26 9E 2B 28
Received (SW1=0x90, SW2=0x00):
05 03 01 04 05 00 ......
```
