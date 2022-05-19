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


# U2F commands

For each possible U2F command, there will be a class that knows how to build the
command [APDU](xref:UsersManualApdu) and parse the data in the response APDU. Each class will know
what information is needed from the caller for that command.

## U2F native commands

| Name | Code |
| :-- | :--: |
| REGISTER | 0x02 |
| GET VERSION | 0x03 |

These commands are nested in an APDU which contains the CTAP1 message value in the Ins field:

| Name | Code | Description |
| :--- | :---: | :---: |
| CTAP1 message | 0x03 | Sends a CTAP1/U2F message to the device. |

### Register

Creates a U2F registration on the device.

#### Command APDU info
CLA | INS | P1 | P2 | Lc | Data |
:---: | :---: | :---: | :---: | :---: | :---: |
0x00 | 0x03 | 0x00 | 0x00 | Length of Data | (See below) |

##### Data
Data is presented as an inner command APDU:

CLA | INS | P1 | P2 | Lc | Data
:---: | :---: | :---: | :---: | :---: | :--: |
0x00 | 0x02 | 0x00 | 0x00 | Length of Data | Client Data Hash, App ID Hash |

#### Response APDU info

This command will generally initially return a "Conditions Not Satisfied" (`0x69 85`) status
and will not return success until the user confirms presence by touching the device. Clients
should repeatedly send the command as long as they receive this status.

### Get Version

Returns the U2F protocol version that the application implements.

#### Command APDU info
CLA | INS | P1 | P2 | Lc | Data |
:---: | :---: | :---: | :---: | :---: | :---: |
0x00 | 0x03 | 0x00 | 0x00 | Length of Data | (See below) |

##### Data
Data is presented as an inner command APDU:

CLA | INS | P1 | P2 | Lc | Data |
:---: | :---: | :---: | :---: | :---: | :---: |
0x00 | 0x03 | 0x00 | 0x00 | (absent) | (absent) |

#### Response APDU info

The command should be successful (response status word = `0x90 00`), and the response Data
field will be an ASCII string without a null terminator.

## Extensions and vendor-specific commands

| Name | Code |
| :--- | :---: |
| VERIFY PIN | 0x43 |
| SET PIN | 0x44 |
| RESET | 0x45 |
| VERIFY FIPS MODE | 0x46 |

These commands are nested in an APDU which contains the CTAP1 message value in the Ins field:

| Name | Code | Description |
| :--- | :---: | :---: |
| CTAP1 message | 0x03 | Sends a CTAP1/U2F message to the device. |

### Verify Pin

Verifies a user-supplied pin presented as bytes. PIN length must be from 6 to 32 bytes.

#### Command APDU info

CLA | INS | P1 | P2 | Lc | Data |
:---: | :---: | :---: | :---: | :---: | :---: |
0x00 | 0x03 | 0x00 | 0x00 | Length of Data | (See below) |

##### Data

Data is presented as an inner command APDU:

CLA | INS | P1 | P2 | Lc | Data |
:---: | :---: | :---: | :---: | :---: | :---: |
0x00 | 0x43 | 0x00 | 0x00 | Length of Data | The PIN to verify presented as bytes |

#### Response APDU info
This command returns status "Success" if the PIN is correct.

|  SW1  |  SW2  |
| :---: | :---: |
| 0x90  | 0x00  |

#### Examples

```shell
```

### Set Pin

Sets the new PIN. PIN length must be from 6 to 32 bytes.

Note: This command is only available on the YubiKey FIPS series.

#### Command APDU info

CLA | INS | P1 | P2 | Lc | Data |
:---: | :---: | :---: | :---: | :---: | :---: |
0x00 | 0x03 | 0x00 | 0x00 | Length of Data | (See below) |

##### Data

Data is presented as an inner command APDU:

CLA | INS | P1 | P2 | Lc | Data |
:---: | :---: | :---: | :---: | :---: | :---: |
0x00 | 0x44 | 0x00 | 0x00 | Length of Data | New PIN Length + Current PIN (as bytes) + New PIN (as bytes) |

#### Response APDU info
This command returns status "Success" if the new PIN is set.

|  SW1  |  SW2  |
| :---: | :---: |
| 0x90  | 0x00  |

#### Examples

```shell
```

### Reset

Resets the YubiKey's U2F application back to a factory default state.

Note: Reset on FIPS devices will wipe the attestation certificate from the device preventing the device from being able to be in FIPS-mode again. This reset behavior is specific to U2F on FIPS.

#### Command APDU info

CLA | INS | P1 | P2 | Lc | Data |
:---: | :---: | :---: | :---: | :---: | :---: |
0x00 | 0x03 | 0x00 | 0x00 | Length of Data | (See below) |

##### Data

Data is presented as an inner command APDU:

CLA | INS | P1 | P2 | Lc | Data |
:---: | :---: | :---: | :---: | :---: | :---: |
0x00 | 0x45 | 0x00 | 0x00 | (absent) |(absent) |

#### Response APDU info
This command returns status "Success" if the application was reset.

|  SW1  |  SW2  |
| :---: | :---: |
| 0x90  | 0x00  |

#### Examples

```shell
```

### Verify FIPS mode

Determines if the YubiKey is in a FIPS-approved operating mode.

Note: For the YubiKey FIPS U2F sub-module to be in a FIPS approved mode of operation, an Admin PIN must be set. By default, no Admin PIN is set. Further, if the YubiKey FIPS U2F sub-module has been reset, it cannot be set into a FIPS approved mode of operation, even with the Admin PIN set.

#### Command APDU info

CLA | INS | P1 | P2 | Lc | Data |
:---: | :---: | :---: | :---: | :---: | :---: |
0x00 | 0x03 | 0x00 | 0x00 | Length of Data | (See below) |

##### Data

Data is presented as an inner command APDU:

CLA | INS | P1 | P2 | Lc | Data |
:---: | :---: | :---: | :---: | :---: | :---: |
0x00 | 0x46 | 0x00 | 0x00 | (absent) |(absent) |

#### Response APDU info
Returns "Success" if (and only if) the YubiKey U2F application is currently in "FIPS Approved mode".

|  SW1  |  SW2  |
| :---: | :---: |
| 0x90  | 0x00  |

#### Examples

```shell
```

## CTAP HID extension commands

| Name | Code |
| :--- | :---: |
| GET DEVICE INFO | 0xC2 |
| SET DEVICE INFO | 0xC3 |

### Get device information

Reads configuration and metadata information about the YubiKey. Similar commands exist in other
applications.

#### Command APDU info

|  CLA  |  INS  |  P1   |  P2   |    Lc    |   Data   |
| :---: | :---: | :---: | :---: | :------: | :------: |
| 0x00  | 0xC2  | 0x00  | 0x00  | (absent) | (absent) |

#### Response APDU info

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
| Auto-eject timeout           | 0x06  | Timeout (in seconds) before the YubiKey automatically "ejects" itself.                                    |
| Challenge-response timeout   | 0x07  | The period of time (in seconds) after which the OTP challenge-response command should timeout.        |
| Device flags                 | 0x08  | Device flags that can control device-global behavior.                                                 |
| Configuration lock           | 0x0A  | Indicates whether or not the YubiKey's configuration has been locked by the user.                     |
| Available capabilities (NFC) | 0x0D  | NFC Applications and capabilities that are available for use on this YubiKey.                         |
| Enabled capabilities (NFC)   | 0x0E  | Applications that are currently enabled over USB on this YubiKey.                                     |

#### Examples

```shell
```

### Set device information

Configures device-wide settings on the YubiKey. Similar commands exist in other
applications.

#### Command APDU info

|  CLA  |  INS  |  P1   |  P2   |    Lc    |   Data      |
| :---: | :---: | :---: | :---: | :------: | :---------: |
| 0x00  | 0xC3  | 0x00  | 0x00  | Length of Data | (See Below) |

##### Data

The device information is encoded in Tag-Length-Value (TLV) format. The following table describes the
possible entries (tags).

| Name                         | Value | Description                                                                                           |
| :--------------------------- | :---: | :---------------------------------------------------------------------------------------------------- |
| Enabled capabilities (USB)   | 0x03  | Applications that are currently enabled over USB on this YubiKey.                                     |
| Auto-eject timeout           | 0x06  | Timeout (in ms?) before the YubiKey automatically "ejects" itself.                                    |
| Challenge-response timeout   | 0x07  | The period of time (in seconds) after which the OTP challenge-response command should timeout.        |
| Device flags                 | 0x08  | Device flags that can control device-global behavior.                                                 |
| Configuration lock           | 0x0A  | Indicates whether or not the YubiKey's configuration has been locked by the user.                     |
| Configuration unlock         | 0x0B  | Indicates whether or not the YubiKey's configuration has been locked by the user.                     |
| Reset after configuration    | 0x0C  | Resets (reboots) the YubiKey after the successful application of all configuration updates.           |
| Enabled capabilities (NFC)   | 0x0E  | Applications that are currently enabled over USB on this YubiKey.                                     |

#### Response APDU info

|   Data   |     SW1     |
| :------: | :---------: |
| (absent) | (See below) |

##### Response Status

| Name | SW1 |
| :--- | :---: |
| Success | 0x00 |
| Invalid command | 0x01 |
| Invalid Parameter | 0x02 |
| Invalid length | 0x03 |
| Invalid sequencing | 0x04 |
| Timeout | 0x05 |
| Channel busy | 0x06 |
| Lock required | 0x0A |
| Invalid Channel | 0x0B |
| Other | 0x7F |

#### Examples

```shell
```
