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
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Begin the process of getting information on all the credentials
    /// associated with a specific relying party stored on the YubiKey.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="EnumerateCredentialsBeginResponse"/>.
    /// <para>
    /// This returns the total number of credentials for the given relying party
    /// on the YubiKey along with information on the "first" credential. If there
    /// is only one credential, then you have all the information you need. If
    /// there are more credentials, then you can get information on all of them
    /// by calling the <c>enumerateCredentialsGetNextRP</c> subcommand.
    /// </para>
    /// <para>
    /// Note that if there are no credentials associated with the given relying
    /// party, the response will be "No Data" (Status = ResponseStatus.NoData,
    /// and CtapStatus = CtapStatus.NoCredentials). In this case, calling the
    /// <c>response.GetData()</c> method will result in an exception.
    /// </para>
    /// <para>
    /// The return from this command consist of the <c>user</c>,
    /// <c>credentialId</c>, <c>publicKey</c>, <c>credProtect</c>,
    /// <c>largeBlobKey</c>, and <c>credentialCount</c>.
    /// </para>
    /// </remarks>
    public class EnumerateCredentialsBeginCommand : CredentialMgmtSubCommand, IYubiKeyCommand<EnumerateCredentialsBeginResponse>
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
        /// <param name="relyingParty">
        ///     The relying party for which the credential enumeration is requested.
        /// </param>
        /// <param name="pinUvAuthToken">
        ///     The PIN/UV Auth Token built from the PIN. This is the encrypted token
        ///     key.
        /// </param>
        /// <param name="authProtocol">
        ///     The Auth Protocol used to build the Auth Token.
        /// </param>
        public EnumerateCredentialsBeginCommand(
            RelyingParty relyingParty,
            ReadOnlyMemory<byte> pinUvAuthToken,
            PinUvAuthProtocolBase authProtocol)
            : base(new CredentialManagementCommand(
            SubCmdEnumerateCredsBegin, EncodeParams(relyingParty), pinUvAuthToken, authProtocol))
        {
        }

        /// <summary>
        /// Constructs a new instance of <see cref="EnumerateCredentialsBeginCommand"/> with a pre-computed PIN/UV auth param.
        /// </summary>
        /// <param name="relyingParty">
        ///     The relying party for which the credential enumeration is requested.
        /// </param>
        /// <param name="pinUvAuthParam">
        ///     The pre-computed PIN/UV auth param for this command.
        /// </param>
        /// <param name="protocol">
        ///     The PIN/UV protocol version used to compute the auth param.
        /// </param>
        public EnumerateCredentialsBeginCommand(
            RelyingParty relyingParty,
            ReadOnlyMemory<byte> pinUvAuthParam,
            PinUvAuthProtocol protocol)
            : base(new CredentialManagementCommand(
            SubCmdEnumerateCredsBegin, EncodeParams(relyingParty), pinUvAuthParam, protocol))
        {
        }

        /// <inheritdoc />
        public EnumerateCredentialsBeginResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new EnumerateCredentialsBeginResponse(responseApdu);

        /// <summary>
        /// Creates the authentication message for this command, consisting of the subcommand byte plus encoded parameters.
        /// </summary>
        /// <param name="relyingParty">
        /// The relying party for which the credential enumeration is requested.
        /// </param>
        /// <returns>
        /// The message to be used for PIN/UV authentication.
        /// </returns>
        public static byte[] GetAuthenticationMessage(RelyingParty relyingParty)
        {
            byte[] encodedParams = EncodeParams(relyingParty);
            byte[] message = new byte[1 + encodedParams.Length];
            message[0] = SubCmdEnumerateCredsBegin;
            encodedParams.CopyTo(message, 1);
            return message;
        }

        // This method encodes the parameters. For
        // EnumerateCredentialsBeginCommand, the parameters consist of only the
        // rpIdHash, and it is encoded as
        //   map
        //     01 byteString
        private static byte[] EncodeParams(RelyingParty relyingParty)
        {
            if (relyingParty is null)
            {
                throw new ArgumentNullException(nameof(relyingParty));
            }

            return new CborMapWriter<int>()
                .Entry(KeyRpIdHash, relyingParty.RelyingPartyIdHash)
                .Encode();
        }
    }
}
