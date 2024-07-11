// Copyright 2021 Yubico AB
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
using System.Collections.Generic;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands
{
    /// <summary>
    /// The response to the <see cref="GetDeviceInfoCommand"/> command, containing the YubiKey's
    /// device configuration details.
    /// </summary>
    /// 
    public class GetPagedDeviceInfoResponse :
        YubiKeyResponse,
        IYubiKeyResponseWithData<Dictionary<int, ReadOnlyMemory<byte>>>
    {
        /// <summary>
        /// Constructs a GetPagedDeviceInfoResponse instance based on a ResponseApdu received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The ResponseApdu returned by the YubiKey.
        /// </param>
        public GetPagedDeviceInfoResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        /// Retrieves and converts the response data from an APDU response into a dictionary of TLV tags and their corresponding values.
        /// </summary>
        /// <returns>A dictionary mapping integer tags to their corresponding values as byte arrays.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the response status is not successful.</exception>
        /// <exception cref="MalformedYubiKeyResponseException">Thrown when the APDU data length exceeds expected bounds or if the data conversion fails.</exception>
        public Dictionary<int, ReadOnlyMemory<byte>> GetData() =>
            GetDeviceInfoResponseHelper
                .ParseResponse(ResponseApdu, Status, StatusMessage, nameof(GetPagedDeviceInfoResponse));
    }
}
