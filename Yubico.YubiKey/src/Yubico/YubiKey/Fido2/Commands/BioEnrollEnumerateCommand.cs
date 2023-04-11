// Copyright 2023 Yubico AB
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
    /// Enumerate the enrolled fingerprints. This is a subcommand of the CTAP
    /// command "authenticatorBioEnrollment".
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="BioEnrollEnumerateResponse"/>.
    /// </remarks>
    public sealed class BioEnrollEnumerateCommand : IYubiKeyCommand<BioEnrollEnumerateResponse>
    {
        private const int SubCmdEnumerateEnroll = 0x04;

        private readonly BioEnrollmentCommand _command;

        /// <inheritdoc />
        public YubiKeyApplication Application => _command.Application;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        public BioEnrollEnumerateCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs an instance of the <see cref="BioEnrollEnumerateCommand" /> class.
        /// </summary>
        /// <param name="pinUvAuthToken">
        /// The PIN/UV Auth Token built from the PIN. This is the encrypted token
        /// key.
        /// </param>
        /// <param name="authProtocol">
        /// The Auth Protocol used to build the Auth Token.
        /// </param>
        public BioEnrollEnumerateCommand(ReadOnlyMemory<byte> pinUvAuthToken, PinUvAuthProtocolBase authProtocol)
        {
            _command = new BioEnrollmentCommand(SubCmdEnumerateEnroll, null, pinUvAuthToken, authProtocol);
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

        /// <inheritdoc />
        public BioEnrollEnumerateResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new BioEnrollEnumerateResponse(responseApdu);
    }
}
