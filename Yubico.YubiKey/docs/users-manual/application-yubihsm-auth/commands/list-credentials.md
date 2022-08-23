---
uid: YubiHsmAuthCmdListCredentials
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

# List credentials

Get the public properties of all credentials present in the YubiHSM Auth application along with the number of retries remaining for each.

## Available

All YubiKeys with the YubiHSM Auth application (included in firmware version 5.4.3 and later).
> [!NOTE]
> Use the .NET API's [HasFeature()](xref:Yubico.YubiKey.YubiKeyFeatureExtensions.HasFeature%28Yubico.YubiKey.IYubiKeyDevice%2CYubico.YubiKey.YubiKeyFeature%29) method to check if a key has the YubiHSM Auth application."

## SDK classes

* [ListCredentialsCommand](xref:Yubico.YubiKey.YubiHsmAuth.Commands.ListCredentialsCommand)
* [ListCredentialsResponse](xref:Yubico.YubiKey.YubiHsmAuth.Commands.ListCredentialsResponse)

## Input

None.

## Output

A byte array formatted as a series of TLVs, where each element is a credential and its number of remaining retries. Each element in the series begins with the Tag 0x72 (known as LabelList). The data is formatted in the following order:

| Order | Meaning | Size (bytes) | Comments |
| :---: | :---: | :---: | :---: |
| 1 | Cryptographic key type | 1 | See [CryptographicKeyType](xref:Yubico.YubiKey.YubiHsmAuth.CryptographicKeyType) |
| 2 | Touch required | 1 | Boolean |
| 3 | Label | 1-64 | ASCII string |
| 4 | Retries remaining | 1 | Positive integer |

For example, for a YubiKey with two credentials stored in the YubiHSM Auth application, the response data (in hexadecimal) might look like:

```text
Byte array: 72 07 26 00 61 62 63 00 04 72 08 26 01 77 78 79 7A 00 00

Notated:
72 07           Tag: LabelList, Length: 7
    26              Key type: AES-128
    00              Touch required: False
    61 62 63 00     Label: 'abc\0'
    04              Retries: 4
72 08           Tag: LabelList, Length: 8
    26              Key type: AES-128
    01              Touch required: True
    77 78 79 7A 00  Label: 'wxyz\0'
    00              Retries: 0
```

## Command APDU

| CLA | INS | P1 | P2 | Lc | Data | Le |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| 00 | 05 | 00 | 00 | (absent) | (absent) | (absent) |

## Response APDU

Total Length: *variable + 2*\
Data Length: *variable*

| Data | SW1 | SW2 |
| :---: | :---: | :---: |
| *data* | 90 | 00 |
