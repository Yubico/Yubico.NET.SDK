// Copyright 2021 Yubico AB
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

namespace Yubico.YubiKey.Otp
{
    internal static class OtpConstants
    {
        // These are common to all of the OTP commands.
        // One of these values goes in the Ins byte of the APDU.
        public const byte RequestSlotInstruction = 0x01;
        public const byte ReadStatusInstruction = 0x03;

        // These are the commands specific to the individual command types.
        // One of these values goes in the P1 byte of the APDU.

        // ConfigureSlotCommand
        public const byte ConfigureShortPressSlot = 0x01;
        public const byte ConfigureLongPressSlot = 0x03;
        // UpdateSlotCommand
        public const byte UpdateShortPressSlot = 0x04;
        public const byte UpdateLongPressSlot = 0x05;
        // ConfigureNdefCommand
        public const byte ProgramNDEFShortPress = 0x08;
        public const byte ProgramNDEFLongPress = 0x09;
        // GetDeviceInfoCommand
        public const byte GetDeviceInfoSlot = 0x13;
        // GetSerialNumberCommand
        public const byte SerialNumberSlot = 0x10;
        // QueryFipsModeCommand
        public const byte QueryFipsSlot = 0x14;
        // SetLegacyDeviceConfigCommand
        public const byte WriteDeviceConfig = 0x11;
        // SwapSlotsCommand
        public const byte SwapSlotsSlot = 0x06;
        // SetDeviceInfoSlot
        public const byte SetDeviceInfoSlot = 0x15;
    }
}
