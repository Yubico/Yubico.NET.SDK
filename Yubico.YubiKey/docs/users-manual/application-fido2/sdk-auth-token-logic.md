---
uid: SdkAuthTokenLogic
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

# The SDK's "automatic" AuthToken logic

Before reading this document, make sure you understand AuthTokens. See
[this User's Manual article on AuthTokens](xref:Fido2AuthTokens), permissions, PIN/UV, and
AuthParams for a detailed discussion.

In order to perform most FIDO2 operations, you will need an AuthToken. This document
discusses what the SDK does to obtain AuthTokens, when the caller has not done that work.
That is, it is possible for the programmer to make calls to `Verify` methods which will
obtain AuthTokens, and the SDK will use them when they need them. But it is also possible
to simply call on the SDK's FIDO2 classes and methods and let them obtain any AuthToken
they need. The task of AuthToken retrieval is done "automatically" by the SDK.

Note that this is applicable only when using the
[Fido2Session](xref:Yubico.YubiKey.Fido2.Fido2Session) class.

## KeyCollector

In order to allow the SDK to obtain AuthTokens automatically, you must supply a
KeyCollector.

## The `Fido2Session.AuthToken` property

If the SDK will be calling on the YubiKey to perform some operation that requires
authentication, it will first simply use the AuthToken currently in the
[AuthToken](xref:Yubico.YubiKey.Fido2.Fido2Session.AuthToken) property. If the operation
works, there was no need to retrieve a new AuthToken.

If the operation returns from the YubiKey with the error CTAP2_ERR_PIN_AUTH_INVALID, the
SDK will obtain a new AuthToken making sure the new one has the appropriate permissions.
The `AuthToken` property will be updated with the new AuthToken.

## PinToken versus PinUvAuthToken

If the connected YubiKey supports only FIDO2 version 2.0, which does not have the concept
of permissions, this will retrieve a PinToken (user verification, i.e. fingerprints, is
not possible). Generally, on a FIDO2 version 2.0 device, the only operations are
`MakeCredential` and `GetAssertion`.

## The AuthTokenPermissions and AuthTokenRelyingPartyId properties

The [AuthTokenPermissions](xref:Yubico.YubiKey.Fido2.Fido2Session.AuthTokenPermissions)
property usually contains the permissions of the last AuthToken retrieved. The
[AuthTokenRelyingPartyId](xref:Yubico.YubiKey.Fido2.Fido2Session.AuthTokenRelyingPartyId)
contains the relying party ID specified when retrieving the latest AuthToken. An
exception is described
[below](#the-credentialmetadata-and-enumeraterelyingparties-exception).

Whenever the SDK retrieves a new AuthToken, it will use as the permissions the combination
of the new, required permission with the permissions specified in the
`AuthTokenPermissions` property (exception
[below](#the-credentialmetadata-and-enumeraterelyingparties-exception)).

For example, if the current `AuthTokenPermissions` property is `CredentialManagement`, and
the SDK needs to perform `BioEnrollment`, it will need a new AuthToken. It will retrieve
one with `CredentialManagement | BioEnrollment`, and replace the `AuthTokenPermissions`
property with the new combination.

Also, when retrieving a new AuthToken, it will use the value in `AuthTokenRelyingPartyId`
as the relying party (exception
[below](#the-credentialmetadata-and-enumeraterelyingparties-exception)).

If the SDK's operation requires a new relying party ID, it will use the new ID and replace
the contents of the `AuthTokenRelyingPartyId` property,

## [ClearAuthToken](xref:Yubico.YubiKey.Fido2.Fido2Session.ClearAuthToken)

This method will reset the `AuthToken`, `AuthTokenPermissions`, and
`AuthTokenRelyingPartyId` properties to null. In this way, you can "start over".

## [AddPermissions](xref:Yubico.YubiKey.Fido2.Fido2Session.AddPermissions%2a)

Call this method at the beginning of a session to obtain an AuthToken with an initial set
of permissions and an initial relying party. You provide as the permissions argument the
set of permissions you are going to be performing in the session. For example, if you know
that you will be performing (or might be performing) both GetAssertion and LargeBlobWrite,
You can call `AddPermissions` with
`PinUvAuthTokenPermissions.GetAssertion | PinUvAuthTokenPermissions.LargeBlobWrite` (and
the appropriate relying party ID).

The SDK will then retrieve an AuthToken with those permissions. Later on, when the SDK
makes a call to get an assertion, the AuthToken will be valid. Then when it makes a call
to write to the LargeBlob, that will work as well, there's no need for the SDK to retrieve
a new AuthToken.

Of course, because of [expiry](fido2-auth-tokens.md#expiry), it is possible an initial
AuthToken might not work. For example, suppose you call `AddPermissions` with
`GetAssertion`, `CredentialManagement`, and `LargeBlobWrite`. After getting an assertion
the AuthToken is expired, meaning it loses permissions. The standard specifies that expiry
means losing all permissions except `LargeBlobWrite` (see the FIDO2 standard, section
6.5.5.7). Hence, even though you specified CredentialManagement at the beginning, the
original AuthToken can no longer be used to build an AuthParam that will authenticate a
CredentialManagement operation.

You can call this method any time, not just at the beginning. This call will add the new
permissions to the ones in the `AuthTokenPermissions` property. If you provide a new
relying party ID, it will replace the one in the `AuthTokenRelyingPartyId` property. If
you pass null for the relying party ID, the original will remain. That is, you cannot
remove the existing `RelyingPartyId` property, except by calling `ClearAuthToken`.

## The CredentialMetadata and EnumerateRelyingParties exception

Suppose the last AuthToken we retrieved had the permisisons `GetAssertion` and
`CredentialManagement`, and a relying party ID of "example.com". Now suppose you call
[GetCredentialMetadata](xref:Yubico.YubiKey.Fido2.Fido2Session.GetCredentialMetadata).
That method needs an AuthToken with the `CredentialManagement` permission but there can be
no relying party ID (see the section in the User's Manual AuthToken article on
[CredentialManagement permission](fido2-auth-tokens.md#credentialmanagement-permission)).

The SDK will try the AuthToken it has, but that won't work, even though the
`AuthTokenPermissions` property includes `CredentialManagement`, because there is a
relying party connected to that AuthToken.

At this point the SDK will need a new AuthToken. Normally, it would build a new one by
"adding permissions" to the existing `AuthTokenPermissions` and using the existing
`AuthTokenRelyingPartyId`. But that won't work here.

In this case, the SDK will build an AuthToken with only the `CredentialManagement`
permission, and no relying party ID. It will then be able to perform the operation.

After the metadata is retrieved, the SDK will make sure the `AuthTokenPermissions` and
`AuthTokenRelyingPartyId` properties are set to their original values (the permisisons
property will now contain. These properties do
not reflect the state of the current `AuthToken`, but they are retained in case future
operations need them.

Suppose you make a call to `GetCredentialMetadata`, which results in the `AuthToken`
property now containing an AuthToken with only the `CredentialManagement` permission. Now
suppose you call `EnumerateRelyingParties`. That is an operation that also requires an
AuthToken with only the `CredentialManagement` permission. The SDK will first try the
`AuthToken` property and that will work.

This is because the SDK will always try the existing AuthToken first. It does not look at
the `AuthTokenPermissions` and `AuthTokenRelyingPartyId` to decide if it should use the
existing. AuthToken.
