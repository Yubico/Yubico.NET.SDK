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

# PIN Complexity policy

Since firmware 5.7, the YubiKey can enforce usage of non-trivial PINs in its applications, this feature has been named _PIN complexity policy_ and is derived from the current Revision 3 of SP 800-63 (specifically SP 800-63B-3) with additional consideration of Revision 4 of SP 800-63 (specifically SP 800-63B-4).

If PIN complexity has been enforced, the YubiKey will refuse to set or change values of following, if they violate the policy:
- PIV PIN and PUK
- FIDO2 PIN

That means that simple values such as `11111111`, `password` or `12345678` will be refused. The YubiKey can also be programmed during the pre-registration to refuse other specific values. More information can be found in our <a href="https://docs.yubico.com/hardware/yubikey/yk-tech-manual/5.7-firmware-specifics.html#pin-complexity">online documentation</a> for the firmware version 5.7 additions.

The SDK has support for getting information about the feature and also a way how to let the client know that an error is related to PIN complexity.

## Read current PIN complexity status
The PIN complexity enforcement status is part of the `IYubiKeyDeviceInfo` through `bool IsPinComplexityEnabled` property.

## Handle PIN complexity errors
The SDK can be used to create a variety of applications. If those support setting or changing PINs, they should handle the situation when a YubiKey refuses the user value because it is violating the PIN complexity.

The SDK communicates this by throwing specific Exceptions.

### PIV Session
In PIV session the exception thrown during PIN complexity violations is `SecurityException` with a specific message: `ExceptionMessages.PinComplexityViolation`.

If the application uses `KeyCollectors`, the violation is reported through `KeyEntryData.IsViolatingPinComplexity`.

The violations are reported for following operations:
- `PivSession.ChangePin()`
- `PivSession.ChangePuk()`
- `PivSession.ResetPin()`

### FIDO2 Session
In the FIDO2 application, `Fido2Exception` with `Status` of `CtapStatus.PinPolicyViolation` is thrown after a PIN complexity was violated. For `KeyCollectors`, `KeyEntryData.IsViolatingPinComplexity` will be set to `true` for these situations.

This applies to following `Fido2Session` operations:
- `Fido2Session.SetPin()`
- `Fido2Session.ChangePin()`

## Example code
You can find examples of code in the `PivSampleCode` and `Fido2SampleCode` examples as well in `PinComplexityTests` integration tests.