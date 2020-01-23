---
uid: OathCredentials
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

# OATH Credentials overview

An OATH credential can be a TOTP (Time-based One-time Password) or a HOTP (HMAC-based One-time Password). 

The credential has a set of parameters.

### Common parameters for TOTP and HOTP credentials:

| Name | Description |
| :--- | :--- |
| Issuer | The issuer parameter is a string value indicating the provider or service this account is associated with. |
| Account Name | The account name is a string that usually is the user's email address. |
| Type | Indicates the type of the credential as either HOTP or TOTP. |
| Algorithm | The hash algorithm used by the credential. |
| Secret | The secret parameter is an arbitrary key value encoded in Base32 according to RFC 3548. |
| Digits | The number of digits in a one-time password (OTP). |
| Requires Touch | The credential requires the user to touch the key to generate a one-time password (OTP). |
| Name | Only get property witch serves as the unique identifier for the credential.|

The Name is created from Period, Issue and Account Name with the following format:

```
"period/issuer:account"
```

If period is a default value - 30 seconds, or the credential's type is HOTP, then the format will be:

```
"issuer:account"
```

Also, if Issuer is not specified, the format will be:

```
"period/account"
```

Or just an Account Name for TOTP with default period or HOTP credentials: 

```
"account"
```

### Specific to HOTP credential:

HOTP is an event based one-time password algorithm. The moving factor (event) is represented here by the counter parameter. The server and user calculate the OTP by applying a hashing and truncating operation to the secret key and the counter. The server compares the OTP it calculated against the one provided by the user. Both sides then increment the counters.

The counters have to be kept in sync between the server and the user. If a user opens the authenticator app to generate an OTP but ends up not using it, the counter on the user side will become out of sync with the server. One way to handle this is a resynchronisation mechanism in which the server tries a couple of future counter values to see if it finds a matching OTP and synchronise the counter accordingly.

| Name | Description |
| :--- | :--- |
| Counter | The initial counter value. The moving factor is incremented each time based on the counter. |
| Period | 0 (Undefined) |

Period should be ignored/undefined as this is only applicable to time-based credentials.

The validity for HOTP code is set to DateTimeOffset.MaxValue because HOTP code is not time based.

### Specific to TOTP credentials:

TOTP is an event based one-time password algorithm. The moving factor (event) is time-based rather than counter-based.

| Name | Description |
| :--- | :--- |
| Period | The validity period in seconds for TOTP code. It can be 15, 30 or 60 seconds. |

The validity for TOTP code is set to DateTimeOffset.Now + credential period (15, 30, or 60 seconds). Also, it is "rounded" to the nearest 15, 30, or 60 seconds. For example, it will start at 1:14:30 and not 1:14:34 if the timestep is 30 seconds.

## OTP code parameters:

| Name | Description |
| :--- | :--- |
| Value | The generated one-time password. |
| Valid From | The timestamp that was used to generate code. |
| Valid Until | The timestamp when the code becomes invalid. |

## Create credentials

There two ways of creating credentials:

1. Using a URI string.

When you enable two-factor authentication on websites, they usually show you a QR code and ask you to scan and launch an authenticator app.

QR codes are used in scanning secrets to generate one-time passwords. Secrets may be encoded in QR codes as a URI with the following format:

```txt
otpauth://TYPE/LABEL?PARAMETERS
```

You can create a credential from the string received from the QR reader or manually from the server.

For example: A TOTP credential for user john@example.com, for use with a service provided by ACME Co, might look like the following:

```csharp
var credential = Credential.ParseUri(
    new Uri("otpauth://totp/ACME%20Co:john@example.com?secret=5JRIUNLTT3URLTR7CLZOTM4P2GFGB3RY&issuer=ACME%20Co&algorithm=SHA1&digits=6&period=30"));
```

Read more about [URI strings](./uri-string-format.md).

The URI specification [RFC 3986](https://datatracker.ietf.org/doc/html/rfc3986).

2. Specifying each parameter

If you are unable to capture the QR code and use a URI string, you can manually create the credential by adding the account information, the provider (Amazon, Google, Microsoft, etc.) and the shared secret.

```
// create TOTP credential
var credential = new Credential {
    Issuer = Yubico,
    AccountName = "test@yubico.com",
    Type = Totp,
    Period = 30,
    Digits = 6,
    Secret = "HXDMVJECJJWSRB3HWIZR4IFUGFTMXBOZ",
    RequireTouch = false 
}

// create HOTP credential
var credential = new Credential {
    Issuer = Yubico,
    AccountName = "test@yubico.com",
    Type = Hotp,
    Period = 0,
    Digits = 6,
    Counter = 
    Secret = "HXDMVJECJJWSRB3HWIZR4IFUGFTMXBOZ",
    RequireTouch = false 
}
```
