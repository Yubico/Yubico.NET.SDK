// Copyright 2022 Yubico AB
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
    ///     This is the partner response class to the
    ///     <see cref="GetAssertionCommand" /> command class.
    /// </summary>
    public class GetAssertionResponse : Fido2Response, IYubiKeyResponseWithData<GetAssertionData>
    {
        /// <summary>
        ///     Constructs a new instance of the
        ///     <see cref="GetAssertionResponse" /> class based on a response APDU
        ///     provided by the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        ///     A response APDU containing the CBOR response for the
        ///     <c>authenticatorGetAssertion</c> command.
        /// </param>
        public GetAssertionResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        ///     Returns a new instance of <see cref="GetAssertionData" /> containing
        ///     the credential (a public key) and other information.
        /// </summary>
        /// <returns>
        ///     A new instance of <c>GetAssertionData</c>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The response indicates there was an error, so there is no data to
        ///     return.
        /// </exception>
        public GetAssertionData GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            return new GetAssertionData(ResponseApdu.Data);
        }
    }
}
