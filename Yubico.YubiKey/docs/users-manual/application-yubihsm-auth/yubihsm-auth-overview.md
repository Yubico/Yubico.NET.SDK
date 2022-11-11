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

For a guide on establishing a secure connection and performing operations on a YubiHSM 2, please refer to [Interacting with a YubiHSM 2](xref:YubiHsmAuthInteractingYubiHsm2).

## Management key

A 16-byte management key is required when adding or deleting credentials from the YubiHSM Auth application. The default value of the management key is all zeros, and should be changed before using the application.

There is a limit of 8 attempts to authenticate with the management key before the management key is blocked. Once it is blocked, the application itself must be reset before authentication can be attempted again. Supplying the correct management key before the key is blocked will reset the retry counter to 8.

## Credential

Each credential contains a cryptographic key set which is used to establish secure sessions with a YubiHSM 2. Once a credential is added, it cannot be changed or modified in any way.

Each credential has a 16-byte password which is required when calculating session keys. There is a limit of 8 retries, after which the credential will be permanently deleted. Supplying the correct password before the credential is deleted will reset the retry counter to 8.

Optionally, the credential may be configured to also require touch when calculating session keys. This proof of user presence provides an additional layer of security.

For more information, see the [credential overview](xref:YubiHsmAuthCredential).