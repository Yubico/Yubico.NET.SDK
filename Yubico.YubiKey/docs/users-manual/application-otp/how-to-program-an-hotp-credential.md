---
uid: OtpProgramHOTP
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

# How to program a slot with an OATH HOTP credential

To configure a [slot](xref:OtpSlots) with an [OATH HOTP credential](xref:OtpHotp), you will use a [ConfigureHotp](xref:Yubico.YubiKey.Otp.Operations.ConfigureHotp) instance. It is instantiated by calling the factory method of the same name ([ConfigureHotp()](xref:Yubico.YubiKey.Otp.OtpSession.ConfigureHotp%28Yubico.YubiKey.Otp.Slot%29)) on your [OtpSession](xref:Yubico.YubiKey.Otp.OtpSession) instance.

The properties of the HOTP credential you wish to set are specified by calling their respective methods on your ``ConfigureHotp`` instance. 

## ConfigureHotp example

Before running any of the code provided below, make sure you have already connected to a particular YubiKey on your host device via the [YubiKeyDevice](xref:Yubico.YubiKey.YubiKeyDevice) class. 

To select the first available YubiKey connected to your host, use:

```C#
IEnumerable<IYubiKeyDevice> yubiKeyList = YubiKeyDevice.FindAll();

var yubiKey = yubiKeyList.First();
```

### Configure a slot with a provided secret key or a randomly generated key

When calling ``ConfigureHotp()``, you must either provide a secret key for the credential with [UseKey()](xref:Yubico.YubiKey.Otp.Operations.ConfigureHotp.UseKey%28System.ReadOnlyMemory%7BSystem.Byte%7D%29) or generate one randomly with [GenerateKey()](xref:Yubico.YubiKey.Otp.Operations.ConfigureHotp.GenerateKey%28System.Memory%7BSystem.Byte%7D%29). The keys must be equal to the length of [HmacKeySize](xref:Yubico.YubiKey.Otp.Operations.ConfigureHotp.HmacKeySize) (20 bytes).  

To configure the [LongPress](xref:Yubico.YubiKey.Otp.Slot.LongPress) slot with an HOTP using a provided secret key (which contains all 0s in this example), use:

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
    ReadOnlyMemory<byte> hmacKey = new byte[ConfigureHotp.HmacKeySize] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, };

    otp.ConfigureHotp(Slot.LongPress)
        .UseKey(hmacKey)
        .Execute();
    }    
```

To configure the ``LongPress`` slot with an HOTP using a randomly generated secret key, use:

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
    Memory<byte> hmacKey = new byte[ConfigureHotp.HmacKeySize];

    otp.ConfigureHotp(Slot.LongPress)
        .GenerateKey(hmacKey)
        .Execute();
}
```

The API does not own the object where secrets are stored. Therefore, you must still provide the place to put the generated information (which is ``hmacKey`` in this example). Once you have done what is needed with the data, clear the memory where it is located.

### Set the initial moving factor and/or generate 8-digit HOTPs

You may optionally set the initial moving factor (the counter) with [UseInitialMovingFactor()](xref:Yubico.YubiKey.Otp.Operations.ConfigureHotp.UseInitialMovingFactor%28System.Int32%29). If you do not call this method, the counter will be set to 0 by default. 

> [!NOTE]  
> ``UseInitialMovingFactor()`` must be given an integer between 0 and 0xffff0 (1,048,560) that is divisible by 0x10 (16), otherwise an exception will be thrown. 

``ConfigureHotp()`` will configure a slot to generate 6-digit HOTPs by default. If you would like to generate 8-digit HOTPs, you must call [Use8Digits()](xref:Yubico.YubiKey.Otp.Operations.ConfigureHotp.Use8Digits%28System.Boolean%29) during configuration. 

To set the initial moving factor to 16 and generate 8-digit HOTPs (with a randomly generated secret key), run the following:

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
    Memory<byte> hmacKey = new byte[ConfigureHotp.HmacKeySize];

    otp.ConfigureHotp(Slot.LongPress)
        .UseInitialMovingFactor(16)
        .GenerateKey(hmacKey)
        .Use8Digits()
        .Execute();
}
```

## Additional settings

The following additional (optional) settings can be applied during configuration:

- [AppendCarriageReturn()](xref:Yubico.YubiKey.Otp.Operations.ConfigureHotp.AppendCarriageReturn%28System.Boolean%29)
- [AppendDelayToFixed()](xref:Yubico.YubiKey.Otp.Operations.ConfigureHotp.AppendDelayToFixed%28System.Boolean%29)
- [AppendDelayToOtp()](xref:Yubico.YubiKey.Otp.Operations.ConfigureHotp.AppendDelayToOtp%28System.Boolean%29)
- [AppendTabToFixed()](xref:Yubico.YubiKey.Otp.Operations.ConfigureHotp.AppendTabToFixed%28System.Boolean%29)
- [SendReferenceString()](xref:Yubico.YubiKey.Otp.Operations.ConfigureHotp.SendReferenceString%28System.Boolean%29)
- [SendTabFirst()](xref:Yubico.YubiKey.Otp.Operations.ConfigureHotp.SendTabFirst%28System.Boolean%29)
- [SetAllowUpdate()](xref:Yubico.YubiKey.Otp.Operations.ConfigureHotp.SetAllowUpdate%28System.Boolean%29)
- [Use10msPacing()](xref:Yubico.YubiKey.Otp.Operations.ConfigureHotp.Use10msPacing%28System.Boolean%29)
- [Use20msPacing()](xref:Yubico.YubiKey.Otp.Operations.ConfigureHotp.Use20msPacing%28System.Boolean%29)
- [UseFastTrigger()](xref:Yubico.YubiKey.Otp.Operations.ConfigureHotp.UseFastTrigger%28System.Boolean%29)
- [UseNumericKeypad()](xref:Yubico.YubiKey.Otp.Operations.ConfigureHotp.UseNumericKeypad%28System.Boolean%29)

The OATH HOTP does not have a fixed part, but you can still use ``AppendDelayToFixed()`` and ``AppendTabToFixed()``. These will simply add a delay or send a tab prior to the HOTP, respectively. 

With the exception of ``SendReferenceString()``, these settings can also be toggled after HOTP configuration by calling [UpdateSlot()](xref:OtpUpdateSlot). 

> [!NOTE] 
> If you call ``SetAllowUpdate(false)`` during the inital configuration, you will not be able to update these settings with ``UpdateSlot()`` (the SDK will throw an exception). This can only be undone by reconfiguring the slot with ``ConfigureHotp()``. It is not necessary to call ``SetAllowUpdate(true)`` during configuration because updates are allowed by default. 
