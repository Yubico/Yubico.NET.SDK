# Feature 764

## User Story: I should be able to set serial number visibility when configuring an OTP slot

## Description:
This seems to have been a design oversight:

In the Configure* operation classes in the OTP.Operations namespace, one should be able to set the serial number visibility (i.e. API, USB, and Button visible). But the methods for adding these flags are not present.

See Class (ConfigureYubicoOtp)[https://docs.yubico.com/yesdk/yubikey-api/Yubico.YubiKey.Otp.Operations.ConfigureYubicoOtp.html] for an example of a configuration missing these methods.

See Class (UpdateSlot)[https://docs.yubico.com/yesdk/yubikey-api/Yubico.YubiKey.Otp.Operations.UpdateSlot.html] to see that they are present for updating a slot.

## How it's implemented in the corresponding Java SDK
See these resources to learn how it was effectively implemented in the Java SDK
https://github.com/Yubico/yubikit-android/tree/main/oath
https://github.com/Yubico/yubikit-android/tree/main/yubiotp

## Definition of Done
- Adjusted implementation of the Configure* (e.g. ConfigureYubicoOtp) operation classes that supports setting the serial number visibility, as it is for UpdateSlot
- Simple Unit Tests. Append tests to existing relevant test classes
- Simple integration test (will be tested manually by user in PR)
- The implementation should follow, where applicable, the implementation in Java
- No breaking changes are allowed
- Follow .editorConfig code formatting

