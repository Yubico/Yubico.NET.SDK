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

## Get an assertion

### Command APDU info

CLA | INS | P1 | P2 | Lc | Data | Le
:---: | :---: | :---: | :---: | :---: | :---:
00 | 10 | 00 | 00 | *data length* | 02 *encoded info* | (absent)

The Ins byte (instruction) is 10, which is the byte for CTAPHID_CBOR.
That means the command information is in a CBOR encoded structure in the
Data.

The data consists of the CTAP Command Byte and the CBOR encoding of the
command's parameters. In this case, the CTAP Command Byte is `02`,
which is the command "`authenticatorGetAssertion`". The CBOR encoding is
described in the documentation for
[GetAssertionParameters](xref:Yubico.YubiKey.Fido2.GetAssertionParameters).

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
  A5
     01 --map--
     02 --byte string--
     03 --byte string--
     04 --map--
     05 --int--
     06 --boolean--
     07 --byte string--
```
