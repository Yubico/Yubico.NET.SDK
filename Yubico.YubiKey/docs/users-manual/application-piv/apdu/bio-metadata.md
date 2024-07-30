---
uid: UsersManualBioMetadata
---

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


## Get bio metadata

### Command APDU Info

| CLA | INS | P1 | P2 |    Lc    |   Data   |    Le    |
|:---:|:---:|:--:|:--:|:--------:|:--------:|:--------:| 
| 00  | F7  | 00 | 96 | (absent) | (absent) | (absent) |

### Response APDU Info

Total Length: *9* + 2\
Data Length: *9*

Data | SW1 | SW2
:---: | :---: | :---:
*bio metadata as set of TLV* | 90 | 00

The data consists of a set of TLVs. The possible valid tags (T of TLV) are listed in the
table below. The length (L of TLV) is one. The values (V of TLV) are dependent on the tags, 
described in the table below.

#### Table 1: List of Metadata Elements
Tag | Name | Meaning | Data
:---: | :---: | :---: | :---:
07 | IsConfigured| state of biometric verification configuration<br/> (ie. fingerprints are enrolled) | 01 (configured)<br/> 00 (not configured)
06 | RetriesRemaining| indicates how many biometric match retries are left| 00-03<br/>(when IsConfigured is 01, value 00 indicates that biometric verification is blocked)
08 | HasTemporaryPin| indicates if a temporary PIN has been generated in the YubiKey | 01 (generated)<br/>00 (not generated)

