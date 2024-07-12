<!-- Copyright 2024 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->


## Verify UV 

### Command APDU info

CLA | INS | P1 | P2 | Lc | Data | Le
:---: | :---: | :---: | :---: | :---: | :---:
00 | 20 | 00 | 96 | *variable* | *variable* | (absent)

The data bytes vary depending on the intended operation:
- none  - check the biometric state
- 02 00 - request a temporary PIN
- 03 00 - perform biometric verification

### Response APDU info

#### Response APDU for VERIFY (success)

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 90 | 00

#### Response APDU for VERIFY (success with temporary PIN)

Total Length: 18\
Data Length: 16

Data | SW1 | SW2
:---: | :---: | :---:
temporary PIN | 90 | 00

#### Response APDU for VERIFY (Invalid biometric match)

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 63 | C0 - C2

If the biometric match failed, the error is `63 CX` where *X* is the number of
retries remaining. When no retries remain, biometric verification becomes blocked, and the error returned is `0x6983` (`AuthenticationMethodBlocked`). To proceed in this situation, the [PIV PIN verification](xref:PivApduVerifyPIN) must be used.
