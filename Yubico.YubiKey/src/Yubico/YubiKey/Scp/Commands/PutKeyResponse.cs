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

namespace Yubico.YubiKey.Scp.Commands
{
    /// <summary>
    /// The response to putting or replacing SCP keys on the YubiKey.
    /// </summary>
    internal class PutKeyResponse : ScpResponse, IYubiKeyResponseWithData<ReadOnlyMemory<byte>>
    {
        private readonly byte[] _checksum;

        /// <summary>
        /// Initializes a new instance of the <see cref="PutKeyResponse"/> class.
        /// </summary>
        /// <param name="responseApdu">The <see cref="ResponseApdu"/> response from the YubiKey.</param>
        public PutKeyResponse(ResponseApdu responseApdu)
            : base(responseApdu)
        {
            _checksum = new byte[responseApdu.Data.Length];
            responseApdu.Data.CopyTo(_checksum);
        }

        /// <summary>
        /// Gets the checksum of the stored key.
        /// </summary>
        /// <returns>The checksum of the stored key.</returns>
        public ReadOnlyMemory<byte> GetData() => _checksum;
    }
}
