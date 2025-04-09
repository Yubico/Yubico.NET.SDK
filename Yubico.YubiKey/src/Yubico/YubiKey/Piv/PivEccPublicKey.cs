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
using Yubico.Core.Tlv;

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
        private const int SliceIndex = 3;
        private const byte LeadingEccByte = 0x04;

        private Memory<byte> _publicPoint;

        // The default constructor. We don't want it to be used by anyone outside
        // this class.
        private PivEccPublicKey()
        {
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
        /// <param name="algorithm">The algorithm used, if empty, it will be determined at best effort.</param>
        /// <exception cref="ArgumentException">
        /// The format of the public point is not supported.
        /// </exception>
        [Obsolete("Usage of PivEccPublic/PivEccPrivateKey is deprecated. Use IPublicKey, IPrivateKey instead", false)]
        public PivEccPublicKey(ReadOnlySpan<byte> publicPoint, PivAlgorithm? algorithm = null)
        {
            if (!LoadEccPublicKey(publicPoint, algorithm))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPublicKeyData));
            }
        }

        /// <summary>
        /// Contains the public point: <c>04 || x-coordinate || y-coordinate</c>.
        /// </summary>
        public ReadOnlySpan<byte> PublicPoint => _publicPoint.Span;

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
        /// <param name="publicKeyObject">
        ///     Where the resulting public key object will be deposited.
        /// </param>
        /// <param name="encodedPublicKey">
        ///     The PIV TLV encoding.
        /// </param>
        /// <param name="algorithm"></param>
        /// <returns>
        /// True if the method was able to create a new RSA public key object,
        /// false otherwise.
        /// </returns>
        internal static bool TryCreate(
            out PivPublicKey publicKeyObject,
            ReadOnlyMemory<byte> encodedPublicKey,
            PivAlgorithm? algorithm)
        {
            var returnValue = new PivEccPublicKey();
            publicKeyObject = returnValue;

            try
            {
                var tlvReader = new TlvReader(encodedPublicKey);
                int tag = tlvReader.PeekTag(2);

                if (tag == PivConstants.PublicKeyTag)
                {
                    tlvReader = tlvReader.ReadNestedTlv(tag);
                }

                ReadOnlyMemory<byte> value = null;

                while (tlvReader.HasData)
                {
                    tag = tlvReader.PeekTag();

                    if (tag != PivConstants.PublicECTag)
                    {
                        return false;
                    }

                    if (value.IsEmpty == false)
                    {
                        return false;
                    }

                    value = tlvReader.ReadValue(PivConstants.PublicECTag);
                }

                return returnValue.LoadEccPublicKey(value.Span, algorithm);
            }
            catch (TlvException)
            {
                return false;
            }
        }

        // Load the public point and build the encoded key.
        // This method will verify that this class supports the public key given.
        // If successful, return true.
        // If the key given is not supported or the key could not be loaded,
        // return false.
        private bool LoadEccPublicKey(ReadOnlySpan<byte> publicPoint, PivAlgorithm? algorithm)
        {
            if (algorithm.HasValue)
            {
                int expectedSize = GetExpectedSize(algorithm);
                if(publicPoint.Length != expectedSize)
                {
                    return false;
                }
                
                Algorithm = algorithm.Value;
            }
            else
            {
                if (publicPoint.IsEmpty || publicPoint[0] != LeadingEccByte)
                {
                    return false;
                }
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
            }
            
            var tlvWriter = new TlvWriter();
            using (tlvWriter.WriteNestedTlv(PivConstants.PublicKeyTag))
            {
                tlvWriter.WriteValue(PivConstants.PublicECTag, publicPoint);
            }

            PivEncodedKey = tlvWriter.Encode();

            // The Metadate encoded key is the contents of the nested. So set
            // that to be a slice of the EncodedKey.
            YubiKeyEncodedKey = PivEncodedKey[SliceIndex..];

            _publicPoint = new Memory<byte>(publicPoint.ToArray());

            return true;
        }

        private static int GetExpectedSize([DisallowNull] PivAlgorithm? algorithm)
        {
            if (algorithm == null)
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

            int expectedSize = algorithm.Value.GetPivKeyDefinition().KeyDefinition.LengthInBytes;
            if (algorithm.Value is PivAlgorithm.EccEd25519 or PivAlgorithm.EccX25519)
            {
                return expectedSize;
            }

            return (expectedSize * 2) + 1;

        }
    }
}
