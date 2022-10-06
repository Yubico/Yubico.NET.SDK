---
uid: YubiHsmAuthCmdGetMgmtRetries
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

# Get management key retries

Get the number of retries remaining for the management key.

Some operations require authentication with the management key (such as adding and deleting credentials). There is a limit of 8 attempts to authenticate with the management key before the management key is blocked. Once the management key is blocked, the application itself must be reset before authentication can be attempted again. To reset the application, see [ResetApplicationCommand](xref:YubiHsmAuthCmdResetApplication). Supplying the correct management key before the management key is blocked will reset the retry counter to 8.

## Available

All YubiKeys with the YubiHSM Auth application (included in firmware version 5.4.3 and later).
> [!NOTE]
> Use the .NET API's [HasFeature()](xref:Yubico.YubiKey.YubiKeyFeatureExtensions.HasFeature%28Yubico.YubiKey.IYubiKeyDevice%2CYubico.YubiKey.YubiKeyFeature%29) method to check if a key has the YubiHSM Auth application.

## SDK classes

* [GetManagementKeyRetriesCommand](xref:Yubico.YubiKey.YubiHsmAuth.Commands.GetManagementKeyRetriesCommand)
* [GetManagementKeyRetriesResponse](xref:Yubico.YubiKey.YubiHsmAuth.Commands.GetManagementKeyRetriesResponse)

## Input

None.

## Output

The number of retries for the management key, as a byte.

## Command APDU

| CLA | INS | P1 | P2 | Lc | Data | Le |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| 00 | 09 | 00 | 00 | (absent) | (absent) | (absent) |

## Response APDU

Total Length: *3*\
Data Length: *1*

| Data | SW1 | SW2 |
| :---: | :---: | :---: |
| management key retries remaining | 90 | 00 |
