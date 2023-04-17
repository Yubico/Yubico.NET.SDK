---
uid: OtpHowTosOverview
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

# OTP application how-to guides

The articles in this section provide examples on how to accomplish common operations with the OTP application and include additional discussions on how/when to call various methods from the respective [Yubico.YubiKey.Otp.Operations classes](xref:Yubico.YubiKey.Otp.Operations). The topics covered include:

- [How to program a slot with a Yubico OTP credential](xref:OtpProgramYubicoOTP)
- [How to program a slot with a static password](xref:OtpProgramStaticPassword)
- [How to program a slot with a challenge-response credential](xref:OtpProgramChallengeResponse)
- [How to calculate a response code for a challenge-response credential](xref:OtpCalcChallengeResponseCode)
- [How to delete a slot’s configuration](xref:OtpDeleteSlotConfig)
- [How to program a slot with an HMAC-SHA1 OATH-HOTP credential](xref:OtpProgramHOTP)
- [How to retrieve a slot’s status](xref:OtpRetrieveSlotStatus)
- [How to configure NDEF to use a slot to generate an OTP](xref:OtpConfigureNDEF)
- [How to read information from an NDEF tag](xref:OtpReadNDEF)
- [How to update slot settings](xref:OtpUpdateSlot)
- [How to swap slot configurations](xref:OtpSwapSlot)

## Working with the OTP operations classes

Before you can run the example code in the how-to articles, your application must:

1. Connect to a particular YubiKey available through the host machine via the [YubiKeyDevice class](xref:Yubico.YubiKey.YubiKeyDevice).

2. Create an instance of the [OtpSession class](xref:Yubico.YubiKey.Otp.OtpSession), which allows you to connect to the OTP application of that YubiKey.

These steps are covered in depth in the [SDK programming guide](xref:UsersManualMakingAConnection).

> [!NOTE]
> Many of the how-to guides create the OtpSession instance with `using (OtpSession otp = new OtpSession(yKey))`. This assumes that `yKey` is an IYubiKeyDevice object that represents the YubiKey.

### Additional macOS requirement: enable input monitoring

Developers working with the SDK on macOS must enable input monitoring in order to interact with a YubiKey's OTP application. The YubiKey acts as a keyboard, and the SDK needs to be able to "monitor" it in order to interact with it. If you do not enable it, the SDK will throw an exception when trying to create an instance of the OtpSession class:

![Exception thrown when trying to create OtpSession instance](../../images/input-monitoring-error.png "Exception thrown when trying to create an OtpSession instance")

To enable input monitoring, open **System Preferences** and go to **Security & Privacy**. Scroll down and click on **Input Monitoring**. Check the box next to the application that needs to monitor YubiKeys via the SDK, such as Visual Studio. You may need to click the lock icon in the bottom left corner and enter your Mac user password in order to make changes. 

![Input monitoring settings](../../images/input-monitoring.png "Input monitoring settings in macOS")

If you are building a macOS application, your users must also go through these same steps to enable input monitoring. 

Additionally, developers must add the following entitlements to their Entitlements.plist file (Entitlements.plist is created automatically when you create a new macOS application project in Visual Studio): 

- ``com.apple.security.smartcard``
- ``com.apple.security.device.usb``

### Fluent interface

The API implements a [fluent interface](https://en.wikipedia.org/wiki/Fluent_interface). This design allows you to easily and concisely chain class methods together. For example:

```C#
using (OtpSession otp = new OtpSession(yKey))
{
  otp.ConfigureYubicoOtp(Slot.ShortPress)
    .UseSerialNumberAsPublicId()
    .UsePrivateId(privateId)
    .UseKey(aesKey)
    .Execute();
}
```

The code above shows how to configure the short-press slot of a YubiKey to generate Yubico OTPs (and sets the public ID to the key's serial number, the private ID to `privateId`, and the AES secret key to `aesKey`).

### The Execute() method

You may notice in the how-to guide examples that the [Execute() method](xref:Yubico.YubiKey.Otp.Operations.OperationBase%601.Execute) is called after the other OTP operations methods.

**In order to apply changes to the YubiKey from any Yubico.YubiKey.Otp.Operations class method, you must call Execute().**

So for the previous example, this means that the Yubico OTP configuration will not be applied to the short-press slot of the YubiKey until Execute() is called.
