---
uid: FidoU2fOverview
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

# FIDO U2F overview

The acronym FIDO stands for "Fast IDentity Online". The FIDO Alliance is a standards body,
of which Yubico is a member, that provides techniques for using security keys to instantly
access any number of online services (such as login) with no drivers or host device
software needed.

There are two FIDO standards that the YubiKey currently implements:

- FIDO U2F
- FIDO2

These standards a related. U2F can be thought of as "version 1" of the standard, and FIDO2
is "version 2". "U2F" stands for "Universal Second Factor" authentication.

The SDK treats each version as a separate application, even though there is some overlap
inside the YubiKey itself. Future versions of the SDK's User's Manual will contain a FIDO2
section as well.

Find the home of FIDO at the [FIDO Alliance](https://fidoalliance.org/). You can also find
Yubico's introduction to U2F [here](https://www.yubico.com/authentication-standards/fido-u2f/).

There are three components in FIDO U2F

- The authenticator (YubiKey)
- The client (a browser, platform component, or application)
- The relying party (verifies the authentication process)

With FIDO U2F, the host (e.g. the laptop or phone) does not need any drivers or other
software outside of the client which implements support for U2F. If no U2F client is
available, it will not be possible to perform the authentication.

You will often hear the term "CTAP1" (pronounced "see-tap-one") with respect to FIDO U2F.

Briefly, CTAP1 is the name given by the newer FIDO2 standard to the part dealing with
communication between the authenticator (the YubiKey) and the client (the browser). In
fact, it stands for "Client To Authenticator Protocol". CTAP1 is the specification
used for U2F. Later on, with FIDO2, the communication protocol was updated as well. Hence,
FIDO2 primarily uses CTAP2. More information on CTAP is given in further User's Manual
articles.

Another term you might hear is "WebAuthn" (pronounced "web-auth-en"). That is related to
FIDO2 and will be described in future SDK User Manuals.
