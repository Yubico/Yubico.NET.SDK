<!-- Copyright 2022 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

## Get FIDO2 and device info

### Command APDU info

| CLA | INS | P1 | P2 | Lc | Data |    Le    |
|:---:|:---:|:--:|:--:|:--:|:----:|:--------:| 
| 00  | 10  | 00 | 00 | 01 |  04  | (absent) |

### Response APDU info

Total Length: *variable + 2*\
Data Length: *variable*

|      Data      | SW1 | SW2 |
|:--------------:|:---:|:---:|
| *encoded info* | 90  | 00  |

The info returned is CBOR encoded. It has a structure similar to the
following.

```
  AC
     01 --data--
     02 --data--
       . . .
     14 --data--
```

The `AC` means there's a map of 12 key/value pairs. Each pair is an
integer followed by data encoded as specified by the integer, defined in
the CTAP 2.1 standard, section 6.4.

The standard specifies integer keys from `01` to `15` (in decimal that
is 1 to 21). However, the YubiKey does not support the key `15`.

Most of the elements are optional, so that any one encoding may or may
not have the same subset of possible key/value pairs.
