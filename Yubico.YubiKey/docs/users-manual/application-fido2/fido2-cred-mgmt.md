---
uid: Fido2CredentialManagement
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

# FIDO2 credential management

The credential management operations allow you to obtain information about the credentials
on a YubiKey without getting an assertion. Note that you can get information only for
discoverable credentials. Remember that to make a credential discoverable, when you make
it (see [MakeCredential](xref:Yubico.YubiKey.Fido2.Fido2Session.MakeCredential%2a)), set
the "`rk`" option to `true`

```csharp
    var makeCredentialParameters = new MakeCredentialParameters(relyingParty, userEntity)
    {
        ClientDataHash = clientDataHash,
    };
    makeCredParams.AddOption(AuthenticatorOptions.rk, true);
    MakeCredentialData credentialData = fido2Session.MakeCredential(makeCredentialParameters);
```

These are the credential management operations:

* [Get Metadata](#get-metadata)
* [Enumerate Relying Parties](#enumerate-relying-parties)
* [Enumerate Credentials](#enumerate-credentials)
* [Delete Credential](#delete-credential)
* [Update User Information](#update-user-information)

In the SDK, the operations that return data will return an instance of the
[CredentialManagementData](xref:Yubico.YubiKey.Fido2.CredentialManagementData) class. That
class contains properties for all possible return values.

* `NumberOfDiscoverableCredentials`
* `RemainingCredentialCount`
* `RelyingParty`
* `RelyingPartyIdHash`
* `TotalRelyingPartyCount`
* `User`
* `CredentialId`
* `CredentialPublicKey`
* `TotalCredentialsForRelyingParty`
* `CredProtectPolicy`
* `LargeBlobKey`

Each operation that does return data will return two or more of these elements. After
performing an operation, look at the resulting `CredentialManagementData` object. Some of
the properties will be null (that operation did not return that item) or not null and
be the information requested.

## PIN/UV Auth Param

In order to perform a credential management operation, it is necessary to compute a
PIN/UV Auth Param.

## Get metadata

This returns the number of discoverable credentials and the number of "empty" slots. For
example, suppose the YubiKey has space for 25 credentials. Currently there are three
discoverable credentials and two non-discoverable. The return from the credential
management operation of get metadata would be 3 and 20. The number of remaining credential
count of 20 means that it is possible to store 20 more credentials, any combination of
discoverable and non-discoverable.

The return is a `CredentialManagementData` object, with the properties set as follows.

```txt
  `NumberOfDiscoverableCredentials  = 3`
  `RemainingCredentialCount = 20`
  `RelyingParty = null`
  `RelyingPartyIdHash = null`
  `TotalRelyingPartyCount = null`
  `User = null`
  `CredentialId = null`
  `CredentialPublicKey = null`
  `TotalCredentialsForRelyingParty = null`
  `CredProtectPolicy = null`
  `LargeBlobKey = null`
```

## Enumerate relying parties

This helps you to build a list of all the relying parties represented among all the
credentials on the YubiKey.

## Enumerate credentials

This helps you to build a list of all the credentials on the YubiKey.

## Delete credential

Remove one credential from the YubiKey.

## Update user information

Each credential contains user information, represented as an instance of the
[UserEntity](xref:Yubico.YubiKey.Fido2.UserEntity) class. You can change what user
information is stored on the YubiKey in that credential.
