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

namespace Yubico.YubiKey.Cryptography
{
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
        public const string ECP256 = "1.2.840.10045.3.1.7";

        /// <summary>
        /// Represents the OID for NIST P-384 curve (also known as secp384r1)
        /// </summary>
        public const string ECP384 = "1.3.132.0.34";

        /// <summary>
        /// Represents the OID for NIST P-521 curve (also known as secp521r1)
        /// </summary>
        public const string ECP521 = "1.3.132.0.35";
        
        /// <summary>
        /// Represents the OID for AES-128 in CBC mode
        /// </summary>
        public const string AES128Cbc = "2.16.840.1.101.3.4.1.2";
        
        /// <summary>
        /// Represents the OID for AES-192 in CBC mode
        /// </summary>
        public const string AES192Cbc = "2.16.840.1.101.3.4.1.22";
        
        /// <summary>
        /// Represents the OID for AES-256 in CBC mode
        /// </summary>
        public const string AES256Cbc = "2.16.840.1.101.3.4.1.42";
        
        /// <summary>
        /// Represents the OID for Triple DES in CBC mode
        /// </summary>
        public const string TripleDESCbc = "1.2.840.113549.3.7";

        /// <summary>
        /// Gets the algorithm and curve OIDs for a specific key type
        /// </summary>
        public static (string AlgorithmOid, string? Curveoid) GetOidsByKeyType(KeyType keyType)
        {
            return keyType switch
            {
                KeyType.RSA1024 => (RSA, null),
                KeyType.RSA2048 => (RSA, null),
                KeyType.RSA3072 => (RSA, null),
                KeyType.RSA4096 => (RSA, null),

                KeyType.ECP256 => (ECDSA, ECP256),
                KeyType.ECP384 => (ECDSA, ECP384),
                KeyType.ECP521 => (ECDSA, ECP521),

                KeyType.X25519 => (X25519, null),
                KeyType.Ed25519 => (Ed25519, null),
                
                KeyType.AES128 => (AES128Cbc, null),
                KeyType.AES192 => (AES192Cbc, null),
                KeyType.AES256 => (AES256Cbc, null),
                KeyType.TripleDES => (TripleDESCbc, null),
                
                _ => throw new ArgumentException($"Unsupported key type: {keyType}")
            };
        }

        public static bool IsECDsaCurve(string? curveOid) => curveOid is ECP256 or ECP384 or ECP521;

        public static bool IsCurve25519Algorithm(string? algorithmOid) => algorithmOid is X25519 or Ed25519;
    }
}
