---
uid: Fido2AuthenticatorSelectionApdu
---

<!-- Copyright 2026 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

## Select an authenticator

### Command APDU info

| CLA | INS | P1 | P2 | Lc | Data |    Le    |
|:---:|:---:|:--:|:--:|:--:|:----:|:--------:|
| 00  | 10  | 00 | 00 | 01 |  0B  | (absent) |

The Ins byte (instruction) is 10, which is the byte for CTAPHID_CBOR.
That means the command information is in the Data.

The data consists of the CTAP Command Byte. In this case, the CTAP
Command Byte is `0B`, which is the command "`authenticatorSelection`".
There are no command parameters.

### Response APDU info

#### Response APDU for a successful selection

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 90  | 00  |

#### Response APDU when the command is not supported

If the authenticator does not implement `authenticatorSelection`, it
may return `CTAP1_ERR_INVALID_COMMAND` (`0x01`).

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 6F  | 01  |

#### Response APDU when the YubiKey times out

This happens when the user does not touch the contact within the timeout
period.

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 6F  | 2F  |

#### Response APDU when user presence is denied

This happens when user presence (UP) is explicitly denied.

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 6F  | 27  |

> [!NOTE]
> On the YubiKey, the user can either touch the key to select it or wait for the
> operation to time out—there is no separate deny or cancel control on the security key
> itself. When the user does not complete UP you will usually see
> `CTAP2_ERR_USER_ACTION_TIMEOUT`. However, `CTAP2_ERR_OPERATION_DENIED`
> may be returned if the user engages a platform dialog to cancel the request.
