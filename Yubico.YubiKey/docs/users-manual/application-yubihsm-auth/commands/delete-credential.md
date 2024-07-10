---
uid: YubiHsmAuthCmdDeleteCredential
---

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

# Delete credential

Remove a credential from the YubiHSM Auth application.

## Available

All YubiKeys with the YubiHSM Auth application (included in firmware version 5.4.3 and later).
> [!NOTE]
> Use the .NET
>
API's [HasFeature()](xref:Yubico.YubiKey.YubiKeyFeatureExtensions.HasFeature%28Yubico.YubiKey.IYubiKeyDevice%2CYubico.YubiKey.YubiKeyFeature%29)
> method to check if a key has the YubiHSM Auth application.

## SDK classes

* [DeleteCredentialCommand](xref:Yubico.YubiKey.YubiHsmAuth.Commands.DeleteCredentialCommand)
* [DeleteCredentialResponse](xref:Yubico.YubiKey.YubiHsmAuth.Commands.DeleteCredentialResponse)

## Input

The input includes the label of the credential to be deleted, and the management key.

There is a limit of 8 attempts to authenticate with the management key before the management key is blocked. Once the
management key is blocked, the application must be reset before performing operations which require authentication with
the management key (such as adding credentials, deleting credentials, and changing the management key). To reset the
application, see [ResetApplicationCommand](xref:YubiHsmAuthCmdResetApplication). Supplying the correct management key
before the management key is blocked will reset the retry counter to 8.

## Output

None.

## Command APDU

| CLA | INS | P1 | P2 |     Lc     |       Data       |    Le    |
|:---:|:---:|:--:|:--:|:----------:|:----------------:|:--------:|
| 00  | 02  | 00 | 00 | *variable* | (TLV, see below) | (absent) |

### Data

The data is sent as concatenated TLV-formatted elements, as follows:

| Tag (hexadecimal) | Length (decimal) |     Value      | Notes                                                |
|:-----------------:|:----------------:|:--------------:|:-----------------------------------------------------|
|       0x7b        |        16        | management key | used to authenticate to the YubiHSM Auth application |
|       0x71        |       1-64       |     label      | UTF-8 encoded string                                 |

## Response APDU

Total Length: 2\

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 90  | 00  |
