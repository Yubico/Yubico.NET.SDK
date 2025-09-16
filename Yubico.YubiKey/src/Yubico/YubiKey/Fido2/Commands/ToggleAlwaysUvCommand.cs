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
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Toggle the "alwaysUv" feature. That is, if it is set to <c>true</c>, set
    /// it to <c>false</c>, and if it is <c>false</c>, set it to <c>true</c>.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="Fido2Response"/>. This command
    /// does not return any data, it only returns "success" or "failure", and has
    /// some FIDO2-specific error information.
    /// <para>
    /// This sets both the "alwaysUv" and "makeCredUvNotReqd" options.
    /// </para>
    /// </remarks>
    public class ToggleAlwaysUvCommand : IYubiKeyCommand<Fido2Response>
    {
        private const int SubCmdToggleAlwaysUv = 0x02;

        private readonly ConfigCommand _command;

        /// <inheritdoc />
        public YubiKeyApplication Application => _command.Application;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private ToggleAlwaysUvCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs a new instance of <see cref="ToggleAlwaysUvCommand"/>.
        /// </summary>
        /// <param name="pinUvAuthToken">
        /// The PIN/UV Auth Token built from the PIN. This is the encrypted token
        /// key.
        /// </param>
        /// <param name="authProtocol">
        /// The Auth Protocol used to build the Auth Token.
        /// </param>
        public ToggleAlwaysUvCommand(
            ReadOnlyMemory<byte> pinUvAuthToken,
            PinUvAuthProtocolBase authProtocol)
        {
            _command = new ConfigCommand(
                SubCmdToggleAlwaysUv, null, pinUvAuthToken, authProtocol);
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

        /// <inheritdoc />
        public Fido2Response CreateResponseForApdu(ResponseApdu responseApdu) =>
            new ConfigResponse(responseApdu);
    }
}
