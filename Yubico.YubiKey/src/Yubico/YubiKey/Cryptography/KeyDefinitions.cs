// Copyright 2024 Yubico AB
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
using Yubico.YubiKey.Fido2.Cose; // Ideally, Cose definitions should move to this namespace

namespace Yubico.YubiKey.Cryptography
{
    /// <summary>
    /// Provides definitions for cryptographic keys, including their types, lengths, and other properties.
    /// </summary>
    public static class KeyDefinitions
    {
        private static readonly Dictionary<KeyType, KeyDefinition> AllDefinitions;

        static KeyDefinitions()
        {
            AllDefinitions = new Dictionary<KeyType, KeyDefinition>
            {
                { KeyType.P256, P256 },
                { KeyType.P384, P384 },
                { KeyType.P521, P521 },
                { KeyType.X25519, X25519 },
                { KeyType.Ed25519, Ed25519 },
                { KeyType.RSA1024, RSA1024 },
                { KeyType.RSA2048, RSA2048 },
                { KeyType.RSA3072, RSA3072 },
                { KeyType.RSA4096, RSA4096 },
                { KeyType.AES128, AES128 },
                { KeyType.AES192, AES192 },
                { KeyType.AES256, AES256 },
                { KeyType.TripleDes, TripleDes },
            };
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
        /// <exception cref="InvalidOperationException">
        /// When the key type is not supported.
        /// </exception>
        public static KeyDefinition GetByKeyType(KeyType type)
        {
            if (!AllDefinitions.TryGetValue(type, out var definition))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.UnsupportedAlgorithm,
                        type));
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
        public static KeyDefinition GetByRSALength(int keySizeBits)
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
                d => d.CoseKeyDefinition != null && d.CoseKeyDefinition.CurveIdentifier == curve)
            ?? throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.UnsupportedAlgorithm,
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
        public static KeyDefinition GetByOid(string oid)
        {
            if (string.Equals(oid, Oids.RSA, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(
                    "RSA keys are not supported by this method as all RSA keys share the same OID.");
            }

            var keyDefinition = AllDefinitions.Values.FirstOrDefault(d => d.AlgorithmOid == oid || d.CurveOid == oid);
            return keyDefinition ?? throw new NotSupportedException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.UnsupportedAlgorithm));
        }

        public static KeyType GetKeyTypeByOid(Oid algorithmOid) => GetKeyTypeByOid(algorithmOid.Value);

        public static KeyType GetKeyTypeByOid(string algorithmOid)
        {
            var keyType = GetByOid(algorithmOid).KeyType;
            return keyType;
        }

        /// <summary>
        /// Gets all RSA key definitions.
        /// </summary>
        /// <returns>
        ///  A collection of RSA key definitions.
        /// </returns>
        public static IReadOnlyCollection<KeyDefinition> GetRsaKeyDefinitions() =>
            AllDefinitions.Values.Where(d => d.IsRsaKey).ToList();

        /// <summary>
        /// Gets all elliptic curve (EC) key definitions.
        /// </summary>
        /// <returns>
        ///  A collection of EC key definitions.
        /// </returns>
        public static IReadOnlyCollection<KeyDefinition> GetEcKeyDefinitions() =>
            AllDefinitions.Values.Where(d => d.IsEcKey).ToList();

        /// <summary>
        /// Contains OIDs for cryptographic algorithms used in key operations.
        /// </summary>
        public static class Oids
        {
            /// <summary>
            /// RSA Encryption algorithm OID (PKCS#1)
            /// </summary>
            public const string RSA = "1.2.840.113549.1.1.1";

            /// <summary>
            /// Represents the general Elliptic Curve public key algorithm OID (ANSI X9.62)
            /// </summary>
            public const string ECDSA = "1.2.840.10045.2.1";

            /// <summary>
            /// Represents the OID for X25519 (Curve25519) used for key exchange
            /// </summary>
            public const string X25519 = "1.3.101.110";

            /// <summary>
            /// Represents the OID for Ed25519 (Edwards25519) used for signatures
            /// </summary>
            public const string Ed25519 = "1.3.101.112";

            /// <summary>
            /// Represents the OID for NIST P-256 curve (also known as secp256r1)
            /// </summary>
            public const string P256 = "1.2.840.10045.3.1.7";

            /// <summary>
            /// Represents the OID for NIST P-384 curve (also known as secp384r1)
            /// </summary>
            public const string P384 = "1.3.132.0.34";

            /// <summary>
            /// Represents the OID for NIST P-521 curve (also known as secp521r1)
            /// </summary>
            public const string P521 = "1.3.132.0.35";
            
            /// <summary>
            /// Represents the OID for AES-128 in CBC mode
            /// </summary>
            public const string Aes128Cbc = "2.16.840.1.101.3.4.1.2";
            
            /// <summary>
            /// Represents the OID for AES-192 in CBC mode
            /// </summary>
            public const string Aes192Cbc = "2.16.840.1.101.3.4.1.22";
            
            /// <summary>
            /// Represents the OID for AES-256 in CBC mode
            /// </summary>
            public const string Aes256Cbc = "2.16.840.1.101.3.4.1.42";
            
            /// <summary>
            /// Represents the OID for Triple DES in CBC mode
            /// </summary>
            public const string TripleDesCbc = "1.2.840.113549.3.7";

            /// <summary>
            /// Gets the algorithm and curve OIDs for a specific key type
            /// </summary>
            public static (string AlgorithmOid, string? Curveoid) GetOidsForKeyType(KeyType keyType)
            {
                return keyType switch
                {
                    KeyType.RSA1024 => (RSA, null),
                    KeyType.RSA2048 => (RSA, null),
                    KeyType.RSA3072 => (RSA, null),
                    KeyType.RSA4096 => (RSA, null),

                    KeyType.P256 => (EC: ECDSA, P256),
                    KeyType.P384 => (EC: ECDSA, P384),
                    KeyType.P521 => (EC: ECDSA, P521),

                    KeyType.X25519 => (X25519, null),
                    KeyType.Ed25519 => (Ed25519, null),
                    
                    KeyType.AES128 => (Aes128Cbc, null),
                    KeyType.AES192 => (Aes192Cbc, null),
                    KeyType.AES256 => (Aes256Cbc, null),
                    KeyType.TripleDes => (TripleDesCbc, null),
                    
                    _ => throw new ArgumentException($"Unsupported key type: {keyType}")
                };
            }
        }

        /// <summary>
        /// Represents an EC key with a length of 256 bits.
        /// </summary>
        public static readonly KeyDefinition P256 = new KeyDefinition
        {
            KeyType = KeyType.P256,
            LengthInBytes = 32,
            LengthInBits = 256,
            AlgorithmOid = Oids.ECDSA,
            CurveOid = Oids.P256,
            IsEcKey = true,
            CoseKeyDefinition = new CoseKeyDefinition
            {
                Type = CoseKeyType.Ec2,
                CurveIdentifier = CoseEcCurve.P256,
                AlgorithmIdentifier = CoseAlgorithmIdentifier.ES256
            }
        };

        /// <summary>
        /// Represents an EC key with a length of 384 bits.
        /// </summary>
        public static readonly KeyDefinition P384 = new KeyDefinition
        {
            KeyType = KeyType.P384,
            LengthInBytes = 48,
            LengthInBits = 384,
            AlgorithmOid = Oids.ECDSA,
            CurveOid = Oids.P384,
            IsEcKey = true,
            CoseKeyDefinition = new CoseKeyDefinition
            {
                Type = CoseKeyType.Ec2,
                CurveIdentifier = CoseEcCurve.P384,
                AlgorithmIdentifier = CoseAlgorithmIdentifier.ES384
            }
        };

        /// <summary>
        /// Represents an EC key with a length of 521 bits.
        /// </summary>
        public static readonly KeyDefinition P521 = new KeyDefinition
        {
            KeyType = KeyType.P521,
            LengthInBytes = 66,
            LengthInBits = 521,
            AlgorithmOid = Oids.ECDSA,
            CurveOid = Oids.P521,
            IsEcKey = true,
            CoseKeyDefinition = new CoseKeyDefinition
            {
                Type = CoseKeyType.Ec2,
                CurveIdentifier = CoseEcCurve.P521,
                AlgorithmIdentifier = CoseAlgorithmIdentifier.ES512
            }
        };

        /// <summary>
        /// Represents an X25519 key.
        /// </summary>
        public static readonly KeyDefinition X25519 = new KeyDefinition
        {
            KeyType = KeyType.X25519,
            LengthInBytes = 32,
            LengthInBits = 256,
            AlgorithmOid = Oids.X25519,
            IsEcKey = true,
            CoseKeyDefinition = new CoseKeyDefinition
            {
                Type = CoseKeyType.Okp,
                CurveIdentifier = CoseEcCurve.X25519,
                AlgorithmIdentifier = CoseAlgorithmIdentifier.ECDHwHKDF256 // Hmm? Really?
            }
        };

        /// <summary>
        /// Represents an Ed25519 key.
        /// </summary>
        public static readonly KeyDefinition Ed25519 = new KeyDefinition
        {
            KeyType = KeyType.Ed25519,
            LengthInBytes = 32,
            LengthInBits = 256,
            AlgorithmOid = Oids.Ed25519,
            IsEcKey = true,
            CoseKeyDefinition = new CoseKeyDefinition
            {
                Type = CoseKeyType.Okp,
                CurveIdentifier = CoseEcCurve.Ed25519,
                AlgorithmIdentifier = CoseAlgorithmIdentifier.EdDSA
            }
        };

        /// <summary>
        ///  Represents an RSA key with a length of 1024 bits.
        /// </summary>
        public static readonly KeyDefinition RSA1024 = new KeyDefinition
        {
            KeyType = KeyType.RSA1024,
            LengthInBytes = 128,
            LengthInBits = 1024,
            AlgorithmOid = Oids.RSA,
            IsRsaKey = true
        };

        /// <summary>
        ///  Represents an RSA key with a length of 2048 bits.
        /// </summary>
        public static readonly KeyDefinition RSA2048 = new KeyDefinition
        {
            KeyType = KeyType.RSA2048,
            LengthInBytes = 256,
            LengthInBits = 2048,
            AlgorithmOid = Oids.RSA,
            IsRsaKey = true
        };

        /// <summary>
        ///  Represents an RSA key with a length of 3072 bits.
        /// </summary>
        public static readonly KeyDefinition RSA3072 = new KeyDefinition
        {
            KeyType = KeyType.RSA3072,
            LengthInBytes = 384,
            LengthInBits = 3072,
            AlgorithmOid = Oids.RSA,
            IsRsaKey = true
        };

        /// <summary>
        /// Represents an RSA key with a length of 4096 bits.
        /// </summary>
        public static readonly KeyDefinition RSA4096 = new KeyDefinition
        {
            KeyType = KeyType.RSA4096,
            LengthInBytes = 512,
            LengthInBits = 4096,
            AlgorithmOid = Oids.RSA,
            IsRsaKey = true
        };

        /// <summary>
        /// Represents an AES key with a length of 128 bits.
        /// </summary>
        public static readonly KeyDefinition AES128 = new KeyDefinition
        {
            KeyType = KeyType.AES128,
            LengthInBytes = 16,
            LengthInBits = 128,
            AlgorithmOid = Oids.Aes128Cbc,
            IsSymmetricKey = true
        };

        /// <summary>
        /// Represents an AES key with a length of 192 bits.
        /// </summary>
        public static readonly KeyDefinition AES192 = new KeyDefinition
        {
            KeyType = KeyType.AES192,
            LengthInBytes = 24, 
            LengthInBits = 192,
            AlgorithmOid = Oids.Aes192Cbc,
            IsSymmetricKey = true
        };

        /// <summary>
        /// Represents an AES key with a length of 256 bits.
        /// </summary>
        public static readonly KeyDefinition AES256 = new KeyDefinition
        {
            KeyType = KeyType.AES256,
            LengthInBytes = 32,
            LengthInBits = 256,
            AlgorithmOid = Oids.Aes256Cbc,
            IsSymmetricKey = true
        };

        /// <summary>
        /// Represents a Triple DES key with a length of 192 bits.
        /// </summary>
        public static readonly KeyDefinition TripleDes = new KeyDefinition
        {
            KeyType = KeyType.TripleDes,
            LengthInBytes = 24,
            LengthInBits = 192,
            AlgorithmOid = Oids.TripleDesCbc,
            IsSymmetricKey = true
        };
    }

    /// <summary>
    /// Represents the definition of a cryptographic key, including its type, length, and other properties.
    /// </summary>
    public class KeyDefinition
    {
        /// <summary>
        /// Gets or sets the type of the key.
        /// </summary>
        public KeyType KeyType { get; init; }

        /// <summary>
        /// Gets or sets the length of the key in bytes.
        /// </summary>
        public int LengthInBytes { get; init; }

        /// <summary>
        /// Gets or sets the length of the key in bits.
        /// </summary>
        public int LengthInBits { get; init; }

        /// <summary>
        /// Gets or sets the object identifier (OID) of the key.
        /// </summary>
        public string AlgorithmOid { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the curve OID for elliptic curve keys.
        /// </summary>
        public string? CurveOid { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether the key is an elliptic curve (EC) key.
        /// </summary>
        public bool IsEcKey { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether the key is an RSA key.
        /// </summary>
        public bool IsRsaKey { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether the key is a symmetric key.
        /// </summary>
        public bool IsSymmetricKey { get; init; }

        /// <summary>
        /// Gets the name of the key, which is the string representation of the key type.
        /// </summary>
        public string Name => KeyType.ToString();

        /// <summary>
        /// Gets or sets the COSE key definition associated with this key.
        /// </summary>
        public CoseKeyDefinition? CoseKeyDefinition { get; init; }

        public override string ToString() => $"{Name} ({LengthInBits} bits)";

        public override bool Equals(object? obj) =>
            obj is KeyDefinition other &&
            KeyType == other.KeyType &&
            LengthInBytes == other.LengthInBytes &&
            LengthInBits == other.LengthInBits &&
            AlgorithmOid == other.AlgorithmOid &&
            CurveOid == other.CurveOid &&
            IsEcKey == other.IsEcKey &&
            IsRsaKey == other.IsRsaKey &&
            IsSymmetricKey == other.IsSymmetricKey &&
            CoseKeyDefinition == other.CoseKeyDefinition;

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(KeyType);
            hashCode.Add(LengthInBytes);
            hashCode.Add(LengthInBits);
            hashCode.Add(AlgorithmOid);
            hashCode.Add(CurveOid);
            hashCode.Add(IsEcKey);
            hashCode.Add(IsRsaKey);
            hashCode.Add(IsSymmetricKey);
            return hashCode.ToHashCode();
        }
    }

    /// <summary>
    /// COSE key definition
    /// <remarks>
    /// This class is based on the IANA COSE Key Common Parameters registry.
    /// <para>
    /// https://www.iana.org/assignments/cose/cose.xhtml
    /// </para>
    /// </remarks>
    /// </summary>
    public class CoseKeyDefinition
    {
        public CoseKeyType Type { get; set; } // kty - Key Type (1=OKP, 2=EC2)
        public CoseEcCurve CurveIdentifier { get; set; } // crv - Curve identifier
        public CoseAlgorithmIdentifier AlgorithmIdentifier { get; set; } // alg - Algorithm identifier
    }
}
