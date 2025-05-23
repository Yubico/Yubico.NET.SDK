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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Piv
{
    /// <summary>
    /// This class holds an ECC private key, which consists of a private value.
    /// </summary>
    /// <remarks>
    /// With a YubiKey, the private value must be the same size as a coordinate
    /// of a point on the curve. So for ECC P-256, each coordinate is 32 bytes
    /// (256 bits), so the private value will be 32 bytes.
    /// </remarks>
    [Obsolete("Usage of PivEccPublic/PivEccPrivateKey, PivRsaPublic/PivRsaPrivateKey is deprecated. Use implementations of ECPublicKey, ECPrivateKey and RSAPublicKey, RSAPrivateKey instead", false)]
    public sealed class PivEccPrivateKey : PivPrivateKey
    {
        private const int EccP256PrivateKeySize = 32;
        private const int EccP384PrivateKeySize = 48;

        private Memory<byte> _privateValue;

        // <summary>
        // Contains the private value.
        // </summary>
        public ReadOnlySpan<byte> PrivateValue => _privateValue.Span;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private PivEccPrivateKey()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create a new instance of an ECC private key object based on the
        /// given private value.
        /// </summary>
        /// <remarks>
        /// The private value will be a "byte array", no tags or length octets.
        /// For <c>PivAlgorithm.EccP256</c> it must be 32 bytes. For
        /// <c>PivAlgorithm.EccP384</c> it must be 48 bytes.
        /// <para>
        /// The class will determine the algorithm (<c>PivAlgorithm.EccP256</c>
        /// or <c>PivAlgorithm.EccP384</c>) based on the size of the point.
        /// </para>
        /// </remarks>
        /// <param name="privateValue">
        /// The private value to use to build the object.
        /// </param>
        /// <param name="algorithm">The algorithm used, if empty, it will be determined at best effort.</param>
        /// <exception cref="ArgumentException">
        /// The size of the private value is not supported by the YubiKey.
        /// </exception>
        [Obsolete("Usage of PivEccPublic/PivEccPrivateKey is deprecated. Use ECPublicKey, ECPrivateKey instead", false)]
        public PivEccPrivateKey(
            ReadOnlySpan<byte> privateValue, 
            PivAlgorithm? algorithm = null)
        {
            if (algorithm.HasValue)
            {
                ValidateSize(privateValue, algorithm);
                Algorithm = algorithm.Value;
            }
            else
            {
                Algorithm = GetBySize(privateValue);
            }

            int tag = Algorithm switch
            {
                PivAlgorithm.EccEd25519 => PivConstants.PrivateECEd25519Tag,
                PivAlgorithm.EccX25519 => PivConstants.PrivateECX25519Tag,
                _ => PivConstants.PrivateECDsaTag
            };
            
            var tlvWriter = new TlvWriter();
            tlvWriter.WriteValue(tag, privateValue);
            EncodedKey = tlvWriter.Encode();
            _privateValue = new Memory<byte>(privateValue.ToArray());
        }

        private static void ValidateSize(ReadOnlySpan<byte> privateValue, [DisallowNull] PivAlgorithm? algorithm)
        {
            int expectedSize = algorithm.Value.GetPivKeyDefinition().KeyDefinition.LengthInBytes;
            if(privateValue.Length != expectedSize)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture, ExceptionMessages.InvalidPrivateKeyData));
            }
        }

        private static PivAlgorithm GetBySize(ReadOnlySpan<byte> privateValue)
        {
            return privateValue.Length switch
            {
                EccP256PrivateKeySize => PivAlgorithm.EccP256,
                EccP384PrivateKeySize => PivAlgorithm.EccP384,
                _ => throw new ArgumentException(string.Format(
                    CultureInfo.CurrentCulture, ExceptionMessages.InvalidPrivateKeyData))
            };
        }

        /// <summary>
        /// Create a new instance of an ECC private key object based on the
        /// encoding.
        /// </summary>
        /// <param name="encodedPrivateKey">
        ///     The PIV TLV encoding.
        /// </param>
        /// <param name="pivAlgorithm"></param>
        /// <returns>
        /// A new instance of a PivEccPrivateKey object based on the encoding.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The encoding of the private key is not supported.
        /// </exception>
        public static PivEccPrivateKey CreateEccPrivateKey(
            ReadOnlyMemory<byte> encodedPrivateKey,
            PivAlgorithm? pivAlgorithm)
        {
            var tlvReader = new TlvReader(encodedPrivateKey);
            int tag = tlvReader.PeekTag();
            if (tlvReader.HasData == false || !PivConstants.IsValidPrivateECTag(tag))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPrivateKeyData));
            }

            var value = tlvReader.ReadValue(tag);
            return new PivEccPrivateKey(value.Span, pivAlgorithm);
        }

        /// <inheritdoc />
        public override void Clear()
        {
            CryptographicOperations.ZeroMemory(_privateValue.Span);
            _privateValue = Memory<byte>.Empty;

            base.Clear();
        }
    }
}
