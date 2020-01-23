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


## Authenticate: key agreement

### Command APDU Info

CLA | INS | P1 | P2 | Lc | Data | Le
:---: | :---: | :---: | :---: | :---: | :---:
00 | 87 | *algorithm* | *slot number* | *data len* | *other party's public key* | (absent)

The *algorithm* is either `11` (ECC-P256) or `14` (ECC-P384). Note that is is not possible
to perform key agreement using RSA.

The *slot number* can be the number of any slot that holds a private key, other than `F9`.
That is, the slot number can be any PIV slot other than `80`, `81`, `9B`, or `F9`. The
attestation key, `F9`, will sign a certificate it creates, but cannot decrypt or perform
key agreement.

The *other party's public key* is encoded as follows

```C
  7C len1 82 00 85 len2 <public key>

  where len1 and len2 are lengths in DER format, and <public key> is the other
  party's public key encoded with both x- and y-coordinates:

  04 <x-xoordinate> <y-coordinate>

  Each coordinate is the size as the key, prepended with 00 bytes if necessary.
```

For ECC, the tags for signing (ECDSA) are `7C`, `82`, `81`, but the tags for ECDH are
`7C`, `82`, `85`. That last tag is how the YubiKey will know to perform ECDH as opposed to
ECDSA.

### Response APDU Info

#### Response APDU for AUTHENTICATE:KEY AGREE (success)

Total Length: *variable + 2*\
Data Length: *variable*

Data | SW1 | SW2
:---: | :---: | :---:
7C *len1* 82 *len2 \<shared secret\>* | 90 | 00

The shared secret will be the same size as the key, and will be the raw data, no further
formatting.

Note that the response might be returned over multiple commands. Each return command
will be able to return up to 256 bytes. To get more bytes of a return, call the GET
RESPONSE APDU.

#### Response APDU for AUTHENTICATE:KEY AGREE (wrong or no PIN, or no touch)

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 69 | 82

If the key was generated or imported with a PIN policy other than "Never", and the command
was sent without first verifying the PIN or the wrong PIN was entered, then this response
will be returned. In addition, if the key's touch policy is not "Never", and after
submitting the command the YubiKey was not touched within the time limit, this response
will be returned.

### Examples

```C
$ opensc-tool -c default  -s 00:a4:04:00:09:a0:00:00:03:08:00:00:10:00
  -s 00:20:00:80:08:31:32:33:34:35:36:ff:ff
  -s 00:87:11:90:47:7c:45:82:00:85:41:
     04:65:2D:C5:8C:DC:1F:09:11:50:DB:91:F5:F5:8C:A5:32:
        A5:09:75:E2:34:20:79:09:10:C7:0F:E3:A3:AB:86:DC:
        EA:9C:70:9F:56:06:3B:CD:22:47:F7:D7:D5:7C:92:5C:
        8F:CF:F2:A2:A8:9A:E2:86:00:CA:9A:C1:5E:2A:10:D2
Using reader with a card: Yubico YubiKey FIDO+CCID 0
Sending: 00 A4 04 00 09 A0 00 00 03 08 00 00 10 00
Received (SW1=0x90, SW2=0x00):
61 11 4F 06 00 00 10 00 01 00 79 07 4F 05 A0 00
00 03 08
Sending: 00 20 00 80 08 31 32 33 34 35 36 FF FF
Received (SW1=0x90, SW2=0x00)
Sending: 00 87 11 90 47 7C 45 82 00 85 41
         04 65 2D C5 8C DC 1F 09 11 50 DB 91 F5 F5 8C A5 32
            A5 09 75 E2 34 20 79 09 10 C7 0F E3 A3 AB 86 DC
            EA 9C 70 9F 56 06 3B CD 22 47 F7 D7 D5 7C 92 5C
            8F CF F2 A2 A8 9A E2 86 00 CA 9A C1 5E 2A 10 D2
Received (SW1=0x90, SW2=0x00):
7C 22 82 20 71 64 DC 80 F1 6A EE 96 98 AE 13 CE
84 62 C9 C4 1B 52 BA C3 E7 0C E3 13 79 F5 31 FE
5A 96 1C 1A
