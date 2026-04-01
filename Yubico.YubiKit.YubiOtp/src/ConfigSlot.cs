// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Encodes the OTP protocol command/slot byte sent to the YubiKey.
/// These values are used as P1 in SmartCard APDUs or as the slot byte in OTP HID frames.
/// </summary>
public enum ConfigSlot : byte
{
    Config1 = 0x01,
    Config2 = 0x03,
    Update1 = 0x04,
    Update2 = 0x05,
    Swap = 0x06,
    Ndef1 = 0x08,
    Ndef2 = 0x09,
    DeviceSerial = 0x10,
    ScanMap = 0x12,
    ChalHmac1 = 0x30,
    ChalHmac2 = 0x38
}
