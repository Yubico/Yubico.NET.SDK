<!-- Copyright 2022 Yubico AB

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
through Rosetta 2.) We also support common Linux distributions such as Debian, Ubuntu, RHEL, and CentOS.
Other distros may still work, but they have not been tested by the SDK team.

Future distribution and platform support will be driven by customer interest.

This SDK targets .NET Standard 2.0, allowing for a wide reach of .NET platforms.
See [this page](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) for more
information on what .NET implementations support .NET Standard 2.0. Note that while this SDK may build
with Xamarin and Mono, only the Windows and macOS operating systems are supported at this time.
Additionally, while .NET Framework 4.6.x is listed as implementing Standard 2.0, this is not
entirely true. The SDK relies on certain cryptographic functionality that is defined
in the standard but not actually implemented in Framework 4.6.x.

## Supported YubiKey applications

The YubiKey is a versatile security key that supports numerous standards and protocols. This SDK offers
full support for integrating with Yubico OTP, along with the OATH, PIV, and FIDO U2F standards.

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

### FIDO U2F

U2F is an open authentication standard that enables keychain devices, mobile phones and other
devices to securely access any number of web-based services - instantly and with no drivers or
client software needed. U2F was created by Google and Yubico, with contribution from NXP, and is
today hosted by the open-authentication industry consortium [FIDO Alliance](https://fidoalliance.org/).
The technical specifications were launched in late 2014, including native support in Google
Accounts and Chrome, and have since resulted in a thriving ecosystem of hardware, software
and service providers.

Read more about FIDO U2F [here](users-manual/application-u2f/fido-u2f-overview.md).

### FIDO2

FIDO2 is the "second generation" of the FIDO open authentication standard. It is similar to U2F
in that implementations allow instant secure access to web-based services, with no drivers or
client software needed. FIDO2 was created by the [FIDO Alliance](https://fidoalliance.org/) -- a
consortium of dozens of tech and other companies as well as government organizations from around the
world -- along with the [W3C](https://www.w3.org) (World Wide Web Consortium). The technical
specifications for FIDO2 were launched in 2018. Today, many [browsers and mobile platforms support
FIDO2](https://support.yubico.com/hc/en-us/articles/360016615020-Operating-system-and-web-browser-support-for-FIDO2-and-U2F).

Read more about FIDO2 [here](xref:Fido2Overview).

### YubiHSM Auth

YubiHSM Auth is a YubiKey CCID application that stores the long-lived credentials used to establish
secure sessions with a YubiHSM 2. The secure session protocol is based on Secure Channel Protocol (SCP).
YubiHSM Auth is supported by YubiKey firmware version 5.4.3.

YubiHSM Auth uses hardware to protect these long-lived credentials. In addition to providing robust
security for the YubiHSM Auth application itself, this hardware protection subsequently increases the
security of the default password-based solution for YubiHSM 2's authentication.

Read more about YubiHSM Auth [here](xref:YubiHsmAuthOverview).

### Security Domain

The Security Domain is a special application on the YubiKey responsible for managing secure communication channels and cryptographic keys.
It implements protocols defined by [Global Platform Consortium](https://globalplatform.org/) that provide confidentiality and integrity for
commands sent between host applications and the YubiKey.

Read more about Security Domain [here](xref:SecurityDomainOverview).
