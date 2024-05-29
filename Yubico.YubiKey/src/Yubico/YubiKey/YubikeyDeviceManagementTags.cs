// Copyright 2023 Yubico AB
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

namespace Yubico.YubiKey
{
    /// <summary>
    /// Device info tags that can be accessed in the device
    /// </summary>
    internal static class YubikeyDeviceManagementTags
    {
        internal const byte UsbPrePersCapabilitiesTag = 0x01;
        internal const byte SerialNumberTag = 0x02;
        internal const byte UsbEnabledCapabilitiesTag = 0x03;
        internal const byte FormFactorTag = 0x04;
        internal const byte FirmwareVersionTag = 0x05;
        internal const byte AutoEjectTimeoutTag = 0x06;
        internal const byte ChallengeResponseTimeoutTag = 0x07;
        internal const byte DeviceFlagsTag = 0x08;
        internal const byte ConfigurationLockPresentTag = 0x0a;
        internal const byte ConfigurationUnlockPresentTag = 0x0b;
        internal const byte ResetAfterConfigTag = 0x0c;
        internal const byte NfcPrePersCapabilitiesTag = 0x0d;
        internal const byte NfcEnabledCapabilitiesTag = 0x0e;
        internal const byte IapDetectionTag = 0x0f;
        internal const byte MoreDataTag = 0x10;
        internal const byte FreeFormTag = 0x11;
        internal const byte HidInitDelay = 0x12;
        internal const byte PartNumberTag = 0x13;
        internal const byte FipsCapableTag = 0x14;
        internal const byte FipsApprovedTag = 0x15;
        internal const byte PinComplexityTag = 0x16;
        internal const byte NfcRestrictedTag = 0x17;
        internal const byte ResetBlockedTag = 0x18;
        internal const byte TemplateStorageVersionTag = 0x20; // FPS version tag
        internal const byte ImageProcessorVersionTag = 0x21; // STM version tag
        internal const byte TempTouchThresholdTag = 0x85;
    }
}
