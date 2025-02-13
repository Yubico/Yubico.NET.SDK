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

namespace Yubico.YubiKey.Otp
{
    /// <summary>
    /// Flags that control the output format of the one-time password.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The output format is controlled by the <see cref="TicketFlags" />
    /// enumeration. These are binary flags that can be turned on or off. As the
    /// <see cref="Yubico.YubiKey.YubiKeyDevice"/> functionality has been extended, the usage of
    /// these flags have become interleaved to allow full backwards
    /// compatibility.
    /// </para>
    /// <para>
    /// The generalized format of the OTP output string looks like:
    /// <c>ref_string &lt;TAB&gt; fixed_string &lt;TAB&gt; OTP_string &lt;TAB&gt; &lt;CR&gt;</c>
    /// </para>
    /// </remarks>
#pragma warning disable CA1815 // Justification: The instances of value type will not be
    // compared to each other
    public struct TicketFlags
#pragma warning restore CA1815
    {
        private byte _value;
#pragma warning disable CA2225 // Justification: Not necessary to have the expected named alternative method
        /// <summary>
        /// Implicitly convert <see cref="TicketFlags"/> to a <see langword="byte"/>.
        /// </summary>
        /// <param name="flags">Flag object to convert.</param>
        public static implicit operator byte(TicketFlags flags)
            => flags._value;

        /// <summary>
        /// Implicitly convert a <see langword="byte"/> to a <see cref="TicketFlags"/>
        /// object.
        /// </summary>
        /// <param name="b">A byte containing the flags.</param>
        public static implicit operator TicketFlags(byte b)
            => new TicketFlags { _value = b };
#pragma warning restore CA2225
        /// <summary>
        /// No ticket flags are requested for this configuration.
        /// </summary>
        public const byte None = 0x00;

        /// <summary>
        /// Send an initial tab character before the fixed string.
        /// </summary>
        public const byte TabFirst = 0x01;

        /// <summary>
        /// Send a tab character after the fixed string.
        /// </summary>
        public const byte AppendTabToFixed = 0x02;

        /// <summary>
        /// Send a tab character after the OTP string.
        /// </summary>
        public const byte AppendTabToOtp = 0x04;

        /// <summary>
        /// Delay .5 second after emitting the fixed string portion.
        /// </summary>
        public const byte AppendDelayToFixed = 0x08;

        /// <summary>
        /// Delay .5 second after emitting the OTP string.
        /// </summary>
        public const byte AppendDelayToOtp = 0x10;

        /// <summary>
        /// Send a carriage return after all other characters.
        /// </summary>
        public const byte AppendCarriageReturn = 0x20;

        /// <summary>
        /// Sets the configuration for OATH HOTP combination with other <see cref="ConfigurationFlags"/>.
        /// </summary>
        /// <remarks>
        /// In order to use OATH HOTP in a slot, neither the
        /// <see cref="ConfigurationFlags.YubicoOtpChallengeResponse"/> nor
        /// <see cref="ConfigurationFlags.HmacSha1ChallengeResponse"/> flags should be set. If they
        /// are, the slot will instead be configured for challenge response, or result in an error.
        /// </remarks>
        public const byte OathHotp = 0x40;

        /// <summary>
        /// Sets the configuration for challenge response mode, along with other <see cref="ConfigurationFlags"/>.
        /// </summary>
        /// <remarks>
        /// Note that this flag collides with <see cref="OathHotp"/>. In order for the YubiKey to
        /// consider this flag to be ChallengeResponse instead of OathHotp, either the
        /// <see cref="ConfigurationFlags.YubicoOtpChallengeResponse"/> or
        /// <see cref="ConfigurationFlags.HmacSha1ChallengeResponse"/> flags need to be set. Failure
        /// to do so will result in an OATH HOTP configuration, or an error.
        /// </remarks>
        public const byte ChallengeResponse = 0x40;

        /// <summary>
        /// Locks and/or protects the long-press configuration slot of the YubiKey.
        /// </summary>
        public const byte ProtectLongPressSlot = 0x80;

        /// <summary>
        /// Ensure that no flags are set that cannot be used to update an existing configuration.
        /// </summary>
        public void ValidateFlagsForUpdate()
        {
            TicketFlags updatableFlags =
                TabFirst
                | AppendTabToFixed
                | AppendTabToOtp
                | AppendDelayToFixed
                | AppendDelayToOtp
                | AppendCarriageReturn;

            if ((_value & ~updatableFlags) != 0)
            {
                throw new InvalidOperationException(ExceptionMessages.OtpConfigFlagsNotUpdatable);
            }
        }
    }
}
