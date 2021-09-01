---
uid: OtpOverview
summary: *content
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

# OTP overview


## YubiKey OTP Application

The OTP application on the YubiKey allows developers to program the device with a variety of configurations through two "slots." Each slot may be programmed with a single configuration--no data is shared between slots, and each slot may be protected with an access code to prevent modification.

The slots are activated by touching the YubiKey during authentication. However, only one slot may be used at a time. Which slot is used is determined by the duration of touch. The first slot, or “short press” slot, is activated when the YubiKey is touched for 1 - 2.5 seconds. The second slot, or “long press” slot, is activated when the YubiKey is touched for 3 - 5 seconds.

"OTP application" is a bit of a misnomer. While the OTP (one-time password) protocol is the focus of the application, the slots may be programmed with additional configurations. The supported configurations include:

- Yubico OTP
- Initiative for Open Authentication HMAC-based OTP (OATH HOTP)
- Static password
- Challenge-response
- Near-Field Communication Data Exchange Format (NDEF)

Off-the-shelf YubiKeys come with the first slot preconfigured with a Yubico OTP (registered with the [YubiCloud validation service](https://www.yubico.com/products/yubicloud/)) and the second slot empty.


## .NET SDK Functionality

The SDK is designed to enable developers to accomplish common YubiKey OTP application configuration tasks:

- Program a slot with a Yubico OTP credential
- Program a slot with a static password
- Program a slot with a challenge-response credential
- Calculate a challenge-response credential
- Delete a slot’s configuration
- Program a slot with an HMAC-SHA1 OATH-HOTP credential
- Retrieve a slot’s status
- Configure NDEF to use a slot to generate an OTP
- Update slot settings
- Swap slot configurations
