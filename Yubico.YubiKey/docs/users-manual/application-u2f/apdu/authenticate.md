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

## Authenticate the YubiKey to a relying party

### Inner command APDU info

CLA | INS | P1 | P2 | Lc | Data | Le
:---: | :---: | :---: | :---: | :---: | :---:
00 | 02 | *control byte* | 00 | *length* | *data* | (absent)

The control byte is either `03` (enforce user presence), `07` (check only), or `08`
(don't enforce user presence).

The data is

```txt
challenge parameter || application parameter || key handle length || key handle
```

Where the challenge parameter is the client data hash and the application parameter is the
hash of the origin data. Each is a SHA-256 message digest so each is 32 byte long. The key
handle length is one byte.

### Response APDU info

#### Response APDU for successful authentication

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
*encoded response* | 90 | 00

where the encoded response is

```txt
user presence || counter || signature
```

#### Response APDU for user presence required

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 69 | 85

#### Response APDU for invalid key handle

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 6A | 80

#### Response APDU for incorrect data length.

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 67 | 00
