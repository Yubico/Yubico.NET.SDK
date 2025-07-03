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

using Yubico.YubiKey.Otp.Operations;

namespace Yubico.YubiKey.Otp
{
    public partial class OtpSettings<T> where T : OperationBase<T>
    {
        /// <summary>
        /// Sends a tab character before the fixed string.
        /// </summary>
        public T SendTabFirst(bool setting = true) =>
            ApplyFlag(Flag.SendTabFirst, setting);

        /// <summary>
        /// Sends a tab character after the fixed string.
        /// </summary>
        public T AppendTabToFixed(bool setting = true) =>
            ApplyFlag(Flag.AppendTabToFixed, setting);

        /// <summary>
        /// Sends a tab character after the OTP string.
        /// </summary>
        public T SetAppendTabToOtp(bool setting = true) =>
            ApplyFlag(Flag.AppendTabToOtp, setting);

        /// <summary>
        /// Adds a 500ms delay after sending the fixed string.
        /// </summary>
        public T AppendDelayToFixed(bool setting = true) =>
            ApplyFlag(Flag.AppendDelayToFixed, setting);

        /// <summary>
        /// Adds a 500ms delay after sending the OTP string.
        /// </summary>
        public T AppendDelayToOtp(bool setting = true) =>
            ApplyFlag(Flag.AppendDelayToOtp, setting);

        /// <summary>
        /// Sends a carriage return [Enter Key] after all characters have been sent.
        /// </summary>
        public T AppendCarriageReturn(bool setting = true) =>
            ApplyFlag(Flag.AppendCarriageReturn, setting);

        /// <summary>
        /// Locks and/or protects the long press configuration slot of the YubiKey.
        /// </summary>
        public T ProtectLongPressSlot(bool setting = true) =>
            ApplyFlag(Flag.ProtectLongPressSlot, setting);
    }
}
