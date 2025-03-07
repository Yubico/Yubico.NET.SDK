---
uid: PivOverview
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

# PIV overview

Personal Identity Verification (PIV) is defined in FIPS 201. It is a US government
standard defining various authentication and cryptographic operations using a smart card.
It defines a set of functions and specifies behavior of a smart card. To be a
PIV-compliant smart card, a device must implement these functions in the specified manner.

A developer that wants to build an application that utilizes a PIV-compliant smart card
can read the PIV specification, create the command APDUs (see the article on
[APDUs](xref:UsersManualApdu)), send them over an appropriate transport protocol, and interpret the
response APDUs.

Alternatively, the developer can use the SDK.
[Make the connection](xref:UsersManualMakingAConnection), create a
[PivSession](xref:Yubico.YubiKey.Piv.PivSession), then call appropriate methods,
such as [GenerateKeyPair](xref:Yubico.YubiKey.Piv.PivSession.GenerateKeyPair%2a) or
[Sign](xref:Yubico.YubiKey.Piv.PivSession.Sign%2a).
