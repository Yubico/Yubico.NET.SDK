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

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    /// <summary>
    ///     The response to the <see cref="GetManagementKeyRetriesCommand" />
    ///     command, containing the retries remaining for the management key.
    /// </summary>
    public sealed class GetManagementKeyRetriesResponse :
        BaseYubiHsmAuthResponse,
        IYubiKeyResponseWithData<int>
    {
        /// <summary>
        ///     Constructs a GetManagementKeyRetriesResponse instance based on a
        ///     ResponseApdu received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        ///     The ResponseApdu returned by the YubiKey.
        /// </param>
        public GetManagementKeyRetriesResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        ///     Gets the number of retries remaining for the management key.
        /// </summary>
        /// <returns>
        ///     The data in the response APDU, as an integer.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the <see cref="IYubiKeyResponse.Status" /> is not equal to
        ///     <see cref="ResponseStatus.Success" />.
        /// </exception>
        public int GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            return ResponseApdu.Data.Span[0];
        }
    }
}
