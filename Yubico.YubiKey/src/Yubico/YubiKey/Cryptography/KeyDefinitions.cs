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
using Yubico.YubiKey.Fido2.Cose; // Ideally, Cose definitions should move to this namespace

namespace Yubico.YubiKey.Cryptography
{
    public static class KeyDefinitions
    {
        public static readonly KeyDefinitionHelper Helper = new KeyDefinitionHelper();

        public struct KeyOids
        {
            public const string OidRsa = "1.2.840.113549"; // all RSA keys share the same OID
            public const string OidP256 = "1.2.840.10045.3.1.7"; // nistP256 or secP256r1
            public const string OidP384 = "1.3.132.0.34"; // nistP384 or secP384r1
            public const string OidP521 = "1.3.132.0.35"; // nistP521 or secP521r1
            public const string OidX25519 = "1.3.101.110"; // Curve25519
            public const string OidEd25519 = "1.3.101.112"; // Edwards25519
        }

        public static readonly KeyDefinition P256 = new KeyDefinition
        {
            Type = KeyType.P256,
            LengthInBytes = 32,
            LengthInBits = 256,
            Oid = KeyOids.OidP256,
            IsEcKey = true,
            CoseKeyDefinition = new CoseKeyDefinition
            {
                Type = CoseKeyType.Ec2,
                CurveIdentifier = CoseEcCurve.P256,
                AlgorithmIdentifier = CoseAlgorithmIdentifier.ES256
            }
        };

        public static readonly KeyDefinition P384 = new KeyDefinition
        {
            Type = KeyType.P384,
            LengthInBytes = 48,
            LengthInBits = 384,
            Oid = KeyOids.OidP384,
            IsEcKey = true,
            CoseKeyDefinition = new CoseKeyDefinition
            {
                Type = CoseKeyType.Ec2,
                CurveIdentifier = CoseEcCurve.P384,
                AlgorithmIdentifier = CoseAlgorithmIdentifier.ES384
            }
        };

        public static readonly KeyDefinition P521 = new KeyDefinition
        {
            Type = KeyType.P521,
            LengthInBytes = 66,
            LengthInBits = 521,
            Oid = KeyOids.OidP521,
            IsEcKey = true,
            CoseKeyDefinition = new CoseKeyDefinition
            {
                Type = CoseKeyType.Ec2,
                CurveIdentifier = CoseEcCurve.P521,
                AlgorithmIdentifier = CoseAlgorithmIdentifier.ES512
            }
        };

        public static readonly KeyDefinition X25519 = new KeyDefinition
        {
            Type = KeyType.X25519,
            LengthInBytes = 32,
            LengthInBits = 256,
            Oid = KeyOids.OidX25519,
            IsEcKey = true,
            CoseKeyDefinition = new CoseKeyDefinition
            {
                Type = CoseKeyType.Okp,
                CurveIdentifier = CoseEcCurve.X25519,
                AlgorithmIdentifier = CoseAlgorithmIdentifier.ECDHwHKDF256
            }
        };

        public static readonly KeyDefinition Ed25519 = new KeyDefinition
        {
            Type = KeyType.Ed25519,
            LengthInBytes = 32,
            LengthInBits = 256,
            Oid = KeyOids.OidEd25519,
            IsEcKey = true,
            CoseKeyDefinition = new CoseKeyDefinition
            {
                Type = CoseKeyType.Okp,
                CurveIdentifier = CoseEcCurve.Ed25519,
                AlgorithmIdentifier = CoseAlgorithmIdentifier.EdDSA
            }
        };

        public static readonly KeyDefinition RSA1024 = new KeyDefinition
        {
            Type = KeyType.RSA1024,
            LengthInBytes = 128,
            LengthInBits = 1024,
            Oid = KeyOids.OidRsa,
            IsRsaKey = true
        };

        public static readonly KeyDefinition RSA2048 = new KeyDefinition
        {
            Type = KeyType.RSA2048,
            LengthInBytes = 256,
            LengthInBits = 2048,
            Oid = KeyOids.OidRsa,
            IsRsaKey = true
        };

        public static readonly KeyDefinition RSA3072 = new KeyDefinition
        {
            Type = KeyType.RSA3072,
            LengthInBytes = 384,
            LengthInBits = 3072,
            Oid = KeyOids.OidRsa,
            IsRsaKey = true
        };

        public static readonly KeyDefinition RSA4096 = new KeyDefinition
        {
            Type = KeyType.RSA4096,
            LengthInBytes = 512,
            LengthInBits = 4096,
            Oid = KeyOids.OidRsa,
            IsRsaKey = true
        };

        public class KeyDefinitionHelper
        {
            public KeyDefinition GetKeyDefinition(KeyType type) => _definitions[type];

            public KeyDefinition GetKeyDefinition(CoseEcCurve curve) =>
                _definitions.Values.Single(
                    d => d.CoseKeyDefinition != null && d.CoseKeyDefinition.CurveIdentifier == curve);

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

            public IReadOnlyCollection<KeyDefinition> GetRsaKeyDefinitions() => // Todo dictioanry?
                _definitions.Values.Where(d => d.IsRsaKey).ToList();

            public IReadOnlyCollection<KeyDefinition> GetEcKeyDefinitions() =>
                _definitions.Values.Where(d => d.IsEcKey).ToList();

            private static readonly IReadOnlyDictionary<KeyType, KeyDefinition> _definitions = new Dictionary<KeyType, KeyDefinition>
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
            public string Oid { get; set; } = string.Empty;
            public bool IsEcKey { get; set; }
            public bool IsRsaKey { get; set; }
            public string Name => Type.ToString();
            public CoseKeyDefinition? CoseKeyDefinition { get; set; }
        }

        public class CoseKeyDefinition
        {
            public CoseKeyType Type { get; set; } // kty - Key Type (1=OKP, 2=EC2)
            public CoseEcCurve CurveIdentifier { get; set; } // crv - Curve identifier
            public CoseAlgorithmIdentifier AlgorithmIdentifier { get; set; } // alg - Algorithm identifier
        }
    }
}
