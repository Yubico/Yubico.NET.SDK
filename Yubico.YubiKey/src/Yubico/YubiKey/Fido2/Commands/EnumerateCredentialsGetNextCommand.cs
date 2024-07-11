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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Continue the process of getting information on all the credentials
    /// associated with a specific relying party stored on the YubiKey.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="EnumerateCredentialsGetNextResponse"/>.
    /// <para>
    /// This returns information on one of the credentials. If there is only one
    /// credential associated with the relying party, then the call to the
    /// <c>enumerateCredentialsBegin</c> subcommand gave you all the information
    /// you need. It also indicated that there was only one credential. If there
    /// are more credentials, then you can get information on the rest after the
    /// first by calling this subcommand, calling it once for every credential.
    /// </para>
    /// <para>
    /// The return from this command consist of the <c>user</c>,
    /// <c>credentialId</c>, <c>publicKey</c>, <c>credProtect</c>, and
    /// <c>largeBlobKey</c>.
    /// </para>
    /// <para>
    /// Note that this command does not need the <c>relyingPartyIdHash</c>,
    /// <c>pinUvAuthToken</c> nor the <c>authProtocol</c>. This command picks up
    /// where the <see cref="EnumerateCredentialsBeginCommand"/> left off, and
    /// can only operate successfully after that "Begin" command has successfully
    /// completed.
    /// </para>
    /// </remarks>
    public class EnumerateCredentialsGetNextCommand : CredentialMgmtSubCommand,
                                                      IYubiKeyCommand<EnumerateCredentialsGetNextResponse>
    {
        private const int SubCmdGetEnumerateCredsGetNext = 0x05;

        /// <summary>
        /// Constructs a new instance of
        /// <see cref="EnumerateCredentialsGetNextCommand"/>.
        /// </summary>
        public EnumerateCredentialsGetNextCommand()
            : base(new CredentialManagementCommand(SubCmdGetEnumerateCredsGetNext))
        {
        }

        /// <inheritdoc />
        public EnumerateCredentialsGetNextResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new EnumerateCredentialsGetNextResponse(responseApdu);
    }
}
