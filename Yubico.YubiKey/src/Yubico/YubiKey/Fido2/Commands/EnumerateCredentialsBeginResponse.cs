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
    /// The response partner to the EnumerateCredentialsBeginCommand, containing the
    /// the total number of credentials associated with the given relying party,
    /// along with the "first" (index 0) set of credential info.
    /// </summary>
    /// <remarks>
    /// Note that if there are no credentials associated with a relying party,
    /// the return is "No Data" (Status = ResponseStatus.NoData, and
    /// CtapStatus = CtapStatus.NoCredentials). In this case, calling
    /// <c>GetData</c> will result in an exception.
    /// </remarks>
    public class EnumerateCredentialsBeginResponse
        : Fido2Response, IYubiKeyResponseWithData<(int credentialCount, CredentialUserInfo credentialUserInfo)>
    {
        private readonly CredentialManagementResponse _response;

        /// <summary>
        /// Constructs a new instance of
        /// <see cref="EnumerateCredentialsBeginResponse"/> based on a response APDU
        /// provided by the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// A response APDU containing the CBOR response data for the
        /// <c>authenticatorCredentialManagement</c> command.
        /// </param>
        public EnumerateCredentialsBeginResponse(ResponseApdu responseApdu)
            : base(responseApdu)
        {
            _response = new CredentialManagementResponse(responseApdu);
        }

        /// <summary>
        /// Gets the total number of credentials and the first (index 0)
        /// set of credential info associated with the given relying party in the
        /// YubiKey.
        /// </summary>
        /// <remarks>
        /// Note that if there are no credentials associated with a relying party,
        /// the return is "No Data" (Status = ResponseStatus.NoData, and
        /// CtapStatus = CtapStatus.NoCredentials). in this case, calling
        /// <c>GetData</c> will result in an exception.
        /// </remarks>
        /// <returns>
        /// The data in the response APDU, presented as a Tuple of
        /// (int,CredentialUserInfo).
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="YubiKeyResponse.Status"/> is not <see cref="ResponseStatus.Success"/>.
        /// </exception>
        /// <exception cref="Ctap2DataException">
        /// If the response from the YubiKey does not match the expected format.
        /// </exception>
        public (int credentialCount, CredentialUserInfo credentialUserInfo) GetData()
        {
            CredentialManagementData mgmtData = _response.GetData();

            if (!(mgmtData.TotalCredentialsForRelyingParty is null)
                && !(mgmtData.User is null)
                && !(mgmtData.CredentialId is null)
                && !(mgmtData.CredentialPublicKey is null)
                && !(mgmtData.CredProtectPolicy is null))
            {
                var userInfo = new CredentialUserInfo(
                    mgmtData.User,
                    mgmtData.CredentialId,
                    mgmtData.CredentialPublicKey,
                    mgmtData.CredProtectPolicy.Value,
                    mgmtData.LargeBlobKey);

                return (mgmtData.TotalCredentialsForRelyingParty.Value, userInfo);
            }

            throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info);
        }
    }
}
