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
    /// <summary>
    ///     Represents the current status of the YubiKey OTP application.
    /// </summary>
    public class OtpStatus
    {
        /// <summary>
        ///     Constructs a new instance of the OtpStatus class.
        /// </summary>
        public OtpStatus()
        {
            FirmwareVersion = new FirmwareVersion();
        }

        /// <summary>
        ///     The version of the firmware running on the YubiKey.
        /// </summary>
        public FirmwareVersion FirmwareVersion { get; set; }

        /// <summary>
        ///     The configuration sequence number, used to see if the OTP configuration has been modified.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Operations that modify the OTP configuration, such as programming a slot or NDEF, will
        ///         result in the sequence number being incremented. In cases where the command class is
        ///         partnered with the <see cref="Commands.ReadStatusResponse" /> class, the sequence number must be
        ///         compared with its prior value. If the sequence number has incremented, the configuration
        ///         has been applied. If it hasn't, then there was some issue encountered when trying to
        ///         apply the configuration.
        ///     </para>
        ///     <param>
        ///         The sequence number is only internally consistent for as long as the device is powered.
        ///         That is, if the YubiKey is unplugged or otherwise loses power, the sequence number will
        ///         reset its value to `1`.
        ///     </param>
        ///     <para>
        ///         Note that the sequence number may be `0` if no valid configurations are present within
        ///         the OTP application.
        ///     </para>
        /// </remarks>
        public byte SequenceNumber { get; set; }

        /// <summary>
        ///     The capacitive touch level reported by the YubiKey. Reserved for Yubico internal use.
        /// </summary>
        /// <remarks>
        ///     The touch level should not be used or relied upon in normal applications. It is a value
        ///     that is used during the manufacturing and production testing of YubiKey devices prior to
        ///     customer fulfillment.
        /// </remarks>
        public byte TouchLevel { get; set; }

        /// <summary>
        ///     Indicates that the short-press configuration (slot 1) is present and valid.
        /// </summary>
        public bool ShortPressConfigured { get; set; }

        /// <summary>
        ///     Indicates that the short-press configuration (slot 1) requires touch for operation.
        /// </summary>
        public bool ShortPressRequiresTouch { get; set; }

        /// <summary>
        ///     Indicates that the long-press configuration (slot 2) is present and valid.
        /// </summary>
        public bool LongPressConfigured { get; set; }

        /// <summary>
        ///     Indicates that the long-press configuration (slot 2) requires touch for operation.
        /// </summary>
        public bool LongPressRequiresTouch { get; set; }

        /// <summary>
        ///     The indicator LED behavior is inverted.
        /// </summary>
        public bool LedBehaviorInverted { get; set; }
    }
}
