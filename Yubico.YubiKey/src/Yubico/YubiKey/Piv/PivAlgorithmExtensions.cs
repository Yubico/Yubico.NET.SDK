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
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv
{
    public record PivAlgorithmDefinition
    {
        public required PivAlgorithm Algorithm { get; init; }
        public required KeyDefinitions.KeyDefinition KeyDefinition { get; init; }
        public bool SupportsKeyGeneration => Algorithm switch
        {
            PivAlgorithm.Rsa1024 => true,
            PivAlgorithm.Rsa2048 => true,
            PivAlgorithm.Rsa3072 => true,
            PivAlgorithm.Rsa4096 => true,
            PivAlgorithm.EccP256 => true,
            PivAlgorithm.EccP384 => true,
            PivAlgorithm.Ed25519 => true,
            _ => false,
        };
    }

    /// <summary>
    /// Extension methods to operate on the PivAlgorithm enum.
    /// </summary>
    public static class PivAlgorithmExtensions
    {

        // WIll be used in PivSession and the Command classes
        // Might do special class for the tuple

// Possible nullable?
// Or store in dict, and use containskey
        public static PivAlgorithmDefinition GetByKeyDefinitionKeyType(this KeyDefinitions.KeyType keyType) => keyType switch
        {
            KeyDefinitions.KeyType.RSA1024 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.Rsa1024, KeyDefinition = KeyDefinitions.RSA1024 },
            KeyDefinitions.KeyType.RSA2048 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.Rsa2048, KeyDefinition = KeyDefinitions.RSA2048 },
            KeyDefinitions.KeyType.RSA3072 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.Rsa3072, KeyDefinition = KeyDefinitions.RSA3072 },
            KeyDefinitions.KeyType.RSA4096 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.Rsa4096, KeyDefinition = KeyDefinitions.RSA4096 },
            KeyDefinitions.KeyType.P256 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.EccP256, KeyDefinition = KeyDefinitions.P256 },
            KeyDefinitions.KeyType.P384 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.EccP384, KeyDefinition = KeyDefinitions.P384 },
            KeyDefinitions.KeyType.Ed25519 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.Ed25519, KeyDefinition = KeyDefinitions.Ed25519 },
            KeyDefinitions.KeyType.X25519 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.X25519, KeyDefinition = KeyDefinitions.X25519 },
            _ => throw new NotSupportedException("Unsupported key type" + keyType),
        };

// Possible nullable?
        public static PivAlgorithmDefinition GetByKeyDefinitionKeyType(this PivAlgorithm algorithm) => algorithm switch
        {
            PivAlgorithm.Rsa1024 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.Rsa1024, KeyDefinition = KeyDefinitions.RSA1024 },
            PivAlgorithm.Rsa2048 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.Rsa2048, KeyDefinition = KeyDefinitions.RSA2048 },
            PivAlgorithm.Rsa3072 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.Rsa3072, KeyDefinition = KeyDefinitions.RSA3072 },
            PivAlgorithm.Rsa4096 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.Rsa4096, KeyDefinition = KeyDefinitions.RSA4096 },
            PivAlgorithm.EccP256 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.EccP256, KeyDefinition = KeyDefinitions.P256 },
            PivAlgorithm.EccP384 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.EccP384, KeyDefinition = KeyDefinitions.P384 },
            PivAlgorithm.Ed25519 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.Ed25519, KeyDefinition = KeyDefinitions.Ed25519 },
            PivAlgorithm.X25519 => new PivAlgorithmDefinition { Algorithm = PivAlgorithm.X25519, KeyDefinition = KeyDefinitions.X25519 },
            _ => throw new NotSupportedException("Unsupported key type" + algorithm),
        };

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
        public static bool IsValidAlgorithmForGenerate(this PivAlgorithm algorithm) => algorithm switch
        {
            PivAlgorithm.Rsa1024 => true,
            PivAlgorithm.Rsa2048 => true,
            PivAlgorithm.Rsa3072 => true,
            PivAlgorithm.Rsa4096 => true,
            PivAlgorithm.EccP256 => true,
            PivAlgorithm.EccP384 => true,
            PivAlgorithm.Ed25519 => true,
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
            return algorithm switch
            {
                PivAlgorithm.Rsa1024 or
                PivAlgorithm.Rsa2048 or
                PivAlgorithm.Rsa3072 or
                PivAlgorithm.Rsa4096 or
                PivAlgorithm.EccP256 or
                PivAlgorithm.EccP384 => algorithm.GetByKeyDefinitionKeyType().KeyDefinition.LengthInBits,
                PivAlgorithm.TripleDes => 192,
                PivAlgorithm.Pin => 64,
                _ => 0,
            };
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
        public static bool IsRsa(this PivAlgorithm algorithm) => algorithm.GetByKeyDefinitionKeyType().KeyDefinition.IsRsaKey;

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
        public static bool IsEcc(this PivAlgorithm algorithm) => algorithm.GetByKeyDefinitionKeyType().KeyDefinition.IsEcKey;
    }
}
