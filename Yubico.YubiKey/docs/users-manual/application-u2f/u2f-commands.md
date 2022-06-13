---
uid: UsersManualU2fCommands
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

# FIDO U2F commands

For each possible U2F command, there will be a class that knows how to build the command
[APDU](xref:UsersManualApdu) and parse the data in the response APDU. Each class will know
what information is needed from the caller for that command.

Many of the FIDO U2F commands have an APDU that is actually an inner and outer APDU. For
each of these commands, there is an inner APDU to describe the command, which is "wrapped"
in a CTAP1 message command.

The CTAP1 message command is 

```txt
  00 03 00 00 len innerCommand
```

where the `innerCommand` is itself an APDU for the specific command. For example, the
inner command for the Echo command (with 8 bytes of data) is

```txt
  00 40 00 00 08 11 22 33 44 55 66 77 88
```

That is wrapped in the CTAP1 command and sent to the YubiKey. It would be this.

```txt
  00 03 00 00 0B 00 40 00 00 08 11 22 33 44 55 66 77 88
```

For these commands, the APDU documentation specifies the APDU as "Inner command APDU
info".

There are some commands that are not CTAP-wrapped. For these commands, the APDU
documentation specifies the APDU as "Full command APDU".

#### List of FIDO U2F commands

* [Echo](#echo)
* [Get device info](#get-device-info)
* [Set device info](#set-device-info)
* [Set legacy device config](#set-legacy-device-config)
* [Get protocol version](#get-protocol-version)
* [Verify FIPS mode](#verify-fips-mode)
* [Set PIN](#set-pin)
* [Verify PIN](#verify-pin)
* [Register](#register)
* [Authenticate](#authenticate)
* [Reset](#reset)

___
## Echo

Sends data to the YubiKey which immediately echoes the same data back. This command is
defined to be a uniform function for debugging, latency, and performance measurements.

### Available

All YubiKeys with the FIDO U2F application.

### SDK classes

[EchoCommand](xref:Yubico.YubiKey.U2f.Commands.EchoCommand)

[EchoResponse](xref:Yubico.YubiKey.U2f.Commands.EchoResponse)

### Input

The data to echo.

### Output

`ReadOnlyMemory<byte>`

The data that had been originally input.

### APDU

[Technical APDU Details](apdu/echo-cmd.md)
___
## Get device info

Reads configuration and metadata information about the YubiKey (including data not related
to U2F). Similar commands exist in other applications.

This is provided in the U2F application in case the Keyboard and CCID interfaces have been
disabled (see [Set device info](#set-device-info)).

### Available

All YubiKeys with the FIDO U2F application.

### SDK classes

[GetDeviceInfoCommand](xref:Yubico.YubiKey.U2f.Commands.GetDeviceInfoCommand)

[GetDeviceInfoResponse](xref:Yubico.YubiKey.U2f.Commands.GetDeviceInfoResponse)

### Input

None.

<a name="deviceinfooutput"></a>
### Output

A byte array that contains the device info. The first byte is the length. The following
bytes are TLVs. For example,

```txt
2e 01 02 02 3f 03 02 02 3f 02 04 00 b5 fe 55 04
01 01 05 03 05 04 02 06 02 00 00 07 01 0f 08 01
00 0d 02 02 3f 0e 02 02 3f 0a 01 00 0f 01 00

2e
  01 02
     02 3f
  03 02
     02 3f
  02 04
     00 b5 fe 55
  04 01
     01
  05 03
     05 04 02
  06 02
     00 00
  07 01
     0f
  08 01
     00
  0d 02
     02 3f
  0e 02
     02 3f
  0a 01
     00
  0f 01
     00
```
<a name="deviceinfoelements"></a>

#### Table 1: List of DeviceInfo Elements
|    Tag    |                  Meaning                   |          Data               |     Comments     |
| :-------: | :----------------------------------------: | :-------------------------: | :--------------: |
|    01     |   Pre-personalization USB capabilities     |   capabilities bit field    | see [YubiKeyCapabilities](xref:Yubico.YubiKey.YubiKeyCapabilities) |
|    02     |             Serial number                  |  32-bit big-endian integer  | |
|    03     |        Enabled USB capabilities            |   capabilities bit field    | see [YubiKeyCapabilities](xref:Yubico.YubiKey.YubiKeyCapabilities) |
|    04     |              Form factor                   |      form factor byte       | see [FormFactor](xref:Yubico.YubiKey.FormFactor) |
|    05     |            Firmware version                |       3-byte version        | major, minor, patch |
|    06     |           Auto-eject timeout               |       16-bit integer        | if 0, no auto-eject, otherwise seconds to auto-eject |
|    07     |        Challenge-response timeout          |           one byte          | if 0, default, otherwise seconds to timeout |
|    08     |               Device flags                 |           one byte          | see [DeviceFlags](xref:Yubico.YubiKey.DeviceFlags) |
|    0A     |        Configuration lock present          |      one byte, boolean      | 0x00 false, 0x01 true |
|    0D     |   Pre-personalization NFC capabilities     |   capabilities bit field    | see [YubiKeyCapabilities](xref:Yubico.YubiKey.YubiKeyCapabilities) |
|    0E     |        Enabled NFC capabilities            |   capabilities bit field    | see [YubiKeyCapabilities](xref:Yubico.YubiKey.YubiKeyCapabilities) |
|    0F     |              iAP Detection                 |           one byte          | currently ignored |

### APDU

[Technical APDU Details](apdu/get-device-info.md)
___
## Set device info

Sets configuration and metadata information about the YubiKey (including data not related
to U2F). Similar commands exist in other applications.

This is provided in the U2F application in case the Keyboard and CCID interfaces have been
disabled. It is possible to disable the Keyboard and CCID interfaces using this command.

### Available

YubiKey 5 and later.

### SDK classes

[SetDeviceInfoCommand](xref:Yubico.YubiKey.U2f.Commands.SetDeviceInfoCommand)

[U2fHidResponse](xref:Yubico.YubiKey.U2f.Commands.U2fHidResponse)

### Input

See also [SetDeviceInfoBaseCommand](xref:Yubico.YubiKey.Management.Commands.SetDeviceInfoBaseCommand)
for more information on the input data and how it is provided. Each is optional. That is,
if you want to set one of these elements, provide the value. If you want to leave the
element as-is, don't provide it. The exception is the Lock Code. If it is not set, don't
provide one. If it is not yet set and you want to set it, provide it. If it is set, to
make any changes, provide it. If you want to change it, provide the current and new
codes.

* Which USB features are to be enabled ([YubiKeyCapabilities](xref:Yubico.YubiKey.YubiKeyCapabilities))
* Which NFC features are to be enabled ([YubiKeyCapabilities](xref:Yubico.YubiKey.YubiKeyCapabilities))
* Challenge Response timeout
* Auto eject timeout
* [Device flags](xref:Yubico.YubiKey.DeviceFlags)
* Reset after config (a boolean)
* Lock code

### Output

None.

### APDU

[Technical APDU Details](apdu/set-device-info.md)
___
## Set legacy device config

Sets configuration and metadata information about the YubiKey (including data not related
to U2F). This is for YubiKey 4 and prior. To set device information on YubiKeys version 5
and later, use [Set Device Info](#set-device-info).

This is provided in the U2F application in case the Keyboard and CCID interfaces have been
disabled. It is possible to disable the Keyboard and CCID interfaces using this command.

### Available

YubiKey version 4 and prior.

### SDK classes

[SetLegacyDeviceConfigCommand](xref:Yubico.YubiKey.U2f.Commands.SetLegacyDeviceConfigCommand)

[YubiKeyResponse](xref:Yubico.YubiKey.YubiKeyResponse)

### Input

See also [SetLegacyDeviceConfigBase](xref:Yubico.YubiKey.Management.Commands.SetLegacyDeviceConfigBase)
for more information on the input data and how it is provided. Each is optional. That is,
if you want to set one of these elements, provide the value. If you want to leave the
element as-is, don't provide it. The exception is the Lock Code. If it is not set, don't
provide one. If it is not yet set and you want to set it, provide it. If it is set, to
make any changes, provide it. If you want to change it, provide the current and new
codes.

* Which YubiKey interfaces are to be enabled ([YubiKeyCapabilities](xref:Yubico.YubiKey.YubiKeyCapabilities))
* Challenge Response timeout
* Auto eject timeout
* Touch eject enabled

### Output

None.

### APDU

[Technical APDU Details](apdu/set-legacy-device-config.md)
___

## Get protocol version

Get the version of the current session's protocol.

### Available

All YubiKeys with the FIDO U2F application.

### SDK classes

[GetProtocolVersionCommand](xref:Yubico.YubiKey.U2f.Commands.GetProtocolVersionCommand)

[GetProtocolVersionResponse](xref:Yubico.YubiKey.U2f.Commands.GetProtocolVersionResponse)

### Input

None.

### Output

A string describing the version.

### APDU

[Technical APDU Details](apdu/get-protocol-version.md)
___
## Verify FIPS mode

Determine if a FIPS YubiKey is in U2F FIPS mode.

A version 4 FIPS YubiKey is manufactured not in FIPS mode. To place it into FIPS mode, the
U2F PIN must be set. At that point the YubiKey is in U2F FIPS mode. It is possible to
reset the YubiKey to take it out of FIPS mode. However, if a YubiKey is [reset](#reset),
the YubiKey cannot be placed into FIPS mode again.

This command will request the status.

Non-FIPS YubiKeys as well as version 5 FIPS YubiKeys cannot be set to U2F FIPS mode. A
version 5 FIPS YubiKey can be set to FIDO2 FIPS. If this command is sent to a YubiKey that
cannot be set to U2F FIPS mode, the response will be an error.

### Available

All YubiKeys with the FIDO U2F application. However, this is meaningful only on version 4
FIPS YubiKeys.

### SDK classes

[VerifyFipsModeCommand](xref:Yubico.YubiKey.U2f.Commands.VerifyFipsModeCommand)

[VerifyFipsModeResponse](xref:Yubico.YubiKey.U2f.Commands.VerifyFipsModeResponse)

### Input

None.

### Output

`bool`

True if the YubiKey is a FIPS device in FIPS mode, false otherwise.

### APDU

[Technical APDU Details](apdu/verify-fips.md)
___
## Set PIN

Sets the new PIN. The PIN is binary and its length must be 6 to 32 bytes.

Note: This command is only available on the YubiKey FIPS series. In addition, once the PIN
has been set, it is not possible to "unset" the PIN, except by resetting the entire U2F
application. It is possible to change the PIN to something new, but not "remove" the PIN
requirement. Note that be [resetting](#reset), the YubiKey cannot be placed into FIPS mode
again.

### Available

All FIPS YubiKeys with the FIDO U2F application.

### SDK classes

[SetPinCommand](xref:Yubico.YubiKey.U2f.Commands.SetPinCommand)

[U2fResponse](xref:Yubico.YubiKey.U2f.Commands.U2fResponse)

### Input

The current PIN and the new PIN. If there is no current PIN (this is the first time the
PIN is being set), then the only input is the new PIN.

### Output

None.

If the command succeeds, the `Status` will be `ResponseStatus.Success`.

### APDU

[Technical APDU Details](apdu/set-pin.md)
___
## Verify PIN

Verify the PIN for the session. Some documentation calls for "unlocking" the U2F
application. Verifying the PIN is how it is unlocked.

The PIN is binary and its length must be 6 to 32 bytes.

Note: This command is only available on the YubiKey FIPS series.

### Available

All FIPS YubiKeys with the FIDO U2F application.

### SDK classes

[VerifyPinCommand](xref:Yubico.YubiKey.U2f.Commands.VerifyPinCommand)

[U2fResponse](xref:Yubico.YubiKey.U2f.Commands.U2fResponse)

### Input

The current PIN.

### Output

None.

If the command succeeds, the `Status` will be `ResponseStatus.Success`.

### APDU

[Technical APDU Details](apdu/verify-pin.md)
___
## Register

Register the YubiKey with a new account. This is the command that will build the response
to the relying party's registration challenge. It will generate a new key pair, sign the
challenge, and return the public key, attestation cert, and signature.

### Available

All YubiKeys with the FIDO U2F application.

### SDK classes

[RegisterCommand](xref:Yubico.YubiKey.U2f.Commands.RegisterCommand)

[RegisterResponse](xref:Yubico.YubiKey.U2f.Commands.RegisterResponse)

### Input

The hash of the origin (application ID) and the client data hash (containing the
challenge).

### Output

A byte array that contains the registration data. It is encoded as follows.

```txt
05 || public key || key handle length || key handle || cert || signature
```

where the public key is an encoded P-256 ECC public key with both coordinates:

```txt
04 || x-coordinate || y-coordinate
```

The cert is the attestation certificate, and the signature is an ECDSA signature formatted
as the following DER/BER.

```txt
  30 len
     02 len rValue
     02 len sValue 
```

### APDU

[Technical APDU Details](apdu/register.md)
___
## Authenticate

Authenticate the YubiKey to the relying party. This is the command that will build the
response to the relying party's authentication challenge. It will use the appropriate
private key to sign the challenge data.

### Available

All YubiKeys with the FIDO U2F application.

### SDK classes

[AuthenticateCommand](xref:Yubico.YubiKey.U2f.Commands.AuthenticateCommand)

[AuthenticateResponse](xref:Yubico.YubiKey.U2f.Commands.AuthenticateResponse)

### Input

The hash of the origin (application ID), the client data hash (containing the
challenge), and the key handle.

### Output

A byte array that contains the authentication data. It is encoded as follows.

```txt
   user presence || counter || signature
```

Where the user presence is one byte (true or false, 1 or 0) indicating whether the user's
presence was verified, the counter is 4 bytes (big endian), and the signature is an ECDSA
signature formatted as the following DER/BER.

```txt
  30 len
     02 len rValue
     02 len sValue 
```

### APDU

[Technical APDU Details](apdu/authenticate.md)
___
## Reset

Reset the U2F application. This will replace the master key meaning any previous key
handles will be lost with no way to recover them.

If the YubiKey is FIPS, it will also take the YubiKey out of FIPS mode, remove the PIN
requirement, and delete the attestation key and cert. The YubiKey will no longer be able
to be set to FIPS mode again.

### Available

All YubiKeys with the FIDO U2F application.

### SDK classes

[ResetCommand](xref:Yubico.YubiKey.U2f.Commands.ResetCommand)

[U2fResponse](xref:Yubico.YubiKey.U2f.Commands.U2fResponse)

### Input

None.

### Output

None.

### APDU

[Technical APDU Details](apdu/reset.md)

