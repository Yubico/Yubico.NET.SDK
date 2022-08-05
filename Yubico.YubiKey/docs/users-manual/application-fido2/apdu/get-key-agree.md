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

## Get the YubiKey's Key Agreement public key

### Command APDU info

CLA | INS | P1 | P2 | Lc | Data | Le
:---: | :---: | :---: | :---: | :---: | :---:
00 | 10 | 00 | 00 | 06 | 06 A2 01 02 02 02 | (absent)

The Ins byte (instruction) is 10, which is the byte for CTAPHID_CBOR.
That means the command information is in a CBOR encoded structure in the
Data.

The data consists of the CTAP Command Byte and the CBOR encoding of the
command's parameters. In this case, the CTAP Command Byte is `06`,
which is the command "`authenticatorClientPin`". The CBOR encoding is
the following.

```txt
  A2         map containing two elements
     01      first element key (of key/value)
        0x   UV/PIN protocol (x=1 for protocol 1, x=2 for protocol 2)
     02      second element key
        02   subcommand, 02 = KeyAgreement
```

### Response APDU info

#### Response APDU for a successful get

Total Length: *variable + 2*\
Data Length: *variable*

Data | SW1 | SW2
:---: | :---: | :---:
*encoded info* | 90 | 00

The info returned is CBOR encoded. It has a structure similar to the
following.

```txt
  A5
     01 --int--
     03 --int--
     20 --int--
     21 --byte string--
     22 --byte string--
```

The integers describe the algorithm and curve, and the byte strings are
the x- and y-coordinates of the public key. 

The lengths of the byte string are dependent on the algorithm.
Currently only one algorithm is supported, ECDH using the NIST curve
P-256. That means the byte strings are both 32 bytes long. The total
length of the encoding will be 78 bytes. Hence, the total length of the
response will be 80 bytes.

#### Response APDU when no protocol is given

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 6F | 14

#### Response APDU when an unsupported protocol is specified

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 6F | 33
