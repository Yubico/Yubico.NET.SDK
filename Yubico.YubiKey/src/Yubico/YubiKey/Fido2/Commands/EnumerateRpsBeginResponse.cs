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

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// The response partner to the EnumerateRpsBeginCommand, containing the
    /// total number of relying parties represented in the YubiKey along with the
    /// "first" (index 0) relying party.
    /// </summary>
    /// <remarks>
    /// Note that if there are no credentials associated with a relying party,
    /// the return is "No Data" (Status = ResponseStatus.NoData, and
    /// CtapStatus = CtapStatus.NoCredentials). in this case, calling
    /// <c>GetData</c> will result in an exception.
    /// </remarks>
    public class EnumerateRpsBeginResponse
        : Fido2Response, IYubiKeyResponseWithData<(int totalRelyingPartyCount, RelyingParty relyingParty)>
    {
        private readonly CredentialManagementResponse _response;

        /// <summary>
        /// Constructs a new instance of
        /// <see cref="EnumerateRpsBeginResponse"/> based on a response APDU
        /// provided by the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// A response APDU containing the CBOR response data for the
        /// <c>authenticatorCredentialManagement</c> command.
        /// </param>
        public EnumerateRpsBeginResponse(ResponseApdu responseApdu)
            : base(responseApdu)
        {
            _response = new CredentialManagementResponse(responseApdu);
        }

        /// <summary>
        /// Gets the total number of relying parties and the first (index 0)
        /// relying party represented in the YubiKey.
        /// </summary>
        /// <remarks>
        /// Note that if there are no credentials associated with a relying party,
        /// the return is "No Data" (Status = ResponseStatus.NoData, and
        /// CtapStatus = CtapStatus.NoCredentials). in this case, calling
        /// <c>GetData</c> will result in an exception.
        /// </remarks>
        /// <returns>
        /// The data in the response APDU, presented as a Tuple of
        /// (int,RelyingParty).
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="YubiKeyResponse.Status"/> is not <see cref="ResponseStatus.Success"/>.
        /// </exception>
        /// <exception cref="Ctap2DataException">
        /// If the response from the YubiKey does not match the expected format.
        /// </exception>
        public (int totalRelyingPartyCount, RelyingParty relyingParty) GetData()
        {
            var credentialManagementData = _response.GetData();

            if (credentialManagementData.RelyingParty is not null
                && credentialManagementData.RelyingPartyIdHash is not null
                && credentialManagementData.TotalRelyingPartyCount is not null)
            {
                if (credentialManagementData.RelyingParty.IsMatchingRelyingPartyId(credentialManagementData.RelyingPartyIdHash.Value))
                {
                    return (credentialManagementData.TotalRelyingPartyCount.Value, credentialManagementData.RelyingParty);
                }
            }

            throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info);
        }
    }
}
