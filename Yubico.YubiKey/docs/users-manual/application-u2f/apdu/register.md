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

## Register the YubiKey with a relying party

### Inner command APDU info

CLA | INS | P1 | P2 | Lc | Data | Le
:---: | :---: | :---: | :---: | :---: | :---:
00 | 01 | 00 | 00 | 64 | *data* | (absent)

The data is

```txt
   challenge parameter || application parameter
```

Where the challenge parameter is the client data hash and the application parameter is the
hash of the origin data. Each is a SHA-256 message digest so each is 32 byte long.

### Response APDU info

#### Response APDU for successful registration

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
*encoded response* | 90 | 00

where the encoded response is

```txt
05 || public key || key handle length || key handle || cert || signature)
```

#### Response APDU for PIN required

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 63 | C0

#### Response APDU for blocked PIN

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 69 | 83

#### Response APDU for touch required

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 69 | 85

#### Response APDU for incorrect data length

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 67 | 00
