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
> Use the .NET API's [HasFeature()](xref:Yubico.YubiKey.YubiKeyFeatureExtensions.HasFeature%28Yubico.YubiKey.IYubiKeyDevice%2CYubico.YubiKey.YubiKeyFeature%29) method to check if a key has the YubiHSM Auth application.

## SDK classes

* [DeleteCredentialCommand](xref:Yubico.YubiKey.YubiHsmAuth.Commands.DeleteCredentialCommand)
* [DeleteCredentialResponse](xref:Yubico.YubiKey.YubiHsmAuth.Commands.DeleteCredentialResponse)

## Input

This operation requires authenticating to the YubiHSM Auth application by providing the application's management key. The credential to be deleted is then selected by providing the credential's label. The label may only contain characters that can be encoded with UTF-8, and its UTF-8 byte count must be between 1 and 64. Non-printing characters are allowed, as long as they can be encoded with UTF-8. For example, null (UTF-8: 0x00) and Right-To-Left Mark U+200F (UTF-8: 0xE2 0x80 0x8F) would be accepted.

## Output

None.

## Command APDU

| CLA | INS | P1 | P2 | Lc | Data | Le |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| 00 | 02 | 00 | 00 | *variable* | (TLV, see below) | (absent) |

### Data

The data is sent as concatenated TLV-formatted elements, as follows:

| Tag (hexadecimal) | Length (decimal) | Value | Notes |
| :---: | :---: | :---: | :--- |
| 0x7b | 16 | managment key | used to authenticate to the YubiHSM Auth application |
| 0x71 | 1-64 | label | UTF-8 encoded string |

## Response APDU

Total Length: 2\

| Data | SW1 | SW2 |
| :---: | :---: | :---: |
| (no data) | 90 | 00 |
