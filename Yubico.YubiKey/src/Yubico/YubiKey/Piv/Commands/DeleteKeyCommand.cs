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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// The <see cref="DeleteKeyCommand"/> is used to Delete a PIV key from the target slot
    /// <remarks>
    /// Any key, including the attestation key can be deleted.
    /// </remarks>
    /// </summary>
    public class DeleteKeyCommand : IYubiKeyCommand<DeleteKeyResponse>
    {
        /// <summary>
        /// The Yubikey slot of the key you want to delete.
        /// </summary>
        public byte SlotToClear { get; set; }

        private const byte MoveOrDeleteInstruction = 0xF6;

        /// <summary>
        /// Constructor for the <see cref="DeleteKeyCommand"/> which is used to delete a PIV key from a slot.
        /// </summary>
        /// <param name="slotToClear">The Yubikey slot of the key you want to clear.</param>
        public DeleteKeyCommand(byte slotToClear)
        {
            SlotToClear = slotToClear;
        }

        /// <summary>
        /// Constructor for the <see cref="DeleteKeyCommand"/> which is used to delete a PIV key from a slot.
        /// </summary>
        public DeleteKeyCommand() { }

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs. For this command it's PIV.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Piv
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Piv;

        /// <summary>
        /// Constructs a <see cref="CommandApdu"/> for the Delete-operation.
        /// </summary>
        /// <returns>
        /// The <see cref="CommandApdu"/> that targets the Delete-operation with the correct parameters.
        /// </returns>
        public CommandApdu CreateCommandApdu() =>
            new CommandApdu
            {
                Ins = MoveOrDeleteInstruction,
                P1 = 0xFF, // Will be cleared
                P2 = SlotToClear
            };

        /// <summary>
        /// Creates the <see cref="DeleteKeyResponse"/> from the <see cref="ResponseApdu"/> data.
        /// </summary>
        /// <param name="responseApdu">The return data with which the Yubikey responded to the
        /// <see cref="DeleteKeyCommand"/>
        /// </param>
        /// <returns>
        /// The <see cref="DeleteKeyResponse"/> for the <see cref="DeleteKeyCommand"/>
        /// </returns>
        public DeleteKeyResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new DeleteKeyResponse(responseApdu);
    }
}
