---
uid: OtpUpdateSlot
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

# How to update slot settings

Some [slot](xref:OtpSlots) settings can be updated via the [UpdateSlot](xref:Yubico.YubiKey.Otp.Operations.UpdateSlot) method without completely reconfiguring an OTP application slot. These settings involve behaviors not related to encryption or other sensitive information.

The slot settings that can be updated include the following:

| Settings |          |
|----------|----------|
| ```SetDormant()``` | ```SetAppendDelayToFixed()``` |
| ```SetSerialNumberApiVisible()``` | ```SetUse10msPacing()``` |
| ```SetUseNumericKeypad()``` | ```ProtectLongPressSlot()``` |
| ```SetAppendTabToOtp()``` | ```SetInvertLed()``` |
| ```SetAppendCarriageReturn()``` | ```SetSerialNumberUsbVisible()``` |
| ```SetAllowUpdate()``` | ```SetAppendTabToFixed()``` |
| ```SetFastTrigger()``` | ```SetAppendDelayToOtp()``` |
| ```SetSerialNumberButtonVisible()``` | ```SetUse20msPacing()``` |
| ```SetSendTabFirst()``` | |

> [!NOTE]
> There is no way to retrieve the settings of an OTP slot configuration. Therefore, when you use ```UpdateSlot```, you’re resetting every setting. For example, if you intend to add a carriage return to the slot configuration and only call ```SetAppendCarriageReturn()```, all other settings will revert to their default states.

## UpdateSlot example

The following is an example of how to update the settings of a slot with ```UpdateSlot```. We’ll assume that the boolean variables are set elsewhere.

```C#
using (OtpSession otp = new OtpSession(_yubiKey))
{
  otp.UpdateSlot(_slot)
    .UseCurrentAccessCode(_currentAccessCode)
    .SetNewAccessCode(_newAccessCode)
    .SetDormant(_dormant)
    .SetFastTrigger(_fastTrigger)
    .SetInvertLed(_invertLed)
    .SetSerialNumberApiVisible(_serialApi)
    .SetSerialNumberButtonVisible(_serialButton)
    .SetSerialNumberUsbVisible(_serialUsb)
    .SetUseNumericKeypad(_numericKeypad)
    .SetSendTabFirst(_sendTabFirst)
    .SetAppendTabToFixed(_appendTabToFixed)
    .SetAppendTabToOtp(_appendTabToOtp)
    .SetAppendDelayToFixed(_appendDelayToFixed)
    .SetAppendDelayToOtp(_appendDelayToOtp)
    .SetAppendCarriageReturn(!_noEnter)
    .SetUse10msPacing(_use10msPacing)
    .SetUse20msPacing(_use20msPacing)
    .SetAllowUpdate(_allowUpdate)
    .ProtectLongPressSlot(_protectLongPressSlot)
    .ExecuteOperation();
}
```
