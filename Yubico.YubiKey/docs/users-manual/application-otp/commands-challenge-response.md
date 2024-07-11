---
uid: OtpCommandChallengeResponse
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

# Challenge-response

Perform a challenge-response style operation using either YubicoOTP or HMAC-SHA1 against a configured
YubiKey slot.

## Available

YubiKey firmware 2.2 and later.

## Command APDU info

| CLA  | INS  |     P1      |  P2  |    Lc    |      Data      |
|:----:|:----:|:-----------:|:----:|:--------:|:--------------:|
| 0x00 | 0x01 | (See below) | 0x00 | (varies) | Challenge data |

### P1: Slot

P1 indicates both the type of challenge-response algorithm and the slot in which to use.

| Option            | Value |
|:------------------|:-----:|
| YubicoOTP (Short) | 0x20  |
| YubicoOTP (Long)  | 0x28  |
| HMAC-SHA1 (Short) | 0x30  |
| HMAC-SHA1 (Long)  | 0x38  |

### Data: Challenge

A string of bytes no greater than 64-bytes in length.

## Response APDU info

|      Lr      |     Data      | SW1  | SW1  |
|:------------:|:-------------:|:----:|:----:|
| 0x10 or 0x14 | Response data | 0x90 | 0x00 |

If the YubicoOTP algorithm was used, a 16-byte response will be given. HMAC-SHA1 will return a 20-byte
response.

## Examples

TODO
