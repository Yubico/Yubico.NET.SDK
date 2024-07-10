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

## Authenticate: management key<br/>single authentication

### Command APDU Info (First Call)

| CLA | INS | P1 | P2 | Lc |    Data     |    Le    |
|:---:|:---:|:--:|:--:|:--:|:-----------:|:--------:|
| 00  | 87  | xx | 9B | 04 | 7C 02 81 00 | (absent) |

Note that the standard specifies that P1 holds the "Algorithm reference". That means the
value should be 03 for Triple-DES, 08 for AES-128, 0A for AES-192, or 0C
for AES-256.

The value in P2 is 9B, the slot for the management key.

### Response APDU Info (First Response)

Total Length: 14\
Data Length: 12

|                            Data                             | SW1 | SW2 |
|:-----------------------------------------------------------:|:---:|:---:|
| 7C 0A 81 08 \<*Client Authentication Challenge (8 bytes)*\> | 90  | 00  |

### Command APDU Info (Second Call)

| CLA | INS | P1 | P2 | Lc |                            Data                            |    Le    |
|:---:|:---:|:--:|:--:|:--:|:----------------------------------------------------------:|:--------:|
| 00  | 87  | xx | 9B | 0C | 7C 0A 82 08 \<*Client Authentication Response (8 bytes)*\> | (absent) |

### Response APDU Info (Second Response)

Total Length: 2\
Data Length: 0

|   Data   | SW1 | SW2 |
|:--------:|:---:|:---:|
| (absent) | 90  | 00  |

#### Second Response APDU: Key Not Authenticated

Total Length: 2\
Data Length: 0

|   Data   | SW1 | SW2 |
|:--------:|:---:|:---:|
| (absent) | 69  | 82  | 

The error code `69 82` means "Security status not satisfied". That is, the response to
the challenge was incorrect (probably used the wrong management key).

### Examples

```
$ opensc-tool -c default -s 00:a4:04:00:09:a0:00:00:03:08:00:00:10:00
   -s 00:87:00:9b:04:7c:02:81:00
   -s 00:87:00:9b:0C:7c:0A:82:08:83:50:d1:81:4c:72:ba:09
Using reader with a card: Yubico YubiKey FIDO+CCID 0
Sending: 00 A4 04 00 09 A0 00 00 03 08 00 00 10 00
Received (SW1=0x90, SW2=0x00):
61 11 4F 06 00 00 10 00 01 00 79 07 4F 05 A0 00
00 03 08
Sending: 00 87 00 9B 04 7C 02 81 00
Received (SW1=0x90, SW2=0x00):
7C 0A 81 08 D4 BD 9B 1D A5 D2 6C DA
Sending: 00 87 00 9B 0C 7C 0A 82 08 83 50 D1 81 4C 72 BA 09
Received (SW1=0x69, SW2=0x82)
```

## Authenticate: management key<br/>mutual authentication

### Command APDU Info (First Call)

| CLA | INS | P1 | P2 | Lc |    Data     |    Le    |
|:---:|:---:|:--:|:--:|:--:|:-----------:|:--------:|
| 00  | 87  | xx | 9B | 04 | 7C 02 80 00 | (absent) |

Note that the difference between this APDU and the single authentication APDU is the third
data byte (the byte at index 2). In single authentication it is 81, in mutual
authentication it is 80.

### Response APDU Info (First Response)

Total Length: 14\
Data Length: 12

|                            Data                             | SW1 | SW2 |
|:-----------------------------------------------------------:|:---:|:---:|
| 7C 0A 80 08 \<*Client Authentication Challenge (8 bytes)*\> | 90  | 00  |

### Command APDU Info (Second Call)

|             CLA              |   INS    | P1 | P2 | Lc |                                            Data                                            | Le |
|:----------------------------:|:--------:|:--:|:--:|:--:|:------------------------------------------------------------------------------------------:|:--:|
|              00              |    87    | xx | 9B | 18 | 7C 16 80 08 \<*Client Authentication Response (8 bytes)*\> 81 08 \<*YubiKey Authentication |    |
| Challenge (8 bytes)*\> 82 00 | (absent) |    |    |    |                                                                                            |    |

### Response APDU Info (Second Response)

Total Length: 14\
Data Length: 12

|                            Data                             | SW1 | SW2 |
|:-----------------------------------------------------------:|:---:|:---:|
| 7C 0A 82 08 \<*YubiKey Authentication Response (8 bytes)*\> | 90  | 00  |

#### Second Response APDU: Key Not Authenticated

Total Length: 2\
Data Length: 0

|   Data   | SW1 | SW2 |
|:--------:|:---:|:---:|
| (absent) | 69  | 82  |

The error code `69 82` means "Security status not satisfied". That is, the response to
the challenge was incorrect (probably used the wrong management key).

### Examples

```
$ opensc-tool -c default -s 00:a4:04:00:09:a0:00:00:03:08:00:00:10:00
   -s 00:87:00:9b:04:7c:02:80:00
   -s 00:87:00:9b:18:7c:16:80:08:83:50:d1:81:4c:72:ba:09:81:08:11:22:33:44:55:66:77:88:82:00
Using reader with a card: Yubico YubiKey FIDO+CCID 0
Sending: 00 A4 04 00 09 A0 00 00 03 08 00 00 10 00
Received (SW1=0x90, SW2=0x00):
61 11 4F 06 00 00 10 00 01 00 79 07 4F 05 A0 00
00 03 08
Sending: 00 87 00 9B 04 7C 02 80 00
Received (SW1=0x90, SW2=0x00):
7C 0A 80 08 61 E2 04 AE 33 5B 32 58
Sending: 00 87 00 9B 18 7C 16 80 08 83 50 D1 81 4C 72 BA 09 81 08 11 22 33 44 55 66 77 88 82 00
Received (SW1=0x69, SW2=0x82)
```
