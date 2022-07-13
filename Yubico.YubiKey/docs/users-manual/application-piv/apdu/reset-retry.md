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


## Reset retry (recover the PIN)

### Command APDU Info

CLA | INS | P1 | P2 | Lc | Data | Le
:---: | :---: | :---: | :---: | :---: | :---:
00 | 2C | 00 | 80 | 10 | *current PUK and new PIN* | (absent)

The data will be 16 bytes long. The PUK is given in the first 8 bytes of the data,
and the new PIN is the next 8 bytes. If the PUK or new PIN is not 8 bytes, it is padded
with FF bytes.

### Response APDU Info

#### Response APDU for RESET RETRY (success)

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 90 | 00

#### Response APDU for RESET RETRY (Invalid PUK)

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 63 | C4

If the PUK entered is incorrect, then the error is `63 CX` where *X* is the number of
retries remaining. In the above, there are 4 retries remaining.

#### Response APDU for RESET RETRY (PUK blocked)

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 69 | 83

The PUK entered might or might not be correct, however, authentication was denied
because the number of retries have been exhausted.

### Examples

```C
$ opensc-tool -c default -s 00:a4:04:00:09:a0:00:00:03:08:00:00:10:00
  -s 00:2C:00:80:10:31:32:33:34:35:36:37:38:31:32:33:34:35:36:37:ff
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 09 A0 00 00 03 08 00 00 10 00
Received (SW1=0x90, SW2=0x00):
61 11 4F 06 00 00 10 00 01 00 79 07 4F 05 A0 00
00 03 08
Sending: 00 2C 00 80 10 31 32 33 34 35 36 37 38 31 32 33 34 35 36 37 FF
Received (SW1=0x90, SW2=0x00)
```
