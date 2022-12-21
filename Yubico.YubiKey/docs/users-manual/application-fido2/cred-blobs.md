---
uid: Fido2CredBlobs
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

# FIDO2 credential blobs ("credBlob" extension)

When you get the [AuthenticatorInfo](xref:Yubico.YubiKey.Fido2.AuthenticatorInfo), you can
check the extensions to see if "credBlob" is supported.

```C#
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
        int maxCredBlobLength = fido2Session.AuthenticatorInfo.MaximumCredentialBlobLength ?? 0;
        if (fido2Session.AuthenticatorInfo.Extensions.Contains<string>("credBlob") && (maxCredBlobLength > 0))
        {
            . . .
        }
    }
```

If it does, you can add up to `maxCredBlobLength` bytes of arbitrary data to a credential.
That is, when you make the credential, you can specify that the YubiKey will store this
extra value with the credential.

If the "credBlob" extension is supported, the standard specifies that the maximum length
of the credential blob must be at least 32. Hence, if it is supported, you know that you
will be able to store at least 32 bytes.

Later on when you get an assertion for that credential, you can specify that the YubiKey
return the "credBlob" data with the assertion.

It is not possible to add a "credBlob" to an existing credential. It is only possible to
store data when making a credential. It is, of course, possible to delete an existing
credential and create a new one.

## Make credential with "credBlob"

With the SDK, making a credential begins with the
[MakeCredentialParameters](xref:Yubico.YubiKey.Fido2.MakeCredentialParameters). To include
a credential blob, use the
[AddCredBlobExtension](xref:Yubico.YubiKey.Fido2.MakeCredentialParameters.AddCredBlobExtension%2a)
method.

```C#
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
        // The credBlob extension is available if and only if the MaximumCredentialBlobLength
        // is provided and is greater than 0.
        if ((fido2Session.AuthenticatorInfo.MaximumCredentialBlobLength ?? 0) > 0)
        {
            var makeCredentialParameters = new MakeCredentialParameters(rp, userEntity);
            // Let's say we want to add the user's badge number as the "credBlob". 
            byte[] dataToAdd = GetUserBadgeNumber(userEntity);
            makeCredentialParameters.AddCredBlobExtension(dataToAdd, fido2Session.AuthenticatorInfo);
            . . .
            MakeCredentialData credentialData = fido2Session.MakeCredential(makeCredentialParameters);
        }
    }
```

## GetAssertion with "credBlob"

To get an assetrtion with the SDK, build
[GetAssertionParameters](xref:Yubico.YubiKey.Fido2.GetAssertionParameters). Set the
"credBlob" extension.

If you do not set the "credBlob" extension to true, the YubiKey will return the assertion,
but it will not return the credential blob.

```C#
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
        var getAssertionParameters = new GetAssertionParameters(rp, clientDataHash);
        getAssertionParameters.AddExtension("credBlob", true);
        . . .
        IList<GetAssertionData> assertionList = fido2Session.GetAssertions(getAssertionParameters);
    }
```

Note that when making a credential, to add an extension we supplied the extension string
along with a byte array. But to include the extension when getting an assertion, supply
the extension string along with a boolean. This just tells the YubiKey that we want it to
perform that extension's operations. In this case, the operation the YubiKey performs is
return the credential blob.

Find the assertion you want in the list. In most cases, the list will be only a single
assertion. Now get the "credBlob" data.

```C#
        byte[] credentialData = assertionList[0].AuthenticatorData.GetCredBlobExtension();
```
