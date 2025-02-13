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
        private static readonly Dictionary<KeyType, KeyDefinition> _allDefinitions;

        static KeyDefinitions()
        {
            _allDefinitions = new Dictionary<KeyType, KeyDefinition>
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
            };
        }

        /// <summary>
        /// Gets all key definitions.
        /// </summary>
        /// <returns>
        /// A collection of key definitions.
        /// </returns>
        public static IReadOnlyDictionary<KeyType, KeyDefinition> AllDefinitions =>
            new Dictionary<KeyType, KeyDefinition>(_allDefinitions);

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
            if (!_allDefinitions.TryGetValue(type, out var definition))
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
             _allDefinitions.Values.SingleOrDefault(
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
            if (string.Equals(oid, KeyOids.Rsa, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(
                    "RSA keys are not supported by this method as all RSA keys share the same OID.");
            }

            return _allDefinitions.Values.SingleOrDefault(d => d.Oid == oid) ??
            throw new NotSupportedException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.UnsupportedAlgorithm));
        }


        /// <summary>
        /// Gets all RSA key definitions.
        /// </summary>
        /// <returns>
        ///  A collection of RSA key definitions.
        /// </returns>
        public static IReadOnlyCollection<KeyDefinition> GetRsaKeyDefinitions() =>
            _allDefinitions.Values.Where(d => d.IsRsaKey).ToList();

        /// <summary>
        /// Gets all elliptic curve (EC) key definitions.
        /// </summary>
        /// <returns>
        ///  A collection of EC key definitions.
        /// </returns>
        public static IReadOnlyCollection<KeyDefinition> GetEcKeyDefinitions() =>
            _allDefinitions.Values.Where(d => d.IsEcKey).ToList();

        /// <summary>
        /// Represents the object identifiers (OIDs) of cryptographic keys.
        /// </summary>
        public struct KeyOids
        {
            /// <summary>
            /// Represents the object identifier (OID) for RSA keys.
            /// <remarks>
            ///  All RSA keys share the same OID
            /// </remarks>
            /// </summary>
            public const string Rsa = "1.2.840.113549";

            /// <summary>
            /// Represents the object identifier (OID) for nistP256 or secP256r1
            /// </summary>
            public const string P256 = "1.2.840.10045.3.1.7";

            /// <summary>
            /// Represents the object identifier (OID) for nistP384 or secP384r1
            /// </summary>
            public const string P384 = "1.3.132.0.34";

            /// <summary>
            /// Represents the object identifier (OID) for nistP521 or secP521r1
            /// </summary>
            public const string P521 = "1.3.132.0.35";

            /// <summary>
            /// Represents the object identifier (OID) for X25519 (Curve25519)
            /// </summary>
            public const string X25519 = "1.3.101.110";

            /// <summary>
            /// Represents the object identifier (OID) for Ed25519 (Edwards25519)
            /// </summary>
            public const string Ed25519 = "1.3.101.112";
        }

        /// <summary>
        /// Represents an EC key with a length of 256 bits.
        /// </summary>
        public static readonly KeyDefinition P256 = new KeyDefinition
        {
            Type = KeyType.P256,
            LengthInBytes = 32,
            LengthInBits = 256,
            Oid = KeyOids.P256,
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
            Type = KeyType.P384,
            LengthInBytes = 48,
            LengthInBits = 384,
            Oid = KeyOids.P384,
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
            Type = KeyType.P521,
            LengthInBytes = 66,
            LengthInBits = 521,
            Oid = KeyOids.P521,
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
            Type = KeyType.X25519,
            LengthInBytes = 32,
            LengthInBits = 256,
            Oid = KeyOids.X25519,
            IsEcKey = true,
            CoseKeyDefinition = new CoseKeyDefinition
            {
                Type = CoseKeyType.Okp,
                CurveIdentifier = CoseEcCurve.X25519,
                AlgorithmIdentifier = CoseAlgorithmIdentifier.ECDHwHKDF256
            }
        };

        /// <summary>
        /// Represents an Ed25519 key.
        /// </summary>
        public static readonly KeyDefinition Ed25519 = new KeyDefinition
        {
            Type = KeyType.Ed25519,
            LengthInBytes = 32,
            LengthInBits = 256,
            Oid = KeyOids.Ed25519,
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
            Type = KeyType.RSA1024,
            LengthInBytes = 128,
            LengthInBits = 1024,
            Oid = KeyOids.Rsa,
            IsRsaKey = true
        };

        /// <summary>
        ///  Represents an RSA key with a length of 2048 bits.
        /// </summary>
        public static readonly KeyDefinition RSA2048 = new KeyDefinition
        {
            Type = KeyType.RSA2048,
            LengthInBytes = 256,
            LengthInBits = 2048,
            Oid = KeyOids.Rsa,
            IsRsaKey = true
        };

        /// <summary>
        ///  Represents an RSA key with a length of 3072 bits.
        /// </summary>
        public static readonly KeyDefinition RSA3072 = new KeyDefinition
        {
            Type = KeyType.RSA3072,
            LengthInBytes = 384,
            LengthInBits = 3072,
            Oid = KeyOids.Rsa,
            IsRsaKey = true
        };

        /// <summary>
        /// Represents an RSA key with a length of 4096 bits.
        /// </summary>
        public static readonly KeyDefinition RSA4096 = new KeyDefinition
        {
            Type = KeyType.RSA4096,
            LengthInBytes = 512,
            LengthInBits = 4096,
            Oid = KeyOids.Rsa,
            IsRsaKey = true
        };

        /// <summary>
        /// Represents the type of a cryptographic key.
        /// </summary>
        public enum KeyType
        {
            P256,
            P384,
            P521,
            X25519,
            Ed25519,
            RSA1024,
            RSA2048,
            RSA3072,
            RSA4096
        }

        /// <summary>
        /// Represents the definition of a cryptographic key, including its type, length, and other properties.
        /// </summary>
        public class KeyDefinition
        {
            /// <summary>
            /// Gets or sets the type of the key.
            /// </summary>
            public KeyType Type { get; set; }

            /// <summary>
            /// Gets or sets the length of the key in bytes.
            /// </summary>
            public int LengthInBytes { get; set; }

            /// <summary>
            /// Gets or sets the length of the key in bits.
            /// </summary>
            public int LengthInBits { get; set; }

            /// <summary>
            /// Gets or sets the object identifier (OID) of the key.
            /// </summary>
            public string Oid { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets a value indicating whether the key is an elliptic curve (EC) key.
            /// </summary>
            public bool IsEcKey { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the key is an RSA key.
            /// </summary>
            public bool IsRsaKey { get; set; }

            /// <summary>
            /// Gets the name of the key, which is the string representation of the key type.
            /// </summary>
            public string Name => Type.ToString();

            /// <summary>
            /// Gets or sets the COSE key definition associated with this key.
            /// </summary>
            public CoseKeyDefinition? CoseKeyDefinition { get; set; }
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
}
