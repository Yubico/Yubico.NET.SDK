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
    /// Flags that control the functionality of the programmed OTP slot. This includes switching the
    /// slot's mode.
    /// </summary>
#pragma warning disable CA1815 // Justification: The instances of value type will not be
    // compared to each other
    public struct ConfigurationFlags
#pragma warning restore CA1815
    {
        private byte _value;
#pragma warning disable CA2225 // Justification: Not necessary to have the expected
        // named alternative method
        /// <summary>
        /// Implicitly convert <see cref="ConfigurationFlags"/> to a <see langword="byte"/>.
        /// </summary>
        /// <param name="flags">Flag object to convert.</param>
        public static implicit operator byte(ConfigurationFlags flags)
            => flags._value;

        /// <summary>
        /// Implicitly convert a <see langword="byte"/> to a <see cref="ConfigurationFlags"/>
        /// object.
        /// </summary>
        /// <param name="b">A byte containing the flags.</param>
        public static implicit operator ConfigurationFlags(byte b)
            => new ConfigurationFlags { _value = b };
#pragma warning restore CA2225
        /// <summary>
        /// No special configuration modifiers are requested for this configuration.
        /// </summary>
        public const byte None = 0x00;

        /// <summary>
        /// Output a reference string of the ModHex characters 0..15 first.
        /// </summary>
        /// <remarks>
        /// This can be used by the verifying application to verify the mapping of the modhex characters.
        /// For all YubiKeys with a firmware version of 2.0 or later, if set in combination with the
        /// <see cref="UseAlphaNumericPassword"/> flag, this string will be replaced with a shifted
        /// character '1' (typically '!' on most keyboard layouts). This can be used to meet strong
        /// password requirements where at least one character is required to be a "special character".
        ///</remarks>
        public const byte SendReferenceString = 0x01;

        /// <summary>
        /// Reserved for compatibility with the YubiKey 1. (Deprecated).
        /// </summary>
        [Obsolete(message: "This value is here for compatibility with YubiKey 1.", error: false)]
        public const byte TicketFirst = 0x02;

        /// <summary>
        /// Truncate the OTP part to 16 characters.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This function is only meaningful in static mode as a truncated dynamic OTP cannot be
        /// successfully decoded.
        /// </para>
        /// <para>
        /// Note that this flag's value collides with other flags in this enumeration. In order to
        /// enable short ticket mode, this flag must be used with <see cref="StaticTicket"/>.
        /// </para>
        /// </remarks>
        public const byte ShortTicket = 0x02;

        /// <summary>
        /// Configures the slot to emit a fixed set of characters, commonly referred to as "static
        /// password" mode.
        /// </summary>
        /// <remarks>
        /// Note that this flag's value collides with other flags in this enumeration. In order to
        /// enable ExtendedScanCodes mode, the <see cref="StaticTicket"/> flag must NOT be set.
        /// </remarks>
        public const byte ExtendedScanCodes = 0x02;

        /// <summary>
        /// Configures the slot for OATH-HOTP mode, using an 8 digit password.
        /// </summary>
        /// <remarks>
        /// Note that this flag's value collides with other flags in this enumeration. In order to
        /// enable 8-digit HOTPs, the <see cref="TicketFlags.OathHotp"/> flag must be set.
        /// </remarks>
        public const byte Use8DigitHotp = 0x02;

        /// <summary>
        /// Add an inter-character pacing time of 10ms between keystrokes.
        /// </summary>
        /// <remarks>
        /// Note that this flag's value collides with other flags in this enumeration. In order to
        /// use 10ms pacing, the <see cref="ChallengeResponse"/> flag must NOT be set.
        /// </remarks>
        public const byte Use10msPacing = 0x04;

        /// <summary>
        /// Set when the HMAC message is less than 64 bytes.
        /// </summary>
        /// <remarks>
        /// Note that this flag's value collides with other flags in this enumeration. In order to
        /// have the intended meaning, this flag must be used with the <see cref="ChallengeResponse"/>
        /// flag.
        /// </remarks>
        public const byte HmacLessThan64Bytes = 0x04;

        /// <summary>
        /// Add an inter-character pacing time of 20ms between keystrokes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When set, an intra-character pacing time of 20 milliseconds is added between each sent
        /// keystroke. Combined with the Use10msPacing flag, the total delay is 30 milliseconds.
        /// </para>
        /// <para>
        /// Note that this flag's value collides with other flags in this enumeration. In order to
        /// use 20ms pacing, the <see cref="ChallengeResponse"/> flag must NOT be set.
        /// </para>
        /// </remarks>
        public const byte Use20msPacing = 0x08;

        /// <summary>
        /// Require YubiKey button touch for challenge response configuration.
        /// </summary>
        /// <remarks>
        /// Note that this flag's value collides with other flags in this enumeration. In order to
        /// have the intended meaning, this flag must be used with the <see cref="ChallengeResponse"/>
        /// flag.
        /// </remarks>
        public const byte UseButtonTrigger = 0x08;

        /// <summary>
        /// Reserved for compatibility with the YubiKey 1. (Deprecated).
        /// </summary>
        [Obsolete(message: "This value is here for compatibility with YubiKey 1.", error: false)]
        public const byte AllowHidTrigger = 0x10;

        /// <summary>
        /// Enable generation of mixed-case characters.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Setting this flag enables generation of mixed-case characters required by password policy
        /// settings in some legacy systems.
        /// </para>
        /// <para>
        /// Note that this flag's value collides with other flags in this enumeration. In order to
        /// have the intended meaning, the <see cref="TicketFlags.OathHotp"/> flag must NOT be set.
        /// </para>
        /// </remarks>
        public const byte UseMixedCasePassword = 0x10;

        /// <summary>
        /// Specifies that the first byte of the token identifier should be modhex.
        /// </summary>
        /// <remarks>
        /// Note that this flag's value collides with other flags in this enumeration. In order to
        /// have the intended meaning, the <see cref="TicketFlags.OathHotp"/> flag must be set.
        /// </remarks>
        public const byte OathFixedModhex1 = 0x10;

        /// <summary>
        /// Uses the same "OTP" generation algorithm, but all dynamic fields are set to fixed values.
        /// </summary>
        /// <remarks>
        /// Note that this flag's value collides with other flags in this enumeration. In order to
        /// have the intended meaning, the <see cref="TicketFlags.ChallengeResponse"/> flag must
        /// NOT be set.
        /// </remarks>
        public const byte StaticTicket = 0x20;

        /// <summary>
        /// Enables Challenge-Response mode instead of an OTP mode.
        /// </summary>
        /// <remarks>
        /// Note that this flag's value collides with other flags in this enumeration. In order to
        /// have the intended meaning, the <see cref="TicketFlags.ChallengeResponse"/> flag must be set.
        /// </remarks>
        public const byte ChallengeResponse = 0x20;

        /// <summary>
        /// Enables Yubico OTP challenge-response mode.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This flag, set together with ChallengeResponse enables Yubico OTP challenge-response mode.
        /// </para>
        /// <para>
        /// When set, the configuration does not work in normal OTP mode.
        /// This flag must be used with the <see cref="ChallengeResponse"/> and
        /// <see cref="TicketFlags.ChallengeResponse"/> flags.
        /// </para>
        /// </remarks>
        public const byte YubicoOtpChallengeResponse = 0x20;

        /// <summary>
        /// Enabled HMAC-SHA1 challenge-response mode.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This flag, set together with ChallengeResponse enables HMAC-SHA1 challenge-response mode.
        /// </para>
        /// <para>
        /// When set, the configuration does not work in normal OTP mode.
        /// This flag must be used with the <see cref="ChallengeResponse"/> and
        /// <see cref="TicketFlags.ChallengeResponse"/> flags.
        /// </para>
        /// </remarks>
        public const byte HmacSha1ChallengeResponse = 0x22;

        /// <summary>
        /// Enable generation of mixed character and digits.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Setting this flag enables generation of mixed character and digits required by password
        /// policy settings in some legacy systems.
        /// </para>
        /// <para>
        /// Note that this flag's value collides with other flags in this enumeration. In order to
        /// have the intended meaning, the <see cref="TicketFlags.ChallengeResponse"/> flag must
        /// NOT be set.
        /// </para>
        /// </remarks>
        public const byte UseAlphaNumericPassword = 0x40;

        /// <summary>
        /// Specifies that the first two bytes of the token identifier should be modhex.
        /// </summary>
        /// <remarks>
        /// Note that this flag's value collides with other flags in this enumeration. In order to
        /// have the intended meaning, the <see cref="TicketFlags.OathHotp"/> flag must also be set.
        /// </remarks>
        public const byte OathFixedModhex2 = 0x40;

        /// <summary>
        /// Specifies that all bytes of the token identifier should be modhex.
        /// </summary>
        /// <remarks>
        /// Note that this flag can only be used if the <see cref="TicketFlags.OathHotp"/> flag is
        /// also set. It may have unintended side effects if used in other contexts.
        /// </remarks>
        public const byte OathFixedModhex = 0x50;

        /// <summary>
        /// Configures the slot to allow for user-triggered static password change.
        /// </summary>
        public const byte AllowManualUpdate = 0x80;

        /// <summary>
        /// Ensure that no flags are set that cannot be used to update an existing configuration.
        /// </summary>
        public void ValidateFlagsForUpdate()
        {
            ConfigurationFlags updatableFlags = Use10msPacing | Use20msPacing;

            if ((_value & ~updatableFlags) != 0)
            {
                throw new InvalidOperationException(ExceptionMessages.OtpConfigFlagsNotUpdatable);
            }
        }
    }
}
