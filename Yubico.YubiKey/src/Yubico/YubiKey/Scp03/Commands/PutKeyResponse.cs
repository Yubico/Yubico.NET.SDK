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

namespace Yubico.YubiKey.Scp03.Commands
{
    /// <summary>
    /// The response to putting or replacing SCP03 keys on the YubiKey.
    /// </summary>
    internal class PutKeyResponse : Scp03Response, IYubiKeyResponseWithData<ReadOnlyMemory<byte>>
    {
        private readonly byte[] _checksum;

        public PutKeyResponse(ResponseApdu responseApdu)
            : base(responseApdu)
        {
            _checksum = new byte[responseApdu.Data.Length];
            responseApdu.Data.CopyTo(_checksum);
        }

        public ReadOnlyMemory<byte> GetData() => _checksum;
    }
}
