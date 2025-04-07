---
uid: YubiHsmAuthCredential
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

# Credential

The core component of the YubiHSM Auth application is the credential, which contains a cryptographic key set that is
used to establish secure sessions with a YubiHSM 2. Once a credential is added (
see [Add Credential](xref:YubiHsmAuthCmdAddCredential)), it cannot be changed or modified in any way.

Adding or deleting a YubiHSM Auth credential requires a 16-byte management key.

## Properties

Each credential contains four major properties. The [cryptographic key set](#cryptographic-key-set) is the property used
to calculate session keys. The other properties are used for identification ([label](#label)) and access
control ([password](#password) and [touch requirement](#touch-requirement)).

### Cryptographic key set

The YubiHSM 2 uses a [secure channel protocol](xref:YubiHsmAuthInteractingYubiHsm2#yubihsm-2-secure-channel) based on
symmetric keys. This means that both parties (YubiKey and YubiHSM 2) must have identical copies of the key set in order
to create a secure session.

The key set is a pair of AES-128 keys (each 128 bits in length):

- ENC: an AES-128 key used for deriving keys for command and response encryption
- MAC: an AES-128 key used for deriving keys for command and response authentication

The key set must be generated on the host machine and then stored on both the YubiKey (as a YubiHSM Auth credential) and
the YubiHSM 2.

### Label

This is a unique identifier for the credential. It is a UTF-8 encoded string and must be between 1 and 64 bytes long.

### Password

This 16-byte password is always required when using the credential to calculate session keys. There is a limit of eight
retries, after which the credential will be permanently deleted. The retry counter is reset when the correct password is
supplied.

### Touch requirement

Optionally, the credential may be configured to require touch when using the credential to calculate session keys. A
touch requirement provides an additional layer of security by ensuring a user is physically present and in control of
the YubiKey. The YubiKey has a capacitive touch sensor that cannot be controlled by software.
