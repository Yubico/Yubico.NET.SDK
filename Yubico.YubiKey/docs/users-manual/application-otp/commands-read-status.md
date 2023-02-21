---
uid: OtpCommandReadStatus
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

# Read status

Read the YubiKey's OTP status structure.

## Available

YubiKey firmware 3.x and later

## Command APDU info

|  CLA  |  INS  |  P1   |  P2   |    Lc    |   Data   |
| :---: | :---: | :---: | :---: | :------: | :------: |
| 0x00  | 0x03  | 0x00  | 0x00  | (absent) | (absent) |

## Response APDU info

|  Lr   |                 Data                  |  SW1  |  SW1  |
| :---: | :-----------------------------------: | :---: | :---: |
| 0x06  | [Status structure](xref:OtpCommands#status-structure) | 0x90  | 0x00  |

## Examples

```shell
$ opensc-tool.exe -c default -r 0 -s 00:a4:04:00:08:a0:00:00:05:27:20:01:01 -s 00:03:00:00
Sending: 00 A4 04 00 08 A0 00 00 05 27 20 01 01
Received (SW1=0x90, SW2=0x00):
04 03 07 03 07 00 06 0F 00 00 ..........
Sending: 00 03 00 00
Received (SW1=0x90, SW2=0x00):
04 03 07 03 07 00 ......
```
