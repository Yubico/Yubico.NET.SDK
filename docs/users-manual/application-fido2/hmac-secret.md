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

The SDK and YubiKey (depending on firmware version) support the hmac-secret and hmac-secret-mc extensions, which enable the creation of a symmetric secret value scoped to a credential. This secret, which can be used for encryption and decryption, supports the use of WedAuthn's [Pseudo-Random Function (PRF)](https://developers.yubico.com/WebAuthn/Concepts/PRF_Extension/index.html) with YubiKeys.

When requested, the YubiKey generates the hmac secret by performing HMAC with SHA-256 using
the secret value it has associated with the credential as the key plus one or two salts provided
by the client. It then encrypts the result (returned as a byte array) using the ECDH-derived shared secret key (the key used to encrypt all communications between the client and the YubiKey). The hmac secret can only be requested during ``MakeCredential()`` (hmac-secret-mc) or ``GetAssertions()`` (hmac-secret).

hmac-secret and hmac-secret-mc are supported for both discoverable and non-discoverable credentials.

## Salts

The client must provide one or two 32-byte salts to the YubiKey when requesting the secret. If one salt is provided, the YubiKey will return a secret value of 32 bytes in length; if two salts are provided, the YubiKey will return two secret values, each 32 bytes in length.

According to the [CTAP standard](https://fidoalliance.org/specs/fido-v2.2-ps-20250714/fido-client-to-authenticator-protocol-v2.2-ps-20250714.html#sctn-hmac-secret-extension), the second secret value returned in the two-salt scenario "can be used when the platform wants to roll over the symmetric secret in one operation."

## hmac-secret vs hmac-secret-mc

The fundamental difference between the hmac-secret and hmac-secret-mc extensions is when the secret is returned by the YubiKey.

With hmac-secret, the secret is returned during ``GetAssertions()``. However, with hmac-secret-mc, the secret is returned during ``MakeCredential()``.

hmac-secret-mc is useful in situations where the secret is needed immediately upon creation of a new credential. By returning the secret in a single operation instead of two (``MakeCredential()`` followed by ``GetAssertions()``), the amount of user interaction required, including user presence and PIN/UV validation, is reduced.

## Verify support for hmac-secret and hmac-secret-mc

The hmac-secret-mc extension is only supported for YubiKeys with firmware version 5.8 and later. To verify whether a particular YubiKey supports the hmac-secret and hmac-secret-mc extensions, check the key's [AuthenticatorInfo](xref:Yubico.YubiKey.Fido2.AuthenticatorInfo):

```C#
    using (fido2Session = new Fido2Session(yubiKey))
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

To enable the hmac-secret extension, we must add it to the parameters for MakeCredential prior to calling the ``MakeCredential()`` method:

```csharp
    using (fido2Session = new Fido2Session(yubiKey))
    {
        // Your app's key collector, which will be used to check user presence and perform PIN/UV 
        // verification during MakeCredential().
        fido2Session.KeyCollector = SomeKeyCollectorDelegate;

        // Create the parameters for MakeCredential (relyingParty, userEntity, and clientDataHashValue
        // set elsewhere).
        var makeCredentialParameters = new MakeCredentialParameters(relyingParty, userEntity)
        {
            ClientDataHash = clientDataHashValue

        };

        // Add the hmac-secret extension plus the "rk" option (to make the credential discoverable).
        makeCredentialParameters.AddHmacSecretExtension(fido2Session.AuthenticatorInfo);
        makeCredentialParameters.AddOption(AuthenticatorOptions.rk, true);

        // Create the hmac-secret enabled credential.
        MakeCredentialData credentialData = fido2Session.MakeCredential(makeCredentialParameters);
    }
```

Once the extension has been added to a credential, we can return the secret with ``GetAssertions()``. However, we must first add the request for the secret along with the salt(s) to the parameters for GetAssertion via ``RequestHmacSecretExtension(salt1, salt2)``. Providing a second salt is not required.

```csharp
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
        // Your app's key collector, which will be used to check user presence and perform PIN/UV 
        // verification during GetAssertions().
        fido2Session.KeyCollector = SomeKeyCollectorDelegate;

        // Create the parameters for GetAssertion (relyingParty and clientDataHash set elsewhere)
        var getAssertionParameters = new GetAssertionParameters(relyingParty, clientDataHashCreate);

        // Add the request for the hmac secret and provide a salt (set elsewhere). 
        getAssertionParameters.RequestHmacSecretExtension(salt1);

        // Get the assertion and hmac secret using the parameters set above.
        IReadOnlyList<GetAssertionData> assertionDataList = fido2Session.GetAssertions(getAssertionParameters);
    }
```

The secret will be returned in the ``GetAssertionData``.

## Enabling the hmac-secret-mc extension and requesting the secret

With hmac-secret-mc, the extension needs to be enabled for a credential during ``MakeCredential()`` *and* the salt(s) must be provided in order to return the secret. To do so, we must add the extension and salt(s) to the parameters for MakeCredential prior to calling the ``MakeCredential()`` method.

If the operation is successful, the secret will be returned in the ``MakeCredentialData``.

```csharp
    using (fido2Session = new Fido2Session(yubiKey))
    {
        // Your app's key collector, which will be used to check user presence and perform PIN/UV 
        // verification during MakeCredential().
        fido2Session.KeyCollector = SomeKeyCollectorDelegate;

        // Create the parameters for MakeCredential (relyingParty, userEntity, and clientDataHashValue
        // set elsewhere).
        var makeCredentialParameters = new MakeCredentialParameters(relyingParty, userEntity)
        {
            ClientDataHash = clientDataHashValue

        };

        // Add the hmac-secret-mc extension with salts (set elsewhere) plus the "rk" option 
        // (to make the credential discoverable).
        makeCredentialParameters.AddHmacSecretMcExtension(fido2Session.AuthenticatorInfo, salt1, salt2);
        makeCredentialParameters.AddOption(AuthenticatorOptions.rk, true);

        // Create the credential and return the hmac secret.
        MakeCredentialData credentialData = fido2Session.MakeCredential(makeCredentialParameters);
    }
```

## Extracting and decrypting the secret

Once the secret has been returned, we can extract and decrypt it using the ``AuthenticatorData.GetHmacSecretExtension()`` method.

If the secret was generated using one salt, the result will be a 32-byte value. In the case of two salts, the result will be 64 bytes in length, with the first 32 bytes representing the value generated with `salt1` and the second 32 bytes representing the value generated with `salt2`.

To perform the extraction and decryption on ``GetAssertionData`` (hmac-secret), perform an operation like shown in the following code sample. Note that if authenticator data was returned for more that one credential, we must specify which credential's secret we'd like to extract.

```csharp
    using (fido2Session = new Fido2Session(yubiKey))
    {
        . . .
        // If the YubiKey contains multiple credentials for the relyingParty, this returns the hmac secret 
        // from the first credential's data.
        byte[] hmacSecretValue = assertionDataList[0].AuthenticatorData.GetHmacSecretExtension(fido2Session.AuthProtocol);
    }
```

For extraction and decryption on ``MakeCredentialData`` (hmac-secret-mc), we do not need to specify a particular credential given that only one was created at the time the secret was returned:

```csharp
    using (fido2Session = new Fido2Session(yubiKey))
    {
        . . .
        byte[] hmacSecretValue = credentialData.AuthenticatorData.GetHmacSecretExtension(fido2Session.AuthProtocol);
    }
```