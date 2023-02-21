---
uid: OtpReadNDEF
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

# How to read NDEF information

In [How to configure NDEF to use a slot to generate an OTP](xref:OtpConfigureNDEF), we discussed how to configure [NDEF](xref:OtpNdef) functionality with OTP application [slots](xref:OtpSlots). Reading NDEF information from the YubiKey requires more thought.

In its initial version, the SDK does not support device notifications. This means you can’t set code to be run automatically when the YubiKey is presented to an NFC reader; you must present the YubiKey to the NFC reader and *then* execute the [ReadNdefTag()](xref:Yubico.YubiKey.Otp.OtpSession.ReadNdefTag) command.

It is possible to simulate “tap and go” functionality by polling reads. However, this presents some reliability challenges. Currently, the most reliable way to read the NDEF tag is to prompt the user to touch the NFC reader with the YubiKey and then run the command while they are in contact.

## ReadNdefTag example

The following sample code reads the configuration set in the previous [article](xref:OtpConfigureNDEF):

```C#
using (OtpSession otp = new OtpSession(yKey))
{
  NdefDataReader reader = otp.ReadNdefTag();
  object output =
    reader.Type == NdefDataType.Uri
    ? reader.ToUri()
    : reader.ToText();

  Console.WriteLine($"NDEF Output: [{output}]");
}
```

When executed against the factory NDEF configuration of a YubiKey, the output will look similar to the following:

```NDEF Output: [https://my.yubico.com/yk/#cccccctcvhvdhuvhvhubcjgucrticnhichbgcnguevgf]```
