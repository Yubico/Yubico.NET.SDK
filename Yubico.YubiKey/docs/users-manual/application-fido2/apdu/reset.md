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

## Get the next assertion

### Command APDU info

| CLA | INS | P1 | P2 | Lc | Data |    Le    |
|:---:|:---:|:--:|:--:|:--:|:----:|:--------:| 
| 00  | 10  | 00 | 00 | 01 |  07  | (absent) |

The Ins byte (instruction) is 10, which is the byte for CTAPHID_CBOR.
That means the command information is in a CBOR encoded structure in the
Data.

The data consists of the CTAP Command Byte and the CBOR encoding of the
command's parameters. In this case, the CTAP Command Byte is `07`,
which is the command "`authenticatorReset`". There are no command parameters.

### Response APDU info

#### Response APDU for a successful reset

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 90  | 00  |

#### Response APDU when the YubiKey denies the request

This happens when the YubiKey will not reset over the transport through
which it is connected. For example, the YubiKey might not allow the
reset command over NFC.

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 6F  | 27  |

#### Response APDU when the YubiKey is not allowed to be reset

This happens when the YubiKey has been inserted for too long. A YubiKey
can only be reset within a time limit of being inserted (the standard
specifies 10 seconds). Generally a program will get this error, then
instruct the user to remove then reinsert the YubiKey and the command
is sent again.

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 6F  | 30  |

#### Response APDU when the YubiKey times out

This happens when the YubiKey can be reset, but the user does not touch
the contact. If the YubiKey has not been inserted for too long, the
Reset command can be executed. But once the YubiKey receives that
command it will require touch before completing it. If the user does not
touch the YubiKey within a timeout period, it will return this error.

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 6F  | 3A  |
