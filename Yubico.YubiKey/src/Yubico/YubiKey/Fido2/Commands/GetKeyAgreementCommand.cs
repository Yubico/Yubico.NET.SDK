// Copyright 2022 Yubico AB
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

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Gets the YubiKey's public key for the Key Agreement algorithm based on
    /// the specified PIN/UV auth protocol.
    /// </summary>
    /// <remarks>
    /// Before sending a PIN to the YubiKey, it must be encrypted. The key used
    /// to encrypt is generated using a Key Agreement algorithm along with a key
    /// derivation function. In FIDO2, the key agreement algorithm is specified
    /// int the PIN/UV Auth Protocol. There are currently two. For each protocol
    /// the key agreement algorithm is ECDH with the P-256 curve, although they
    /// have different key derivation functions.
    /// </remarks>
    public class GetKeyAgreementCommand : IYubiKeyCommand<GetKeyAgreementResponse>
    {
        private readonly ClientPinCommand _command;

        private const int SubCmdGetKeyAgreement = 0x02;

        /// <inheritdoc />
        public YubiKeyApplication Application => _command.Application;

        /// <summary>
        /// The PIN/UV Auth Protocol for which the public key is requested.
        /// </summary>
        public PinUvAuthProtocol PinUvAuthProtocol
        {
            get => _command.PinUvAuthProtocol ?? PinUvAuthProtocol.None;
            set => _command.PinUvAuthProtocol = value;
        }

        /// <summary>
        /// Constructs a new instance of <see cref="GetKeyAgreementCommand"/>.
        /// </summary>
        /// <remarks>
        /// This command can only be executed if the PIN/UV Auth Protocol is
        /// specified. If you use this constructor, make sure you set the
        /// <see cref="PinUvAuthProtocol"/> property before sending.
        /// </remarks>
        public GetKeyAgreementCommand()
        {
            _command = new ClientPinCommand()
            {
                SubCommand = SubCmdGetKeyAgreement,
            };
        }

        /// <summary>
        /// Constructs a new instance of <see cref="GetKeyAgreementCommand"/>.
        /// </summary>
        /// <remarks>
        /// This command can only be executed if the PIN/UV Auth Protocol is
        /// specified.
        /// </remarks>
        /// <param name="protocol">
        /// Which protocol the caller will be using.
        /// </param>
        public GetKeyAgreementCommand(PinUvAuthProtocol protocol)
            : this()
        {
            PinUvAuthProtocol = protocol;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

        /// <inheritdoc />
        public GetKeyAgreementResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetKeyAgreementResponse(responseApdu);
    }
}
