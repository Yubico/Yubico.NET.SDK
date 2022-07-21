---
uid: OtpCommandUpdateSlot
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


# Update slot

Updates the flags for a given configuration slot if the slot configuration allows for it. The slot
must either have the "Allow Update" flag set, or be marked as "Dormant".

## Available

YubiKey firmware 2.3 and later

## Command APDU info

|  CLA  |  INS  |     P1      |  P2   |  Lc   |    Data     |
| :---: | :---: | :---------: | :---: | :---: | :---------: |
| 0x00  | 0x01  | (See below) | 0x00  |  52   | (see below) |

### P1: Slot

P1 determines which slot to program. It can have one of the following values:

| Option               | Value |
| :------------------- | :---: |
| Short press (Slot 1) | 0x04  |
| Long press (Slot 2)  | 0x05  |

### Data: Configuration structure

The data field contains a standard configuration structure.

| Field       | Size  | Description                   |
| :---------- | :---: | :---------------------------- |
| Fixed Data  |  16   | Do not set this field.        |
| UID         |   6   | Do not set this field.        |
| AES Key     |  16   | Do not set this field.        |
| Access Code |   6   | Do not set this field.        |
| EXT Flags   |   1   | Extended flags                |
| TKT Flags   |   1   | Ticket configuration flags    |
| CFG Flags   |   2   | General configuration flags   |
| Reserved    |   2   | Must be zero                  |
| CRC         |   2   | CRC16 value of all the fields |

The only changes that are allowed are the setting of the following flags:

- Ticket Flags: Tab First, Append Tab 1, Append Tab 2, Append Delay 1, Append Delay 2, Append Carriage Return
- Config Flags: Pacing 10ms, Pacing 20ms
- Extended Flags: Serial Number Visibility (Button, USB, API), Use Numeric Keypad, Fast Trigger, Allow Update, Dormant, Invert LED

## Response APDU info

|  Lr   |                 Data                  |  SW1  |  SW1  |
| :---: | :-----------------------------------: | :---: | :---: |
| 0x06  | [Status structure](#status-structure) | 0x90  | 0x00  |

## Examples

TODO
