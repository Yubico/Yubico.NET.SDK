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


## Authenticate: sign

### Command APDU Info

CLA | INS | P1 | P2 | Lc | Data | Le
:---: | :---: | :---: | :---: | :---: | :---:
00 | 87 | *algorithm* | *slot number* | *data len* | *encoded digest of data to sign* | (absent)

The *algorithm* is either `06` (RSA-1048), `07` (RSA-2048), `11` (ECC-P256), or `14`
(ECC-P384).

The *slot number* can be the number of any slot that holds a private key, other than `F9`.
That is, the slot number can be any PIV slot other than `80`, `81`, `9B`, or `F9`. The
attestation key, `F9`, will sign a certificate it creates, so it can sign. It simply
cannot sign arbitrary data, only attestation statements.

The *encoded digest* is

```C
  7C len1 82 00 81 len2 <digest block>

  where len1 and len2 are lengths in DER format, and  <digest block> is the digest of
  the data to sign.
```

With RSA, the digest is encoded using either PKCS 1 v 1.5 padding, or PKCS 1 PSS.

For example, if using PKCS 1 v 1.5 padding, the encoded digest is built as follows.

```C
  formatted digest = 00 01 FF FF ... FF 00 <DER of DigestInfo>

  With a 2048-bit RSA key, the data to pass to the command is

  7c 82 01 06 82 00 81 82 01 00 <formatted digest>
     ^      ^          ^      ^  |<-- 256 bytes -->|
     |      |          |      |
     |      |          ---------- len2
     ---------- len1
```

If the data for the APDU is too long for one call (256 bytes), then there will be two
calls (a chain).

For ECC, there is one format:

```C
  7C len1 82 00 81 len2 <digest>

  where len1 and len2 are lengths in DER format, and  <digest> is the digest of the
  data to sign.

  If the key is EccP256, the digest must be 256 bits (32 bytes) or shorter. You will
  generally use SHA-256.

  7C 24 82 00 81 20 <32-byte digest>

  If the key is EccP384, the digest must be 384 bits (48 bytes) or shorter. You will
  generally use SHA-384.

  7C 34 82 00 81 30 <48-byte digest>
```

### Response APDU Info

#### Response APDU for AUTHENTICATE:SIGN (success)

Total Length: *variable + 2*\
Data Length: *variable*

Data | SW1 | SW2
:---: | :---: | :---:
7C *len1* 82 *len2 \<signature\>* | 90 | 00

Note that the signature might be returned over multiple commands. Each return command
will be able to return up to 256 bytes. To get more bytes of a return, call the GET
RESPONSE APDU.

The signature is returned encoded as follows,

```
  7C len1 82 len2 <signature>

  For example, with RSA-2048, the signature will be

  7C 82 01 04 82 828 01 00 <256-byte signature>

  With ECC-P256, the signature will be

  7C 48 82 46 <70-byte signature>

  An ECC signature is ECDSA, which is the DER encoding of

  SEQUENCE {
    r   INTEGER,
    s   INTEGER
  }

  Both r and s are the same size as the key, so will be 32 bytes long. It is possible that
  the encoding will be up to 33 bytes, and it can be shorter. For example,

  30 44 02 20 <32-byte r> 02 20 <32-byte s>
```

#### Response APDU for AUTHENTICATE:SIGN (wrong or no PIN, or no touch)

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 69 | 82

If the key was generated or imported with a PIN policy other than "Never", and the command
was sent without first verifying the PIN or the wrong PIN was entered, then the following
response will be returned. In addition, if the key's touch policy is not "Never", and
after sumbitting the command the YubiKey was not touched within the time limit, this
response will be returned.

### Examples
```C
$ opensc-tool -c default -s 00:a4:04:00:09:a0:00:00:03:08:00:00:10:00
  -s 00:20:00:80:08:31:32:33:34:35:36:ff:ff
  -s 10:87:07:9c:d9:7c:82:01:06:82:00:81:82:01:00:
       00:01:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:
       ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:
       ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:
       ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:
       ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:
       ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:
       ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:
       ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:
       ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:
       ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:
       ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:
       ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:
       ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:ff:00
  -s 00:87:07:9c:31:
       30:2f:30:0b:06:09:60:86:48:01:65:03:04:02:01:04:
       20:00:01:02:03:04:05:06:07:08:09:0a:0b:0c:0d:0e:
       0f:10:11:12:13:14:15:16:17:18:19:1a:1b:1c:1d:1e:
       1f
  -s 00:c0:00:00
  -s 00:c0:00:00
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 09 A0 00 00 03 08 00 00 10 00
Received (SW1=0x90, SW2=0x00):
61 11 4F 06 00 00 10 00 01 00 79 07 4F 05 A0 00 a.O.......y.O...
00 03 08                                        ...
Sending: 00 20 00 80 08 31 32 33 34 35 36 FF FF
Received (SW1=0x90, SW2=0x00)
Sending: 10 87 07 9C D9 7C 82 01 06 82 00 81 82 01 00 00 01 FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF 00
Received (SW1=0x90, SW2=0x00)
Sending: 00 87 07 9C 31 30 2F 30 0B 06 09 60 86 48 01 65 03 04 02 01 04 20 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 10 11 12 13 14 15 16 17 18 19 1A 1B 1C 1D 1E 1F
Received (SW1=0x90, SW2=0x00):
7C 82 01 04 82 82 01 00 AF 71 DA 5B 16 AA 7D 15
50 8A 6A 57 3C 78 86 BB F7 53 29 E0 C4 9C F8 C8
D5 37 D4 D4 E5 3F 9D DE 11 17 B4 11 EE 45 D4 1E
B9 75 92 55 34 E6 2B 1F 8A 49 20 48 AD E4 D0 F4
2C DC F5 80 B7 25 49 83 B3 43 14 0F 31 E7 E1 F0
B4 F8 75 C1 B7 9E F9 6A 2D BC 3A F8 2F 84 4D FC
42 27 21 F1 23 13 50 EA 96 05 47 7C BF 0C 97 46
6B 1D A6 5F 80 B9 7B 89 8A F4 8C C3 4B 9F AB 91
29 BB C3 70 7A 9C 99 E6 48 33 90 B5 49 97 AD D0
6B 0B 36 10 A9 B2 FC CA D7 8C EC 30 6D 50 CB BE
57 D7 63 3E C1 A9 80 7F E6 37 FA 51 D8 8C 0B 22
70 95 1B 7A EA 5C E3 43 D7 09 77 54 C4 39 40 F5
B1 A5 BE D7 0C 96 FC 74 41 93 4A 27 C7 07 CE 2F
3A FD C1 FB F8 D3 06 B9 02 D0 16 C7 21 46 38 74
2F 50 1E CF 95 A9 B6 39 74 AC 15 7B E8 23 81 53
F0 AC 15 9F 12 DE 6C DB C5 F2 F0 01 7E 42 31 0E
Sending: 00 C0 00 00
Received (SW1=0x90, SW2=0x00):
67 13 97 38 84 CD A5 D1
Sending: 00 C0 00 00
Received (SW1=0x6A, SW2=0x80)
```

