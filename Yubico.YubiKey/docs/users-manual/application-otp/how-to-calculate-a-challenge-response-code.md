---
uid: OtpCalcChallengeResponseCode
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

# How to send a challenge to a YubiKey and receive a response code

Once a YubiKey has been [programmed with a challenge-response credential](xref:OtpProgramChallengeResponse), you can send a challenge to that key and receive its response via a [CalculateChallengeResponse](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse) instance. It is instantiated by calling the factory method of the same name on your [OtpSession](xref:Yubico.YubiKey.Otp.OtpSession) instance. 

You can send three types of challenges to a YubiKey:

- HMAC-SHA1
- Yubico OTP
- [Time-based one-time password (TOTP)](https://www.yubico.com/resources/glossary/oath-totp/)

The challenge type must align with the type of credential that the YubiKey was programmed with, otherwise an exception will occur when calling ``CalculateChallengeResponse()``. To send an HMAC-SHA1 or TOTP challenge, the key must be programmed with an HMAC-SHA1 credential. To send a Yubico OTP challenge, the key must be programmed with a Yubico OTP credential.

When using TOTP, the YubiKey will digest the challenge with the HMAC-SHA1 credential that it was programmed with.

## Response code types

The response from a YubiKey can be received via one of three methods:

1. ``GetCode()``: returns a string object containing six (``MinOtpDigits``) to ten (``MaxOtpDigits``) 32-bit integers. A 6-digit code will be returned unless a larger number is specified when calling this method (for example, ``GetCode(8)``).
1. ``GetDataBytes()``: returns a byte array.
1. ``GetDataInt()``: returns a single integer (Int32). For HOTP challenges, the code returned will represent the same number as ``GetCode(10)``.

Any of these response types can be used for HOTP and TOTP challenges. However, ``GetDataBytes()`` is the only compatible method for Yubico OTP challenges because the response code is a Yubico OTP, which must be represented in ModHex.

## Touch

An important consideration when calculating a challenge-response code is that you must handle the possibility that the key was programmed to require the user to touch the YubiKey button in order to execute a challenge-response operation. Although your program doesn’t have to process the button-touch, you do need to alert the user to touch the button. This is handled by calling the [UseTouchNotifier()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.UseTouchNotifier(System.Action)) method, which takes an Action delegate as a parameter.

When the YubiKey requires a touch, the SDK spawns your handler as a Task. There are two important considerations:

1. Your handler executes on a different thread. This means that you should not try to access the YubiKey from that thread — the handler is strictly for alerting the user. If you have a GUI app, it must marshal itself to the proper thread.

1. Your handler is executed asynchronously. The SDK does not wait for your handler to execute, and it doesn’t care when or if it completes.

## Settings and quirks

Regardless of the challenge type, you must call ``UseYubiOtp()`` when sending a challenge with ``CalculateChallengeResponse()`` (more specifially, call ``UseYubiOtp(false)`` for HOTP and TOTP challenges or ``UseYubiOtp(true)`` for Yubico OTP challenges). There is no default setting; an exception will occur if you do not call ``UseYubiOtp()``.

## CalculateChallengeResponse() examples

Before running any of the code provided below, make sure you have already connected to a particular YubiKey on your host device via the [YubiKeyDevice](xref:Yubico.YubiKey.YubiKeyDevice) class. 

To select the first available YubiKey connected to your host, use:

```C#
IEnumerable<IYubiKeyDevice> yubiKeyList = YubiKeyDevice.FindAll();

var yubiKey = yubiKeyList.First();
```

### HMAC-SHA1 challenge-response example

In this example, we send an HOTP challenge (``hmacChal``) to the short press slot of the YubiKey with ``UseChallenge(hmacChal)`` and get the response as a string object containing eight 32-bit integers via ``.GetCode(8)``. In addition, we use ``.UseTouchNotifier()`` to tell the user to touch the YubiKey through a message printed to the console. The YubiKey's short press slot must be configured with an HOTP credential for this operation to succeed.

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
  // The challenge, hmacChal, has been set elsewhere.
  string result = otp.CalculateChallengeResponse(Slot.ShortPress)
    .UseChallenge(hmacChal)
    .UseYubiOtp(false)
    .UseTouchNotifier(() => Console.WriteLine("Touch the key."))
    .GetCode(8);
}
```

### Yubico OTP challenge-response example

In this example, we send a Yubico OTP challenge (``yOtpChal``) to the key with ``UseChallenge(yOtpChal)`` and get the response as a byte array of [ModHex](xref:OtpModhex) characters via ``GetDataBytes()``. The YubiKey's short press slot must be configured with a Yubico OTP credential for this operation to succeed.

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
  // The challenge, yOtpChal, has been set elsewhere.
  ReadOnlyMemory<byte> resp = otp.CalculateChallengeResponse(Slot.ShortPress)
    .UseChallenge(yOtpChal)
    .UseYubiOtp(true)
    .UseTouchNotifier(() => Console.WriteLine("Touch the key."))
    .GetDataBytes();
  string result = ModHex.EncodeBytes(resp.Span);
}
```

### TOTP challenge-response example

In this final example, we send a time-based challenge to the long press slot of the key with ``UseTotp()`` and get the response from the YubiKey as a single 32-bit integer via ``GetDataInt()``. The YubiKey's long press slot must be configured with an HOTP credential for this operation to succeed.

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
  int result = otp.CalculateChallengeResponse(Slot.LongPress)
    .UseTotp()
    .UseYubiOtp(false)
    .UseTouchNotifier(() => Console.WriteLine("Touch the key."))
    .GetDataInt();
}
```
