---
uid: OtpDeleteSlotConfig
summary: *content
---

<!-- Copyright 2021 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# How to delete a slot's configuration

Deleting the configuration stored in a [slot](xref:OtpSlots) via the ```DeleteSlot()``` method is a simple operation. The only parameters that must be provided are the slot name and the slot access code (if applicable). Therefore, ```DeleteSlot()``` executes the operation directly instead of constructing an object.

## DeleteSlot example

In the following example, the configuration of the [long-press](xref:Yubico.YubiKey.Otp.Slot.LongPress) slot of the OTP application will be deleted, assuming the correct access code is given:

```
using (OtpSession otp = new OtpSession(yKey))
{
  otp.DeleteSlot(Slot.LongPress)
    .UseCurrentAccessCode(_currentAccessCode)
    .Execute();
}

```

> [!NOTE]
> This method will fail if the slot you are trying to delete is not currently configured.
