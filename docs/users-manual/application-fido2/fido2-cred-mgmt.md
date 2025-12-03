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

## Support in the YubiKey

Not all YubiKeys support CredentialManagement. To find out if a particular YubiKey can
perform these operations, check for the "`credMgmt`" options.

```csharp
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
        if (fido2Session.AuthenticatorInfo.GetOptionValue("credMgmt") == OptionValue.True)
        {
            . . .
        }
    }
```

## Commands and Fido2Session methods

In the SDK, there are two ways to perform a CredentialManagement operation:

* Commands
* Fido2Session methods

The commands are

* [GetCredentialMetadataCommand](xref:Yubico.YubiKey.Fido2.Commands.GetCredentialMetadataCommand)
* [EnumerateRpsBeginCommand](xref:Yubico.YubiKey.Fido2.Commands.EnumerateRpsBeginCommand)
* [EnumerateRpsGetNextCommand](xref:Yubico.YubiKey.Fido2.Commands.EnumerateRpsGetNextCommand)
* [EnumerateCredentialsBeginCommand](xref:Yubico.YubiKey.Fido2.Commands.EnumerateCredentialsBeginCommand)
* [EnumerateCredentialsGetNextCommand](xref:Yubico.YubiKey.Fido2.Commands.EnumerateCredentialsGetNextCommand)
* [DeleteCredentialCommand](xref:Yubico.YubiKey.Fido2.Commands.DeleteCredentialCommand)
* [UpdateUserInfoCommand](xref:Yubico.YubiKey.Fido2.Commands.UpdateUserInfoCommand)

Some of the commands require a PinToken. You will be responsible for building a AuthToken
(see next section).

The Fido2Session methods are

* [GetCredentialMetadata](xref:Yubico.YubiKey.Fido2.Fido2Session.GetCredentialMetadata)
* [EnumerateRelyingParties](xref:Yubico.YubiKey.Fido2.Fido2Session.EnumerateRelyingParties)
* [EnumerateCredentialsForRelyingParty](xref:Yubico.YubiKey.Fido2.Fido2Session.EnumerateCredentialsForRelyingParty%2a)
* [DeleteCredential](xref:Yubico.YubiKey.Fido2.Fido2Session.DeleteCredential%2a)
* [UpdateUserInfoForCredential](xref:Yubico.YubiKey.Fido2.Fido2Session.UpdateUserInfoForCredential%2a)

If you use these methods, the SDK will build the proper AuthToken if needed.

## PIN/UV Auth Param

In order to perform some credential management operations, it is necessary to compute a
PIN/UV Auth Param. The SDK will build the PIN/UV Auth Param, you do not need to supply it.
The PIN/UV Auth Param is built using an `AuthToken`. If you use the Fido2Session methods,
the SDK will also obtain the `AuthToken`.

If supported by the YubiKey (firmware 5.8 or later), the read-only credential management operations (Get Metadata, Enumerate Relying Parties, and Enumerate Credentials) can use a special type of AuthToken called the Persistent PinUvAuthToken (PPUAT). PPUATs allow for frequent reuse, reducing the need for repeated PIN entry.

See the User's Manual [entry on AuthTokens](xref:Fido2AuthTokens) for a detailed
discussion on how they work.

## Get metadata

This returns the number of discoverable credentials and the number of "empty" slots. For
example, suppose the YubiKey has space for 25 credentials. Currently there are three
discoverable credentials and two non-discoverable. The return from the credential
management operation of get metadata would be 3 and 22. The number of remaining credential
count of 22 means that it is possible to store 22 more discoverable credentials. The
YubiKey stores no information on non-discoverable credentials, so the two non-discoverable
credentials in this example have no effect on the number of spaces available. See also the
User's Manual [entry on credentials](fido2-credentials.md) for more information on
non-discoverable credentials.

The return is a `Tuple` of 2 integers:

```csharp
    (int residentCredentialCount, int remainingCredentialCount) = fido2Session.GetCredentialMetadata();

    // In the example above, residentCredentialCount would be 3 and
    // remainingCredentialCount would be 22.
```

## Enumerate relying parties

This helps you to build a list of all the relying parties represented among all the
credentials on the YubiKey.

If you use Fido2Session.EnumerateRelyingParties, the SDK will return an array of
`RelyingParty` objects.

If you use the commands, you will need to use the `EnumerateRpsBeginCommand` command to
obtain the first relying party and the total count of relying parties represented, and
then the `EnumerateRpsGetNextCommand` to get each successive relying party.

```csharp
    var enumBeginCmd = new EnumerateRpsBeginCommand(pinToken, protocol);
    EnumerateRpsBeginResponse enumBeginRsp = connection.SendCommand(enumBeginCmd);

    (int rpCount, RelyingParty firstRp) = enumBeginRsp.GetData();

    for (int index = 1; index < rpCount; index++)
    {
        var getNextCmd = new EnumerateRpsGetNextCommand();
        EnumerateRpsGetNextResponse credMgmtRsp = connection.SendCommand(getNextCmd);
        RelyingParty nextRp = getNextRsp.GetData();
    }
```

## Enumerate credentials

This helps you to build a list of all the credentials on the YubiKey.

If you use Fido2Session.EnumerateCredentialsForRelyingParty, the SDK will return an array
of [CredentialUserInfo](xref:Yubico.YubiKey.Fido2.CredentialUserInfo) objects, each one
containing the `User`, `CredentialId`, `CredentialPublicKey`, `CredProtectPolicy`, and the
`LargeBlobKey` (if there is one) for each credential found on the YubiKey associated with
the specified relying party. You specify which relying party you are interested in by
supplying the `RelyingParty` object, which you likely retrieved during a call to obtain a
list of relying parties.

If you use the commands, you will need to use the `EnumerateCredentialsBeginCommand`
command to obtain the first credential and the total count of credentials available, and
then the `EnumerateCredentialsGetNextCommand` to get each successive credential.

```csharp
    var enumBeginCmd = new EnumerateCredentialsBeginCommand(relyingParty.RelyingPartyIdHash, pinToken, protocol);
    EnumerateCredentialsBeginResponse enumBeginRsp = connection.SendCommand(enumBeginCmd);

    (int credCount, CredentialUserInfo userInfo) = enumBeginRsp.GetData();

    for (int index = 1; index < credCount; index++)
    {
        var getNextCmd = new EnumerateCredentialsGetNextCommand();
        EnumerateCredentialsGetNextResponse getNextRsp = connection.SendCommand(getNextCmd);
        userInfo = getNextRsp.GetData();
    }
```

## Delete credential

This allows you to remove one credential from the YubiKey.

Whether you use the [command](xref:Yubico.YubiKey.Fido2.Commands.DeleteCredentialCommand)
or the Fido2Session method, you must supply the CredentialId. This tells the YubiKey which
credential to remove. You will likely use the
[Enumerate commands](xref:Yubico.YubiKey.Fido2.Commands.EnumerateCredentialsBeginCommand)
or the Fido2Session.EnumerateCredentialsForRelyingParty method to obtain a list of
[CredentialUserInfo](xref:Yubico.YubiKey.Fido2.CredentialUserInfo) objects,
and choose the credential to delete from that list. Finally, you can use the
[CredentialId](xref:Yubico.YubiKey.Fido2.CredentialUserInfo.CredentialId) property
in the object as the input to the delete call.

This operation needs the [PIN/UV Auth Param](#pinuv-auth-param).

It is possible that there is some [large blob](large-blobs.md) data stored against the
credential you are deleting. If so, you will likely want to delete that data as well. If
you use the commands to delete, it is your responsibility to delete the large blob data.
The `Fido2Session` method will delete it for you.

## Update user information

Each credential contains user information, represented as an instance of the
[UserEntity](xref:Yubico.YubiKey.Fido2.UserEntity) class. You can change what user
information is stored on the YubiKey in that credential.

The way to change the user information is to create a new `UserEntity` object, and then
call the command or the `Fido2Session` method. This replaces the information on the
YubiKey, it does not "edit" it.

For example,

```csharp
    // Find the relying party of interest by enumerating all RPs and selecting from the list.
    IReadOnlyList<RelyingParty> rpList = fido2Session.EnumerateRelyingParties();
    int index = ChooseRelyingParty(rpList);

    // Find the credential of interest by enumerating all the credentials associated with
    // the relying party of intereset and selecting from the list.
    IReadOnlyList<CredentialUserInfo> credList =
        fido2Session.EnumerateCredentialsForRelyingParty(rpList[index]);
    index = ChooseCredential(credList);

    // Create a new UserEntity based on the current.
    var updatedUserInfo = new UserEntity(credArray[index].User.Id)
    {
        Name = credArray[index].User.Name,
        DisplayName = "Jane Doe",
    };

    fido2Session.UpdateUserInfoForCredential(credArray[index].CredentialId, updatedUserInfo);
```

Suppose the original user information was the following:

* Id = 0x3A 67 ... E9
* Name = jdoe
* DisplayName = J Doe

In the sample, the display name was changed to "Jane Doe". It built a new `UserEntity`
object with the following:

* Id = 0x3A 67 ... E9
* Name = jdoe
* DisplayName = Jane Doe

Then it called the update method.

If it had supplied a `UserEntity` object with only the display name (because that is all
it needed to change), then after the update, the YubiKey would have contained an entry for
a user with no `Id` and no `Name`, just a `DisplayName`.

You must supply all the user information in the updated object. That is, the object you
provide as the update must include all the info that does not change as well as the info
that does.
