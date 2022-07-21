---
uid: OtpCommandProgramNdef
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

# Program NDEF

Sets the static payload for the NFC data exchange (NDEF) used by NFC enabled keys.

## Available

YubiKey firmware 3.x, and 5.x and later

## Command APDU info

|  CLA  |  INS  |     P1      |  P2   |    Lc    |    Data     |
| :---: | :---: | :---------: | :---: | :------: | :---------: |
| 0x00  | 0x01  | (See below) | 0x00  | (Varies) | (See below) |

### P1: Slot

P1 determines which slot to program. It can have one of the following values:

| Option                | Value |
| :-------------------- | :---: |
| NDEF Slot 1 (Primary) | 0x08  |
| NDEF Slot 2           | 0x09  |

### Data: NDEF configuration structure

| Field       | Size  | Description                                                   |
| :---------- | :---: | :------------------------------------------------------------ |
| Length      |   1   | Number of valid bytes in the data field.                      |
| Type        |   1   | Leave this set to `55`                                        |
| Data        |  54   | The NDEF payload. It does not need to fill the entire buffer. |
| Access Code |   6   | The current access code for the slot, if any.                 |

## Response APDU info

|  Lr   |                 Data                  |  SW1  |  SW1  |
| :---: | :-----------------------------------: | :---: | :---: |
| 0x06  | [Status structure](#status-structure) | 0x90  | 0x00  |

## Examples

TODO
