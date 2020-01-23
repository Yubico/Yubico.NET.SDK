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

# .NET YubiKey SDK

The SDK allows you to integrate the YubiKey and its applications into your .NET-based
application or library.

## SDK documentation

The documentation for the .NET YubiKey SDK is split into two main sections:

- A [user's manual](users-manual/intro.md) that describes the concepts that you will
  encounter while working with the SDK and the YubiKey. It provides a general outline of
  how to use the SDK. Tutorials and walk-throughs can be found here as well.
- [API Documentation](yubikey-api/index.md) is where detailed descriptions of the classes and
  interfaces of the SDK reside.

## Supported platforms

Modern .NET supports more than just Microsoft Windows, and so do we. Support for macOS is built in,
and has been tested on both Intel and Apple Silicon (i.e. M1) platforms. (Apple Silicon is supported
through Rosetta 2. The .NET 6 runtime will add native support, however it is not ready at the time
of this writing.)

Future platform support will be driven by customer interest.

This SDK targets .NET Standard 2.0, allowing for a wide reach of .NET platforms.
See [this page](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) for more
information on what .NET implementations support .NET Standard 2.0. Note that while this SDK may build
with Xamarin and Mono, only the Windows and macOS operating systems are supported at this time.

## Supported YubiKey applications

The YubiKey is a versatile security key that supports numerous standards and protocols. This SDK offers
full support for integrating with Yubico OTP, along with OATH and PIV standards.

### OTP

Yubico OTP is a simple yet strong authentication mechanism that is supported by all
YubiKeys out of the box. Yubico OTP can be used as the second factor in a 2-factor authentication scheme
or on its own, providing 1-factor authentication.

Read more about OTP [here](users-manual/application-otp/otp-overview.md).

### OATH

The Initiative for Open Authentication (OATH) is an organization that specifies two
open one-time password standards: HMAC OTP (HOTP), and the more familiar Time-based OTP (TOTP).
Read more about OATH [here](users-manual/application-oath/oath-overview.md).

### PIV

Personal Identity Verification (PIV), or FIPS 201, is a US government standard. It enables
RSA signing and encryption, along with ECC signing and key agreement operations using a
private key stored on a smart card (such as the YubiKey 5).

PIV is primarily used for non-web applications. It has built-in support under Windows and
can be used on macOS as well.

Read more about PIV [here](users-manual/application-piv/piv-overview.md).
