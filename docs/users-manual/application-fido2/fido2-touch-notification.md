---
uid: Fido2TouchFingerprintNotification
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

# FIDO2 touch and fingerprint notification

When the YubiKey is attempting to perform "User Verification" (UV), the end user must
verify their fingerprint on the YubiKey's fingerprint reader (this applies to the YubiKey
Bio series only).

In addition, some operations, such as
[MakeCredential](xref:Yubico.YubiKey.Fido2.Fido2Session.MakeCredential%2a) or
[GetAssertion](xref:Yubico.YubiKey.Fido2.Fido2Session.GetAssertions%2a), will not complete
until the user touches the contact. For example, a YubiKey will begin an operation, but at
some point will stop processing until the contact has been touched. Once touched, it will
finish the operation.

In those situations, you will likely want to notify the user that they need to verify
their fingerprint or touch the contact. But when exactly do you make that notification?

The SDK will call the KeyCollector at the moment fingerprint or touch is needed. Once it
receives that call, the KeyCollector can notify the user.

Normally, the KeyCollector is used to collect something from the user such as a PIN, key,
or some other secret value. However, with fingerprint or touch, there is nothing to
collect from the user and your application does not need to return anything to the SDK. It
is necessary only to notify the user to perform some task.

The [KeyCollector and touch article](../sdk-programming-guide/key-collector-touch.md) in
the User's Manual "SDK programming guide" explains how the process of touch notification
works, describes requirements of your `KeyCollector`, and provides rudimentary samples.
