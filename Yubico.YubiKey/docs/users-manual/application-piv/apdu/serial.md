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

## Get the serial number

### Command APDU Info

| CLA | INS | P1 | P2 |    Lc    |   Data   |    Le    |
|:---:|:---:|:--:|:--:|:--------:|:--------:|:--------:| 
| 00  | F8  | 00 | 00 | (absent) | (absent) | (absent) |

### Response APDU Info

Total Length: 6\
Data Length: 4

|                      Data                      | SW1 | SW2 |
|:----------------------------------------------:|:---:|:---:|
| *big endian bytes of the 32-bit serial number* | 90  | 00  |

### Examples

```C
$ opensc-tool -c default -s 00:a4:04:00:09:a0:00:00:03:08:00:00:10:00
  -s 00:f8:00:00
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 09 A0 00 00 03 08 00 00 10 00
Received (SW1=0x90, SW2=0x00):
61 11 4F 06 00 00 10 00 01 00 79 07 4F 05 A0 00
00 03 08
Sending: 00 F8 00 00
Received (SW1=0x90, SW2=0x00):
00 AE 17 CB

In this case, the serial number is 0x00AE17CB = decimal 11409355
```

