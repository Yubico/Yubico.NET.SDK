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

using Yubico.YubiKey.Otp.Operations;

namespace Yubico.YubiKey.Otp
{
    public partial class OtpSettings<T> where T : OperationBase<T>
    {
        /// <summary>
        /// Allows the serial number to be retrieved by holding down the touch button while inserting
        /// the device into the USB port.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Once the LED starts to flash, release the button and the serial number will then be sent
        /// as a string of digits.
        /// </para>
        /// <para>
        /// This is a device wide setting. If it is set in either configurable slot, it is considered
        /// enabled by the device.
        /// </para>
        /// </remarks>
        public T SetSerialNumberButtonVisible(bool setting = true) =>
            ApplyFlag(Flag.SerialNumberButtonVisible, setting);

        /// <summary>
        /// Makes the serial number appear in the YubiKey's USB descriptor's iSerialNumber field.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This makes each device unique from the host computer's view.
        /// </para>
        /// <para>
        /// This is a device wide setting. If it is set in either configurable slot, it is considered
        /// enabled by the device.
        /// </para>
        /// </remarks>
        public T SetSerialNumberUsbVisible(bool setting = true) => ApplyFlag(Flag.SerialNumberUsbVisible, setting);

        /// <summary>
        /// Allows the serial number to be read by proprietary means, including being
        /// visible to the Yubico.YubiKey SDK.
        /// </summary>
        /// <remarks>
        /// This is a device wide setting. If it is set in either configurable slot, it is considered
        /// enabled by the device.
        /// </remarks>
        public T SetSerialNumberApiVisible(bool setting = true) => ApplyFlag(Flag.SerialNumberApiVisible, setting);

        /// <summary>
        /// Causes numeric characters to be sent as keystrokes from the numeric keypad rather than the
        /// normal numeric keys on an 84-key keyboard.
        /// </summary>
        public T UseNumericKeypad(bool setting = true) => ApplyFlag(Flag.UseNumericKeypad, setting);

        /// <summary>
        /// Causes the trigger action of the YubiKey button to become faster.
        /// </summary>
        /// <remarks>
        /// This only applies when one configuration is written. If both configurations are active,
        /// this setting has no effect.
        /// </remarks>
        public T UseFastTrigger(bool setting = true) => ApplyFlag(Flag.FastTrigger, setting);

        /// <summary>
        /// Allows certain non-security related settings to be modified after the configuration
        /// has been written.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The list below is of all settings that can be updated when this setting is set. However,
        /// some of the options are not compatible with all settings, so it's important to use care
        /// when choosing settings to apply later.
        /// </para>
        /// <list type="bullet">
        /// <listheader><b>Settings That Can Be Updated</b></listheader>
        /// <item>
        /// <term><see cref="AllowUpdate(bool)"/></term>
        /// <description><inheritdoc cref="AllowUpdate(bool)" path="/summary"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="SetDormant(bool)"/></term>
        /// <description><inheritdoc cref="SetDormant(bool)" path="/summary"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="UseFastTrigger(bool)"/></term>
        /// <description><inheritdoc cref="UseFastTrigger(bool)" path="/summary"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="SetInvertLed(bool)"/></term>
        /// <description><inheritdoc cref="SetInvertLed(bool)" path="/summary"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="SetSerialNumberApiVisible(bool)"/></term>
        /// <description><inheritdoc cref="SetSerialNumberApiVisible(bool)" path="/summary"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="SetSerialNumberButtonVisible(bool)"/></term>
        /// <description><inheritdoc cref="SetSerialNumberButtonVisible(bool)" path="/summary"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="SetSerialNumberUsbVisible(bool)"/></term>
        /// <description><inheritdoc cref="SetSerialNumberUsbVisible(bool)" path="/summary"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="UseNumericKeypad(bool)"/></term>
        /// <description><inheritdoc cref="UseNumericKeypad(bool)" path="/summary"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="SendTabFirst(bool)"/></term>
        /// <description><inheritdoc cref="SendTabFirst(bool)" path="/summary"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="AppendTabToFixed(bool)"/></term>
        /// <description><inheritdoc cref="AppendTabToFixed(bool)" path="/summary"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="SetAppendTabToOtp(bool)"/></term>
        /// <description><inheritdoc cref="SetAppendTabToOtp(bool)" path="/summary"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="AppendDelayToFixed(bool)"/></term>
        /// <description><inheritdoc cref="AppendDelayToFixed(bool)" path="/summary"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="AppendDelayToOtp(bool)"/></term>
        /// <description><inheritdoc cref="AppendDelayToOtp(bool)" path="/summary"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="AppendCarriageReturn(bool)"/></term>
        /// <description><inheritdoc cref="AppendCarriageReturn(bool)" path="/summary"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="Use10msPacing(bool)"/></term>
        /// <description><inheritdoc cref="Use10msPacing(bool)" path="/summary"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="Use20msPacing(bool)"/></term>
        /// <description><inheritdoc cref="Use20msPacing(bool)" path="/summary"/></description>
        /// </item>
        /// </list>
        /// </remarks>
        public T AllowUpdate(bool setting = true) => ApplyFlag(Flag.AllowUpdate, setting);

        /// <summary>
        /// Allows a configuration to be stored without being accessible.
        /// </summary>
        public T SetDormant(bool setting = true) => ApplyFlag(Flag.Dormant, setting);

        /// <summary>
        /// Inverts the configured state of the LED.
        /// </summary>
        public T SetInvertLed(bool setting = true) => ApplyFlag(Flag.InvertLed, setting);
    }
}
