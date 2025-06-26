---
uid: OtpSlotAccessCodes
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

# How to set, reset, remove, and use slot access codes

The YubiKey's OTP application [slots](xref:OtpSlots) can be protected by a six-byte access code. Once a slot is
configured with an access code, that slot cannot be reconfigured in any way unless the correct access code in provided
during the reconfiguration operation.

> [!NOTE]
> Attempting to perform an OTP application configuration operation without providing the correct access code will result
> in the following exception:
>
> ```System.InvalidOperationException has been thrown.```
> ```YubiKey Operation Failed. [Warning, state of non-volatile memory is unchanged.]```

Access codes can only be set, reset, or removed during another slot configuration operation:

- [ConfigureYubicoOtp()](xref:Yubico.YubiKey.Otp.OtpSession.ConfigureYubicoOtp%28Yubico.YubiKey.Otp.Slot%29)
- [ConfigureHotp()](xref:Yubico.YubiKey.Otp.OtpSession.ConfigureHotp%28Yubico.YubiKey.Otp.Slot%29)
- [ConfigureChallengeResponse()](xref:Yubico.YubiKey.Otp.OtpSession.ConfigureChallengeResponse%28Yubico.YubiKey.Otp.Slot%29)
- [ConfigureStaticPassword()](xref:Yubico.YubiKey.Otp.OtpSession.ConfigureStaticPassword%28Yubico.YubiKey.Otp.Slot%29)
- [UpdateSlot()](xref:Yubico.YubiKey.Otp.OtpSession.UpdateSlot%28Yubico.YubiKey.Otp.Slot%29)

For example, to configure the YubiKey's Long Press (2) slot with a _new_ HOTP credential and prevent unauthorized removal, invoke the ``ConfigureHotp()`` function and provide a slot access code to protect the configuration.
Conversely, if you would like to protect an _existing_ slot configuration such as the factory-programmed Yubico OTP (YubiOTP) on the Short Press (1) slot, invoke the ``updateSlot()`` function to set the slot access code.

> [!NOTE]
> If a slot is configured with an access code,
> calling [ConfigureNdef()](xref:Yubico.YubiKey.Otp.OtpSession.ConfigureNdef%28Yubico.YubiKey.Otp.Slot%29) will fail,
> even
> if the correct access code is provided during the operation. Similarly, if a slot is not configured with an access
> code,
> you cannot set one when calling ``ConfigureNdef()``.

## Slot access code properties

Access codes must be exactly six
bytes ([MaxAccessCodeLength](xref:Yubico.YubiKey.Otp.SlotAccessCode.MaxAccessCodeLength)).
The [SlotAccessCode](xref:Yubico.YubiKey.Otp.SlotAccessCode) container class pads the code with zeros (0x00) if less
than six bytes are provided and throws an exception if more than six bytes are provided.

If a slot is configured with an access code, that code must be specified during any reconfiguration operation. In
addition, if you don’t also resupply the same (or any) code as a "new" access code, an access code will not be carried
over to the new slot configuration, and the slot will no longer be protected after reconfiguration.

## Example code

Before running any of the code provided below, make sure you have already connected to a particular YubiKey on your host
device via the [YubiKeyDevice](xref:Yubico.YubiKey.YubiKeyDevice) class.

To select the first available YubiKey connected to your host, use:

```C#
IEnumerable<IYubiKeyDevice> yubiKeyList = YubiKeyDevice.FindAll();

var yubiKey = yubiKeyList.First();
```

## Exampe: set a slot access code

To set a slot's access code during slot programming when no access code is present,
call [SetNewAccessCode()](xref:Yubico.YubiKey.Otp.Operations.OperationBase%601.SetNewAccessCode%28Yubico.YubiKey.Otp.SlotAccessCode%29)
during a slot configuration operation, and provide the access code as a ``SlotAccessCode`` object. Prior to the
configuration operation, initialize the ``SlotAccessCode`` object by passing it the access code
in ``ReadOnlyMemory<byte>`` form.

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
  ReadOnlyMemory<byte> hmacKey = new byte[ConfigureHotp.HmacKeySize] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, };

  ReadOnlyMemory<byte> accessCodeBytes = new byte[SlotAccessCode.MaxAccessCodeLength] { 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, };
  SlotAccessCode accessCode = new SlotAccessCode(accessCodeBytes);

  otp.ConfigureHotp(Slot.LongPress)
     .UseKey(hmacKey)
     .SetNewAccessCode(accessCode)
     .Execute();
}
```

## Example: reset a slot access code

To reset a slot's access code during slot programming, you must provide the current access code
with [UseCurrentAccessCode()](xref:Yubico.YubiKey.Otp.Operations.OperationBase%601.UseCurrentAccessCode%28Yubico.YubiKey.Otp.SlotAccessCode%29)
followed by the new access code with ``SetNewAccessCode()``:

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
  ReadOnlyMemory<byte> hmacKey = new byte[ConfigureHotp.HmacKeySize] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, };

  ReadOnlyMemory<byte> currentAccessCodeBytes = new byte[SlotAccessCode.MaxAccessCodeLength] { 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, };
  SlotAccessCode currentAccessCode = new SlotAccessCode(currentAccessCodeBytes);

  ReadOnlyMemory<byte> newAccessCodeBytes = new byte[SlotAccessCode.MaxAccessCodeLength] { 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, };
  SlotAccessCode newAccessCode = new SlotAccessCode(newAccessCodeBytes);

  otp.ConfigureHotp(Slot.LongPress)
     .UseKey(hmacKey)
     .UseCurrentAccessCode(currentAccessCode)
     .SetNewAccessCode(newAccessCode)
     .Execute();
}
```

## Example: remove a slot access code

If you want to remove a slot's access code, you must either:

- provide a new access code of all zeros, or
- only call ``UseCurrentAccessCode()`` during the reconfiguration operation. The slot's access code will be removed if a
  code is not provided via ``SetNewAccessCode()`` after calling ``UseCurrentAccessCode()``.

> [!NOTE]
> A 6-byte access code of zeros (0x00) is considered no access code. The factory default state of the access code for
> each OTP slot is all zeros.

Once the access code is removed, you do not need to call ``UseCurrentAccessCode()`` with subsequent configuration
operations.

> [!NOTE]
> Technically, if a slot does not have an access code, you could provide any 6-byte code
> with ``UseCurrentAccessCode()``, and the operation would succeed.

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
  ReadOnlyMemory<byte> hmacKey = new byte[ConfigureHotp.HmacKeySize] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, };

  ReadOnlyMemory<byte> currentAccessCodeBytes = new byte[SlotAccessCode.MaxAccessCodeLength] { 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, };
  SlotAccessCode currentAccessCode = new SlotAccessCode(currentAccessCodeBytes);

  // New access code of all zeros.
  ReadOnlyMemory<byte> newAccessCodeBytes = new byte[SlotAccessCode.MaxAccessCodeLength] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, };
  SlotAccessCode newAccessCode = new SlotAccessCode(newAccessCodeBytes);

  otp.ConfigureHotp(Slot.LongPress)
     .UseKey(hmacKey)
     .UseCurrentAccessCode(currentAccessCode)
     .SetNewAccessCode(newAccessCode)
     .Execute();
}
```

## Example: provide a slot access code during a configuration operation

Once a slot has been configured with an access code, you must provide that access code with ``UseCurrentAccessCode()``
when performing a configuration operation on that slot. To retain the access code, you must also
call ``SetNewAccessCode()``. If you do not call ``SetNewAccessCode()``, the access code will be removed.

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
  ReadOnlyMemory<byte> currentAccessCodeBytes = new byte[SlotAccessCode.MaxAccessCodeLength] { 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, };
  SlotAccessCode currentAccessCode = new SlotAccessCode(currentAccessCodeBytes);

  Memory<byte> privateId = new byte[ConfigureYubicoOtp.PrivateIdentifierSize];
  Memory<byte> aesKey = new byte[ConfigureYubicoOtp.KeySize];

  otp.ConfigureYubicoOtp(Slot.LongPress)
     .UseCurrentAccessCode(currentAccessCode)
     .SetNewAccessCode(currentAccessCode)
     .UseSerialNumberAsPublicId()
     .GeneratePrivateId(privateId)
     .GenerateKey(aesKey)
     .Execute();
}
```

In this example, the slot is now (re)configured with a Yubico OTP credential and is still protected by the same access
code (``currentAccessCode``).

## Deleting a slot configuration when an access code is present

To delete a slot configuration that is protected with an access code, you must
call [DeleteSlotConfiguration](xref:Yubico.YubiKey.Otp.OtpSession.DeleteSlotConfiguration%28Yubico.YubiKey.Otp.Slot%29)
and provide the current access code with ``UseCurrentAccessCode()``. You cannot set a new access code during this
operation--calling ``SetNewAccessCode()`` will succeed, but the operation will not be applied.

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

To delete a slot configuration that is not protected with an access code,
use [DeleteSlot()](xref:Yubico.YubiKey.Otp.OtpSession.DeleteSlot%28Yubico.YubiKey.Otp.Slot%29). 
