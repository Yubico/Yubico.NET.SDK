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

# How to set, modify, remove, and use slot access codes

The YubiKey's OTP application [slots](xref:OtpSlots) can be protected by a six-byte access code. Once a slot is
configured with an access code, that slot cannot be reconfigured in any way unless the correct access code is provided
during the reconfiguration operation.

Attempting to perform a slot configuration operation without providing the correct access code will result
in the following exception:

```System.InvalidOperationException has been thrown.```
```YubiKey Operation Failed. [Warning, state of non-volatile memory is unchanged.]```

## Slot access code properties

Access codes can only be set, modified, or removed during one of the following slot configuration operations:

- [ConfigureYubicoOtp()](xref:Yubico.YubiKey.Otp.OtpSession.ConfigureYubicoOtp%28Yubico.YubiKey.Otp.Slot%29)
- [ConfigureHotp()](xref:Yubico.YubiKey.Otp.OtpSession.ConfigureHotp%28Yubico.YubiKey.Otp.Slot%29)
- [ConfigureChallengeResponse()](xref:Yubico.YubiKey.Otp.OtpSession.ConfigureChallengeResponse%28Yubico.YubiKey.Otp.Slot%29)
- [ConfigureStaticPassword()](xref:Yubico.YubiKey.Otp.OtpSession.ConfigureStaticPassword%28Yubico.YubiKey.Otp.Slot%29)
- [UpdateSlot()](xref:Yubico.YubiKey.Otp.OtpSession.UpdateSlot%28Yubico.YubiKey.Otp.Slot%29)

Therefore, the **only** way to modify a slot access code that doesn't result in the reconfiguration of the slot's current cryptographic credential is to use ``UpdateSlot()``. However, calling ``UpdateSlot()`` will revert a number of other slot settings (such as ``SetAppendCarriageReturn()``) to their default states unless otherwise specified during the operation. See [How to update slot settings](xref:OtpUpdateSlot) for more information.

> [!NOTE]
> If a slot is configured with an access code,
> calling [ConfigureNdef()](xref:Yubico.YubiKey.Otp.OtpSession.ConfigureNdef%28Yubico.YubiKey.Otp.Slot%29) will fail,
> even
> if the correct access code is provided during the operation. Similarly, if a slot is not configured with an access
> code,
> you cannot set one when calling ``ConfigureNdef()``.

Access codes must be exactly six
bytes ([MaxAccessCodeLength](xref:Yubico.YubiKey.Otp.SlotAccessCode.MaxAccessCodeLength)).
The [SlotAccessCode](xref:Yubico.YubiKey.Otp.SlotAccessCode) container class pads the code with zeros (0x00) if less
than six bytes are provided and throws an exception if more than six bytes are provided.

If a slot is configured with an access code, that code must be specified during any reconfiguration operation. In
addition, if you don’t resupply the same (or any) code as a "new" access code, an access code will not be carried
over to the new slot configuration, and the slot will no longer be protected after reconfiguration.

If a slot is protected by an access code, deleting the slot's configuration requires the use of the compatible [DeleteSlotConfiguration](xref:Yubico.YubiKey.Otp.OtpSession.DeleteSlotConfiguration%28Yubico.YubiKey.Otp.Slot%29) method.

## Example code

Before running any of the code provided below, make sure you have already connected to a particular YubiKey on your host
device via the [YubiKeyDevice](xref:Yubico.YubiKey.YubiKeyDevice) class.

To select the first available YubiKey connected to your host, use:

```C#
IEnumerable<IYubiKeyDevice> yubiKeyList = YubiKeyDevice.FindAll();

var yubiKey = yubiKeyList.First();
```

### Example: set a slot access code

To set a slot's access code when no access code is present,
call [SetNewAccessCode()](xref:Yubico.YubiKey.Otp.Operations.OperationBase%601.SetNewAccessCode%28Yubico.YubiKey.Otp.SlotAccessCode%29)
during a slot configuration operation, and provide the access code as a ``SlotAccessCode`` object. Prior to the
configuration operation, initialize the ``SlotAccessCode`` object by passing it the access code
in ``ReadOnlyMemory<byte>`` form.

In this example, we are setting a new access code while configuring the long press slot with a new HOTP credential.

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
  // example HOTP key
  ReadOnlyMemory<byte> hmacKey = new byte[ConfigureHotp.HmacKeySize] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, };

  // example slot access code
  ReadOnlyMemory<byte> accessCodeBytes = new byte[SlotAccessCode.MaxAccessCodeLength] { 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, };
  SlotAccessCode accessCode = new SlotAccessCode(accessCodeBytes);

  otp.ConfigureHotp(Slot.LongPress)
     .UseKey(hmacKey)
     .SetNewAccessCode(accessCode)
     .Execute();
}
```

### Example: modify a slot access code

To modify a slot's access code, you must provide the current access code
with [UseCurrentAccessCode()](xref:Yubico.YubiKey.Otp.Operations.OperationBase%601.UseCurrentAccessCode%28Yubico.YubiKey.Otp.SlotAccessCode%29)
followed by the new access code with ``SetNewAccessCode()`` during a slot configuration operation.

In this example, we are reconfiguring the long press slot with a new access code via the ``UpdateSlot()`` method. ``UpdateSlot()`` will not modify the slot's cryptographic configuration. However, it will revert a number of other slot settings (such as ``SetAppendCarriageReturn()``) to their default states unless otherwise specified during the operation.

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
  // Example current slot access code.
  ReadOnlyMemory<byte> currentAccessCodeBytes = new byte[SlotAccessCode.MaxAccessCodeLength] { 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, };
  SlotAccessCode currentAccessCode = new SlotAccessCode(currentAccessCodeBytes);

  // Example new slot access code.
  ReadOnlyMemory<byte> newAccessCodeBytes = new byte[SlotAccessCode.MaxAccessCodeLength] { 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, };
  SlotAccessCode newAccessCode = new SlotAccessCode(newAccessCodeBytes);

  otp.UpdateSlot(Slot.LongPress)
     .UseCurrentAccessCode(currentAccessCode)
     .SetNewAccessCode(newAccessCode)
     .Execute();
}
```

### Example: remove a slot access code

If you want to remove a slot's access code during a configuration operation, you can either:

- provide a new access code of all zeros with ``SetNewAccessCode()``, or
- skip the ``SetNewAccessCode()`` call entirely

> [!NOTE]
> A 6-byte access code of zeros (0x00) is the factory default state for each OTP slot.

Once the access code is removed, you do not need to call ``UseCurrentAccessCode()`` with subsequent configuration
operations.

In this example, we are effectively removing the access code from the long press slot by providing a new code of all zeros during the ``UpdateSlot()`` operation. ``UpdateSlot()`` will not modify the slot's cryptographic configuration. However, it will revert a number of other slot settings (such as ``SetAppendCarriageReturn()``) to their default states unless otherwise specified during the operation.

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
  // Example current access code.
  ReadOnlyMemory<byte> currentAccessCodeBytes = new byte[SlotAccessCode.MaxAccessCodeLength] { 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, };
  SlotAccessCode currentAccessCode = new SlotAccessCode(currentAccessCodeBytes);

  // New access code of all zeros.
  ReadOnlyMemory<byte> newAccessCodeBytes = new byte[SlotAccessCode.MaxAccessCodeLength] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, };
  SlotAccessCode newAccessCode = new SlotAccessCode(newAccessCodeBytes);

  otp.UpdateSlot(Slot.LongPress)
     .UseCurrentAccessCode(currentAccessCode)
     .SetNewAccessCode(newAccessCode)
     .Execute();
}
```

### Example: provide a slot access code during a configuration operation

Once a slot has been configured with an access code, you must provide that access code with ``UseCurrentAccessCode()``
when performing a configuration operation on that slot. To retain the access code, you must also
call ``SetNewAccessCode()`` and provide the same access code. If you do not call ``SetNewAccessCode()``, the access code will be removed.

> [!NOTE]
> If a slot does not have an access code, providing any 6-byte code
> with ``UseCurrentAccessCode()`` during a configuration operation will succeed.

In this example, we are reconfiguring an access code-protected long press slot with a new Yubico OTP credential. The access code is carried over to the new slot configuration by the ``SetNewAccessCode(currentAccessCode)`` call.

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
  // Example current access code.
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

### Example: deleting a slot configuration when an access code is present

To delete a slot configuration that is protected with an access code, you must
call [DeleteSlotConfiguration](xref:Yubico.YubiKey.Otp.OtpSession.DeleteSlotConfiguration%28Yubico.YubiKey.Otp.Slot%29)
and provide the current access code with ``UseCurrentAccessCode()``.

You cannot set a new access code during this
operation. The ``DeleteSlotConfiguration`` operation will still succeed if you call ``SetNewAccessCode()``, but the new access code will not be applied.

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
  // Example current access code.
  ReadOnlyMemory<byte> currentAccessCodeBytes = new byte[SlotAccessCode.MaxAccessCodeLength] { 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, };
  SlotAccessCode currentAccessCode = new SlotAccessCode(currentAccessCodeBytes);

  otp.DeleteSlotConfiguration(Slot.LongPress)
     .UseCurrentAccessCode(currentAccessCode)
     .Execute();
}
```

> [!NOTE]
> To delete a slot configuration that is not protected with an access code,
use [DeleteSlot()](xref:Yubico.YubiKey.Otp.OtpSession.DeleteSlot%28Yubico.YubiKey.Otp.Slot%29).