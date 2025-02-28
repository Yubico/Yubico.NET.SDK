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
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
        /// <summary>
        /// The raw byte representation of the cryptographic data in DER format.
        /// </summary>
        protected readonly byte[] _bytes;
        protected readonly string _pemStringFull;

        /// <summary>
        /// Initializes a new instance of TestCrypto with PEM-encoded data from a file.
        /// </summary>
        /// <param name="filePath">Path to the PEM file containing cryptographic data.</param>
        protected TestCrypto(string filePath)
        {
            _pemStringFull = File
                .ReadAllText(filePath)
                .Replace("\n", "")
                .Trim();
            _bytes = GetBytesFromPem(_pemStringFull);
        }

        /// <summary>
        /// Returns the raw byte representation of the key data.
        /// </summary>
        /// <returns>Byte array containing the decoded cryptographic data.</returns>
        public byte[] KeyBytes => _bytes;

        /// <summary>
        /// Returns the complete PEM-encoded string representation.
        /// </summary>
        /// <returns>String containing the full PEM data including headers and footers.</returns>
        public string AsPemString() => _pemStringFull;

        /// <summary>
        /// Returns the Base64-encoded data without PEM headers and footers.
        /// </summary>
        /// <returns>Base64 string of the cryptographic data.</returns>
        public string AsBase64String() => StripPemHeaderFooter(_pemStringFull);
        
        private static byte[] GetBytesFromPem(string pemData)
        {
            var base64 = StripPemHeaderFooter(pemData);
            return Convert.FromBase64String(base64);
        }

        private static string StripPemHeaderFooter(string pemData)
        {
            var base64 = pemData
                .Replace("-----BEGIN PUBLIC KEY-----", "")
                .Replace("-----END PUBLIC KEY-----", "")
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("-----BEGIN EC PRIVATE KEY-----", "")
                .Replace("-----END EC PRIVATE KEY-----", "")
                .Replace("-----BEGIN CERTIFICATE-----", "")
                .Replace("-----END CERTIFICATE-----", "")
                .Replace("-----BEGIN CERTIFICATE REQUEST-----", "")
                .Replace("-----END CERTIFICATE REQUEST-----", "")
                .Replace("\n", "")
                .Trim();
            return base64;
        }
    }

    /// <summary>
    /// Represents a cryptographic key for testing purposes, supporting both RSA and EC keys.
    /// Provides conversion methods to standard .NET cryptographic types.
    /// </summary>
    public class TestKey : TestCrypto
    {
        private readonly string _curve;
        private readonly bool _isPrivate;

        /// <summary>
        /// Loads a test key from the TestData directory.
        /// </summary>
        /// <param name="filePath">The path to the PEM file containing the key data</param>
        /// <param name="curve">The curve or key type (e.g., "rsa2048", "secp256r1")</param>
        /// <param name="isPrivate">True for private key, false for public key</param>
        /// <returns>A TestKey instance representing the loaded key</returns>
        private TestKey(string filePath, string curve, bool isPrivate) : base(filePath)
        {
            _curve = curve;
            _isPrivate = isPrivate;
        }

        /// <summary>
        /// Converts the key to an RSA instance if it represents an RSA key.
        /// </summary>
        /// <returns>RSA instance initialized with the key data</returns>
        /// <exception cref="InvalidOperationException">Thrown if the key is not an RSA key</exception>
        public RSA AsRSA()
        {
            if (!_curve.StartsWith("rsa", StringComparison.OrdinalIgnoreCase))
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
            if (_curve.StartsWith("rsa", StringComparison.OrdinalIgnoreCase))
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
        public static TestKey Load(string curve, bool isPrivate)
        {
            var fileName = $"{curve}_{(isPrivate ? "private" : "public")}.pem";
            var filePath = Path.Combine("TestData", fileName);
            return new TestKey(filePath, curve, isPrivate);
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

        private TestCertificate(string filePath, bool isAttestation) : base(filePath)
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
        public static TestCertificate Load(string curve, bool isAttestation = false)
        {
            var fileName = $"{curve}_cert{(isAttestation ? "_attest" : "")}.pem";
            var filePath = Path.Combine("TestData", fileName);
            return new TestCertificate(filePath, isAttestation);
        }
    }

    /// <summary>
    /// Provides convenient static methods to access test keys and certificates.
    /// </summary>
    public static class TestKeys
    {

        /// <summary>
        /// Gets a private key for the specified curve.
        /// </summary>
        /// <param name="curve">The curve or key type</param>
        /// <returns>TestKey instance representing the private key</returns>
        public static TestKey GetPrivateKey(string curve) => TestKey.Load(curve, true);

        /// <summary>
        /// Get a private key for the specified algorithm.
        /// </summary>
        /// <param name="algorithm">The piv algorithm</param>
        /// <returns>TestKey instance representing the private key</returns>
        public static TestKey GetPrivateKey(PivAlgorithm algorithm) => GetPrivateKey(GetCurveFromAlgorithm(algorithm));

        /// <summary>
        /// Gets a public key for the specified curve.
        /// </summary>
        /// <param name="curve">The curve or key type</param>
        /// <returns>TestKey instance representing the public key</returns>
        public static TestKey GetPublicKey(string curve) => TestKey.Load(curve, false);

        /// <summary>
        /// Gets a certificate for the specified curve.
        /// </summary>
        /// <param name="curve">The curve or key type</param>
        /// <param name="isAttestation">True to get an attestation certificate</param>
        /// <returns>TestCertificate instance</returns>s
        public static TestCertificate GetCertificate(string curve, bool isAttestation = false) =>
            TestCertificate.Load(curve, isAttestation);

        private static string GetCurveFromAlgorithm(PivAlgorithm algorithm)
        {
            return algorithm switch
            {
                PivAlgorithm.Rsa1024 => "rsa1024",
                PivAlgorithm.Rsa2048 => "rsa2048",
                PivAlgorithm.Rsa3072 => "rsa3072",
                PivAlgorithm.Rsa4096 => "rsa4096",
                PivAlgorithm.EccP256 => "p256",
                PivAlgorithm.EccP384 => "p384",
                PivAlgorithm.EccP521 => "p521",
                PivAlgorithm.EccEd25519 => "ed25519",
                PivAlgorithm.EccX25519 => "x25519",
                _ => throw new ArgumentException("No curve mapped", nameof(algorithm))
            };
        }
    }
}
