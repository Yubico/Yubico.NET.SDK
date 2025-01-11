﻿// Copyright 2024 Yubico AB
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

namespace Yubico.YubiKey.Cryptography
{
    public static class KeyDefinitions
    {
        public struct KeyOids
        {
            public const string OidRsa = "1.2.840.113549"; // all RSA keys share the same OID
            public const string OidP256 = "1.2.840.10045.3.1.7"; // nistP256 or secP256r1
            public const string OidP384 = "1.3.132.0.34"; // nistP384 or secP384r1
            public const string OidP521 = "1.3.132.0.35"; // nistP521 or secP521r1
            public const string OidX25519 = "1.3.101.110"; // Curve25519
            public const string OidEd25519 = "1.3.101.112"; // Edwards25519
        }

        public class Helper
        {
            public KeyDefinition GetKeyDefinition(KeyType type) => _definitions[type];

            public KeyDefinition GetKeyDefinitionByOid(string oid)
            {
                if (string.Equals(oid, KeyOids.OidRsa, StringComparison.OrdinalIgnoreCase))
                {
                    throw new NotSupportedException(
                        "RSA keys are not supported by this method as all RSA keys share the same OID.");
                }

                try
                {
                    return _definitions.Values.Single(d => d.Oid == oid);
                }
                catch (InvalidOperationException e)
                {
                    throw new NotSupportedException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.UnsupportedAlgorithm,
                            e.Message));
                }
            }

            public IReadOnlyCollection<KeyDefinition> GetRsaKeyDefinitions() =>
                _definitions.Values.Where(d => d.IsRsaKey).ToList();

            public IReadOnlyCollection<KeyDefinition> GetEcKeyDefinitions() =>
                _definitions.Values.Where(d => d.IsEcKey).ToList();

            private readonly IReadOnlyDictionary<KeyType, KeyDefinition> _definitions =
                new Dictionary<KeyType, KeyDefinition>
                {
                    {
                        KeyType.P256,
                        new KeyDefinition
                        {
                            Type = KeyType.P256, LengthInBytes = 32, LengthInBits = 256, Oid = KeyOids.OidP256,
                            IsEcKey = true,
                            CoseKeyDefinition = new CoseKeyDefinition
                                { CoseKeyType = 2, CoseCurve = 1, CoseAlgorithm = -7, RequiresYCoordinate = true }
                        }
                    },
                    {
                        KeyType.P384,
                        new KeyDefinition
                        {
                            Type = KeyType.P384, LengthInBytes = 48, LengthInBits = 384, Oid = KeyOids.OidP384,
                            IsEcKey = true,
                            CoseKeyDefinition = new CoseKeyDefinition
                                { CoseKeyType = 2, CoseCurve = 2, CoseAlgorithm = -35, RequiresYCoordinate = true }
                        }
                    },
                    {
                        KeyType.P521,
                        new KeyDefinition
                        {
                            Type = KeyType.P521, LengthInBytes = 65, LengthInBits = 521, Oid = KeyOids.OidP521,
                            IsEcKey = true,
                            CoseKeyDefinition = new CoseKeyDefinition
                                { CoseKeyType = 2, CoseCurve = 3, CoseAlgorithm = -36, RequiresYCoordinate = true }
                        }
                    },
                    {
                        KeyType.X25519,
                        new KeyDefinition
                        {
                            Type = KeyType.X25519, LengthInBytes = 32, LengthInBits = 256, Oid = KeyOids.OidX25519,
                            IsEcKey = true,
                            CoseKeyDefinition = new CoseKeyDefinition
                                { CoseKeyType = 1, CoseCurve = 4, CoseAlgorithm = -25 }
                        }
                    },
                    {
                        KeyType.Ed25519,
                        new KeyDefinition
                        {
                            Type = KeyType.Ed25519, LengthInBytes = 32, LengthInBits = 256, Oid = KeyOids.OidEd25519,
                            IsEcKey = true,
                            CoseKeyDefinition = new CoseKeyDefinition
                                { CoseKeyType = 1, CoseCurve = 6, CoseAlgorithm = -8 }
                        }
                    },
                    {
                        KeyType.RSA1024,
                        new KeyDefinition
                        {
                            Type = KeyType.RSA1024, LengthInBytes = 128, LengthInBits = 1024, Oid = KeyOids.OidRsa,
                            IsRsaKey = true
                        }
                    },
                    {
                        KeyType.RSA2048,
                        new KeyDefinition
                        {
                            Type = KeyType.RSA2048, LengthInBytes = 256, LengthInBits = 2048, Oid = KeyOids.OidRsa,
                            IsRsaKey = true
                        }
                    },
                    {
                        KeyType.RSA3072,
                        new KeyDefinition
                        {
                            Type = KeyType.RSA3072, LengthInBytes = 384, LengthInBits = 3072, Oid = KeyOids.OidRsa,
                            IsRsaKey = true
                        }
                    },
                    {
                        KeyType.RSA4096,
                        new KeyDefinition
                        {
                            Type = KeyType.RSA4096, LengthInBytes = 512, LengthInBits = 4096, Oid = KeyOids.OidRsa,
                            IsRsaKey = true
                        }
                    },
                };
        }

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

        public class KeyDefinition
        {
            public KeyType Type { get; set; }
            public int LengthInBytes { get; set; }
            public int LengthInBits { get; set; }
            public string? Oid { get; set; }
            public bool IsEcKey { get; set; }
            public bool IsRsaKey { get; set; }
            public string Name => Type.ToString();
            public CoseKeyDefinition? CoseKeyDefinition { get; set; }
        }

        public class CoseKeyDefinition
        {
            public int CoseKeyType { get; set; } // kty - Key Type (1=OKP, 2=EC2)
            public int CoseCurve { get; set; } // crv - Curve identifier
            public int CoseAlgorithm { get; set; } // alg - Algorithm identifier
            public bool RequiresYCoordinate { get; set; } // true for EC2, false for OKP
        }
    }
}
