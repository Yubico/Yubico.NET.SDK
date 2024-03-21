---
uid: YubiHsmAuthInteractingYubiHsm2
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

# Interacting with a YubiHSM 2

`Yubico.YubiKey.YubiHsmAuth` provides an easy way to programmatically manage the YubiHSM Auth application on the YubiKey. While this SDK also supports the calculation of session keys, it is instead recommended for developers to rely on the [YubiHSM SDK](https://docs.yubico.com/hardware/yubihsm-2/hsm-2-user-guide/) for interactions with the YubiHSM 2.

## YubiHSM SDK

Once credentials are added to the YubiHSM Auth application, use the [YubiHSM SDK](https://docs.yubico.com/hardware/yubihsm-2/hsm-2-user-guide/) (and bundled tools) to establish a secure session with a YubiHSM 2 device and perform operations on it.

The [YubiHSM Shell](https://docs.yubico.com/hardware/yubihsm-2/hsm-2-user-guide/hsm2-sdk-tools-libraries.html#yubihsm-shell) tool supports authentication with YubiHSM Auth credentials in both interactive mode and command-line mode. Once the user is authenticated, all YubiHSM Shell commands can be used. Refer to [this guide](https://docs.yubico.com/hardware/yubikey/yk-5/tech-manual/yubihsm-auth.html#using-yubihsm-auth-with-yubihsm-shell) for more information.

It is also possible to use low-level commands to communicate natively with a YubiHSM 2. The individual commands (documented [here](https://docs.yubico.com/hardware/yubihsm-2/hsm-2-user-guide/hsm2-cmd-reference.html)) are implemented by the [libyubihsm](https://docs.yubico.com/hardware/yubihsm-2/hsm-2-user-guide/hsm2-sdk-tools-libraries.html#libyubihsm) C library.

## YubiHSM 2 secure channel

In order to establish an encrypted and authenticated session with a YubiHSM 2, the YubiHSM Auth application must follow the YubiHSM 2 secure channel protocol. This protocol is based on the Global Platform Secure Channel Protocol 03 (SCP03), but there are two important differences:

- The YubiHSM 2 secure channel protocol does not use APDUs, so the commands and possible options do not match the complete SCP03 specification.
- SCP03 uses a set of three long-lived AES keys, while the YubiHSM 2 secure channel uses a set of two long-lived AES keys.

The two long-lived keys used in the YubiHSM 2 authentication protocol include an ENC key and a MAC key. In order to successfully create a secure channel, the long-lived keys in the YubiHSM Auth credential and YubiHSM 2 device must be identical.

Those long-lived key sets are used by the YubiHSM Auth application to derive a set of three session-specific AES-128 keys using the challenge-response protocol as defined in SCP03:

- Session Secure Channel Encryption Key (S-ENC): used for data confidentiality.
- Secure Channel Message Authentication Code Key for Command (S-MAC): used for data and protocol integrity.
- Secure Channel Message Authentication Code Key for Response (S-RMAC): used for data and protocol integrity.

Session-specific keys can be requested from the YubiHSM Auth application and are returned to the caller. These session-specific keys are used to encrypt and authenticate commands and responses with a YubiHSM 2 device during a single session. The session keys are discarded afterwards.