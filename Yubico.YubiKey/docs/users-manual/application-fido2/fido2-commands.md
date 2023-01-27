---
uid: UsersManualFido2Commands
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

# FIDO2 commands

For each possible U2F command, there will be a class that knows how to build the command
[APDU](xref:UsersManualApdu) and parse the data in the response APDU. Each class will know
what information is needed from the caller for that command.

#### List of FIDO2 commands

* [Version](#get-version)
* [Get Info](#get-info)
* [Get Key Agreement](#get-key-agreement) (get a public key)
* [Set PIN](#set-pin)
* [Change PIN](#change-pin)
* [Get PIN Token](#get-pin-token)
* [Get PIN/UV Auth Token Using PIN](#pin-uv-auth-using-pin)
* [Get PIN/UV Auth Token Using UV](#pin-uv-auth-using-uv)
* [Make credential](#make-credential)
* [Get Assertion](#get-assertion)
* [Get Next Assertion](#get-next-assertion)
* [Get Credential Metadata](#get-credential-metadata)
* [Get Large Blob](#get-large-blob)
* [Set Large Blob](#set-large-blob)
* [Reset](#reset)
___
## Get version

Get the YubiKey's version number.

### Available

All YubiKeys with the FIDO2 application.

### SDK classes

--VersionCommand--xref:Yubico.YubiKey.Fido2.Commands.VersionCommand--

--VersionResponse--xref:Yubico.YubiKey.Fido2.Commands.VersionResponse--

### Input

None.

### Output

[FirmwareVersion](xref:Yubico.YubiKey.FirmwareVersion)

### APDU

[Technical APDU Details](apdu/version.md)
___
## Get info

Get information about the YubiKey's FIDO2 application.

### Available

All YubiKeys with the FIDO2 application.

### SDK classes

[GetInfoCommand](xref:Yubico.YubiKey.Fido2.Commands.GetInfoCommand)

[GetInfoResponse](xref:Yubico.YubiKey.Fido2.Commands.GetInfoResponse)

### Input

None.

### Output

[AuthenticatorInfo](xref:Yubico.YubiKey.Fido2.AuthenticatorInfo)

Also see the FIDO2 CTAP standard (CTAP 2.1), section 6.4 for a list of possible elements
returned.

The standard specifies 21 possible elements an authenticator can return from a GetInfo
command. Most of the elements are optional, so that any one encoding may or may not have
the same subset of possible key/value pairs.

The YubiKey can return up to 20 of the defined elements. It will not return
`vendorPrototypeConfigCommands`.

### APDU

[Technical APDU Details](apdu/get-info.md)
___
## Get key agreement

Get the YubiKey's public key that will be used to perform key agreement. The shared secret
result of key agreement will be used to derive a shared key used for PIN operations.

### Available

All YubiKeys with the FIDO2 application.

### SDK classes

[GetKeyAgreementCommand](xref:Yubico.YubiKey.Fido2.Commands.GetKeyAgreementCommand)

[GetKeyAgreementResponse](xref:Yubico.YubiKey.Fido2.Commands.GetKeyAgreementResponse)

### Input

[The UV/PIN Auth Protocol](xref:Yubico.YubiKey.Fido2.PinProtocols.PinUvAuthProtocol).

### Output

[The FIDO2 COSE EC Public Key](xref:Yubico.YubiKey.Fido2.Cose.CoseEcPublicKey)

### APDU

[Technical APDU Details](apdu/get-key-agree.md)
___
## Set PIN

Set the YubiKey's FIDO application to be PIN-protected.

### Available

All YubiKeys with the FIDO2 application.

### SDK classes

[SetPinCommand](xref:Yubico.YubiKey.Fido2.Commands.SetPinCommand)

[SetPinResponse](xref:Yubico.YubiKey.Fido2.Commands.SetPinResponse)

### Input

A [Protocol object](xref:Yubico.YubiKey.Fido2.PinProtocols.PinUvAuthProtocolBase) and the
PIN.

### Output

None

### APDU

[Technical APDU Details](apdu/set-pin.md)
___
## Change PIN

Change the YubiKey's FIDO application's PIN.

### Available

All YubiKeys with the FIDO2 application.

### SDK classes

[ChangePinCommand](xref:Yubico.YubiKey.Fido2.Commands.ChangePinCommand)

[ChangePinResponse](xref:Yubico.YubiKey.Fido2.Commands.ChangePinResponse)

### Input

A [Protocol object](xref:Yubico.YubiKey.Fido2.PinProtocols.PinUvAuthProtocolBase), the
current PIN and the new PIN.

### Output

None

### APDU

[Technical APDU Details](apdu/change-pin.md)
___
## Get PIN token

Get a PIN token, which can be used in later operations such as Make Credential.

There are actually three versions of "Get PIN Token":

* getPinToken
* getPinUvAuthTokenUsingPinWithPermissions
* getPinUvAuthTokenUsingUvWithPermissions

The SDK has three different command classes to call each of the three operations:

* GetPinTokenCommand
* [GetPinUvAuthTokenUsingPinCommand](#pin-uv-auth-using-pin)
* [GetPinUvAuthTokenUsingUvCommand](#pin-uv-auth-using-uv)

### Available

All YubiKeys with the FIDO2 application.

### SDK classes

[GetPinTokenCommand](xref:Yubico.YubiKey.Fido2.Commands.GetPinTokenCommand)

[GetPinUvAuthTokenResponse](xref:Yubico.YubiKey.Fido2.Commands.GetPinUvAuthTokenResponse)

### Input

* [The UV/PIN Auth Protocol](xref:Yubico.YubiKey.Fido2.PinProtocols.PinUvAuthProtocolBase)
* [The PIN](fido2-pin.md)

### Output

The encrypted token as a byte array.

### APDU

[Technical APDU Details](apdu/get-pin-token.md)
___
<a name="pin-uv-auth-using-pin"></a>
## Get PIN/UV Auth token using PIN

Get A PIN/UV Auth token, to be used in later operations such as Make Credential.

There are actually three versions of "Get PIN Token":

* getPinToken
* getPinUvAuthTokenUsingPinWithPermissions
* getPinUvAuthTokenUsingUvWithPermissions

The SDK has three different command classes to call each of the three operations:

* [GetPinTokenCommand](#get-pin-token)
* GetPinUvAuthTokenUsingPinCommand
* [GetPinUvAuthTokenUsingUvCommand](#pin-uv-auth-using-uv)

### Available

All YubiKeys with the FIDO2 application.

### SDK classes

[GetPinUvAuthTokenUsingPinCommand](xref:Yubico.YubiKey.Fido2.Commands.GetPinUvAuthTokenUsingPinCommand)

[GetPinUvAuthTokenResponse](xref:Yubico.YubiKey.Fido2.Commands.GetPinUvAuthTokenResponse)

### Input

* [The UV/PIN Auth Protocol](xref:Yubico.YubiKey.Fido2.PinProtocols.PinUvAuthProtocolBase)
* [The PIN](fido2-pin.md)
* A bit field listing the [permissions](xref:Yubico.YubiKey.Fido2.Commands.PinUvAuthTokenPermissions)
* An optional relying party ID (`rpId`)

### Output

The encrypted token as a byte array.

### APDU

[Technical APDU Details](apdu/get-auth-token-using-pin.md)
___
<a name="pin-uv-auth-using-uv"></a>
## Get PIN/UV Auth token using user verification (UV)

Get A PIN/UV Auth token, to be used in later operations such as Make Credential.

There are actually three versions of "Get PIN Token":

* getPinToken
* getPinUvAuthTokenUsingPinWithPermissions
* getPinUvAuthTokenUsingUvWithPermissions

The SDK has three different command classes to call each of the three operations:

* [GetPinTokenCommand](#get-pin-token)
* [GetPinUvAuthTokenUsingPinCommand](#pin-uv-auth-using-pin)
* GetPinUvAuthTokenUsingUvCommand

### Available

All YubiKeys with the FIDO2 application.

### SDK classes

[GetPinUvAuthTokenUsingUvCommand](xref:Yubico.YubiKey.Fido2.Commands.GetPinUvAuthTokenUsingUvCommand)

[GetPinUvAuthTokenResponse](xref:Yubico.YubiKey.Fido2.Commands.GetPinUvAuthTokenResponse)

### Input

* [The UV/PIN Auth Protocol](xref:Yubico.YubiKey.Fido2.PinProtocols.PinUvAuthProtocolBase)
* A bit field listing the [permissions](xref:Yubico.YubiKey.Fido2.Commands.PinUvAuthTokenPermissions)
* An optional relying party ID (`rpId`)

### Output

The encrypted token as a byte array.

### APDU

[Technical APDU Details](apdu/get-auth-token-using-uv.md)
___
## Make credential

Make a credential for a relying party.

### Available

All YubiKeys with the FIDO2 application.

### SDK classes

[MakeCredentialCommand](xref:Yubico.YubiKey.Fido2.Commands.MakeCredentialCommand)

[MakeCredentialResponse](xref:Yubico.YubiKey.Fido2.Commands.MakeCredentialResponse)

### Input

The `authenticatorMakeCredential` parameters specified in section 6.1 of the FIDO2
specifications.

### Output

The credential (public key) and other information.

[MakeCredentialData](xref:Yubico.YubiKey.Fido2.MakeCredentialData)

### APDU

[Technical APDU Details](apdu/make-credential.md)
___
## Get assertion

Get an assertion (credential) that will be verified by a relying party.

### Available

All YubiKeys with the FIDO2 application.

### SDK classes

[GetAssertionCommand](xref:Yubico.YubiKey.Fido2.Commands.GetAssertionCommand)

[GetAssertionResponse](xref:Yubico.YubiKey.Fido2.Commands.GetAssertionResponse)

### Input

The `authenticatorGetAssertion` parameters specified in section 6.2 of the FIDO2
specifications.

[GetAssertionParameters](xref:Yubico.YubiKey.Fido2.GetAssertionParameters)

### Output

The credential, along with other information.

[GetAssertionData](xref:Yubico.YubiKey.Fido2.GetAssertionData)

### APDU

[Technical APDU Details](apdu/get-assertion.md)
___
## Get next assertion

Get the next assertion (credential) associated with the relying party specified
in a previous call to [Get Assertion])(get-assertion).

### Available

All YubiKeys with the FIDO2 application.

### SDK classes

[GetNextAssertionCommand](xref:Yubico.YubiKey.Fido2.Commands.GetNextAssertionCommand)

[GetAssertionResponse](xref:Yubico.YubiKey.Fido2.Commands.GetAssertionResponse)

Note that the response to `GetNextAssertion` is the same as the response to
`GetAssertion`.

### Input

None.

### Output

The credential, along with other information.

[GetAssertionData](xref:Yubico.YubiKey.Fido2.GetAssertionData)

### APDU

[Technical APDU Details](apdu/get-next-assertion.md)
___
## Get credential metadata

Get information about the credentials on the YubiKey. This is one of the sub-commands of
the `authenticatorCredentialManagement` command.

Not all YubiKeys support credential management. If you send this command to a YubiKey that
does not support it, the response will be "Unsupported option".

### Available

All YubiKeys with the FIDO2 application.

### SDK classes

[GetCredentialMetadataCommand](xref:Yubico.YubiKey.Fido2.Commands.GetCredentialMetadataCommand)

[CredentialManagementResponse](xref:Yubico.YubiKey.Fido2.Commands.CredentialManagementResponse)

### Input

None.

### Output

The number of existing discoverable credentials on the YubiKey, and the maximum number of
additional credentials the YubiKey can store.

The data is returned in the form of a
[CredentialManagementData](xref:Yubico.YubiKey.Fido2.CredentialManagementData) object.

### APDU

[Technical APDU Details](apdu/get-cred-metadata.md)
___
## Get large blob

Get the large blob data out of the YubiKey. This command gets the raw data, it does not
perform any parsing or decoding.

Not all YubiKeys support large blobs. If you send this command to a YubiKey that does not
support it, the response will be "Unsupported extension".

### Available

All YubiKeys with the FIDO2 application.

### SDK classes

[GetLargeBlobCommand](xref:Yubico.YubiKey.Fido2.Commands.GetLargeBlobCommand)

[GetLargeBlobResponse](xref:Yubico.YubiKey.Fido2.Commands.GetLargeBlobResponse)

### Input

offset and count

Because a large blob can be bigger than the maximum message length, it is possible
retrieving the entire data will require more than one call. The offset specifies the
offset in the large blob data on the YubiKey where the returned data should begin. The
first call specifies an offset of zero, and each subsequent call specifies an offset of
the total number of bytes returned so far by each previous call.

The count is the number of bytes requested this call. This value must be less than or
equal to the "maximum fragment length". There is a maximum message size (specified by the
YubiKey and found in the `AuthenticatorInfo`) and the `MaxFragmentLength` is the
`MaxMessageSize - 64`.

### Output

The bytes the YubiKey was able to return. This is in the form of a `ReadOnlyMemory<byte>`.
If the number of bytes returned is less than the count given, then there are no more bytes
to return. If the number is equal to the count, there could be more bytes on the YubiKey,
and the caller should send another command.

### APDU

[Technical APDU Details](apdu/get-large-blob.md)
___
## Set large blob

Store large blob data on the YubiKey. This command stores the data given, it does not
perform any encoding. This replaces any data currently in the large blob storage area on
the YubiKey.

Not all YubiKeys support large blobs. If you send this command to a YubiKey that does not
support it, the response will be "Unsupported extension".

### Available

All YubiKeys with the FIDO2 application.

### SDK classes

[SetLargeBlobCommand](xref:Yubico.YubiKey.Fido2.Commands.SetLargeBlobCommand)

[SetLargeBlobResponse](xref:Yubico.YubiKey.Fido2.Commands.SetLargeBlobResponse)

### Input

data to store, offset, count, PinUvAuthParam, PinProtocol

Because a large blob can be bigger than the maximum message length, it is possible storing
the entire data will require more than one call. The offset specifies the offset in the
large blob data on the YubiKey where the input data should be stored. The first call
specifies an offset of zero, and each subsequent call specifies an offset of the total
number of bytes stored so far by each previous call.

The count is the total number of bytes that will be stored. That is, it is the sum of all
the lengths of bytes stored by each call. The first time the set command is called, the
offset is zero and the count is the total number of bytes. Each subsequent call the offset
is where the previous call left off and the count is ignored.

Each block of input must be less than or equal to `maxFragmentLength` bytes
(`MaxMessageSize - 64`).

The caller need authorization to store, and obtains that by generating a PinUvAuthParam.

### Output

None

### APDU

[Technical APDU Details](apdu/set-large-blob.md)
___
## Reset

Reset the FIDO2 application on a YubiKey. This will delete all existing FIDO2 keys and
credentials, and remove the PIN.

It is not sufficient to simply execute this command in order to reset, it must be done
within a time limit of inserting a YubiKey and must be accompanied by a proof of user
presence (touch).

### Available

All YubiKeys with the FIDO2 application.

### SDK classes

[ResetCommand](xref:Yubico.YubiKey.Fido2.Commands.ResetCommand)

[ResetResponse](xref:Yubico.YubiKey.Fido2.Commands.ResetResponse)

### Input

None.

### Output

None

### APDU

[Technical APDU Details](apdu/reset.md)
___
