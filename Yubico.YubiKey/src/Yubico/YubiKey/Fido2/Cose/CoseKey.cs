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
            var cborMap = new CborMap<int>(coseEncodedKey);

            // Set out-parameter
            bytesRead = cborMap.BytesRead;

            return CreateFromMap(cborMap, coseEncodedKey);
        }

        private static CoseKey CreateFromMap(CborMap<int> cborMap, ReadOnlyMemory<byte> coseEncodedKey)
        {
            var algorithm = GetAlgorithm(cborMap);
            return algorithm switch
            {
                CoseAlgorithmIdentifier.ECDHwHKDF256
                    or CoseAlgorithmIdentifier.ES256
                    or CoseAlgorithmIdentifier.ES384
                    or CoseAlgorithmIdentifier.ES512
                    or CoseAlgorithmIdentifier.ESP256
                    when IsKeyType(cborMap, CoseKeyType.Ec2)
                    => CoseEcPublicKey.CreateFromEncodedKey(coseEncodedKey),
                CoseAlgorithmIdentifier.EdDSA
                    when IsKeyType(cborMap, CoseKeyType.Okp)
                    => CoseEdDsaPublicKey.CreateFromEncodedKey(coseEncodedKey),
                CoseAlgorithmIdentifier.ECDHwHKDF256
                    or CoseAlgorithmIdentifier.ES256
                    or CoseAlgorithmIdentifier.ES384
                    or CoseAlgorithmIdentifier.ES512
                    or CoseAlgorithmIdentifier.ESP256
                    or CoseAlgorithmIdentifier.EdDSA
                    => throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info),
                _ => throw new NotSupportedException(
                    string.Format(CultureInfo.CurrentCulture, ExceptionMessages.UnsupportedAlgorithm))
            };
        }

        /// <summary>
        /// Creates the COSE key representation for <paramref name="coseEncodedKey"/>,
        /// tolerating algorithms this SDK does not model.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This never returns null. When the algorithm is one this SDK models,
        /// the behavior is identical to <see cref="Create"/>. When the algorithm
        /// is unrecognized, this returns a <see cref="CoseUnsupportedPublicKey"/>
        /// carrying the original encoding plus the reported key type and
        /// algorithm, instead of throwing <see cref="NotSupportedException"/>.
        /// </para>
        /// <para>
        /// This still throws when the encoding is malformed, when the key type
        /// or algorithm is missing, or when a recognized algorithm is paired
        /// with the wrong key type. Those indicate corrupt data rather than a
        /// future algorithm, so they are not tolerated. Malformed CBOR fails
        /// here exactly as it does in <see cref="Create"/>, with the same
        /// exception.
        /// </para>
        /// <para>
        /// One case differs from <see cref="Create"/> by design. For an
        /// algorithm this SDK does not model, <see cref="Create"/> never
        /// inspects the key type, so it reports the unrecognized algorithm and
        /// throws <see cref="NotSupportedException"/> whether or not a key type
        /// is present. This method must read the key type in order to build the
        /// result, so a missing one surfaces as <see cref="Ctap2DataException"/>
        /// instead. Both reject the input; only the reported reason differs.
        /// </para>
        /// <para>
        /// Use this when decoding a response field where an unrecognized key
        /// must not abort decoding of the surrounding data.
        /// </para>
        /// </remarks>
        /// <param name="coseEncodedKey">
        /// A valid COSE key representation.
        /// </param>
        /// <returns>
        /// A COSE key instance corresponding to the type described by the CBOR
        /// data, or a <see cref="CoseUnsupportedPublicKey"/> if this SDK does
        /// not model the algorithm.
        /// </returns>
        /// <exception cref="Ctap2DataException">
        /// The encoding is not a correct COSE key encoding, or it is missing the
        /// key type or the algorithm.
        /// </exception>
        internal static CoseKey CreateOrUnsupported(ReadOnlyMemory<byte> coseEncodedKey)
        {
            // Build the map outside the try, so that the only
            // NotSupportedException the catch below can observe is the one the
            // algorithm dispatch raises. CborMap also throws
            // NotSupportedException for an indefinite-length map; that path is
            // currently unreachable because the Ctap2Canonical reader rejects
            // indefinite-length items first, but decoding failures are malformed
            // data rather than an unrecognized algorithm, and must not be
            // absorbed into an unsupported key if that ever changes.
            var cborMap = new CborMap<int>(coseEncodedKey);

            try
            {
                return CreateFromMap(cborMap, coseEncodedKey);
            }
            catch (NotSupportedException)
            {
                // Reaching here means CreateFromMap dispatched on an algorithm
                // this SDK does not model, so GetAlgorithm already ran and the
                // algorithm is present.
                var algorithm = (CoseAlgorithmIdentifier)cborMap.ReadInt32(TagAlgorithm);

                // The key type is not read on the unsupported algorithm path,
                // because IsKeyType only runs for algorithms the SDK models.
                // COSE requires it in every key, so a missing one is malformed;
                // reject it here the same way IsKeyType would.
                if (!cborMap.Contains(TagKeyType))
                {
                    throw new Ctap2DataException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.Ctap2MissingRequiredField));
                }

                var keyType = (CoseKeyType)cborMap.ReadInt32(TagKeyType);

                return new CoseUnsupportedPublicKey(coseEncodedKey, keyType, algorithm);
            }
        }

        private static CoseAlgorithmIdentifier GetAlgorithm(CborMap<int> map)
        {
            if (!map.Contains(TagAlgorithm))
            {
                throw new Ctap2DataException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.Ctap2MissingRequiredField));
            }

            var algorithm = (CoseAlgorithmIdentifier)map.ReadInt32(TagAlgorithm);
            return algorithm;
        }

        private static bool IsKeyType(CborMap<int> map, CoseKeyType expectedKeyType)
        {
            if (!map.Contains(TagKeyType))
            {
                throw new Ctap2DataException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.Ctap2MissingRequiredField));
            }

            return (CoseKeyType)map.ReadInt32(TagKeyType) == expectedKeyType;
        }
    }
}
