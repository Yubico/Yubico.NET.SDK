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
using System.Text;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands
{
    /// <summary>
    /// The response containing the U2F protocol version implemented by the application.
    /// </summary>
    /// <remarks>
    /// This is the partner Response class to <see cref="GetProtocolVersionCommand"/>. The
    /// data is returned as a string, describing the protocol version of the U2F application.
    /// </remarks>
    public class GetProtocolVersionResponse : U2fResponse, IYubiKeyResponseWithData<string>
    {
        /// <summary>
        /// Constructs a GetProtocolVersionResponse object from the given
        /// <see cref="ResponseApdu"/>.
        /// </summary>
        /// <param name="responseApdu">
        /// The object containing the response APDU returned by the YubiKey.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="responseApdu"/> is `null`.
        /// </exception>
        public GetProtocolVersionResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        /// Gets the U2F protocol version string from the response.
        /// </summary>
        /// <remarks>
        /// If the status of the response is not <see cref="ResponseStatus.Success"/>,
        /// this method will throw an exception.
        /// </remarks>
        /// <returns>
        /// The data in the response APDU, which is encoded as an ASCII string. This data describes the
        /// U2F protocol version.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="YubiKeyResponse.Status"/> is not <see cref="ResponseStatus.Success"/>.
        /// </exception>
        public string GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            ReadOnlySpan<byte> responseApduData = ResponseApdu.Data.Span;
            return Encoding.ASCII.GetString(responseApduData.ToArray());
        }
    }
}
