---
uid: UsersManualMetadata
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


## Get metadata

### Command APDU Info

CLA | INS | P1 | P2 | Lc | Data | Le
:---: | :---: | :---: | :---: | :---: | :---:
00 | F7 | 00 | *slot number* | (absent) | (absent) | (absent)

### Response APDU Info

Total Length: *variable + 2*\
Data Length: *variable*

Data | SW1 | SW2
:---: | :---: | :---:
*metadata as set of TLV* | 90 | 00

The data consists of a set of TLVs. The possible valid tags (T of TLV) are listed in the
table below. The length (L of TLV) is one, two, or three bytes, using the DER encoding
rules. The values (V of TLV) are dependent on the tags, described in the table below.

#### Table 1: List of Metadata Elements
Tag | Name | Meaning | Data | Slots
:---: | :---: | :---: | :---:
01 | Algorithm| Algorithm/Type of the key | ff (PIN or PUK), 03 (Triple DES), 08 (AES-128),<br/>0A (AES-192), 0C (AES-256),<br/>06 (RSA-1024), 07 (RSA-2048),<br/>11 (ECC-P256), or 14 (ECC-P384) | all slots
02 | Policy| PIN and touch policy | PIN: 0 (Default), 1 (Never),<br/>2 (Once), 3 (Always)<br/>Touch: 0 (Default), 1 (Never),<br/>2 (Always), 3 (Cached) | 9a, 9b, 9c, 9d, 9e, f9, 82 - 95
03 | Origin| Imported or generated | 1 (generated), 2 (imported) | 9a, 9c, 9d, 9e, f9, 82 - 95
04 | Public| Pub key partner to the pri key | DER encoding of public key  | 9a, 9c, 9d, 9e, f9, 82 - 95
05 | Default| Whether PIN/PUK/Mgmt Key has default value | 01 (default) 00 (not default) | 80, 81, 9b
06 | Retries| Number of Retries left | Two bytes, the retry count and remaining count | 80, 81

Another way to look at what is returned is the following table that lists which data
elements are returned for each slot.

#### Table 2: List of PIV Slots and the Metadata Elements Returned
| Slot Number (hex) | Key | Data Returned (tags) |
| :---: | :---: | :---: |
| 80 | PIN | 01, 05, 06 |
| 81 | PUK | 01, 05, 06 |
| 9B | Management | 01, 02, 05 |
| 82, 83, ..., 95 (20 slots)  | Retired Keys | 01, 02, 03, 04 |
| 9A | Authentication | 01, 02, 03, 04 |
| 9C | Signing | 01, 02, 03, 04 |
| 9D | Key Management | 01, 02, 03, 04 |
| 9E | Card Authentication | 01, 02, 03, 04 |
| F9 | Attestation | 01, 02, 03, 04 |

The length of a TLV follows the DER encoding rules (values in hex).

```C
 00       to 7F        lengths of    0 to    127
 81 80    to 81 FF     lengths of  128 to    255
 82 01 00 to 82 ff ff  lengths of  256 to 65,535
```

Note that the DER encoding rules allow lengths of `83 xx xx xx` and more, but any info
returned by this command will not be longer than 65,535 bytes.

### Examples

```C
Get Metadata on a public/private key pair, in this case, the PIV Authentication key in
slot 9A. It's ECC.

$ opensc-tool -c default -s 00:a4:04:00:09:a0:00:00:03:08:00:00:10:00
  -s 00:f7:00:9a
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 09 A0 00 00 03 08 00 00 10 00
Received (SW1=0x90, SW2=0x00):
61 11 4F 06 00 00 10 00 01 00 79 07 4F 05 A0 00
00 03 08
Sending: 00 F7 00 9A
Received (SW1=0x90, SW2=0x00):
01 01 11 02 02 02 01 03 01 01 04 43 86 41 04 C4
17 7F 2B 96 8F 9C 00 0C 4F 3D 2B 88 B0 AB 5B 0C
3B 19 42 63 20 8C A1 2F EE 1C B4 D8 81 96 9F D8
C8 D0 8D D1 BB 66 58 00 26 7D 05 34 A8 A3 30 D1
59 DE 66 01 0E 3F 21 13 29 C5 98 56 07 B5 26

Look at the data as a sequence of
TL
 V

01 01
   11
02 02
   02 01
03 01
   01
04 43
   86 41 04 C4 17 7F 2B 96 8F 9C 00 0C 4F 3D 2B 88
   B0 AB 5B 0C 3B 19 42 63 20 8C A1 2F EE 1C B4 D8
   81 96 9F D8 C8 D0 8D D1 BB 66 58 00 26 7D 05 34
   A8 A3 30 D1 59 DE 66 01 0E 3F 21 13 29 C5 98 56
   07 B5 26
```
```C
Get Metadata on a public/private key pair, in this case, the Digital Signature key in
slot 9C. It's RSA.

$ opensc-tool -c default -s 00:a4:04:00:09:a0:00:00:03:08:00:00:10:00
  -s 00:f7:00:9c
  -s 00:c0:00:00
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 09 A0 00 00 03 08 00 00 10 00
Received (SW1=0x90, SW2=0x00):
61 11 4F 06 00 00 10 00 01 00 79 07 4F 05 A0 00
00 03 08
Sending: 00 F7 00 9C
Received (SW1=0x90, SW2=0x00):
01 01 07 02 02 03 01 03 01 01 04 82 01 09 81 82
01 00 F1 50 BE FB B0 9C AD FE F8 0A 3D 10 8C 36
92 DC 34 B7 09 86 42 C9 CD 00 55 D1 A4 A0 40 61
5A 2A 8A B4 7D AC A1 34 A2 2F 0A 36 D2 34 B7 D8
72 58 20 D6 04 66 80 7A 7A 0A D1 03 32 A2 D0 C9
92 7E 59 B8 63 F8 FD A3 0F D0 F1 A1 48 50 DF 82
DC 4F 9F 7C 18 02 29 35 72 DD 10 54 80 12 68 89
8F 05 CA A0 EB D4 F0 82 85 B8 67 AD F3 F7 86 2E
D3 6E C8 E0 46 C4 6C 67 57 53 47 C7 38 84 AC F4
F4 44 81 AB DB 64 EE 53 B5 35 AE 92 FF 8E FE 00
A7 A8 B2 86 3B 66 DB 8E A7 07 FF 13 28 49 E5 9B
D1 C8 D2 2C F9 84 D5 8A FF 00 3E 88 FB C1 E1 F8
37 8E 9D DB 5D 45 61 1B 29 29 A5 B7 C3 E7 38 E9
1A 15 F3 58 DD CA E2 E1 3D 86 BA BC 63 E2 CD A4
75 3A F9 9C D8 23 0F D8 18 59 F8 12 29 62 AB DC
BE A5 01 C5 28 C3 E8 A1 65 CF 39 30 66 18 6A E5
Sending: 00 C0 00 00
Received (SW1=0x90, SW2=0x00):
AD FA EC 48 CC E7 BA 8B F7 56 6B DD 7B 56 2A 3B
E7 E9 82 03 01 00 01

01 01
   07
02 02
   03 01
03 01
   01
04 82 01 09
   81 82 01 00 F1 50 BE FB B0 9C AD FE F8 0A 3D 10
   8C 36 92 DC 34 B7 09 86 42 C9 CD 00 55 D1 A4 A0
   40 61 5A 2A 8A B4 7D AC A1 34 A2 2F 0A 36 D2 34
   B7 D8 72 58 20 D6 04 66 80 7A 7A 0A D1 03 32 A2
   D0 C9 92 7E 59 B8 63 F8 FD A3 0F D0 F1 A1 48 50
   DF 82 DC 4F 9F 7C 18 02 29 35 72 DD 10 54 80 12
   68 89 8F 05 CA A0 EB D4 F0 82 85 B8 67 AD F3 F7
   86 2E D3 6E C8 E0 46 C4 6C 67 57 53 47 C7 38 84
   AC F4 F4 44 81 AB DB 64 EE 53 B5 35 AE 92 FF 8E
   FE 00 A7 A8 B2 86 3B 66 DB 8E A7 07 FF 13 28 49
   E5 9B D1 C8 D2 2C F9 84 D5 8A FF 00 3E 88 FB C1
   E1 F8 37 8E 9D DB 5D 45 61 1B 29 29 A5 B7 C3 E7
   38 E9 1A 15 F3 58 DD CA E2 E1 3D 86 BA BC 63 E2
   CD A4 75 3A F9 9C D8 23 0F D8 18 59 F8 12 29 62
   AB DC BE A5 01 C5 28 C3 E8 A1 65 CF 39 30 66 18
   6A E5 AD FA EC 48 CC E7 BA 8B F7 56 6B DD 7B 56
   2A 3B E7 E9 82 03 01 00 01
```
```C
Get Metadata on the Management Key.

$ opensc-tool -c default -s 00:a4:04:00:09:a0:00:00:03:08:00:00:10:00
  -s 00:f7:00:9b
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 09 A0 00 00 03 08 00 00 10 00
Received (SW1=0x90, SW2=0x00):
61 11 4F 06 00 00 10 00 01 00 79 07 4F 05 A0 00
00 03 08
Sending: 00 F7 00 9B
Received (SW1=0x90, SW2=0x00):
01 01 03 02 02 00 01 05 01 00

01 01
   03
02 02
   00 01
05 01
   00
```
```C
Get Metadata on the PIN.

$ opensc-tool -c default -s 00:a4:04:00:09:a0:00:00:03:08:00:00:10:00
  -s 00:f7:00:80
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 09 A0 00 00 03 08 00 00 10 00
Received (SW1=0x90, SW2=0x00):
61 11 4F 06 00 00 10 00 01 00 79 07 4F 05 A0 00
00 03 08
Sending: 00 F7 00 80
Received (SW1=0x90, SW2=0x00):
01 01 FF 05 01 01 06 02 05 05

01 01
   FF
05 01
   01
06 02
   05 05
```
```C
Get Metadata on the PUK.

$ opensc-tool -c default -s 00:a4:04:00:09:a0:00:00:03:08:00:00:10:00
  -s 00:f7:00:81
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 09 A0 00 00 03 08 00 00 10 00
Received (SW1=0x90, SW2=0x00):
61 11 4F 06 00 00 10 00 01 00 79 07 4F 05 A0 00
00 03 08
Sending: 00 F7 00 81
Received (SW1=0x90, SW2=0x00):
01 01 FF 05 01 01 06 02 05 05

01 01
   FF
05 01
   01
06 02
   05 05
```
```C
Get Metadata on the Attestation Key.

$ opensc-tool -c default -s 00:a4:04:00:09:a0:00:00:03:08:00:00:10:00
  -s 00:f7:00:f9
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 09 A0 00 00 03 08 00 00 10 00
Received (SW1=0x90, SW2=0x00):
61 11 4F 06 00 00 10 00 01 00 79 07 4F 05 A0 00
00 03 08
Sending: 00 F7 00 F9
Received (SW1=0x90, SW2=0x00):
01 01 07 02 02 02 01 03 01 02 04 82 01 09 81 82
01 00 AC 45 96 66 97 9F 96 FF D9 04 4F 24 5D 5D
4F 99 3C D3 21 A5 FD 2A 63 FC 2B ED 8F 58 EF A7
C2 E3 79 68 94 0D 53 30 23 EC 6C A6 65 B7 E3 CB
C6 27 BE 72 92 B0 38 D4 7D E1 54 86 BF 75 07 16
F8 07 E4 7E A3 6B AB DF D6 9D E1 C7 7B A9 E9 D1
3E 97 8C 3E A0 57 C9 07 30 58 E4 9B AC 78 69 A1
6B 71 7C F0 78 9B C3 7F 4A C1 CB 40 BD 94 7F F4
19 36 BB 41 CC 35 CD 2E DB B1 97 A8 B0 05 9C E7
00 2D BF 35 C2 2A E4 92 91 F3 FC 86 C4 D1 B4 58
3C 46 51 5F BF 94 D4 F0 7E E7 4A 1B 85 F1 A3 3A
EC B5 1C 0E 86 90 5F 22 09 F1 A5 C8 6B BB 36 5A
63 80 F5 DE 46 E7 51 D8 F0 21 85 73 80 08 01 14
A7 3B B9 5F 80 15 15 A1 E7 7E 53 4D F3 9E 5B BA
7B B6 3C 1F B6 85 18 A8 99 0A 29 47 06 95 C8 94
78 04 06 B8 D0 65 76 15 5D 5E 8D 03 10 98 CE 54
Sending: 00 C0 00 00
Received (SW1=0x90, SW2=0x00):
D8 2F E6 EE DA 47 8E BB E1 59 2E D3 B8 DD 16 1B
9A 71 82 03 01 00 01

01 01
   07
02 02
   02 01
03 01
   02
04 82 01 09
   81 82 01 00 AC 45 96 66 97 9F 96 FF D9 04 4F 24
   5D 5D 4F 99 3C D3 21 A5 FD 2A 63 FC 2B ED 8F 58
   EF A7 C2 E3 79 68 94 0D 53 30 23 EC 6C A6 65 B7
   E3 CB C6 27 BE 72 92 B0 38 D4 7D E1 54 86 BF 75
   07 16 F8 07 E4 7E A3 6B AB DF D6 9D E1 C7 7B A9
   E9 D1 3E 97 8C 3E A0 57 C9 07 30 58 E4 9B AC 78
   69 A1 6B 71 7C F0 78 9B C3 7F 4A C1 CB 40 BD 94
   7F F4 19 36 BB 41 CC 35 CD 2E DB B1 97 A8 B0 05
   9C E7 00 2D BF 35 C2 2A E4 92 91 F3 FC 86 C4 D1
   B4 58 3C 46 51 5F BF 94 D4 F0 7E E7 4A 1B 85 F1
   A3 3A EC B5 1C 0E 86 90 5F 22 09 F1 A5 C8 6B BB
   36 5A 63 80 F5 DE 46 E7 51 D8 F0 21 85 73 80 08
   01 14 A7 3B B9 5F 80 15 15 A1 E7 7E 53 4D F3 9E
   5B BA 7B B6 3C 1F B6 85 18 A8 99 0A 29 47 06 95
   C8 94 78 04 06 B8 D0 65 76 15 5D 5E 8D 03 10 98
   CE 54 D8 2F E6 EE DA 47 8E BB E1 59 2E D3 B8 DD
   16 1B 9A 71 82 03 01 00 01
```
