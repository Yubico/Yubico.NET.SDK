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

## The PIN/UV protocol interface

### Pin protocol 1

### Pin protocol 2

## PIN operations

### Get key agreement

### Set PIN

### Change PIN

### Getting the PIN/UV authentication token

## PIN adjacent operations

### Getting the number of PIN retries

The YubiKey only allows for a few attempts at entering the FIDO PIN before a lockout occurs. This is an
important safety measure to guard against an attacker guessing at the PIN. By default the number of
guesses allowed is 8, however this can be configured.

If a PIN entry attempt failed, most UIs will want to display a message to the user. This message should
not only indicate that the attempt failed, but at some point prior to a lockout, should also alert the
user to the number of guesses remaining.

Under certain conditions (such as repeated failed attempts), the YubiKey may also require the user to
unplug the key and plug it back in. This is to further protect the YubiKey against automated attacks
on the PIN.

### Getting the number of user verification (UV) retries
