---
uid: UsersManualPinComplexityPolicy
---

<!-- Copyright 2024 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# PIN complexity policy

PIN complexity is an optional feature available on YubiKeys with firmware version 5.7 or later. If PIN complexity is
enabled, the YubiKey will block the usage of non-trivial PINs, such as `11111111`, `password`, or `12345678`.

YubiKeys can also be programmed during the pre-registration process to refuse other specific values. For more
information on PIN complexity and the full PIN blocklist, see
the <a href="https://docs.yubico.com/hardware/yubikey/yk-tech-manual/5.7-firmware-specifics.html#pin-complexity">YubiKey
Technical Manual</a>.

> [!NOTE]
> PIN complexity policy is derived from the current Revision 3
> of <a href="https://pages.nist.gov/800-63-3/sp800-63-3.html">NIST SP 800-63</a> (specifically SP 800-63B-3), with
> additional consideration of <a href="https://pages.nist.gov/800-63-4/sp800-63.html">Revision 4 of SP 800-63</a> (
> specifically SP 800-63B-4).

For the SDK, PIN complexity enablement means that the YubiKey will refuse to set or change the following values if they
violate the policy:

- [PIV PIN and PUK](xref:UsersManualPinPukMgmtKey)
- [FIDO2 PIN](xref:TheFido2Pin)

## Managing PIN complexity with the SDK

PIN complexity can be managed by the SDK in two ways:

1. Reading the current PIN complexity status of a key.
2. Handling PIN complexity-related errors.

### Reading the current PIN complexity status

To verify whether PIN complexity is enabled for a particular YubiKey, check
the [IsPinComplexityEnabled](xref:Yubico.YubiKey.IYubiKeyDeviceInfo.IsPinComplexityEnabled) property, which is part of
the [IYubiKeyDeviceInfo](xref:Yubico.YubiKey.IYubiKeyDeviceInfo) interface.

### Handling PIN complexity errors

Applications that support setting or changing PINs should be able to handle the situation when a YubiKey refuses the
user value because it violates the PIN complexity policy.

The SDK communicates PIN complexity violations by throwing specific exceptions.

#### PivSession exceptions

During a [PivSession](xref:Yubico.YubiKey.Piv.PivSession), PIN complexity violations result in
a `System.Security.SecurityException` with the message, `ExceptionMessages.PinComplexityViolation`.

If the application uses a [KeyCollector](xref:UsersManualKeyCollector), the violation is reported through
the [KeyEntryData.IsViolatingPinComplexity](xref:Yubico.YubiKey.KeyEntryData.IsViolatingPinComplexity) property.

PIN complexity violations are reported for following PIV operations:

- [PivSession.ChangePin()](xref:Yubico.YubiKey.Piv.PivSession.ChangePin)
- [PivSession.ChangePuk()](xref:Yubico.YubiKey.Piv.PivSession.ChangePuk)
- [PivSession.ResetPin()](xref:Yubico.YubiKey.Piv.PivSession.ResetPin)

#### Fido2Session exceptions

During a [Fido2Session](xref:Yubico.YubiKey.Fido2.Fido2Session), PIN complexity violations result in
a [Fido2Exception](xref:Yubico.YubiKey.Fido2.Fido2Exception) object with
a [Status](xref:Yubico.YubiKey.Fido2.Fido2Exception.Status)
of [CtapStatus.PinPolicyViolation](xref:Yubico.YubiKey.Fido2.CtapStatus.PinPolicyViolation).

If the application uses a [KeyCollector](xref:UsersManualKeyCollector), the violation is reported through
the [KeyEntryData.IsViolatingPinComplexity](xref:Yubico.YubiKey.KeyEntryData.IsViolatingPinComplexity) property.

PIN complexity violations are reported for following FIDO2 operations:

- [Fido2Session.SetPin()](xref:Yubico.YubiKey.Fido2.Fido2Session.SetPin)
- [Fido2Session.ChangePin()](xref:Yubico.YubiKey.Fido2.Fido2Session.ChangePin)

### Example code

For code samples demonstrating how to handle PIN complexity violations, see
the [PivSampleCode](https://github.com/Yubico/Yubico.NET.SDK/blob/main/Yubico.YubiKey/examples/PivSampleCode/KeyCollector/SampleKeyCollector.cs), [Fido2SampleCode](https://github.com/Yubico/Yubico.NET.SDK/blob/main/Yubico.YubiKey/examples/Fido2SampleCode/KeyCollector/Fido2SampleKeyCollector.cs),
and [PinComplexityTests](https://github.com/Yubico/Yubico.NET.SDK/blob/main/Yubico.YubiKey/tests/integration/Yubico/YubiKey/PinComplexityTests.cs)
integration tests.