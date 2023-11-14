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

# What's new in the SDK?

Here you can find all of the updates and release notes for published versions of the SDK.

## 1.9.x Releases

### 1.9.1

Release date: November 14th, 2023

Bug fixes:

- **SCard handle contention**. Previously, the SDK was opening all smart card handles with
  shared permissions. This is typically the "nice" thing to do, however this allowed other
  applications and services (such as smart card minidrivers) to alter the current state of
  the YubiKey without the SDK's knowledge. This would cause random failures and exceptions
  to occur when using the SDK. We now open the handle exclusively. Callers should take care
  to not keep a YubiKey connection or session open longer than is needed.
- **Config changes over FIDO2**. The YubiKey management commands should have been available
  over all three logical USB interfaces. The SDK will typically use the first available
  interface, giving some preference to Smart Card. On FIDO only devices, this operation would
  have failed as the commands were not properly wired up over this interface.

Miscellaneous:

- **Dependency updates**. The dependencies of the SDK were updated to the latest packages
  available. Since the SDK itself does not take many dependencies outside of the .NET Base
  Class Libraries (BCL), there should not be much of a noticeable impact. The two that
  affected the SDK itself (and not just test code) were:
  - `Microsoft.Extensions.Logging.Abstractions` (6.0.1 -> 7.0.1)
  - `System.Memory` (4.5.4 -> 4.5.5)

### 1.9.0

Release date: October 13th, 2023

Features:

- **FIDO2 PIN Config**. The PIN config feature, if supported by the
  connected YubiKey, is a set of operations: set the minimum PIN
  length, force a PIN change, and return a minimum PIN length to a
  relying party.

- **FIDO2 GUI option for sample code**. There is now a version of the
  FIDO2 sample code that uses Windows Forms. This GUI version of the
  sample code is provided mainly to demonstrate how to build touch and
  fingerprint notifications in a KeyCollector. This sample code runs
  only in a Windows environment.

- **SCP03 CMAC added to CryptographyProviders**. SCP03 operations rely
  on the AES-CMAC algorithm, and, starting in this release, they will call on the
  CryptogrphyProviders class to retrieve an implementation. The default
  implementation uses OpenSSL.

- **SCP03 keys**. This release adds the ability to change SCP03 key
  sets. This includes replacing the default key set, adding new key
  sets, and removing key sets. This is done using the new Scp03Session
  class.

- **SCP03 architecture**. The process for building an SCP03 connection was updated.
  The previous method ([Yubico.YubiKey.YubiKeyDeviceExtensions.WithScp03()](xref:Yubico.YubiKey.YubiKeyDeviceExtensions.WithScp03%28Yubico.YubiKey.YubiKeyDevice%2CYubico.YubiKey.Scp03.StaticKeys%29)) is now deprecated, and
  the new method ([Yubico.YubiKey.IYubiKeyDevice.ConnectScp03()](xref:Yubico.YubiKey.IYubiKeyDevice.ConnectScp03%28Yubico.YubiKey.YubiKeyApplication%2CYubico.YubiKey.Scp03.StaticKeys%29)) simply requires passing in the SCP03 key set to the
  PivSession constructor. It is also possible to build an
  IYubiKeyConnection that uses SCP03 via [Yubico.YubiKey.Piv.PivSession()](xref:Yubico.YubiKey.Piv.PivSession.%23ctor%28Yubico.YubiKey.IYubiKeyDevice%2CYubico.YubiKey.Scp03.StaticKeys%29).

- **SCP03 documentation**. The User's Manual article on SCP03 was
  updated to provide more comprehensive information.

## 1.8.x Releases

### 1.8.0

Release date: June 30th, 2023

Features:

- **FIDO2 Bio Enroll**. This allows enrolling and enumerating
  fingerprint templates. In addition, the SDK implemented fingerprint
  verification for FIDO2 and incorporated it into the automatic
  verification process.

- **FIDO2 Authenticator Config Operations**. This is a series of new
  methods that allow the programmer to perform some esoteric FIDO2
  configuration operations, such as enabling enterprise attestation and
  increasing the minimum PIN length.

- **FIDO2 Update Credential Management to Support CredentialMgmtPreview**. 
  Some older YubiKeys do not support the "credential management" feature
  (enumerate credentials, delete credentials, and others), but do
  support the "credential management preview" feature. This is the same
  as "credential management" except that the preview version does not
  include "Update User Info". The credential management commands and
  Fido2Session methods now support "Preview", meaning calls to the
  credential management methods (e.g. Fido2Session.EnumerateRelyingParties)
  will work on older YubiKeys that support "CredentialMgmtPreview",
  just as the newer YubiKeys.

- **FIDO2 HMAC Secret Extension and CredProtect Extension**. These are
  oft-used extensions, and the SDK now has methods to make using them
  easier (e.g. MakeCredentialParameters.AddHmacExtension and
  AuthenticatorData.GetHmacSecretExtension).

- **FIDO2 Encoded Attestation** The full encoded attestation statement
  is available when making a credential. This is useful if you are
  implementing or interoperating with the WebAuthn data types. That is,
  it is often easier to copy this field in its encoded form rather than
  using the parsed properties.

- **FIDO2 Update Sample Code**. The FIDO2 sample project now contains
  examples that perform bio enroll, credential management,
  authenticator config, HMAC secret, and credProtect operations.

- **OTP Documentation Updates**. There are new articles and information
  about slots (e.g. access codes, deleting), new articles on Hotp (what
  it is and programming an Hotp credential), new articles on static
  passwords (what it is and programming a slot to contain a static
  password), and a new article on updating slots, including manual
  update.

Bug Fixes:

- NFC response code in FIDO2 now handled properly.

## 1.7.x Releases

### 1.7.0

Release date: March 31st, 2023

Features:

- **FIDO2 Credential Management**. The credential management feature allows a client application to retrieve 
  information about discoverable FIDO2 credentials on a YubiKey, update user information, and delete credentials.
  This includes enumerating the relying parties and user information for all the discoverable credentials.

## 1.6.x Releases

### 1.6.1

Release date: February 2nd, 2023

Features:

- Added KeyCollector variants to the YubiHsmAuthSession class for methods which require credential gathering.
  - [TryGetAes128SessionKeys](xref:Yubico.YubiKey.YubiHsmAuth.YubiHsmAuthSession.TryGetAes128SessionKeys(System.String,System.ReadOnlyMemory{System.Byte},System.ReadOnlyMemory{System.Byte},Yubico.YubiKey.YubiHsmAuth.SessionKeys@))
  - [TryAddCredential](xref:Yubico.YubiKey.YubiHsmAuth.YubiHsmAuthSession.TryAddCredential(Yubico.YubiKey.YubiHsmAuth.CredentialWithSecrets))
  - [TryDeleteCredential](xref:Yubico.YubiKey.YubiHsmAuth.YubiHsmAuthSession.TryDeleteCredential(System.String))
  - [TryChangeManagementKey](xref:Yubico.YubiKey.YubiHsmAuth.YubiHsmAuthSession.TryChangeManagementKey)

Bug fixes:

- Fixed a bug which prevented large responses from the OATH application from being received by the SDK. Fixes
  [GitHub Issue #35](https://github.com/Yubico/Yubico.NET.SDK/issues/35).
- The YubiKey can now accept a zero-length NDEF text prefix, which was previously prevented by the SDK.
- Added an MSBuild target that instructs .NET Framework-based builds to automatically copy the correct
  version of `Yubico.NativeShims.dll` into the build's output directory. This requires the use of `PackageReferences`
  in the consuming project's csproj file in order to properly consume this dependency transitively through
  the `Yubico.YubiKey` package. `Packages.config` is not supported. Fixes
  [GitHub Issue #11](https://github.com/Yubico/Yubico.NET.SDK/issues/11).
- Addressed a difference in behavior found in EcdsaVerify that caused .NET Framework users to receive
  an exception. Fixes [GitHub Issue #36](https://github.com/Yubico/Yubico.NET.SDK/issues/36).

### 1.6.0

Release date: January 16th, 2023

Features:

- **FIDO2 Credential Blobs and Large Blob support.** FIDO2 allows applications to store additional information
  alongside a credential. Credential Blobs and Large Blobs are two separate, though related, features for achieving
  this.

Bug fixes:

- Added an MSBuild rule for projects that target .NET Framework 4.x that now automatically copy the correct
  version of Yubico.NativeShims.dll into the build directory. This addresses the "Missing DLL" issue that .NET
  Framework users would encounter. Fixes [GitHub Issue #11](https://github.com/Yubico/Yubico.NET.SDK/issues/11).
- Addressed an issue where the SDK would enumerate FIDO devices on Windows despite being un-elevated. Windows requires
  process elevation in order to communicate with FIDO devices. The SDK would display one or more YubiKeys with
  incorrect properties as a result. Fixes [GitHub Issue #20](https://github.com/Yubico/Yubico.NET.SDK/issues/20).
- A difference in behavior between .NET Framework 4.x and .NET 6 caused OAEP padding operations to fail for projects
  running on .NET Framework 4.x. The SDK has been updated to work around this difference in behavior and should
  now work for all supported versions of .NET. Fixes [GitHub Issue #33](https://github.com/Yubico/Yubico.NET.SDK/issues/33).
- The YubiKey requires a short delay when switching between its USB interfaces. Switching too quickly can result
  in failed operations and other strange behaviors. The SDK will now automatically wait the required amount of
  time to ensure stable communication with the YubiKey. Note that this may cause the first operation or command
  sent to the YubiKey to appear slow. Subsequent calls to the same application will not be affected.
  Fixes [GitHub Issue #34](https://github.com/Yubico/Yubico.NET.SDK/issues/34).

## 1.5.x Releases

### 1.5.1 (Yubico.YubiKey), 1.5.2 (Yubico.NativeShims)

Release date: November 18th, 2022

Bug fixes:

- Fixed a bug in Yubico.NativeShims where a function parameter wasn't properly initialized. This
  affected enumeration of smart cards in some cases.
- Upgraded System.Formats.CBOR to 7.0.0 now that .NET 7 has been released.
- FIDO2 re-initializes the auth protocol after a failed PIN attempt. This now matches spec behavior.
- Upgraded the version of OpenSSL that Yubico.NativeShims uses to 3.0.7. Note: the SDK was *not* affected by any of
  the November 2022 security advisories.

### 1.5.0

Release date: October 28th, 2022

Features:

- **YubiHSM Auth.** YubiHSM Auth is a YubiKey application that stores the long-lived credentials used to
  establish secure sessions with a YubiHSM 2. The secure session protocol is based on Secure Channel Protocol
  3 (SCP03). The SDK adds full support for this application. This includes both management of credentials
  and creating the session keys for communicating with a YubiHSM 2.
- **FIDO2 partial support.** The basic building blocks for FIDO2 are now available. Making credentials and
  generating assertions are now possible using the SDK, along with verification using both PIN and biometric
  touch. Both PIN protocols are also available. Future releases will add additional FIDO2 functionality.

## 1.4.x Releases

### 1.4.2

Release date: September 27th, 2022

Bug fixes:

- The UWP .NET Native toolchain has slightly different rules around P/Invoke name resolution than normal .NET,
  which caused UWP projects to crash when enumerating YubiKeys. Additional annotation has been added to some of
  the Windows API P/Invoke definitions to help the native compiler resolve the APIs and prevent these crashes.


### 1.4.1

Release date: September 12th, 2022

Bug fixes:

- TOTP calculations in the OATH application were incorrect. The OATH application was mistakenly using a random
  challenge instead of the time for calculation of TOTP credentials. This has been resolved.
- The device listener was attempting to modify a collection that it was also iterating over in a loop. This is
  not allowed by .NET. The list to iterate over is now a clone of the original list.
- MacOS does not always return properties of HID devices (including Vendor and Product IDs). This can cause
  the enumeration code path to fail on certain MacOS based devices, including Apple Silicon devices. The SDK now
  expects all HID properties to be optional and will skip over devices that don't have the minimum set required.


### 1.4.0

Release date: June 30th, 2022

Features:

- **AES-based PIV management keys**. Newer versions of the YubiKey (firmware 5.4.2 and above) have the ability
  to use AES-based encryption for the management key. This is in addition to the existing Triple-DES based
  management keys. Read the updated [PIN, PUK, and Management Key](xref:UsersManualPinPukMgmtKey) article for
  more information.
- **FIDO U2F**. Applications using this SDK can now use the YubiKey's FIDO U2F application. This means that
  the SDK is now also enumerating the HID FIDO device, in addition to the HID keyboard and smart card devices
  exposed by the YubiKey. Use this feature if your application wants to handle U2F registration or authentication.
  Note that on Microsoft Windows, applications must run with elevated privileges in order to talk to FIDO devices.
  This is a requirement set in place by Microsoft. See [FIDO U2F overview](xref:FidoU2fOverview) for more information.
- **SCP03**. Secure Channel Protocol 03 (also referred to as SCP03) is a Global Platform specification that
  allows clients of smart cards to encrypt all traffic to and from the card. Since the YubiKey can act as a smart
  card, this means that it is now possible to encrypt all traffic for the PIV application.
  In order for this to work, however, your YubiKey must be pre-configured for this feature. Read more about
  [SCP03 here](xref:UsersManualScp03).
- **Debian, RHEL, and CentOS support**. Our testing of Linux platforms has expanded to include the Debian,
  Red Hat Enterprise Linux (RHEL), and CentOS distributions. Please read [running on Linux](xref:RunningOnLinux)
  for more details.

Bug fixes:

- High CPU usage when the SDK can't connect to the smart card subsystem.
- Yubico.NativeShims DLL not found when using .NET Framework 4.x. Note that there is an additional issue
  with `packages.config` that is not able to be resolved. Developers are urged to upgrade to the newer
  `<PackageReferences>` method if at all possible. Manual installation of the Yubico.NativeShims library
  will be necessary if you are stuck on `packages.config`.
- "Duplicate resource" error when compiling for UWP applications.


## 1.3.x Releases

### 1.3.1

Release date: April 13th, 2022

Bug fixes:

- Applications targeting .NET Core 3.x, .NET 5, or higher would encounter an exception that said
  `Microsoft.BCL.HashCode` could not be found. Adding that NuGet reference manually would work around
  the issue. This issue has now been addressed and a work around is no longer required.
- An exception would be thrown if a YubiKey with a non-visible serial number was plugged in. This was
  a regression in behavior and has now been fixed.
- The reference to the newly introduced assembly `Yubico.NativeShims` was pinned to a pre-release version. This
  has been fixed and now points to the latest publicly listed package.

### 1.3.0

Release date: March 31st, 2022

This release brings enhancements across the SDK.

Features:

- **PIV Objects**. There is now a new namespace, `Yubico.YubiKey.Piv.Objects` that contains high level
  representations of common PIV objects such as [CHUID](xref:Yubico.YubiKey.Piv.Objects.CardholderUniqueId),
  [CCC](xref:Yubico.YubiKey.Piv.Objects.CardCapabilityContainer), and
  [KeyHistory](xref:Yubico.YubiKey.Piv.Objects.KeyHistory). These objects, paired
  with two new methods [ReadObject](xref:Yubico.YubiKey.Piv.PivSession.ReadObject*) and
  [WriteObject](xref:Yubico.YubiKey.Piv.PivSession.WriteObject(Yubico.YubiKey.Piv.Objects.PivDataObject))
  provide a much easier mechanism for interacting with common PIV objects.
- **Direct credential gathering**. Some applications, such as PIV and OATH, require a user to authenticate using
  a PIN or password. The SDK has a robust mechanism called the [KeyCollector](xref:UsersManualKeyCollector)
  for gathering credentials. Supplying a key collector will mean that your application will always be notified
  for the right credential at the right time. Sometimes, though, you may not want to use a key collector, and
  supplying the credential directly to the session is preferable. For this, we've added overloads to
  the most common credential gathering routines (e.g. [TryVerifyPin](xref:Yubico.YubiKey.Piv.PivSession.TryVerifyPin(System.ReadOnlyMemory{System.Byte},System.Nullable{System.Int32}@)))
  that allow you to provide the credential directly, without the need for a key collector.
- **Feature queries**. Rather than keeping track of YubiKey firmware versions and other properties, your
  application can now directly [query a YubiKey](xref:Yubico.YubiKey.YubiKeyFeatureExtensions.HasFeature(Yubico.YubiKey.IYubiKeyDevice,Yubico.YubiKey.YubiKeyFeature))
  to see whether it supports a particular feature.
- **Protected PIV management keys**. Some applications, such as YubiKey Manager or the YubiKey Smart
  Card Mini-Driver, may opt to [only use the PIV PIN](xref:UsersManualPivPinOnlyMode). It does this by
  storing the PIV management key in a PIN protected object and using the PIN to unlock the smart card. The SDK
  has been enlightened to these modes of operations and the PivSession will automatically detect and act
  appropriately. That is, the KeyCollector will automatically ask for a PIN instead of the Management key
  for keys that are configured in this way. No extra handling is required by your application.
- **Yubico.NativeShims**. A new internal-use library has been introduced to help facilitate better
  interoperability with the underlying native platform libraries. No functional changes should have
  occurred as a result of this change. This will instead open the door to broader support of platforms,
  specifically with regards to Linux distributions.


Bug fixes:

- Fixed a high CPU usage issue on Windows that was introduced in 1.2.0. This bug was encountered when multiple
  YubiKeys were plugged into a single computer, and the user reduced the number of keys to one.
- Fixed an issue where the interfaces and applications were not being reported correctly for YubiKey NEOs.

## 1.2.x Releases

### 1.2.0

Release date: February 7th, 2022

This release adds support for device notifications. Now, applications can be notified in real-time that a
YubiKey has been inserted or removed from the computer. Read more about how device notifications work and
how to use them on [this page](xref:DeviceNotifications).

Device notifications are supported on all currently supported platforms.

## 1.1.x Releases

### 1.1.0

Release date: December 3rd, 2021

This release marks the beginning of support for Linux platforms. The primary target for testing has been
against Ubuntu Linux 20.04 LTS and 21.10. Other Ubuntu-based distributions should work as well. Additional Linux
platforms may work based on their [ABI](https://en.wikipedia.org/wiki/Application_binary_interface) compatibility
with Ubuntu. Further distributions will be added to the supported list once thorough testing on those platforms
has been completed.

Limited smart card only support may be present for additional distributions, as they depend on the
PCSC-lite library.

Some symlinks may need to be present in order for the .NET runtime to find the appropriate system
libraries (such as pcsc-lite, udev, etc.) Information about how to create these links can be found
on [this page](xref:YubiKeyTransportHIDKeyboard).

## 1.0.x Releases

### 1.0.2

Release date: October 26th, 2021

Added Authenticode signing to the release process. Assemblies are now signed in addition to the NuGet package.

No code changes in this release.

### 1.0.1

Release date: October 1st, 2021

Bug fixes:
- PIV: Fixed an issue that was preventing the SDK from allowing attestation to occur on certain slots.
- OATH Sample code: Fixed an issue that was causing an exception to be thrown during `RunGetCredentials`.
- PIV Sample code: Worked around an issue in the .NET BCL where certificate generation behavior was different on macOS from Windows.

### 1.0.0

Release date: August 30th, 2021

This is the first official, generally available release of the YubiKey SDK for Desktop aimed at the .NET developer community and
ecosystem. In this release, the [OTP](xref:OtpOverview), [OATH](xref:OathOverview), and [PIV](xref:PivOverview) applications are
fully supported. Please refer to those applications' sections within the documentation for more information.

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
