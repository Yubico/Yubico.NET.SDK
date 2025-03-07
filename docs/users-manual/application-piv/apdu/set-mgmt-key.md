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

## Set management key

### Command APDU Info

| CLA | INS | P1 |       P2       | Lc |      Data      |    Le    |
|:---:|:---:|:--:|:--------------:|:--:|:--------------:|:--------:| 
| 00  | FF  | FF | *touch policy* | 1B | *new key data* | (absent) |

The touch policy is either `FF` (no touch required) or `FE` (touch
required) or `FD` for cached.

The new key data is formatted as follows

```C
  03 9B 18 <24 binary bytes>

  03 means Triple-DES
  9B indicates slot 9B (where the management key resides)
  18 is the length (0x18 = decimal 24)
```

### Response APDU Info

#### Response APDU for SET MANAGEMENT KEY (success)

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 90  | 00  |

#### Response APDU for SET MANAGEMENT KEY (authentication failed)

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 69  | 82  |

### Examples

```C
$ opensc-tool -c default -s 00:a4:04:00:09:a0:00:00:03:08:00:00:10:00
  -s 00:FF:FF:FF:1B:03:9B:18:01:02:03:04:05:06:07:08:
                             08:07:06:05:04:03:02:01:
                             08:07:06:05:04:03:02:01
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 09 A0 00 00 03 08 00 00 10 00
Received (SW1=0x90, SW2=0x00):
61 11 4F 06 00 00 10 00 01 00 79 07 4F 05 A0 00
00 03 08
Sending: 00 FF FF FF 1B 03 9B 18 01 02 03 04 05 06 07 08 08 07 06 05 04 03 02 01 08 07 06 05 04 03 02 01 
Received (SW1=0x69, SW2=0x82)
```
