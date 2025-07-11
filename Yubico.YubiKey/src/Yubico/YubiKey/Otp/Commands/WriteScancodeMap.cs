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
using System.Globalization;
using Yubico.Core.Devices.Hid;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Otp.Commands
{
    /// <summary>
    /// Updates the scancode mapping used for Yubico OTP and the Yubico OTP based password generator.
    /// </summary>
    public class WriteScancodeMap : IYubiKeyCommand<ReadStatusResponse>
    {
        private const byte RequestSlotInstruction = 0x01;
        private const byte WriteScancodeMapSlot = 0x12;
        // Note that this is not ModHex, but it's what yubico_personalization
        // uses to write the map.
        private static string _defaultScancodeMap => "cbdefghijklnrtuvCBDEFGHIJKLNRTUV0123456789!\t\n";
        private static readonly int _scancodeMapLength = _defaultScancodeMap.Length;

        private ReadOnlyMemory<byte> _scancodeMap = Array.Empty<byte>();

        /// <summary>
        /// The default HID usage map.
        /// </summary>
        public static ReadOnlyMemory<byte> DefaultScancodeMap =>
            HidCodeTranslator.GetInstance(KeyboardLayout.en_US).GetHidCodes(_defaultScancodeMap);

        /// <inheritdoc />
        public YubiKeyApplication Application => YubiKeyApplication.Otp;

        /// <summary>
        /// The scancode map to be written when the command is sent to the YubiKey.
        /// </summary>
        public ReadOnlyMemory<byte> ScancodeMap
        {
            get => _scancodeMap;
            set
            {
                if (value.Length != _scancodeMapLength)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.WrongHidCodeMapLength,
                            _scancodeMapLength,
                            value.Length),
                            nameof(value));
                }

                _scancodeMap = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteScancodeMap"/> class. The scancode map
        /// will be initialized to the default, until it is overridden by the caller.
        /// </summary>
        public WriteScancodeMap() :
            this(DefaultScancodeMap)
        {

        }

        /// <summary>
        /// Initializing a new instance of the <see cref="WriteScancodeMap"/> class with a custom
        /// scancode map.
        /// </summary>
        /// <param name="scancodeMap">The scancode map to program on the YubiKey.</param>
        public WriteScancodeMap(ReadOnlyMemory<byte> scancodeMap)
        {
            ScancodeMap = scancodeMap;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() =>
            new CommandApdu()
            {
                Ins = RequestSlotInstruction,
                P1 = WriteScancodeMapSlot,
                Data = _scancodeMap.ToArray()
            };

        /// <inheritdoc />
        public ReadStatusResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new ReadStatusResponse(responseApdu);
    }
}
