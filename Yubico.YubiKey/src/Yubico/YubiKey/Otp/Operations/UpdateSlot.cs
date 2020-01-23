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

using System;
using System.Globalization;
using Yubico.YubiKey.Otp.Commands;

namespace Yubico.YubiKey.Otp.Operations
{
    /// <summary>
    /// Class for updating an existing OTP slot configuration.
    /// </summary>
    public class UpdateSlot : OperationBase<UpdateSlot>
    {
        internal UpdateSlot(IYubiKeyConnection connection, IOtpSession session, Slot slot)
            : base(connection, session, slot) { }

        /// <inheritdoc/>
        protected override void ExecuteOperation()
        {
            YubiKeyFlags ykFlags = Settings.YubiKeyFlags;
            var cmd = new UpdateSlotCommand
            {
                YubiKeyFlags = ykFlags,
                OtpSlot = OtpSlot!.Value
            };
            cmd.ApplyCurrentAccessCode(CurrentAccessCode);
            cmd.SetAccessCode(NewAccessCode);

            ReadStatusResponse response = Connection.SendCommand(cmd);
            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.YubiKeyOperationFailed,
                    response.StatusMessage));
            }
        }

        #region Flags to Relay
        /// <inheritdoc cref="OtpSettings{T}.SetDormant(bool)"/>
        /// <returns>The current <see cref="UpdateSlot"/> instance.</returns>
        public UpdateSlot SetDormant(bool setConfig) =>
            Settings.SetDormant(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.UseFastTrigger(bool)"/>
        /// <returns>The current <see cref="UpdateSlot"/> instance.</returns>
        public UpdateSlot SetFastTrigger(bool setConfig) =>
            Settings.UseFastTrigger(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.SetInvertLed(bool)"/>
        /// <returns>The current <see cref="UpdateSlot"/> instance.</returns>
        public UpdateSlot SetInvertLed(bool setConfig) =>
            Settings.SetInvertLed(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.SetSerialNumberApiVisible(bool)"/>
        /// <returns>The current <see cref="UpdateSlot"/> instance.</returns>
        public UpdateSlot SetSerialNumberApiVisible(bool setConfig) =>
            Settings.SetSerialNumberApiVisible(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.SetSerialNumberButtonVisible(bool)"/>
        /// <returns>The current <see cref="UpdateSlot"/> instance.</returns>
        public UpdateSlot SetSerialNumberButtonVisible(bool setConfig) =>
            Settings.SetSerialNumberButtonVisible(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.SetSerialNumberUsbVisible(bool)"/>
        /// <returns>The current <see cref="UpdateSlot"/> instance.</returns>
        public UpdateSlot SetSerialNumberUsbVisible(bool setConfig) =>
            Settings.SetSerialNumberUsbVisible(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.UseNumericKeypad(bool)"/>
        /// <returns>The current <see cref="UpdateSlot"/> instance.</returns>
        public UpdateSlot SetUseNumericKeypad(bool setConfig) =>
            Settings.UseNumericKeypad(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.SendTabFirst(bool)"/>
        /// <returns>The current <see cref="UpdateSlot"/> instance.</returns>
        public UpdateSlot SetSendTabFirst(bool setConfig) =>
            Settings.SendTabFirst(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.AppendTabToFixed(bool)"/>
        /// <returns>The current <see cref="UpdateSlot"/> instance.</returns>
        public UpdateSlot SetAppendTabToFixed(bool setConfig) =>
            Settings.AppendTabToFixed(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.SetAppendTabToOtp(bool)"/>
        /// <returns>The current <see cref="UpdateSlot"/> instance.</returns>
        public UpdateSlot SetAppendTabToOtp(bool setConfig) =>
            Settings.SetAppendTabToOtp(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.AppendDelayToFixed(bool)"/>
        /// <returns>The current <see cref="UpdateSlot"/> instance.</returns>
        public UpdateSlot SetAppendDelayToFixed(bool setConfig) =>
            Settings.AppendDelayToFixed(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.AppendDelayToOtp(bool)"/>
        /// <returns>The current <see cref="UpdateSlot"/> instance.</returns>
        public UpdateSlot SetAppendDelayToOtp(bool setConfig) =>
            Settings.AppendDelayToOtp(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.AppendCarriageReturn(bool)"/>
        /// <returns>The current <see cref="UpdateSlot"/> instance.</returns>
        public UpdateSlot SetAppendCarriageReturn(bool setConfig) =>
            Settings.AppendCarriageReturn(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.Use10msPacing(bool)"/>
        /// <returns>The current <see cref="UpdateSlot"/> instance.</returns>
        public UpdateSlot SetUse10msPacing(bool setConfig) =>
            Settings.Use10msPacing(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.Use20msPacing(bool)"/>
        /// <returns>The current <see cref="UpdateSlot"/> instance.</returns>
        public UpdateSlot SetUse20msPacing(bool setConfig) =>
            Settings.Use20msPacing(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.AllowUpdate(bool)"/>
        /// <returns>The current <see cref="UpdateSlot"/> instance.</returns>
        public UpdateSlot SetAllowUpdate(bool setConfig) =>
            Settings.AllowUpdate(setConfig);

        /// <inheritdoc cref="OtpSettings{T}.ProtectLongPressSlot(bool)"/>
        /// <returns>The current <see cref="UpdateSlot"/> instance.</returns>
        public UpdateSlot ProtectLongPressSlot(bool setConfig = true) =>
            Settings.ProtectLongPressSlot(setConfig);
        #endregion
    }
}
