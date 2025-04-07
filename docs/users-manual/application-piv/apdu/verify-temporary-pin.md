<!-- Copyright 2024 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

## Verify temporary PIN

### Command APDU info

| CLA | INS | P1 | P2 | Lc |         Data          |    Le    |
|:---:|:---:|:--:|:--:|:--:|:---------------------:|:--------:| 
| 00  | 20  | 00 | 96 | 12 | 01 10 *temporary PIN* | (absent) |

### Response APDU info

#### Response APDU for VERIFY (success)

Total Length: 2\
Data Length: 0

   Data    | SW1 | SW2 
:---------:|:---:|:---:
 (no data) | 90  | 00  

#### Response APDU for VERIFY (Invalid temporary PIN)

Total Length: 2\
Data Length: 0

   Data    | SW1 | SW2 
:---------:|:---:|:---:
 (no data) | 63  | C0  

If the temporary PIN is incorrect, then the error is `63 C0`. The temporary PIN in invalidated in the YubiKey and a new
one needs to be obtained.

