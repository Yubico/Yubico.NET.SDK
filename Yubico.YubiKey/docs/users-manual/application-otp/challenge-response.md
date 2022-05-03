---
uid: OtpChallengeResponse
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

# Challenge-response

The other OTP application configurations ([Yubico OTP](xref:OtpYubicoOtp), [OATH HOTP](xref:OtpHotp), and [static password](xref:OtpStaticPassword)) require the user to activate the configured [slot](xref:OtpSlots) (by touching the YubiKey or scanning it with an [NFC reader](xref:OtpNdef)) in order to generate and submit the password from the YubiKey to a host device. Challenge-response, on the other hand, begins with a “challenge” that a host sends to the YubiKey. The YubiKey receives the challenge (as a byte array) and “responds” by encrypting or digesting (hashing) the challenge with a stored secret key and sending it back to the host for authentication.

Challenge-response authentication is primarily used in situations where the host cannot connect to an external validation service. In these cases, the host itself or a server on an internal network will handle the validation of the responses.

To implement challenge-response authentication with a .NET application, the following must occur:

* A slot on the YubiKey must be [configured](#sdk-functionality) with a secret key and encryption/hashing algorithm.

* The application must be able to [send challenges to and receive responses](#sdk-functionality) from a YubiKey.

* A copy of the secret key must be shared with the validating party.

* The validating party must be able to validate responses and pass the result back to the application.

> [!NOTE]  
> All YubiKey-host communication for challenge-response is done via the [HID communication protocol](xref:OtpHID). Therefore, challenge-response authentication will only work when a YubiKey is physically plugged into a host over USB or Lightning. Challenges and responses cannot be communicated wirelessly with NFC.

## Supported challenge-response algorithms

The .NET SDK and the YubiKey support the following encryption and hashing algorithms for challenge-response:

* [Yubico OTP](xref:OtpYubicoOtp) (encryption)

* HMAC SHA1 as defined in [RFC2104](https://datatracker.ietf.org/doc/html/rfc2104) (hashing)

For Yubico OTP challenge-response, the key will receive a 6-byte challenge. The YubiKey will then create a 16-byte string by concatenating the challenge with 10 bytes of unique device fields. For Yubico OTP challenge-response, these 10 bytes of additional data are not important. They are merely added as padding so that the challenge may be encrypted with a 16-byte key using the AES encryption algorithm. (AES requires that data be encrypted in blocks of the same size as the encryption key.)

For HMAC SHA1 challenge-response, the key will receive a challenge of up to 64 bytes in size, which will be digested (hashed) with a 20-byte secret key.

> [!NOTE]  
> Hashing/digesting is a one-way operation, meaning that once a block of data is hashed, it cannot be converted back into its original form. Encryption, on the other hand, is a two-way operation. When a block of data is encrypted, it can be decrypted back into its original form at any time. This is an important distinction because the validating party will have to respond differently to Yubico OTP responses (encrypted) and HMAC SHA1 responses (hashed). For Yubico OTP, the validating party will have to decrypt the response and compare the result with the original challenge. For HMAC SHA1, the validating party will have to perform the same hashing operation with the original challenge and compare the result to the response received from the YubiKey.

## Challenge initiation and authentication

The challenge-response process works as follows:

1. The YubiKey is plugged into the host.

1. The application on the host sends a challenge to a specific slot of the YubiKey via the SDK.

1. The YubiKey receives the challenge and encrypts/digests it with the secret key and encryption/hashing algorithm that the slot was configured with.

1. The YubiKey sends the response back to the host, and the application receives it as a string of numeric digits, a byte string, or a single integer (as determined by the SDK).

1. The application sends the response to the validating party. For Yubico OTP challenge-response, the response must be decrypted using the YubiKey’s unique secret key. For HMAC SHA1 challenge-response, the validating party must digest the challenge with the secret key using the same HMAC SHA1 algorithm.

1. For Yubico OTP, if the decrypted response matches the original challenge that was sent to the YubiKey, authentication was successful, and the user is logged in. (For Yubico OTP challenge-response, the 6-byte challenge must match the first 6 bytes of the decrypted response–the other bytes are ignored.) For HMAC SHA1, if the response matches the server's digested challenge, authentication was successful, and the user is logged in.

> [!NOTE]  
> For the authentication process to succeed, the size of the challenge must align with the algorithm that the YubiKey was configured with. Similarly, the validating party must decrypt the response using the same algorithm that the challenge was encrypted with.

## SDK functionality

The SDK’s challenge-response functionality centers around the following two methods:

* [CalculateChallengeResponse()](xref:Yubico.YubiKey.Otp.OtpSession.CalculateChallengeResponse%28Yubico.YubiKey.Otp.Slot%29)

* [ConfigureChallengeResponse()](xref:Yubico.YubiKey.Otp.OtpSession.ConfigureChallengeResponse%28Yubico.YubiKey.Otp.Slot%29)

ConfigureChallengeResponse() allows you to configure an OTP application slot on a YubiKey to receive a challenge from a host and process it based on a specific algorithm and secret key. CalculateChallengeResponse() allows a host to send a challenge to a YubiKey and then receive the response from the YubiKey.

### ConfigureChallengeResponse()

When calling ConfigureChallengeResponse(), you must set the secret key for the slot, which can be generated randomly via [GenerateKey()](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.GenerateKey%28System.Memory%7BSystem.Byte%7D%29) or set explicitly with [UseKey()](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.UseKey%28System.ReadOnlyMemory%7BSystem.Byte%7D%29). If you choose to generate a key, that key must be shared with the validating party before being cleared from memory. Secrets cannot be extracted from the YubiKey once configured.

You must also set the algorithm that will be used to respond to challenges by calling either [UseHmacSha1()](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.UseHmacSha1) or [UseYubiOtp()](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.UseYubiOtp). For example, if you call UseHmacSha1(), the YubiKey will encrypt challenges it receives with the secret key via the HMAC SHA1 algorithm.

> [!NOTE]  
> It’s important that the size of your secret key matches the size that is expected for the algorithm you chose ([16 bytes](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.YubiOtpKeySize) for Yubico OTP and [20 bytes](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.HmacSha1KeySize) for HMAC SHA1). For example, if you call UseYubiOtp(), the key that you set with UseKey() must be 16 bytes long. Otherwise, the YubiKey will not be able to respond to a challenge correctly.

The ConfigureChallengeResponse class also provides optional methods for requiring users to touch the YubiKey to initiate the challenge-response operation ([UseButton()](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.UseButton%28System.Boolean%29)) or enabling the key to process HMAC SHA1 challenges of less than 64 bytes ([UseSmallChallenge()](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse.UseSmallChallenge%28System.Boolean%29)).

For a full list of the methods in the ConfigureChallengeResponse class, please see the [API documentation](xref:Yubico.YubiKey.Otp.Operations.ConfigureChallengeResponse).

For an example of how to use ConfigureChallengeResponse(), please see [How to program a slot with a challenge-response credential](xref:OtpProgramChallengeResponse).

### CalculateChallengeResponse()

In order for a host to send a challenge to a YubiKey and receive a response, an application on the host must call CalculateChallengeResponse(). With this method, you can:

* send the challenge to the YubiKey as a byte array with [UseChallenge()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.UseChallenge%28System.Byte%5B%5D%29).

* send a message to the user to notify them to touch the YubiKey to initiate the challenge-response operation with [UseTouchNotifier()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.UseTouchNotifier%28System.Action%29). This is only needed if the YubiKey slot is configured to require the button touch with UseButton().

* receive the response from the YubiKey. The response can be received as a string of numeric digits via [GetCode()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.GetCode%28System.Int32%29) (for HMAC SHA1 challenges), as a byte string via [GetDataBytes()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.GetDataBytes) (for Yubico OTP challenges), or as a single integer via [GetDataInt()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.GetDataInt) (for HMAC SHA1 challenges).

> [!NOTE]  
> The size of the challenge sent to the YubiKey with UseChallenge() must correspond to the configuration of the YubiKey slot. If the slot is configured to perform Yubico OTP, the challenge must be [6 bytes](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.YubicoOtpChallengeSize) long. If the slot is configured for HMAC SHA1, the challenge must be [64 bytes](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.MaxHmacChallengeSize) long. However, if the slot has been configured with UseSmallChallenge(), an HMAC SHA1 challenge smaller than 64 bytes is acceptable.

Alternatively, the application can send a TOTP challenge to the YubiKey with [UseTotp()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.UseTotp) and receive the response with GetCode(). The time period of the TOTP challenge can be set via [WithPeriod()](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.WithPeriod%28System.Int32%29).

For a full list of the methods in the CalculateChallengeResponse class, please see the [API documentation](xref:Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse).

For an example of how to use CalculateChallengeResponse(), please see [How to calculate a response code for a challenge-response credential](xref:OtpCalcChallengeResponseCode).
