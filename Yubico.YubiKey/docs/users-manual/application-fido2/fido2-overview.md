---
uid: Fido2Overview
---

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

# FIDO2 overview

The acronym FIDO stands for "Fast IDentity Online". The FIDO Alliance is a standards body,
of which Yubico is a member, that provides techniques for using security keys to instantly
access any number of online services (such as login) with no drivers or host device
software needed.

There are two FIDO standards that the YubiKey currently implements:

- FIDO U2F
- FIDO2

These standards are related. U2F can be thought of as "version 1" of the standard, and FIDO2
is "version 2". "U2F" stands for "Universal Second Factor" authentication.

The SDK treats each version as a separate application, even though there is some overlap
inside the YubiKey itself.

Find the home of FIDO at the [FIDO Alliance](https://fidoalliance.org/). You can also find
Yubico's introduction to FIDO2 [here](https://www.yubico.com/authentication-standards/fido2/).

There are three components in FIDO U2F:

- The authenticator (YubiKey)
- The client (a browser, platform component, or application)
- The relying party (verifies the authentication process)

With FIDO2, the host (e.g. the laptop or phone) does not need any drivers or other
software outside of the client which implements support for FIDO2. If no FIDO2 client is
available, it will not be possible to perform the authentication.

You will often hear the term "CTAP" (pronounced "see-tap") with respect to FIDO2.

Briefly, CTAP is the name given by the FIDO2 standard to the section of the standard
dealing with communication between the client (browser) and authenticator (the YubiKey).
In fact, it stands for "Client To Authenticator Protocol". CTAP1 is the specification used
for U2F. Later on, with FIDO2, the communication protocol was updated as well. Hence,
FIDO2 primarily uses CTAP2. More information on CTAP is given in further User's Manual
articles.

Another term you might hear is "WebAuthn" (pronounced "web-auth-en", short for Web
Authentication). This is a specification from the W3C (World Wide Web Consortium) and the
FIDO Alliance that further defines many of the details of FIDO2, CTAP2, and the
communication between the relying party and client.

As often happens in computer technology, the once-precise terminology has become somewhat
informal. Very often, you will hear people talking about FIDO2 and they will use the
terms CTAP, CTAP2, or WebAuthn to mean the entirety of FIDO2. To help dispell any
confusion, here is a brief summary of these standards and their relationships to each
other.

- FIDO: Fast IDentiy Online, the standards body that developed U2F and FIDO2
- CTAP: Client To Authenticator Protocol
- U2F: Universal Second Factor, the first generation of the standard
  - Includes CTAP, later called CTAP1
  - Includes Relying Party to Client communication
- FIDO2: The second generation of the standard
  - Developed as a partnership between the FIDO Alliance and W3C
  - Relying Party to Client communication defined in WebAuthn
  - Includes CTAP2, defined in FIDO2 and W3C documents
