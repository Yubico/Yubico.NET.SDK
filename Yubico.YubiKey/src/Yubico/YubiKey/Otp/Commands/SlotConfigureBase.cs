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
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using Yubico.Core.Buffers;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Otp.Commands
{
    /// <summary>
    /// Base class for commands that configure, update, or delete an OTP slot.
    /// </summary>
    public abstract class SlotConfigureBase : IYubiKeyCommand<ReadStatusResponse>
    {
        #region Offset Reference Constants
#pragma warning disable CS1591 // Documenting this would be overkill.
        protected const int FixedDataOffset = 0;
        protected const int UidOffset = FixedDataOffset + FixedDataLength;
        protected const int AesKeyOffset = UidOffset + UidLength;
        protected const int AccessCodeOffset = AesKeyOffset + AesKeyLength;
        protected const int FixedSizeOffset = AccessCodeOffset + AccessCodeLength;
        protected const int ExtendedFlagsOffset = FixedSizeOffset + 1;
        protected const int TicketFlagsOffset = ExtendedFlagsOffset + 1;
        protected const int ConfigurationFlagsOffset = TicketFlagsOffset + 1;
        protected const int ReservedOffset = ConfigurationFlagsOffset + 1;
        protected const int CrcOffset = ReservedOffset + 2;
        protected const int CurrentAccessCodeOffset = CrcOffset + 2;
        protected const int ConfigurationStructSize = CurrentAccessCodeOffset + AccessCodeLength;
#pragma warning restore CS1591
        #endregion

        #region Component Size Reference Constants
        /// <summary>
        /// The required size for the FixedData buffer.
        /// </summary>
        internal const int FixedDataLength = 16;

        /// <summary>
        /// The required size for the Uid buffer.
        /// </summary>
        internal const int UidLength = 6;

        /// <summary>
        /// The required size for the AesKey buffer.
        /// </summary>
        internal const int AesKeyLength = 16;

        /// <summary>
        /// The required size for the AccessCode buffer.
        /// </summary>
        internal const int AccessCodeLength = 6;

        /// <summary>
        /// The maximum length of a static password.
        /// </summary>
        internal const int MaxPasswordLength = FixedDataLength + UidLength + AesKeyLength;
        #endregion

        private readonly Memory<byte> _configurationBuffer = new byte[ConfigurationStructSize];

        /// <summary>
        /// Gets reference to the raw buffer that contains the configuration.
        /// </summary>
        protected Span<byte> ConfigurationBuffer => _configurationBuffer.Span;
        private Slot _otpSlot = Slot.ShortPress;

        /// <summary>
        /// The code to use for indicating the short-press OTP slot.
        /// </summary>
        protected abstract byte ShortPressCode { get; }

        /// <summary>
        /// The code to use for indicating the long-press OTP slot.
        /// </summary>
        protected abstract byte LongPressCode { get; }

        /// <summary>
        /// Allows the command to indicate whether to calculate the CRC for the buffer.
        /// </summary>
        protected virtual bool CalculateCrc => true;

        /// <summary>
        /// Determines which of the two configurable slots this configuration is for.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if setting an invalid value is attempted.</exception>
        public Slot OtpSlot
        {
            get => _otpSlot;
            set
            {
                if (value != Slot.ShortPress && value != Slot.LongPress)
                {
                    throw new ArgumentException(ExceptionMessages.InvalidOtpSlot, nameof(value));
                }

                _otpSlot = value;
            }
        }

        /// <inheritdoc />
        public YubiKeyApplication Application => YubiKeyApplication.Otp;

        /// <summary>
        /// Adds the access code currently protecting the configuration to the command. This is needed
        /// to apply a new configuration to a write-protected slot.
        /// </summary>
        /// <remarks>
        /// If the configurable slot is protected by an access code, that code must be supplied by
        /// the caller through this method. If the intention is to retain the access code and leave
        /// its value unchanged, the <see cref="SetAccessCode"/> function must also be
        /// called. Failure to do so will result in the access code being removed from the slot,
        /// effectively making it unprotected.
        /// </remarks>
        /// <param name="accessCode">The current access code to the configurable slot.</param>
        public void ApplyCurrentAccessCode(ReadOnlySpan<byte> accessCode)
        {
            if (accessCode.Length != AccessCodeLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPropertyLength,
                        nameof(accessCode),
                        AccessCodeLength,
                        accessCode.Length));
            }

            // The current access code is placed at the end of the config structure
            accessCode.CopyTo(
                ConfigurationBuffer.Slice(CurrentAccessCodeOffset, AccessCodeLength));
        }

        /// <summary>
        /// An access code that can be used to protect the slot configuration.
        /// </summary>
        /// <remarks>
        /// <para>
        /// It should be noted that setting an access code will lock the ability to modify the configuration
        /// of the slot. There is no way to recover usage of the slot if the access code is lost or forgotten.
        /// It is important to stress this potential pitfall when designing any application or process that
        /// relies on this feature.
        /// </para>
        /// <para>
        /// If a configurable slot already has an access code and you need to apply it to a new configuration,
        /// call the <see cref="ApplyCurrentAccessCode"/> method. The value specified by SetAccessCode
        /// will be the code used to protect the slot after the configuration has been applied.
        /// </para>
        /// <para>
        /// Note that if the slot is already protected by an access code, and you wish to have the same
        /// access code remain, both ApplyCurrentAccessCode and SetAccessCode must be called with the same
        /// value. Failure to call SetAccessCode will effectively cause the newly applied configuration
        /// to be unprotected.
        /// </para>
        /// <para>
        /// Setting the access code to all zeros is equivalent to not setting an access code. The slot
        /// will not be protected.
        /// </para>
        /// </remarks>
        /// <param name="accessCode">The value to use as the access code for the new configuration.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if the access code length doesn't equal <see cref="AccessCodeLength"/>.
        /// </exception>
        public void SetAccessCode(ReadOnlySpan<byte> accessCode)
        {
            if (accessCode.Length != AccessCodeLength)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPropertyLength,
                        nameof(accessCode),
                        AccessCodeLength,
                        accessCode.Length));
            }

            accessCode.CopyTo(ConfigurationBuffer.Slice(AccessCodeOffset, AccessCodeLength));
        }

        /// <summary>
        /// Extended flags that control behaviors on either a slot or global basis.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if an invalid flag set is specified.
        /// </exception>
        public virtual ExtendedFlags ExtendedFlags
        {
            get => ConfigurationBuffer[ExtendedFlagsOffset];
            set => ConfigurationBuffer[ExtendedFlagsOffset] = value;
        }

        /// <summary>
        /// Flags that control the output format of the text returned by the YubiKey button press.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if an invalid flag set is specified.
        /// </exception>
        public virtual TicketFlags TicketFlags
        {
            get => ConfigurationBuffer[TicketFlagsOffset];
            set => ConfigurationBuffer[TicketFlagsOffset] = value;
        }

        /// <summary>
        /// Flags that define the mode and other configurable options for this slot.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if an invalid flag set is specified.
        /// </exception>
        public virtual ConfigurationFlags ConfigurationFlags
        {
            get => ConfigurationBuffer[ConfigurationFlagsOffset];
            set => ConfigurationBuffer[ConfigurationFlagsOffset] = value;
        }

        /// <summary>
        /// YubiKey flags that control YubiKey behavior.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if an invalid flag set is specified.
        /// </exception>
        public virtual YubiKeyFlags YubiKeyFlags
        {
            get => new YubiKeyFlags
            {
                Extended = ExtendedFlags,
                Ticket = TicketFlags,
                Configuration = ConfigurationFlags
            };
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                ExtendedFlags = value.Extended;
                TicketFlags = value.Ticket;
                ConfigurationFlags = value.Configuration;
            }
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            if (CalculateCrc)
            {
                short crc = (short)~Crc13239.Calculate(ConfigurationBuffer.Slice(0, CrcOffset));
                BinaryPrimitives.WriteInt16LittleEndian(ConfigurationBuffer.Slice(CrcOffset, 2), crc);
            }

            return new CommandApdu()
            {
                Ins = OtpConstants.RequestSlotInstruction,
                P1 = OtpSlot == Slot.ShortPress
                    ? ShortPressCode
                    : LongPressCode,
                Data = ConfigurationBuffer.ToArray()
            };

        }

        /// <summary>
        /// Clears the configuration buffer to remove lingering sensitive data.
        /// </summary>
        public void Clear() => CryptographicOperations.ZeroMemory(ConfigurationBuffer);

        /// <inheritdoc />
        public ReadStatusResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new ReadStatusResponse(responseApdu);
    }
}
