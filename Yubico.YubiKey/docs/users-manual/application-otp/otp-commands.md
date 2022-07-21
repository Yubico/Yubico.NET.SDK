---
uid: OtpCommands
summary: *content
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
