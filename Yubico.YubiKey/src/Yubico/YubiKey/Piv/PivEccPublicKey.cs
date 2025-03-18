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
using Microsoft.Extensions.Logging;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv
{
    /// <summary>
    /// This class holds an ECC public key.
    /// </summary>
    /// <remarks>
    /// An ECC public key consists of a public point. To build an ECC key object
    /// from the public point, use this class's constructor.
    /// <para>
    /// Once you have the object built, you can get the encoded public key from
    /// either the <c>PivEncodedPublicKey</c> or <c>YubiKeyEncodedPublicKey</c>
    /// property.
    /// </para>
    /// <para>If you have an encoded public key, and want to build an object, use
    /// the static factory <c>Create</c> method in the base class
    /// <see cref="PivPublicKey"/>.
    /// </para>
    /// <para>
    /// The YubiKey supports only P256 and P384 ECC keys, which means that the
    /// point must be exactly 65 or 97 bytes long (the point must be of the form
    /// <c>04 || x-coordinate || y-coordinate</c>).
    /// </para>
    /// <para>
    /// You can build an object from either the encoded public key (using the
    /// <c>PivPublicKey.Create</c> static factory method), and then examine the
    /// public point, or you can build an object from the public point, then
    /// examine the encoding.
    /// </para>
    /// </remarks>
    public sealed class PivEccPublicKey : PivPublicKey
    {
        private const int EccP256PublicKeySize = 65;
        private const int EccP384PublicKeySize = 97;
        private const byte LeadingEccByte = 0x04;

        private ReadOnlyMemory<byte> _publicPoint;

        /// <summary>
        /// Contains the public point: <c>04 || x-coordinate || y-coordinate</c>.
        /// </summary>
        public new ReadOnlySpan<byte> PublicPoint
        {
            set => _publicPoint = value.ToArray();
            get => _publicPoint.ToArray();
        }

        private PivEccPublicKey()
        {
        }

        private PivEccPublicKey(
            Memory<byte> publicPoint,
            Memory<byte> pivTlvEncodedKey,
            Memory<byte> yubiKeyEncodedKey,
            Memory<byte> encodedKey,
            PivAlgorithm algorithm,
            KeyDefinitions.KeyDefinition keyDefinition)
        {
            _publicPoint = publicPoint;
            EncodedKey = encodedKey;
            PivEncodedKey = pivTlvEncodedKey;
            YubiKeyEncodedKey = yubiKeyEncodedKey;
            Algorithm = algorithm;
            KeyDefinition = keyDefinition;
        }

        internal static bool CanCreate(ReadOnlyMemory<byte> encodedPublicKey)
        {
            try
            {
                var tlvReader = new TlvReader(encodedPublicKey);
                int tag = tlvReader.PeekTag(2);
                if (tag != PublicKeyTag)
                {
                    return false;
                }

                tlvReader = tlvReader.ReadNestedTlv(tag);
                tag = tlvReader.PeekTag();
                return tag == EccTag && tlvReader.TryReadValue(out _, EccTag);
            }
            catch (TlvException ex)
            {
                Logger.LogWarning(ex, "Exception while reading public key data");
                return false;
            }
        }

        public static PivEccPublicKey CreateFromPublicKey(IPublicKeyParameters keyParameters)
        {
            var keyDefinition = keyParameters.GetKeyDefinition();
            var keyType = keyDefinition.KeyType;
            var algorithm = keyType.GetPivAlgorithm();

            return EncodeAndCreate(
                keyParameters.GetPublicPoint().Span, algorithm,
                keyParameters.ExportSubjectPublicKeyInfo(), 
                keyDefinition);
        }

        public static PivEccPublicKey CreateFromPublicPoint(
            ReadOnlyMemory<byte> publicPoint,
            KeyDefinitions.KeyType keyType)
        {
            var keyDefinition = KeyDefinitions.GetByKeyType(keyType);
            var algorithm = keyType.GetPivAlgorithm();
            byte[] encodedKey = AsnPublicKeyWriter.EncodeToSpki(publicPoint, keyType);

            return EncodeAndCreate(publicPoint.Span, algorithm, encodedKey, keyDefinition);
        }

        /// <summary>
        /// Try to create a new instance of an ECC public key object based on the
        /// encoding.
        /// </summary>
        /// <remarks>
        /// This static method will build a <c>PivEccPublicKey</c> object and set
        /// the output parameter <c>publicKeyObject</c> to the resulting key. If
        /// the encoding is not of a supported ECC public key, it will return
        /// false.
        /// </remarks>
        /// <param name="encodedPublicKey">
        /// The PIV TLV encoding.
        /// </param>
        /// <param name="algorithm">The algorithm (and key size) of the key pair generated. If set, will use the algorithm instead of guessing the algorithm by length</param>
        /// <returns>
        /// True if the method was able to create a new RSA public key object,
        /// false otherwise.
        /// </returns>
        internal static PivPublicKey CreateFromPivEncoding(
            ReadOnlyMemory<byte> encodedPublicKey,
            PivAlgorithm? algorithm = null)
        {
            try
            {
                var tlvReader = new TlvReader(encodedPublicKey);

                // Read the public key tag
                int tag = tlvReader.PeekTag(2);
                if (tag == PublicKeyTag)
                {
                    tlvReader = tlvReader.ReadNestedTlv(tag);
                }

                // Read the ECC tag
                tag = tlvReader.PeekTag();
                if (tag != EccTag)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidPublicKeyData)
                        );
                }

                var publicPoint = tlvReader.ReadValue(EccTag);
                if (algorithm.HasValue)
                {
                    var keyType = algorithm.Value.GetKeyType();
                    var keyDefinition = KeyDefinitions.GetByKeyType(keyType);
                    byte[] encodedKey = AsnPublicKeyWriter.EncodeToSpki(publicPoint, keyType);
                    return EncodeAndCreate(publicPoint.Span, algorithm.Value, encodedKey, keyDefinition);
                }
                else
                {
                    var pivAlgorithm = GetAlgorithm(publicPoint.Span);
                    var keyType = pivAlgorithm.GetKeyType();
                    var keyDefinition = KeyDefinitions.GetByKeyType(keyType);
                    byte[] encodedKey = AsnPublicKeyWriter.EncodeToSpki(publicPoint, keyType);
                    return EncodeAndCreate(publicPoint.Span, pivAlgorithm, encodedKey, keyDefinition);
                }
            }
            catch (TlvException)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPublicKeyData)
                    );
            }
        }

        private static PivEccPublicKey EncodeAndCreate(
            ReadOnlySpan<byte> publicPoint,
            PivAlgorithm algorithm,
            ReadOnlyMemory<byte> encodedKey,
            KeyDefinitions.KeyDefinition keyDefinition)
        {
            var tlvWriter = new TlvWriter();
            using (tlvWriter.WriteNestedTlv(PublicKeyTag))
            {
                tlvWriter.WriteValue(EccTag, publicPoint);
            }

            var pivEncodedKey = tlvWriter.Encode().AsSpan();

            // The first two bytes are the public key tag, followed by third byte, the ECC tag 
            var yubiKeyEncodedKey = pivEncodedKey[3..];

            return new PivEccPublicKey(
                publicPoint.ToArray(),
                pivEncodedKey.ToArray(),
                yubiKeyEncodedKey.ToArray(),
                encodedKey.ToArray(),
                algorithm,
                keyDefinition);
        }

        // This should not be used, as it is not guaranteed to be correct for all algorithms (e.g. P256, ED25519, X25519 which all share have the same length)
        private static PivAlgorithm GetAlgorithm(ReadOnlySpan<byte> publicPoint)
        {
            var algorithm = publicPoint.Length switch
            {
                EccP256PublicKeySize => PivAlgorithm.EccP256,
                EccP384PublicKeySize => PivAlgorithm.EccP384,
                _ => throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, ExceptionMessages.InvalidPublicKeyData))
            };

            if (algorithm is PivAlgorithm.EccP256 or PivAlgorithm.EccP384 &&
                publicPoint[0] != LeadingEccByte)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPublicKeyData));
            }

            return algorithm;
        }

        /// <summary>
        /// Create a new instance of an ECC public key object based on the
        /// given point.
        /// </summary>
        /// <remarks>
        /// The point must be provided in the following form
        /// <code>
        ///   04 || x-coordinate || y-coordinate
        ///   each coordinate must be the same size, either 32 bytes (256 bits)
        ///   or 48 bytes (384 bits).
        ///   Prepend 00 bytes if necessary.
        /// </code>
        /// The class will determine the algorithm (<c>PivAlgorithm.EccP256</c>
        /// or <c>PivAlgorithm.EccP384</c>) based on the size of the point.
        /// </remarks>
        /// <param name="publicPoint">
        /// The public point to use to build the object.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The format of the public point is not supported.
        /// </exception>
        [Obsolete(
            "This constructor is deprecated. Users must specify management key algorithm type, as it cannot be assumed.")]
        public PivEccPublicKey(
            ReadOnlySpan<byte>
                publicPoint)
        {
            if (LoadEccPublicKey(publicPoint) == false)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPublicKeyData));
            }
        }

        [Obsolete("Use PivEccPublicKey.CreateFromPublicPoint(publicPoint, algorithm) instead")]
        private bool LoadEccPublicKey(ReadOnlySpan<byte> publicPoint)
        {
            switch (publicPoint.Length)
            {
                case EccP256PublicKeySize:
                    Algorithm = PivAlgorithm.EccP256;

                    break;

                case EccP384PublicKeySize:
                    Algorithm = PivAlgorithm.EccP384;

                    break;

                default:
                    return false;
            }

            if (publicPoint[0] != LeadingEccByte)
            {
                return false;
            }

            var tlvWriter = new TlvWriter();
            using (tlvWriter.WriteNestedTlv(PublicKeyTag))
            {
                tlvWriter.WriteValue(EccTag, publicPoint);
            }

            PivEncodedKey = tlvWriter.Encode();
            YubiKeyEncodedKey = PivEncodedKey[3..];

            _publicPoint = new Memory<byte>(publicPoint.ToArray());
            // TODO Must set the new properties (EncodedKey, KeyDefinition, PublicPoint)
            return true;
        }
    }
}
