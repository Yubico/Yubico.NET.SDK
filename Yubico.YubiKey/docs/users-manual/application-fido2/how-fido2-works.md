---
uid: HowFido2Works
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

# How FIDO2 works

At its foundation, FIDO2 is very similar to U2F. However, there are differences. For
example:

- In FIDO2 the private key is stored on the authenticator
- In FIDO2 the messages between entities are much more complex with more information
- In FIDO2 the PIN is encrypted before transmitting it to the authenticator
- FIDO2 has mechanisms for biometric authenticators (e.g. "on-board" fingerprint readers)

As with U2F, there are two main operations in FIDO2:

- Make a credential (registration)
- Get an assertion (authenticate)

First, the user registers the YubiKey and ties it to a particular account.

Second, when logging on, the user makes sure the appropriate YubiKey is inserted. During
login, the YubiKey, browser, and authentication server will communicate and perform the
steps necessary to authenticate. The user will likely need to tap the YubiKey in order to
complete authentication.

## The entities involved

There are three components in FIDO2

- The authenticator (YubiKey)
- The client (a browser, platform component, or application)
- The relying party (verifies the authentication process)

FIDO2 can only work when using clients that already have support. For example, suppose you
have an online account with a bank which has added FIDO2 support. Now you try to log into
that account using a Vivaldi browser on MacOS. That might not work because it is possible
that browser does not support FIDO2. You can log in using Safari or Chrome, because they
do have support.

The client is, of course, the "medium" through which the authenticator and relying party
communicate. However, the client also plays a role in verifying that the relying party is
correct, not a fake or attacker.

## Make a credential

The goal of registration is for the authenticator to provide a public key to the relying
party. This key is to be associated with an account.

The client provides relying party data (RpIdHash) to the authenticator. This is data the
authenticator will use during authentication. Later on, if the RpIdHash provided during
authentication does not match the RpIdHash from registration, then the authenticator will
return an error.

In order to register, the relying party supplies a challenge, which the authenticator
signs. That signature (an attestation statement), along with a certificate verifying the
private key used (attestation certificate), is sent to the relying party. The relying
party can verify the signature using the public key (verifying that the sender does indeed
have access to the private key) and then verify the public key using the attestation
certificate (which should chain to a known root).

Generally, an authentication server will have a database of accounts, each entry
containing a username and password info (not the password itself, but information that can
be used to verify the password). With FIDO2, each entry will also contain a public key as
well.

## Get assertion

The goal of authentication is for the authenticator to send, to the relying party, a value
that proves the authenticator is the one registered to the specified user.

In addition, the authenticator, working with the client, can verify the current relying
party (who the client is actually connected to) is the correct one.

The relying party sends a challenge to the client. The client bundles this challenge with
the relying party data (RpIdHash), and sends it to the authenticator.

The authenticator looks in its storage for the private key associated with the specified
relying party. If there is none, it cannot create an assertion, so returns an error. This
happens when the client is connected to an attacker's site.

If there is an entry for the given relying party, the authenticator will sign the
challenge (and other data), and return this signature.

The client passes the signture on to the relying party, which can verify the signature
using the public key associated with the account.

## More details

See the [FIDO2 Credentials](xref:Fido2Credentials) doc for more details.
