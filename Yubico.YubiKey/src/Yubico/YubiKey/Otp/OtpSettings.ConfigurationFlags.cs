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
using Yubico.YubiKey.Otp.Operations;

namespace Yubico.YubiKey.Otp
{
    public partial class OtpSettings<T> where T : OperationBase<T>
    {
        /// <summary>
        /// Sends a reference string of the ModHex characters for 0-15 before the fixed or OTP data.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This can be used by the verifying application to verify the mapping of the modhex characters.
        /// </para>
        /// <para>
        /// For all YubiKeys with a firmware version of 2.0 or later, if set in combination with
        /// <see cref="UseAlphaNumericPassword(bool)"/>, this string will be replaced with a
        /// shifted character '1' (typically '!' on most keyboard layouts). This can be used to meet
        /// strong password requirements where at least one character is required to be a "special
        /// character".
        /// </para>
        /// </remarks>
        public T SendReferenceString(bool setting = true) =>
            ApplyFlag(Flag.SendReferenceString, setting);

        /// <summary>
        /// Reserved for compatibility with the YubiKey 1. Usage of this option is discouraged.
        /// </summary>
        [Obsolete("Reserved for compatibility with the YubiKey 1. Usage of this option is discouraged.")]
        public T TicketFirst(bool setting = true) =>
            ApplyFlag(Flag.TicketFirst, setting);

        /// <summary>
        /// Truncates the OTP string to 16 characters.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This function is only valid in static mode as a truncated dynamic OTP cannot be successfully
        /// decoded.
        /// </para>
        /// <para>
        /// In order to enable short ticket mode, you must also use <see cref="UseStaticTicketMode(bool)"/>.
        /// </para>
        /// </remarks>
        public T ShortTicket(bool setting = true) =>
            ApplyFlag(Flag.ShortTicket, setting);

        /// <summary>
        /// Configures the slow to emit a static password.
        /// </summary>
        /// <remarks>
        /// This setting is not compatible with <see cref="UseStaticTicketMode(bool)"/>, or
        /// <see cref="ShortTicket(bool)"/>.
        /// </remarks>
        public T UseStaticPasswordMode(bool setting = true) =>
            ApplyFlag(Flag.StaticPasswordMode, setting);

        /// <summary>
        /// Configures the slot to use an eight-digit password for OATH-HOTP.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This setting must be used with <see cref="SetOathHotp(bool)"/>.
        /// </para>
        /// <para>
        /// By default, OATH-HOTP uses six-digit passwords.
        /// </para>
        /// </remarks>
        public T Use8DigitHotp(bool setting = true) =>
            ApplyFlag(Flag.Use8DigitHotp, setting);

        /// <summary>
        /// Adds an inter-character pacing time of 10ms between each keystroke.
        /// </summary>
        /// <remarks>
        /// This setting is not compatible with <see cref="UseYubicoOtpChallengeResponseMode(bool)"/> nor
        /// <see cref="UseHmacSha1ChallengeResponseMode(bool)"/>.
        /// </remarks>
        public T Use10msPacing(bool setting = true) =>
            ApplyFlag(Flag.Use10msPacing, setting);

        /// <summary>
        /// Set when the HMAC message is less than 64 bytes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default HMAC challenge is exactly 64 bytes. This setting specifies that the challenge
        /// will always be <b>less than</b> 64 bytes.
        /// </para>
        /// <para>
        /// This setting must be used with <b>either</b> <see cref="UseYubicoOtpChallengeResponseMode(bool)"/> or
        /// <see cref="UseHmacSha1ChallengeResponseMode(bool)"/>.
        /// </para>
        /// </remarks>
        public T HmacLessThan64Bytes(bool setting = true) =>
            ApplyFlag(Flag.HmacLessThan64Bytes, setting);

        /// <summary>
        /// Adds an inter-character pacing time of 20ms between each keystroke.
        /// </summary>
        /// <remarks>
        /// This setting is <b>not</b> compatible with <see cref="UseYubicoOtpChallengeResponseMode(bool)"/> nor
        /// <see cref="UseHmacSha1ChallengeResponseMode(bool)"/>.
        /// </remarks>
        public T Use20msPacing(bool setting = true) =>
            ApplyFlag(Flag.Use20msPacing, setting);

        /// <summary>
        /// Require user acceptance by touching the YubiKey button for challenge-response operations 
        /// </summary>
        /// <remarks>
        /// This setting must be used with <b>either</b> <see cref="UseYubicoOtpChallengeResponseMode(bool)"/>
        /// or <see cref="UseHmacSha1ChallengeResponseMode(bool)"/>.
        /// </remarks>
        public T UseButtonTrigger(bool setting = true) =>
            ApplyFlag(Flag.UseButtonTrigger, setting);

        /// <summary>
        /// Reserved for compatibility with the YubiKey 1. Usage of this option is discouraged.
        /// </summary>
        [Obsolete("Reserved for compatibility with the YubiKey 1. Usage of this option is discouraged.")]
        public T AllowHidTrigger(bool setting = true) =>
            ApplyFlag(Flag.AllowHidTrigger, setting);

        /// <summary>
        /// Enable use of mixed case characters for password generation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This accomodates some legacy systems that require mixed-case characters in passwords.
        /// </para>
        /// <para>
        /// This setting is incompatible with <see cref="SetOathHotp(bool)"/>.
        /// </para>
        /// </remarks>
        public T UseMixedCasePassword(bool setting = true) =>
            ApplyFlag(Flag.UseMixedCasePassword, setting);

        /// <summary>
        /// Specifies that the first byte of the token identifier should be modhex.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This setting must be used with <see cref="SetOathHotp(bool)"/>.
        /// </para>
        /// </remarks>
        public T OathFixedModhex1(bool setting = true) =>
            ApplyFlag(Flag.OathFixedModhex1, setting);

        /// <summary>
        /// Sets all dynamic fields to fixed values.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Static mode uses the same "OTP" generation algorithm, but all dynamic fields are set to
        /// fixed values.
        /// </para>
        /// <para>
        /// This setting is <b>not</b> compatible with <see cref="Flag.YubicoOtpChallengeResponse"/> nor
        /// <see cref="Flag.HmacSha1ChallengeResponse"/>.
        /// </para>
        /// </remarks>
        public T UseStaticTicketMode(bool setting = true) =>
            ApplyFlag(Flag.StaticTicket, setting);

        /// <summary>
        /// Enables Yubico OTP Challenge-Response Mode
        /// </summary>
        /// <remarks>
        /// <para>
        /// When set, the configuration does not work in normal OTP mode.
        /// </para>
        /// <para>
        /// This setting is mutually exclusive with <see cref="UseHmacSha1ChallengeResponseMode(bool)"/>.
        /// Also, it is incompatible with <see cref="SetOathHotp(bool)"/>.
        /// </para>
        /// </remarks>
        public T UseYubicoOtpChallengeResponseMode(bool setting = true) =>
            ApplyFlag(Flag.YubicoOtpChallengeResponse, setting);

        /// <summary>
        /// HMAC-SHA1 Challenge-Response Mode
        /// </summary>
        /// <remarks>
        /// <para>
        /// When set, the configuration does not work in normal OTP mode.
        /// </para>
        /// <para>
        /// This setting is mutually exclusive with <see cref="UseYubicoOtpChallengeResponseMode(bool)"/>.
        /// Also, it is incompatible with <see cref="SetOathHotp(bool)"/>.
        /// </para>
        /// </remarks>
        public T UseHmacSha1ChallengeResponseMode(bool setting = true) =>
            ApplyFlag(Flag.HmacSha1ChallengeResponse, setting);

        /// <summary>
        /// Enables generation of mixed characters and digits.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Satisfies some legacy systems' requirement for mixed characters and digits in passwords.
        /// </para>
        /// <para>
        /// This setting is <b>not</b> compatible with <see cref="Flag.YubicoOtpChallengeResponse"/> nor
        /// <see cref="Flag.HmacSha1ChallengeResponse"/>.
        /// </para>
        /// </remarks>
        public T UseAlphaNumericPassword(bool setting = true) =>
            ApplyFlag(Flag.UseAlphaNumericPassword, setting);

        /// <summary>
        /// Specifies that the first two bytes of the token identifier should be modhex.
        /// </summary>
        /// <remarks>
        /// This setting must be used with <see cref="SetOathHotp(bool)"/>.
        /// </remarks>
        public T UseOathFixedModhex2(bool setting = true) =>
            ApplyFlag(Flag.OathFixedModhex2, setting);

        /// <summary>
        /// Specifies that all bytes of the token identifier should be modhex.
        /// </summary>
        /// <remarks>
        /// This setting must be used with <see cref="SetOathHotp(bool)"/>.
        /// </remarks>
        public T UseOathFixedModhex(bool setting = true) =>
            ApplyFlag(Flag.OathFixedModhex, setting);

        /// <summary>
        /// Configures the slot to allow for user-triggered static password change.
        /// </summary>
        /// <remarks>
        /// YubiKey 2 and later supports user-initiated update of a static password. If
        /// configured, the user presses and holds the key for 8-15 seconds. When
        /// the button is released, the indicator light flashes. By pressing shortly,
        /// the change is confirmed and the new OTP is yielded.
        /// </remarks>
        public T AllowManualUpdate(bool setting = true) =>
            ApplyFlag(Flag.AllowManualUpdate, setting);
    }
}
