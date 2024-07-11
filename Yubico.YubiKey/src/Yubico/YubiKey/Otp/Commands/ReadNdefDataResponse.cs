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

namespace Yubico.YubiKey.Otp.Commands
{
    /// <summary>
    /// Reads the current NDEF data from the YubiKey. Note that this command only works over NFC.
    /// </summary>
    public class ReadNdefDataResponse : OtpResponse, IYubiKeyResponseWithData<ReadOnlyMemory<byte>>
    {
        /// <summary>
        /// Constructs a ReadNdefDataResponse instance based on a ResponseApdu received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The ResponseApdu returned by the YubiKey.
        /// </param>
        public ReadNdefDataResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        /// Gets the NDEF payload as a series of bytes.
        /// </summary>
        /// <returns>
        /// The NDEF payload.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="YubiKeyResponse.Status"/> is not <see cref="ResponseStatus.Success"/>.
        /// </exception>
        public ReadOnlyMemory<byte> GetData() =>
            Status != ResponseStatus.Success
                ? throw new InvalidOperationException(StatusMessage)
                : ResponseApdu.Data;
    }
}
