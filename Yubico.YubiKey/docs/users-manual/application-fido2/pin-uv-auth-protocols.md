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

### Pin protocol one

### Pin protocol two

## PIN operations

### Get key agreement

The platform is going to encrypt the PIN before sending it to the YubiKey. But to do so it
needs a key, one that can encrypt in such a way that the YubiKey will be able to decrypt.

Generally, there are three ways to achieve this: encrypt the PIN using the YubiKey's
public RSA key, encrypt the PIN using a symmetric key and encrypt the symmetric key using
the YubiKey's public RSA key, or use a key agreement algorithm such as DH or ECDH to
compute a shared symmetric key and encrypt the PIN using that key.

Currently, CTAP2 supports only ECDH using a specified set of standard curves, such as
NIST's P-256. In this system, the platform queries the YubiKey to determine which UV/PIN
auth protocols it supports, and from the list returned chooses one it supports as well.
Based on the protocol chosen, it will know which curve to use and will generate a new key
pair. In order to compute the shared secret, it will combine its new private key with the
YubiKey's public key. Later on, the platform will send to the YubiKey its public key so
that the YubiKey can generate the same shared secret by combining its private key with the
platform's public key.

This means that the platform must obtain the YubiKey's public key. It does so by sending
the [GetKeyAgreemnetCommand](xref:Yubico.YubiKey.Fido2.Commands.GetKeyAgreementCommand).
The response to this command is the public key the platform will use to compute the shared
secret. See the User's manual entry on
[FIDO2 commands](fido2-commands.md#get-key-agreement) for more information.

### Set PIN

Note that the FIDO2 standards contain some special requirements on the PIN. In brief, the
PIN must be supplied as "... the UTF-8 representation of" the "Unicode characters in
Normalization Form C." For a discussion of what that means, see the User's Manual article
on [the FIDO2 PIN](fido2-pin.md).

The platform will need to encrypt the PIN before sending it to the YubiKey. That means it
must first decide on a protocol, then obtain the
[YubiKey's public key](#get-key-agreement), generate a key pair, and compute the shared
secret. Finally, pass the PIN to the
[SetPinCommand](xref:Yubico.YubiKey.Fido2.Commands.SetPinCommand).

The process is simple,

1. create an instance of one of the
[PIN Protocol](xref:Yubico.YubiKey.Fido2.PinProtocols.PinUvAuthProtocolBase) classes,
this specifies which protocol to use
2. call the
[GetKeyAgreementCommand](xref:Yubico.YubiKey.Fido2.Commands.GetKeyAgreementCommand) to
obtain the YubiKey's public key
3. call the `PinProtocol` object's `Encapsulate` method to generate the platform public
key and compute the shared secret
4. call the [SetPinCommand](xref:Yubico.YubiKey.Fido2.Commands.SetPinCommand).

### Change PIN

Changing a PIN is very similar to setting the PIN.

1. create an instance of one of the
[PIN Protocol](xref:Yubico.YubiKey.Fido2.PinProtocols.PinUvAuthProtocolBase) classes,
this specifies which protocol to use, this does not need to be the same protocol used to
set the PIN originally.
2. call the
[GetKeyAgreementCommand](xref:Yubico.YubiKey.Fido2.Commands.GetKeyAgreementCommand) to
obtain the YubiKey's public key
3. call the `PinProtocol` object's `Encapsulate` method to generate the platform public
key and compute the shared secret
4. call the [ChangePinCommand](xref:Yubico.YubiKey.Fido2.Commands.ChangePinCommand),
supplying both the current PIN and the new PIN.

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

User Verification (UV) refers to the act of authenticating oneself directly on the YubiKey. The only
YubiKey that supports UV is the YubiKey Bio Series, which performs UV via onboard fingerprint sensor.
All other YubiKey versions support PIN authentication only.

UV is implemented in addition to the device PIN. UV is typically the preferred authentication method,
but in circumstances where UV fails (e.g. UV is blocked from too many retries) the device can use
PIN authentication instead. In order to set up UV on a YubiKey, the device must first have a PIN set
and a fingerprint registered.

Like the PIN, a user has only a few attempts to successfully authenticate using UV. After several
consecutive failed attempts, the YubiKey will disable the UV authentication method and fall back
to PIN authentication. Once the PIN has been entered successfully, UV will be re-enabled and its
retry counter reset.

The YubiKey allows developers to
[query the number of UV retries left](xref:Yubico.YubiKey.Fido2.Commands.GetUvRetriesCommand)
so that they can indicate to the user how many attempts are left.
