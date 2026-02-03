// Copyright 2025 Yubico AB
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
    /// The response class to retrieve the public key from an asymmetric
    /// credential in the YubiHSM Auth application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This response class handles the public key data returned by the
    /// GetPubkey command. The response contains a 65-byte public key
    /// (PK-OCE) for asymmetric credentials such as ECC P-256.
    /// </para>
    /// <para>
    /// The associated command class is <see cref="GetPubkeyCommand"/>.
    /// </para>
    /// </remarks>
    public sealed class GetPubkeyResponse : YubiKeyResponse,
        IYubiKeyResponseWithData<ReadOnlyMemory<byte>>
    {
        /// <summary>
        /// Constructs a GetPubkeyResponse based on a ResponseApdu
        /// received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The ResponseApdu returned by the YubiKey.
        /// </param>
        public GetPubkeyResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        /// Get the public key (PK-OCE) from the asymmetric credential.
        /// </summary>
        /// <remarks>
        /// If the method cannot return the data, it will throw an exception.
        /// This happens when the <see cref="IYubiKeyResponse.Status"/>
        /// property indicates an error, or the data returned from the YubiKey
        /// was malformed or incomplete.
        /// </remarks>
        /// <returns>
        /// A 65-byte read-only memory containing the public key for the credential.
        /// For ECC P-256 credentials, this is an uncompressed elliptic curve point
        /// in the format: 0x04 || X (32 bytes) || Y (32 bytes).
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
