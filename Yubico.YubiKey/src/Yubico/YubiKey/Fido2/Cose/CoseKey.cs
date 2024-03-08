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
using System.Globalization;
using Yubico.YubiKey.Fido2.Cbor;

namespace Yubico.YubiKey.Fido2.Cose
{
    /// <summary>
    /// A base class for all COSE key representations.
    /// </summary>
    public abstract class CoseKey : ICborEncode
    {
        /// <summary>
        /// The CBOR tag (key of key/value pair) for the COSE key type.
        /// </summary>
        protected const int TagKeyType = 1;
        /// <summary>
        /// The CBOR tag (key of key/value pair) for the COSE key algorithm.
        /// </summary>
        protected const int TagAlgorithm = 3;

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
        /// Return a new byte array that is the key data encoded following the
        /// FIDO2/CBOR standard.
        /// </summary>
        /// <returns>
        /// The encoded key.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The object contains no key data.
        /// </exception>
        byte[] ICborEncode.CborEncode() => Encode();

        /// <summary>
        /// Return a new byte array that is the key data encoded following the
        /// FIDO2/CBOR standard.
        /// </summary>
        /// <returns>
        /// The encoded key.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The object contains no key data.
        /// </exception>
        public abstract byte[] Encode();

        /// <summary>
        /// Creates the correct COSE key representation based on the CBOR data provided.
        /// </summary>
        /// <param name="coseEncodedKey">
        /// A valid COSE key representation.
        /// </param>
        /// <param name="bytesRead">
        /// The method will return the number of bytes read in this argument.
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
        public static CoseKey Create(ReadOnlyMemory<byte> coseEncodedKey, out int bytesRead)
        {
            var map = new CborMap<int>(coseEncodedKey);
            bytesRead = map.BytesRead;

            if (!map.Contains(TagAlgorithm))
            {
                throw new Ctap2DataException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.Ctap2MissingRequiredField));
            }

            // We only support ECC using the P-256 curve. For the algorithm, we
            // might encounter either -7 (ES256 = ECDSA with SHA-256) or -25
            // (ECDHwHKDF256). If the -25 seems odd, it is specified in the FIDO2
            // standard.
            var algorithm = (CoseAlgorithmIdentifier)map.ReadInt32(TagAlgorithm);
            if ((algorithm == CoseAlgorithmIdentifier.ECDHwHKDF256) || (algorithm == CoseAlgorithmIdentifier.ES256))
            {
                return new CoseEcPublicKey(coseEncodedKey);
            }

            throw new NotSupportedException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.UnsupportedAlgorithm));
        }
    }
}
