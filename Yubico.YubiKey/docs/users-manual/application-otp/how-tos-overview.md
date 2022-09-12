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
- How to program a slot with an HMAC-SHA1 OATH-HOTP credential
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

### To run any of the OTP operation methods, you must call Execute()

You may notice in the how-to guide examples that the [Execute() method](xref:Yubico.YubiKey.Otp.Operations.OperationBase%601.Execute) is called after the other OTP operations methods. For example:

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

In order to run ConfigureYubicoOtp(), UseSerialNumberAsPublicId(), UsePrivateId(), and UseKey() and apply their respective changes, you **must call Execute()**. This applies to **ALL** Yubico.YubiKey.Otp.Operations class methods. If methods are chained together as in the example, you must call Execute() at the end of the chain.
