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

using System.Collections.Frozen;
using System.Globalization;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography.Cose;

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>
/// Provides definitions for cryptographic keys, including their types, lengths, and other properties.
/// </summary>
public static class KeyDefinitions
{
    private static readonly FrozenDictionary<KeyType, KeyDefinition> AllDefinitions;

    static KeyDefinitions()
    {
        AllDefinitions = new Dictionary<KeyType, KeyDefinition>
        {
            { KeyType.P256, P256 },
            { KeyType.P384, P384 },
            { KeyType.P521, P521 },
            { KeyType.X25519, X25519 },
            { KeyType.Ed25519, Ed25519 },
            { KeyType.Rsa1024, Rsa1024 },
            { KeyType.Rsa2048, Rsa2048 },
            { KeyType.Rsa3072, Rsa3072 },
            { KeyType.Rsa4096, Rsa4096 },
            { KeyType.Aes128, Aes128 },
            { KeyType.Aes192, Aes192 },
            { KeyType.Aes256, Aes256 },
            { KeyType.TripleDes, TripleDes },
        }.ToFrozenDictionary();
    }

    /// <summary>
    /// Gets all key definitions.
    /// </summary>
    /// <returns>
    /// A collection of key definitions.
    /// </returns>
    public static IReadOnlyDictionary<KeyType, KeyDefinition> All => AllDefinitions;

    /// <summary>
    /// Gets a key definition by its type.
    /// </summary>
    /// <param name="type">
    /// The type of the key.
    /// </param>
    /// <returns>
    /// The key definition for the specified key type.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// When the key type is not supported.
    /// </exception>
    public static KeyDefinition GetByKeyType(KeyType type)
    {
        if (!AllDefinitions.TryGetValue(type, out var definition))
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "Unsupported key type '{0}'.",
                    type),
                nameof(type));
        }

        return definition;
    }

    /// <summary>
    /// Gets a key definition by its RSA key length.
    /// </summary>
    /// <param name="keySizeBits">
    /// The length of the RSA key in bits.
    /// </param>
    /// <returns>
    /// The key definition for the specified RSA key length.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// When the RSA key length is not supported.
    /// </exception>
    public static KeyDefinition GetByRsaLength(int keySizeBits)
    {
        foreach (var keyDef in GetRsaKeyDefinitions())
        {
            // Allow small variations in key size
            if (keySizeBits == keyDef.LengthInBits || Math.Abs(keySizeBits - keyDef.LengthInBits) <= 1)
            {
                return keyDef;
            }
        }

        throw new NotSupportedException($"Unsupported RSA length: {keySizeBits}");
    }

    public static KeyDefinition GetByRsaModulusLength(byte[] modulus) => GetByRsaLength(modulus.Length * 8);

    /// <summary>
    /// Gets a key definition by its curve type.
    /// </summary>
    /// <param name="curve">
    ///  The curve type of the key.
    /// </param>
    /// <returns>
    ///  The key definition for the specified curve type.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// When the curve type is not supported.
    /// </exception>
    public static KeyDefinition GetByCoseCurve(CoseEcCurve curve) =>
        AllDefinitions.Values.SingleOrDefault(
            d => d.CoseKeyDefinition is not null && d.CoseKeyDefinition.CurveIdentifier == curve)
        ?? throw new InvalidOperationException(
            string.Format(
                CultureInfo.CurrentCulture,
                "Unsupported COSE EC curve '{0}'.",
                curve));

    /// <summary>
    /// Gets a key definition by its object identifier (OID).
    /// </summary>
    /// <param name="oid">
    ///  The object identifier (OID) of the key.
    /// </param>
    /// <returns>
    ///  The key definition for the specified OID.
    /// </returns>
    /// <exception cref="NotSupportedException">
    ///  When the OID is not supported or when the OID is for an RSA key.
    /// </exception>
    public static KeyDefinition GetByOid(Oid oid) => GetByOid(oid.Value);

    /// <summary>
    /// Gets a key definition by its object identifier (OID).
    /// </summary>
    /// <param name="oid">
    ///  The object identifier (OID) of the key.
    /// </param>
    /// <returns>
    ///  The key definition for the specified OID.
    /// </returns>
    /// <exception cref="NotSupportedException">
    ///  When the OID is not supported or when the OID is for an RSA key.
    /// </exception>
    public static KeyDefinition GetByOid(string? oid)
    {
        if (string.IsNullOrEmpty(oid))
        {
            throw new ArgumentNullException(nameof(oid));
        }

        if (string.Equals(oid, Oids.Rsa, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                "RSA keys are not supported by this method as all RSA keys share the same OID.");
        }

        if (string.Equals(oid, Oids.EcPublicKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                "The id-ecPublicKey algorithm OID does not identify a curve. Use the curve OID instead.");
        }

        var keyDefinition = AllDefinitions.Values.FirstOrDefault(d => d.AlgorithmOid == oid || d.CurveOid == oid);
        return keyDefinition ?? throw new NotSupportedException(
            string.Format(
                CultureInfo.CurrentCulture,
                "Unsupported algorithm or curve OID '{0}'.",
                oid));
    }

    public static KeyType GetKeyTypeByOid(Oid algorithmOid) => GetKeyTypeByOid(algorithmOid.Value);

    public static KeyType GetKeyTypeByOid(string? algorithmOid) => GetByOid(algorithmOid).KeyType;

    /// <summary>
    /// Gets all RSA key definitions.
    /// </summary>
    /// <returns>
    ///  A collection of RSA key definitions.
    /// </returns>
    public static IReadOnlyCollection<KeyDefinition> GetRsaKeyDefinitions() =>
        AllDefinitions.Values.Where(d => d.IsRsa).ToList();

    /// <summary>
    /// Gets all elliptic curve (EC) key definitions.
    /// </summary>
    /// <returns>
    ///  A collection of EC key definitions.
    /// </returns>
    public static IReadOnlyCollection<KeyDefinition> GetEcKeyDefinitions() =>
        AllDefinitions.Values.Where(d => d.IsEllipticCurve).ToList();

    /// <summary>
    /// Represents an EC key with a length of 256 bits.
    /// </summary>
    public static readonly KeyDefinition P256 = new KeyDefinition
    {
        KeyType = KeyType.P256,
        DisplayName = "P-256",
        LengthInBytes = 32,
        LengthInBits = 256,
        AlgorithmOid = Oids.EcPublicKey,
        CurveOid = Oids.P256,
        CoseKeyDefinition = new CoseKeyDefinition
        {
            Type = CoseKeyType.Ec2,
            CurveIdentifier = CoseEcCurve.P256
        }
    };

    /// <summary>
    /// Represents an EC key with a length of 384 bits.
    /// </summary>
    public static readonly KeyDefinition P384 = new KeyDefinition
    {
        KeyType = KeyType.P384,
        DisplayName = "P-384",
        LengthInBytes = 48,
        LengthInBits = 384,
        AlgorithmOid = Oids.EcPublicKey,
        CurveOid = Oids.P384,
        CoseKeyDefinition = new CoseKeyDefinition
        {
            Type = CoseKeyType.Ec2,
            CurveIdentifier = CoseEcCurve.P384
        }
    };

    /// <summary>
    /// Represents an EC key with a length of 521 bits.
    /// </summary>
    public static readonly KeyDefinition P521 = new KeyDefinition
    {
        KeyType = KeyType.P521,
        DisplayName = "P-521",
        LengthInBytes = 66,
        LengthInBits = 521,
        AlgorithmOid = Oids.EcPublicKey,
        CurveOid = Oids.P521,
        CoseKeyDefinition = new CoseKeyDefinition
        {
            Type = CoseKeyType.Ec2,
            CurveIdentifier = CoseEcCurve.P521
        }
    };

    /// <summary>
    /// Represents an X25519 key.
    /// </summary>
    public static readonly KeyDefinition X25519 = new KeyDefinition
    {
        KeyType = KeyType.X25519,
        DisplayName = "X25519",
        LengthInBytes = 32,
        LengthInBits = 256,
        AlgorithmOid = Oids.X25519,
        CoseKeyDefinition = new CoseKeyDefinition
        {
            Type = CoseKeyType.Okp,
            CurveIdentifier = CoseEcCurve.X25519
        }
    };

    /// <summary>
    /// Represents an Ed25519 key.
    /// </summary>
    public static readonly KeyDefinition Ed25519 = new KeyDefinition
    {
        KeyType = KeyType.Ed25519,
        DisplayName = "Ed25519",
        LengthInBytes = 32,
        LengthInBits = 256,
        AlgorithmOid = Oids.Ed25519,
        CoseKeyDefinition = new CoseKeyDefinition
        {
            Type = CoseKeyType.Okp,
            CurveIdentifier = CoseEcCurve.Ed25519
        }
    };

    /// <summary>
    ///  Represents an RSA key with a length of 1024 bits.
    /// </summary>
    public static readonly KeyDefinition Rsa1024 = new KeyDefinition
    {
        KeyType = KeyType.Rsa1024,
        DisplayName = "RSA-1024",
        CoseKeyDefinition = new CoseKeyDefinition { Type = CoseKeyType.Rsa },
        LengthInBytes = 128,
        LengthInBits = 1024,
        AlgorithmOid = Oids.Rsa,
    };

    /// <summary>
    ///  Represents an RSA key with a length of 2048 bits.
    /// </summary>
    public static readonly KeyDefinition Rsa2048 = new KeyDefinition
    {
        KeyType = KeyType.Rsa2048,
        DisplayName = "RSA-2048",
        CoseKeyDefinition = new CoseKeyDefinition { Type = CoseKeyType.Rsa },
        LengthInBytes = 256,
        LengthInBits = 2048,
        AlgorithmOid = Oids.Rsa,
    };

    /// <summary>
    ///  Represents an RSA key with a length of 3072 bits.
    /// </summary>
    public static readonly KeyDefinition Rsa3072 = new KeyDefinition
    {
        KeyType = KeyType.Rsa3072,
        DisplayName = "RSA-3072",
        CoseKeyDefinition = new CoseKeyDefinition { Type = CoseKeyType.Rsa },
        LengthInBytes = 384,
        LengthInBits = 3072,
        AlgorithmOid = Oids.Rsa,
    };

    /// <summary>
    /// Represents an RSA key with a length of 4096 bits.
    /// </summary>
    public static readonly KeyDefinition Rsa4096 = new KeyDefinition
    {
        KeyType = KeyType.Rsa4096,
        DisplayName = "RSA-4096",
        CoseKeyDefinition = new CoseKeyDefinition { Type = CoseKeyType.Rsa },
        LengthInBytes = 512,
        LengthInBits = 4096,
        AlgorithmOid = Oids.Rsa,
    };

    /// <summary>
    /// Represents an AES key with a length of 128 bits.
    /// </summary>
    public static readonly KeyDefinition Aes128 = new KeyDefinition
    {
        KeyType = KeyType.Aes128,
        DisplayName = "AES-128",
        CoseKeyDefinition = new CoseKeyDefinition { Type = CoseKeyType.Symmetric },
        LengthInBytes = 16,
        LengthInBits = 128,
        AlgorithmOid = Oids.Aes128Cbc,
    };

    /// <summary>
    /// Represents an AES key with a length of 192 bits.
    /// </summary>
    public static readonly KeyDefinition Aes192 = new KeyDefinition
    {
        KeyType = KeyType.Aes192,
        DisplayName = "AES-192",
        CoseKeyDefinition = new CoseKeyDefinition { Type = CoseKeyType.Symmetric },
        LengthInBytes = 24,
        LengthInBits = 192,
        AlgorithmOid = Oids.Aes192Cbc,
    };

    /// <summary>
    /// Represents an AES key with a length of 256 bits.
    /// </summary>
    public static readonly KeyDefinition Aes256 = new KeyDefinition
    {
        KeyType = KeyType.Aes256,
        DisplayName = "AES-256",
        CoseKeyDefinition = new CoseKeyDefinition { Type = CoseKeyType.Symmetric },
        LengthInBytes = 32,
        LengthInBits = 256,
        AlgorithmOid = Oids.Aes256Cbc,
    };

    /// <summary>
    /// Represents a Triple DES key with a length of 192 bits.
    /// </summary>
    public static readonly KeyDefinition TripleDes = new KeyDefinition
    {
        KeyType = KeyType.TripleDes,
        DisplayName = "Triple DES",
        LengthInBytes = 24,
        LengthInBits = 192,
        AlgorithmOid = Oids.TripleDesCbc,
    };
}