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


## Change reference data

### Command APDU Info

CLA | INS | P1 | P2 | Lc | Data | Le
:---: | :---: | :---: | :---: | :---: | :---:
00 | 24 | 00 | 80 | 10 | *current PIN and new PIN* | (absent)
00 | 24 | 00 | 81 | 10 | *current PUK and new PUK* | (absent)

Which element to change is given in the P2 field of the APDU and the current and new
data (old and new PIN or PUK) are given in the data field. The data is simply the two
values concatenated.

Both the PIN and PUK are allowed to be 6 to 8 characters, but if one is less than 8, it
will be padded with 0xff. For example, the default PIN is "123456", but on the device,
it is represented as `31 32 33 34 34 36 FF FF`.

The data is therefore 16 bytes, current value (possibly padded) followed by the new
value (possibly padded).

### Response APDU Info

#### Response APDU for CHANGE REFERENCE DATA (success)

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 90 | 00

#### Response APDU for CHANGE REFERENCE DATA (invalid current PIN or PUK)

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 63 | C2

If the PIN entered is incorrect, then the error is `63 CX` where *X* is the number of
retries remaining. In the above, there are 2 retries remaining.

#### Response APDU for CHANGE REFERENCE DATA (PIN or PUK Blocked)

Total Length: 2\
Data Length: 0

Data | SW1 | SW2
:---: | :---: | :---:
(no data) | 69 | 83

The PIN or PUK entered might or might not be correct, however, authentication was denied
because the number of retries have been exhausted.

### Examples

```C
$ opensc-tool -c default -s 00:a4:04:00:09:a0:00:00:03:08:00:00:10:00
  -s 00:24:00:80:10:31:32:33:34:35:36:ff:ff:36:35:34:33:32:31:ff:ff
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 09 A0 00 00 03 08 00 00 10 00
Received (SW1=0x90, SW2=0x00):
61 11 4F 06 00 00 10 00 01 00 79 07 4F 05 A0 00
00 03 08
Sending: 00 24 00 80 10 31 32 33 34 35 36 FF FF 36 35 34 33 32 31 FF FF
Received (SW1=0x90, SW2=0x00)

$ opensc-tool -c default -s 00:a4:04:00:09:a0:00:00:03:08:00:00:10:00
   -s 00:24:00:81:10:31:32:33:34:35:36:37:38:38:37:36:35:34:33:32:31
Using reader with a card: Yubico YubiKey OTP+FIDO+CCID 0
Sending: 00 A4 04 00 09 A0 00 00 03 08 00 00 10 00
Received (SW1=0x90, SW2=0x00):
61 11 4F 06 00 00 10 00 01 00 79 07 4F 05 A0 00
00 03 08
Sending: 00 24 00 81 10 31 32 33 34 35 36 37 38 38 37 36 35 34 33 32 31
Received (SW1=0x90, SW2=0x00)
```
