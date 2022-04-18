---
uid: OathBackupCredentials
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

# How to backup credentials

Secrets on a YubiKey are, by design, write-only objects. This means that the shared secrets stored in the YubiKey can only be written into, and not read out, of the device. If a credential is to be copied, it must be known beforehand and either written down or copied before programming the YubiKey. 

It is not possible to create an exact copy of a YubiKey. It is possible to duplicate the credentials stored on the YubiKey if that credential was first generated outside of the YubiKey. When you add a credential, be sure you copy the shared secret key for that credential and store it in a safe place. 

The best ways to backup credentials are:
- Add credentials at the same time to multiple YubiKeys if you have them.
- Or save a copy of the QR code (capture the screen) or make a copy of the shared secret key and place them in secure storage until needed again. 

Otherwise, if you add credentials to one YubiKey and then later decide to buy another YubiKey for a backup, you must log into every account and go through the setup process again. To get a new credential for each account, delete the original credentials from the original YubiKey, and then add the new credentials to both YubiKeys.
