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

## Set device info

### Full command APDU info

| CLA | INS | P1 | P2 |      Lc       |         Data          |    Le    |
|:---:|:---:|:--:|:--:|:-------------:|:---------------------:|:--------:|
| 00  | C3  | 00 | 00 | *data length* | *encoded device info* | (absent) |

The data is encoded as

```txt
length
  01 02            --optional
     value
  03 02            --optional
     value
  04 01            --optional
     value
  05 03            --optional
     value
  06 02            --optional
     value
  07 01            --optional
     value
  08 01            --optional
     value
  0d 02            --optional
     value
  0e 02            --optional
     value
  0a 01            --optional
     value
  0b 01            --optional
     value
```

See this [table](../u2f-commands.md#deviceinfoelements) for information
on each of the tags. There is a difference between that table and the
data provided for Set Device Info, namely the lock code. The lock code
is 16 binary bytes.

If there is no lock code, and the caller wants to set one, provide it in
the data under the tag `0A`. For example,

From no lock code to lock code

```txt
    0A 10
       9A 30 B5 86 27 F1 1D 99
       40 7C 4E 14 03 BD 82 17
```

If there is a lock code already set, provide it under the tag `0B`.

```txt
    0B 10
       9A 30 B5 86 27 F1 1D 99
       40 7C 4E 14 03 BD 82 17
```

To change the lock code, provide the current code under the `0B` tag and
the new lock code under the `0A` tag.

```txt
    0B 10
       9A 30 B5 86 27 F1 1D 99
       40 7C 4E 14 03 BD 82 17
    0A 10
       71 08 B4 96 AC 38 64 F2
       81 D0 48 55 B2 EA 07 73
```

To remove the lock code (set the YubiKey so that a lock code is no
longer needed), set the new lock code to all `00` bytes.

```txt
    0B 10
       71 08 B4 96 AC 38 64 F2
       81 D0 48 55 B2 EA 07 73
    0A 10
       00 00 00 00 00 00 00 00
       00 00 00 00 00 00 00 00
```

### Response APDU info

#### Response APDU for successful setting the device info

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 90  | 00  |

#### Response APDU for incorrect lock code provided

If there is a lock code set, but the Set command is sent with no lock
code (nothing under the 0B tag), or with an incorrect lock code, this
is the return.

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 6F  | 00  |

#### Response APDU when sent to YubiKeys before version 5

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 69  | 00  |
