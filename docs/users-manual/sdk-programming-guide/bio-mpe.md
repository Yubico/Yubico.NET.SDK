---
uid: UsersManualBioMpe
---

<!-- Copyright 2025 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# YubiKey Bio Multi-protocol Edition considerations and quirks

YubiKey Bio Multi-protocol Edition (MPE) keys possess some unique attributes that require special consideration when using the .NET YubiKey SDK compared to other YubiKeys with FIDO and PIV capabilities. This page details these differences and how to manage them.

## Shared PIN, no PUK

Typically, YubiKeys that have both the PIV and FIDO applications (like the 5 Series) have separate PIV and FIDO PINs. However, the YubiKey Bio MPE, which integrates fingerprint biometrics from the FIDO application with PIV functionality, uses a shared PIN for the FIDO and PIV applications. Use of the shared PIN results in two major changes:

- The addition of a special [device-wide reset](#resetting-a-yubikey-bio-mpe), which resets both the FIDO and PIV applications simultaneously
- The omission of the PIV [PUK](xref:UsersManualPinPukMgmtKey) (PIN Unblocking Key)

No PUK means that once a YubiKey Bio MPE's PIN has been blocked, there is no way to unblock/change the PIN — the key must be reset. This also means that the SDK's [ResetRetryCommand](xref:UsersManualPivCommands#reset-retry-recover-the-pin) (used to reset the PIN using the PUK) will fail along with any attempt to change the nonexistent PUK with the [ChangeReferenceDataCommand](xref:UsersManualPivCommands#change-reference-data).

## Resetting a YubiKey Bio MPE

For all YubiKeys except for the YubiKey Bio MPE, factory resets are done strictly *by application*. For example, if you wanted to reset the PIV and FIDO applications on a YubiKey 5 Series key, you would need to perform both a [PIV reset](xref:Yubico.YubiKey.Piv.PivSession.ResetApplication) and a [FIDO reset](xref:Fido2Reset). 

Under most circumstances, it is not possible to perform factory resets for the PIV and FIDO applications individually with the YubiKey Bio MPE. Instead, a special device-wide reset must be used, which resets both PIV and FIDO applications at the same time. This device-wide reset can be performed via the ``DeviceReset()`` method, the ``DeviceResetCommand()``, or by sending a command APDU with the device reset instruction. 

> [!NOTE]
> The individual FIDO reset can technically be used with YubiKey Bio MPE keys, but *only* if the FIDO application is not "blocked" (check the key's [ResetBlocked](xref:Yubico.YubiKey.YubiKeyDevice.ResetBlocked) property to confirm). The individual PIV reset cannot be used with YubiKey Bio MPE keys regardless of the PIV application's ``ResetBlocked`` status.

### DeviceReset() method

Using the [DeviceReset()](xref:Yubico.YubiKey.YubiKeyDevice.DeviceReset) method is simple: [connect](xref:UsersManualMakingAConnection) to a YubiKey with the [YubiKeyDevice](xref:Yubico.YubiKey.YubiKeyDevice) class, then call the method on that key.

To select the first available YubiKey connected to your host, use:

```C#
IEnumerable<IYubiKeyDevice> yubiKeyList = YubiKeyDevice.FindAll();

var yubiKey = yubiKeyList.First();
```

Then perform the reset:

```C#
yubiKey.DeviceReset();
```

### DeviceResetCommand() 

The device-wide reset can also be performed using the lower-level [DeviceResetCommand](xref:Yubico.YubiKey.Management.Commands.DeviceResetCommand) and [DeviceResetResponse](xref:Yubico.YubiKey.Management.Commands.DeviceResetResponse) classes (which is what the ``DeviceReset()`` method implements under the hood). 

After connecting to a particular YubiKey with the ``YubiKeyDevice`` class as shown in the previous example, we need to set up an additional connection to the key's management application using the [IYubiKeyConnection](xref:Yubico.YubiKey.IYubiKeyConnection) class:

```C#
IYubiKeyConnection connection = yubiKey.Connect(YubiKeyApplication.Management);
```

Then send the ``DeviceResetCommand`` to the key:

```C#
DeviceResetCommand resetCommand = new DeviceResetCommand();
DeviceResetResponse resetResponse = connection.SendCommand(resetCommand);
```

For error handling, check the ``DeviceResetResponse`` instance's [Status](xref:Yubico.YubiKey.IYubiKeyResponse.Status) and [StatusMessage](xref:Yubico.YubiKey.IYubiKeyResponse.StatusMessage) properties. For general information on using the SDK's command classes, see [Commands](xref:UsersManualCommands).

### DeviceReset APDUs

At the lowest level, the device-wide reset can be performed by sending a command [APDU](xref:UsersManualApdu) to a YubiKey and handling its response APDU (which is what the ``DeviceResetCommand`` and ``DeviceResetResponse`` implement under the hood). 

The command APDU is simple, requiring the instruction ``1F`` with no additional data. The response APDU returned from the key will only contain the status word.

**Command APDU**:

| CLA | INS | P1 | P2 |    Lc    |   Data   |    Le    |
|:---:|:---:|:--:|:--:|:--------:|:--------:|:--------:| 
| 00  | 1F  | 00 | 00 | (absent) | (absent) | (absent) |

**Response APDU (success)**:

Total Length: 2 bytes

Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 90  | 00  |

**Response APDU (failure)**:

Total Length: 2 bytes

Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 6f  | 00  |

#### OpenSC example

To perform the device reset on a YubiKey Bio MPE with a tool like [OpenSC](https://github.com/OpenSC/OpenSC), you must first send a command APDU to connect to the key's management application (``00a4040008a000000527471117``) followed by the device reset command APDU (``001F0000``):

```text
opensc-tool -c default -s 00a4040008a000000527471117 -s 001F0000
Using reader with a card: Yubico YubiKey FIDO+CCID
Sending: 00 A4 04 00 08 A0 00 00 05 27 47 11 17 
Received (SW1=0x90, SW2=0x00):
56 69 72 74 75 61 6C 20 6D 67 72 20 2D 20 46 57 Virtual mgr - FW
20 76 65 72 73 69 6F 6E 20 35 2E 37 2E 32        version 5.7.2
Sending: 00 1F 00 00 
Received (SW1=0x90, SW2=0x00)
```