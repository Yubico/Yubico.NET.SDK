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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Yubico.YubiKey.Otp.Operations;

namespace Yubico.YubiKey.Otp
{
    /// <summary>
    /// Helper class to manage the flags used by the YubiKey OTP configuration.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> of the operation class.</typeparam>
    public partial class OtpSettings<T> where T : OperationBase<T>
    {
        private Flag _flags;
        private readonly T _op;

        private IEnumerable<Flag> FlagsSet =>
            ((Flag[])Enum.GetValues(typeof(Flag)))
            .Where(f => _flags.HasFlag(f));

        private T ApplyFlag(Flag flag, bool setIt)
        {
            if (_op.Version < _flagDefinitions[flag].RequiredVersion)
            {
                throw new InvalidOperationException(ExceptionMessages.NotSupportedByYubiKeyVersion);
            }

            _flags = setIt
                ? _flags | flag
                : _flags & ~flag;

            return _op;
        }

        internal bool IsFlagSet(Flag flag) => (_flags & flag) != 0;

        internal OtpSettings(T op)
        {
            _op = op;
        }

        /// <summary>
        /// The YubiKey OTP flags collected in one data-structure.
        /// </summary>
        public YubiKeyFlags YubiKeyFlags
        {
            get
            {
                int bitmask = 0;
                try
                {
                    foreach (Flag flag in FlagsSet.Where(k => k != Flag.None))
                    {
                        OtpFlagItem flagItem = _flagDefinitions[flag];

                        // Doing this here makes the RequiredOr check easier.
                        Flag requiredOr =
                            flagItem.RequiredOrFlags == Flag.None
                                ? flag
                                : flagItem.RequiredOrFlags;

                        // Are there any incompatible flags?
                        if ((_flags & flagItem.InvalidFlags) != Flag.None)
                        {
                            throw new InvalidOperationException(
                                string.Format(
                                    CultureInfo.CurrentCulture,
                                    ExceptionMessages.OtpFlagConflict,
                                    flag.ToString(),
                                    (_flags & flagItem.InvalidFlags).ToString()
                                    ));
                        }

                        // Does this have at least one of the OR flags?
                        if ((_flags & requiredOr) == Flag.None)
                        {
                            throw new InvalidOperationException(

                                // Conditional expression to get the best exception
                                // message for the situation.
                                string.Format(
                                    CultureInfo.CurrentCulture,
                                    1 < GetBitCount(~_flags & flagItem.RequiredOrFlags)
                                        ? ExceptionMessages.OtpFlagRequiredOr
                                        : ExceptionMessages.OtpFlagRequired,
                                    flag.ToString(),
                                    (~_flags & flagItem.RequiredOrFlags).ToString()
                                    ));
                        }

                        // Does this have all of the AND flags?
                        if ((_flags & flagItem.RequiredAndFlags) != flagItem.RequiredAndFlags)
                        {
                            throw new InvalidOperationException(

                                // Conditional expression to get the best exception
                                // message for the situation.
                                string.Format(
                                    CultureInfo.CurrentCulture,
                                    1 < GetBitCount(~_flags & flagItem.RequiredAndFlags)
                                        ? ExceptionMessages.OtpFlagRequiredAnd
                                        : ExceptionMessages.OtpFlagRequired,
                                    flag.ToString(),
                                    (~_flags & flagItem.RequiredOrFlags).ToString()
                                    ));
                        }

                        bitmask |= flagItem.BitMask;
                    }
                }
                catch (KeyNotFoundException)
                {
                    throw new InvalidOperationException(ExceptionMessages.OtpFlagsNotValid);
                }

                return new YubiKeyFlags
                {
                    Ticket = (byte)(bitmask & 0xff),
                    Extended = (byte)((bitmask & 0xff00) >> 8),
                    Configuration = (byte)((bitmask & 0xff0000) >> 16)
                };

                // We would never use this for performance sensitive code,
                // but if we're throwing this exception, that's not a concern
                // here.
                static int GetBitCount(Flag b) => Convert.ToString((byte)b, 2).ToCharArray().Count(c => c == '1');
            }
        }

        /// <summary>
        /// Sets the configuration for OATH HOTP.
        /// </summary>
        /// <remarks>
        /// In order to use OATH HOTP in a slot, neither <see cref="Flag.YubicoOtpChallengeResponse"/>
        /// nor <see cref="Flag.HmacSha1ChallengeResponse"/> can be set.
        /// </remarks>
        public T SetOathHotp(bool setting = true) => ApplyFlag(Flag.OathHotP, setting);

        /// <inheritdoc />
        public override string ToString() => _flags.ToString();

        #region Underlying Flags Enum

        // This is tucked away here so that we have a convenient place to keep
        // the bit definitions for these flags. Also, the framework takes care
        // of printing out the individual flags for PrintString for debugging.
        // Finally, the Enum class gives us the ability to easily iterate
        // through these and check the requirements for each one.
        [Flags]
        internal enum Flag : long
        {
            None = 0,

            // Global Flags
            OathHotP = 0b1L << 1,

            // Ticket Flags
            SendTabFirst = 0b1L << 2,
            AppendTabToFixed = 0b1L << 3,
            AppendTabToOtp = 0b1L << 4,
            AppendDelayToFixed = 0b1L << 5,
            AppendDelayToOtp = 0b1L << 6,
            AppendCarriageReturn = 0b1L << 7,
            ProtectLongPressSlot = 0b1L << 8,

            // Extended Flags
            SerialNumberButtonVisible = 0b1L << 9,
            SerialNumberUsbVisible = 0b1L << 10,
            SerialNumberApiVisible = 0b1L << 11,
            UseNumericKeypad = 0b1L << 12,
            FastTrigger = 0b1L << 13,
            AllowUpdate = 0b1L << 14,
            Dormant = 0b1L << 15,
            InvertLed = 0b1L << 16,

            // Configuration Flags
            SendReferenceString = 0b1L << 17,
            TicketFirst = 0b1L << 18,
            ShortTicket = 0b1L << 19,
            StaticPasswordMode = 0b1L << 20,
            Use8DigitHotp = 0b1L << 21,
            Use10msPacing = 0b1L << 22,
            HmacLessThan64Bytes = 0b1L << 23,
            Use20msPacing = 0b1L << 24,
            UseButtonTrigger = 0b1L << 25,
            AllowHidTrigger = 0b1L << 26,
            UseMixedCasePassword = 0b1L << 27,
            OathFixedModhex1 = 0b1L << 28,
            StaticTicket = 0b1L << 29,
            YubicoOtpChallengeResponse = 0b1L << 30,
            HmacSha1ChallengeResponse = 0b1L << 31,
            UseAlphaNumericPassword = 0b1L << 32,
            OathFixedModhex2 = 0b1L << 33,
            OathFixedModhex = 0b1L << 34,
            AllowManualUpdate = 0b1L << 35,

            // Aggregate Flags for Checking Mode.
            // Important: These are for checking flag interop, not for setting
            // values.
            OutputMode =
                SendTabFirst
                | AppendTabToFixed
                | AppendTabToOtp
                | AppendDelayToFixed
                | AppendDelayToOtp
                | AppendCarriageReturn
                | SendReferenceString
                | TicketFirst
                | Use10msPacing
                | Use20msPacing
                | ShortTicket,

            OathHotPMode =
                OathHotP
                | Use8DigitHotp
                | OathFixedModhex
                | OathFixedModhex1
                | OathFixedModhex2,
            ChallengeResponseMode = YubicoOtpChallengeResponse | HmacSha1ChallengeResponse,

            StaticTicketMode =
                StaticTicket
                | UseMixedCasePassword
                | UseAlphaNumericPassword
                | AllowManualUpdate
        }

        #endregion

        #region Flag Processing Definitions

        private static readonly Dictionary<Flag, OtpFlagItem> _flagDefinitions =
            new Dictionary<Flag, OtpFlagItem>
            {
                [Flag.OathHotP] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_1_0,
                    ticket: 0b1 << 6,
                    invalidFlags: Flag.ChallengeResponseMode),
                [Flag.SendTabFirst] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.All,
                    ticket: 0b1 << 0),
                [Flag.AppendTabToFixed] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.All,
                    ticket: 0b1 << 1),
                [Flag.AppendTabToOtp] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.All,
                    ticket: 0b1 << 2),
                [Flag.AppendDelayToFixed] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.All,
                    ticket: 0b1 << 3),
                [Flag.AppendDelayToOtp] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.All,
                    ticket: 0b1 << 4),
                [Flag.AppendCarriageReturn] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.All,
                    ticket: 0b1 << 5),
                [Flag.ProtectLongPressSlot] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_0_0,
                    ticket: 0b1 << 7),
                [Flag.SerialNumberButtonVisible] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_2_0,
                    extended: 0b1 << 0),
                [Flag.SerialNumberUsbVisible] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_2_0,
                    extended: 0b1 << 1),
                [Flag.SerialNumberApiVisible] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_2_0,
                    extended: 0b1 << 2),
                [Flag.UseNumericKeypad] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_3_0,
                    extended: 0b1 << 3),
                [Flag.FastTrigger] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_3_0,
                    extended: 0b1 << 4),
                [Flag.AllowUpdate] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_3_0,
                    extended: 0b1 << 5),
                [Flag.Dormant] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_3_0,
                    extended: 0b1 << 6),
                [Flag.InvertLed] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_4_0,
                    extended: 0b1 << 7),
                [Flag.SendReferenceString] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.All,
                    config: 0b1 << 0),
                [Flag.TicketFirst] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.All,
                    config: 0b1 << 1),
                [Flag.ShortTicket] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_0_0,
                    config: 0b1 << 1,
                    requiredOrFlags: Flag.StaticTicket),
                [Flag.StaticPasswordMode] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_0_0,
                    config: 0b1 << 1,
                    invalidFlags: Flag.StaticTicket),
                [Flag.Use8DigitHotp] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_1_0,
                    config: 0b1 << 1,
                    requiredOrFlags: Flag.OathHotP),
                [Flag.Use10msPacing] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.All,
                    config: 0b1 << 2,
                    invalidFlags: Flag.ChallengeResponseMode),
                [Flag.HmacLessThan64Bytes] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_2_0,
                    config: 0b1 << 2,
                    requiredOrFlags: Flag.HmacSha1ChallengeResponse),
                [Flag.Use20msPacing] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.All,
                    config: 0b1 << 3,
                    invalidFlags: Flag.ChallengeResponseMode),
                [Flag.UseButtonTrigger] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_2_0,
                    config: 0b1 << 3,
                    requiredOrFlags: Flag.ChallengeResponseMode | Flag.StaticTicket),
                [Flag.AllowHidTrigger] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.All,
                    config: 0b1 << 4),
                [Flag.UseMixedCasePassword] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_0_0,
                    config: 0b1 << 4,
                    requiredOrFlags: Flag.StaticTicket),
                [Flag.OathFixedModhex1] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_1_0,
                    config: 0b1 << 4,
                    requiredOrFlags: Flag.OathHotP),
                [Flag.StaticTicket] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.All,
                    config: 0b1 << 5,
                    invalidFlags: Flag.ChallengeResponseMode),
                [Flag.YubicoOtpChallengeResponse] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_2_0,
                    config: 0b1 << 5,
                    ticket: 0b1 << 6,
                    invalidFlags:
                    Flag.HmacSha1ChallengeResponse
                    | Flag.OutputMode
                    | Flag.StaticTicketMode),
                [Flag.HmacSha1ChallengeResponse] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_2_0,
                    config: (0b1 << 5) | (0b1 << 1),
                    ticket: 0b1 << 6,
                    invalidFlags:
                    Flag.YubicoOtpChallengeResponse
                    | Flag.OutputMode
                    | Flag.StaticTicketMode),
                [Flag.UseAlphaNumericPassword] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_0_0,
                    config: 0b1 << 6,
                    requiredOrFlags: Flag.StaticTicket),
                [Flag.OathFixedModhex2] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_1_0,
                    config: 0b1 << 6,
                    requiredOrFlags: Flag.OathHotP),
                [Flag.OathFixedModhex] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_1_0,
                    config: (0b1 << 4) | (0b1 << 6),
                    requiredOrFlags: Flag.OathHotP),
                [Flag.AllowManualUpdate] = new OtpFlagItem(
                    requiredVersion: FirmwareVersion.V2_0_0,
                    config: 0b1 << 7,
                    requiredOrFlags: Flag.StaticTicket),
            };

        private class OtpFlagItem
        {
            public OtpFlagItem(
                FirmwareVersion requiredVersion,
                byte ticket = 0,
                byte extended = 0,
                byte config = 0,
                Flag invalidFlags = Flag.None,
                Flag requiredOrFlags = Flag.None,
                Flag requiredAndFlags = Flag.None)
            {
                RequiredVersion = requiredVersion;
                BitMask = ticket | (extended << 8) | (config << 16);
                InvalidFlags = invalidFlags;
                RequiredOrFlags = requiredOrFlags;
                RequiredAndFlags = requiredAndFlags;
            }

            // This is the bitmask that gets passed to the YubiKey.
            // Note that we will only use the lower 24 bits of this int.
            public int BitMask { get; }

            // These are things that aren't allowed with this flag.
            public Flag InvalidFlags { get; }

            // These are flags that must have one set with this.
            public Flag RequiredOrFlags { get; }

            // These are flags that all must be set with this.
            public Flag RequiredAndFlags { get; }
            public FirmwareVersion RequiredVersion { get; }
        }

        #endregion
    }
}
