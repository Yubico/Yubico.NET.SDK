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


## Shared PIN, no PUK



## Resetting a YubiKey Bio MPE

For all YubiKeys except for the YubiKey Bio MPE, factory resets are done *by application*. For example, if you wanted to reset the PIV and FIDO applications on a YubiKey 5 Series key, you would need to perform both a PIV reset and a FIDO reset. 

The YubiKey Bio MPE is a special case. Given that the PIV and FIDO applications on a YubiKey Bio MPE share a PIN, it is not possible to perform factory resets for the PIV and FIDO applications individually. Instead, a special device-wide reset must be used, which resets both PIV and FIDO applications at the same time. This device-wide reset can be performed via the ``DeviceReset()`` method, the ``DeviceResetCommand()``, or by sending a command APDU with the device reset instruction. 

### DeviceReset() method

Using the [DeviceReset()](xref:Yubico.YubiKey.YubiKeyDevice.DeviceReset) method is simple: connect to a YubiKey, then call the method on that key.

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



### APDUs

**Command APDU**:

| CLA | INS | P1 | P2 |    Lc    |   Data   |    Le    |
|:---:|:---:|:--:|:--:|:--------:|:--------:|:--------:| 
| 00  | 1F  | 00 | 00 | (absent) | (absent) | (absent) |

**Response APDU (success)**:

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 90  | 00  |

**Response APDU (failure)**:

Total Length: 2\
Data Length: 0

|   Data    | SW1 | SW2 |
|:---------:|:---:|:---:|
| (no data) | 6f  | 00  |