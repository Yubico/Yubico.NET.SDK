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
using System.Collections.Generic;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv;

/// <summary>
///     Represents the connection between a PIV algorithm and its corresponding key definition.
/// </summary>
public record PivAlgorithmDefinition
{
    /// <summary>
    ///     The PIV algorithm associated with this definition.
    /// </summary>
    public required PivAlgorithm Algorithm { get; init; }

    /// <summary>
    ///     The key definition associated with this algorithm.
    /// </summary>
    public required KeyDefinition KeyDefinition { get; init; }

    /// <summary>
    ///     Indicates whether this algorithm supports key pair generation on the YubiKey.
    /// </summary>
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
            _ => false
        };
}

/// <summary>
///     Extension methods for working with the PivAlgorithm enum.
/// </summary>
public static class PivAlgorithmExtensions
{
    // Mapping between PIV algorithms and key definitions
    private static readonly Dictionary<PivAlgorithm, PivAlgorithmDefinition> _pivAlgorithmMap;
    private static readonly Dictionary<KeyType, PivAlgorithm> _keyTypeToPivAlgorithmMap;

    static PivAlgorithmExtensions()
    {
        // Initialize mappings
        _pivAlgorithmMap = new Dictionary<PivAlgorithm, PivAlgorithmDefinition>
        {
            {
                PivAlgorithm.Rsa1024, new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.Rsa1024, KeyDefinition = KeyDefinitions.RSA1024 }
            },
            {
                PivAlgorithm.Rsa2048, new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.Rsa2048, KeyDefinition = KeyDefinitions.RSA2048 }
            },
            {
                PivAlgorithm.Rsa3072, new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.Rsa3072, KeyDefinition = KeyDefinitions.RSA3072 }
            },
            {
                PivAlgorithm.Rsa4096, new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.Rsa4096, KeyDefinition = KeyDefinitions.RSA4096 }
            },
            {
                PivAlgorithm.EccP256, new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.EccP256, KeyDefinition = KeyDefinitions.P256 }
            },
            {
                PivAlgorithm.EccP384, new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.EccP384, KeyDefinition = KeyDefinitions.P384 }
            },
            {
                PivAlgorithm.EccP521, new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.EccP521, KeyDefinition = KeyDefinitions.P521 }
            },
            {
                PivAlgorithm.EccEd25519, new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.EccEd25519, KeyDefinition = KeyDefinitions.Ed25519 }
            },
            {
                PivAlgorithm.EccX25519, new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.EccX25519, KeyDefinition = KeyDefinitions.X25519 }
            },
            {
                PivAlgorithm.Aes128, new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.Aes128, KeyDefinition = KeyDefinitions.AES128 }
            },
            {
                PivAlgorithm.Aes192, new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.Aes192, KeyDefinition = KeyDefinitions.AES192 }
            },
            {
                PivAlgorithm.Aes256, new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.Aes256, KeyDefinition = KeyDefinitions.AES256 }
            },
            {
                PivAlgorithm.TripleDes, new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.TripleDes, KeyDefinition = KeyDefinitions.TripleDes }
            },
            {
                PivAlgorithm.None, new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.None, KeyDefinition = new KeyDefinition() }
            },
            {
                PivAlgorithm.Pin, new PivAlgorithmDefinition
                    { Algorithm = PivAlgorithm.Pin, KeyDefinition = new KeyDefinition() }
            }
        };

        // Create reverse mapping (KeyType to PivAlgorithm)
        _keyTypeToPivAlgorithmMap = new Dictionary<KeyType, PivAlgorithm>
        {
            { KeyType.None, PivAlgorithm.None },
            { KeyType.RSA1024, PivAlgorithm.Rsa1024 },
            { KeyType.RSA2048, PivAlgorithm.Rsa2048 },
            { KeyType.RSA3072, PivAlgorithm.Rsa3072 },
            { KeyType.RSA4096, PivAlgorithm.Rsa4096 },
            { KeyType.ECP256, PivAlgorithm.EccP256 },
            { KeyType.ECP384, PivAlgorithm.EccP384 },
            { KeyType.ECP521, PivAlgorithm.EccP521 },
            { KeyType.Ed25519, PivAlgorithm.EccEd25519 },
            { KeyType.X25519, PivAlgorithm.EccX25519 },
            { KeyType.AES128, PivAlgorithm.Aes128 },
            { KeyType.AES192, PivAlgorithm.Aes192 },
            { KeyType.AES256, PivAlgorithm.Aes256 },
            { KeyType.TripleDES, PivAlgorithm.TripleDes }
        };
    }

    /// <summary>
    ///     Gets the PivAlgorithmDefinition for a given KeyDefinition.
    /// </summary>
    /// <param name="keyDefinition">The key definition to look up.</param>
    /// <returns>The corresponding PivAlgorithmDefinition.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no matching algorithm definition is found.</exception>
    public static PivAlgorithmDefinition GetPivKeyDefinition(this KeyDefinition keyDefinition)
    {
        if (!_keyTypeToPivAlgorithmMap.TryGetValue(keyDefinition.KeyType, out var algorithm))
        {
            throw new InvalidOperationException(
                $"No PIV algorithm mapping found for key type: {keyDefinition.KeyType}");
        }

        return _pivAlgorithmMap[algorithm];
    }

    /// <summary>
    ///     Gets the PivAlgorithmDefinition for a given PivAlgorithm.
    /// </summary>
    /// <param name="algorithm">The PIV algorithm to look up.</param>
    /// <returns>The corresponding PivAlgorithmDefinition, or null if not found.</returns>
    public static PivAlgorithmDefinition GetPivKeyDefinition(this PivAlgorithm algorithm) =>
        _pivAlgorithmMap.TryGetValue(algorithm, out var definition)
            ? definition
            : throw new NotSupportedException($"Unsupported algorithm: {algorithm}");

    /// <summary>
    ///     Converts a PivAlgorithm to its corresponding KeyType.
    /// </summary>
    /// <param name="pivAlgorithm">The PIV algorithm to convert.</param>
    /// <returns>The corresponding KeyType.</returns>
    /// <exception cref="NotSupportedException">Thrown when the PIV algorithm cannot be mapped to a KeyType.</exception>
    public static KeyType GetKeyType(this PivAlgorithm pivAlgorithm)
    {
        if (!_pivAlgorithmMap.TryGetValue(pivAlgorithm, out var definition))
        {
            throw new NotSupportedException($"Unsupported PIV algorithm: {pivAlgorithm}");
        }

        return definition.KeyDefinition.KeyType;
    }

    /// <summary>
    ///     Converts a KeyType to its corresponding PivAlgorithm.
    /// </summary>
    /// <param name="keyType">The key type to convert.</param>
    /// <returns>The corresponding PivAlgorithm.</returns>
    /// <exception cref="NotSupportedException">Thrown when the key type is not supported by PIV.</exception>
    public static PivAlgorithm GetPivAlgorithm(this KeyType keyType)
    {
        if (!_keyTypeToPivAlgorithmMap.TryGetValue(keyType, out var algorithm))
        {
            throw new NotSupportedException($"Unsupported key type: {keyType}");
        }

        return algorithm;
    }

    /// <summary>
    ///     Determines if the given algorithm is one that can be used to generate
    ///     a key pair.
    /// </summary>
    /// <remarks>
    ///     The PivAlgorithm enum contains Triple-DES and other algorithms, which
    ///     cannot be used as an algorithm to generate a key pair. Make sure the
    ///     given algorithm is one that can be used when generating a key pair.
    ///     <para>
    ///         This also works if checking an algorithm for import, signing, or
    ///         decrypting/key exchange.
    ///     </para>
    /// </remarks>
    /// <param name="algorithm">
    ///     The algorithm name to check.
    /// </param>
    /// <returns>
    ///     A boolean, true if the algorithm is one that can be used to generate
    ///     a key pair, and false otherwise.
    /// </returns>
    [Obsolete("Use algorithm.GetPivKeyDef()?.SupportsKeyGeneration ?? false instead")]
    public static bool IsValidAlgorithmForGenerate(this PivAlgorithm algorithm) =>
        algorithm.GetPivKeyDefinition()?.SupportsKeyGeneration ?? false;

    /// <summary>
    ///     The size of a key, in bits, of the given algorithm.
    /// </summary>
    /// <remarks>
    ///     The PivAlgorithm enum specifies algorithm and key size for RSA and
    ///     ECC. If you have a variable of type <c>PivAlgorithm</c>, use this
    ///     extension to get the bit size out.
    ///     <para>
    ///         For example, suppose you obtain a public key from storage, and have a
    ///         <see cref="PivPublicKey" /> object. Maybe your code performs different
    ///         tasks based on the key size (e.g. use SHA-256 or SHA-384, or build a
    ///         buffer for signing). You can look at the <c>Algorithm</c> property to
    ///         learn the algorithm and key size. However, if all you want is the key
    ///         size, use this extension:
    ///         <code language="csharp">
    ///    PivPublicKey publicKey = SomeClass.GetPublicKey(someSearchParam);
    ///    byte[] buffer = new byte[publicKey.Algorithm.KeySizeBits() / 8];
    /// </code>
    ///     </para>
    ///     <para>
    ///         This will return the following values for each value of
    ///         <c>PivAlgorithm</c>.
    ///         <code>
    ///     Rsa1024    1024
    ///     Rsa2048    2048
    ///     Rsa3072    3072
    ///     Rsa4096    4096
    ///     EccP256     256
    ///     EccP384     384
    ///     EccP521     521
    ///     EccEd25519  256
    ///     EccX25519   256
    ///     Aes128      128
    ///     Aes192      192
    ///     Aes256      256
    ///     TripleDes   192
    ///     Pin          64
    ///     None          0
    /// </code>
    ///         Note that a Triple-DES key is made up of three DES keys, and each DES
    ///         key is 8 bytes (64 bits). However, because there are 8 "parity bits"
    ///         in each DES key, the actual key strength of a DES key is 56 bits.
    ///         That means the actual key strength of a Triple-DES key is 168 bits. In
    ///         addition, because of certain attacks, it is possible to reduce the
    ///         strength of a Triple-DES key to 112 bits (it takes the equivalent of
    ///         a 112-bit brute-force attack to break a Triple-DES key). Nonetheless,
    ///         this extension will return 192 as the key length, in bits, of a
    ///         Triple-DES key.
    ///     </para>
    ///     <para>
    ///         A PIN or PUK is 6 to 8 bytes long. Hence, the maximum size, in bits,
    ///         of a <c>PivAlgorithm.Pin</c> is 64.
    ///     </para>
    /// </remarks>
    /// <param name="algorithm">
    ///     The algorithm name to check.
    /// </param>
    /// <returns>
    ///     An int, the size, in bits, of a key of the given algorithm.
    /// </returns>
    public static int KeySizeBits(this PivAlgorithm algorithm)
    {
        // Special handling for PIN algorithm
        if (algorithm == PivAlgorithm.Pin)
        {
            return 64;
        }

        if (algorithm == PivAlgorithm.None)
        {
            return 0;
        }

        // Try to get the key definition
        var keyDefinition = algorithm.GetPivKeyDefinition();
        return keyDefinition.KeyDefinition.LengthInBits;
    }

    /// <summary>
    ///     Determines if the given algorithm is RSA.
    /// </summary>
    /// <remarks>
    ///     The PivAlgorithm enum contains <c>Rsa1024</c>, <c>Rsa2048</c>, <c>Rsa3072</c>, and <c>Rsa4096</c>. But
    ///     sometimes you just want to know if an algorithm is RSA or not. It
    ///     would seem you would have to write code such as the following.
    ///     <code language="csharp">
    ///     if ((algorithm == PivAlgorith.Rsa1024) || (algorithm == PivAlgorithm.Rsa2048) || (algorithm == PivAlgorithm.Rsa3072) || (algorithm == PivAlgorithm.Rsa4096))
    /// </code>
    ///     <para>
    ///         With this extension, you can simply write.
    ///         <code language="csharp">
    ///     if (algorithm.IsRsa())
    /// </code>
    ///     </para>
    /// </remarks>
    /// <param name="algorithm">
    ///     The algorithm to check.
    /// </param>
    /// <returns>
    ///     A boolean, true if the algorithm is RSA, and false otherwise.
    /// </returns>
    public static bool IsRsa(this PivAlgorithm algorithm) => algorithm.GetKeyType().IsRSA();

    /// <summary>
    ///     Determines if the given algorithm is ECC.
    /// </summary>
    /// <remarks>
    ///     The PivAlgorithm enum contains <c>EccP256</c> and <c>EccP384</c>. But
    ///     sometimes you just want to know if an algorithm is ECC or not. It
    ///     would seem you would have to write code such as the following.
    ///     <code language="csharp">
    ///     if ((algorithm == PivAlgorith.EccP256) || (algorithm == PivAlgorithm.ECCP384))
    /// </code>
    ///     <para>
    ///         With this extension, you can simply write.
    ///         <code language="csharp">
    ///     if (algorithm.IsEcc())
    /// </code>
    ///     </para>
    /// </remarks>
    /// <param name="algorithm">
    ///     The algorithm to check.
    /// </param>
    /// <returns>
    ///     A boolean, true if the algorithm is ECC, and false otherwise.
    /// </returns>
    public static bool IsEcc(this PivAlgorithm algorithm) => algorithm.GetKeyType().IsEllipticCurve();
}
