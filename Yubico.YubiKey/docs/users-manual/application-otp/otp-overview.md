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

# OTP Application Overview


## OTP Application Configurations

The OTP application on the YubiKey allows developers to program the device with a variety of configurations through two "slots." Each slot may be programmed with a single configuration — no data is shared between slots, and each slot may be protected with an access code to prevent modification.

"OTP application" is a bit of a misnomer. While OTP (one-time password) functionality is the focus of the application, the slots may be programmed with other configurations. Supported configurations include:

- Yubico OTP
- Initiative for Open Authentication HMAC-based OTP (OATH HOTP)
- Static password
- Challenge-response (using the HMAC-SHA1 or Yubico OTP algorithms)

YubiKeys that support NFC also include a configurable NDEF (NFC Data Exchange Format) tag. This tag can be configured to point to a slot that is programmed with a Yubico OTP or an OATH HOTP in order to make the OTP easily readable in NFC authentication scenarios.

Off-the-shelf YubiKeys come with the first slot preconfigured with a Yubico OTP (registered with the [YubiCloud validation service](https://www.yubico.com/products/yubicloud/)) and the second slot empty.


## .NET SDK Functionality

The SDK is designed to enable developers to accomplish common YubiKey OTP application configuration tasks:

- Program a slot with a Yubico OTP credential
- Program a slot with a static password
- Program a slot with a challenge-response credential
- Calculate a response code for a challenge-response credential
- Delete a slot’s configuration
- Program a slot with an HMAC-SHA1 OATH-HOTP credential
- Retrieve a slot’s status
- Configure NDEF to use a slot to generate an OTP
- Update slot settings
- Swap slot configurations
