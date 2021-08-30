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

# What's new in the SDK?

Here you can find all of the updates and release notes for published versions of the SDK.

## 1.0.x Releases

### 1.0.0

Release date: August 30th, 2021

This is the first official, generally available release of the YubiKey SDK for Desktop aimed at the .NET developer community and
ecosystem. In this release, the OTP, OATH, and PIV applications are fully supported. Please refer to those application's sections
within the documentation for more information.

The [Overview of the SDK](xref:OverviewOfSdk) page also goes into much more depth on what this SDK contains, and how it is structured.

Lastly, this SDK has also been released as open source under the Apache 2.0 license. Please refer to the `CONTRIBUTING.md` file in the
root of the repository for information on how you can contribute.

### 1.0.0-Beta.20210721.1

Release date: July 21st, 2021

This is the beta refresh of the YubiKey Desktop SDK. In this release, the OATH, PIV and OTP applications are now fully supported.
Many OTP features have been completed since the last beta release, we have implemented:

- HOTP
- Challenge-Response with HMAC and Yubico OTP algorithms
- Calculate Challenge-Response with touch notification
- Reading and writing NDEF tags,
- Delete, swap and update OTP slot functionalities.

### 1.0.0-Beta.20210618.1

Release date: June 18th, 2021

This is the first public preview of the new YubiKey Desktop SDK. This SDK allows you to integrate the YubiKey into your .NET
based application or workflow. The OATH and PIV applications are fully supported, with partial support for Yubico OTP. Full
support for Yubico OTP will come in the next beta refresh. There is support for macOS and Windows, over both USB and
Near-Field Communication (NFC).

As the first public beta, the API surface is considered stable. However, if sufficient feedback is received, some minor breaking
changes may occur prior to general availability (GA).

### 1.0.0-Alpha.20210521.1

Release date: May 21st, 2021

This was a limited availability preview.

- A bug was addressed in the smart card reader code which computed an incorrect buffer offset based on the architecture
  of the computer running the YubiKey SDK software.
- OATH functionality is now "feature complete."
- YubiKey device management functionality has been added.

### 1.0.0-Alpha.20210505.1

Release date: May 5th, 2021

This was a limited availability preview.

- PIV functionality is now "feature complete". OATH APIs are partially available.
- A bug was identified and addressed where the default PIV management key could not be used due to a `CryptographicException`
  being thrown by the .NET TripleDES implementation. This is because the default management key is considered a "weak" key.
- A design re-review of the PivSession class identified an over-use of the TryParse pattern. This has been addressed.

Breaking API changes in Yubico.YubiKey:

- Several methods on the `PivSession` class have been renamed as they no longer follow the TryParse pattern.
- `KeyEntryData` and `KeyEntryRequest` have been moved from the `Yubico.YubiKey.Cryptography` namespace to the
  `Yubico.YubiKey` namespace.
- Information previously found in `IYubiKey.DeviceInfo` has been collapsed into the YubiKey object itself by means of
  the `IYubiKeyDeviceInfo` interface.
- Naming of the cryptography delegates have been updated to reflect the .NET Framework Design Guidelines naming conventions.
  For example, `CreateRng` and `CreateTripleDes` have been renamed to `RngCreator` and `TripleDesCreator` respectively.

### 1.0.0-Alpha.20210329.1

Release date: March 29th, 2021

This was a limited availability preview.

- A bug was found and addressed that affected the stability of smart card connections. This would affect any
  command that was sent from the PIV or OATH applications, and would have a higher likelihood of occurring
  for long-running operations. The bug would result in certain method calls failing sporadically.

Breaking API changes in Yubico.YubiKey:

- The `ConnectionType` enum has been renamed to `Transport`
- `YubiKeyEnumerator.GetYubiKeys()` has been replaced by `YubiKey.FindAll()`
- There is no longer a concrete YubiKey instance type. Interaction should be done through the `IYubikey` interface and
  related types.
- Certain constants related to the OTP NDEF "file ID" have been pulled out into an enumeration called `NdefFileId`
- `CreateAttestationCertificateCommand` and `CreateAttestationCertificateResponse` classes have been renamed to
  `CreateAttestationStatementCommand` and `CreateAttestationStatementResponse`, respectively, to reflect the
  terminology already established in published specifications and documentation.

### 1.0.0-Alpha.20210222.1

Release date: February 22nd, 2021

This was a limited availability preview.

- Enumeration of YubiKeys on macOS and Windows platforms
- macOS supports CCID communication only. Windows supports CCID and HID.
- OTP, OATH, PIV, and SCP03 have full low-level command support. All APDUs are mapped to a C# class.
- PIV high level commands are partially implemented. Certificate enrollment scenarios were prioritized.
