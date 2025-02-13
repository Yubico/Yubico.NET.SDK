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
using System.Text;
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Oath.Commands
{
    /// <summary>
    /// Renames an existing credential by setting new issuer and account names.
    /// <remarks>
    /// This command is only available on the YubiKeys with firmware version 5.3.0 and later.
    /// </remarks>
    /// </summary>
    public class RenameCommand : IYubiKeyCommand<RenameResponse>
    {
        private const byte RenameInstruction = 0x05;
        private const byte NameTag = 0x71;

        /// <summary>
        /// The credential to edit.
        /// </summary>
        public Credential? Credential { get; set; }

        /// <summary>
        /// The new issuer.
        /// </summary>
        public string? NewIssuer { get; set; }

        /// <summary>
        /// The new account name.
        /// </summary>
        public string? NewAccount { get; set; }

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Oath
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Oath;

        /// <summary>
        /// Constructs an instance of the <see cref="RenameCommand" /> class.
        /// </summary>
        public RenameCommand()
        {
        }

        /// <summary>
        /// Constructs an instance of the <see cref="RenameCommand" /> class.
        /// </summary>
        /// <param name="credential">
        /// The credential to edit.
        /// </param>
        /// <param name="newIssuer">
        /// The new issuer.
        /// </param>
        /// <param name="newAccount">
        /// The new account name.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The credential to rename is null, or the new account name to set is null, empty or
        /// consists only of white-space characters.
        /// </exception>
        public RenameCommand(Credential credential, string? newIssuer, string newAccount)
        {
            if (credential is null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            if (string.IsNullOrWhiteSpace(newAccount))
            {
                throw new ArgumentNullException(nameof(newAccount));
            }

            Credential = credential;
            NewIssuer = newIssuer;
            NewAccount = newAccount;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            if (Credential is null)
            {
                throw new InvalidOperationException(ExceptionMessages.InvalidCredential);
            }

            if (string.IsNullOrWhiteSpace(NewAccount))
            {
                throw new InvalidOperationException(ExceptionMessages.InvalidCredentialAccount);
            }

            byte[] nameBytes = Encoding.UTF8.GetBytes(Credential.Name);

            var tlvWriter = new TlvWriter();
            tlvWriter.WriteValue(NameTag, nameBytes);

            var newCredential = new Credential
            {
                Issuer = NewIssuer,
                AccountName = NewAccount,
                Type = Credential.Type,
                Period = Credential.Period
            };

            byte[] newNameBytes = Encoding.UTF8.GetBytes(newCredential.Name);

            tlvWriter.WriteValue(NameTag, newNameBytes);

            return new CommandApdu
            {
                Ins = RenameInstruction,
                Data = tlvWriter.Encode()
            };
        }

        /// <inheritdoc />
        public RenameResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new RenameResponse(responseApdu);
    }
}
