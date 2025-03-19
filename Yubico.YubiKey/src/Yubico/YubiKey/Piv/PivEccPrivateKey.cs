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
using System.Globalization;
using System.Security.Cryptography;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;

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
        /// <exception cref="ArgumentException">
        /// The size of the private value is not supported by the YubiKey.
        /// </exception>
        public PivEccPrivateKey(ReadOnlySpan<byte> privateValue)
        {
            var keyType = privateValue.Length switch
            {
                EccP256PrivateKeySize => KeyType.P256,
                EccP384PrivateKeySize => KeyType.P384,
                _ => throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPrivateKeyData)),
            };

            Algorithm = keyType.GetPivAlgorithm();

            var tlvWriter = new TlvWriter();
            tlvWriter.WriteValue(EccTag, privateValue);
            EncodedKey = tlvWriter.Encode();
            _privateValue = new Memory<byte>(privateValue.ToArray());

            KeyParameters = ECPrivateKeyParameters.CreateFromValue(privateValue.ToArray(), keyType);
        }

        /// <summary>
        /// Create a new instance of an ECC private key object based on the given
        /// private value and algorithm.
        /// </summary>
        /// <remarks>
        /// The private value will be a "byte array", no tags or length octets.
        /// For <c>PivAlgorithm.EccP256</c> it must be 32 bytes. For
        /// <c>PivAlgorithm.EccP384</c> it must be 48 bytes.
        /// </remarks>
        /// <param name="privateValue">
        /// The private value to use to build the object.
        /// </param>
        /// <param name="algorithm">
        /// The algorithm to use with the private value.
        /// </param>
        public PivEccPrivateKey(ReadOnlySpan<byte> privateValue, PivAlgorithm algorithm)
        {
            int eccTag = algorithm switch
            {
                PivAlgorithm.EccEd25519 => EccEd25519Tag,
                PivAlgorithm.EccX25519 => EccX25519Tag,
                _ => EccTag
            };

            var tlvWriter = new TlvWriter();
            tlvWriter.WriteValue(eccTag, privateValue);
            EncodedKey = tlvWriter.Encode();
            Algorithm = algorithm;
            
            var keyType = algorithm.GetPivKeyDefinition()!.KeyDefinition.KeyType; // TODO null
            KeyParameters = ECPrivateKeyParameters.CreateFromValue(privateValue.ToArray(), keyType);
            _privateValue = new Memory<byte>(privateValue.ToArray());
        }

        private PivEccPrivateKey(
            Memory<byte> privateValue,
            PivAlgorithm algorithm,
            KeyDefinition keyDefinition,
            byte[] pivEncodedKey,
            IPrivateKeyParameters keyParameters)
        {
            _privateValue = privateValue;
            KeyParameters = keyParameters;
            Algorithm = algorithm;
            EncodedKey = pivEncodedKey;
            KeyDefinition = keyDefinition;
        }

        /// <summary>
        /// Create a new instance of an ECC private key object based on the
        /// encoding.
        /// </summary>
        /// <param name="encodedPrivateKey">
        /// The PIV TLV encoding.
        /// </param>
        /// <returns>
        /// A new instance of a PivEccPrivateKey object based on the encoding.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The encoding of the private key is not supported.
        /// </exception>
        public static PivEccPrivateKey CreateEccPrivateKey(ReadOnlyMemory<byte> encodedPrivateKey)
        {
            if (TlvObject.TryParse(encodedPrivateKey.Span, out var tlv) && IsValidEccTag(tlv.Tag))
            {
                return tlv.Tag switch
                {
                    EccTag => new PivEccPrivateKey(tlv.Value.Span),
                    EccEd25519Tag => new PivEccPrivateKey(tlv.Value.Span, PivAlgorithm.EccEd25519),
                    EccX25519Tag => new PivEccPrivateKey(tlv.Value.Span, PivAlgorithm.EccX25519),
                    _ => throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidPrivateKeyData))
                };
            }

            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPrivateKeyData));
        }

        /// <inheritdoc />
        public override void Clear()
        {
            CryptographicOperations.ZeroMemory(_privateValue.Span);
            _privateValue = Memory<byte>.Empty;

            base.Clear();
        }

        public static PivPrivateKey CreateFromPrivateKey(IPrivateKeyParameters keyParameters)
        {
            var keyDefinition = keyParameters.KeyDefinition;
            var keyType = keyDefinition.KeyType;
            var algorithm = keyType.GetPivAlgorithm();
            var privateValue = keyParameters.PrivateKey;
            int eccTag = algorithm switch
            {
                PivAlgorithm.EccEd25519 => EccEd25519Tag,
                PivAlgorithm.EccX25519 => EccX25519Tag,
                _ => EccTag
            };

            var tlvWriter = new TlvWriter();
            tlvWriter.WriteValue(eccTag, privateValue.ToArray());
            byte[] pivEncodedKey = tlvWriter.Encode();

            return new PivEccPrivateKey(privateValue.ToArray(), algorithm, keyDefinition, pivEncodedKey, keyParameters);
        }
    }
}
