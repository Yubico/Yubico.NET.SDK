---
uid: YubiHsmAuthCmdListCredentials
---

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

# List credentials

## Command APDU

| CLA | INS | P1 | P2 | Lc | Data | Le |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| 00 | 05 | 00 | 00 | (absent) | (absent) | (absent) |

## Response APDU

Total Length: *variable + 2*\
Data Length: *variable*

| Data | SW1 | SW2 |
| :---: | :---: | :---: |
| *data* | 90 | 00 |
