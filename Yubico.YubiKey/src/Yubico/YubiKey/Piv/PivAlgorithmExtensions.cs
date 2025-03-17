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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Yubico.Core.Logging;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv
{
    public record PivAlgorithmDefinition
    {
        public required PivAlgorithm Algorithm { get; init; }
        public required KeyDefinitions.KeyDefinition KeyDefinition { get; init; }

        public bool SupportsKeyGeneration =>
            Algorithm switch
            {
                PivAlgorithm.Rsa1024 => true,
                PivAlgorithm.Rsa2048 => true,
                PivAlgorithm.Rsa3072 => true,
                PivAlgorithm.Rsa4096 => true,
                PivAlgorithm.EccP256 => true,
                PivAlgorithm.EccP384 => true,
                PivAlgorithm.EccEd25519 => true,
                PivAlgorithm.EccX25519 => true,
                _ => false,
            };
    }

    public static class IPublicKeyParametersExtensions
    {
        public static PivAlgorithmDefinition GetPivDefinition(this IPublicKeyParameters parameters) =>
            parameters.GetKeyDefinition().GetKeyDefinition();
    }

    public static class KeyParametersPivHelper
    {
        const int PrimePTag = 0x01;
        const int PrimeQTag = 0x02;
        const int ExponentPTag = 0x03;
        const int ExponentQTag = 0x04;
        const int CoefficientTag = 0x05;
        const int CrtComponentCount = 5;

        public static T CreateFromPivEncoding<T>(ReadOnlyMemory<byte> pivEncodingBytes) where T : IPrivateKeyParameters
        {
            if (pivEncodingBytes.IsEmpty)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPrivateKeyData));
            }

            byte tag = pivEncodingBytes.Span[0];
            IPrivateKeyParameters pkp = tag switch
            {
                _ when PivPrivateKey.IsValidEccTag(tag) => CreateEcFromPivEncoding(pivEncodingBytes),
                _ when PivPrivateKey.IsValidRsaTag(tag) => CreateRsaFromPivEncoding(pivEncodingBytes),
                _ => throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, ExceptionMessages.InvalidPrivateKeyData))
            };

            return (T)pkp;
        }

        private static ECPrivateKeyParameters CreateEcFromPivEncoding(ReadOnlyMemory<byte> pivEncodingBytes)
        {
            if (TlvObject.TryParse(pivEncodingBytes.Span, out var tlv) && PivPrivateKey.IsValidEccTag(tlv.Tag))
            {
                switch (tlv.Tag)
                {
                    case PivPrivateKey.EccTag:
                        List<KeyDefinitions.KeyDefinition> allowed =
                            [KeyDefinitions.P256, KeyDefinitions.P384, KeyDefinitions.P521];
                        var keyDefinition = allowed.Single(kd => kd.LengthInBytes == tlv.Value.Span.Length);
                        return ECPrivateKeyParameters.CreateFromValue(tlv.Value.Span.ToArray(), keyDefinition.KeyType);
                    case PivPrivateKey.EccEd25519Tag:
                        return ECPrivateKeyParameters.CreateFromValue(tlv.Value.ToArray(), KeyDefinitions.KeyType.Ed25519);
                    case PivPrivateKey.EccX25519Tag:
                        return ECPrivateKeyParameters.CreateFromValue(tlv.Value.ToArray(), KeyDefinitions.KeyType.X25519);
                }
            }
            
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPrivateKeyData));
        }

        private static RSAPrivateKeyParameters CreateRsaFromPivEncoding(ReadOnlyMemory<byte> pivEncodingBytes)
        {
            var tlvReader = new TlvReader(pivEncodingBytes);
            var valueArray = new ReadOnlyMemory<byte>[CrtComponentCount];

            int index = 0;
            for (; index < CrtComponentCount; index++)
            {
                valueArray[index] = ReadOnlyMemory<byte>.Empty;
            }

            index = 0;
            while (index < CrtComponentCount)
            {
                if (tlvReader.HasData == false)
                {
                    break;
                }

                int tag = tlvReader.PeekTag();
                var temp = tlvReader.ReadValue(tag);
                if (tag <= 0 || tag > CrtComponentCount)
                {
                    continue;
                }

                if (valueArray[tag - 1].IsEmpty == false)
                {
                    continue;
                }

                index++;
                valueArray[tag - 1] = temp;
            }

            var primeP = valueArray[PrimePTag - 1].Span;
            var primeQ = valueArray[PrimeQTag - 1].Span;
            var exponentP = valueArray[ExponentPTag - 1].Span;
            var exponentQ = valueArray[ExponentQTag - 1].Span;
            var coefficient = valueArray[CoefficientTag - 1].Span;

            var rsaParameters = new RSAParameters
            {
                // D = privateExponent,      // Private exponent
                // Modulus = modulus,        // Modulus (n)
                // Exponent = publicExponent, // Public exponent (e)
                P = primeP.ToArray(), // First prime factor
                Q = primeQ.ToArray(), // Second prime factor
                DP = exponentP.ToArray(), // d mod (p-1)
                DQ = exponentQ.ToArray(), // d mod (q-1)
                InverseQ = coefficient.ToArray() // (q^-1) mod p
            };

            return new RSAPrivateKeyParameters(rsaParameters);
        }
    }

    /// <summary>
    /// Extension methods to operate on the PivAlgorithm enum.
    /// </summary>
    public static class PivAlgorithmExtensions
    {
        //  Todo WIll be used in PivSession and the Command classes
        // Might do special class for the tuple

        // Possible nullable?
        // Or store in dict, and use containskey
        // public static PivAlgorithmDefinition? GetByKeyDefinitionKeyType(this KeyDefinitions.KeyType keyType) => keyType switch
        // {
        //     KeyDefinitions.KeyType.RSA1024 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.Rsa1024, KeyDefinition = KeyDefinitions.RSA1024 },
        //     KeyDefinitions.KeyType.RSA2048 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.Rsa2048, KeyDefinition = KeyDefinitions.RSA2048 },
        //     KeyDefinitions.KeyType.RSA3072 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.Rsa3072, KeyDefinition = KeyDefinitions.RSA3072 },
        //     KeyDefinitions.KeyType.RSA4096 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.Rsa4096, KeyDefinition = KeyDefinitions.RSA4096 },
        //     KeyDefinitions.KeyType.P256 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.EccP256, KeyDefinition = KeyDefinitions.P256 },
        //     KeyDefinitions.KeyType.P384 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.EccP384, KeyDefinition = KeyDefinitions.P384 },
        //     KeyDefinitions.KeyType.Ed25519 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.EccEd25519, KeyDefinition = KeyDefinitions.Ed25519 },
        //     KeyDefinitions.KeyType.X25519 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.EccX25519, KeyDefinition = KeyDefinitions.X25519 },
        //     _ => null,
        // };

        // TODO Optimize
        public static PivAlgorithmDefinition GetKeyDefinition(this KeyDefinitions.KeyDefinition keyDefinition)
        {
            PivAlgorithmDefinition[] definitions =
            [
                new PivAlgorithmDefinition { Algorithm = PivAlgorithm.Rsa1024, KeyDefinition = KeyDefinitions.RSA1024 },
                new PivAlgorithmDefinition { Algorithm = PivAlgorithm.Rsa2048, KeyDefinition = KeyDefinitions.RSA2048 },
                new PivAlgorithmDefinition { Algorithm = PivAlgorithm.Rsa3072, KeyDefinition = KeyDefinitions.RSA3072 },
                new PivAlgorithmDefinition { Algorithm = PivAlgorithm.Rsa4096, KeyDefinition = KeyDefinitions.RSA4096 },
                new PivAlgorithmDefinition { Algorithm = PivAlgorithm.EccP256, KeyDefinition = KeyDefinitions.P256 },
                new PivAlgorithmDefinition { Algorithm = PivAlgorithm.EccP384, KeyDefinition = KeyDefinitions.P384 },
                new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.EccEd25519, KeyDefinition = KeyDefinitions.Ed25519 },
                new PivAlgorithmDefinition { Algorithm = PivAlgorithm.EccX25519, KeyDefinition = KeyDefinitions.X25519 }
            ];

            return definitions.Single(d => d.KeyDefinition.KeyType == keyDefinition.KeyType);
        }

        // Possible nullable?
        public static PivAlgorithmDefinition? GetPivKeyDef(this PivAlgorithm algorithm) =>
            algorithm switch
            {
                PivAlgorithm.Rsa1024 => new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.Rsa1024, KeyDefinition = KeyDefinitions.RSA1024 },
                PivAlgorithm.Rsa2048 => new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.Rsa2048, KeyDefinition = KeyDefinitions.RSA2048 },
                PivAlgorithm.Rsa3072 => new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.Rsa3072, KeyDefinition = KeyDefinitions.RSA3072 },
                PivAlgorithm.Rsa4096 => new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.Rsa4096, KeyDefinition = KeyDefinitions.RSA4096 },
                PivAlgorithm.EccP256 => new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.EccP256, KeyDefinition = KeyDefinitions.P256 },
                PivAlgorithm.EccP384 => new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.EccP384, KeyDefinition = KeyDefinitions.P384 },
                PivAlgorithm.EccEd25519 => new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.EccEd25519, KeyDefinition = KeyDefinitions.Ed25519 },
                PivAlgorithm.EccX25519 => new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.EccX25519, KeyDefinition = KeyDefinitions.X25519 },

                // PivAlgorithm.TripleDes => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.TripleDes, KeyDefinition = KeyDefinitions.TripleDes },
                _ => null,
            };

        public static PivAlgorithm GetPivAlgorithm(this KeyDefinitions.KeyType keyType)
        {
            return keyType switch
            {
                KeyDefinitions.KeyType.Ed25519 => PivAlgorithm.EccEd25519,
                KeyDefinitions.KeyType.X25519 => PivAlgorithm.EccX25519,
                KeyDefinitions.KeyType.P256 => PivAlgorithm.EccP256,
                KeyDefinitions.KeyType.P384 => PivAlgorithm.EccP384,
                KeyDefinitions.KeyType.P521 => PivAlgorithm.EccP521,
                KeyDefinitions.KeyType.RSA1024 => PivAlgorithm.Rsa1024,
                KeyDefinitions.KeyType.RSA2048 => PivAlgorithm.Rsa2048,
                KeyDefinitions.KeyType.RSA3072 => PivAlgorithm.Rsa3072,
                KeyDefinitions.KeyType.RSA4096 => PivAlgorithm.Rsa4096,
                _ => throw new NotSupportedException("Unsupported keytype")
            };
        }

        public static KeyDefinitions.KeyType GetKeyType(this PivAlgorithm pivAlgorithm)
        {
            return pivAlgorithm switch
            {
                PivAlgorithm.EccEd25519 => KeyDefinitions.KeyType.Ed25519,
                PivAlgorithm.EccX25519 => KeyDefinitions.KeyType.X25519,
                PivAlgorithm.EccP256 => KeyDefinitions.KeyType.P256,
                PivAlgorithm.EccP384 => KeyDefinitions.KeyType.P384,
                PivAlgorithm.EccP521 => KeyDefinitions.KeyType.P521,
                _ => throw new NotSupportedException("Unsupported pivAlgorithm")
            };
        }

        /// <summary>
        /// Determines if the given algorithm is one that can be used to generate
        /// a key pair.
        /// </summary>
        /// <remarks>
        /// The PivAlgorithm enum contains Triple-DES and other algorithms, which
        /// cannot be used as an algorithm to generate a key pair. Make sure the
        /// given algorithm is one that can be used when generating a key pair.
        /// <para>
        /// This also works if checking an algorithm for import, signing, or
        /// decrypting/key exchange.
        /// </para>
        /// </remarks>
        /// <param name="algorithm">
        /// The algorithm name to check.
        /// </param>
        /// <returns>
        /// A boolean, true if the algorithm is one that can be used to generate
        /// a key pair, and false otherwise.
        /// </returns>
        [Obsolete("Use other")]
        public static bool IsValidAlgorithmForGenerate(this PivAlgorithm algorithm) =>
            algorithm switch
            {
                PivAlgorithm.Rsa1024 => true,
                PivAlgorithm.Rsa2048 => true,
                PivAlgorithm.Rsa3072 => true,
                PivAlgorithm.Rsa4096 => true,
                PivAlgorithm.EccP256 => true,
                PivAlgorithm.EccP384 => true,
                PivAlgorithm.EccEd25519 => true,
                _ => false,
            };

        /// <summary>
        /// The size of a key, in bits, of the given algorithm.
        /// </summary>
        /// <remarks>
        /// The PivAlgorithm enum specifies algorithm and key size for RSA and
        /// ECC. If you have a variable of type <c>PivAlgorithm</c>, use this
        /// extension to get the bit size out.
        /// <para>
        /// For example, suppose you obtain a public key from storage, and have a
        /// <see cref="PivPublicKey"/> object. Maybe your code performs different
        /// tasks based on the key size (e.g. use SHA-256 or SHA-384, or build a
        /// buffer for signing). You can look at the <c>Algorithm</c> property to
        /// learn the algorithm and key size. However, if all you want is the key
        /// size, use this extension:
        /// <code language="csharp">
        ///    PivPublicKey publicKey = SomeClass.GetPublicKey(someSearchParam);
        ///    byte[] buffer = new byte[publicKey.Algorithm.KeySizeBits() / 8];
        /// </code>
        /// </para>
        /// <para>
        /// This will return the following values for each value of
        /// <c>PivAlgorithm</c>.
        /// <code>
        ///     Rsa1024    1024
        ///     Rsa2048    2048
        ///     Rsa3072    3072
        ///     Rsa4096    4096
        ///     EccP256     256
        ///     EccP384     384
        ///     TripleDes   192
        ///     Pin          64
        ///     None          0
        /// </code>
        /// Note that a Triple-DES key is made up of three DES keys, and each DES
        /// key is 8 bytes (64 bits). However, because there are 8 "parity bits"
        /// in each DES key, the actual key strength of a DES key is 56 bits.
        /// That means the actual key strength of a Triple-DES key is 168 bits. In
        /// addition, because of certain attacks, it is possible to reduce the
        /// strength of a Triple-DES key to 112 bits (it takes the equivalent of
        /// a 112-bit brute-force attack to break a Triple-DES key). Nonetheless,
        /// this extension will return 192 as the key length, in bits, of a
        /// Triple-DES key.
        /// </para>
        /// <para>
        /// A PIN or PUK is 6 to 8 bytes long. Hence, the maximum size, in bits,
        /// of a <c>PivAlgorithm.Pin</c> is 64.
        /// </para>
        /// </remarks>
        /// <param name="algorithm">
        /// The algorithm name to check.
        /// </param>
        /// <returns>
        /// An int, the size, in bits, of a key of the given algorithm.
        /// </returns>
        public static int KeySizeBits(this PivAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case PivAlgorithm.TripleDes:
                    return 192;
                case PivAlgorithm.Pin:
                    return 64;
            }

            var keyDefinition = algorithm.GetPivKeyDef();
            return keyDefinition != null
                ? keyDefinition.KeyDefinition.LengthInBits
                : 0;
        }

        /// <summary>
        /// Determines if the given algorithm is RSA.
        /// </summary>
        /// <remarks>
        /// The PivAlgorithm enum contains <c>Rsa1024</c>, <c>Rsa2048</c>, <c>Rsa3072</c>, and <c>Rsa4096</c>. But
        /// sometimes you just want to know if an algorithm is RSA or not. It
        /// would seem you would have to write code such as the following.
        /// <code language="csharp">
        ///     if ((algorithm == PivAlgorith.Rsa1024) || (algorithm == PivAlgorithm.Rsa2048) || (algorithm == PivAlgorithm.Rsa3072) || (algorithm == PivAlgorithm.Rsa4096))
        /// </code>
        /// <para>
        /// With this extension, you can simply write.
        /// <code language="csharp">
        ///     if (algorithm.IsRsa())
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="algorithm">
        /// The algorithm to check.
        /// </param>
        /// <returns>
        /// A boolean, true if the algorithm is RSA, and false otherwise.
        /// </returns>
        public static bool IsRsa(this PivAlgorithm algorithm)
        {
            var keyDefinition = algorithm.GetPivKeyDef();
            return keyDefinition is { KeyDefinition.IsRsaKey: true };
        }

        /// <summary>
        /// Determines if the given algorithm is ECC.
        /// </summary>
        /// <remarks>
        /// The PivAlgorithm enum contains <c>EccP256</c> and <c>EccP384</c>. But
        /// sometimes you just want to know if an algorithm is ECC or not. It
        /// would seem you would have to write code such as the following.
        /// <code language="csharp">
        ///     if ((algorithm == PivAlgorith.EccP256) || (algorithm == PivAlgorithm.ECCP384))
        /// </code>
        /// <para>
        /// With this extension, you can simply write.
        /// <code language="csharp">
        ///     if (algorithm.IsEcc())
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="algorithm">
        /// The algorithm to check.
        /// </param>
        /// <returns>
        /// A boolean, true if the algorithm is ECC, and false otherwise.
        /// </returns>
        public static bool IsEcc(this PivAlgorithm algorithm)
        {
            var keyDefinition = algorithm.GetPivKeyDef();
            return keyDefinition is { KeyDefinition.IsEcKey: true };
        }
    }
}
