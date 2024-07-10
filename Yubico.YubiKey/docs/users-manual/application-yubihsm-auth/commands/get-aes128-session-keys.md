---
uid: YubiHsmAuthCmdGetAes128SessionKeys
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

# Get AES-128 session keys

Get the SCP03 session keys from an AES-128 credential.

## Available

All YubiKeys with the YubiHSM Auth application (included in firmware version 5.4.3 and later).
> [!NOTE]
> Use the .NET
>
API's [HasFeature()](xref:Yubico.YubiKey.YubiKeyFeatureExtensions.HasFeature%28Yubico.YubiKey.IYubiKeyDevice%2CYubico.YubiKey.YubiKeyFeature%29)
> method to check if a key has the YubiHSM Auth application.

## SDK classes

* [GetAes128SessionKeysCommand](xref:Yubico.YubiKey.YubiHsmAuth.Commands.GetAes128SessionKeysCommand)
* [GetAes128SessionKeysResponse](xref:Yubico.YubiKey.YubiHsmAuth.Commands.GetAes128SessionKeysResponse)

## Input

Before calling this operation, the host needs to generate an 8-byte challenge (the host challenge). The host challenge
is typically generated using a random or pseudorandom method. The host sends the host challenge to the HSM device, which
returns its own 8-byte challenge (the HSM device challenge).
See [YubiHSM Shell](https://developers.yubico.com/yubihsm-shell/) for ways to communicate with the HSM device.

To call GetAes128SessionKeysCommand, you must pass it the label and password of the AES-128 credential that will be used
to calculate the SCP03 session keys as well as the host challenge and HSM device challenge from the initial step. There
is a limit of 8 attempts to authenticate with the password before the credential is deleted. Once the credential is
deleted, it cannot be recovered. Supplying the correct password before the credential is deleted will reset the retry
counter to 8.

The credential may require proof of user presence. This is configured when the credential is added (
see [AddCredentialCommand](xref:YubiHsmAuthCmdAddCredential)). In this case, the user must touch the YubiKey in order to
complete the authentication procedure. Otherwise, the command will fail (though the credential password retry counter
does not change).

## Output

An array which contains the ENC, MAC, and R-MAC session keys. Each key is exactly 16-bytes long.

In the case of a failure, the status word in the response may include further information. For example, the credential
was configured to require touch, but the user did not touch the YubiKey.

## Command APDU

| CLA | INS | P1 | P2 |     Lc     |       Data       |    Le    |
|:---:|:---:|:--:|:--:|:----------:|:----------------:|:--------:|
| 00  | 03  | 00 | 00 | *variable* | (TLV, see below) | (absent) |

### Data

The data is sent as concatenated TLV-formatted elements, as follows:

| Tag (hexadecimal) | Length (decimal) |                 Value                  | Notes                                            |
|:-----------------:|:----------------:|:--------------------------------------:|:-------------------------------------------------|
|       0x71        |       1-64       |                 label                  | UTF-8 encoded string                             |
|       0x77        |        16        | {host challenge, HSM device challenge} | challenges as byte arrays, concatenated together |
|       0x73        |        16        |                password                | byte array                                       |

## Response APDU

Total Length: *50*\
Data Length: *48*

|       Data        | SW1 | SW2 |
|:-----------------:|:---:|:---:|
| {ENC, MAC, R-MAC} | 90  | 00  |