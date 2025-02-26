---
uid: OtpCommandConfigureSlot
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

# Configure slot

Commits a configuration to one of two programmable slots. Slot 1 corresponds to the "short press"
of the YubiKey button, and Slot 2 the "long press".

## Available

Short press (slot 1): YubiKey firmware 1.x and later\
Long press (slot 2): YubiKey firmware 2.0 and later

Note: Access over USB (CCID) disabled after YubiKey firmware 5.x

## Command APDU info

| CLA  | INS  |     P1      |  P2  | Lc |    Data     |
|:----:|:----:|:-----------:|:----:|:--:|:-----------:|
| 0x00 | 0x01 | (See below) | 0x00 | 52 | (see below) |

### P1: Slot

P1 determines which slot to program. It can have one of the following values:

| Option               | Value |
|:---------------------|:-----:|
| Short press (Slot 1) | 0x01  |
| Long press (Slot 2)  | 0x03  |

### Data: Configuration structure

The data field contains a standard configuration structure.

| Field       | Size | Description                        |
|:------------|:----:|:-----------------------------------|
| Fixed Data  |  16  | Fixed data in binary form.         |
| UID         |  6   | Fixed UID part of the ticket.      |
| AES Key     |  16  | AES key                            |
| Access Code |  6   | Access code to re-program the slot |
| EXT Flags   |  1   | Extended flags                     |
| TKT Flags   |  1   | Ticket configuration flags         |
| CFG Flags   |  1   | General configuration flags        |
| Reserved    |  2   | Must be zero                       |
| CRC         |  2   | CRC16 value of all the fields      |

<!-- TODO -->
TBD page will go into more detail on how to construct a valid configuration.

## Response APDU info

|  Lr  |                         Data                          | SW1  | SW1  |
|:----:|:-----------------------------------------------------:|:----:|:----:|
| 0x06 | [Status structure](xref:OtpCommands#status-structure) | 0x90 | 0x00 |

## Examples

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
