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
    /// Get the YubiKey's FIDO2 credential metadata.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="CredentialManagementResponse"/>.
    /// <para>
    /// This returns metadata on all the credentials. The return from this
    /// command consists of the <c>existingResidentCredentialsCount</c> and
    /// <c>maxPossibleRemainingResidentCredentialsCount</c> (section 6.8 of the
    /// standard).
    /// </para>
    /// </remarks>
    public class GetCredentialMetadataCommand : IYubiKeyCommand<GetCredentialMetadataResponse>
    {
        private const int SubCmdGetMetadata = 0x01;

        private readonly CredentialManagementCommand _command;

        /// <inheritdoc />
        public YubiKeyApplication Application => _command.Application;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private GetCredentialMetadataCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs a new instance of <see cref="GetCredentialMetadataCommand"/>.
        /// </summary>
        /// <param name="pinUvAuthToken">
        /// The PIN/UV Auth Token built from the PIN. This is the encrypted token
        /// key.
        /// </param>
        /// <param name="authProtocol">
        /// The Auth Protocol used to build the Auth Token.
        /// </param>
        public GetCredentialMetadataCommand(
            ReadOnlyMemory<byte> pinUvAuthToken, PinUvAuthProtocolBase authProtocol)
        {
            _command = new CredentialManagementCommand(SubCmdGetMetadata, null, pinUvAuthToken, authProtocol);
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

        /// <inheritdoc />
        public GetCredentialMetadataResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetCredentialMetadataResponse(responseApdu);
    }
}
