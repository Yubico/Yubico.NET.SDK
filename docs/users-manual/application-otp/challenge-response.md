---
uid: OtpChallengeResponse
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

# Challenge-response

The other OTP application configurations ([Yubico OTP](xref:OtpYubicoOtp), [OATH HOTP](xref:OtpHotp),
and [static password](xref:OtpStaticPassword)) require the user to activate the configured [slot](xref:OtpSlots) (by
touching the YubiKey or scanning it with an [NFC reader](xref:OtpNdef)) in order to generate and transmit the password
from the YubiKey to a host device. Challenge-response, on the other hand, begins with a “challenge” that a host sends to
the YubiKey. The YubiKey receives the challenge as a byte array and “responds” by encrypting or digesting (hashing)
the challenge with a stored secret key and sending the response back to the host for authentication.

Challenge-response is flexible. It can be used in single and multi-factor authentication for logging into applications
or devices, and validation can take place on a host device itself or on a validation server on an internal or external
network. The SDK supports all of these scenarios.

To implement challenge-response authentication with a .NET application, the following must occur:

* A slot on the YubiKey must be [configured](#sdk-functionality) with a secret key and encryption/hashing algorithm.

* The application must be able to [send challenges to and receive responses](#sdk-functionality) from a YubiKey.

* A copy of the secret key must be shared with the validating party.

* The validating party must be able to validate responses and pass the result back to the application.

> [!IMPORTANT]  
> All YubiKey-host communication for challenge-response is done via the [HID communication protocol](xref:OtpHID).
> Therefore, challenge-response authentication will only work when a YubiKey is physically plugged into a host over USB
> or
> Lightning. Challenges and responses cannot be communicated wirelessly with NFC.

## Supported challenge-response algorithms

The .NET SDK and the YubiKey support the following algorithms for challenge-response:

* [Yubico OTP](xref:OtpYubicoOtp) (encryption)

* HMAC-SHA1 as defined in [RFC2104](https://datatracker.ietf.org/doc/html/rfc2104) (hashing)

For Yubico OTP challenge-response, an application will send the YubiKey a 6-byte challenge. The YubiKey will then create a 16-byte
string by concatenating the challenge with 10 bytes of unique device fields. For Yubico OTP challenge-response, these 10
bytes of additional data are not important—they are merely added as padding so that the challenge may then be encrypted
with a 16-byte key using the AES encryption algorithm (AES requires that data be encrypted in blocks of the same size as
the encryption key). The resulting Yubico OTP (as a byte array) becomes the response.

For HMAC-SHA1 challenge-response, an application will send the YubiKey a challenge of up to 64 bytes in size, which will be digested (hashed) with a 20-byte secret key, resulting in a 20-byte response (the HMAC-SHA1 hash value). Responses can be received 
by an application as a byte array or a 6-10 digit numeric code. With HMAC-SHA1, the challenge can be either an 
application-specified byte array or the current Unix time.

> [!NOTE]  
> Hashing/digesting is a one-way operation, meaning that once a block of data is hashed, it cannot be converted back
> into its original form. Encryption, on the other hand, is a two-way operation. When a block of data is encrypted, it
> can
> be decrypted back into its original form. This is an important distinction because the validating party
> will
> have to respond differently to Yubico OTP responses (encrypted) and HMAC-SHA1 responses (hashed). For Yubico OTP, the
> validating party will have to decrypt the response and compare the result with the original challenge. For HMAC-SHA1,
> the validating party will have to perform the same hashing operation with the original challenge and compare the
> result
> to the response received from the YubiKey.

## Challenge initiation and authentication

The challenge-response process works as follows:

1. The YubiKey is connected to the host.

1. The application on the host sends a challenge to a specific slot of the YubiKey via the SDK.

1. The YubiKey receives the challenge and encrypts/digests it with the secret key and encryption/hashing algorithm that
   the slot was configured with.

1. The YubiKey sends the response back to the host, and the application receives it as a raw byte array, a string object of
   numeric digits, or an integer (as configured with the SDK).

1. The application sends the response to the validating party. For Yubico OTP challenge-response, the response must be
   decrypted using the YubiKey’s unique secret key. For HMAC-SHA1 challenge-response, the validating party must digest
   the challenge with the secret key and the HMAC-SHA1 algorithm.

1. For Yubico OTP, if the decrypted response matches the original challenge that was sent to the YubiKey, authentication
   was successful, and the user is logged in. (For Yubico OTP challenge-response, the 6-byte challenge must match the
   first 6 bytes of the decrypted response—the other bytes are ignored.) For HMAC-SHA1, if the response matches the
   validating party's digested challenge, authentication was successful, and the user is logged in.

## SDK functionality

The SDK’s challenge-response functionality centers around the following two methods:

* [CalculateChallengeResponse()](xref:Yubico.YubiKey.Otp.OtpSession.CalculateChallengeResponse%28Yubico.YubiKey.Otp.Slot%29)

* [ConfigureChallengeResponse()](xref:Yubico.YubiKey.Otp.OtpSession.ConfigureChallengeResponse%28Yubico.YubiKey.Otp.Slot%29)

``ConfigureChallengeResponse()`` allows you to configure an OTP application slot on a YubiKey to receive a challenge
from a host and process it based on a specific algorithm and secret key. ``CalculateChallengeResponse()`` allows a host
to send a challenge to a YubiKey and then receive its response.

### ConfigureChallengeResponse()

When calling ``ConfigureChallengeResponse()``, you must set the secret key for the slot, which can be generated randomly
via [GenerateKey()](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.GenerateKey%28System.Memory%7BSystem.Byte%7D%29)
or set explicitly
with [UseKey()](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.UseKey%28System.ReadOnlyMemory%7BSystem.Byte%7D%29).
If you choose to generate a key, that key must be shared with the validating party before being cleared from memory.
Secrets cannot be extracted from the YubiKey once configured.

You must also set the algorithm that will be used to respond to challenges by calling
either [UseHmacSha1()](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.UseHmacSha1)
or [UseYubiOtp()](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.UseYubiOtp). For example, if you
call ``UseHmacSha1()``, the YubiKey will digest challenges it receives with the secret key via the HMAC-SHA1 algorithm.

> [!NOTE]  
> It’s important that the size of your secret key matches the size that is expected for the algorithm you
> chose ([16 bytes](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.YubiOtpKeySize) for Yubico OTP
> and [20 bytes](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.HmacSha1KeySize) for HMAC-SHA1). The SDK will throw an exception if the key length is
> incorrect for the chosen algorithm.

The ``ConfigureChallengeResponse`` class also provides optional methods for requiring users to touch the YubiKey to
initiate the challenge-response
operation ([UseButton()](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.UseButton%28System.Boolean%29))
or enabling the key to process HMAC-SHA1 challenges of less than 64
bytes ([UseSmallChallenge()](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.UseSmallChallenge%28System.Boolean%29)).

> [!NOTE]  
> ``UseSmallChallenge()`` is included for compatibility with legacy systems whose implementations break data sets into
> multiple blocks, which often results in the last element being smaller than 64 bytes.

For a full list of the methods in the ``ConfigureChallengeResponse`` class, see
the [API documentation](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse).

For an example of how to use ``ConfigureChallengeResponse()``, see 
[How to program a slot with a challenge-response credential](xref:OtpProgramChallengeResponse).

### CalculateChallengeResponse()

In order for a host to send a challenge to a YubiKey and receive a response, an application on the host must
call ``CalculateChallengeResponse()``. With this method, you can:

* send a Yubico OTP or HMAC-SHA1 challenge to the YubiKey as an application-specified byte array
  with [UseChallenge()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.UseChallenge%28System.Byte%5B%5D%29). 
  Alternatively, the current Unix time can be sent as a challenge with 
  [UseTotp()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.UseTotp) for HMAC-SHA1 challenge-response.

* send a message to the user to notify them to touch the YubiKey to initiate the challenge-response operation
  with [UseTouchNotifier()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.UseTouchNotifier%28System.Action%29).
  This is only needed if the YubiKey slot was configured to require the button touch with ``UseButton()``.

* receive the response from the YubiKey. The response can be received as a string object of 6-10 numeric digits
  via [GetCode()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.GetCode%28System.Int32%29) (HMAC-SHA1), as a byte
  array via [GetDataBytes()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.GetDataBytes) (Yubico OTP, HMAC-SHA1), or as a single 10-digit, 32-bit integer
  via [GetDataInt()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.GetDataInt) (HMAC-SHA1).

In addition, the time period for time-based challenges sent with ``UseTotp()`` (i.e. how long a TOTP response is valid for) can be set
via [WithPeriod()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.WithPeriod%28System.Int32%29). The
default period is 30 seconds. Time-based challenges can only be used with keys configured for HMAC-SHA1 challenge-response. 
The SDK will throw an exception if you call both ``UseTotp()`` and ``UseChallenge()``.

> [!NOTE]  
> The size of the challenge sent to the YubiKey with ``UseChallenge()`` must align with the slot's configuration. If the
> slot is configured to perform Yubico OTP, the challenge must
> be [6 bytes](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.YubicoOtpChallengeSize) long. If the slot
> is
> configured for HMAC-SHA1, the challenge must
> be [64 bytes](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.MaxHmacChallengeSize) long. However, if
> the
> slot has been configured with ``UseSmallChallenge()``, a challenge smaller than 64 bytes is acceptable. The
> SDK will throw an exception if the challenge size does not match the YubiKey slot's configuration.

For a full list of the methods in the ``CalculateChallengeResponse`` class, see
the [API documentation](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse).

For an example of how to use ``CalculateChallengeResponse()``, see [How to calculate a response code for a challenge-response credential](xref:OtpCalcChallengeResponseCode).
