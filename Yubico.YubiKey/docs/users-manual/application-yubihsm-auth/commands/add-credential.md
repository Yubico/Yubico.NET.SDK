---
uid: YubiHsmAuthCmdAddCredential
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

# Add credential

Store long-lived keys in the YubiHSM Auth application by creating a new credential.

## Available

All YubiKeys with the YubiHSM Auth application (included in firmware version 5.4.3 and later).
> [!NOTE]
> Use the .NET API's [HasFeature()](xref:Yubico.YubiKey.YubiKeyFeatureExtensions.HasFeature%28Yubico.YubiKey.IYubiKeyDevice%2CYubico.YubiKey.YubiKeyFeature%29) method to check if a key has the YubiHSM Auth application."
## SDK classes

* AddCredentialCommand
* AddCredentialResponse

## Input

This operation requires authenticating to the YubiHSM Auth application as part of the command. This is done by providing the application's management key (16 bytes). The rest of the input data is related to the new credential:

* Label (64 bytes)
* Algorithm (1 byte)
* Keys (length depends on algorithm)
* Credential password (16 bytes)
* Touch policy (1 byte)

Further information is found in the [Data](#data) section.

> [!NOTE]
> The label may only contain characters that can be encoded with UTF-8, and its UTF-8 byte count must be between 1 and 64. Non-printing characters are allowed, as long as they can be encoded with UTF-8. For example, null (UTF-8: 0x00) and Right-To-Left Mark U+200F (UTF-8: 0xE2 0x80 0x8F) would be accepted. Since the label is used for display purposes, it is recommended to prefer printable characters.

## Output

None, though some information may be included in the status word when the command fails (see [Response APDU](#response-apdu)).

## Command APDU

| CLA | INS | P1 | P2 | Lc | Data | Le |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| 00 | 01 | 00 | 00 | *variable* | (TLV, see below) | (absent) |

### Data

The data is sent as concatenated TLV-formatted elements, as follows:

| Tag (hexadecimal) | Length (decimal) | Value | Notes |
| :---: | :---: | :---: | :--- |
| 0x7b | 16 | managment key | used to authenticate to the YubiHSM Auth application |
| 0x71 | 1-64 | label | UTF-8 encoded string |
| 0x74 | 1 | cryptographic key type | See [CryptographicKeyType](xref:Yubico.YubiKey.YubiHsmAuth.CryptographicKeyType) |
| 0x75 | 16 | ENC key | |
| 0x76 | 16 | MAC key | |
| 0x73 | 16 | password | byte array |
| 0x7a | 1 | touch required | boolean: 0-not required, 1-required |

### Example

The following example is in hexadecimal.

```text
Byte array: 00 01 00 00 4c 7b 10 00 01 02 03 04 05 06 07 08 09 0a 0b 0c/
            0d 0e 0f 71 03 61 62 63 74 01 26 75 10 ca fe b0 ba ca fe b0/
            ba ca fe b0 ba ca fe b0 ba 76 10 13 37 f0 0d 13 37 f0 0d 13/
            37 f0 0d 13 37 f0 0d 73 10 a0 a1 a2 a3 b0 b1 b2 b3 c0 c1 c2/
            c3 d0 d1 d2 d3 74 01 00
Notated:
00              CLA
01              INS (add credential)
00              P1
00              P2
4c              Total length of the Data field
                                                            -- Data --
7b 10 00 01 02 03 04 05 06 07 08 09 0a 0b 0c 0d 0e 0f       Management key
71 03 61 62 63                                              Label: 'abc'
74 01 26                                                    Algorithm: AES-128
75 10 ca fe b0 ba ca fe b0 ba ca fe b0 ba ca fe b0 ba       Encryption key
76 10 13 37 f0 0d 13 37 f0 0d 13 37 f0 0d 13 37 f0 0d       MAC key
73 10 a0 a1 a2 a3 b0 b1 b2 b3 c0 c1 c2 c3 d0 d1 d2 d3       Password
74 01 00                                                    Touch required: false
```

## Response APDU

The data field is always empty. On success, the status word will be 0x90 0x00. If there was a failure, further information may be communicated in the status word.

Total Length: 2\

| Data | SW1 | SW2 |
| :---: | :---: | :---: |
| (no data) | 90 | 00 |

### Common non-success status words

| Value | Meaning |
| :---: | :--- |
| 0x6983 | A credential with that label already exists |
| 0x6a84 | No space (30 credentials maximum) |
| 0x63c# | Wrong management key, where # is the number of attempts remaining |
| 0x6a80 | Wrong syntax |