---
uid: OtpDeleteSlotConfig
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

Deleting a [slot's](xref:OtpSlots) configuration removes all credentials, associated counters (if any), slot settings, etc. To delete a slot's configuration, you must use one of two methods:

- [DeleteSlot()](xref:Yubico.YubiKey.Otp.OtpSession.DeleteSlot%28Yubico.YubiKey.Otp.Slot%29): for slots that are not configured with an access code.
- [DeleteSlotConfiguration()](xref:Yubico.YubiKey.Otp.OtpSession.DeleteSlotConfiguration%28Yubico.YubiKey.Otp.Slot%29): for slots that are configured with an access code.

> [!NOTE]
> These methods will fail if the slot you are attempting to delete is not configured.

## Examples

Before running any of the code provided below, make sure you have already connected to a particular YubiKey on your host device via the [YubiKeyDevice](xref:Yubico.YubiKey.YubiKeyDevice) class. 

To select the first available YubiKey connected to your host, use:

```C#
IEnumerable<IYubiKeyDevice> yubiKeyList = YubiKeyDevice.FindAll();

var yubiKey = yubiKeyList.First();
```

### Deleting a slot configuration when an access code is not present

To delete a slot configuration that is not protected with an access code, use [DeleteSlot()](xref:Yubico.YubiKey.Otp.OtpSession.DeleteSlot%28Yubico.YubiKey.Otp.Slot%29). You cannot chain other methods to ``DeleteSlot()``, including ``Execute()``. When calling ``DeleteSlot()``, just provide the slot field (in this example, ``Slot.LongPress``). 

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
  otp.DeleteSlot(Slot.LongPress);
}
```

### Deleting a slot configuration when an access code is present

To delete a slot configuration that is protected with an access code, you must call [DeleteSlotConfiguration](xref:Yubico.YubiKey.Otp.OtpSession.DeleteSlotConfiguration%28Yubico.YubiKey.Otp.Slot%29) and provide the current access code with [UseCurrentAccessCode()](xref:Yubico.YubiKey.Otp.Operations.OperationBase%601.UseCurrentAccessCode%28Yubico.YubiKey.Otp.SlotAccessCode%29). You cannot set a new access code during this operation--calling [SetNewAccessCode()](xref:Yubico.YubiKey.Otp.Operations.OperationBase%601.SetNewAccessCode%28Yubico.YubiKey.Otp.SlotAccessCode%29) will succeed, but the operation will not be applied. 

Unlike ``DeleteSlot()``, ``DeleteSlotConfiguration()`` requires ``Execute()`` for the operation to apply the changes. 

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
  ReadOnlyMemory<byte> currentAccessCodeBytes = new byte[SlotAccessCode.MaxAccessCodeLength] { 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, };
  SlotAccessCode currentAccessCode = new SlotAccessCode(currentAccessCodeBytes);

  otp.DeleteSlotConfiguration(Slot.LongPress)
    .UseCurrentAccessCode(currentAccessCode)
    .Execute();
}
```