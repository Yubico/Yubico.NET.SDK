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

Once a YubiKey's [slot](xref:OtpSlots) has
been [programmed with a challenge-response credential](xref:OtpProgramChallengeResponse), you can send
a [challenge](xref:OtpChallengeResponse) to that key and receive its response via
a [CalculateChallengeResponse](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse) instance. It is
instantiated by calling the factory method of the same name on your [OtpSession](xref:Yubico.YubiKey.Otp.OtpSession)
instance.

You can send two types of challenges to a YubiKey:

- an application-specified byte array (Yubico OTP and HMAC-SHA1)
- the current Unix time (HMAC-SHA1 configurations only)

The challenge type and size must align with the type of credential that the YubiKey was programmed with, otherwise an exception
will be thrown.

For HMAC-SHA1 challenge-response, the YubiKey will digest the challenge with the HMAC-SHA1 credential that it
was programmed with. The resulting hash value can then be compared to the hash value produced by the validation server via the HMAC-SHA1 algorithm. 
For Yubico OTP challenge-response, the YubiKey will encrypt the challenge using its Yubico OTP
credential, producing a Yubico OTP (as a byte array). This OTP can then be decrypted by the validation server with the credential's secret
key.

## Response types

The response from a YubiKey can be received via one of three methods:

1. [GetCode()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.GetCode%28System.Int32%29): returns a
   string object containing [six](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.MinOtpDigits)
   to [ten](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.MaxOtpDigits) 32-bit integers. A 6-digit code
   will be returned by default unless a larger number is specified when calling this method (for
   example, ``GetCode(8)``).
1. [GetDataBytes()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.GetDataBytes): returns a byte array.
1. [GetDataInt()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.GetDataInt): returns a single 10-digit, 32-bit
   integer. The integer returned will represent the same code as ``GetCode(10)``.

Any of these response methods can be used for HMAC-SHA1 challenges. However, ``GetDataBytes()`` is the only
compatible method for Yubico OTP challenges.

## Touch

An important consideration when calculating a challenge-response code is that you must handle the possibility that the
key was programmed to require the user to touch the YubiKey button in order to execute a challenge-response operation.
Although your program doesn’t have to process the button-touch, you do need to alert the user to touch the button. This
is handled by calling
the [UseTouchNotifier()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.UseTouchNotifier(System.Action))
method, which takes an Action delegate as a parameter.

When the YubiKey requires a touch, the SDK spawns your handler as a Task. There are two important considerations:

1. Your handler executes on a different thread. This means that you should not try to access the YubiKey from that
   thread — the handler is strictly for alerting the user. If you have a GUI app, it must marshal itself to the proper
   thread.

1. Your handler is executed asynchronously. The SDK does not wait for your handler to execute, and it doesn’t care when
   or if it completes.

## Settings and quirks

Regardless of the challenge type, you must
call [UseYubiOtp()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.UseYubiOtp%28System.Boolean%29) when
sending a challenge with ``CalculateChallengeResponse()`` Specifically, call ``UseYubiOtp(false)`` for HMAC-SHA1 challenges 
or ``UseYubiOtp(true)`` for Yubico OTP challenges. There is no default setting; an exception will occur
if you do not call ``UseYubiOtp()``.

For Yubico OTP challenge-response, the challenge must be 6 bytes
long ([YubicoOtpChallengeSize](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.YubicoOtpChallengeSize)).
For HMAC SHA-1 challenge-response, the application-specified challenge must be 64 bytes
long ([MaxHmacChallengeSize](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.MaxHmacChallengeSize)) unless
the YubiKey was previously configured
with [UseSmallChallenge()](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.UseSmallChallenge%28System.Boolean%29).

Additionally, for time-based HMAC-SHA1 challenges sent with [UseTotp()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.UseTotp), you can set the time period that the response is valid for
via [WithPeriod()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.WithPeriod%28System.Int32%29) (the
default is 30 seconds).

## CalculateChallengeResponse() examples

Before running any of the code provided below, make sure you have already connected to a particular YubiKey on your host
device via the [YubiKeyDevice](xref:Yubico.YubiKey.YubiKeyDevice) class.

To select the first available YubiKey connected to your host, use:

```C#
IEnumerable<IYubiKeyDevice> yubiKeyList = YubiKeyDevice.FindAll();

var yubiKey = yubiKeyList.First();
```

### HMAC-SHA1 challenge-response example

In this example, we send an HMAC-SHA1 challenge (``hmacChal``) to the short press slot of the YubiKey
with [UseChallenge()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.UseChallenge%28System.Byte%5B%5D%29)
and get the response as a string object containing eight 32-bit integers via ``GetCode()``.

In addition, we
use [UseTouchNotifier()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.UseTouchNotifier%28System.Action%29)
to tell the user to touch the YubiKey through a message printed to the console. The YubiKey's short press slot must be
configured with an HMAC-SHA1 credential for this operation to succeed.

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
  // The challenge, hmacChal, was set elsewhere.
  string result = otp.CalculateChallengeResponse(Slot.ShortPress)
    .UseChallenge(hmacChal)
    .UseYubiOtp(false)
    .UseTouchNotifier(() => Console.WriteLine("Touch the key."))
    .GetCode(8);
}
```

### Yubico OTP challenge-response example

In this example, we send a Yubico OTP challenge (``yOtpChal``) to the key with ``UseChallenge()`` and get the response
as a byte array via ``GetDataBytes()``. This byte array is then converted to a string of [ModHex](xref:OtpModhex)
characters
via [ModHex.EncodeBytes()](xref:Yubico.Core.Buffers.ModHex.EncodeBytes%28System.ReadOnlySpan%7BSystem.Byte%7D%2CSystem.Span%7BSystem.Char%7D%29).

The YubiKey's short press slot must be configured with a Yubico OTP credential for this operation to succeed.

```C#
using (OtpSession otp = new OtpSession(yubiKey))
{
  // The challenge, yOtpChal, was set elsewhere.
  ReadOnlyMemory<byte> resp = otp.CalculateChallengeResponse(Slot.ShortPress)
    .UseChallenge(yOtpChal)
    .UseYubiOtp(true)
    .UseTouchNotifier(() => Console.WriteLine("Touch the key."))
    .GetDataBytes();
  string result = ModHex.EncodeBytes(resp.Span);
}
```

### Time-based HMAC-SHA1 challenge-response example

In this final example, we send a time-based challenge to the long press slot of the key
with [UseTotp()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.UseTotp) and get the response from the
YubiKey as a single 32-bit integer via ``GetDataInt()``.

The YubiKey's long press slot must be configured with an HMAC-SHA1 credential for this operation to succeed.

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
