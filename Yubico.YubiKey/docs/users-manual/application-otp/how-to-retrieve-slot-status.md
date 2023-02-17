---
uid: OtpRetrieveSlotStatus
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

# How to retrieve a slot's status

When you construct an [OtpSession](xref:Yubico.YubiKey.Otp.OtpSession) object, you can retrieve the general status of both OTP application [slots](xref:OtpSlots). Slot status will tell you if the slot:

* is configured
* requires touch

To output slot status to the console, do the following:

```C#
using (OtpSession otp = new OtpSession(yKey))
{
  Output(Slot.ShortPress, otp.IsShortPressConfigured, otp.ShortPressRequiresTouch);
  Output(Slot.LongPress, otp.IsLongPressConfigured, otp.LongPressRequiresTouch);
}
void Output(Slot slot, bool configured, bool touchRequired)
{
  Console.WriteLine($"Slot {slot} Configured: {configured}");
  if (configured)
  {
    Console.WriteLine($"Slot {slot} Requires Touch: {touchRequired}");
  }
}
```
