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
using System.Buffers.Binary;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Otp.Commands
{
    /// <summary>
    /// The response to the <see cref="GetSerialNumberCommand"/> command, containing the YubiKey's
    /// serial number.
    /// </summary>
    public class GetSerialNumberResponse : OtpResponse, IYubiKeyResponseWithData<int>
    {
        private const int SerialNumberLength = 4;

        /// <summary>
        /// Constructs a GetSerialNumberResponse based on a ResponseApdu received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The object containing the response APDU returned by the YubiKey.
        /// </param>
        public GetSerialNumberResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {

        }

        /// <summary>
        /// Gets the serial number.
        /// </summary>
        /// <returns>
        /// The data in the ResponseAPDU, presented as an int.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="YubiKeyResponse.Status"/> is not <see cref="ResponseStatus.Success"/>.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        /// Thrown when the data received from the YubiKey does not
        /// match the expectations of the parser.
        /// </exception>
        public int GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            if (ResponseApdu.Data.Length < SerialNumberLength)
            {
                throw new MalformedYubiKeyResponseException()
                {
                    ResponseClass = nameof(GetSerialNumberResponse),
                    ExpectedDataLength = SerialNumberLength,
                    ActualDataLength = ResponseApdu.Data.Length
                };
            }

            return BinaryPrimitives.ReadInt32BigEndian(ResponseApdu.Data.Slice(0, SerialNumberLength).Span);
        }
    }
}
