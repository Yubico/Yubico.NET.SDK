---
uid: Fido2BioEnrollment
---

<!-- Copyright 2023 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# FIDO2 Bio Enrollment (and related operations)

Through the Bio Enrollment commands and methods, it is possible to set a YubiKey with a
fingerprint for FIDO2 authentication. There are some supporting operations as well.

* [GetModality](xref:Yubico.YubiKey.Fido2.Fido2Session.GetBioModality)
* [GetFingerprintSensorInfo](xref:Yubico.YubiKey.Fido2.Fido2Session.GetFingerprintSensorInfo)
* [EnumerateBioEnrollments](xref:Yubico.YubiKey.Fido2.Fido2Session.EnumerateBioEnrollments)
* [EnrollFingerprint](xref:Yubico.YubiKey.Fido2.Fido2Session.EnrollFingerprint%2a)
* [SetBioTemplateFriendlyName](xref:Yubico.YubiKey.Fido2.Fido2Session.SetBioTemplateFriendlyName%2a)
* [RemoveBioEnrollment](xref:Yubico.YubiKey.Fido2.Fido2Session.TryRemoveBioTemplate%2a)

## Feature detection

If you want to write code that can programmatically determine if a particular YubiKey
supports the Bio Enrollment operations, look at the
[AuthenticatorInfo](xref:Yubico.YubiKey.Fido2.AuthenticatorInfo) options.

```C#
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
        if (fido2Session.AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.bioEnroll) == OptionValue.True)
        {
            . . .
        }
    }
```

## Get Information

You can get information about the YubiKey's bio sensor.

```C#
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
        BioModality modality = fido2Session.GetBioModality();
        FingerprintSensorInfo sensorInfo = fido2Session.GetFingerprintSensorInfo();
    }
```

Neither of these calls require an [AuthToken](fido2-auth-tokens.md) (authentication).

It is also possible to get a list of templates a YubiKey holds. That operation requires an
AuthToken and will be discussed later.

## Getting an AuthToken

Other than the two information operations described above, Bio Enrollment operations
require an AuthToken. The SDK offers [automatic verification](fido2-auth-tokens.md),
but if you will be performing verification directly, here are some things you must
know.

First, you must obtain a PinUvAuthToken with the permission
[BioEnrollment](xref:Yubico.YubiKey.Fido2.Commands.PinUvAuthTokenPermissions.BioEnrollment).
It is not possible to use a PinToken.

Second, an AuthToken with the BioEnrollment permission can be reused. That is, a YubiKey
will not expire an AuthToken after performing a Bio Enrollment operation. Note that a
YubiKey will expire an AuthToken used to make a credential or get an assertion. An expired
AuthToken will not be valid for use in a Bio Enrollment operation.

Third, an AuthToken obtained using fingerprints might not work with Bio Enrollment
operations. Generally there are two ways to get an AuthToken, verifying the PIN and "user
verification" (UV). User verification is verifying the fingerprint. However, it is
possible a YubiKey Bio series device will not allow UV for Bio enrollment operations. That
is, it might not be possible to use a fingerprint to authenticate a user in order to add a
new fingerprint or even to delete an existing fingerprint.

Note that it will still be possible to use a fingerprint to obtain an AuthToken with other
permissions, such as MakeCredential.

To know whether it is possible to use fingerprint authentication to perform any Bio
Enrollment operation, check the "uvBioEnroll" option.

```C#
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
        if (fido2Session.AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.uvBioEnroll) == OptionValue.True)
        {
            . . .
        }
    }
```

If this option is not supported, then you must verify the PIN in order to obtain an
AuthToken to be used for Bio Enrollment operations.

## Enrolling a fingerprint

If you call [EnrollFingerprint](xref:Yubico.YubiKey.Fido2.Fido2Session.EnrollFingerprint%2a),
then you must supply a KeyCollector. The SDK will notify that KeyCollector when a
fingerprint sample is needed, and it will also report on the success or failure of the
most recent sample. If you do not want to supply a KeyCollector, then call the Enroll
commands directly.

To enroll a fingerprint, the user must supply a number of samples. How many? With each
sample provided, the SDK will report the number remaining. You might notice that the
[GetFingerprintSensorInfo](xref:Yubico.YubiKey.Fido2.Fido2Session.GetFingerprintSensorInfo)
call will return a
[FingerprintSensorInfo](xref:Yubico.YubiKey.Fido2.FingerprintSensorInfo) object, which
contains the property `MaxCaptureCount`. This is not necessarily the total number of good
samples required. It is a requirement of the FIDO2 standard and indicates that the number
of good samples required will never be greater than the value. For example, the
`MaxCaptureCount` might be 16, yet it is possible to capture a fingerprint with only five
good samples. But you know that the count will never be more than 16.

Upon calling the `EnrollFingerprint` method, the SDK will determine if it needs an
AuthToken. If so, it will determine if it can use a fingerprint to verify. If not, it will
need the PIN. It will call the KeyCollector no matter what the verification method.

Once the SDK has an AuthToken, it will need fingerprint samples. It will contact the
KeyCollector announcing the need for a fingerprint sample. After the user has provided
one, the SDK will determine if it needs another. If so, it will contact the KeyCollector
again, this time providing a
[BioEnrollSampleResult](xref:Yubico.YubiKey.BioEnrollSampleResult) object. That object
will contain information you can relay to the user, and inform them of the need for a
new sample.

After enough good samples have been collected, the SDK will contact the KeyCollector with
the request of Release.

### Cancel

Once the YubiKey has started the process of collecting a fingerprint sample, it is
possible to contact it and request the operation cancel. This is done through the
`KeyCollector`'s argument `KeyEntryData`.

When the SDK contacts your KeyCollector indicating it needs fingerprint samples, the
accompanying `KeyEntryData` will contain a property
[SignalUserCancel](xref:Yubico.YubiKey.KeyEntryData.SignalUserCancel). This is a
delegate you can copy when you get the request of
[EnrollFingerprint](xref:Yubico.YubiKey.KeyEntryRequest.EnrollFingerprint). Later on,
during the sampling operation, you can call the delegate to signal to the SDK that you
would like the operation to be canceled.

Normally, you specify canceling an operation by having your KeyCollector return `false`.
However, because notifying touch or fingerprint is a "non-modal" operation, the return
value cannot be used. Hence, the way to cancel is by calling the SDK's cancel delegate.

If the `SignalUserCancel` property in the `KeyEntryData` is null, you indicate cancel by
returning `false`. If the `SignalUserCancel` is not null, you indicate cancel by calling
that delegate.

See also the User's Manual
[article on touch notification](../sdk-programming-guide/key-collector-touch.md) for a
deeper discussion of this topic.

### Maximum failed samples

The YubiKey can determine that a particular sample was not good enough, or did not match
the previous samples. In that case, it will simply return a code indicating the reason it
failed.

Depending on the version of the YubiKey, it is possible it will have a maximum number of
failed samples before it rejects the enrollment. However, it is also possible there is no
maximum. That is, it is possible the YubiKey accepts unlimited bad sample attempts. Hence,
if you want to enforce a limit, you will have to program that in your KeyCollector using
the `SignalUserCancel`.

### Timeout

Once the YubiKey is ready to accept a sample, it will wait a certain number of seconds
before it gives up and cancels the operation. At that point, the SDK will throw an
exception. This timeout can be 28 seconds or more.

The standard specifies that a user can specify an alternate timeout. Hence, the
`EnrollFingerprint` method has an argument for `timeoutMilliseconds`.

However, depending on the YubiKey version, this might not be supported. The SDK will
accept the argument, but the YubiKey might ignore it. Hence, if you want to enforce an
alternate timeout, you will have to program that in your KeyCollector using the
`SignalUserCancel`.
