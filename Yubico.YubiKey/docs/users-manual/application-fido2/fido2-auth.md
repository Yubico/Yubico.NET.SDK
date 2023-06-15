---
uid: Fido2Auth
---

<!-- Copyright 2023 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# FIDO2 Authentication (PIN and Fingerprint)

In order to perform many FIDO2 operations, authentication is required. With the YubiKey,
authentication will be either a PIN or a fingerprint.

However, the rules of authentication with FIDO2 are not straightforward. For example, a
PIN does not authenticate a session, but rather allows you to obtain a "token" from the
YubiKey which is then passed back to the YubiKey as permission to perform various
operations. Furthermore, there are times when you need more than one token (you need to
authenticate or verify the PIN more than once) in a single session.

In order to perform authentication in your application, there are two main options when
calling on the SDK to perform FIDO2 operations:

* Use only the [Fido2Session](xref:Yubico.YubiKey.Fido2.Fido2Session) APIs, supply a
[KeyCollector](../sdk-programming-guide/key-collector.md), and let the SDK handle both
authentication logic and making calls to the appropriate PIN or fingerprint verification
methods when needed.
* Learn the rules of FIDO2 authentication and make sure your code calls the appropriate
`TryVerify` methods (or the appropriate commands, e.g. `GetPinUvAuthTokenUsingPinCommand`)
before performing the relevant operations.

If you choose the first option (automatic auth by the SDK), then you must build a
KeyCollector. See the articles ([here](../sdk-programming-guide/key-collector.md) and
[here](../sdk-programming-guide/key-collector-touch.md)) for more information on this
topic. The FIDO2 sample code has a KeyCollector to help you build one for your
application.

There is also further information on PINs, fingerprints, and FIDO2 authentication in
the following articles (make sure you read at least the first one on the rules of PIN
composition):

* [The FIDO2 PIN](fido2-pin.md)
* [The FIDO2 fingerprint and Bio Enrollment](fido2-bio-enrollment.md)
* [AuthTokens, permissions, PIN/UV, and AuthParams](fido2-auth-tokens.md)
* [The SDK's AuthToken retrieval logic](sdk-auth-token-logic.md)
* [Touch and fingerprint notification](fido2-touch-notification.md)
* [PIN/UV authentication protocols](pin-uv-auth-protocols.md)

If you choose the second option, possibly because you don't want to build a KeyCollector,
then you must learn the standard's rules of authentication. You can do that either by
studying the FIDO2 CTAP2.1 standard or the articles listed above, or ideally both.

Then you must call the appropriate Verify methods before operations. This could require
specifying permissions. It could also require making multiple verification calls in a
single session. If you develop a strong understanding of the FIDO2 authentication rules,
then you will be able to do this.
