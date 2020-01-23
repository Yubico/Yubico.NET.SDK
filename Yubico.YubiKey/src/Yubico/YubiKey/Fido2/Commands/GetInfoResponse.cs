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
using System.Formats.Cbor;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.Serialization;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// The response to the <see cref="GetInfoCommand"/> command, containing the device information.
    /// </summary>
    internal sealed class GetInfoResponse : Fido2Response, IYubiKeyResponseWithData<DeviceInfo>
    {
        /// <summary>
        /// Constructs a GetInfoResponse instance based on a ResponseApdu received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The ResponseApdu returned by the YubiKey.
        /// </param>
        public GetInfoResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {

        }

        /// <summary>
        /// Gets the <see cref="DeviceInfo"/> class that contains details about the authenticator, such as
        /// a list of all supported protocol versions, supported extensions, AAGUID of the device, and its capabilities.
        /// </summary>
        /// <returns>
        /// The data in the response APDU, presented as a DeviceInfo class.
        /// </returns>
        public DeviceInfo GetData()
        {
            ThrowIfFailed();

            if (ResponseApdu.Data.IsEmpty)
            {
                throw new MalformedYubiKeyResponseException(ExceptionMessages.UnknownFidoError)
                {
                    ActualDataLength = 0,
                    ResponseClass = nameof(GetInfoResponse)
                };
            }

            byte[] responseData = ResponseApdu.Data.ToArray();

            if (responseData.Length < 2)
            {
                throw new MalformedYubiKeyResponseException(ExceptionMessages.UnknownFidoError)
                {
                    ActualDataLength = responseData.Length,
                    ResponseClass = nameof(GetInfoResponse)
                };
            }

            if (responseData[0] != (byte)Fido2Status.Success)
            {
                throw new MalformedYubiKeyResponseException(ExceptionMessages.BadFido2Status)
                {
                    ActualDataLength = responseData.Length,
                    DataErrorIndex = 0,
                    ResponseClass = nameof(GetInfoResponse)
                };
            }

            Memory<byte> cborData = responseData.AsMemory(1);

            var reader = new CborReader(cborData, CborConformanceMode.Ctap2Canonical);

            DeviceInfo deviceInfo = Ctap2CborSerializer.Deserialize<DeviceInfo>(reader);

            return deviceInfo;
        }
    }
}
