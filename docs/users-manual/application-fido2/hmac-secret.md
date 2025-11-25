---
uid: Fido2HmacSecret
---

<!-- Copyright 2025 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# FIDO2 hmac-secret and hmac-secret-mc extensions

If it is, when making a credential you can specify that the YubiKey create a secret value
associated with that credential. Later on when getting an assertion, you can ask the
YubiKey to retrieve that secret. What you do with that secret is up to you. The standard
remarks that it can be used to encrypt or decrypt data.

Each client will have access to this secret value, so that it is possible to securely
share information among clients. The secret value is actually built from a value on the
YubiKey and a salt provided by the client. The standard says, "The authenticator and the
platform each only have the part of the complete secret to prevent offline attacks."
Hence, for all clients to share this secret, each client must use the same salt.

In order to generate the "hmac-secret", the YubiKey will perform HMAC with SHA-256 using
the secret value it has associated with the credential as the key, and the salt provided
(along with possibly other data) as the data to MAC. It will then encrypt that result
using the shared key (the key shared between the client and the YubiKey, the result of the
ECDH operation used to encrypt all communications between the client and the YubiKey).

## Salts

It is possible to pass in two 32-byte salts to the
`GetAssertionParameters.RequestHmacSecretExtension` method. In that case, the YubiKey will
return two values. The standard says the second value is used "...when the platform wants
to roll over the symmetric secret...".

```csharp
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
            var getAssertionParameters = new GetAssertionParameters(rp, clientDataHash);
            getAssertionParameters.RequestHmacSecretExtension(salt1, salt2);
            . . .
            IReadOnlyList<GetAssertionData> assertionDataList = fido2Session.GetAssertions(makeCredentialParameters);
            . . .
            byte[] hmacSecretValue = assertionDataList[0].AuthenticatorData.GetHmacSecretExtension(
                fido2Session.AuthProtocol);
    }
```

## hmac-secret vs hmac-secret-mc

The fundamental difference between the hmac-secret and hmac-secret-mc extensions is when the secret is returned by the YubiKey.

With hmac-secret, the secret is returned during ``GetAssertions()``. However, with hmac-secret-mc, the secret is returned during ``MakeCredential()``.

hmac-secret-mc is useful in situations where the secret is needed immediately upon creation of a new credential. By returning the secret in a single operation instead of two (``MakeCredential()`` followed by ``GetAssertions()``), the amount of required user interaction, including user presence and PIN/UV validation, is reduced.

## Verify support for hmac-secret and hmac-secret-mc

To verify whether a particular YubiKey supports the hmac-secret and hmac-secret-mc extensions, check the key's [AuthenticatorInfo](xref:Yubico.YubiKey.Fido2.AuthenticatorInfo):

```C#
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
        if (fido2Session.AuthenticatorInfo.Extensions.Contains<string>("hmac-secret"))
        {
            . . .
        }
        else if (fido2Session.AuthenticatorInfo.Extensions.Contains<string>("hmac-secret-mc"))
        {
            . . .
        }
    }
```

## Enabling the hmac-secret extension and requesting the secret

With hmac-secret, the extension needs to be enabled for a credential during ``MakeCredential()`` in order to return the secret during ``GetAssertions()``. It is not possible to add the extension to an existing credential.

To enable the hmac-secret extension, we must add it to the parameters for ``MakeCredential()`` prior to calling the ``MakeCredential()`` method:

```csharp
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
            var makeCredentialParameters = new MakeCredentialParameters(rp, userEntity);
            makeCredentialParameters.AddHmacSecretExtension(fido2Session.AuthenticatorInfo);
            . . .
            MakeCredentialData credentialData = fido2Session.MakeCredential(makeCredentialParameters);
    }
```

Once the extension has been added to a credential, we can return the secret with ``GetAssertions()``. However, we must first add the request for the secret along with the salt(s) to the parameters for ``GetAssertions()`` via ``RequestHmacSecretExtension(salt)``.

```csharp
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
            var getAssertionParameters = new GetAssertionParameters(rp, clientDataHash);
            getAssertionParameters.RequestHmacSecretExtension(salt);
            . . .
            IReadOnlyList<GetAssertionData> assertionDataList = fido2Session.GetAssertions(makeCredentialParameters);
    }
```

The secret will be returned in the ``GetAssertionData``.

## Enabling the hmac-secret-mc extension and requesting the secret

With hmac-secret-mc, the extension needs to be enabled for a credential during ``MakeCredential()`` *and* the salt(s) must be provided in order to return the secret. To do so, we must add the extension and salt(s) to the parameters for ``MakeCredential()`` prior to calling the ``MakeCredential()`` method.

If the operation is successful, the secret will be returned in the ``MakeCredentialData``.

## Extracting and decrypting the secret

Once the secret has been returned, we can extract and decrypt it using the ``AuthenticatorData.GetHmacSecretExtension`` method.

If the secret was generated using one salt, the result will be a 32-byte value. In the case of two salts, the result will be 64 bytes in length, with the first 32 bytes representing the value generated with `salt1` and the second 32 bytes representing the value generated with `salt2`.

To perform the extraction and decryption on ``GetAssertionData`` (hmac-secret), do something like the following code sample. Note that if authenticator data was returned for more that one credential, we must specify which credential's secret we'd like to extract.

```csharp
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
            . . .
            byte[] hmacSecretValue = assertionDataList[0].AuthenticatorData.GetHmacSecretExtension(
                fido2Session.AuthProtocol);
    }
```

With the extraction and decryption on ``MakeCredentialData`` (hmac-secret-mc), we do not need to specify a particular credential given that only one was created at the time the secret was returned:

