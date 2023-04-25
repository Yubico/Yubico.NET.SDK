---
uid: OtpProgramStaticPassword
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

# How to program a slot with a static password

To configure a [slot](xref:OtpSlots) to emit a [static password](xref:OtpStaticPassword), you will use a [ConfigureStaticPassword](xref:Yubico.YubiKey.Otp.Operations.ConfigureStaticPassword) instance. It is instantiated by calling the factory method of the same name ([ConfigureStaticPassword()](xref:Yubico.YubiKey.Otp.OtpSession.ConfigureStaticPassword(Yubico.YubiKey.Otp.Slot))) on your [OtpSession](xref:Yubico.YubiKey.Otp.OtpSession) instance.

The properties of the static password you wish to set are specified by calling methods on your ```ConfigureStaticPassword``` instance. Each of those methods return a ```this``` reference back to the ```ConfigureStaticPassword``` instance. This allows you to chain together the configuration in a flexible and simple way, regardless of the combination of options you choose.

## ConfigureStaticPassword example

The following example code will set a static password on the [short-press](xref:Yubico.YubiKey.Otp.Slot.ShortPress) slot on a YubiKey. We will assume that you already have an [IYubiKeyDevice](xref:Yubico.YubiKey.IYubiKeyDevice) reference.

```C#
using (OtpSession otp = new OtpSession(yKey))
{
  otp.ConfigureStaticPassword(Slot.ShortPress)
    .WithKeyboard(KeyboardLayout.en_US)
    .AppendCarriageReturn()
    .SetPassword("You'll never guess this!".ToCharArray())
    .Execute();
}
```

Because each of these calls returns a reference to the ```ConfigureStaticPassword``` instance, you can break up the chain if you need to. For example:

```C#
using (OtpSession otp = new OtpSession(yKey))
{
  ConfigureStaticPassword operation = otp.ConfigureStaticPassword(Slot.ShortPress)
    .WithKeyboard(KeyboardLayout.en_US);
  if (addCR)
  {
    operation = operation.AppendCarriageReturn();
  }
  operation.SetPassword("You'll never guess this!".ToCharArray())
    .Execute();
}
```

Most operations have default parameters, but they also allow you to specify the value like so:

```C#
using (OtpSession otp = new OtpSession(yKey))
{
  otp.ConfigureStaticPassword(Slot.ShortPress)
    .WithKeyboard(KeyboardLayout.en_US)
    .AppendCarriageReturn(addCR)
    .SetPassword("You'll never guess this!".ToCharArray())
    .Execute();
}
```

> [!NOTE]
> The ```SetPassword``` method takes a ```Memory<char>``` reference (mutable) instead of a ```string``` (immutable in .NET). Because you should clear out sensitive data afterwards, a mutable collection is used.

## Slot reconfiguration and access codes

If a slot is protected by an access code and you wish to reconfigure it with a static password, you must provide that access code with ``UseCurrentAccessCode()`` during the ``ConfigureStaticPassword()`` operation. Otherwise, the operation will fail and throw the following exception:

```System.InvalidOperationException has been thrown. YubiKey Operation Failed. [Warning, state of non-volatile memory is unchanged.]```

For more information on slot access codes, please see [How to set, reset, remove, and use slot access codes](xref:OtpSlotAccessCodes).