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
using System.Formats.Asn1;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.TestUtilities
{
    /// <summary>
    /// This class is used to load test keys and certificates from files.
    /// The files contain PEM-encoded data such as RSA keys, EC keys, and X.509 certificates.
    /// These keys and certificates are used in unit tests and have been generated through
    /// the script `generate-test-data.sh` which is checked into this repository under Yubico/YubiKey/utilities/TestData.
    /// </summary>
    public abstract class TestCrypto
    {
        public const string TestDataDirectory = "TestData";

        /// <summary>
        /// The raw byte representation of the cryptographic data in DER format.
        /// </summary>
        protected readonly byte[] _bytes;

        protected readonly string _pemStringFull;

        /// <summary>
        /// Initializes a new instance of TestCrypto with PEM-encoded data from a file.
        /// </summary>
        /// <param name="filePath">Path to the PEM file containing cryptographic data.</param>
        protected TestCrypto(
            string filePath)
        {
            _pemStringFull = File.ReadAllText(filePath);
            _bytes = PemHelper.GetBytesFromPem(_pemStringFull);
        }

        /// <summary>
        /// Returns the raw byte DER representation of the key data.
        /// </summary>
        /// <returns>Byte array containing the decoded cryptographic data.</returns>
        public byte[] EncodedKey => _bytes;

        /// <summary>
        /// Returns the complete PEM-encoded string representation.
        /// </summary>
        /// <returns>String containing the full PEM data including headers and footers.</returns>
        public string AsPemString() => _pemStringFull;

        /// <summary>
        /// Returns the Base64-encoded data without PEM headers and footers.
        /// </summary>
        /// <returns>Base64 string of the cryptographic data.</returns>
        public string AsBase64String() => PemHelper.AsBase64String(_pemStringFull);

        public static byte[] ReadTestData(
            string fileName) => File.ReadAllBytes(Path.Combine(TestDataDirectory, fileName));
    }

    /// <summary>
    /// Represents a cryptographic key for testing purposes, supporting both RSA and EC keys.
    /// Provides conversion methods to standard .NET cryptographic types.
    /// </summary>
    public class TestKey : TestCrypto
    {
        public readonly KeyType KeyType;
        public KeyDefinition KeyDefinition { get; private set; }


        /// <summary>
        /// Loads a test key from the TestData directory.
        /// </summary>
        /// <param name="filePath">The path to the PEM file containing the key data</param>
        /// <param name="keyType"></param>
        /// <returns>A TestKey instance representing the loaded key</returns>
        private TestKey(
            string filePath,
            KeyType keyType) : base(filePath)
        {
            KeyDefinition = keyType.GetKeyDefinition();
            KeyType = keyType;
        }

        public KeyDefinition GetKeyDefinition() =>
            KeyDefinitions.GetByKeyType(KeyType);

        public byte[] GetExponent()
        {
            try
            {
                return AsRSA().ExportParameters(false).Exponent!;
            }
            catch { return []; }
        }

        public byte[] GetModulus()
        {
            try
            {
                return AsRSA().ExportParameters(false).Modulus!;
            }
            catch { return []; }
        }

        public byte[] GetPublicPoint()
        {
            var publicKeyParameters = AsnPublicKeyDecoder.CreatePublicKey(EncodedKey);
            return publicKeyParameters switch
            {
                ECPublicKey ecParams => ecParams.PublicPoint.ToArray(),
                Curve25519PublicKey eDsaParams => eDsaParams.PublicPoint.ToArray(),
                RSAPublicKey => throw new InvalidOperationException(
                    "Use GetModulus() and GetExponent() instead for RSA keys"),
                _ => throw new ArgumentOutOfRangeException(nameof(publicKeyParameters))
            };
        }

        public byte[] GetPrivateKeyValue()
        {
            var privateKeyParameters = AsnPrivateKeyDecoder.CreatePrivateKey(EncodedKey);
            return privateKeyParameters switch
            {
                ECPrivateKey ecParams => ecParams.Parameters.D!,
                Curve25519PrivateKey cv25519 => cv25519.PrivateKey.ToArray(),
                RSAPrivateKey => throw new InvalidOperationException("Use AsRSA() instead for RSA keys"),
                _ => throw new ArgumentOutOfRangeException(nameof(privateKeyParameters))
            };
        }
        
        public IPrivateKey GetPrivateKey()
        {
            if (KeyDefinition.IsRSA)
            {
                return RSAPrivateKey.CreateFromPkcs8(EncodedKey);
            }

            if (KeyDefinition is { IsEllipticCurve: true, AlgorithmOid: Oids.ECDSA })
            {
                return ECPrivateKey.CreateFromPkcs8(EncodedKey);
            }

            return Curve25519PrivateKey.CreateFromPkcs8(EncodedKey);
        }
        
        public IPublicKey GetPublicKey()
        {
            if (KeyDefinition.IsRSA)
            {
                return RSAPublicKey.CreateFromPkcs8(EncodedKey);
            }

            if (KeyDefinition is { IsEllipticCurve: true, AlgorithmOid: Oids.ECDSA })
            {
                return ECPublicKey.CreateFromPkcs8(EncodedKey);
            }

            return Curve25519PublicKey.CreateFromPkcs8(EncodedKey);
        }

        /// <summary>
        /// Converts the key to an RSA instance if it represents an RSA key.
        /// </summary>
        /// <returns>RSA instance initialized with the key data</returns>
        /// <exception cref="InvalidOperationException">Thrown if the key is not an RSA key</exception>
        public RSA AsRSA()
        {
            if (!KeyType.IsRSA())
            {
                throw new InvalidOperationException("Not an RSA key");
            }

            var rsa = RSA.Create();
            rsa.ImportFromPem(_pemStringFull);

            return rsa;
        }

        /// <summary>
        /// Converts the key to an ECDsa instance if it represents an EC key.
        /// </summary>
        /// <returns>ECDsa instance initialized with the key data</returns>
        /// <exception cref="InvalidOperationException">Thrown if the key is not an EC key</exception>
        public ECDsa AsECDsa()
        {
            if (!KeyType.IsEllipticCurve())
            {
                throw new InvalidOperationException("Not an EC key");
            }

            var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(_pemStringFull);

            return ecdsa;
        }

        /// <summary>
        /// Converts the key to a PIV private key format.
        /// </summary>
        /// <returns>PivPrivateKey instance</returns>
        public static TestKey Load(
            KeyType keyType,
            bool isPrivate,
            int? index = null)
        {
            if (index is 0 or 1)
            {
                index = null;
            }

            var curveName = keyType.ToString().ToLower();
            var fileName = $"{curveName}_{(isPrivate ? "private" : "public")}{(index.HasValue ? $"_{index}" : "")}.pem";
            var filePath = Path.Combine(TestDataDirectory, fileName);
            return new TestKey(filePath, keyType);
        }
    }

    /// <summary>
    /// Represents an X.509 certificate for testing purposes.
    /// Supports both regular and attestation certificates.
    /// </summary>
    public class TestCertificate : TestCrypto
    {
        /// <summary>
        /// Indicates whether this certificate is an attestation certificate.
        /// </summary>
        public readonly bool IsAttestation;

        private TestCertificate(
            string filePath,
            bool isAttestation) : base(filePath)
        {
            IsAttestation = isAttestation;
        }

        /// <summary>
        /// Converts the certificate to an X509Certificate2 instance.
        /// </summary>
        /// <returns>X509Certificate2 instance initialized with the certificate data</returns>
        public X509Certificate2 AsX509Certificate2() => X509CertificateLoader.LoadCertificate(_bytes);

        /// <summary>
        /// Loads a certificate from the TestData directory.
        /// </summary>
        /// <param name="curve">The curve or key type associated with the certificate</param>
        /// <param name="isAttestation">True if loading an attestation certificate</param>
        /// <returns>A TestCertificate instance</returns>
        public static TestCertificate Load(
            KeyType keyType,
            bool isAttestation = false)
        {
            var curveName = keyType.ToString().ToLower();
            var fileName = $"{curveName}_cert{(isAttestation ? "_attest" : "")}.pem";
            var filePath = Path.Combine(TestDataDirectory, fileName);
            return new TestCertificate(filePath, isAttestation);
        }
    }

    /// <summary>
    /// Provides convenient static methods to access test keys and certificates.
    /// </summary>
    public static class TestKeys
    {
        public static TestKey GetTestPrivateKey(
            KeyType keyType) => TestKey.Load(keyType, true);

        /// <summary>
        /// Get a private key for the specified algorithm.
        /// </summary>
        /// <param name="algorithm">The piv algorithm</param>
        /// <returns>TestKey instance representing the private key</returns>
        public static TestKey GetTestPrivateKey(
            PivAlgorithm algorithm) => GetTestPrivateKey(algorithm.GetKeyType());

        public static (TestKey testPublicKey, TestKey testPrivateKey) GetKeyPair(
            KeyType keyType) => (GetTestPublicKey(keyType), GetTestPrivateKey(keyType));


        /// <summary>
        /// Gets a public key for the specified curve.
        /// </summary>
        /// <param name="keyType">The key type</param>
        /// <returns>TestKey instance representing the public key</returns>
        public static TestKey GetTestPublicKey(
            KeyType keyType,
            int? index = null)
        {
            return TestKey.Load(keyType, false, index);
        }

        /// <summary>
        /// Gets a certificate for the specified curve.
        /// </summary>
        /// <param name="curve">The curve or key type</param>
        /// <param name="isAttestation">True to get an attestation certificate</param>
        /// <returns>TestCertificate instance</returns>s
        public static TestCertificate GetTestCertificate(
            KeyType curve,
            bool isAttestation = false) =>
            TestCertificate.Load(curve, isAttestation);
    }
}
