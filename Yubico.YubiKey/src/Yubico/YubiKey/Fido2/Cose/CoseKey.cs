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
using System.Formats.Cbor;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2.Cose
{
    /// <summary>
    /// A base class for all COSE key representations.
    /// </summary>
    public abstract class CoseKey
    {
        protected const long TagKeyType = 1;
        protected const long TagAlgorithm = 3;

        /// <summary>
        /// The key's type (or family). E.g. "EC2" for elliptic curve with an X,Y point.
        /// </summary>
        public CoseKeyType Type { get; set; }

        /// <summary>
        /// The key's algorithm.
        /// </summary>
        public CoseAlgorithmIdentifier Algorithm { get; set; }

        /// <summary>
        /// Constructs a <see cref="CoseKey"/> instance.
        /// </summary>
        protected CoseKey()
        {

        }

        /// <summary>
        /// Creates the correct COSE key representation based on the CBOR data provided.
        /// </summary>
        /// <param name="coseEncodedKey">
        /// A valid COSE key representation.
        /// </param>
        /// <returns>
        /// A COSE key instance corresponding to the type described by the CBOR data.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="coseEncodedKey"/> parameter was null.
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
        public static CoseKey Create(ReadOnlyMemory<byte> coseEncodedKey)
        {
            var cborReader = new CborReader(coseEncodedKey);
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
                    return new CosePublicEcKey(coseEncodedKey);
            }

            throw new NotSupportedException("Algorithm not supported.");
        }
    }
}
