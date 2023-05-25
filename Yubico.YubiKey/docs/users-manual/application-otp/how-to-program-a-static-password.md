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

The configuration properties of the static password you wish to set are specified by calling methods on your ```ConfigureStaticPassword``` instance. Each of those methods return a ```this``` reference back to the ```ConfigureStaticPassword``` instance. This allows you to chain together the configuration in a flexible and simple way, regardless of the combination of options you choose.

## ConfigureStaticPassword() properties

``ConfigureStaticPassword()`` allows you to either: 

- provide a specific static password with [SetPassword()](xref:Yubico.YubiKey.Otp.Operations.ConfigureStaticPassword.SetPassword%28System.ReadOnlyMemory%7BSystem.Char%7D%29), or 
- randomly generate a static password with [GeneratePassword()](xref:Yubico.YubiKey.Otp.Operations.ConfigureStaticPassword.GeneratePassword%28System.Memory%7BSystem.Char%7D%29).

Both options require you to specify a keyboard layout by calling [WithKeyboard()](xref:Yubico.YubiKey.Otp.Operations.ConfigureStaticPassword.WithKeyboard%28Yubico.Core.Devices.Hid.KeyboardLayout%29). If you do not call ``WithKeyboard()``, an exception will be thrown.

Static password characters are stored as [HID usage IDs](xref:OtpHID) on the YubiKey, and these usage IDs are communicated to a host device during an authentication attempt. Because some characters do not use the same HID usage ID across all keyboard layouts, the YubiKey needs to know which keyboard layout a user's host device is likely to use so that it can store the correct usage IDs.

In addition to traditional keyboard layouts, such as German and US English, the [KeyboardLayout](xref:Yubico.Core.Devices.Hid.KeyboardLayout) class also includes [ModHex](xref:OtpModhex). For generated static passwords, if ModHex is selected as the keyboard layout, the generated password will only be composed of ModHex characters, which have the same HID usage IDs across all latin alphabet keyboard layouts. Therefore, in cases where the YubiKey will be used with host devices that implement multiple or unknown keyboard layouts, ModHex provides a way to ensure correct interpretation of the static password by all hosts.

> [!NOTE]
> Technically, ModHex can be selected as the keyboard layout when providing a password with ``SetPassword()``, but it essentially acts as a check. If your provided password contains any characters that aren't ModHex, an exception will be thrown.

Importantly, the ``SetPassword()`` and ``GeneratePassword()`` methods take a ```Memory<char>``` reference (mutable) instead of a ```string``` (immutable in .NET). Because you should clear out sensitive data afterwards, a mutable (i.e. changeable) collection is used.

Static passwords must be 1 to 38 characters in length (the [MaxPasswordLength](xref:Yubico.YubiKey.Otp.Operations.ConfigureStaticPassword.MaxPasswordLength)). An exception will be thrown if the length of the provided or generated password is outside of this range. 

## ConfigureStaticPassword() examples

Before running any of the code provided below, make sure you have already connected to a particular YubiKey on your host device via the [YubiKeyDevice](xref:Yubico.YubiKey.YubiKeyDevice) class. 

To select the first available YubiKey connected to your host, use:

```C#
IEnumerable<IYubiKeyDevice> yubiKeyList = YubiKeyDevice.FindAll();

var yubiKey = yubiKeyList.First();
```

### Using SetPassword()

The following example code sets a specific static password ("You'll never guess this!") on the [long-press](xref:Yubico.YubiKey.Otp.Slot.LongPress) slot on a YubiKey (with the US English keyboard layout) and adds a carriage return to the end of the password:

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
  otp.ConfigureStaticPassword(Slot.LongPress)
    .WithKeyboard(Yubico.Core.Devices.Hid.KeyboardLayout.en_US)
    .AppendCarriageReturn()
    .SetPassword("You'll never guess this!".ToCharArray())
    .Execute();
}
```

Because each of these calls returns a reference to the ```ConfigureStaticPassword``` instance, you can break up the chain if you need to. For example:

```C#
bool addCR = true;

using (OtpSession otp = new OtpSession(yubiKey))
{
  ConfigureStaticPassword operation = otp.ConfigureStaticPassword(Slot.LongPress)
    .WithKeyboard(Yubico.Core.Devices.Hid.KeyboardLayout.en_US);
  if (addCR)
  {
    operation = operation.AppendCarriageReturn();
  }
  operation.SetPassword("You'll never guess this!".ToCharArray())
    .Execute();
}
```

### Using GeneratePassword()

The following example code generates a 38-character static password (containing only ModHex characters) to use on the long-press slot on a YubiKey:

```C#
Memory<char> password = new char[ConfigureStaticPassword.MaxPasswordLength];

using (OtpSession otp = new OtpSession(yubiKey))
{
  otp.ConfigureStaticPassword(Slot.LongPress)
    .WithKeyboard(Yubico.Core.Devices.Hid.KeyboardLayout.en_ModHex)
    .GeneratePassword(password)
    .Execute();
}
```

Because ``GeneratePassword()`` stores the generated password in the ``password`` char array, make sure to clear the data from ``password`` once it is no longer needed. 

## Additional settings

The following additional (optional) settings can be applied during configuration:

- [AppendCarriageReturn()](xref:Yubico.YubiKey.Otp.Operations.ConfigureStaticPassword.AppendCarriageReturn%28System.Boolean%29)
- [AppendDelayToFixed()](xref:Yubico.YubiKey.Otp.Operations.ConfigureStaticPassword.AppendDelayToFixed%28System.Boolean%29)
- [SendTabFirst()](xref:Yubico.YubiKey.Otp.Operations.ConfigureStaticPassword.SendTabFirst%28System.Boolean%29)
- [SetAllowUpdate()](xref:Yubico.YubiKey.Otp.Operations.ConfigureStaticPassword.SetAllowUpdate%28System.Boolean%29)
- [Use10msPacing()](xref:Yubico.YubiKey.Otp.Operations.ConfigureStaticPassword.Use10msPacing%28System.Boolean%29)
- [Use20msPacing()](xref:Yubico.YubiKey.Otp.Operations.ConfigureStaticPassword.Use20msPacing%28System.Boolean%29)
- [UseFastTrigger()](xref:Yubico.YubiKey.Otp.Operations.ConfigureStaticPassword.UseFastTrigger%28System.Boolean%29)
- [UseNumericKeypad()](xref:Yubico.YubiKey.Otp.Operations.ConfigureStaticPassword.UseNumericKeypad%28System.Boolean%29)

The static password does not have both a fixed part and a variable part like Yubico OTPs do, but you can still use ``AppendDelayToFixed()`` without error. [AppendTabToFixed()](xref:Yubico.YubiKey.Otp.Operations.ConfigureStaticPassword.AppendTabToFixed%28System.Boolean%29) will succeed, but instead of sending a tab before the static password, it will break up or alter the static password. Use ``SendTabFirst()`` instead.

These settings can also be toggled after static password configuration by calling [UpdateSlot()](xref:OtpUpdateSlot). 

> [!NOTE] 
> If you call ``SetAllowUpdate(false)`` during the inital configuration, you will not be able to update these settings with ``UpdateSlot()`` (the SDK will throw an exception). This can only be undone by reconfiguring the slot with ``ConfigureStaticPassword()`` (or another OTP application configuration). It is not necessary to call ``SetAllowUpdate(true)`` during configuration because updates are allowed by default. 

Most settings have default parameters, but they also allow you to specify the value, as shown with ``AppendCarriageReturn()`` in the example below:

```C#
bool addCR = true;

using (OtpSession otp = new OtpSession(yubiKey))
{
  otp.ConfigureStaticPassword(Slot.LongPress)
    .WithKeyboard(Yubico.Core.Devices.Hid.KeyboardLayout.en_US)
    .AppendCarriageReturn(addCR)
    .SetPassword("You'll never guess this!".ToCharArray())
    .Execute();
}
```

### Manual updates

If a slot has already been configured with a generated static password, that password can be updated to a new randomly generated password by pressing and holding the contact of the YubiKey for 8-15 seconds. When the contact is released, the indicator light will flash. Touching the contact again confirms the change, and the new static password is yielded.

To enable this feature, [AllowManualUpdate()](xref:Yubico.YubiKey.Otp.Operations.ConfigureStaticPassword.AllowManualUpdate%28System.Boolean%29) must be set to ``true``. However, the [static ticket flag](xref:Yubico.YubiKey.Otp.ConfigurationFlags.StaticTicket) must be set or an exception will be thrown when calling ``AllowManualUpdate()``. At this time, the SDK does not provide an operations class for toggling the static ticket flag. [Configuration flags](xref:Yubico.YubiKey.Otp.ConfigurationFlags), including the static ticket flag, can only be manipulated via the lower level [ConfigureSlotCommand class](xref:Yubico.YubiKey.Otp.Commands.ConfigureSlotCommand). 

For more information on working with command classes, see the [SDK programming guide](xref:UsersManualCommands).

## Slot reconfiguration and access codes

If a slot is protected by an access code and you wish to reconfigure it with a static password, you must provide that access code with ``UseCurrentAccessCode()`` during the ``ConfigureStaticPassword()`` operation. Otherwise, the operation will fail and throw the following exception:

```System.InvalidOperationException has been thrown. YubiKey Operation Failed. [Warning, state of non-volatile memory is unchanged.]```

For more information on slot access codes, please see [How to set, reset, remove, and use slot access codes](xref:OtpSlotAccessCodes).