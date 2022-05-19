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


## Create attestation statement

### Command APDU Info

CLA | INS | P1 | P2 | Lc | Data | Le
:---: | :---: | :---: | :---: | :---: | :---:
00 | F9 | *slot number* | 00 | (absent) | (absent) | (absent)

The slot number can be one of the following (hex values): `9A, 9C, 9D, 9E, 82, 93, 84, 85, 86, 87, 88, 89, 8A, 8B, 8C, 8D, 8E, 8F,
90, 91, 92, 93, 94, 95`.

### Response APDU Info

Total Length: *variable + 2*\
Data Length: *variable*

Data | SW1 | SW2
:---: | :---: | :---:
*certificate* | 90 | 00

Note that the certificate will be returned over multiple commands. Each return command
will be able to return up to 256 bytes. To get more bytes of a return, call the GET
RESPONSE APDU.

### Examples

```C
$ opensc-tool -c default -s 00:a4:04:00:09:a0:00:00:03:08:00:00:10:00
  -s 00:f9:9c:00 -s 00:c0:00:00 -s 00:c0:00:00 -s 00:c0:00:00
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 09 A0 00 00 03 08 00 00 10 00
Received (SW1=0x90, SW2=0x00):
61 11 4F 06 00 00 10 00 01 00 79 07 4F 05 A0 00
00 03 08
Sending: 00 F9 9C 00
Received (SW1=0x90, SW2=0x00):
30 82 03 20 30 82 02 08 A0 03 02 01 02 02 10 01
48 79 0D CE 69 34 3C BD 08 C3 CB 14 EC B9 50 30
0D 06 09 2A 86 48 86 F7 0D 01 01 0B 05 00 30 21
31 1F 30 1D 06 03 55 04 03 0C 16 59 75 62 69 63
6F 20 50 49 56 20 41 74 74 65 73 74 61 74 69 6F
6E 30 20 17 0D 31 36 30 33 31 34 30 30 30 30 30
30 5A 18 0F 32 30 35 32 30 34 31 37 30 30 30 30
30 30 5A 30 25 31 23 30 21 06 03 55 04 03 0C 1A
59 75 62 69 4B 65 79 20 50 49 56 20 41 74 74 65
73 74 61 74 69 6F 6E 20 39 63 30 82 01 22 30 0D
06 09 2A 86 48 86 F7 0D 01 01 01 05 00 03 82 01
0F 00 30 82 01 0A 02 82 01 01 00 CE D2 15 EF 4E
B8 57 BE 7E 7A 33 5C 6E 3A 51 C8 51 52 82 4F CE
EA E1 DA B4 0D 7C 55 8D 4A 90 3A 5E 4B 88 2C 4D
EB 4C 48 5D 4D E7 18 F3 48 1B 22 4A 33 AF 93 08
5C 97 1C 01 1A 8F 76 5F 6E 96 E9 48 CA 8C 91 3C
Sending: 00 C0 00 00
Received (SW1=0x90, SW2=0x00):
3A 2B 84 C0 0B 64 8B 4B 74 48 BC 8E 8A 94 E7 92
DB 2D 6C FF 5D 75 BF 16 A3 13 F3 55 5C F2 B4 31
34 02 0A 32 DE 5F 37 C4 21 F7 71 0B 01 31 D8 8B
75 3D A2 44 FD C5 DE 26 6C C4 9E 58 36 60 20 C5
0B 57 F9 9C B3 9A 7E F8 D6 87 21 CE A7 17 69 46
40 96 4B F4 21 DE 3F FC 02 D8 49 04 07 94 64 A7
2A 92 57 52 C7 BC D8 F8 56 D9 30 7F 4E 5C 52 9D
FD 55 A7 51 BB 8F B0 D0 CD CE 99 8B AD EB 62 E3
40 79 1D 72 BA 58 8C F7 F9 DB CE 1C BD D9 13 30
0A 59 66 97 15 4B 7F 59 25 82 DC 7E 91 FD 22 23
43 F0 B7 73 0F C3 00 65 14 82 A0 B2 E5 56 CD 7A
F7 74 4B 41 70 D1 1C CD 0B AD 19 02 03 01 00 01
A3 4E 30 4C 30 11 06 0A 2B 06 01 04 01 82 C4 0A
03 03 04 03 05 02 04 30 14 06 0A 2B 06 01 04 01
82 C4 0A 03 07 04 06 02 04 00 AE 17 FB 30 10 06
0A 2B 06 01 04 01 82 C4 0A 03 08 04 02 03 01 30
Sending: 00 C0 00 00
Received (SW1=0x90, SW2=0x00):
0F 06 0A 2B 06 01 04 01 82 C4 0A 03 09 04 01 01
30 0D 06 09 2A 86 48 86 F7 0D 01 01 0B 05 00 03
82 01 01 00 3F F9 48 2D 26 DB 57 D6 86 76 00 81
0C CB 65 FF D0 5E 31 97 82 F0 97 9F EA 3D D4 0D
14 24 37 92 5D E6 B2 A7 5D 71 73 5B D4 87 40 8D
0A E4 A4 A5 F9 D4 56 D7 98 FE B8 47 9A 52 CD 21
BC A7 6D 09 97 71 DC CF D0 D0 0D 9A 2B 32 13 2A
50 49 92 EC 14 7C 68 C4 97 C4 E1 E5 41 C3 38 CC
6D 85 0C FD BD C2 28 4D 53 B0 E2 45 7A 14 B5 1D
62 80 F4 FE 0C 87 50 F0 47 38 33 1C 39 EA 40 DF
1B 94 E7 FC 87 06 CA A3 D7 35 A6 4A 14 FC B0 88
8E E1 13 EB 06 97 AE 1F 4F 8E B7 DE 93 30 55 02
02 CB 22 DE 5F DE 3B 12 83 03 93 FE 33 E4 11 C4
B5 EC AE D6 57 9A 13 5A 15 F8 4F 75 55 72 F6 E9
29 E1 CA 9B A6 22 FC 0E E0 54 A4 F2 DD 23 80 CC
04 F1 E0 DC 28 72 02 7F 6C 14 E1 E1 69 13 4C 3A
Sending: 00 C0 00 00
Received (SW1=0x90, SW2=0x00):
F5 F8 57 04 B9 4C 6B CD D8 9C D1 65 1C 20 E9 0C
B7 7B DA E4 0E 55 FE B5 5A 11 61 D5 A8 BF 72 36
ED 40 21 47
```

