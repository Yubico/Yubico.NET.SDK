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

using System;
using Yubico.YubiKey.Otp.Operations;

namespace Yubico.YubiKey.Otp
{
    /// <summary>
    /// Interface for <see cref="OtpSession"/>.
    /// </summary>
    /// <remarks>
    /// This exists to facilitate mocking and dependency-injection for unit tests.
    /// Unless you are developing another mock session for testing, you have no
    /// reason to directly use this interface.
    /// </remarks>
    public interface IOtpSession : IDisposable
    {
        #region OTP Operation Object Factory
        /// <inheritdoc cref="OtpSession.CalculateChallengeResponse(Slot)"/>
        public CalculateChallengeResponse CalculateChallengeResponse(Slot slot);

        /// <inheritdoc cref="OtpSession.ConfigureYubicoOtp(Slot)"/>
        public ConfigureYubicoOtp ConfigureYubicoOtp(Slot slot);

        /// <inheritdoc cref="OtpSession.DeleteSlotConfiguration(Slot)"/>
        public DeleteSlotConfiguration DeleteSlotConfiguration(Slot slot);

        /// <inheritdoc cref="OtpSession.ConfigureChallengeResponse(Slot)"/>
        public ConfigureChallengeResponse ConfigureChallengeResponse(Slot slot);

        /// <inheritdoc cref="OtpSession.ConfigureHotp(Slot)"/>
        public ConfigureHotp ConfigureHotp(Slot slot);

        /// <inheritdoc cref="OtpSession.ConfigureNdef(Slot)"/>
        public ConfigureNdef ConfigureNdef(Slot slot);

        /// <inheritdoc cref="OtpSession.ConfigureStaticPassword(Slot)"/>
        public ConfigureStaticPassword ConfigureStaticPassword(Slot slot);

        /// <inheritdoc cref="OtpSession.UpdateSlot(Slot)"/>
        public UpdateSlot UpdateSlot(Slot slot);
        #endregion

        #region Non-Builder Implementations
        /// <inheritdoc cref="OtpSession.DeleteSlot(Slot)"/>
        public void DeleteSlot(Slot slot);

        /// <inheritdoc cref="OtpSession.SwapSlots"/>
        public void SwapSlots();

        /// <inheritdoc cref="OtpSession.ReadNdefTag"/>
        public NdefDataReader ReadNdefTag();
        #endregion

        #region Properties
        /// <inheritdoc cref="OtpStatus.ShortPressConfigured"/>
        public bool IsShortPressConfigured { get; }

        /// <inheritdoc cref="OtpStatus.ShortPressRequiresTouch"/>
        public bool ShortPressRequiresTouch { get; }

        /// <inheritdoc cref="OtpStatus.LongPressConfigured"/>
        public bool IsLongPressConfigured { get; }

        /// <inheritdoc cref="OtpStatus.LongPressRequiresTouch"/>
        public bool LongPressRequiresTouch { get; }

        internal FirmwareVersion FirmwareVersion { get; }

        internal IYubiKeyDevice YubiKey { get; }
        #endregion
    }
}
