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
    public sealed class PivCurve25519PublicKey : PivPublicKey
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

        private PivCurve25519PublicKey()
        {
        }

        private PivCurve25519PublicKey(
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

        public static PivCurve25519PublicKey CreateFromPublicKey(IPublicKeyParameters keyParameters)
        {
            var keyDefinition = keyParameters.GetKeyDefinition();
            var keyType = keyDefinition.KeyType;
            var algorithm = keyType.GetPivAlgorithm();

            return EncodeAndCreate(
                keyParameters.GetPublicPoint().Span, algorithm,
                keyParameters.ExportSubjectPublicKeyInfo(), 
                keyDefinition);
        }

        public static PivCurve25519PublicKey CreateFromPublicPoint(
            ReadOnlyMemory<byte> publicPoint,
            KeyDefinitions.KeyType keyType)
        {
            var keyDefinition = KeyDefinitions.GetByKeyType(keyType);
            var algorithm = keyType.GetPivAlgorithm();
            byte[] encodedKey = AsnPublicKeyWriter.EncodeToSpki(publicPoint, keyType);

            return EncodeAndCreate(publicPoint.Span, algorithm, encodedKey, keyDefinition);
        }
        
        internal static PivPublicKey CreateFromPivEncoding(
            ReadOnlyMemory<byte> encodedPublicKey,
            PivAlgorithm algorithm)
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
                // if (algorithm.HasValue)
                // {
                    var keyType = algorithm.GetKeyType();
                    var keyDefinition = KeyDefinitions.GetByKeyType(keyType);
                    byte[] encodedKey = AsnPublicKeyWriter.EncodeToSpki(publicPoint, keyType);
                    return EncodeAndCreate(publicPoint.Span, algorithm, encodedKey, keyDefinition);
                // }
                // else
                // {
                //     var pivAlgorithm = GetAlgorithm(publicPoint.Span);
                //     var keyType = pivAlgorithm.GetKeyType();
                //     var keyDefinition = KeyDefinitions.GetByKeyType(keyType);
                //     byte[] encodedKey = AsnPublicKeyWriter.EncodeToSpki(publicPoint, keyType);
                //     return EncodeAndCreate(publicPoint.Span, pivAlgorithm, encodedKey, keyDefinition);
                // }
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

        private static PivCurve25519PublicKey EncodeAndCreate(
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

            return new PivCurve25519PublicKey(
                publicPoint.ToArray(),
                pivEncodedKey.ToArray(),
                yubiKeyEncodedKey.ToArray(),
                encodedKey.ToArray(),
                algorithm,
                keyDefinition);
        }

        // // This should not be used, as it is not guaranteed to be correct for all algorithms (e.g. P256, ED25519, X25519 which all share have the same length)
        // private static PivAlgorithm GetAlgorithm(ReadOnlySpan<byte> publicPoint)
        // {
        //     var algorithm = publicPoint.Length switch
        //     {
        //         EccP256PublicKeySize => PivAlgorithm.EccP256,
        //         EccP384PublicKeySize => PivAlgorithm.EccP384,
        //         _ => throw new ArgumentException(
        //             string.Format(CultureInfo.CurrentCulture, ExceptionMessages.InvalidPublicKeyData))
        //     };
        //
        //     if (algorithm is PivAlgorithm.EccP256 or PivAlgorithm.EccP384 &&
        //         publicPoint[0] != LeadingEccByte)
        //     {
        //         throw new ArgumentException(
        //             string.Format(
        //                 CultureInfo.CurrentCulture,
        //                 ExceptionMessages.InvalidPublicKeyData));
        //     }
        //
        //     return algorithm;
        // }
    }
}
