---
uid: OtpProgramYubicoOTP
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

# How to program a slot with a Yubico OTP credential

To program a [slot](xref:OtpSlots) with a [Yubico OTP](xref:OtpYubicoOtp) credential, you will use
a [ConfigureYubicoOtp](xref:Yubico.YubiKey.Otp.Operations.ConfigureYubicoOtp) instance. It is instantiated by calling
the factory method of the same name on your [OtpSession](xref:Yubico.YubiKey.Otp.OtpSession) instance.

First, a clarification of terms is needed. “Yubico OTP” is both an OTP credential type and
a [challenge-response](xref:OtpChallengeResponse) algorithm. In this context, we are referring to the credential type. A
Yubico OTP credential is touch-activated. When you touch the YubiKey, it will emit a binary challenge
using [ModHex](xref:OtpModhex) characters.

A Yubico OTP credential contains the following three parts, which must be set during instantiation:

* Public ID

  The public ID is a prefix that is prepended to the actual challenge; it is not used to generate the challenge. The
  serial number of the YubiKey is often used to generate this ID.

* Private ID

  The private ID is a six-byte value that is used as part of the algorithm to create a challenge and as a way to
  validate identity.

* Key

  The key is a 16-byte AES key that is used as the primary secret for the credential.

## ConfigureYubicoOtp example

You can configure the [ShortPress](xref:Yubico.YubiKey.Otp.Slot.ShortPress) slot of your YubiKey with a Yubico OTP
credential as follows:

```C#
using (OtpSession otp = new OtpSession(yKey))
{
  // privateId and aesKey are Memory<byte> references.
  otp.ConfigureYubicoOtp(Slot.ShortPress)
    .UseSerialNumberAsPublicId()
    .UsePrivateId(privateId)
    .UseKey(aesKey)
    .Execute();
}
```

In this example, we’re configuring a Yubico OTP credential using the serial number of the YubiKey to generate the public
ID and supplying an existing private ID and AES key.

You can also generate a new private ID and AES key to use instead:

```C#
using (OtpSession otp = new OtpSession(yKey))
{
  Memory<byte> privateId = new byte[ConfigureYubicoOtp.PrivateIdentifierSize];
  Memory<byte> aesKey = new byte[ConfigureYubicoOtp.KeySize];

  otp.ConfigureYubicoOtp(Slot.ShortPress)
    .UseSerialNumberAsPublicId()
    .GeneratePrivateId(privateId)
    .GenerateKey(aesKey)
    .Execute();

  // Do whatever is needed with privateId and aesKey, and clear them.
}
```

The API does not own the object where secrets are stored. Because of this, you must still provide the place to put the
generated information. Once you have done what is needed with the data, you should clear the memory where it is located.

## Slot reconfiguration and access codes

If a slot is protected by an access code and you wish to reconfigure it with a Yubico OTP credential, you must provide
that access code with ``UseCurrentAccessCode()`` during the ``ConfigureYubicoOtp()`` operation. Otherwise, the operation
will fail and throw the following exception:

```System.InvalidOperationException has been thrown. YubiKey Operation Failed. [Warning, state of non-volatile memory is unchanged.]```

For more information on slot access codes, please
see [How to set, reset, remove, and use slot access codes](xref:OtpSlotAccessCodes).