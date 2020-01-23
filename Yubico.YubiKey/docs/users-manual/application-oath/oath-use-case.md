---
uid: OathUseCase
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

# Building a basic authenticator

The most popular use case for the OATH applications is to utilize it by building a time-based OTP authenticator app. Below are some basic steps in order to implement one.

1. Find the connected Yubikey:

```csharp
IEnumerable<IYubiKeyDevice> keys = YubiKeyDevice.FindByTransport(Transport.UsbSmartCard);

var yubiKeyToUse = keys.First();
```

2. Create an OathSession object:

```csharp
var oathSession = new OathSession(yubiKeyToUse);
```

This will connect to the OATH application on the chosen YubiKey.

3. Get all configured credentials from the YubiKey

```csharp
IList<Credential> credentials = oathSession.GetCredentials();
```

You would probably want to find if there any HOTP credentials and credentials that require touch to generate OTPs. This way you don't show the values for those credentials until it is requested by tapping "Generate code" button, for example.

Also, you will need to track TOTP credentials that have non-default periods, like 15 and 60 seconds.

4. Calculate the credentials and show OTPs

```csharp
IDictionary<Credential, Code> credentialCodes = oathSession.CalculateAllCredentials();
```

When HOTP credentials or credentials that require touch are requested, calculate them by using CalculateCredential() method:

```csharp
Code otpCode = CalculateCredential(Credential);
```
Also, any credentials with a non-default period should be recalculated in their respective interval. 

5. Add new credentials.

The best way to add credential it is by implementing a QR code scanner and reading an URI string from the QR code.

```csharp
// Pass the string that received from QR reader or manually from server. It will return credential parsed from URI string.
Credential credential = _oathSession.AddCredential(
                "otpauth://totp/ACME%20Co:test@example.com?secret=HXDMVJECJJWSRB3HWIZR4IFUGFTMXBOZ&issuer=ACME%20Co&algorithm=SHA1&digits=6&period=30");
```

Read more about [credentials](./oath-credentials.md) and [URI strings](./uri-string-format.md).

6. Remove and Rename credentials if needed.

```csharp
var credentialTotp = new Credential 
{
  Issuer = "Yubico",
  AccountName = "test@yubico.com",
  Type = Totp,
  Period = 60,
  Secret = "test",
  Digits = 8
};

// Pass credential to rename as well as the new Issuer and AccountName.
oathSession.RenameCredential(credentialTotp, "Test", "example@test.com");

var credentialTotp = new Credential 
{
  Issuer = "Yubico",
  AccountName = "test@yubico.com",
  Type = Totp,
  Period = 60,
  Secret = "test",
  Digits = 8
};

// Pass credential to remove.
oathSession.RemoveCredential(credentialTotp);
```

Read more about [OathSession](./oath-session.md) methods and [OathPassword](./oath-password.md) implementation on the YubiKey.
