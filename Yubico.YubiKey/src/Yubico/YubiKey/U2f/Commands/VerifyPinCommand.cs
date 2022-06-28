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

namespace Yubico.YubiKey.U2f.Commands
{
    /// <summary>
    /// Verifies a user-supplied PIN.
    /// </summary>
    /// <remarks>
    /// Some U2F documentation may describe this as "unlocking" the U2F
    /// application.
    /// </remarks>
    public sealed class VerifyPinCommand : IYubiKeyCommand<VerifyPinResponse>
    {
        private const byte Ctap1MessageInstruction = 0x03;
        private const byte VerifyPinInstruction = 0x43;

        /// <summary>
        /// The PIN needed to perform U2F operations on a FIPS YubiKey.
        /// </summary>
        /// <remarks>
        /// The PIN must be from 6 to 32 bytes long (inclusive). It is binary
        /// data. This command class will use whatever PIN you supply, so if it
        /// is an incorrect length, you will get the error when trying to
        /// execute tht command.
        /// <para>
        /// This class will copy a reference to the PIN provided. Do not
        /// overwrite the data until after the command has executed. After it has
        /// executed, overwrite the buffer for security reasons.
        /// </para>
        /// </remarks>
        public ReadOnlyMemory<byte> Pin { get; set; }

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        /// <see cref="YubiKeyApplication.FidoU2f"/>
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.FidoU2f;

        /// <summary>
        /// Constructs an instance of the <see cref="VerifyPinCommand" /> class.
        /// </summary>
        /// <remarks>
        /// This constructor is provided for those developers who want to use the
        /// object initializer pattern. For example,:
        /// <code>
        ///   var command = new VerifyPinCommand()
        ///   {
        ///       Pin = somePinValue;
        ///   };
        /// </code>
        /// </remarks>
        private VerifyPinCommand()
        {
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
                Data = Pin,
            };

            return new CommandApdu()
            {
                Ins = Ctap1MessageInstruction,
                Data = innerCommand.AsByteArray(ApduEncoding.ExtendedLength),
            };
        }

        /// <inheritdoc />
        public VerifyPinResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new VerifyPinResponse(responseApdu);
    }
}
