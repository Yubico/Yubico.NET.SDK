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

## Get a PIN token

### Command APDU info

CLA | INS | P1 | P2 | Lc | Data | Le
:---: | :---: | :---: | :---: | :---: | :---:
00 | 10 | 00 | 00 | *data length* | 06 *encoded info* | (absent)

The Ins byte (instruction) is 10, which is the byte for CTAPHID_CBOR.
That means the command information is in a CBOR encoded structure in the
Data.

The data consists of the CTAP Command Byte and the CBOR encoding of the
command's parameters. In this case, the CTAP Command Byte is `06`,
which is the command "`authenticatorClientPin`". The CBOR encoding is
the following:

```txt
  A4         map containing four elements
     01      key (of key/value) specifying ...
        0x   ... PIN/UV protocol (x=1 for protocol one, x=2 for protocol two)
     02      key specifying ...
        05   ... subcommand, 05 = getPinToken
     03      key specifying ...
        <>   ... CBOR-encoded COSE_Key, the platform's public key
     06      key specifying ...
        <>   ... encrypted hash of current PIN
```

### Response APDU info

#### Response APDU for a successful get

Total Length: *variable + 2*\
Data Length: *variable*

      Data      | SW1 | SW2

:--------------:| :---: | :---:
*encoded info* | 90 | 00

The info returned is CBOR encoded. It has a structure similar to the
following.

```txt
  A1
     02 --byte string--
```

The byte string is the encrypted token. For protocol one, the string
will be 32 bytes long, and for protocol two the string will be 48 bytes
long.

#### Response APDU when no protocol is given

Total Length: 2\
Data Length: 0

   Data    | SW1 | SW2 
:---------:|:---:|:---:
 (no data) | 6F  | 14  

#### Response APDU when an unsupported protocol is specified

Total Length: 2\
Data Length: 0

   Data    | SW1 | SW2 
:---------:|:---:|:---:
 (no data) | 6F  | 33  
