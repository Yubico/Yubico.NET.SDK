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

| Method | Description | Try-Parse version |
| -- | -- |
| [Add credential](xref:Yubico.YubiKey.YubiHsmAuth.YubiHsmAuthSession.AddCredential(System.ReadOnlyMemory{System.Byte},Yubico.YubiKey.YubiHsmAuth.CredentialWithSecrets)) | Add a credential. | [Try add credential](xref:Yubico.YubiKey.YubiHsmAuth.YubiHsmAuthSession.TryAddCredential(System.ReadOnlyMemory{System.Byte},Yubico.YubiKey.YubiHsmAuth.CredentialWithSecrets,System.Nullable{System.Int32}@)) |
| [Change management key](xref:Yubico.YubiKey.YubiHsmAuth.YubiHsmAuthSession.TryChangeManagementKey(System.ReadOnlyMemory{System.Byte},System.ReadOnlyMemory{System.Byte},System.Nullable{System.Int32}@)) | Change the management key. | [Try change management key](xref:Yubico.YubiKey.YubiHsmAuth.YubiHsmAuthSession.TryChangeManagementKey(System.ReadOnlyMemory{System.Byte},System.ReadOnlyMemory{System.Byte},System.Nullable{System.Int32}@)) |
| [Delete credential](xref:Yubico.YubiKey.YubiHsmAuth.YubiHsmAuthSession.DeleteCredential(System.ReadOnlyMemory{System.Byte},System.String)) | Delete a credential. | [Try delete credential](xref:Yubico.YubiKey.YubiHsmAuth.YubiHsmAuthSession.TryDeleteCredential(System.ReadOnlyMemory{System.Byte},System.String,System.Nullable{System.Int32}@)) |
| [Get AES-128 session keys](xref:Yubico.YubiKey.YubiHsmAuth.YubiHsmAuthSession.GetAes128SessionKeys(System.String,System.ReadOnlyMemory{System.Byte},System.ReadOnlyMemory{System.Byte},System.ReadOnlyMemory{System.Byte})) | Calculate session keys from an AES-128 credential. These session keys are used to establish a secure session with a YubiHSM 2 device. | n/a |
| [Get application version](xref:Yubico.YubiKey.YubiHsmAuth.YubiHsmAuthSession.GetApplicationVersion) | Get the version of the YubiHSM Auth application returned as a major, minor, and patch value. | n/a |
| [Get management key retries](xref:Yubico.YubiKey.YubiHsmAuth.YubiHsmAuthSession.GetManagementKeyRetries) | Get the number of retries remaining for the management key. | n/a |
| [List credentials](xref:Yubico.YubiKey.YubiHsmAuth.YubiHsmAuthSession.ListCredentials) | Get the public properties of all credentials in the YubiHSM Auth application, along with the number of retries remaining for each. | n/a |
| [Reset application](xref:Yubico.YubiKey.YubiHsmAuth.YubiHsmAuthSession.ResetApplication) | Reset the YubiHSM Auth application, which will delete all credentials and set the management key to its default value (all zeros). | n/a |