---
uid: OtpCommandReadNdef
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

# Read NDEF payload (NFC only)

Accessing NDEF requires selecting the NDEF application over NFC. Though conceptually still part of the OTP
application, NDEF has its own application ID and is processed separately.

NDEF AID: `0xD2, 0x76, 0x00, 0x00, 0x85, 0x01, 0x01`

## Available

YubiKey firmware 3.x and 5.x

## Command APDU info

| Command Sequence | CLA  | INS  |  P1  |  P2  |    Lc    |   Data    |    Le    |
|:-----------------|:----:|:----:|:----:|:----:|:--------:|:---------:|:--------:|
| 1 (Select File)  | 0x00 | 0xA4 | 0x00 | 0x0C |   0x02   | 0xE1 0x04 | (absent) |
| 2 (Read Data)    | 0x00 | 0xB0 | 0x00 | 0x00 | (absent) | (absent)  |   0x00   |

## Response APDU info

Only the "Read Data" APDU returns data:

|    Lr    |    Data     | SW1  | SW2  |
|:--------:|:-----------:|:----:|:----:|
| (varies) | (see below) | 0x90 | 0x00 |

The response data is an NFC Data Exchange Format (NDEF) record as defined by the NFC Forum's technical
specification of the same name. It is as follows:

| Field                 |   Size   | Description                                                                             |
|:----------------------|:--------:|:----------------------------------------------------------------------------------------|
| Tag                   |    1     | Always `0`                                                                              |
| Length                |    1     | Length of the NDEF record                                                               |
| NDEF Header           |    1     | Always `0xD1`: Message Begin+End, Short Record, Type Name Format = NFC Forum well known |
| Length of record type |    1     | Always `1`                                                                              |
| Payload Length        |    1     |                                                                                         |
| Type                  |    1     | NFC Forum global type "U"                                                               |
| Payload               | (varies) | The actual NDEF message. Of length "payload length" bytes.                              |

## Examples

```shell
$ opensc-tool.exe -c default -r 0 -s 00:A4:04:00:07:D2:76:00:00:85:01:01 -s 00:A4:00:0C:02:E1:04 -s 00:B0:00:00:00
Sending: 00 A4 04 00 07 D2 76 00 00 85 01 01
Received (SW1=0x90, SW2=0x00)
Sending: 00 A4 00 0C 02 E1 04
Received (SW1=0x90, SW2=0x00)
Sending: 00 B0 00 00 00
Received (SW1=0x90, SW2=0x00):
00 43 D1 01 3F 55 04 6D 79 2E 79 75 62 69 63 6F .C..?U.my.yubico
2E 63 6F 6D 2F 79 6B 2F 23 63 63 63 63 63 63 6E .com/yk/#ccccccn
65 75 68 74 66 64 6E 76 6C 75 6E 68 6C 67 66 72 euhtfdnvlunhlgfr
6A 62 6E 65 65 62 76 6E 67 64 67 6C 6C 67 76 64 jbneebvngdgllgvd
64 65 72 65 62                                  dereb
```
