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

YubiHSM Auth is a YubiKey CCID application that stores the long-lived credentials used to establish secure sessions to a YubiHSM 2. The secure session protocol is based on Secure Channel Protocol 3 (SCP03). YubiHSM Auth is supported by YubiKey firmware version 5.4.3.

YubiHSM Auth uses hardware to protect the long-lived credentials for accessing a YubiHSM 2. This increases the security of the authentication credentials, as compared to the authentication solution for the YubiHSM 2 based on software credentials derived from the Password-Based Key Derivation Function 2 (PBKDF2) algorithm with a password as input.

## Credentials and PIN codes

Each YubiHSM Auth credential is comprised of two AES-128 keys which are used to derive the three session-specific AES-128 keys. The YubiHSM Auth application can store up to 32 YubiHSM Auth credentials in the YubiKey.

Each YubiHSM Auth long-lived credential is protected by a 16-byte user access code. This code must be provided to the YubiKey for each YubiHSM Auth operation, which requires access to the YubiHSM Auth long-lived credential in order to derive the session-specific AES-128 keys.

Storing or deleting YubiHSM Auth credentials requires a separate 16-byte admin access code.

Each access code has a limit of eight retries and an optional verification of user presence (touch).

## YubiHSM 2 secure channel

The YubiKey's YubiHSM Auth application can be used to establish an encrypted and authenticated session with a YubiHSM 2. The YubiHSM 2 secure channel protocol is based on the Global Platform Secure Channel Protocol ‘03’ (SCP03), but there are two important differences:

- The YubiHSM 2 secure channel protocol does not use APDUs, so the commands and possible options do not match the complete SCP03 specification.
- SCP03 uses key sets with three long-lived AES keys, while the YubiHSM 2 secure channel uses key sets with two long-lived AES keys.

The YubiHSM 2 authentication protocol uses a set of static credentials called a long-lived key set. This consists of two AES-128 keys:

- ENC: used for deriving keys for command and response encryption, as specified in SCP03.
- MAC: used for deriving keys for command and response authentication, as specified in SCP03.

The identical long-lived keyset is protected in the YubiHSM 2 and in the YubiKey YubiHSM Auth application.

Those long-lived key sets are used by the YubiHSM Auth application to derive a set of three session-specific AES-128 keys using the challenge-response protocol as defined in SCP03:

- Session Secure Channel Encryption Key (S-ENC): used for data confidentiality.
- Secure Channel Message Authentication Code Key for Command (S-MAC): used for data and protocol integrity.
- Secure Channel Message Authentication Code Key for Response (S-RMAC): used for data and protocol integrity.

The YubiHSM Auth session-specific keys are output from the YubiKey to the calling library, which uses the session keys to encrypt and authenticate commands and responses during a single session. The session keys are discarded afterwards.