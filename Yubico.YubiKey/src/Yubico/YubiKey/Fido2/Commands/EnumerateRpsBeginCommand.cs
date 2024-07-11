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
    /// Begin the process of getting information on all the relying parties
    /// represented in credentials on the YubiKey.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="EnumerateRpsBeginResponse"/>.
    /// <para>
    /// This returns the total number of relying parties represented in the set
    /// of credentials, along with  information on the "first" relying party. If
    /// there is only one RP, then you have all the information you need. If
    /// there are more RPs, then you can get information on all of them by
    /// calling the <c>enumerateRPsGetNextRP</c> subcommand.
    /// </para>
    /// <para>
    /// Note that if there are no credentials associated with the given relying
    /// party, the response will be "No Data"
    /// (Status = ResponseStatus.NoData, and
    /// CtapStatus = CtapStatus.NoCredentials). In this case, calling the
    /// <c>response.GetData()</c> method will result in an exception.
    /// </para>
    /// </remarks>
    public class EnumerateRpsBeginCommand : CredentialMgmtSubCommand, IYubiKeyCommand<EnumerateRpsBeginResponse>
    {
        private const int SubCmdEnumerateRpsBegin = 0x02;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private EnumerateRpsBeginCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs a new instance of <see cref="EnumerateRpsBeginCommand"/>.
        /// </summary>
        /// <param name="pinUvAuthToken">
        /// The PIN/UV Auth Token built from the PIN. This is the encrypted token
        /// key.
        /// </param>
        /// <param name="authProtocol">
        /// The Auth Protocol used to build the Auth Token.
        /// </param>
        public EnumerateRpsBeginCommand(
            ReadOnlyMemory<byte> pinUvAuthToken, PinUvAuthProtocolBase authProtocol)
            : base(new CredentialManagementCommand(SubCmdEnumerateRpsBegin, null, pinUvAuthToken, authProtocol))
        {
        }

        /// <inheritdoc />
        public EnumerateRpsBeginResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new EnumerateRpsBeginResponse(responseApdu);
    }
}
