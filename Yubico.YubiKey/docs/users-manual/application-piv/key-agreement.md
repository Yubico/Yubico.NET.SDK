---
uid: UsersManualKeyAgreement
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

# Elliptic Curve Diffie-Hellman key agreement

If a slot contains an ECC key (<c>PivAlgorithm.EccP256</c>, <c>PivAlgorithm.EccP384</c>),
there are two operations it can perform: signing (ECDSA) and key agreement (EC
Diffie-Hellman, or ECDH).

An ECDH operation does not encrypt data. Rather, it generates a shared secret.

Here is a description of "classical" ECDH.

* Two correspondents agree to an EC parameter set.
* Phase 1: Each correspondent generates a private and public value.
    * The correspondents send each other the public values.
* Phase 2: Each correspondent uses their own private value with the other correspondent's
  public value to generate a secret.
    * If the two use the same parameters, they will generate the same secret.

The value each correspondent generates is a point on the curve. The ECDH algorithm is
defined as using the x-coordinate of that resulting point.

The correspondents can now use this "shared secret" as a key, or as the foundation of a
key derivation operation. They share a key. Or we can say they agree on a key, hence the
term "Key Agree". Generally they will use the key to encrypt bulk data in a message or
conversation.

An eavesdropper can see the parameters and public values, but without at least one of the
private values, cannot compute the shared secret.

This is similar to the RSA digital envelope. In that system, a sender generates a session
key, encrypts it using the recipient's public key, and encrypts the bulk data with the
session key. The recipient uses their private key to decrypt the session key, then uses
the session key to decrypt the bulk data.

Note that the RSA algorithm can be used for encryption and signing, but cannot be used for
key agreement. ECC can be used for signing and key agreement.

It is possible to use ECC for encryption as well. It is generally called ECES (Elliptic
Curve Encryption Scheme) or EC ElGamal. However, the .NET Base Class Libraries (BCL) and
the YubiKey do not support EC encryption.

## "Perfect Forward Secrecy"

One of the strengths of Diffie-Hellman in general (EC and the original formulation based
on large prime numbers) is that it is possible to use different public and private values
each time an operation is performed. That means if an attacker is able to break one
message, session key, or even a private key, it does not help in breaking other messages.
In other words, each message must be attacked independently. This is known as "perfect
forward secrecy".

In contrast, if an attacker breaks one RSA private key, all digitial envelope messages,
old and new, that used or will use that key are compromised.

Perfect forward secrecy only applies if different public and private values are used each
time.

## Man-in-the-middle attack

One of the easiest ways to attack DH is to intercept messages between two participants.
Suppose Alice wants to communicate with Bob, and the two decide to use ECDH with a
particular parameter set. But suppose Eve is able to intercept all of Bob's incoming and
outgoing messages.

When Alice sends her public value, Eve intercepts and keeps it for herself. Eve passes on
to Bob her own public value. When Bob sends his public value, Eve intercepts, keeps that
public value, and sends her own public value to Alice.

Alice will use her private value and Eve's public value to derive a session key. Call this
Key-AE.

Eve will use her private value and Alice's public value to derive Key-AE.

Similarly, Bob will derive Key-BE. Eve will aso be able to derive Key-BE.

When Alice sends a message to Bob encrypted with Key-AE, Eve will intercept, decrypt it,
store it, encrypt the recovered message using Key-BE, then send this newly encrypted
message to Bob. Bob will be able to decrypt using Key-BE.

Neither Bob nor Alice know that they are communicating with Eve.

To prevent this attack, we need to use certificates.

## Key agreement with public and private keys

In classical ECDH, each party generates a public and private value. However, it is
possible to extract a public value from an EC public key and the private value from the
partner EC private key. That is, you can generate an EC key pair, then use the keys to
perform the Key Agreement operation.

Generating the key pair is equivalent to Phase 1.

Combining your private key with the correspondent's public key is Phase 2.

This means that in order to perform Phase 2, the software (or YubiKey) will need to
extract the public and private values from the keys, then perform classical ECDH.

Now that we have keys, we can build certificates. Alice and Bob generate key pairs and
obtain certificates. Instead of exchanging public values, they exchange certificates.

If Eve intercepts Alice's certificate and sends her own certificate to Bob, he will reject
it because the name on that certificate is not Alice. Or if Eve builds a new cert with
Alice's name, Bob will still be able to reject it because it does not chain to one of his
trusted roots.

## Back to perfect forward secrecy

Suppose you build a system where each participant has a key pair and a certificate, and
you use these certs each time someone sends a message. For example, if Alice and Bob want
to communicate, each will always use their private key and the public key found in the
other party's cert. In this case, each message between Alice and Bob will use the same
public and private values, and hence derive the same session key.

A message between Alice and Carlos will be encrypted using a key derived from Alice's and
Carlos's key pairs, so it will be different from the key used by Alice and Bob. But each
message between Alice and Carlos will use the same session key.

If an attacker is able to break Alice's private key, all communication between Alice and
Bob, and in fact between Alice and anyone else, is compromised.

This is not perfect forward secrecy. Hence, to use ECDH with keys while retaining pefect
forward secrecy, each participant must generate a new key pair each message. This is not
an easy task. It is much easier to obtain a certificate for a particular public key, and
have each sender use that certificate to extract a public key. And then each sender can
use their private key, and send to the recipient their certificate.

In order to have forward perfect secrecty and avoid the man-in-the-middle attack, it seems
that each time anyone wants to send someone else a message, they must generate a new key
pair and obtain a new cert. And then the sender must contact the recipient and request
that they generate a new key pair and obtain a new certificate. Only then can someone
start a new session.

## Solution: signatures

The solution is not to create a new certificate for each ECDH public key, but rather to
sign messages containing the public value.

Alice has a key pair with a certificate. Let's say her key pair is ECC. Because she will
be using it to sign, let's call this her ECDSA key. When she wants to communicate with
Bob, she performs this modified classical DH.

* Phase 1: Alice generates a new EC key pair.
    * She sends this newly generated ECDH public key to Bob, this message signed using her
      ECDSA private key. The message also contains her cert.
    * Bob gets this message and verifies it using Alice's cert (chaining to a trusted root).
* Phase 1: Bob generates a new EC key pair.
    * Bob sends to Alice his public value, signed using his public key (it can be RSA). The
      message contains his cert.
    * Alice gets this message and verifies it using Bob's cert (chaining to a trusted root).
* Phase 2: Both Alice and Bob have the necessary elements to each derive the session key.

If Eve intercepts a message and forwards her own public value, it will be rejected if it
is not signed. If she signs it using her own private key, then it will not verify or will
not chain to the trusted root, and will be rejected.

## Conclusion

What this all means is that to use ECDH with protections against the man-in-the-middle
attack, you must combine it with certificates. To easily achieve perfect forward secrecy,
you should sign the messages that contain the public values (as opposed to obtaining a
certificate for every message).

Incidentally, this is one of the reasons many systems require multiple keys and certs.
That is, a standard will often specify that each participant create a signing key and cert
and a separate encryption key and cert.

If a signing key is broken, an attacker can impersonate a participant in the future, but
still cannot read any older messages.
