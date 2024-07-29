<!-- Copyright 2021 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

## Authenticate: decrypt

### Command APDU Info

| CLA | INS |     P1      |      P2       |     Lc     |           Data            |    Le    |
|:---:|:---:|:-----------:|:-------------:|:----------:|:-------------------------:|:--------:| 
| 00  | 87  | *algorithm* | *slot number* | *data len* | *encoded data to decrypt* | (absent) |

The *algorithm* is either `06` (RSA-1048) or `07` (RSA-2048). Note that it is not possible
to decrypt using ECC.

The *slot number* can be the number of any slot that holds a private key, other than `F9`.
That is, the slot number can be any PIV slot other than `80`, `81`, `9B`, or `F9`. The
attestation key, `F9`, will sign a certificate it creates, but cannot decrypt or perform
key agreement.

The *encoded data* is

```C
  7C len1 82 00 81 len2 <data to decrypt>

  where len1 and len2 are lengths in DER format, and <data to decrypt> is the same
  size as the key.
```

Notice that in the encoded data, the tags for RSA, `7C`, `82`, `81` are the same as the
tags when signing. The RSA signing and decrypting operations are mathematically identical.

### Response APDU Info

#### Response APDU for AUTHENTICATE:DECRYPT (success)

Total Length: *variable + 2*\
Data Length: *variable*

|                  Data                  | SW1 | SW2 |
|:--------------------------------------:|:---:|:---:|
| 7C *len1* 82 *len2 \<decrypted data\>* | 90  | 00  |

The decrypted data will not be decoded. That is, it will still be in the form of
PKCS 1 v1.5 (`00 02 pad 00 plaintext`) or OAEP.

Note that the response might be returned over multiple commands. Each return command
will be able to return up to 256 bytes. To get more bytes of a return, call the GET
RESPONSE APDU.

#### Response APDU for AUTHENTICATE:DECRYPT (wrong or no PIN, or no touch)

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 | 
|:---------:|:---:|:---:|
| (no data) | 69  | 82  |

If the key was generated or imported with a PIN policy other than "Never", and the command
was sent without first verifying the PIN or the wrong PIN was entered, then this response
will be returned. In addition, if the key's touch policy is not "Never", and after
submitting the command the YubiKey was not touched within the time limit, this response
will be returned.

### Examples

```C
opensc-tool -c default  -s 00:a4:04:00:09:a0:00:00:03:08:00:00:10:00
  -s 00:20:00:80:08:31:32:33:34:35:36:ff:ff
  -s 00:87:06:9d:88:7c:81:85:82:00:81:81:80:
      06:84:ef:45:b9:0c:4e:2b:0e:cd:c1:83:23:21:1b:bc:
      d7:b0:3a:d7:6e:39:cd:48:2e:3d:8c:cc:50:ea:e2:3b:
      70:a1:81:3c:e6:f8:06:88:72:3f:07:ff:18:a3:11:93:
      0a:d1:ae:16:69:2c:ad:73:ba:a7:aa:a2:ce:58:00:32:
      d7:2f:4f:92:48:92:96:54:2c:1d:a8:71:59:38:2b:4e:
      54:95:8a:ca:5c:fd:a7:09:d9:7c:c8:c6:a9:e9:20:ba:
      3d:05:f7:b9:d4:5e:68:5a:19:a5:f5:82:67:fc:b1:5f:
      7f:cf:50:2b:32:cf:ed:b9:4c:ae:a5:8e:e5:f6:3e:33
Using reader with a card: Yubico YubiKey FIDO+CCID 0
Sending: 00 A4 04 00 09 A0 00 00 03 08 00 00 10 00
Received (SW1=0x90, SW2=0x00):
61 11 4F 06 00 00 10 00 01 00 79 07 4F 05 A0 00
00 03 08
Sending: 00 20 00 80 08 31 32 33 34 35 36 FF FF
Received (SW1=0x90, SW2=0x00)
Sending: 00 87 06 9D 88 7C 81 85 82 00 81 81 80 06 84 EF 45 B9 0C 4E 2B 0E CD C1 83 23 21 1B BC D7 B0 3A D7 6E 39 CD 48 2E 3D 8C CC 50 EA E2 3B 70 A1 81 3C E6 F8 06 88 72 3F 07 FF 18 A3 11 93 0A D1 AE 16 69 2C AD 73 BA A7 AA A2 CE 58 00 32 D7 2F 4F 92 48 92 96 54 2C 1D A8 71 59 38 2B 4E 54 95 8A CA 5C FD A7 09 D9 7C C8 C6 A9 E9 20 BA 3D 05 F7 B9 D4 5E 68 5A 19 A5 F5 82 67 FC B1 5F 7F CF 50 2B 32 CF ED B9 4C AE A5 8E E5 F6 3E 33
Received (SW1=0x90, SW2=0x00):
7C 81 83 82 81 80 12 72 21 EB 3C C7 96 28 91 CD
BC 23 C9 74 D4 E0 51 EA 76 59 04 80 78 1A F0 E0
18 F1 4E C2 1E 1E 36 DA FE 88 39 C0 68 3E 46 BD
34 90 F5 29 7E DD E6 C1 C2 A1 EE 0A 37 A8 C5 C6
C2 22 88 86 D4 C7 21 AE 93 7C 57 3A 44 93 78 7D
1F 5D 67 E5 F3 44 42 B6 4E D6 80 5B C9 8F 51 15
1A 9A 74 15 8D B4 5B 5C EC 70 D0 9A 73 C8 0F 7B
F4 62 12 6F FD 1F 71 8A 8A F0 20 A6 44 AF 91 13
A6 0E 0C 1B 44 89
```
