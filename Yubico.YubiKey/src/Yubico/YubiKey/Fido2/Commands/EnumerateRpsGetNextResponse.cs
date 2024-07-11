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

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// The response partner to the EnumerateRpsGetNextCommand, containing the
    /// next relying party represented in the YubiKey.
    /// </summary>
    public class EnumerateRpsGetNextResponse : Fido2Response, IYubiKeyResponseWithData<RelyingParty>
    {
        private readonly CredentialManagementResponse _response;

        /// <summary>
        /// Constructs a new instance of
        /// <see cref="EnumerateRpsGetNextResponse"/> based on a response APDU
        /// provided by the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// A response APDU containing the CBOR response data for the
        /// <c>authenticatorCredentialManagement</c> command.
        /// </param>
        public EnumerateRpsGetNextResponse(ResponseApdu responseApdu)
            : base(responseApdu)
        {
            _response = new CredentialManagementResponse(responseApdu);
        }

        /// <inheritdoc/>
        public RelyingParty GetData()
        {
            CredentialManagementData mgmtData = _response.GetData();

            if (!(mgmtData.RelyingParty is null)
                && !(mgmtData.RelyingPartyIdHash is null))
            {
                if (mgmtData.RelyingParty.IsMatchingRelyingPartyId(mgmtData.RelyingPartyIdHash.Value))
                {
                    return mgmtData.RelyingParty;
                }
            }

            throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info);
        }
    }
}
