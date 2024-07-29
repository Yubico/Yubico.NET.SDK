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

## Reset the FIDO U2F application

### Inner command APDU info

| CLA | INS | P1 | P2 |    Lc    |   Data   |    Le    |
|:---:|:---:|:--:|:--:|:--------:|:--------:|:--------:|
| 00  | 45  | 00 | 00 | (absent) | (absent) | (absent) |

### Response APDU info

#### Response APDU for a successful reset

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 90  | 00  |

#### Response APDU when the YubiKey is not reset

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 69  | 86  |
