---
uid: OathCommands
---

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

# OATH commands and APDUs

For each possible OATH command, there will be a class that knows how to build the
command [APDU](xref:UsersManualApdu) and parse the data in the response APDU. Each class will know
what information is needed from the caller for that command.

## General Definitions

The OATH application is used to manage and use OATH credentials with YubiKey NEO, YubiKey 4, or YubiKey 5.

It can be accessed over USB (when the CCID transport is enabled) or over NFC, using ISO 7816-4 commands.

### Commands

Commands marked as Require Auth require a successful VALIDATE command to be performed before they are available, if a
validation code is set.

| Name           | Code | Require Auth |
|:---------------|:----:|:------------:|
| PUT            | 0x01 |      Y       |
| DELETE         | 0x02 |      Y       |
| SET CODE       | 0x03 |      Y       |
| RESET          | 0x04 |      N       |
| LIST           | 0xa1 |      Y       |
| CALCULATE      | 0xa2 |      Y       |
| VALIDATE       | 0xa3 |      N       |
| CALCULATE ALL  | 0xa4 |      Y       |
| SEND REMAINING | 0xa5 |      Y       |

### Algorithms

| Name        | Code |
|:------------|:----:|
| HMAC-SHA1   | 0x01 |
| HMAC-SHA256 | 0x02 |
| HMAC-SHA512 | 0x03 |

Note: HMAC-SHA512 requires YubiKey 4.3.1 or later.

### Types

| Name | Code |
|:-----|:----:|
| HOTP | 0x01 |
| TOTP | 0x02 |

### Properties

| Name            | Code | Description                                                  |
|:----------------|:----:|:-------------------------------------------------------------|
| Only increasing | 0x01 | Enforces that a challenge is always higher than the previous |
| Require touch   | 0x02 | Require button press to generate OATH codes                  

Note: Require touch requires YubiKey 4.2.4 or later.

## List credentials

Lists configured credentials.

### Command APDU info

 CLA  | INS  |  P1  |  P2  |    Lc    |   Data   |
:----:|:----:|:----:|:----:|:--------:|:--------:|
 0x00 | 0xa1 | 0x00 | 0x00 | (absent) | (absent) |

### Response APDU info

Response will be a continual list of objects looking like:

|    Data     |     SW1     |     SW2     |
|:-----------:|:-----------:|:-----------:|
| (See below) | (See below) | (See below) |

#### Data

| Name          | Code                                         |
|:--------------|:---------------------------------------------|
| Name list tag | 0x72                                         |
| Name length   | Length of name + 1                           |
| Algorithm     | High 4 bits is type, low 4 bits is algorithm |
| Name data     | Name                                         |

#### Response Status

| Name                | SW1  | SW2  |
|:--------------------|:----:|:----:|
| Success             | 0x90 | 0x00 |
| More data available | 0x61 | 0xxx |
| Auth required       | 0x69 | 0x82 |
| Generic error       | 0x65 | 0x81 |

### Examples

```shell
opensc-tool -c default -s 00:a4:04:00:07:a0:00:00:05:27:21:01 -s 00:a1:00:00
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 07 A0 00 00 05 27 21 01
Received (SW1=0x90, SW2=0x00):
79 03 05 02 04 71 08 4B B7 A7 FA D7 AF 40 1B y....q.K.....@.
Sending: 00 A1 00 00
Received (SW1=0x90, SW2=0x00):
72 1B 21 4D 69 63 72 6F 73 6F 66 74 3A 74 65 73 r.!Microsoft:tes
74 40 6F 75 74 6C 6F 6F 6B 2E 63 6F 6D 72 16 12 t@outlook.comr..
47 6F 6F 67 6C 65 3A 74 65 73 74 40 67 6D 61 69 Google:test@gmai
6C 2E 63 6F 6D                                  l.com
```

## Put credential

Adds a new (or overwrites) OATH credential.

### Command APDU info

 CLA  | INS  |  P1  |  P2  |       Lc       |    Data     |
:----:|:----:|:----:|:----:|:--------------:|:-----------:|
 0x00 | 0x01 | 0x00 | 0x00 | Length of Data | (See below) |

#### Data

| Name            | Code                                         |
|:----------------|:---------------------------------------------|
| Name tag        | 0x71                                         |
| Name length     | Length of name data, max 64 bytes            |
| Name data       | Name                                         |
| Key tag         | 0x73                                         |
| Key length      | Length of key data + 2                       |
| Key algorithm   | High 4 bits is type, low 4 bits is algorithm |
| Digits          | Number of digits in OATH code                |
| Key data        | Key (Secret)                                 |
| Property tag(o) | 0x78                                         |
| Property(o)     | Property byte                                |
| IMF tag(o)      | 0x7a (only valid for HOTP)                   |
| IMF length(o)   | Length of imf data, always 4 bytes           |
| IMF data(o)     | Imf                                          |

Notes:

- Name data is typically presented as "period/issuer:account", but the "period/" and "issuer:" are optional under
  certain configurations.
- Minimal length of the Key data (secret) is 14 bytes, if the length is less then pad with 0s.
- Key (secret) is arbitary key value encoded in Base32 according to RFC 3548.
- Imf data is a Counter, which counts the number of iterations for HOTP.

### Response APDU info

|   Data   |     SW1     |     SW2     |
|:--------:|:-----------:|:-----------:|
| (absent) | (See below) | (See below) |

#### Response Status

| Name          | SW1  | SW2  |
|:--------------|:----:|:----:|
| Success       | 0x90 | 0x00 |
| No space      | 0x6a | 0x84 |
| Auth required | 0x69 | 0x82 |
| Wrong syntax  | 0x6a | 0x80 |

### Examples

```shell
opensc-tool -c default -s 00:a4:04:00:07:a0:00:00:05:27:21;01 -s 00:01:00:00:30:71:1A:4D:69:63:72:6F:73:6F:66:74:3A:74:65:73:74:40:6F:75:74:6C:6F:6F:6B:2E:63:6F:6D:73:10:21:06:9C:00:00:00:00:00:00:00:00:00:00:00:00:00:
78:02
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 07 A0 00 00 05 27 21 01
Received (SW1=0x90, SW2=0x00):
79 03 05 02 04 71 08 4B B7 A7 FA D7 AF 40 1B y....q.K.....@.
Sending:
00 01 00 00                            // instruction = 01
30                                     // overoll data length
71                                     // Name tag
1A                                     // Name length
4D 69 63 72 6F 73 6F 66 74 3A 74 65 73 // Name data presented as
74 40 6F 75 74 6C 6F 6F 6B 2E 63 6F 6D // issuer:account
73                                     // Key (secret) tag
10                                     // Key length + 2
21                                     // totp (type) + sha1 (algorithm)
06                                     // number of dighits
9C 00 00 00 00 00 00 00 00 00 00 00 00 00 // Key (secret) data
78                                     // Property tag
02                                     // Require touch property
Received (SW1=0x90, SW2=0x00)
```

## Rename credential

Renames an existing OATH credential on the YubiKey.

### Command APDU info

 CLA  | INS  |  P1  |  P2  |       Lc       |    Data     |
:----:|:----:|:----:|:----:|:--------------:|:-----------:|
 0x00 | 0x05 | 0x00 | 0x00 | Length of Data | (See below) |

#### Data

| Name        | Code                              |
|:------------|:----------------------------------|
| Name tag    | 0x71                              |
| Name length | Length of name data, max 64 bytes |
| Name data   | The current credential's name     |
| Name tag    | 0x71                              |
| Name length | Length of name data, max 64 bytes |
| Name data   | The new credential's name         |

Notes:

- This command is only available on YubiKeys with firmware version 5.3.0 and later.
- Name data is presented as `period/issuer:account`, but the "period/" and "issuer:" are optional under certain
  configurations.
- The new issuer can be an empty string.

### Response APDU info

|   Data   |     SW1     |     SW2     |
|:--------:|:-----------:|:-----------:|
| (absent) | (See below) | (See below) |

#### Response Status

| Name           | SW1  | SW2  |
|:---------------|:----:|:----:|
| Success        | 0x90 | 0x00 |
| No such object | 0x69 | 0x84 |
| Auth required  | 0x69 | 0x82 |
| Wrong syntax   | 0x6a | 0x80 |

### Examples

```shell
opensc-tool -c default -s 00:a4:04:00:07:a0:00:00:05:27:21;01 -s 00:01:00:00:30:71:1A:4D:69:63:72:6F:73:6F:66:74:3A:74:65:73:74:40:6F:75:74:6C:6F:6F:6B:2E:63:6F:6D:73:10:21:06:9C:00:00:00:00:00:00:00:00:00:00:00:00:00:
78:02
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 07 A0 00 00 05 27 21 01
Received (SW1=0x90, SW2=0x00):
79 03 05 02 04 71 08 4B B7 A7 FA D7 AF 40 1B y....q.K.....@.
Sending:
00 05 00 00                            // instruction = 01
30                                     // overoll data length
71                                     // Name tag
1A                                     // The current name length
4D 69 63 72 6F 73 6F 66 74 3A 74 65 73 // The current name data 74 74 40 6F 75 74 6C 6F 6F 6B 2E 63 6F 6D // presented as issuer:account
71                                     // Name tag
14                                     // The new name length
6F 66 74 3A 74 65 73 74 40 6F 75 74    // The new name data
6B 2E 63 6F 6D 6C 6F 6F                // presented as issuer:account
Received (SW1=0x90, SW2=0x00)
```

## Delete credential

Deletes an existing credential.

### Command APDU info

 CLA  | INS  |  P1  |  P2  |        Lc        |    Data     |
:----:|:----:|:----:|:----:|:----------------:|:-----------:|
 0x00 | 0x02 | 0x00 | 0x00 | (Length of Data) | (See below) |

#### Data

| Name        | Code           |
|:------------|:---------------|
| Name tag    | 0x71           |
| Name length | Length of name |
| Name data   | Name           |

### Response APDU info

|   Data   |     SW1     |     SW2     |
|:--------:|:-----------:|:-----------:|
| (absent) | (See below) | (See below) |

#### Response Status

| Name           | SW1  | SW2  |
|:---------------|:----:|:----:|
| Success        | 0x90 | 0x00 |
| No such object | 0x69 | 0x84 |
| Auth required  | 0x69 | 0x82 |
| Wrong syntax   | 0x6a | 0x80 |

### Examples

```shell
opensc-tool -c default -s 00:a4:04:00:07:a0:00:00:05:27:21:01 -s 00:02:00:00:1C:71:1A:4D:69:63:72:6F:73:6F:66:74:3A:74:65:73:74:40:6F:75:74:6C:6F:6F:6B:2E:63:6F:6D
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 07 A0 00 00 05 27 21 01
Received (SW1=0x90, SW2=0x00):
79 03 05 02 04 71 08 4B B7 A7 FA D7 AF 40 1B y....q.K.....@.
Sending: 00 02 00 00 1C 71 1A 4D 69 63 72 6F 73 6F 66 74 3A 74 65 73 74 40 6F 75 74 6C 6F 6F 6B 2E 63 6F 6D
Received (SW1=0x90, SW2=0x00)
```

## Reset

Resets the application to just-installed state.

### Command APDU info

| CLA  | INS  |  P1  |  P2  |    Lc    |   Data   |
|:----:|:----:|:----:|:----:|:--------:|:--------:|
| 0x00 | 0x04 | 0xde | 0xad | (absent) | (absent) |

### Response APDU info

|   Data   | SW1  | SW2  |
|:--------:|:----:|:----:|
| (absent) | 0x90 | 0x00 |

### Examples

```shell
opensc-tool -c default -s 00:a4:04:00:07:a0:00:00:05:27:21:01 -s 00:04:DE:AD
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 07 A0 00 00 05 27 21 01
Received (SW1=0x90, SW2=0x00):
79 03 05 02 04 71 08 4B B7 A7 FA D7 AF 40 1B y....q.K.....@.
Sending: 00 04 DE AD
Received (SW1=0x90, SW2=0x00)
```

## Set Password

Configures Authentication. If length 0 is sent, authentication is removed. The key to be set is expected to be a
user-supplied UTF-8 encoded password passed through 1000 rounds of PBKDF2 with the ID from select used as salt. 16 bytes
of that are used. When configuring authentication you are required to send an 8 byte challenge and one
authentication-response with that key, in order to confirm that the application and the host software can calculate the
same response for that key.

### Command APDU info

 CLA  | INS  |  P1  |  P2  |       Lc       |    Data     |
:----:|:----:|:----:|:----:|:--------------:|:-----------:|
 0x00 | 0x03 | 0x00 | 0x00 | Length of Data | (See below) |

#### Data

| Name             | Code                     |
|:-----------------|:-------------------------|
| Key tag          | 0x73                     |
| Key length       | Length of key data + 2   |
| Key algorithm    | Algorithm                |
| Key data         | Key (Secret)             |
| Challenge tag    | 0x74                     |
| Challenge length | Length of challenge data |
| Challenge data   | Challenge                |
| Response  tag    | 0x75                     |
| Response  length | Length of response data  |
| Response  data   | Response                 |

### Response APDU info

|   Data   |     SW1     |     SW2     |
|:--------:|:-----------:|:-----------:|
| (absent) | (See below) | (See below) |

#### Response Status

| Name                   | SW1  | SW2  |
|:-----------------------|:----:|:----:|
| Success                | 0x90 | 0x00 |
| Response doesn’t match | 0x69 | 0x84 |
| Auth required          | 0x69 | 0x82 |
| Wrong syntax           | 0x6a | 0x80 |

### Examples

```shell
opensc-tool -c default -s 00:a4:04:00:07:a0:00:00:05:27:21:01 -s 00:03:00:00:33:73:11:01:78:0E:45:A0:06:52:CC:B0:8C:4B:DA:CD:DA:CA:51:34:74:08:F1:03:DA:89:58:E4:40:85:75:14:01:1E:E1:FF:2A:98:2D:4D:CC:CD:8E:B3:3A:12:E4:88:7E:F5:E0:0C
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 07 A0 00 00 05 27 21 01
Received (SW1=0x90, SW2=0x00):
79 03 05 02 04 71 08 91 B8 DA 1C 23 45 F2 6B y....q.....#E.k
Sending: 00 03 00 00 33 73 11 01 78 0E 45 A0 06 52 CC B0 8C 4B DA CD DA CA 51 34 74 08 F1 03 DA 89 58 E4 40 85 75 14 01 1E E1 FF 2A 98 2D 4D CC CD 8E B3 3A 12 E4 88 7E F5 E0 0C
Received (SW1=0x90, SW2=0x00)
```

## Validate

Validates authentication (mutually). The challenge for this comes from the SELECT command. The response if computed by
performing the correct HMAC function of that challenge with the correct key. A new challenge is then sent to the
application, together with the response. The application will then respond with a similar calculation that the host
software can verify.

### Command APDU info

 CLA  | INS  |  P1  |  P2  |       Lc       |    Data     |
:----:|:----:|:----:|:----:|:--------------:|:-----------:|
 0x00 | 0xa3 | 0x00 | 0x00 | Length of Data | (See below) |

#### Data

| Name             | Code                     |
|:-----------------|:-------------------------|
| Response  tag    | 0x75                     |
| Response  length | Length of response data  |
| Response  data   | Response                 |
| Challenge  tag   | 0x74                     |
| Challenge length | Length of challenge data |
| Challenge data   | Challenge                |

### Response APDU info

Response will be a continual list of objects looking like:

|    Data     |     SW1     |     SW2     |
|:-----------:|:-----------:|:-----------:|
| (See below) | (See below) | (See below) |

#### Data

| Name             | Code                    |
|:-----------------|:------------------------|
| Response  tag    | 0x75                    |
| Response  length | Length of response data |
| Response  data   | Response                |

#### Response Status

| Name             | SW1  | SW2  |
|:-----------------|:----:|:----:|
| Success          | 0x90 | 0x00 |
| Auth not enabled | 0x69 | 0x84 |
| Wrong syntax     | 0x6a | 0x80 |
| Generic error    | 0x65 | 0x81 |

## Calculate

Performs CALCULATE for one named credential. Gets an OTP (one-time password) value generated on a YubiKey.

### Command APDU info

 CLA  | INS  |  P1  |     P2      |       Lc       |    Data     |
:----:|:----:|:----:|:-----------:|:--------------:|:-----------:|
 0x00 | 0xa2 | 0x00 | (See below) | Length of Data | (See below) |

#### P2

| Name               | Code |
|:-------------------|:-----|
| Full response      | 0x00 |
| Truncated response | 0x01 |

#### Data

| Name             | Code                     |
|:-----------------|:-------------------------|
| Name  tag        | 0x71                     |
| Name  length     | Length of name data      |
| Name  data       | Name                     |
| Challenge  tag   | 0x74                     |
| Challenge length | Length of challenge data |
| Challenge data   | Challenge                |

### Response APDU info

The first 4 bytes of the response data is an OTP (one-time password) value.

|    Data     |     SW1     |     SW2     |
|:-----------:|:-----------:|:-----------:|
| (See below) | (See below) | (See below) |

#### Data

| Name             | Code                              |
|:-----------------|:----------------------------------|
| Response  tag    | 0x75 - full; 0x76 - truncated     |
| Response  length | Length of response data + 1       |
| Digits           | Number of digits in the OATH code |
| Response  data   | Response                          |

#### Response Status

| Name           | SW1  | SW2  |
|:---------------|:----:|:----:|
| Success        | 0x90 | 0x00 |
| No such object | 0x69 | 0x84 |
| Auth required  | 0x69 | 0x82 |
| Wrong syntax   | 0x6a | 0x80 |
| Generic error  | 0x65 | 0x81 |

```shell
opensc-tool -c default -s 00:A4:04:00:07:A0:00:00:05:27:21:01 -s 00:A2:00:00:26:71:1A:4D:69:63:72:6F:73:6F:66:74:3A:74:65:73:74:40:6F:75:74:6C:6F:6F:6B:2E:63:6F:6D:74:08:F1:03:DA:89:58:E4:40:85
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 07 A0 00 00 05 27 21 01
Received (SW1=0x90, SW2=0x00):
79 03 05 02 04 71 08 91 B8 DA 1C 23 45 F2 6B y....q.....#E.k
Sending: 00 A2 00 00 26 71 1A 4D 69 63 72 6F 73 6F 66 74 3A 74 65 73 74 40 6F 75 74 6C 6F 6F 6B 2E 63 6F 6D 74 08 F1 03 DA 89 58 E4 40 85
Received (SW1=0x90, SW2=0x00):
75 15 06 8A 9B 0D F3 D7 18 43 96 40 A6 58 6F 89 u........C.@.Xo.
D4 03 1D C4 C4 9F 6C                            ......l
```

## Calculate All

Calculates OTPs (one-time passwords) for all available credentials.
Returns name + response for TOTP credentials and just name for HOTP credentials to avoid overloading the HOTP counters.

Note: HOTP credentials, credentials requiring touch and credentials with non default period should be recalculated
separetely if needed by using CALCULATE command.

### Command APDU info

 CLA  | INS  |  P1  |     P2      |       Lc       |    Data     |
:----:|:----:|:----:|:-----------:|:--------------:|:-----------:|
 0x00 | 0xa4 | 0x00 | (See below) | Length of Data | (See below) |

#### P2

| Name               | Code |
|:-------------------|:-----|
| Full response      | 0x00 |
| Truncated response | 0x01 |

#### Data

| Name             | Code                     |
|:-----------------|:-------------------------|
| Challenge  tag   | 0x74                     |
| Challenge length | Length of challenge data |
| Challenge data   | Challenge                |

### Response APDU info

For HOTP the response tag is 0x77 (No response). For credentials requiring touch the response tag is 0x7c (No response).

The first 4 bytes of the response data is an OTP (one-time password) value.

The response will be a list of the following objects:

|    Data     |     SW1     |     SW2     |
|:-----------:|:-----------:|:-----------:|
| (See below) | (See below) | (See below) |

#### Data

| Name             | Code                                                             |
|:-----------------|:-----------------------------------------------------------------|
| Name  tag        | 0x71                                                             |
| Name  length     | Length of name data                                              |
| Name  data       | Name                                                             |
| Response  tag    | 0x77 - for HOTP; 0x7c - for touch; 0x75 - full; 0x76 - truncated |
| Response  length | Length of response data + 1                                      |
| Digits           | Number of digits in the OATH code                                |
| Response  data   | Response                                                         |

#### Response Status

| Name                | SW1  | SW2  |
|:--------------------|:----:|:----:|
| Success             | 0x90 | 0x00 |
| More data available | 0x61 | 0xXX |
| Auth required       | 0x69 | 0x82 |
| Wrong syntax        | 0x6a | 0x80 |
| Generic error       | 0x65 | 0x81 |

```shell
opensc-tool -c default -s 00:A4:04:00:07:A0:00:00:05:27:21:01 -s 00:A4:00:00:0A:74:08:F1:03:DA:89:58:E4:40:85
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 07 A0 00 00 05 27 21 01
Received (SW1=0x90, SW2=0x00):
79 03 05 02 04 71 08 91 B8 DA 1C 23 45 F2 6B y....q.....#E.k
Sending: 00 A4 00 00 0A 74 08 F1 03 DA 89 58 E4 40 85
Received (SW1=0x90, SW2=0x00):
71 1A 4D 69 63 72 6F 73 6F 66 74 3A 74 65 73 74 q.Microsoft:test
40 6F 75 74 6C 6F 6F 6B 2E 63 6F 6D 75 15 06 8A @outlook.comu...
9B 0D F3 D7 18 43 96 40 A6 58 6F 89 D4 03 1D C4 .....C.@.Xo.....
C4 9F 6C 71 15 41 70 70 6C 65 3A 74 65 73 74 40 ..lq.Apple:test@
69 63 6C 6F 75 64 2E 63 6F 6D 77 01 06          icloud.comw..
```
