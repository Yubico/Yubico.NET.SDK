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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Otp.Commands
{
    /// <summary>
    /// Applies a configuration to one of the two configurable NDEF slots. Note that only the primary
    /// NDEF slot (Slot.ShortPress) is accessible through NFC.
    /// </summary>
    public class ConfigureNdefCommand : IYubiKeyCommand<ReadStatusResponse>
    {
        private const int NdefConfigSize = 62;
        private const int AccessCodeOffset = 56;

        private readonly Slot _ndefSlot;
        private readonly byte[] _configurationBuffer;

        /// <summary>
        /// The required size for the AccessCode buffer.
        /// </summary>
        public const int AccessCodeLength = 6;

        /// <inheritdoc />
        public YubiKeyApplication Application => YubiKeyApplication.Otp;

        /// <summary>
        /// Constructs an instance of the <see cref="ConfigureNdefCommand"/> class.
        /// </summary>
        /// <param name="slot">
        /// The slot to which the configuration should apply. The <see cref="Slot.ShortPress"/> slot
        /// corresponds to the primary NDEF configuration.
        /// </param>
        /// <param name="configuration">
        /// The configuration data for the slot. Use the <see cref="NdefConfig"/> class and methods
        /// to generate this data.
        /// </param>
        public ConfigureNdefCommand(Slot slot, ReadOnlySpan<byte> configuration) :
            this(slot, configuration, ReadOnlySpan<byte>.Empty)
        {

        }

        /// <summary>
        /// Constructs an instance of the <see cref="ConfigureNdefCommand"/> class for a slot which
        /// is protected by an access code.
        /// </summary>
        /// <param name="slot">
        /// The slot to which the configuration should apply. The <see cref="Slot.ShortPress"/> slot
        /// corresponds to the primary NDEF configuration.
        /// </param>
        /// <param name="configuration">
        /// The configuration data for the slot. Use the <see cref="NdefConfig"/> class and methods
        /// to generate this data.
        /// </param>
        /// <param name="accessCode">The access code protecting the slot.</param>
        /// <remarks>
        /// YubiKey 5 NFC devices with firmware versions 5.0.0 to 5.2.6 and 5.3.0 to 5.3.1 are affected
        /// by [YSA-2020-04](https://www.yubico.com/support/security-advisories/ysa-2020-04/). Devices
        /// with this firmware will not verify access codes on NDEF slots correctly. Please read the
        /// security advisory for more details.
        /// </remarks>
        public ConfigureNdefCommand(Slot slot, ReadOnlySpan<byte> configuration, ReadOnlySpan<byte> accessCode)
        {
            _ndefSlot = slot;

            _configurationBuffer = new byte[NdefConfigSize];

            if (configuration.Length != NdefConfigSize)
            {
                throw new ArgumentException(ExceptionMessages.InvalidNdefConfig, nameof(configuration));
            }

            configuration.CopyTo(_configurationBuffer.AsSpan());

            if (accessCode.Length > 0)
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

                accessCode.CopyTo(_configurationBuffer.AsSpan(AccessCodeOffset, AccessCodeLength));
            }
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => new CommandApdu()
        {
            Ins = OtpConstants.RequestSlotInstruction,
            P1 =
                _ndefSlot == Slot.ShortPress
                ? OtpConstants.ProgramNDEFShortPress
                : OtpConstants.ProgramNDEFLongPress,
            Data = _configurationBuffer
        };

        /// <inheritdoc />
        public ReadStatusResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new ReadStatusResponse(responseApdu);
    }
}
