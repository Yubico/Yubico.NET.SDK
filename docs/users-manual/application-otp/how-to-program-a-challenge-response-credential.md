---
uid: OtpProgramChallengeResponse
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

# How to program a slot with a challenge-response credential

To program a [slot](xref:OtpSlots) with a [challenge-response](xref:OtpChallengeResponse) credential, you must use
a [ConfigureChallengeResponse](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse) instance. It is
instantiated by calling the factory method of the same name on your [OtpSession](xref:Yubico.YubiKey.Otp.OtpSession)
instance.

The challenge-response credential, unlike the other configurations, is passive. It only responds when it is queried with
a challenge via [CalculateChallengeResponse()](xref:OtpCalcChallengeResponseCode).

Slots can be programmed with one of the following types of credentials:

- [HMAC-SHA1](https://datatracker.ietf.org/doc/html/rfc2104)
- [Yubico OTP](xref:OtpYubicoOtp)

During a challenge-response operation, a slot programmed with an HMAC-SHA1 credential will digest the challenge with
that credential via the HMAC-SHA1 algorithm, producing an HMAC-SHA1 hash value, which can be received by the application 
as a byte array or 6-10 digit numeric code. If the slot was programmed with a Yubico OTP
credential, the key will encrypt the challenge with that credential via the Yubico OTP algorithm, producing a Yubico
OTP (as a byte array).

## Algorithm selection and key sizes

When programming a slot with a credential, you must call
either [UseYubiOtp()](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.UseYubiOtp)
or [UseHmacSha1()](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.UseHmacSha1) to select the algorithm
you'd like to use.

In addition, a secret key must be provided
via [UseKey()](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.UseKey%28System.ReadOnlyMemory%7BSystem.Byte%7D%29)
or generated randomly
via [GenerateKey()](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.GenerateKey%28System.Memory%7BSystem.Byte%7D%29).
The key must be [16 bytes](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.YubiOtpKeySize) in size for
Yubico OTP or [20 bytes](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.HmacSha1KeySize) for HMAC-SHA1.

> [!NOTE]
> The secret key must also be provided to the validation server for the purposes of verifying response codes sent from
> the YubiKey during challenge-response operations.

## Short challenge mode for HMAC-SHA1

An HMAC-SHA1 challenge is 64 bytes by default. The YubiKey also supports a short challenge
mode ([UseSmallChallenge()](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.UseSmallChallenge%28System.Boolean%29))
where challenges, which are sent to a YubiKey with ``CalculateChallengeResponse()``, can be configured to be less than
64 bytes. ``UseSmallChallenge()`` is included for compatibility with legacy systems whose implementations break data
sets into multiple blocks, which often results in the last element being smaller than 64 bytes (which would change the
result). Due to this truncation, it’s important to use the setting that will be expected by the consumer of the OTP
code.

> [!NOTE]
> You can use challenges smaller than 64 bytes without setting the short challenge mode by padding the end
> of the challenge with zeros. Regardless, both sides of the operation must agree on the length of the
> challenge.

## Requiring touch

When programming a slot with a Yubico OTP or HMAC-SHA1 challenge-response credential, you can also include a setting
that requires the user to touch the YubiKey before the cryptographic operation can proceed during a challenge-response
operation. Requiring touch improves security by ensuring that a user performs a physical operation.

To enable this setting, add
the [UseButton()](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.UseButton(System.Boolean)) method to
your ``ConfigureChallengeResponse()`` operation.

If a YubiKey has been configured to require a button touch, you must make sure to alert the user of this requirement
during a challenge-response operation. This can be accomplished by calling
the [UseTouchNotifier()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.UseTouchNotifier(System.Action))
method when sending a challenge to a YubiKey via ``CalculateChallengeResponse()``.

## ConfigureChallengeResponse examples

Before running any of the code provided below, make sure you have already connected to a particular YubiKey on your host
device via the [YubiKeyDevice](xref:Yubico.YubiKey.YubiKeyDevice) class.

To select the first available YubiKey connected to your host, use:

```C#
IEnumerable<IYubiKeyDevice> yubiKeyList = YubiKeyDevice.FindAll();

var yubiKey = yubiKeyList.First();
```

### HMAC-SHA1 example

The following code configures the [short press](xref:Yubico.YubiKey.Otp.Slot.ShortPress) slot with a challenge-response
credential and a predefined secret key. This configuration uses the HMAC-SHA1 algorithm and requires the user to touch
the button during a challenge-response operation.

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
  // The secret key, hmacKey, was set elsewhere.
  otp.ConfigureChallengeResponse(Slot.ShortPress)
    .UseHmacSha1()
    .UseKey(hmacKey)
    .UseButton()
    .Execute();
}
```

### Yubico OTP example

The following code configures the [long press](xref:Yubico.YubiKey.Otp.Slot.LongPress) slot with a challenge-response
credential. This configuration uses the Yubico OTP algorithm and a randomly generated secret key.

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
  //Don't forget to share the secret key with the validation server before clearing it from memory.
  Memory<byte> secretKey = new byte[ConfigureYubicoOtp.KeySize];

  otp.ConfigureChallengeResponse(Slot.LongPress)
    .UseYubiOtp()
    .GenerateKey(secretKey)
    .Execute();
}
```

## Slot reconfiguration and access codes

If a slot is protected by an access code and you wish to reconfigure it with a challenge-response credential, you must
provide that access code with ``UseCurrentAccessCode()`` during the ``ConfigureChallengeResponse()`` operation.
Otherwise, the operation will fail and throw the following exception:

```System.InvalidOperationException has been thrown. YubiKey Operation Failed. [Warning, state of non-volatile memory is unchanged.]```

For more information on slot access codes, see [How to set, reset, remove, and use slot access codes](xref:OtpSlotAccessCodes).

