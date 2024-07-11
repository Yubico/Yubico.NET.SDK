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
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Update the user information for a credential stored on the YubiKey.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="Fido2Response"/>. This command
    /// does not return any data, it only returns "success" or "failure".
    /// <para>
    /// This replaces the user information currently on the YubiKey. That is, you
    /// don't "edit" an entry. You generally will get the current user info, then
    /// create an entirely new <see cref="Yubico.YubiKey.Fido2.UserEntity"/>,
    /// copying any information from the previous object you want to retain, and
    /// setting any new information. Then call this command with the new object.
    /// </para>
    /// <para>
    /// Note that this feature is available only to YubiKeys that support
    /// "credMgmt". It is not available to those that support only
    /// "CredentialMgmtPreview". It is not a subclass of
    /// <c>CredentialMgmtSubCommand</c> and hence does not possess the property
    /// <c>IsPreview</c>.
    /// </para>
    /// </remarks>
    public class UpdateUserInfoCommand : IYubiKeyCommand<Fido2Response>
    {
        private const int SubCmdUpdateUserInfo = 0x07;
        private const int KeyCredentialId = 2;
        private const int KeyUserEntity = 3;

        private readonly CredentialManagementCommand _command;

        /// <inheritdoc />
        public YubiKeyApplication Application => _command.Application;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private UpdateUserInfoCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs a new instance of <see cref="UpdateUserInfoCommand"/>.
        /// </summary>
        /// <param name="credentialId">
        /// The <c>CredentialId</c> of the credential with the user info to
        /// update.
        /// </param>
        /// <param name="userEntity">
        /// The <c>UserEntity</c> containing the new user info to be stored on
        /// the YubiKey.
        /// </param>
        /// <param name="pinUvAuthToken">
        /// The PIN/UV Auth Token built from the PIN. This is the encrypted token
        /// key.
        /// </param>
        /// <param name="authProtocol">
        /// The Auth Protocol used to build the Auth Token.
        /// </param>
        public UpdateUserInfoCommand(
            CredentialId credentialId,
            UserEntity userEntity,
            ReadOnlyMemory<byte> pinUvAuthToken,
            PinUvAuthProtocolBase authProtocol)
        {
            _command = new CredentialManagementCommand(
                SubCmdUpdateUserInfo, EncodeParams(credentialId, userEntity), pinUvAuthToken, authProtocol);
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

        /// <inheritdoc />
        public Fido2Response CreateResponseForApdu(ResponseApdu responseApdu) =>
            new Fido2Response(responseApdu);

        // This method encodes the parameters. For
        // UpdateUserInfoCommand, the parameters consist of the
        // credentialId and the new user info represented as an instance of a
        // UserEntity object. It is encoded as
        //   map
        //     02 encoding of credentialID
        //     03 encoding of userEntity
        private static byte[] EncodeParams(CredentialId credentialId, UserEntity userEntity)
        {
            return new CborMapWriter<int>()
                .Entry(KeyCredentialId, credentialId)
                .Entry(KeyUserEntity, userEntity)
                .Encode();
        }
    }
}
