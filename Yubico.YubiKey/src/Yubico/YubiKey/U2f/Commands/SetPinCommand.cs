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
    /// Sets the PIN or changes the PIN to a new value.
    /// </summary>
    /// <remarks>
    /// This command is only available on the YubiKey FIPS series.
    /// </remarks>
    public sealed class SetPinCommand : IYubiKeyCommand<SetPinResponse>
    {
        private const byte Ctap1MessageInstruction = 0x03;
        private const byte SetPinInstruction = 0x44;

        /// <summary>
        /// The PIN needed to perform U2F operations on a FIPS YubiKey. If this is
        /// empty, then the caller expects that there is no PIN yet set.
        /// </summary>
        /// <remarks>
        /// If there is a PIN, it must be from 6 to 32 bytes long (inclusive). It
        /// is binary data. This command class will use whatever PIN you supply,
        /// so if it is an incorrect length, you will get the error when trying
        /// to execute the command.
        /// <para>
        /// This class will copy a reference to the PIN provided. Do not
        /// overwrite the data until after the command has executed. After it has
        /// executed, overwrite the buffer for security reasons.
        /// </para>
        /// <para>
        /// If there is no current PIN (this command is being called to set the
        /// PIN for the first time), there is no need to set this property.
        /// </para>
        /// </remarks>
        public ReadOnlyMemory<byte> CurrentPin { get; set; }

        /// <summary>
        /// The PIN that will replace the current PIN.
        /// </summary>
        /// <remarks>
        /// The PIN must be from 6 to 32 bytes long (inclusive). This command
        /// class will use whatever PIN you supply, so if it is an incorrect
        /// length, you will get the error when trying to execute the command.
        /// <para>
        /// It is binary data. It is not possible to pass in an Empty PIN
        /// (changing a YubiKey from PIN required to no PIN). Once a PIN is set,
        /// the U2F application on that YubiKey must always have a PIN. The only
        /// way to remove a PIN is to reset the application.
        /// </para>
        /// <para>
        /// This class will copy a reference to the PIN provided. Do not
        /// overwrite the data until after the command has executed. After it has
        /// executed, overwrite the buffer for security reasons.
        /// </para>
        /// </remarks>
        public ReadOnlyMemory<byte> NewPin { get; set; }

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        /// <see cref="YubiKeyApplication.FidoU2f"/>
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.FidoU2f;

        /// <summary>
        /// Constructs an instance of the <see cref="SetPinCommand" /> class.
        /// </summary>
        /// <remarks>
        /// This constructor is provided for those developers who want to use the
        /// object initializer pattern. For example, to set the PIN on a YubiKey
        /// with no current PIN:
        /// <code>
        ///   var command = new SetPinCommand()
        ///   {
        ///       NewPin = somePinValue;
        ///   };
        /// </code>
        /// </remarks>
        private SetPinCommand()
        {
            CurrentPin = ReadOnlyMemory<byte>.Empty;
            NewPin = ReadOnlyMemory<byte>.Empty;
        }

        /// <summary>
        /// Constructs an instance of the <see cref="SetPinCommand" /> class.
        /// </summary>
        /// <param name="currentPin">
        /// The PIN currently required to use the U2F application on this
        /// YubiKey, represented as bytes. If there is no current PIN, pass in an
        /// Empty value.
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
        public SetPinResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new SetPinResponse(responseApdu);
    }
}
