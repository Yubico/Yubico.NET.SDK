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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands
{
    /// <summary>
    /// The response to the <see cref="GetDeviceInfoCommand"/> command, containing the YubiKey's
    /// device configuration details.
    /// </summary>
    [Obsolete("This class has been replaced by GetPagedDeviceInfoResponse")]
    public sealed class GetDeviceInfoResponse : U2fResponse, IYubiKeyResponseWithData<YubiKeyDeviceInfo>
    {

        /// <summary>
        /// Constructs a GetDeviceInfoResponse instance based on a ResponseApdu received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The ResponseApdu returned by the YubiKey.
        /// </param>
        public GetDeviceInfoResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        /// Gets the <see cref="YubiKeyDeviceInfo"/> class that contains details about the current
        /// configuration of the YubiKey.
        /// </summary>
        /// <returns>
        /// The data in the response APDU, presented as a YubiKeyDeviceInfo class.
        /// </returns>
        public YubiKeyDeviceInfo GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(ExceptionMessages.NoResponseDataApduFailed);
            }

            if (ResponseApdu.Data.Length > 255)
            {
                throw new MalformedYubiKeyResponseException
                {
                    ResponseClass = nameof(GetDeviceInfoResponse),
                    ActualDataLength = ResponseApdu.Data.Length
                };
            }

            if (!YubiKeyDeviceInfo.TryCreateFromResponseData(ResponseApdu.Data, out var deviceInfo))
            {
                throw new MalformedYubiKeyResponseException
                {
                    ResponseClass = nameof(GetDeviceInfoResponse),
                };
            }

            return deviceInfo;
        }
    }
}
