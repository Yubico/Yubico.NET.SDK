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
using Yubico.YubiKey.Fido2.PinProtocols;
using Yubico.YubiKey.Fido2.Cbor;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Begin the process of getting information on all the credentials
    /// associated with a specific relying party stored on the YubiKey.
    /// </summary>
    /// <remarks>
    /// This returns information on one of the credentials, and the total number
    /// of credentials on the YubiKey. If there is only one credential, then you
    /// have all the information you need. If there are more credentials, then
    /// you can get information on all of them by calling the
    /// <c>enumerateCredentialsGetNextRP</c> sub-command.
    /// <para>
    /// The return from this command is the <c>CredentialManagementData</c>
    /// class, but only six of the elements are included: <c>user</c>,
    /// <c>credentialId</c>, <c>publicKey</c>, <c>credProtect</c>,
    /// <c>largeBlobKey</c>, and <c>totalCredentials</c>.
    /// </para>
    /// </remarks>
    public class EnumerateCredentialsBeginCommand : CredentialManagementCommand
    {
        private const int SubCmdEnumerateCredsBegin = 0x04;
        private const int KeyRpIdHash = 1;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private EnumerateCredentialsBeginCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs a new instance of <see cref="EnumerateCredentialsBeginCommand"/>.
        /// </summary>
        /// <param name="relyingPartyIdHash">
        /// The digest of the relying party ID, for which the credential
        /// enumeration is requested.
        /// </param>
        /// <param name="pinUvAuthToken">
        /// The PIN/UV Auth Token built from the PIN. This is the encrypted token
        /// key.
        /// </param>
        /// <param name="authProtocol">
        /// The Auth Protocol used to build the Auth Token.
        /// </param>
        public EnumerateCredentialsBeginCommand(
            ReadOnlyMemory<byte> relyingPartyIdHash,
            ReadOnlyMemory<byte> pinUvAuthToken,
            PinUvAuthProtocolBase authProtocol)
            : base(SubCmdEnumerateCredsBegin, EncodeParams(relyingPartyIdHash), pinUvAuthToken, authProtocol)
        {
        }

        // This method encodes the parameters. For
        // EnumerateCredentialsBeginCommand, the parameters consist of only the
        // rpIdHash, and it is encoded as
        //   map
        //     01 byteString
        private static byte[] EncodeParams(ReadOnlyMemory<byte> relyingPartyIdHash)
        {
            return new CborMapWriter<int>()
                .Entry(KeyRpIdHash, relyingPartyIdHash)
                .Encode();
        }
    }
}
