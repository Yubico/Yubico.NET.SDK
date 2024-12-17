---
uid: UriStringFormat
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

# URI string format

```txt
otpauth://TYPE/LABEL?PARAMETERS
```

## Scheme

Each URI begins with a scheme name that refers to a specification for assigning identifiers within that scheme.

| The scheme name |
|:---------------:|
|     otpauth     |

This scheme name is used by Authenticator apps to
generate one-time passcodes using OATH.

The otpauth:// URI scheme was originally formalised by Google. Most authenticator apps register a handler for otpauth://
so the camera app knows how to prompt the user to launch the authenticator app when it’s scanned.

## Type

| Valid types |
|:-----------:|
|    hotp     | 
|    totp     | 

The type is needed to distinguish whether the credential will be used for counter-based HOTP or for time-based TOTP.

Read more about the difference between the two types of [OATH credentials](./oath-credentials.md).

## Label

The label is used to identify which account a credential is associated with. It also serves as the unique identifier for
the credential itself.

The label is created from:

| Name         | Description                                                                                  |
|:-------------|:---------------------------------------------------------------------------------------------|
| Issuer       | An optional string value indicating the provider or service this account is associated with. |
| Account Name | A URI-encoded string that usually is the user's email address.                               |

It is formatted as "Issuer:Account" when both parameters are present. It is formatted as "Account" when there is no
Issuer.

The label prevents collisions between different accounts with different providers that might be identified using the
same account name, e.g. the user's email address.

The issuer and account name should be separated by a literal or url-encoded colon, and optional spaces may precede the
account name. Neither issuer nor account name may themselves contain a colon. According
to [RFC 5234](https://www.rfc-editor.org/rfc/rfc5234.txt) a valid label might look like:

```txt
Example:alice@gmail.com

ACME%20Co:john.doe@email.com
```

## Parameters

### Secret

The secret is provided by the website to the user in the QR code, both sides need to retain this secret key for one-time
password generation.

The secret parameter is an arbitrary credential value encoded in Base32 according
to [RFC 3548](https://datatracker.ietf.org/doc/html/rfc3548).

The padding specified in [RFC 3548 section 2.2](https://datatracker.ietf.org/doc/html/rfc3548#section-2.2) is not
required and should be omitted.

There is Base32 helper class in the Yubico.Core library.

### Issuer

The issuer parameter is an optional string value indicating the provider or service the credential is associated with.
It is URL-encoded according to [RFC 3986](https://datatracker.ietf.org/doc/html/rfc3986).

Valid values corresponding to the label examples above would be:

```txt
issuer=Example

issuer=ACME%20Co
```

The issuer parameter is recommended, but it can be absent. Also, the issuer parameter and issuer string in label should
be equal.

### Algorithm

The hash algorithm used by the credential. It is optional.

| Valid algorithm |
|:---------------:|
| SHA1 (Default)  | 
|     SHA256      | 
|     SHA512      |

### Digits

The number of digits in a one-time password (OTP).

| Valid number of digits |
|:----------------------:|
|           6            | 
|           7            | 
|           8            |

### Counter

The counter is only used if the type is HOTP.

The counter parameter is required when provisioning HOTP credentials. It will set the initial counter value.

### Period

Period is only used if the type is TOTP.

The period parameter defines a validity period in seconds for the TOTP code.

| Valid period in seconds |
|:-----------------------:|
|           15            | 
|      30 (Default)       | 
|           60            |

## Examples

### Without parameters

```txt
otpauth://totp/Example:alice@google.com?secret=JBSWY3DPEHPK3PXP&issuer=Example
```

Try this live [authenticator demo](https://rootprojects.org/authenticator/) with the source
code [here](https://git.coolaj86.com/coolaj86/browser-authenticator.js). Note that this specific demo and code is not
affiliated with Yubico.

The URI specification [RFC 3986](https://datatracker.ietf.org/doc/html/rfc3986).
