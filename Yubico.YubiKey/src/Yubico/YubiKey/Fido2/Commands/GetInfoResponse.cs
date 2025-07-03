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
    /// The response to the <see cref="GetInfoCommand"/> command, containing
    /// information about the device and FIDO2 application.
    /// </summary>
    public sealed class GetInfoResponse : Fido2Response, IYubiKeyResponseWithData<AuthenticatorInfo>
    {
        /// <summary>
        /// Constructs a <c>GetInfoResponse</c> instance based on a ResponseApdu
        /// received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The ResponseApdu returned by the YubiKey.
        /// </param>
        public GetInfoResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        /// Gets the <see cref="AuthenticatorInfo"/> class that contains details
        /// about the authenticator, such as a list of all supported protocol
        /// versions, supported extensions, AAGUID of the device, and its
        /// capabilities.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="YubiKeyResponse.Status"/> is not <see cref="ResponseStatus.Success"/>.
        /// </exception>
        /// <returns>
        /// The data in the response APDU, presented as an <c>AuthenticatorInfo</c> class.
        /// </returns>
        public AuthenticatorInfo GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            return new AuthenticatorInfo(ResponseApdu.Data);
        }
    }
}
