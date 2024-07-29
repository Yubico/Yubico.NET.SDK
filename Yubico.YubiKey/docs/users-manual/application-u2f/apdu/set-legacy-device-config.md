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

## Set legacy device config

### Full command APDU info

| CLA | INS | P1 | P2 |      Lc       |         Data          |    Le    |
|:---:|:---:|:--:|:--:|:-------------:|:---------------------:|:--------:|
| 00  | 40  | 00 | 00 | *data length* | *encoded device info* | (absent) |

The data is encoded as four bytes

```txt
byte[0] is the touch eject value along with the interfaces
byte[1] is the challenge-response timeout
byte[2] and byte[3] make up the auto eject timeout (little endian)

byte[0] = 0x80 | interfaces if touch eject is true
byte[0] = 0x00 | interfaces if touch eject is false
the interfaces are
  0x00   OTP
  0x01   CCID
  0x02   OTP | CCID
  0x03   U2F
  0x04   OTP | U2F
  0x05   CCID | U2F
  0x06   All (OTP | CCID | U2F)

byte[3] and byte[4] make up the auto eject timeout
it is little endian
  decimal 300 is 0x2C 01
  decimal 555 is 0xFF 00
```

### Response APDU info

#### Response APDU for successful setting the device info

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 90  | 00  |

#### Response APDU when sent to YubiKeys version 5 and later

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 69  | 00  |
