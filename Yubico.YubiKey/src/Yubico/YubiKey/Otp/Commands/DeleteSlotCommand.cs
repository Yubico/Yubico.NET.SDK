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

namespace Yubico.YubiKey.Otp.Commands
{
    /// <summary>
    /// Command to delete an OTP slot configuration.
    /// </summary>
    /// <remarks>
    /// The purpose of this command is to build a completely empty APDU (except
    /// for a security code, if the slot has one). This is what tells the YubiKey
    /// to delete the OTP slot configuration.
    /// </remarks>
    public class DeleteSlotCommand : SlotConfigureBase
    {
        protected override byte ShortPressCode => OtpConstants.ConfigureShortPressSlot;
        protected override byte LongPressCode => OtpConstants.ConfigureLongPressSlot;
        protected override bool CalculateCrc => false;
    }
}
