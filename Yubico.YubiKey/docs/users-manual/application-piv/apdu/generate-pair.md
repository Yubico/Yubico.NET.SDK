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


## Generate asymmetric key pair

### Command APDU Info

CLA | INS | P1 | P2 | Lc | Data | Le
:---: | :---: | :---: | :---: | :---: | :---: | :---:
00 | 47 | 00 | *Slot number* | *data len* | AC *\<remaining bytes\>* 80 01 *\<alg\>* <br />\[AA 01 *\<pin policy\>*\] <br />\[AB 01 *\<touch policy\>*\] | (absent)

The slot number can be one of the following (hex values):

```C
9A, 9C, 9D, 9E,
82, 93, 84, 85, 86, 87, 88, 89, 8A, 8B, 8C, 8D, 8E, 8F,
90, 91, 92, 93, 94, 95
F9
```

Note that SP 800-73-4 declares that another possible value for the slot number is `04`.
However, the YubiKey does not support that slot.

The value for the "remaining bytes" field must be equal to the number of bytes that come after it. For example, if three bytes come after the "remaining bytes" field, the field's value must be 03.

There are only four choices for "alg" (algorithm and size): RSA-1024 (06),
RSA-2048 (07), ECC-P-256 (11), and ECC-P-384 (14).

Both the PIN policy and touch policy are optional. If either or both are not given, they
will be default. The default for PIN is "once" and touch is "never".

The value for the PIN policy in the APDU is either "never" (01), "once" (02), or
"always" (03). The value for the touch policy in the APDU is either "never" (01),
"always" (02) or "cached" (03).

An APDU to generate an ECC-P256 key pair with a PIN policy of "once" and a touch policy
of "always" would be the following:

```C
  00 47 00 9C 0B AC 09 80 01 11 AA 01 02 AB 01 02
```

An APDU to generate an RSA-2048 key pair with PIN and touch policies of "default" would be
the following:

```C
  00 47 00 9D 05 AC 03 80 01 07
```

### Response APDU Info: Management Key Authentication Missing

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 69 | 82

### Response APDU Info: Success

Total Length: *variable + 2*\
Data Length: *variable*

Data | SW1 | SW2
:---: | :---: | :---:
*public key* | 90 | 00

The public key is in the form of a set of TLVs. If the key is ECC, there is one TLV, where
the value (the V) is the public point. If the key is RSA, there are two TLVs, where the
first value is the modulus and the second is the public exponent.

```
ECC:

86 || length || 04 || public point

86 41
   04 C4 17 7F 2B 96 8F 9C 00 0C 4F 3D 2B 88 B0 AB
   5B 0C 3B 19 42 63 20 8C A1 2F EE 1C B4 D8 81 96
   9F D8 C8 D0 8D D1 BB 66 58 00 26 7D 05 34 A8 A3
   30 D1 59 DE 66 01 0E 3F 21 13 29 C5 98 56 07 B5
   26

Note that the 04 is the first byte in the standard way to represent an ECC point.
The 04 means that both the x- and y-coordinates follow. This is the only format the
YubiKey supports. The other two are 02 and 03, indicating a compressed point, only
the x-coordinate is given and the reader must compute the y-coordinate. There are
two possible y-coordinates for each x, and the 02 or 03 indicates which to use.

   04
x-coordinate:
   C4 17 7F 2B 96 8F 9C 00 0C 4F 3D 2B 88 B0 AB 5B
   0C 3B 19 42 63 20 8C A1 2F EE 1C B4 D8 81 96 9F
y-coordinate:
   D8 C8 D0 8D D1 BB 66 58 00 26 7D 05 34 A8 A3 30
   D1 59 DE 66 01 0E 3F 21 13 29 C5 98 56 07 B5 26
```

```
RSA:

81 || length || modulus || 82 || length || public exponent

81 82 01 00
   F1 50 BE FB B0 9C AD FE F8 0A 3D 10 8C 36 92 DC
   34 B7 09 86 42 C9 CD 00 55 D1 A4 A0 40 61 5A 2A
   8A B4 7D AC A1 34 A2 2F 0A 36 D2 34 B7 D8 72 58
   20 D6 04 66 80 7A 7A 0A D1 03 32 A2 D0 C9 92 7E
   59 B8 63 F8 FD A3 0F D0 F1 A1 48 50 DF 82 DC 4F
   9F 7C 18 02 29 35 72 DD 10 54 80 12 68 89 8F 05
   CA A0 EB D4 F0 82 85 B8 67 AD F3 F7 86 2E D3 6E
   C8 E0 46 C4 6C 67 57 53 47 C7 38 84 AC F4 F4 44
   81 AB DB 64 EE 53 B5 35 AE 92 FF 8E FE 00 A7 A8
   B2 86 3B 66 DB 8E A7 07 FF 13 28 49 E5 9B D1 C8
   D2 2C F9 84 D5 8A FF 00 3E 88 FB C1 E1 F8 37 8E
   9D DB 5D 45 61 1B 29 29 A5 B7 C3 E7 38 E9 1A 15
   F3 58 DD CA E2 E1 3D 86 BA BC 63 E2 CD A4 75 3A
   F9 9C D8 23 0F D8 18 59 F8 12 29 62 AB DC BE A5
   01 C5 28 C3 E8 A1 65 CF 39 30 66 18 6A E5 AD FA
   EC 48 CC E7 BA 8B F7 56 6B DD 7B 56 2A 3B E7 E9
82 03
   01 00 01
```

### Examples

```C
$ opensc-tool -c default -s 00:a4:04:00:09:a0:00:00:03:08:00:00:10:00
  -s 00:47:00:9c:0b:ac:09:80:01:06:aa:01:02:ab:01:02
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 09 A0 00 00 03 08 00 00 10 00
Received (SW1=0x90, SW2=0x00):
61 11 4F 06 00 00 10 00 01 00 79 07 4F 05 A0 00
00 03 08
Sending: 00 47 00 9C 0B AC 09 80 01 06 AA 01 02 AB 01 02
Received (SW1=0x69, SW2=0x82)
```
