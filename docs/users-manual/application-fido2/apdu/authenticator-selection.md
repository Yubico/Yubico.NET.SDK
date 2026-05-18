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

## Authenticator selection (CTAP `authenticatorSelection`)

### Command APDU info

| CLA | INS | P1 | P2 | Lc | Data |    Le    |
|:---:|:---:|:--:|:--:|:--:|:----:|:--------:|
| 00  | 10  | 00 | 00 | 01 |  0B  | (absent) |

The `Ins` byte is `10` (CTAPHID_CBOR). The data is the CTAP command byte `0B` (*authenticatorSelection*) only; there are no CBOR parameters (CTAP version `2.2` §6.9).

### Response APDU info

#### Success

Total Length: 2 (or success with empty CBOR payload after status, depending on transport framing)  
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 90  | 00  |

#### Command not supported

If the authenticator does not implement *authenticatorSelection*, it may return `CTAP1_ERR_INVALID_COMMAND` (`0x01`), which the SDK surfaces with SW2 = `01` and SW1 = `6F` (no precise diagnosis), consistent with other CTAP error mappings.

#### User action timeout

If the user does not complete User Presence (UP) in time, the authenticator returns `CTAP2_ERR_USER_ACTION_TIMEOUT` (`0x2F`).

#### User Presence (UP) denied

CTAP version `2.2` §6.9 states that if User Presence (UP) is **explicitly denied**, the authenticator returns `CTAP2_ERR_OPERATION_DENIED` (`0x27`). That is distinct from waiting until a timer expires (see below).

> [!NOTE]
> On the YubiKey, the only user affordance is **touch to approve** or **no touch** until the operation times out. There is **no separate “deny” or “cancel” control on the security key itself**, so when the user does not complete UP you will usually see **`CTAP2_ERR_USER_ACTION_TIMEOUT`**, not an explicit denial. **`CTAP2_ERR_OPERATION_DENIED`** may be returned if the user engages a platform dialog to cancel the request.
