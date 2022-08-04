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
    /// <summary>
    /// A base class for all COSE key representations.
    /// </summary>
    public abstract class CoseKey
    {
        /// <summary>
        /// The key's type (or family). E.g. "EC2" for elliptic curve with an X,Y point.
        /// </summary>
        public CoseKeyType Type { get; set; }

        /// <inheritdoc />
        public ReadOnlyMemory<byte> KeyId { get; set; }

        /// <summary>
        /// The key's algorithm.
        /// </summary>
        public CoseAlgorithmIdentifier Algorithm { get; set; }

        /// <summary>
        /// The set of allowed operations for this key.
        /// </summary>
        public IReadOnlyList<CoseKeyOperations> Operations { get; set; }

        /// <summary>
        /// The base Initial Vector used for this key.
        /// </summary>
        public ReadOnlyMemory<byte> BaseIv { get; set; }

        /// <summary>
        /// Constructs the <see cref="CoseKey"/> base class based on the CBOR representation of a key.
        /// </summary>
        /// <param name="map">
        /// A COSE key in its native CBOR representation. Key must conform to the COSE specification (RFC8152).
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The CBOR map that was supposed to contain the COSE key was null.
        /// </exception>
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

        /// <summary>
        /// Constructs a <see cref="CoseKey"/> instance.
        /// </summary>
        protected CoseKey()
        {
            Operations = new List<CoseKeyOperations>();
        }

        /// <summary>
        /// Creates the correct COSE key representation based on the CBOR data provided.
        /// </summary>
        /// <param name="cborReader">
        /// A valid `CborReader` instance that is currently positioned on the beginning of the COSE key representation.
        /// </param>
        /// <returns>
        /// A COSE key instance corresponding to the type described by the CBOR data.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="cborReader"/> parameter was null.
        /// </exception>
        /// <exception cref="Ctap2DataException">
        /// <para>
        /// The CBOR reader is not in the correct position.
        /// </para>
        /// --- or ---
        /// <para>
        /// The <see cref="CoseAlgorithmIdentifier"/> could not be determined from the data provided.
        /// </para>
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The <see cref="CoseAlgorithmIdentifier"/> is not supported by this object representation.
        /// </exception>
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
    }
}
