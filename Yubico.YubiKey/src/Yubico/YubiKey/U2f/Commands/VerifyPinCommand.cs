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
    /// Verifies a user-supplied PIN.
    /// </summary>
    public sealed class VerifyPinCommand : IYubiKeyCommand<U2fResponse>
    {
        private const byte Ctap1MessageInstruction = 0x03;
        private const byte VerifyPinInstruction = 0x43;
        private const int MinimumPinLength = 6;
        private const int MaximumPinLength = 32;

        private ReadOnlyMemory<byte> _pin;

        public ReadOnlyMemory<byte> Pin
        {
            get => _pin;

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

                _pin = value;
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
        private VerifyPinCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs an instance of the <see cref="VerifyPinCommand" /> class.
        /// </summary>
        /// <param name="pin">
        /// The PIN to verify, represented as bytes.
        /// </param>
        public VerifyPinCommand(ReadOnlyMemory<byte> pin)
        {
            Pin = pin;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            var innerCommand = new CommandApdu()
            {
                Ins = VerifyPinInstruction,
                Data = Pin.ToArray(),
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
