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
using System.Text;
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Oath.Commands
{
    /// <summary>
    /// Deletes an existing credential.
    /// </summary>
    public class DeleteCommand : IYubiKeyCommand<DeleteResponse>
    {
        private const byte DeleteInstruction = 0x02;
        private const byte NameTag = 0x71;

        /// <summary>
        /// The credential to delete.
        /// </summary>
        public Credential? Credential { get; set; }

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Oath
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Oath;

        /// <summary>
        /// Constructs an instance of the <see cref="DeleteCommand" /> class.
        /// </summary>
        public DeleteCommand()
        {
        }

        /// <summary>
        /// Constructs an instance of the <see cref="DeleteCommand" /> class.
        /// </summary>
        /// <param name="credential">
        /// The credential to delete.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The credential is null.
        /// </exception>
        public DeleteCommand(Credential credential)
        {
            if (credential is null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            Credential = credential;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            if (Credential is null)
            {
                throw new InvalidOperationException(ExceptionMessages.InvalidCredential);
            }

            byte[] nameBytes = Encoding.UTF8.GetBytes(Credential.Name);

            var tlvWriter = new TlvWriter();
            tlvWriter.WriteValue(NameTag, nameBytes);

            return new CommandApdu
            {
                Ins = DeleteInstruction,
                Data = tlvWriter.Encode()
            };
        }

        /// <inheritdoc />
        public DeleteResponse CreateResponseForApdu(ResponseApdu responseApdu) => new DeleteResponse(responseApdu);
    }
}
