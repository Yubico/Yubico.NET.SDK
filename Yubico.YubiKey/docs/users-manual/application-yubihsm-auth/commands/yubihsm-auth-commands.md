---
uid: YubiHsmAuthCommands
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

# YubiHSM Auth commands

For each possible YubiHSM Auth command, there will be a class that knows how to build the command APDU and parse the data in the response APDU. Each class will know what information is needed from the caller for that command.

## List of YubiHSM Auth commands

* [List credentials](xref:YubiHsmAuthCmdListCredentials): get the public properties of all credentials present in the YubiHSM Auth application along with the number of retries remaining for each
* [Add credential](xref:YubiHsmAuthCmdAddCredential): store long-lived keys in the YubiHSM Auth application by creating a new credential
* [Delete credential](xref:YubiHsmAuthCmdDeleteCredential): remove a credential
* [Get management key retries](xref:YubiHsmAuthCmdGetMgmtRetries): get the number of retries for the management key
* [Change management key](xref:YubiHsmAuthCmdChangeManagementKey): change the management key
* [Get application version](xref:YubiHsmAuthCmdGetAppVersion): get the version of the YubiHSM Auth application
* [Reset application](xref:YubiHsmAuthCmdResetApplication): reset the YubiHSM Auth application