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

These standards a related. U2F can be thought of as "version 1" of the standard, and FIDO2
is "version 2". "U2F" stands for "Universal Second Factor" authentication.

The SDK treats each version as a separate application, even though there is some overlap
inside the YubiKey itself.

Find the home of FIDO at the [FIDO Alliance](https://fidoalliance.org/). You can also find
Yubico's introduction to FIDO2 [here](https://www.yubico.com/authentication-standards/fido2/).
