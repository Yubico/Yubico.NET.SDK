---
uid: OtpCalcChallengeResponseCode
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

# How to calculate a response code for a challenge-response credential

To calculate a response code for a [challenge-response](xref:OtpChallengeResponse) credential, you must use a ```CalculateChallengeResponse``` instance. It is instantiated by calling the method of the same name on your [OtpSession](xref:Yubico.YubiKey.Otp.OtpSession) instance.

As with [programming a challenge-response credential](xref:OtpProgramChallengeResponse), you can calculate an OTP for both the Yubico OTP and the HMAC-SHA1 algorithms. In this case, HMAC-SHA1 is the default algorithm; to use Yubico OTP, you must specify ```UseYubiOtp()``` in your code.

## Touch

An important consideration when calculating a challenge-response OTP is that you must handle the possibility that the key was programmed to require the user to touch the YubiKey button to execute a challenge-response operation. Although your program doesn’t have to process the button-touch, you do need to alert the user to touch the button. This is handled by calling the ```UseTouchNotifier()``` method, which takes an Action delete as a parameter.

When the YubiKey requires a touch, the SDK spawns your handler as a Task. There are two important considerations:

1. Your handler executes on a different thread. This means that you should not try to access the YubiKey from that thread — the handler is strictly for alerting the user. If you have a GUI app, it must marshal itself to the proper thread.

1. Your handler is executed asynchronously. The SDK does not wait for your handler to execute, and it doesn’t care when or if it completes.

## CalculateChallengeResponse example

The following is an example of calculating an OTP. This operation will send ```hmacChal``` to the key, notify the user through a message printed to the console, and then receive the response as a six-digit code in characters.

```
using (OtpSession otp = new OtpSession(yKey))
{
  // The challenge, hmacChal, has been set elsewhere.
  string result = otp.CalculateChallengeResponse(Slot.ShortPress)
    .UseChallenge(hmacChal)
    .UseTouchNotifier(() => Console.WriteLine("Touch the key."))
    .GetCode(6);
}
```

In this example, we send ```yOtpChal``` to the key and get the result as ModHex:

```
using (OtpSession otp = new OtpSession(key))
{
  // The challenge, yOtpChal, has been set elsewhere.
  ReadOnlyMemory<byte> resp = otp.CalculateChallengeResponse(Slot.ShortPress)
    .UseChallenge(yOtpChal)
    .UseTouchNotifier(() => Console.WriteLine("Touch the key."))
    .GetDataBytes();
  string result = ModHex.EncodeBytes(resp.Span);
}
```

In the final example, we send a TOTP challenge to the key and get the result as an eight-digit code:

```
using (OtpSession otp = new OtpSession(yKey))
{
  // The challenge, hmacChal, has been set elsewhere.
  string result = otp.CalculateChallengeResponse(Slot.ShortPress)
    .UseTotp()
    .UseTouchNotifier(() => Console.WriteLine("Touch the key."))
    .GetCode(8);
}
```
