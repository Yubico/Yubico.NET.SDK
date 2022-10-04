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

none

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

none

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

Make a credential for a relying party. This is the 

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
