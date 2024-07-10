---
uid: YubiHsmAuthCmdChangeManagementKey
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

# Change management key

This command is used to change the management key. The management key is required
when [adding](xref:YubiHsmAuthCmdAddCredential) or [deleting](xref:YubiHsmAuthCmdDeleteCredential) credentials from the
YubiHSM Auth application.

There is a limit of 8 attempts to authenticate with the management key before the management key is blocked. Once the
management key is blocked, the application itself must be reset before authentication can be attempted again. To reset
the application, see [ResetApplicationCommand](xref:YubiHsmAuthCmdResetApplication). Supplying the correct management
key before the management key is blocked will reset the retry counter to 8.

## Available

All YubiKeys with the YubiHSM Auth application (included in firmware version 5.4.3 and later).
> [!NOTE]
> Use the .NET
>
API's [HasFeature()](xref:Yubico.YubiKey.YubiKeyFeatureExtensions.HasFeature%28Yubico.YubiKey.IYubiKeyDevice%2CYubico.YubiKey.YubiKeyFeature%29)
> method to check if a key has the YubiHSM Auth application.

## SDK classes

* [ChangeManagementKeyCommand](xref:Yubico.YubiKey.YubiHsmAuth.Commands.ChangeManagementKeyCommand)
* [ChangeManagementKeyResponse](xref:Yubico.YubiKey.YubiHsmAuth.Commands.ChangeManagementKeyResponse)

## Input

This command takes in the current management key and the new management key. Each management key is a byte array with
exactly 16 bytes.

The default value of the management key is all zeros:

```text
00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
```

## Output

None.

## Command APDU

| CLA | INS | P1 | P2 | Lc |            Data            |    Le    |
|:---:|:---:|:--:|:--:|:--:|:--------------------------:|:--------:|
| 00  | 08  | 00 | 00 | 20 | See [section below](#data) | (absent) |

### Data

The data field is a byte array formatted as a pair of TLVs representing the current and new management keys. Both TLV
elements have the same tag and must be arranged in the following order:

| Order | Meaning                | Tag  | Size (bytes) |
|:-----:|:-----------------------|:----:|:------------:|
|   1   | Current management key | 0x7b |      16      |
|   2   | New management key     | 0x7b |      16      |

## Response APDU

The data field is always empty. On success, the status word will be 0x90 0x00. If there was a failure, further
information may be communicated in the status word.

Total Length: 2\

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 90  | 00  |

### Common failure status words

| Value  | Meaning                                                                            |
|:------:|:-----------------------------------------------------------------------------------|
| 0x6983 | A credential with that label already exists                                        |
| 0x63c# | Wrong management key, where # is the number of attempts remaining (a maximum of 8) |
| 0x6a80 | Wrong syntax                                                                       |
