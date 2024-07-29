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

## Get data

### Command APDU Info

| CLA | INS | P1 | P2 |     Lc     |                    Data                     |    Le    |
|:---:|:---:|:--:|:--:|:----------:|:-------------------------------------------:|:--------:| 
| 00  | CB  | 3F | FF | 3, 4, or 5 | *TLV with T of 5C and V of data object tag* | (absent) |

Note that there are other standards and applications that use the GET DATA APDU, and they
sometimes use different values for INS, P1, and P2. They sometimes use the same values as
possible input, but describe options in different cases. However, the PIV standard
specifies only this combination of INS, P1, and P2.

### Response APDU info: success

Total Length: *variable + 2*\
Data Length: *variable*

|     Data      | SW1 | SW2 |
|:-------------:|:---:|:---:|
| *data object* | 90  | 00  |

The data object will be a TLV with a tag of `7E` or `53`. If the Get Data command
requested "Discovery" (data tag of `7E`), then the TLV will be `73 L V`. Otherwise it will
be `53 L V`.

### Response APDU info: data object not found

Total Length: *2*\
Data Length: *0*

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 6A  | 82  |

### Response APDU info: security status not satisfied

Total Length: *2*\
Data Length: *0*

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 69  | 82  |

### Examples

```C

This gets the CHUID

$ opensc-tool -c default -s 00:a4:04:00:09:a0:00:00:03:08:00:00:10:00
  -s 00:cb:3f:ff:05:5c:03:5f:c1:02
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 09 A0 00 00 03 08 00 00 10 00
Received (SW1=0x90, SW2=0x00):
61 11 4F 06 00 00 10 00 01 00 79 07 4F 05 A0 00
00 03 08
Sending: 00 CB 3F FF 05 5C 03 5F C1 02
Received (SW1=0x90, SW2=0x00):
53 3B 30 19 D4 E7 39 DA 73 9C ED 39 CE 73 9D 83
68 58 21 08 42 10 84 21 38 42 10 C3 F5 34 10 AD
64 BE AC 16 11 4A 56 93 A2 9D 58 3B 74 CB 44 35
08 32 30 33 30 30 31 30 31 3E 00 FE 00

53 3B
   30 19
      D4 E7 39 DA 73 9C ED 39 CE 73 9D 83 68 58 21 08
      42 10 84 21 38 42 10 C3 F5
   34 10
      AD 64 BE AC 16 11 4A 56 93 A2 9D 58 3B 74 CB 44
   35 08
      32 30 33 30 30 31 30 31
   3E 00
   FE 00
```
