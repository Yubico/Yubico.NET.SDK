---
uid: UsersManualFido2Commands
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

# FIDO2 commands

For each possible U2F command, there will be a class that knows how to build the command
[APDU](xref:UsersManualApdu) and parse the data in the response APDU. Each class will know
what information is needed from the caller for that command.

#### List of FIDO2 commands

* [Version](#get-version)
* [Get Info](#get-info)
___
## Get version

Get the YubiKey's version number.

### Available

All YubiKeys with the FIDO2 application.

### SDK classes

--VersionCommand--xref:Yubico.YubiKey.Fido2.Commands.VersionCommand--

--VersionResponse--xref:Yubico.YubiKey.Fido2.Commands.VersionResponse--

### Input

None.

### Output

[FirmwareVersion](xref:Yubico.YubiKey.FirmwareVersion)

### APDU

[Technical APDU Details](apdu/version.md)
___
## Get info

Get information about the YubiKey's FIDO2 application.

### Available

All YubiKeys with the FIDO2 application.

### SDK classes

--GetInfoCommand--xref:Yubico.YubiKey.Fido2.Commands.GetInfoCommand--

--GetInfoResponse--xref:Yubico.YubiKey.Fido2.Commands.GetInfoResponse--

### Input

None.

### Output

--Fido2DeviceInfo--xref:Yubico.YubiKey.Fido2.Fido2DeviceInfo--

### APDU

[Technical APDU Details](apdu/get-info.md)
___
