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

## 1.14.x Releases

### 1.14.0

Release date: September 17th, 2025

Features:

- The SDK has been updated to target .NET Framework 4.7.2, which provides broad reliability, security, and performance improvements. ([#274](https://github.com/Yubico/Yubico.NET.SDK/pull/274))

- A ``FailedApdu`` helper method has been added to the ``OtpErrorTransform`` pipeline to streamline the creation of failed APDU responses. Additionally, the sequence validation logic of the ``OtpErrorTransform`` pipeline has been updated to handle edge cases more effectively. ([#276](https://github.com/Yubico/Yubico.NET.SDK/pull/276))

- The NuGet package metadata has been updated for the ``Yubico.Core.csproj`` and ``Yubico.YubiKey.csproj`` files to improve discoverability, consistency, and clarity. The updates include new ``PackageId`` and ``PackageTags`` fields as well as a reorganized ``PackageReleaseNotes`` field. ([#265](https://github.com/Yubico/Yubico.NET.SDK/pull/265))

- ``ToString`` overrides have been introduced in the [CommandApdu](xref:Yubico.Core.Iso7816.CommandApdu) and [ResponseApdu](xref:Yubico.Core.Iso7816.ResponseApdu) classes to provide a human-readable string representation of their internal state. These changes improve debugging and logging of APDUs. ([#270](https://github.com/Yubico/Yubico.NET.SDK/pull/270))

Bug Fixes:

- Previously, [DeleteSlot()](xref:Yubico.YubiKey.Otp.OtpSession.DeleteSlot%28Yubico.YubiKey.Otp.Slot%29) and [DeleteSlotConfiguration()](xref:Yubico.YubiKey.Otp.OtpSession.DeleteSlotConfiguration%28Yubico.YubiKey.Otp.Slot%29) would throw an exception when the slot configuration was successfully removed as intended. This has been fixed so that no exception occurs following a successful ``DeleteSlot()`` or ``DeleteSlotConfiguration()`` operation. ([#276](https://github.com/Yubico/Yubico.NET.SDK/pull/276))

- Prerelease versions of Yubico packages are now prevented from being referenced into published NuGet packages. This fixes an issue where a prerelease version of Yubico.NativeShims was incorrectly referenced by Yubico.Core. ([#282](https://github.com/Yubico/Yubico.NET.SDK/pull/282))

- The ``OtpSession`` logger initialization has been updated to use the correct logger. ([#275](https://github.com/Yubico/Yubico.NET.SDK/pull/275))

- The detection logic for ``NativeShimsPath`` has been improved, ensuring that 32-bit processes on 64-bit systems are correctly mapped to the "x86" directory. ([#284](https://github.com/Yubico/Yubico.NET.SDK/pull/284))

Documentation:

- The [FIDO2 reset](xref:Fido2Reset) documentation has been updated to fix an error in the instructions and clarify timeout durations. ([#278](https://github.com/Yubico/Yubico.NET.SDK/pull/278))

- The documentation on [slot access codes](xref:OtpSlotAccessCodes) has been updated to improve clarity and examples. ([#268](https://github.com/Yubico/Yubico.NET.SDK/pull/268))

- The documentation on PIV [public](xref:UsersManualPublicKeys) and [private](xref:UsersManualPrivateKeys) keys has been updated with new sample code demonstrating how to use the latest factory methods. ([#245](https://github.com/Yubico/Yubico.NET.SDK/pull/245), [#272](https://github.com/Yubico/Yubico.NET.SDK/pull/272))

- The documentation for the [UseFastTrigger](xref:Yubico.YubiKey.Otp.OtpSettings%601.UseFastTrigger%28System.Boolean%29) method has been updated to clarify information on behavior and applicability. ([#294](https://github.com/Yubico/Yubico.NET.SDK/pull/294))

- All hardcoded links to the Yubico.NET.SDK GitHub repository have been updated to point to the HEAD branch. This ensures that links to sample code point to the latest version of that code. ([#286](https://github.com/Yubico/Yubico.NET.SDK/pull/286), [#279](https://github.com/Yubico/Yubico.NET.SDK/pull/279))

- An [SDK overview](https://github.com/Yubico/Yubico.NET.SDK/blob/develop/.github/copilot-instructions.md) designed to help the Copilot coding agent work more efficiently has been added to the Yubico.NET.SDK GitHub repository. ([#296](https://github.com/Yubico/Yubico.NET.SDK/pull/296))

Dependencies:

- Several dependencies across the Yubico.YubiKey and Yubico.Core projects have been updated to the latest versions. ([#274](https://github.com/Yubico/Yubico.NET.SDK/pull/274))

## 1.13.x Releases

### 1.13.2

Release date: July 3rd, 2025

Features:

- A new ``RawData`` property, which exposes raw CBOR-encoded data that can be more easily passed to third party tools for parsing, has been added to the FIDO2 ``MakeCredentialData`` class. ([#225](https://github.com/Yubico/Yubico.NET.SDK/pull/225))

- A new ``VersionQualifier`` has been added for handling YubiKey firmware (by version number, type, and iteration), which enables apps built with the SDK to distinguish between standard production YubiKeys and release candidate (RC) YubiKeys. The ``YubiKeyDeviceInfo`` class has also been updated to support ``VersionQualifier``. ([#240](https://github.com/Yubico/Yubico.NET.SDK/pull/240))

- The GitHub Actions workflows have been updated to use the ``windows-2022`` runner instead of ``windows-2019``, which ensures compatibility with newer environments and improves the consistency of the build and publish pipelines. ([#242](https://github.com/Yubico/Yubico.NET.SDK/pull/242))

Documentation:

- The documentation site has been updated with a new search bar, light/dark mode, new styling, and a modified table of contents. ([#241](https://github.com/Yubico/Yubico.NET.SDK/pull/241))

- New documentation covering the YubiKey Bio Multi-protocol Edition and its quirks, including the ``DeviceReset()`` method, has been added. ([#237](https://github.com/Yubico/Yubico.NET.SDK/pull/237))

- A discrepancy in the documentation on attestation statement generation has been fixed. ([#236](https://github.com/Yubico/Yubico.NET.SDK/pull/236))

- The documentation covering the default management key value and algorithm has been clarified. ([#233](https://github.com/Yubico/Yubico.NET.SDK/pull/233))

- The DER encoding details in the documentation on the PIV ``AuthenticateSignCommand()`` have been corrected. ([#239](https://github.com/Yubico/Yubico.NET.SDK/pull/239))

Bug Fixes:

- NativeShims now outputs Net47 build files into the correct architecture-specific folders. Supported architectures include x86, x64, and Arm64. ([#211](https://github.com/Yubico/Yubico.NET.SDK/pull/211))

- An ongoing [dotnet issue](https://github.com/dotnet/runtime/issues/112080) that has broken the resolution of core libraries on macOS 15 prevented the SDK from locating important dependencies on Mac when using .NET8 and above. To fix macOS and .NET compatibility with the SDK, the ``CoreFoundation``, ``IOKitFramework``, and ``WinSCard`` constants have been updated to use absolute paths (``/System/Library/Frameworks/...``) instead of relative paths (``.framework/...``) to align with macOS system conventions. ([#255](https://github.com/Yubico/Yubico.NET.SDK/pull/255))

- Use of the deprecated ``PivPrivateKey`` and ``PivPublicKey`` types when importing into the new PIV methods is now handled correctly (by throwing an exception). ([#231](https://github.com/Yubico/Yubico.NET.SDK/pull/231))

- An issue affecting the use of the RSA-3072 and RSA-4096 algorithms with attestation certificates has been fixed. ([#230](https://github.com/Yubico/Yubico.NET.SDK/pull/230))

Dependencies:

- The Yubico.NET.SDK repository now includes the GitHub dependabot to automate dependency updates for the ``nuget`` and ``dotnet-sdk`` package ecosystems. ([#244](https://github.com/Yubico/Yubico.NET.SDK/pull/244))

- Several dependencies across the Core (Yubico.Core.csproj), Integration Tests (Yubico.YubiKey.IntegrationTests.csproj), Sandbox (Yubico.YubiKey.TestApp.csproj), Unit Tests (Yubico.YubiKey.UnitTests.csproj), and Utilities (Yubico.YubiKey.TestUtilities.csproj) projects have been updated to newer versions. ([#256](https://github.com/Yubico/Yubico.NET.SDK/pull/256), [#254](https://github.com/Yubico/Yubico.NET.SDK/pull/254), [#250](https://github.com/Yubico/Yubico.NET.SDK/pull/250))

Deprecations:

- ``PivEccPublic``, ``PivEccPrivateKey``, ``PivRsaPublic``, and ``PivRsaPrivateKey`` have been marked as obsolete. Use implementations of ``ECPublicKey``, ``ECPrivateKey``, ``RSAPublicKey``, and ``RSAPrivateKey`` instead. ([#231](https://github.com/Yubico/Yubico.NET.SDK/pull/231))

- The ``CreateFromPkcs8`` methods in the ``Curve25519PublicKey``, ``ECPublicKey``, and ``RSAPublicKey`` classes have been marked as obsolete and replaced with new ``CreateFromSubjectPublicKeyInfo`` methods. ([#243](https://github.com/Yubico/Yubico.NET.SDK/pull/243))

### 1.13.1

Release date: April 28th, 2025

This release mainly addresses an issue that was affecting FIDO2 on YubiKey 5.7.4 and greater as well as adds support for compressed certificates within the PIV application. It also contains miscellaneous and documentation updates.

Features:

- Support for compressed certificates in the PIV application [#219](https://github.com/Yubico/Yubico.NET.SDK/pull/219)
- Ability to create a FirmwareVersion object through parsing a version string (e.g. 1.0.0) [#220](https://github.com/Yubico/Yubico.NET.SDK/pull/220)

Bug Fixes:

- PinUvAuthParam was erroneously truncated which caused failures on multiple FIDO2 commands for YubiKey v 5.7.4 [#222](https://github.com/Yubico/Yubico.NET.SDK/pull/222)

Documentation:

- Updates to challenge-response documentation to improve clarity [#221](https://github.com/Yubico/Yubico.NET.SDK/pull/221)

Miscellaneous:

- Integration tests will now run on Bio USB C keys as well [a4c4df](https://github.com/Yubico/Yubico.NET.SDK/commit/a4c4df10047bedf507e4ce36b80ed5001b996b9a).

### 1.13.0

Release date: April 9th, 2025

Features:

- Curve25519 support has been added for PIV [(#210)](https://github.com/Yubico/Yubico.NET.SDK/pull/210):

  - Keys can now be imported or generated using the Ed25519 and X25519 algorithms.
  - The key agreement operation can be performed with an X25519 key.
  - Digital signatures can now be created with a Ed25519 key.
  - New related unit tests have been added.

- Unit tests have been added for RSA-3072 and RSA-4096 keys. [(#197)](https://github.com/Yubico/Yubico.NET.SDK/pull/197)

- Support for large APDUs has been improved [(#208)](https://github.com/Yubico/Yubico.NET.SDK/pull/208):

  - When sending large APDU commands to a YubiKey via the smartcard connection, the CommandChainingTransform will now throw an exception when the cumulative APDU data (sent in chunks of up to 255 bytes) exceeds the max APDU size for the given YubiKey (varies based on firmware version; see [SmartCardMaxApduSizes](xref:Yubico.YubiKey.SmartCardMaxApduSizes)).

- Support for Ed25519 and P384 credentials has been added for FIDO. [(#186)](https://github.com/Yubico/Yubico.NET.SDK/pull/186)

- Ubuntu runners have been upgraded from version 20.04 to 22.04 to support the compilation of Yubico.NativeShims. [(#188)](https://github.com/Yubico/Yubico.NET.SDK/pull/188)

Bug Fixes:

- The default logger now only writes output for the "Error" log level unless another level is specified. Previously, the logger wrote output for all log levels, which could become overly long and difficult to evaluate. [(#185)](https://github.com/Yubico/Yubico.NET.SDK/pull/185)

Miscellaneous:

- The [License](https://github.com/Yubico/Yubico.NET.SDK/blob/develop/LICENSE.txt) was updated to remove the information for the AesCmac.cs file from the Bouncy Castle library. [(#196)](https://github.com/Yubico/Yubico.NET.SDK/pull/196)

## 1.12.x Releases

### 1.12.1

Release date: December 19th, 2024

Bug Fixes: Now selects correct device initializing Fido2Session [(#179)](https://github.com/Yubico/Yubico.NET.SDK/pull/179)

### 1.12.0

Release date: December 18th, 2024

Features:

- Security Domain application and Secure Channel Protocol (SCP) ([#164](https://github.com/Yubico/Yubico.NET.SDK/pull/164)):

  - SCP11a/b/c is now supported for the PIV, OATH, OTP, and YubiHSM applications.
  - SCP03 support has been extended to the OATH, OTP, and YubiHSM applications (previously PIV only).
  - The Yubico.YubiKey.Scp namespace now provides all SCP and Security Domain functionality. This namepace replaces functionality in the Yubico.YubiKey.Scp03 namespace, which has been deprecated.
  - The new `SecurityDomainSession` class provides an interface for managing the Security Domain application of a YubiKey. This includes SCP configuration (managing SCP03 key sets and SCP11 asymmetric keys and certificates) and creation of an encrypted communication channel with other YubiKey applications.
  - New key parameter classes have been added: `ScpKeyParameters`, `Scp03KeyParameters`, `Scp11KeyParameters`, `ECKeyParameters`, `ECPrivateKeyParameters`, `ECPublicKeyParameters`.
- [YubiKeyDeviceListener](xref:Yubico.YubiKey.YubiKeyDeviceListener) has been reconfigured to run the listeners in the background instead of the main thread. In addition, the listeners can now be [stopped](xref:Yubico.YubiKey.YubiKeyDeviceListener.StopListening) when needed to reclaim resources. Once stopped, the listeners can be restarted. ([#89](https://github.com/Yubico/Yubico.NET.SDK/pull/89))
- Microsoft.Extensions.Logging.Console is now the default logger. To enable logging from a dependent project (e.g. unit tests, integration tests, an app), you can either add an appsettings.json to your project or use the ConfigureLoggerFactory. ([#139](https://github.com/Yubico/Yubico.NET.SDK/pull/139))
- The SDK now uses inferred variable types (var) instead of explicit types in all projects except Yubico.Core. This change aims to improve code readability, reduce verbosity, and enhance developer productivity while maintaining type safety. ([#141](https://github.com/Yubico/Yubico.NET.SDK/pull/141))

Bug Fixes:

- The [PivSession.ChangeManagementKey](xref:Yubico.YubiKey.Piv.PivSession.ChangeManagementKey(Yubico.YubiKey.Piv.PivTouchPolicy)) method was incorrectly assuming Triple-DES was the default management key algorithm for FIPS keys. The SDK now verifies the management key alorithm based on key type and firmware version. ([#162](https://github.com/Yubico/Yubico.NET.SDK/pull/162), [#167](https://github.com/Yubico/Yubico.NET.SDK/pull/167))
- The SDK now correctly sets the IYubiKeyDeviceInfo property [IsSkySeries](xref:Yubico.YubiKey.IYubiKeyDeviceInfo.IsSkySeries) to True for YubiKey Security Key Series Enterprise Edition keys. ([#158](https://github.com/Yubico/Yubico.NET.SDK/pull/158))
- Exceptions are now caught when running PivSession.Dispose. This fixes an issue where the Dispose method could not close the Connection in the event of a disconnected YubiKey. ([#104](https://github.com/Yubico/Yubico.NET.SDK/issues/104))
- A dynamic DLL resolution based on process architecture (x86/x64) has been implemented for NativeShims.dll. This fixes a reported issue with the NativeShims.dll location for 32-bit processes. ([#154](https://github.com/Yubico/Yubico.NET.SDK/pull/154))

Miscellaneous:

- Users are now able to verify that the NuGet package has been generated from our repository using [Github Attestations](https://docs.github.com/en/actions/security-for-github-actions/using-artifact-attestations/using-artifact-attestations-to-establish-provenance-for-builds) ([#169](https://github.com/Yubico/Yubico.NET.SDK/pull/169)) like this:
  > \> gh attestation verify .\Yubico.Core.1.12.0.nupkg --repo Yubico/Yubico.NET.SDK

Deprecations:

- Yubico.YubiKey/Scp03 namespace.
- All Yubico.Yubikey.StaticKeys endpoints.

Migration Notes:

- Use the `SecurityDomainSession` for Security Domain operations.
- Review your logging configuration if using custom logging.
- Align with Android/Python SDK naming conventions.

## 1.11.x Releases

### 1.11.0

Release date: June 28th, 2024

This release introduces significant enhancements and new features for YubiKeys running the latest firmware (version 5.7) and YubiKey Bio/Bio Multi-Protocol Edition keys. Highlights include temporary disablement of NFC connectivity, PIN complexity status, support for RSA 3072 and 4096-bit keys, and support for biometric verification. Additionally, USB reclaim speed has been optimized and adjustments to the touch sensor sensitivity have been implemented. For details on all changes, see below.  

Features:

- Support for YubiKeys with the latest firmware (version 5.7):
  - NFC connectivity can now be temporarily disabled with [SetIsNfcRestricted()](xref:Yubico.YubiKey.YubiKeyDevice.SetIsNfcRestricted%28System.Boolean%29) ([#91](https://github.com/Yubico/Yubico.NET.SDK/pull/91)).
  - Additional property pages on the YubiKey are now read into [YubiKeyDeviceInfo](xref:Yubico.YubiKey.YubiKeyDeviceInfo) ([#92](https://github.com/Yubico/Yubico.NET.SDK/pull/92)).
  - PIN complexity:
    - Complexity status can now be checked with [IsPinComplexityEnabled](xref:Yubico.YubiKey.YubiKeyDevice.IsPinComplexityEnabled) ([#92](https://github.com/Yubico/Yubico.NET.SDK/pull/92)).
    - PIN complexity error messages and exceptions have been added ([#112](https://github.com/Yubico/Yubico.NET.SDK/pull/112)).
  - The set of YubiKey applications that are capable of being put into FIPS mode can be retrieved with [FipsCapable](xref:Yubico.YubiKey.YubiKeyDevice.FipsCapable). The set of YubiKey applications that are in FIPS mode can be retrieved with [FipsApproved](xref:Yubico.YubiKey.YubiKeyDevice.FipsApproved) ([#92](https://github.com/Yubico/Yubico.NET.SDK/pull/92)).
  - The part number for a key’s Secure Element processor, if available, can be retrieved with [PartNumber](xref:Yubico.YubiKey.YubiKeyDevice.PartNumber) ([#92](https://github.com/Yubico/Yubico.NET.SDK/pull/92)).
  - The set of YubiKey applications that are blocked from being reset can be retrieved with [ResetBlocked](xref:Yubico.YubiKey.YubiKeyDevice.ResetBlocked) ([#92](https://github.com/Yubico/Yubico.NET.SDK/pull/92)).
  - PIV:
    - 3072 and 4096 RSA keys can now be generated and imported ([#100](https://github.com/Yubico/Yubico.NET.SDK/pull/100)).
    - Keys can now be moved between all YubiKey PIV slots except for the attestation slot with [MoveKeyCommand](xref:Yubico.YubiKey.Piv.Commands.MoveKeyCommand). Any PIV key can now be deleted from any PIV slot with [DeleteKeyCommand](xref:Yubico.YubiKey.Piv.Commands.DeleteKeyCommand) ([#103](https://github.com/Yubico/Yubico.NET.SDK/pull/103)).
- Support for YubiKey Bio/Bio Multi-Protocol Edition keys:
  - Bio metadata can now be retrieved with [GetBioMetadataCommand](xref:Yubico.YubiKey.Piv.Commands.GetBioMetadataCommand) ([#108](https://github.com/Yubico/Yubico.NET.SDK/pull/108)).
  - New PIV PIN verification policy enum values ([MatchOnce](xref:Yubico.YubiKey.Piv.PivPinPolicy.MatchOnce), [MatchAlways](xref:Yubico.YubiKey.Piv.PivPinPolicy.MatchAlways)) have been added ([#108](https://github.com/Yubico/Yubico.NET.SDK/pull/108)).
  - [Biometric verification](../application-piv/commands.md#biometric-verification) is now supported ([#108](https://github.com/Yubico/Yubico.NET.SDK/pull/108)).
  - A device-wide reset can now be performed on YubiKey Bio Multi-protocol keys with [DeviceReset](xref:Yubico.YubiKey.IYubiKeyDevice.DeviceReset) ([#110](https://github.com/Yubico/Yubico.NET.SDK/pull/110)).
- The USB reclaim speed, which controls the time it takes to switch from one YubiKey application to another, has been reduced for compatible YubiKeys. To use the previous 3-second reclaim timeout for all keys, see [UseOldReclaimTimeoutBehavior](xref:Yubico.YubiKey.YubiKeyCompatSwitches.UseOldReclaimTimeoutBehavior) ([#93](https://github.com/Yubico/Yubico.NET.SDK/pull/93)).
- The sensitivity of the YubiKey’s capacitive touch sensor can now be temporarily adjusted with [SetTemporaryTouchThreshold](xref:Yubico.YubiKey.YubiKeyDevice.SetTemporaryTouchThreshold%28System.Int32%29) ([#95](https://github.com/Yubico/Yubico.NET.SDK/pull/95)).

Bug fixes:

- The ManagementKeyAlgorithm is now updated when the PIV Application is reset ([#105](https://github.com/Yubico/Yubico.NET.SDK/pull/105)).
- macOS input reports are now queued so that large responses aren't dropped ([#84](https://github.com/Yubico/Yubico.NET.SDK/pull/84)).
- Smart card handles are now opened shared by default. To open them exclusively, use [OpenSmartCardHandlesExclusively](xref:Yubico.Core.CoreCompatSwitches.OpenSmartCardHandlesExclusively) with AppContext.SetSwitch ([#83](https://github.com/Yubico/Yubico.NET.SDK/pull/83)).
- A build issue that occurred when compiling `Yubico.NativeShims` on MacOS has been fixed ([#109](https://github.com/Yubico/Yubico.NET.SDK/pull/109)).
- The correct certificate OID friendly names are now used for ECDsaCng (nistP256) and ECDsaOpenSsl (ECDSA_P256) ([#78](https://github.com/Yubico/Yubico.NET.SDK/pull/78)).

Miscellaneous:

- The way that YubiKey device info is read by the SDK has changed, and as a result, the following GetDeviceInfo command classes have been deprecated ([#91](https://github.com/Yubico/Yubico.NET.SDK/pull/91)):
  - `Yubico.YubiKey.Management.Commands.GetDeviceInfoCommand`
  - `Yubico.YubiKey.Otp.Commands.GetDeviceInfoCommand`
  - `Yubico.YubiKey.U2f.Commands.GetDeviceInfoCommand`
  - `Yubico.YubiKey.Management.Commands.GetDeviceInfoResponse`
  - `Yubico.YubiKey.Otp.Commands.GetDeviceInfoResponse`
  - `Yubico.YubiKey.U2f.Commands.GetDeviceInfoResponse`
- Integration test guardrails have been added to ensure tests are done only on specified keys. ([#100](https://github.com/Yubico/Yubico.NET.SDK/pull/100)).
- Unit tests were run on all platforms in CI ([#80](https://github.com/Yubico/Yubico.NET.SDK/pull/80)).

Dependencies:

- The test packages xUnit and Microsoft.NET.Test.Sdk have been updated ([#94](https://github.com/Yubico/Yubico.NET.SDK/pull/94)).

## 1.10.x Releases

### 1.10.0

Release date: April 10th, 2024

This release improves our native dependencies exposed through the `Yubico.NativeShims` package. We have also worked to improve the build and test experience of this repository by improving our automation and build files.

Changes:  

- **Yubico.NativeShims targets OpenSSL version 3.x on all platforms** - OpenSSL v1.1.x has reached end-of-life. The SDK now removes this dependency on all platforms, now upgrading to the supported 3.x version.
- **Dropped support for 32-bit Linux** - Yubico.NativeShims no longer builds for 32-bit (x86) Linux. We now depend on Ubuntu releases that contain OpenSSL 3.x by default. These newer releases no longer have mainstream support for this platform.
- **[Compilation hardening of Yubico.NativeShims](https://github.com/Yubico/Yubico.NET.SDK/pull/67)** -  Added commonly used compiler flags to increase security and code quality  
  **MacOS / Linux:**  
  -Wformat: Warn about format string issues in printf-like functions.  
  -Wformat-nonliteral: Warn about format strings that are not string literals.  
  -Wformat-security: Warn about potential security issues related to format strings.  
  -Wall: Enable most warning messages  
  -Wextra: Enable some additional warning messages not included in -Wall  
  -Werror: Treat all warnings as errors  
  -Wcast-qual: Warn when casting away const-ness  
  -Wshadow: Warn when a local variable shadows another variable  
  -pedantic: Issue warnings for language features beyond the C standard  
  -pedantic-errors: Treat pedantic warnings as errors  
  -Wbad-function-cast: Warn about dubious function pointer casts  
  -O2: Optimize code for performance  
  -fpic: Generate position-independent code  
  -fstack-protector-all: Enable stack protection for all functions  
  -D_FORTIFY_SOURCE=2: Enable runtime and compile-time checks for certain security-critical functions  
  **Windows flags:**
  /guard:cf: Enable control flow guard security feature  
  /GS: Enable buffer security check  
  /Gs: Control stack security check
- [Addressed compiler warning concerning Runtime Identifiers (RID)](https://github.com/Yubico/Yubico.NET.SDK/issues/59)
- **Enabled `dotnet format`** - The repository now uses `dotnet format` to ensure that pull requests adhere to the repository's coding standards. A pass of the tool has been run against the entire repository and a new baseline has been checked in.
  
## 1.9.x Releases

### 1.9.1

Release date: November 14th, 2023

Bug fixes:

- **SCard handle contention**. Previously, the SDK was opening all smart card handles with
  shared permissions, meaning that other applications and services were still able to interact
  with the YubiKey while the SDK performed smart card operations. However, this allowed these
  other entities (such as smart card minidrivers) to alter the current state of the YubiKey
  without the SDK's knowledge. This would sometimes cause random failures and exceptions to
  occur when using the SDK. The SDK now opens the handle exclusively, which means other
  applications will not be able to open the smart card handle for read and write operations
  while the SDK is using it. Callers should take care to not keep a YubiKey connection or
  session open longer than is needed.
- **Config changes over FIDO2**. The YubiKey Management commands are now available over all
  three logical USB interfaces (HID keyboard, HID FIDO, and smart card). The SDK will typically
  use the first available interface, giving some preference to the smart card. Previously,
  this operation would have failed on FIDO-only devices as the management commands were not
  properly wired up over this interface.

Miscellaneous:

- **Dependency updates**. The dependencies of the SDK were updated to the latest packages
  available. Since the SDK itself does not take many dependencies outside of the .NET Base
  Class Libraries (BCL), there should not be much of a noticeable impact. The two that
  affect the SDK itself (and not just test code) are:
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
  The previous method (`Yubico.YubiKey.YubiKeyDeviceExtensions.WithScp03()`) is now deprecated, and
  the new method (`Yubico.YubiKey.IYubiKeyDevice.ConnectScp03()` simply requires passing in the SCP03 key set to the
  PivSession constructor. It is also possible to build an
  IYubiKeyConnection that uses SCP03 via `Yubico.YubiKey.Piv.PivSession()`.

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
  [SCP03 here](xref:UsersManualScp).
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
on [this page](xref:YubiKeyTransportOverview).

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
