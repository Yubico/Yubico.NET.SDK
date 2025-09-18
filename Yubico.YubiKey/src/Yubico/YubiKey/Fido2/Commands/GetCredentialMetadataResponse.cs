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

namespace Yubico.YubiKey.Fido2.Commands;

/// <summary>
///     The response partner to the GetCredentialMetadataCommand, containing
///     information about the discoverable credentials on the YubiKey.
/// </summary>
public class GetCredentialMetadataResponse
    : Fido2Response, IYubiKeyResponseWithData<(int discoverableCredentialCount, int remainingCredentialCount)>
{
    private readonly CredentialManagementResponse _response;

    /// <summary>
    ///     Constructs a new instance of
    ///     <see cref="GetCredentialMetadataResponse" /> based on a response APDU
    ///     provided by the YubiKey.
    /// </summary>
    /// <param name="responseApdu">
    ///     A response APDU containing the CBOR response data for the
    ///     <c>authenticatorCredentialManagement</c> command.
    /// </param>
    public GetCredentialMetadataResponse(ResponseApdu responseApdu)
        : base(responseApdu)
    {
        _response = new CredentialManagementResponse(responseApdu);
    }

    #region IYubiKeyResponseWithData<(int discoverableCredentialCount, int remainingCredentialCount)> Members

    /// <summary>
    ///     Gets the number of discoverable credentials and the remaining "empty
    ///     slots" in the FIDO2 application of the YubiKey, which is the number
    ///     of discoverable credentials fow which the YubiKey has space.
    /// </summary>
    /// <returns>
    ///     The data in the response APDU, presented as a pair of ints.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <see cref="YubiKeyResponse.Status" /> is not <see cref="ResponseStatus.Success" />.
    /// </exception>
    /// <exception cref="Ctap2DataException">
    ///     If the response from the YubiKey does not match the expected format.
    /// </exception>
    public (int discoverableCredentialCount, int remainingCredentialCount) GetData()
    {
        var credentialManagementData = _response.GetData();
        if (credentialManagementData.NumberOfDiscoverableCredentials is not null &&
            credentialManagementData.RemainingCredentialCount is not null)
        {
            return (credentialManagementData.NumberOfDiscoverableCredentials.Value,
                credentialManagementData.RemainingCredentialCount.Value);
        }

        throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info);
    }

    #endregion
}
