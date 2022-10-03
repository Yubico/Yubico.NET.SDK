---
uid: YubiHsmAuthSession
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

# YubiHSM Auth session APIs

The high level YubiHSM Auth session APIs provide a simpler way to work with the YubiHSM Auth application on the YubiKey. The YubiHSM Auth session API is a layer built on the lower level command API. Session APIs help perform YubiHSM Auth scenarios in a shorter amount of development time and without getting involved with each command's details.

For more information on the YubiHSM Auth application and commands, see [YubiHSM Auth Overview](xref:YubiHsmAuthOverview).

## YubiHsmAuthSession

To perform YubiHSM Auth operations, first select the IYubiKeyDevice you would like to use. Next, create an instance of the YubiHsmAuthSession class using that device. During the lifetime of that session, you can use the session APIs as a simple way to work with the YubiHSM Auth application on the YubiKey.

```csharp
// use the first YubiKey found
var yubiKeyToUse = YubiKeyDevice.FindAll().First();
using (var YubiHsmAuthSession = new YubiHsmAuthSession(yubiKeyToUse))
{
    // call session methods
}
```

> [!NOTE]
> For more information on connecting to a YubiKey with the YubiKeyDevice class, please see the [SDK programming guide](xref:UsersManualMakingAConnection).

## Methods

Clicking on the method will bring you to the API documentation where more information can be found.

| Method | Description |
| -- | -- |
| [Reset application](xref:Yubico.YubiKey.YubiHsmAuth.YubiHsmAuthSession.ResetApplication) | Reset the YubiHSM Auth application, which will delete all credentials and set the management key to its default value (all zeros). |
| [Get application version](xref:Yubico.YubiKey.YubiHsmAuth.YubiHsmAuthSession.GetApplicationVersion) | Get the version of the YubiHSM Auth application returned as a major, minor, and patch value. |
| [Get management key retries](xref:Yubico.YubiKey.YubiHsmAuth.YubiHsmAuthSession.GetManagementKeyRetries) | Get the number of retries remaining for the management key. When supplying the management key for an operation, there is a limit of 8 retries before the application is locked and must be completely reset. Supplying the correct management key before the application is locked will reset the retry counter to 8. |
