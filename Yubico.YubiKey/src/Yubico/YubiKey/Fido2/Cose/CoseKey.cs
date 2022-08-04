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
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Linq;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2.Cose
{
    public abstract class CoseKey : ICoseKey
    {
        protected CoseKey(CborMap map)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            Type = (CoseKeyType)map.ReadUInt64(1);
            KeyId = map.ReadByteString(2);
            Algorithm = (CoseAlgorithmIdentifier)map.ReadUInt64(3);
            Operations = map.ReadArray(4).Select(x => (CoseKeyOperations)x).ToList();
            BaseIv = map.ReadByteString(5);
        }

        protected CoseKey()
        {
            Operations = new List<CoseKeyOperations>();
        }

        public static CoseKey Create(CborReader cborReader)
        {
            if (cborReader is null)
            {
                throw new ArgumentNullException(nameof(cborReader));
            }

            var map = new CborMap(cborReader);

            if (!map.Contains(3))
            {
                throw new Ctap2DataException("Missing required field.");
            }

            switch ((CoseAlgorithmIdentifier)map.ReadUInt64(3))
            {
                case CoseAlgorithmIdentifier.ES256:
                case CoseAlgorithmIdentifier.ES384:
                case CoseAlgorithmIdentifier.ES512:
                    return new CosePublicEcKey(map);
            }

            throw new NotSupportedException("Algorithm not supported.");
        }

        /// <inheritdoc />
        public CoseKeyType Type { get; set; }

        /// <inheritdoc />
        public ReadOnlyMemory<byte> KeyId { get; set; }

        /// <inheritdoc />
        public CoseAlgorithmIdentifier Algorithm { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<CoseKeyOperations> Operations { get; set; }

        /// <inheritdoc />
        public ReadOnlyMemory<byte> BaseIv { get; set; }
    }
}
