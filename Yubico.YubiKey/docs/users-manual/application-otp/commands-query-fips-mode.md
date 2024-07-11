---
uid: OtpCommandQueryFipsMode
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

# Query FIPs mode

Determines whether or not the device is loaded with FIPS capable firmware, as well as if the key
is currently in a FIPS compliant state.

## Available

YubiKey firmware 4.4.x.

## Command APDU info

| CLA  | INS  |  P1  |  P2  |    Lc    |   Data   |
|:----:|:----:|:----:|:----:|:--------:|:--------:|
| 0x00 | 0x01 | 0x14 | 0x00 | (absent) | (absent) |

## Response APDU info

If a FIPS key:

|  Lr  |                    Data                    | SW1  | SW2  |
|:----:|:------------------------------------------:|:----:|:----:|
| 0x01 | 0 = not FIPS compliant, 1 = FIPS compliant | 0x90 | 0x00 |

Just because a key may be branded FIPS or have FIPS capable firmware loaded, does not mean that the
YubiKey is FIPS compliant. Configurations on the key need to be locked or otherwise protected in
order to claim compliant behavior.

If not a FIPS key:

|    Lr    |   Data   | SW1  | SW2  |
|:--------:|:--------:|:----:|:----:|
| (absent) | (absent) | 0x6B | 0x00 |

0x6B00 is "SW_WRONG_P1P2", which in this context simply means that the query command is not present.
This behavior can be assumed to mean that the key does not support FIPS mode, and that it does not
have FIPS capable firmware.

## Examples

YubiKey 5.2.4 (via NFC)

```shell
$ opensc-tool.exe -c default -r 0 -s 00:a4:04:00:08:a0:00:00:05:27:20:01:01 -s 00:01:14:00
Sending: 00 A4 04 00 08 A0 00 00 05 27 20 01 01
Received (SW1=0x90, SW2=0x00):
A0 A2 7B 13 F8 80 ..{...
Sending: 00 01 14 00
Received (SW1=0x6B, SW2=0x00)
```

YubiKey FIPS 4.4.5

```shell
$ opensc-tool.exe -c default -r 1 -s 00:a4:04:00:08:a0:00:00:05:27:20:01:01 -s 00:01:14:00
Sending: 00 A4 04 00 08 A0 00 00 05 27 20 01 01
Received (SW1=0x90, SW2=0x00):
04 04 05 01 05 00 06 0F 00 00 ..........
Sending: 00 01 14 00
Received (SW1=0x90, SW2=0x00):
00 .
```
