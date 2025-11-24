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

When you get the [AuthenticatorInfo](xref:Yubico.YubiKey.Fido2.AuthenticatorInfo), you can
check the extensions to see if "hmac-secret" is supported.

```C#
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
        if (fido2Session.AuthenticatorInfo.Extensions.Contains<string>("hmac-secret"))
        {
            . . .
        }
    }
```

If it is, when making a credential you can specify that the YubiKey create a secret value
associated with that credential. Later on when getting an assertion, you can ask the
YubiKey to retrieve that secret. What you do with that secret is up to you. The standard
remarks that it can be used to encrypt or decrypt data.

Each client will have access to this secret value, so that it is possible to securely
share information among clients. The secret value is actually built from a value on the
YubiKey and a salt provided by the client. The standard says, "The authenticator and the
platform each only have the part of the complete secret to prevent offline attacks."
Hence, for all clients to share this secret, each client must use the same salt.

## Requesting the YubiKey create this secret

The YubiKey will generate a secret for a credential only if instructed to do so at the
time the credential is made. It is not possible to "add" this secret to an existing
credential.

```csharp
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
            var makeCredentialParameters = new MakeCredentialParameters(rp, userEntity);
            makeCredentialParameters.AddHmacSecretExtension(fido2Session.AuthenticatorInfo);
            . . .
            MakeCredentialData credentialData = fido2Session.MakeCredential(makeCredentialParameters);
    }
```

## Requesting the secret

When getting an assertion, you specify you want the YubiKey to return the assertion and
the secret value. If you don't, the YubiKey will return the assertion, but it won't return
the secret.

```csharp
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
            var getAssertionParameters = new GetAssertionParameters(rp, clientDataHash);
            getAssertionParameters.RequestHmacSecretExtension(salt);
            . . .
            IReadOnlyList<GetAssertionData> assertionDataList = fido2Session.GetAssertions(makeCredentialParameters);
    }
```

## Extracting the secret

Once you have an assertion, you will find the secret in the `Extensions` in the
`GetAssertionData.AuthenticatorData` property. There is a method in that class that will
parse and decrypt the value returned.

```csharp
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
            . . .
            byte[] hmacSecretValue = assertionDataList[0].AuthenticatorData.GetHmacSecretExtension(
                fido2Session.AuthProtocol);
    }
```

The result will be an array 32 bytes long.

## Decrypting the value returned

In order to generate the "hmac-secret", the YubiKey will perform HMAC with SHA-256 using
the secret value it has associated with the credential as the key, and the salt provided
(along with possibly other data) as the data to MAC. It will then encrypt that result
using the shared key (the key shared between the client and the YubiKey, the result of the
ECDH operation used to encrypt all communications between the client and the YubiKey).

The value returned by the YubiKey is 32 bytes. The
`AuthenticatorData.GetHmacSecretExtension` method will decrypt that value and return the
result.

## Two salts

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

The result will be an array 64 bytes long. The first 32 bytes make up the result based on
`salt1`, and the second 32 bytes make up the result based on `salt2`.
