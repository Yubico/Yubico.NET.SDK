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

# OTP commands and APDUs

For each possible OTP command, there will be a class that knows how to build the command
[APDU](xref:UsersManualApdu) and parse the data in the response APDU. Each class will know what information
is needed from the caller for that command.

Because the OTP application originated as a HID transport protocol, the mappings between
"commands" and APDUs is not 1:1. In fact, almost all OTP commands are routed through a single
APDU and dispatched based off of the first parameter in the payload.

## Status structure

The only way to validate that the state of the OTP application has been changed as intended is by examining
the status structure before and after the command. If the configuration has been successfully applied, the
sequence number will have increased.

Note that this is an imperfect detection mechanism as there is the possibility for a race condition between
the initial read of the status structure and the issuance of the command.

The response data is in the following form:

| Size (Bytes) |     Name      | Description                                                           |
| :----------: | :-----------: | :-------------------------------------------------------------------- |
|      1       | Major Version | Typically denotes the line of YubiKey (3 for NEO, 4, 5, etc.)         |
|      1       | Minor Version | Can represent substantial revisions within a YubiKey line.            |
|      1       | Patch Version | The minor and/or bug-fix revision of the firmware.                    |
|      1       |  Sequence #   | Configuration sequence number. `0` if no valid configuration present. |
|      2       |  Touch Level  | The touch level currently detected by the key's button.               |

# Commands

## Configure slot

Commits a configuration to one of two programmable slots. Slot 1 corresponds to the "short press"
of the YubiKey button, and Slot 2 the "long press".

### Available

Short press (slot 1): YubiKey firmware 1.x and later\
Long press (slot 2): YubiKey firmware 2.0 and later

Note: Access over USB (CCID) disabled after YubiKey firmware 5.x

### Command APDU info

|  CLA  |  INS  |     P1      |  P2   |  Lc   |    Data     |
| :---: | :---: | :---------: | :---: | :---: | :---------: |
| 0x00  | 0x01  | (See below) | 0x00  |  52   | (see below) |

#### P1: Slot

P1 determines which slot to program. It can have one of the following values:

| Option               | Value |
| :------------------- | :---: |
| Short press (Slot 1) | 0x01  |
| Long press (Slot 2)  | 0x03  |

#### Data: Configuration structure

The data field contains a standard configuration structure.

| Field       | Size  | Description                        |
| :---------- | :---: | :--------------------------------- |
| Fixed Data  |  16   | Fixed data in binary form.         |
| UID         |   6   | Fixed UID part of the ticket.      |
| AES Key     |  16   | AES key                            |
| Access Code |   6   | Access code to re-program the slot |
| EXT Flags   |   1   | Extended flags                     |
| TKT Flags   |   1   | Ticket configuration flags         |
| CFG Flags   |   1   | General configuration flags        |
| Reserved    |   2   | Must be zero                       |
| CRC         |   2   | CRC16 value of all the fields      |

<!-- TODO -->
TBD page will go into more detail on how to construct a valid configuration.

### Response APDU info

|  Lr   |                 Data                  |  SW1  |  SW1  |
| :---: | :-----------------------------------: | :---: | :---: |
| 0x06  | [Status structure](#status-structure) | 0x90  | 0x00  |

### Examples

```shell
$ opensc-tool.exe -c default -r 1 -s 00:a4:04:00:08:a0:00:00:05:27:20:01:01 -s 00:01:01:00:34:de:ad:be:ef:
de:ad:be:ef:de:ad:be:ef:de:ad:be:ef:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:
00:00:00:00:00:10:20:20:a0:00:00:2a:ac
Sending: 00 A4 04 00 08 A0 00 00 05 27 20 01 01
Received (SW1=0x90, SW2=0x00):
05 03 01 04 05 00 ......
Sending: 00 01 01 00 34 DE AD BE EF DE AD BE EF DE AD BE EF DE AD BE EF 00 00 00 00 00 00 00 00 00 00 00 00
00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 10 20 20 A0 00 00 2A AC
Received (SW1=0x90, SW2=0x00):
05 03 01 05 05 00 ......
```

## Update slot

Updates the flags for a given configuration slot if the slot configuration allows for it. The slot
must either have the "Allow Update" flag set, or be marked as "Dormant".

### Available

YubiKey firmware 2.3 and later

### Command APDU info

|  CLA  |  INS  |     P1      |  P2   |  Lc   |    Data     |
| :---: | :---: | :---------: | :---: | :---: | :---------: |
| 0x00  | 0x01  | (See below) | 0x00  |  52   | (see below) |

#### P1: Slot

P1 determines which slot to program. It can have one of the following values:

| Option               | Value |
| :------------------- | :---: |
| Short press (Slot 1) | 0x04  |
| Long press (Slot 2)  | 0x05  |

#### Data: Configuration structure

The data field contains a standard configuration structure.

| Field       | Size  | Description                   |
| :---------- | :---: | :---------------------------- |
| Fixed Data  |  16   | Do not set this field.        |
| UID         |   6   | Do not set this field.        |
| AES Key     |  16   | Do not set this field.        |
| Access Code |   6   | Do not set this field.        |
| EXT Flags   |   1   | Extended flags                |
| TKT Flags   |   1   | Ticket configuration flags    |
| CFG Flags   |   2   | General configuration flags   |
| Reserved    |   2   | Must be zero                  |
| CRC         |   2   | CRC16 value of all the fields |

The only changes that are allowed are the setting of the following flags:

- Ticket Flags: Tab First, Append Tab 1, Append Tab 2, Append Delay 1, Append Delay 2, Append Carriage Return
- Config Flags: Pacing 10ms, Pacing 20ms
- Extended Flags: Serial Number Visibility (Button, USB, API), Use Numeric Keypad, Fast Trigger, Allow Update, Dormant, Invert LED

### Response APDU info

|  Lr   |                 Data                  |  SW1  |  SW1  |
| :---: | :-----------------------------------: | :---: | :---: |
| 0x06  | [Status structure](#status-structure) | 0x90  | 0x00  |

### Examples

TODO

## Swap slot configurations

Swaps the configurations in the short and long press slots.

### Available

YubiKey firmware 2.3.2 and later

### Command APDU info

|  CLA  |  INS  |  P1   |  P2   |    Lc    |   Data   |
| :---: | :---: | :---: | :---: | :------: | :------: |
| 0x00  | 0x01  | 0x06  | 0x00  | (absent) | (absent) |

### Response APDU info

|  Lr   |                 Data                  |  SW1  |  SW1  |
| :---: | :-----------------------------------: | :---: | :---: |
| 0x06  | [Status structure](#status-structure) | 0x90  | 0x00  |

### Examples

```shell
$ opensc-tool.exe -c default -r 1 -s 00:a4:04:00:08:a0:00:00:05:27:20:01:01 -s 00:01:06:00
Sending: 00 A4 04 00 08 A0 00 00 05 27 20 01 01
Received (SW1=0x90, SW2=0x00):
05 03 01 02 05 00 ......
Sending: 00 01 06 00
Received (SW1=0x90, SW2=0x00):
05 03 01 03 05 00 ......
```

## Program NDEF

Sets the static payload for the NFC data exchange (NDEF) used by NFC enabled keys.

### Available

YubiKey firmware 3.x, and 5.x and later

### Command APDU info

|  CLA  |  INS  |     P1      |  P2   |    Lc    |    Data     |
| :---: | :---: | :---------: | :---: | :------: | :---------: |
| 0x00  | 0x01  | (See below) | 0x00  | (Varies) | (See below) |

#### P1: Slot

P1 determines which slot to program. It can have one of the following values:

| Option                | Value |
| :-------------------- | :---: |
| NDEF Slot 1 (Primary) | 0x08  |
| NDEF Slot 2           | 0x09  |

#### Data: NDEF configuration structure

| Field       | Size  | Description                                                   |
| :---------- | :---: | :------------------------------------------------------------ |
| Length      |   1   | Number of valid bytes in the data field.                      |
| Type        |   1   | Leave this set to `55`                                        |
| Data        |  54   | The NDEF payload. It does not need to fill the entire buffer. |
| Access Code |   6   | The current access code for the slot, if any.                 |

### Response APDU info

|  Lr   |                 Data                  |  SW1  |  SW1  |
| :---: | :-----------------------------------: | :---: | :---: |
| 0x06  | [Status structure](#status-structure) | 0x90  | 0x00  |

### Examples

TODO

## Get serial number

Reads the serial number of the YubiKey if it is allowed by the configuration. Note that certain
keys, such as the Security Key by Yubico, do not have serial numbers.

### Available

YubiKey firmware 1.2 and later.

### Command APDU info

|  CLA  |  INS  |  P1   |  P2   |    Lc    |   Data   |
| :---: | :---: | :---: | :---: | :------: | :------: |
| 0x00  | 0x01  | 0x10  | 0x00  | (absent) | (absent) |

### Response APDU info

|  Lr   |     Data      |  SW1  |  SW1  |
| :---: | :-----------: | :---: | :---: |
| 0x04  | Serial Number | 0x90  | 0x00  |

### Examples

```shell
$ opensc-tool.exe -c default -r 1 -s 00:a4:04:00:08:a0:00:00:05:27:20:01:01 -s 00:01:10:00
Sending: 00 A4 04 00 08 A0 00 00 05 27 20 01 01
Received (SW1=0x90, SW2=0x00):
04 03 07 03 07 00 06 0F 00 00 ..........
Sending: 00 01 10 00
Received (SW1=0x90, SW2=0x00):
00 6B 95 6A .k.j  // Serial Number: 7050602
```

## Update scan-code map

Updates the scan-codes (or keyboard presses) that the YubiKey will use when typing out one-time passwords.

### Available

YubiKey firmware 3.0 and later.

### Command APDU info

|  CLA  |  INS  |  P1   |  P2   |  Lc   |    Data     |
| :---: | :---: | :---: | :---: | :---: | :---------: |
| 0x00  | 0x01  | 0x12  | 0x00  | 0x2D  | (see below) |

The data field is a simple 45-byte array that holds keyboard scan-codes for use during OTP keyboard
operations. The default set of characters is:
> "cbdefghijklnrtuvCBDEFGHIJKLNRTUV0123456789!\t\r"

This is represented by the following array of bytes:

```C
0x06, 0x05, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, // c-i
0x0d, 0x0e, 0x0f, 0x11, 0x15, 0x17, 0x18, 0x19, // j-v
0x86, 0x85, 0x87, 0x88, 0x89, 0x8a, 0x8b, 0x8c, // C-I
0x8d, 0x8e, 0x8f, 0x91, 0x95, 0x97, 0x98, 0x99, // J-V
0x27, 0x1e, 0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, // 0-7
0x25, 0x26,                                     // 8-9
0x9e, 0x2b, 0x28                                // !, \t, \r
```

### Response APDU info

|  Lr   |                 Data                  |  SW1  |  SW1  |
| :---: | :-----------------------------------: | :---: | :---: |
| 0x06  | [Status structure](#status-structure) | 0x90  | 0x00  |

### Examples

Reprogram the YubiKey with the default scan-code map:

```shell
$ opensc-tool.exe -c default -r 1 -s 00:a4:04:00:08:a0:00:00:05:27:20:01:01 -s 00:01:12:00:2D:06:05:07:08:09:0A:0
B:0C:0D:0E:0F:11:15:17:18:19:86:85:87:88:89:8A:8B:8C:8D:8E:8F:91:95:97:98:99:27:1E:1F:20:21:22:23:24:25:26:9E:2B:28
Sending: 00 A4 04 00 08 A0 00 00 05 27 20 01 01
Received (SW1=0x90, SW2=0x00):
05 03 01 03 05 00 ......
Sending: 00 01 12 00 2D 06 05 07 08 09 0A 0B 0C 0D 0E 0F 11 15 17 18 19 86 85 87 88 89 8A 8B 8C 8D 8E 8F 91 95 97 98 99 27 1E 1F 20 21 22 23 24 25 26 9E 2B 28
Received (SW1=0x90, SW2=0x00):
05 03 01 04 05 00 ......
```

## Get device information

Reads configuration and metadata information about the YubiKey. Similar commands exist in other
applications. The Command APDU may be different, however the data in the Response APDU will be
of identical format.

### Available

YubiKey firmware 4.1 and later.

### Command APDU info

|  CLA  |  INS  |  P1   |  P2   |    Lc    |   Data   |
| :---: | :---: | :---: | :---: | :------: | :------: |
| 0x00  | 0x01  | 0x13  | 0x00  | (absent) | (absent) |

### Response APDU info

|    Lr    |    Data     |  SW1  |  SW2  |
| :------: | :---------: | :---: | :---: |
| (Varies) | (See Below) | 0x90  | 0x00  |

The device information is encoded in Tag-Length-Value (TLV) format. The following table describes the
possible entries (tags).

| Name                         | Value | Description                                                                                           |
| :--------------------------- | :---: | :---------------------------------------------------------------------------------------------------- |
| Available capabilities (USB) | 0x01  | USB Applications and capabilities that are available for use on this YubiKey.                         |
| Serial number                | 0x02  | Returns the serial number of the YubiKey (if present and visible).                                    |
| Enabled capabilities (USB)   | 0x03  | Applications that are currently enabled over USB on this YubiKey.                                     |
| Form factor                  | 0x04  | Specifies the form factor of the YubiKey (USB-A, USB-C, Nano, etc.)                                   |
| Firmware version             | 0x05  | The Major.Minor.Patch version number of the firmware running on the YubiKey.                          |
| Auto-eject timeout           | 0x06  | Timeout in (ms?) before the YubiKey automatically "ejects" itself.                                    |
| Challenge-response timeout   | 0x07  | The period of time (in seconds) after which the OTP challenge-response command should timeout.        |
| Device flags                 | 0x08  | Device flags that can control device-global behavior.                                                 |
| Configuration lock           | 0x0A  | Indicates whether or not the YubiKey's configuration has been locked by the user.                     |
| Available capabilities (NFC) | 0x0D  | NFC Applications and capabilities that are available for use on this YubiKey.                         |
| Enabled capabilities (NFC)   | 0x0E  | Applications that are currently enabled over USB on this YubiKey.                                     |

### Examples

YubiKey 4.3.7

```shell
$ opensc-tool.exe -c default -r 1 -s 00:a4:04:00:08:a0:00:00:05:27:20:01:01 -s 00:01:13:00
Sending: 00 A4 04 00 08 A0 00 00 05 27 20 01 01
Received (SW1=0x90, SW2=0x00):
04 03 07 03 07 00 06 0F 00 00 ..........
Sending: 00 01 13 00
Received (SW1=0x90, SW2=0x00):
0C 01 01 FF 02 04 00 6B 95 6A 03 01 3F .......k.j..?

0C                      // Overall bytes
    01 01 FF            // Available capabilities (USB), length = 1, All capabilities available
    02 04 00 6B 95 6A   // Serial number, length = 4, value = 7060602
    03 01 3F            // Enabled capabilities (USB), length = 1, All capabilities enabled
```

YubiKey 5.2.3

```shell
$ opensc-tool.exe -c default -r 0 -s 00:a4:04:00:08:a0:00:00:05:27:20:01:01 -s 00:01:13:00
Sending: 00 A4 04 00 08 A0 00 00 05 27 20 01 01
Received (SW1=0x90, SW2=0x00):
AF 5C 62 50 C3 A8 .\bP..
Sending: 00 01 13 00
Received (SW1=0x90, SW2=0x00):
2E 01 02 02 3F 03 02 00 3C 02 04 00 A8 57 9B 04 ....?...<....W..
01 01 05 03 05 02 03 06 02 00 0A 07 01 0F 08 01 ................
80 0D 02 02 3F 0E 02 02 3B 0A 01 00 0F 01 00    ....?...;......

2B                      // Overall bytes
    01 02 02 3F         // Available capabilities (USB), length = 2, All capabilities
    03 02 00 3C         // Enabled capabilities (USB), length = 2, U2F and CCID unused
    02 04 00 AB 57 9B   // Serial number, length = 4, value = 11229083
    04 01 01            // Form factor, length = 1, 5A Keychain
    05 03 05 02 03      // Firmware version, length = 3, value = 5.2.3
    06 02 00 0A         // Auto eject timeout, length = 2, value = 40,960
    07 01 0F            // Challenge response timeout, length = 1, value = 15
    08 01 80            // Device flags, length = 1, Touch eject enabled
    0D 02 02 3F         // Available capabilities (NFC), length = 2
    0E 02 02 3B         // Enabled capabilities (NFC), length = 2
    0A 01 00            // Configuration log, length = 1, Unlocked
```

## Query FIPs mode

Determines whether or not the device is loaded with FIPS capable firmware, as well as if the key
is currently in a FIPS compliant state.

### Available

YubiKey firmware 4.4.x.

### Command APDU info

|  CLA  |  INS  |  P1   |  P2   |    Lc    |   Data   |
| :---: | :---: | :---: | :---: | :------: | :------: |
| 0x00  | 0x01  | 0x14  | 0x00  | (absent) | (absent) |

### Response APDU info

If a FIPS key:

|  Lr   |                    Data                    |  SW1  |  SW2  |
| :---: | :----------------------------------------: | :---: | :---: |
| 0x01  | 0 = not FIPS compliant, 1 = FIPS compliant | 0x90  | 0x00  |

Just because a key may be branded FIPS or have FIPS capable firmware loaded, does not mean that the
YubiKey is FIPS compliant. Configurations on the key need to be locked or otherwise protected in
order to claim compliant behavior.

If not a FIPS key:

|    Lr    |   Data   |  SW1  |  SW2  |
| :------: | :------: | :---: | :---: |
| (absent) | (absent) | 0x6B  | 0x00  |

0x6B00 is "SW_WRONG_P1P2", which in this context simply means that the query command is not present.
This behavior can be assumed to mean that the key does not support FIPS mode, and that it does not
have FIPS capable firmware.

### Examples

YubiKey 5.2.4 (via NFC)

```shell
$ opensc-tool.exe -c default -r 0 -s 00:a4:04:00:08:a0:00:00:05:27:20:01:01 -s 00:01:14:00
Sending: 00 A4 04 00 08 A0 00 00 05 27 20 01 01
Received (SW1=0x90, SW2=0x00):
A0 A2 7B 13 F8 80 ..{...
Sending: 00 01 14 00
Received (SW1=0x6B, SW2=0x00)
```

YubiKey FIPS 4.4.5

```shell
$ opensc-tool.exe -c default -r 1 -s 00:a4:04:00:08:a0:00:00:05:27:20:01:01 -s 00:01:14:00
Sending: 00 A4 04 00 08 A0 00 00 05 27 20 01 01
Received (SW1=0x90, SW2=0x00):
04 04 05 01 05 00 06 0F 00 00 ..........
Sending: 00 01 14 00
Received (SW1=0x90, SW2=0x00):
00 .
```

## Challenge/Response

Perform a challenge response style operation using either YubicoOTP or HMAC-SHA1 against a configured
YubiKey slot.

### Available

YubiKey firmware 2.2 and later.

### Command APDU info

|  CLA  |  INS  |     P1      |  P2   |    Lc    |      Data      |
| :---: | :---: | :---------: | :---: | :------: | :------------: |
| 0x00  | 0x01  | (See below) | 0x00  | (varies) | Challenge data |

#### P1: Slot

P1 indicates both the type of challenge / response algorithm and the slot in which to use.

| Option            | Value |
| :---------------- | :---: |
| YubicoOTP (Short) | 0x20  |
| YubicoOTP (Long)  | 0x28  |
| HMAC-SHA1 (Short) | 0x30  |
| HMAC-SHA1 (Long)  | 0x38  |

#### Data: Challenge

A string of bytes no greater than 64-bytes in length.

### Response APDU info

|      Lr      |     Data      |  SW1  |  SW1  |
| :----------: | :-----------: | :---: | :---: |
| 0x10 or 0x14 | Response data | 0x90  | 0x00  |

If the YubicoOTP algorithm was used, a 16-byte response will be given. HMAC-SHA1 will return a 20-byte
response.

### Examples

TODO

## Read status

Read the YubiKey's OTP status structure.

### Available

YubiKey firmware 3.x and later

### Command APDU info

|  CLA  |  INS  |  P1   |  P2   |    Lc    |   Data   |
| :---: | :---: | :---: | :---: | :------: | :------: |
| 0x00  | 0x03  | 0x00  | 0x00  | (absent) | (absent) |

### Response APDU info

|  Lr   |                 Data                  |  SW1  |  SW1  |
| :---: | :-----------------------------------: | :---: | :---: |
| 0x06  | [Status structure](#status-structure) | 0x90  | 0x00  |

### Examples

```shell
$ opensc-tool.exe -c default -r 0 -s 00:a4:04:00:08:a0:00:00:05:27:20:01:01 -s 00:03:00:00
Sending: 00 A4 04 00 08 A0 00 00 05 27 20 01 01
Received (SW1=0x90, SW2=0x00):
04 03 07 03 07 00 06 0F 00 00 ..........
Sending: 00 03 00 00
Received (SW1=0x90, SW2=0x00):
04 03 07 03 07 00 ......
```

## Read NDEF payload (NFC only)

Accessing NDEF requires selecting the NDEF application over NFC. Though conceptually still part of the OTP
application, NDEF has its own application ID and is processed separately.

NDEF AID: `0xD2, 0x76, 0x00, 0x00, 0x85, 0x01, 0x01`

### Available

YubiKey firmware 3.x and 5.x

### Command APDU info

| Command Sequence |  CLA  |  INS  |  P1   |  P2   |    Lc    |   Data    |    Le    |
| :--------------- | :---: | :---: | :---: | :---: | :------: | :-------: | :------: |
| 1 (Select File)  | 0x00  | 0xA4  | 0x00  | 0x0C  |   0x02   | 0xE1 0x04 | (absent) |
| 2 (Read Data)    | 0x00  | 0xB0  | 0x00  | 0x00  | (absent) | (absent)  |   0x00   |

### Response APDU info

Only the "Read Data" APDU returns data:

|    Lr    |    Data     |  SW1  |  SW2  |
| :------: | :---------: | :---: | :---: |
| (varies) | (see below) | 0x90  | 0x00  |

The response data is an NFC Data Exchange Format (NDEF) record as defined by the NFC Forum's technical
specification of the same name. It is as follows:

| Field                 |   Size   | Description                                                                             |
| :-------------------- | :------: | :-------------------------------------------------------------------------------------- |
| Tag                   |    1     | Always `0`                                                                              |
| Length                |    1     | Length of the NDEF record                                                               |
| NDEF Header           |    1     | Always `0xD1`: Message Begin+End, Short Record, Type Name Format = NFC Forum well known |
| Length of record type |    1     | Always `1`                                                                              |
| Payload Length        |    1     |                                                                                         |
| Type                  |    1     | NFC Forum global type "U"                                                               |
| Payload               | (varies) | The actual NDEF message. Of length "payload length" bytes.                              |

### Examples

```shell
$ opensc-tool.exe -c default -r 0 -s 00:A4:04:00:07:D2:76:00:00:85:01:01 -s 00:A4:00:0C:02:E1:04 -s 00:B0:00:00:00
Sending: 00 A4 04 00 07 D2 76 00 00 85 01 01
Received (SW1=0x90, SW2=0x00)
Sending: 00 A4 00 0C 02 E1 04
Received (SW1=0x90, SW2=0x00)
Sending: 00 B0 00 00 00
Received (SW1=0x90, SW2=0x00):
00 43 D1 01 3F 55 04 6D 79 2E 79 75 62 69 63 6F .C..?U.my.yubico
2E 63 6F 6D 2F 79 6B 2F 23 63 63 63 63 63 63 6E .com/yk/#ccccccn
65 75 68 74 66 64 6E 76 6C 75 6E 68 6C 67 66 72 euhtfdnvlunhlgfr
6A 62 6E 65 65 62 76 6E 67 64 67 6C 6C 67 76 64 jbneebvngdgllgvd
64 65 72 65 62                                  dereb
```
