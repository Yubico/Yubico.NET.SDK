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
    ///     The response to the <see cref="GetLargeBlobCommand" /> command, returning
    ///     the large blob.
    /// </summary>
    public sealed class GetLargeBlobResponse : Fido2Response, IYubiKeyResponseWithData<ReadOnlyMemory<byte>>
    {
        /// <summary>
        ///     Constructs a <c>GetLargeBlobResponse</c> instance based on a ResponseApdu
        ///     received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        ///     The ResponseApdu returned by the YubiKey.
        /// </param>
        public GetLargeBlobResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        ///     Gets the raw data returned by the YubiKey. The data is not parsed in
        ///     any way.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when <see cref="YubiKeyResponse.Status" /> is not <see cref="ResponseStatus.Success" />.
        /// </exception>
        /// <returns>
        ///     The large blob data.
        /// </returns>
        public ReadOnlyMemory<byte> GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            return ResponseApdu.Data;
        }
    }
}
