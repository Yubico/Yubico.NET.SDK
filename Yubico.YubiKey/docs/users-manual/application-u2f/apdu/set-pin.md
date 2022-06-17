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

## Set the PIN to a new value

### Inner command APDU info

CLA | INS | P1 | P2 | Lc | Data | Le
:---: | :---: | :---: | :---: | :---: | :---:
00 | 44 | 00 | 00 | *data len* | *data* | (absent)

The data is encoded as

```text
   new PIN length (one byte) || current PIN || new PIN
```

If there is no current PIN, then the data will be encoded as

```text
   new PIN length (one byte) || new PIN
```

### Response APDU info

#### Response APDU for successful setting the new PIN

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 90 | 00

#### Response APDU for current PIN incorrect

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 63 | C0

#### Response APDU for PIN not supported

If you try to set a U2F PIN on a non-FIPS YubiKey, a version 5 YubiKey
(FIPS or non-FIPS), or a Security Key Series YubiKey, this is the
response.

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 6D | 00
