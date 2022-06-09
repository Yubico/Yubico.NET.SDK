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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands
{
    /// <summary>
    /// Sets the new PIN.
    /// </summary>
    /// <remarks>
    /// This command is only available on the YubiKey FIPS series.
    /// </remarks>
    public sealed class SetPinCommand : IYubiKeyCommand<U2fResponse>
    {
        private const byte Ctap1MessageInstruction = 0x03;
        private const byte SetPinInstruction = 0x44;
        private const int MinimumPinLength = 6;
        private const int MaximumPinLength = 32;

        private ReadOnlyMemory<byte> _currentPin;
        private ReadOnlyMemory<byte> _newPin;

        public ReadOnlyMemory<byte> CurrentPin
        {
            get => _currentPin;

            set
            {
                if (value.IsEmpty)
                {
                    throw new ArgumentException(ExceptionMessages.InvalidPin);
                }

                if ((value.Length < MinimumPinLength) || (value.Length > MaximumPinLength))
                {
                    throw new ArgumentException(ExceptionMessages.InvalidPinLength);
                }

                _currentPin = value;
            }
        }

        public ReadOnlyMemory<byte> NewPin
        {
            get => _newPin;

            set
            {
                if (value.IsEmpty)
                {
                    throw new ArgumentException(ExceptionMessages.InvalidPin);
                }

                if ((value.Length < MinimumPinLength) || (value.Length > MaximumPinLength))
                {
                    throw new ArgumentException(ExceptionMessages.InvalidPinLength);
                }

                _newPin = value;
            }
        }
        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        /// <see cref="YubiKeyApplication.FidoU2f"/>
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.FidoU2f;

        // We explicitly do not want a default constructor for this command.
        private SetPinCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs an instance of the <see cref="SetPinCommand" /> class.
        /// </summary>
        /// <param name="currentPin">
        /// The current PIN set, represented as bytes.
        /// </param>
        /// <param name="newPin">
        /// The new PIN to set, represented as bytes.
        /// </param>
        public SetPinCommand(ReadOnlyMemory<byte> currentPin, ReadOnlyMemory<byte> newPin)
        {
            CurrentPin = currentPin;
            NewPin = newPin;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            byte[] payload = new byte[1 + CurrentPin.Length + NewPin.Length];

            payload[0] = (byte)NewPin.Length;
            Array.Copy(CurrentPin.ToArray(), 0, payload, 1, CurrentPin.Length);
            Array.Copy(NewPin.ToArray(), 0, payload, CurrentPin.Length + 1, NewPin.Length);

            var innerCommand = new CommandApdu()
            {
                Ins = SetPinInstruction,
                Data = payload,
            };

            return new CommandApdu()
            {
                Ins = Ctap1MessageInstruction,
                Data = innerCommand.AsByteArray(ApduEncoding.ExtendedLength),
            };
        }

        /// <inheritdoc />
        public U2fResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new U2fResponse(responseApdu);
    }
}
