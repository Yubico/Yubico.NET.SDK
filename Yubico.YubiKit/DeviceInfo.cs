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

namespace Yubico.YubiKit;

public readonly record struct DeviceInfo
{
    private const int TAG_USB_SUPPORTED = 0x01;
    private const int TAG_SERIAL_NUMBER = 0x02;
    private const int TAG_USB_ENABLED = 0x03;
    private const int TAG_FORMFACTOR = 0x04;
    private const int TAG_FIRMWARE_VERSION = 0x05;
    private const int TAG_AUTO_EJECT_TIMEOUT = 0x06;
    private const int TAG_CHALLENGE_RESPONSE_TIMEOUT = 0x07;
    private const int TAG_DEVICE_FLAGS = 0x08;
    private const int TAG_NFC_SUPPORTED = 0x0d;
    private const int TAG_NFC_ENABLED = 0x0e;
    private const int TAG_CONFIG_LOCKED = 0x0a;
    private const int TAG_PART_NUMBER = 0x13;
    private const int TAG_FIPS_CAPABLE = 0x14;
    private const int TAG_FIPS_APPROVED = 0x15;
    private const int TAG_PIN_COMPLEXITY = 0x16;
    private const int TAG_NFC_RESTRICTED = 0x17;
    private const int TAG_RESET_BLOCKED = 0x18;
    private const int TAG_VERSION_QUALIFIER = 0x19;
    private const int TAG_FPS_VERSION = 0x20;
    private const int TAG_STM_VERSION = 0x21;
    public string SerialNumber { get; init; }
    public string FirmwareVersion { get; init; }
    public string FormFactor { get; init; }
}