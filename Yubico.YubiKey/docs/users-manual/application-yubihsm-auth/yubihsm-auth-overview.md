---
uid: YubiHsmAuthOverview
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

# YubiHSM Auth overview

YubiHSM Auth is a YubiKey CCID application that stores the long-lived [credentials](xref:YubiHsmAuthCredential) used to establish secure sessions with a YubiHSM 2. The secure session protocol is based on Secure Channel Protocol 3 (SCP03). YubiHSM Auth is supported by YubiKey firmware version 5.4.3.

YubiHSM Auth uses hardware to protect these credentials. In addition to providing robust security for the YubiHSM Auth application itself, this hardware protection subsequently increases the security of the default password-based solution for YubiHSM 2's authentication.

## Credential

Each credential contains a cryptographic key set which is used to establish secure sessions with a YubiHSM 2. Adding or deleting a credential requires a 16-byte management key to be supplied. Once a credential is added, it cannot be edited.

Each credential has a 16-byte password which is required when calculating session keys. There is a limit of eight retries, after which the credential will be permanently deleted. Optionally, the credential may be configured to also require touch when calculating session keys. This proof of user presence provides an additional layer of security.

For more information, see the [credential overview](xref:YubiHsmAuthCredential).

## YubiHSM 2 secure channel

In order to establish an encrypted and authenticated session with a YubiHSM 2, the YubiHSM Auth application must follow the YubiHSM 2 secure channel protocol. This protocol is based on the Global Platform Secure Channel Protocol ‘03’ (SCP03), but there are two important differences:

- The YubiHSM 2 secure channel protocol does not use APDUs, so the commands and possible options do not match the complete SCP03 specification.
- SCP03 uses a set of three long-lived AES keys, while the YubiHSM 2 secure channel uses a set of two long-lived AES keys.

The two long-lived keys used in the YubiHSM 2 authentication protocol include an ENC key and a MAC key. In order to successfully create a secure channel, the long-lived keys in the YubiHSM Auth credential and YubiHSM 2 device must be identical.

Those long-lived key sets are used by the YubiHSM Auth application to derive a set of three session-specific AES-128 keys using the challenge-response protocol as defined in SCP03:

- Session Secure Channel Encryption Key (S-ENC): used for data confidentiality.
- Secure Channel Message Authentication Code Key for Command (S-MAC): used for data and protocol integrity.
- Secure Channel Message Authentication Code Key for Response (S-RMAC): used for data and protocol integrity.

Session-specific keys can be requested from the YubiHSM Auth application and are returned to the caller. These session-specific keys are used to encrypt and authenticate commands and responses with a YubiHSM 2 device during a single session. The session keys are discarded afterwards.