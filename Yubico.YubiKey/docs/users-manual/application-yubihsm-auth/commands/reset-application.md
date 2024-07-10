---
uid: YubiHsmAuthCmdResetApplication
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

# Reset application

Reset the YubiHSM Auth application. All credentials will be deleted, the management key will be reset to the default
value (all zeros), and the management key retry counter will be reset to 8.

## Available

All YubiKeys with the YubiHSM Auth application (included in firmware version 5.4.3 and later).
> [!NOTE]
> Use the .NET
>
API's [HasFeature()](xref:Yubico.YubiKey.YubiKeyFeatureExtensions.HasFeature%28Yubico.YubiKey.IYubiKeyDevice%2CYubico.YubiKey.YubiKeyFeature%29)
> method to check if a key has the YubiHSM Auth application.

## SDK classes

* [ResetApplicationCommand](xref:Yubico.YubiKey.YubiHsmAuth.Commands.ResetApplicationCommand)
* [ResetApplicationResponse](xref:Yubico.YubiKey.YubiHsmAuth.Commands.ResetApplicationResponse)

## Input

None.

## Output

None.

## Command APDU

| CLA | INS | P1 | P2 |    Lc    |   Data   |    Le    |
|:---:|:---:|:--:|:--:|:--------:|:--------:|:--------:|
| 00  | 06  | de | ad | (absent) | (absent) | (absent) |

## Response APDU

Total Length: 2\

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 90  | 00  |
