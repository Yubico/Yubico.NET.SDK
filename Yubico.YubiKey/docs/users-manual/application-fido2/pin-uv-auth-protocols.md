---
uid: Fido2PinProtocol
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

# CTAP2 PIN/UV authentication protocols

CTAP2 (the Client To Authenticator Protocol defined as part of the FIDO2 specification) has been designed in such
a way that plaintext PINs are never sent to the authenticator (the YubiKey). This is to prevent unwanted
eavesdropping between applications and the YubiKey - whether that be a software hook, or someone monitoring
USB traffic with hardware.

The PIN/UV auth protocol is the mechanism that is used to support the exchange of PIN and User Verification (UV)
data to and from the YubiKey. It ensures that the PIN is encrypted in such a way that only the application
and the YubiKey have the necessary knowledge to decrypt the PIN. This is done using a secure key-agreement and
key-exchange defined by the protocol.

If the application provides the correct PIN, the YubiKey will return a `pinUvAuthToken`, which can be used to
authenticate subsequent CTAP2 commands on the YubiKey. Similarly, user verification methods such as fingerprint
verification used by the YubiKey Bio Series can also use these auth protocols to obtain a `pinUvAuthToken`.
As fingerprints are securely matched and verified on the YubiKey itself, this form of user verification is
sufficient enough to not require an additional PIN. The YubiKey will, however, fall back to requiring a PIN
if too many failed fingerprint matches have occured.

A `pinUvAuthToken`, referred to simply as "authentication token" in the rest of this document, is a randomly-
generated, opaque sequence of bytes that acts as a stand-in for the PIN. The authentication token is long
enough that it would be impractical to brute force.

While authentication tokens are not the PIN, they still need to be handled with some care - at least for the
duration that the YubiKey remain powered. Therefore, it is strongly recommended that you zero the memory
that was holding the authentication token when your application has finished using it.

See also the [article on AuthTokens](fido2-auth-tokens.md).

## The two PIN/UV auth protocols

The standard defines two auth protocols for the method of securing the auth token
communication between the client and the YubiKey: the original protocol, now called
"Protocol One", and then a second (improved) protocol, called "Protocol Two".

Both prescribe using Elliptic Curve key agreement to share a base key. Also, both specify
deriving encryption and authentication keys from that shared base key, but they use
different key derivation functions. Furthermore, while both use AES-CBC to encrypt and
HMAC with SHA-256 to authenticate, the details of each are different.

Generally, older YubiKeys might support only Protocol One because it was the only choice
at the time of production. Once Protocol Two was finalized in the standard, later YubiKeys
added support for it. If a YubiKey supports Protocol Two, it will also support Protocol
One in case it needs to communicate with a client that supports only Protocol One.

When a client needs to obtain an AuthToken, it will contact the YubiKey and specify which
protocol to use to make the transfer. Generally, clients will query the YubiKey for a list
of protocols it supports. If the YubiKey supports only Protocol One, the client will
communicate using that protocol. If the YubiKey supports both, the client will choose
Protocol Two.

If your code uses the [Fido2Session](xref:Yubico.YubiKey.Fido2.Fido2Session) class to
perform FIDO2 operations, it will not need to choose a protocol. If a protocol is needed,
the `Fido2Session` will determine which protocols the connected YubiKey supports; it will
choose Protocol Two if supported, and Protocol One otherwise.
