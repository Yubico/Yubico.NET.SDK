---
uid: YubiHsmAuthCmdGetAppVersion
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

# Get application version

Get the version of the YubiHSM Auth application returned as a major, minor, and patch value.

## Available

All YubiKeys with the YubiHSM Auth application (included in firmware version 5.4.3 and later).
> [!NOTE]
> Use the .NET
>
API's [HasFeature()](xref:Yubico.YubiKey.YubiKeyFeatureExtensions.HasFeature%28Yubico.YubiKey.IYubiKeyDevice%2CYubico.YubiKey.YubiKeyFeature%29)
> method to check if a key has the YubiHSM Auth application.

## SDK classes

* [GetApplicationVersionCommand](xref:Yubico.YubiKey.YubiHsmAuth.Commands.GetApplicationVersionCommand)
* [GetApplicationVersionResponse](xref:Yubico.YubiKey.YubiHsmAuth.Commands.GetApplicationVersionResponse)

## Input

None.

## Output

An array of three bytes which correspond with the major, minor, and patch value.

## Command APDU

| CLA | INS | P1 | P2 |    Lc    |   Data   |    Le    |
|:---:|:---:|:--:|:--:|:--------:|:--------:|:--------:|
| 00  | 07  | 00 | 00 | (absent) | (absent) | (absent) |

## Response APDU

Total Length: *5*\
Data Length: *3*

|         Data          | SW1 | SW2 |
|:---------------------:|:---:|:---:|
| {major, minor, patch} | 90  | 00  |