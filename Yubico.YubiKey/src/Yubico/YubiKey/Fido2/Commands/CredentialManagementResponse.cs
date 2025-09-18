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
///     The response partner to the CredentialManagementCommand.
/// </summary>
/// <remarks>
///     Some subcommands return no data, they simply return a success of failure
///     code.
/// </remarks>
public class CredentialManagementResponse : Fido2Response, IYubiKeyResponseWithData<CredentialManagementData>
{
    /// <summary>
    ///     Constructs a new instance of
    ///     <see cref="CredentialManagementResponse" /> based on a response APDU
    ///     provided by the YubiKey.
    /// </summary>
    /// <param name="responseApdu">
    ///     A response APDU containing the CBOR response data for the
    ///     <c>authenticatorCredentialManagement</c> command.
    /// </param>
    public CredentialManagementResponse(ResponseApdu responseApdu) : base(responseApdu)
    {
    }

    #region IYubiKeyResponseWithData<CredentialManagementData> Members

    /// <inheritdoc />
    public CredentialManagementData GetData()
    {
        if (Status != ResponseStatus.Success)
        {
            throw new InvalidOperationException(StatusMessage);
        }

        return new CredentialManagementData(ResponseApdu.Data);
    }

    #endregion
}
