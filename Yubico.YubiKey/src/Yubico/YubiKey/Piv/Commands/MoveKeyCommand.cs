// Copyright 2024 Yubico AB
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

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// The <see cref="MoveKeyCommand"/> is used to move a PIV key from one slot to another.
    /// The source slot must not be the <see cref="PivSlot.Attestation"/>-slot and the destination slot must be empty.
    /// </summary>
    public class MoveKeyCommand : IYubiKeyCommand<MoveKeyResponse>
    {
        /// <summary>
        /// The Yubikey slot of the key you want to move. This must be a valid slot number.
        /// </summary>
        public byte SourceSlot { get; set; }

        /// <summary>
        /// The target Yubikey slot for the key you want to move. This must be a valid slot number.
        /// </summary>
        public byte DestinationSlot { get; set; }

        private const byte MoveOrDeleteInstruction = 0xF6;

        /// <summary>
        /// Constructor for the <see cref="MoveKeyCommand"/> which is used to move a PIV key from one slot to another.
        /// The source slot must not be the <see cref="PivSlot.Attestation"/>-slot and the destination slot must be empty.
        /// </summary>
        /// <param name="sourceSlot">The Yubikey slot of the key you want to move. This must be a valid slot number.</param>
        /// <param name="destinationSlot">The target Yubikey slot for the key you want to move. This must be a valid slot number.</param>
        public MoveKeyCommand(byte sourceSlot, byte destinationSlot)
        {
            SourceSlot = sourceSlot;
            DestinationSlot = destinationSlot;
        }

        /// <summary>
        /// Constructor for the <see cref="MoveKeyCommand"/> which is used to move a PIV key from one slot to another.
        /// The source slot must not be the <see cref="PivSlot.Attestation"/>-slot and the destination slot must be empty.
        /// </summary>
        public MoveKeyCommand()
        {

        }

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs. For this
        /// command it's PIV.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Piv
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Piv;

        /// <summary>
        /// This will create and validate the <see cref="CommandApdu"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">An exception will be thrown upon invalid slot usage.
        /// Either one of the slots were the <see cref="PivSlot.Attestation"/> or the source and destination slot were the same.</exception>
        /// <returns>The <see cref="CommandApdu"/> that targets the Move-operation with the correct parameters</returns>
        public CommandApdu CreateCommandApdu()
        {
            ValidateSlots(SourceSlot, DestinationSlot);

            return new CommandApdu
            {
                Ins = MoveOrDeleteInstruction,
                P1 = DestinationSlot,
                P2 = SourceSlot,
            };
        }

        private static void ValidateSlots(byte sourceSlot, byte destinationSlot)
        {
            if (sourceSlot == destinationSlot)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidSlotsSameSourceAndDestinationSlotsCannotBeTheSame));
            }

            ValidateSlot(sourceSlot);
            ValidateSlot(destinationSlot);
        }

        private static void ValidateSlot(byte slot)
        {
            if (slot == PivSlot.Attestation)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidSlot,
                        slot));
            }
        }

        /// <summary>
        /// Creates the <see cref="MoveKeyResponse"/> from the <see cref="ResponseApdu"/> data.
        /// </summary>
        /// <param name="responseApdu">The return data with which the Yubikey responded
        /// to the <see cref="MoveKeyCommand"/></param>
        /// <returns>
        /// The <see cref="MoveKeyResponse"/> for the <see cref="MoveKeyCommand"/>
        /// </returns>
        public MoveKeyResponse CreateResponseForApdu(ResponseApdu responseApdu) => new MoveKeyResponse(responseApdu);
    }
}
