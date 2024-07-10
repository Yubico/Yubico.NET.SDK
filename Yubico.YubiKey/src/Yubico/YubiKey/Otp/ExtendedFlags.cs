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

namespace Yubico.YubiKey.Otp
{
    /// <summary>
    ///     Extended properties and configuration settings.
    /// </summary>
    #pragma warning disable CA1815 // Justification: The instances of value type will not be

    // compared to each other
    public struct ExtendedFlags
        #pragma warning restore CA1815
    {
        private byte _value;
        #pragma warning disable CA2225 // Justification: Not necessary to have the expected named alternative method
        /// <summary>
        ///     Implicitly convert <see cref="ExtendedFlags" /> to a <see langword="byte" />.
        /// </summary>
        /// <param name="flags">Flag object to convert.</param>
        public static implicit operator byte(ExtendedFlags flags) => flags._value;

        /// <summary>
        ///     Implicitly convert a <see langword="byte" /> to a <see cref="ExtendedFlags" />
        ///     object.
        /// </summary>
        /// <param name="b">A byte containing the flags.</param>
        public static implicit operator ExtendedFlags(byte b) => new ExtendedFlags { _value = b };
        #pragma warning restore CA2225
        /// <summary>
        ///     No extended flags are requested for this configuration.
        /// </summary>
        public const byte None = 0x00;

        /// <summary>
        ///     Allows the serial number to be retrieved by holding the touch button while
        ///     inserting the device into the USB port.
        /// </summary>
        /// <remarks>
        ///     Once the LED starts to flash, release the button and the serial number will then be sent as a string of digits.
        ///     This flag is a device wide setting. If it is set in either configurable slot, it is considered
        ///     enabled by the device.
        /// </remarks>
        public const byte SerialNumberButtonVisible = 0x01;

        /// <summary>
        ///     Makes the serial number appear in the YubiKey's USB descriptor's iSerialNumber
        ///     field.
        /// </summary>
        /// <remarks>
        ///     Note that this makes each device unique from the host computer's view.
        ///     This flag is a device wide setting. If it is set in either configurable slot, it is considered
        ///     enabled by the device.
        /// </remarks>
        public const byte SerialNumberUsbVisible = 0x02;

        /// <summary>
        ///     Allows the serial number to be read by proprietary means, including being
        ///     visible to the Yubico.YubiKey SDK.
        /// </summary>
        /// <remarks>
        ///     This flag is a device wide setting. If it is set in either configurable slot, it is considered
        ///     enabled by the device.
        /// </remarks>
        public const byte SerialNumberApiVisible = 0x04;

        /// <summary>
        ///     Causes numeric characters to be sent as keystrokes from the numeric keypad
        ///     rather than the normal numeric keys on an 84-key keyboard.
        /// </summary>
        public const byte UseNumericKeypad = 0x08;

        /// <summary>
        ///     Causes the trigger action of the YubiKey button to become faster.
        /// </summary>
        /// <remarks>
        ///     It only applies when one configuration is written.
        ///     If both configurations are set, this flag has no effect.
        /// </remarks>
        public const byte FastTrigger = 0x10;

        /// <summary>
        ///     Allows certain non-security related flags to be modified after the configuration
        ///     has been written.
        /// </summary>
        public const byte AllowUpdate = 0x20;

        /// <summary>
        ///     Allows a configuration to be stored without being accessible.
        /// </summary>
        public const byte Dormant = 0x40;

        /// <summary>
        ///     Inverts the configured state of the LED.
        /// </summary>
        public const byte InvertLed = 0x80;

        /// <summary>
        ///     Ensure that no flags are set that cannot be used to update an existing configuration.
        /// </summary>
        public void ValidateFlagsForUpdate()
        {
            ExtendedFlags updatableFlags =
                AllowUpdate
                | Dormant
                | FastTrigger
                | InvertLed
                | SerialNumberApiVisible
                | SerialNumberButtonVisible
                | SerialNumberUsbVisible
                | UseNumericKeypad;

            if ((_value & ~updatableFlags) != 0)
            {
                throw new InvalidOperationException(ExceptionMessages.OtpConfigFlagsNotUpdatable);
            }
        }
    }
}
