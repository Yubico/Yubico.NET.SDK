// Copyright 2026 Yubico AB
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
    /// The response class for the <see cref="CreateHostChallengeEccP256Command"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This response contains the challenge (EPK-OCE) returned by the YubiKey device.
    /// For asymmetric (ECC) credentials, this is a 65-byte uncompressed ECC SECP256R1 public key.
    /// </para>
    /// </remarks>
    public sealed class CreateHostChallengeEccP256Response : YubiKeyResponse,
        IYubiKeyResponseWithData<ReadOnlyMemory<byte>>
    {
        /// <summary>
        /// Constructs a <see cref="CreateHostChallengeEccP256Response"/> based on a ResponseApdu
        /// received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The ResponseApdu returned by the YubiKey.
        /// </param>
        public CreateHostChallengeEccP256Response(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        /// Gets the challenge data returned by the device.
        /// </summary>
        /// <remarks>
        /// For asymmetric (ECC) credentials, this returns a 65-byte uncompressed public key.
        /// </remarks>
        /// <returns>
        /// The challenge data as a <see cref="ReadOnlyMemory{Byte}"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="IYubiKeyResponse.Status"/> is not equal to
        /// <see cref="ResponseStatus.Success"/>.
        /// </exception>
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
