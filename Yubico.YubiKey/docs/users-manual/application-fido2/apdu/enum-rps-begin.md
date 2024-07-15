<!-- Copyright 2023 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

## Enumerate RPs: begin

### Command APDU info

CLA | INS | P1 | P2 | Lc | Data | Le
:---: | :---: | :---: | :---: | :---: | :---:
00 | 10 | 00 | 00 | *data length* | 0A *encoded info* | (absent)

The Ins byte (instruction) is 10, which is the byte for CTAPHID_CBOR.
That means the command information is in a CBOR encoded structure in the
Data.

The data consists of the CTAP Command Byte and the CBOR encoding of the
command's parameters. In this case, the CTAP Command Byte is `0A`,
which is the command "`authenticatorCredentialManagement`". The CBOR
encoding is

```txt
  A3
     01 --int-- subcommand = 02
     03 --int-- protocol
     04 --byte string-- PinUvAuthParam
```

### Response APDU info

#### Response APDU for a successful get

Total Length: *variable + 2*\
Data Length: *variable*

|      Data      | SW1 | SW2 |
|:--------------:|:---:|:---:|
| *encoded info* | 90  | 00  |

The info returned is CBOR encoded. It has a structure similar to the
following.

```txt
  A3
     03 --map-- Rp
     04 --byte string-- RpIdHash
     05 --int-- total number of Rps
```
