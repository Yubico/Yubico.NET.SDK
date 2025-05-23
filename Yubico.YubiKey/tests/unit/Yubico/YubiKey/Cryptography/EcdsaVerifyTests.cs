// Copyright 2022 Yubico AB
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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Xunit;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.Cryptography
{
    public class EcdsaVerifyTests
    {
        [Fact]
        [Obsolete("Obsolete")]
        public void PivKey_VerifyDigestedData_Succeeds()
        {
            var pubKey = new PivEccPublicKey(GetEncodedPoint());
            byte[] digest = GetDigest();
            byte[] signature = GetSignature();

            using var verifier = new EcdsaVerify(pubKey);
            bool isVerified = verifier.VerifyDigestedData(digest, signature);
            Assert.True(isVerified);
        }

        [Fact]
        public void CoseKey_VerifyDigestedData_Succeeds()
        {
            var pubKey = new CoseEcPublicKey(CoseEcCurve.P256, GetX(), GetY());
            byte[] digest = GetDigest();
            byte[] signature = GetSignature();

            using var verifier = new EcdsaVerify(pubKey);
            bool isVerified = verifier.VerifyDigestedData(digest, signature);
            Assert.True(isVerified);
        }

        [Theory]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        [InlineData(KeyType.ECP521)]
        public void CoseKey_VerifyDigestedData_WithMultipleCurves_Succeeds(KeyType keyType)
        {
            // Arrange
            (var ecCurve, var coseCurve) = GetCurves(keyType);

            var pubKey = ECDsa.Create(ecCurve);
            var sha256 = SHA256.Create();

            var dataToSign = Encoding.GetEncoding("UTF-8").GetBytes("Hello World");
            var hash = sha256.ComputeHash(dataToSign);
            var signature = pubKey.SignHash(hash, DSASignatureFormat.Rfc3279DerSequence);

            var ecParams = pubKey.ExportParameters(false);
            var pubKeyCose = new CoseEcPublicKey(coseCurve, ecParams.Q.X, ecParams.Q.Y);

            // Act
            using var verifier = new EcdsaVerify(pubKeyCose);
            var isVerified = verifier.VerifyDigestedData(hash, signature);

            // Assert
            Assert.True(isVerified);
        }

        [Fact]
        public void Cert_Verify_DigestedDataSucceeds()
        {
            var pubKey = X509CertificateLoader.LoadCertificate(GetCert());

            byte[] digest = GetDigest();
            byte[] signature = GetSignature();
            using var verifier = new EcdsaVerify(pubKey);
            bool isVerified = verifier.VerifyDigestedData(digest, signature);
            Assert.True(isVerified);
        }

        [Fact]
        public void ECDsa_VerifyDigestedData_Succeeds()
        {
            var eccCurve = ECCurve.CreateFromValue(Oids.ECP256);
            var eccParams = new ECParameters
            {
                Curve = (ECCurve)eccCurve
            };

            eccParams.Q.X = GetX();
            eccParams.Q.Y = GetY();

            using var pubKey = ECDsa.Create(eccParams);

            byte[] digest = GetDigest();
            byte[] signature = GetSignature();

            using var verifier = new EcdsaVerify(pubKey);
            bool isVerified = verifier.VerifyDigestedData(digest, signature);
            Assert.True(isVerified);
        }

        [Theory]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        [InlineData(KeyType.ECP521)]
        public void ECDsa_VerifyDigestedData_WithIeeeFormat_Succeeds(KeyType keyType)
        {
            // Arrange
            (var ecCurve, _) = GetCurves(keyType);

            var pubKey = ECDsa.Create(ecCurve);
            var sha256 = SHA256.Create();

            var dataToSign = Encoding.GetEncoding("UTF-8").GetBytes("Hello World");
            var hash = sha256.ComputeHash(dataToSign);
            var signature = pubKey.SignHash(hash, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

            // Act
            using var verifier = new EcdsaVerify(pubKey);
            var isVerified = verifier.VerifyDigestedData(hash, signature, false);

            // Assert
            Assert.True(isVerified);
        }

        [Theory]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        [InlineData(KeyType.ECP521)]
        public void ECDsa_VerifyDigestedData_WithDerFormat_Succeeds(KeyType keyType)
        {
            // Arrange
            (var ecCurve, _) = GetCurves(keyType);

            var pubKey = ECDsa.Create(ecCurve);
            var sha256 = SHA256.Create();

            var dataToSign = Encoding.GetEncoding("UTF-8").GetBytes("Hello World");
            var hash = sha256.ComputeHash(dataToSign);
            var signature = pubKey.SignHash(hash, DSASignatureFormat.Rfc3279DerSequence);

            // Act
            using var verifier = new EcdsaVerify(pubKey);
            bool isVerified = verifier.VerifyDigestedData(hash, signature, true);

            // Assert
            Assert.True(isVerified);
        }

        [Theory]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        [InlineData(KeyType.ECP521)]
        public void ECDsa_VerifyData_WithDerFormat_Succeeds(KeyType keyType)
        {
            // Arrange
            (var ecCurve, _) = GetCurves(keyType);

            var pubKey = ECDsa.Create(ecCurve);
            HashAlgorithm hashAlgorithm = keyType switch
            {
                KeyType.ECP256 => CryptographyProviders.Sha256Creator(),
                KeyType.ECP384 => CryptographyProviders.Sha384Creator(),
                KeyType.ECP521 => CryptographyProviders.Sha512Creator(),
                _ => throw new ArgumentException(ExceptionMessages.UnsupportedAlgorithm),
            };

            var dataToSign = Encoding.GetEncoding("UTF-8").GetBytes("Hello World");
            var hash = hashAlgorithm.ComputeHash(dataToSign);
            var signature = pubKey.SignHash(hash, DSASignatureFormat.Rfc3279DerSequence);

            // Act
            using var verifier = new EcdsaVerify(pubKey);
            var isVerified = verifier.VerifyData(dataToSign, signature, true);

            // Assert
            Assert.True(isVerified);
        }

        [Fact]
        public void EncodedKey_VerifyDigestedData_Succeeds()
        {
            byte[] pubKey = GetEncodedPoint();
            byte[] digest = GetDigest();
            byte[] signature = GetSignature();

            using var verifier = new EcdsaVerify(pubKey);
            bool isVerified = verifier.VerifyDigestedData(digest, signature);
            Assert.True(isVerified);
        }

        private byte[] GetEncodedPoint()
        {
            byte[] xCoord = GetX();
            byte[] yCoord = GetY();

            byte[] encoding = new byte[xCoord.Length + yCoord.Length + 1];
            encoding[0] = 0x4;
            Array.Copy(xCoord, 0, encoding, 1, xCoord.Length);
            Array.Copy(yCoord, 0, encoding, xCoord.Length + 1, yCoord.Length);

            return encoding;
        }

        private static (ECCurve ecCurve, CoseEcCurve coseCurve) GetCurves(KeyType keyType)
        {
            var keyDefinition = KeyDefinitions.GetByKeyType(keyType);
            var (eccCurve, coseCurve) = keyDefinition.KeyType switch
            {
                KeyType.ECP256 => (ECCurve.NamedCurves.nistP256, CoseEcCurve.P256),
                KeyType.ECP384 => (ECCurve.NamedCurves.nistP384, CoseEcCurve.P384),
                KeyType.ECP521 => (ECCurve.NamedCurves.nistP521, CoseEcCurve.P521),
                _ => throw new ArgumentException("Unknown curve")
            };

            return (eccCurve, coseCurve);
        }


        private byte[] GetX() => new byte[]
        {
            0x3C, 0x51, 0xD3, 0x50, 0x87, 0x45, 0xA8, 0xCB, 0x8D, 0x64, 0x9D, 0xFF, 0x81, 0xE7, 0x6A, 0xA6, 0x68, 0xEC,
            0xA5,
            0xA4, 0xF7, 0x4B, 0xAE, 0x3B, 0xB9, 0x01, 0xB2, 0xCF, 0x1E, 0x8E, 0xD2, 0x80
        };

        private byte[] GetY() => new byte[]
        {
            0x30, 0x44, 0xDD, 0x3C, 0x3E, 0x28, 0x5F, 0xA4, 0x90, 0x6B, 0xF7, 0xA0, 0x24, 0x1A, 0x7C, 0x1A, 0xA2, 0xF4,
            0x22,
            0x90, 0x24, 0x09, 0xE8, 0xAE, 0xAE, 0x14, 0x5E, 0x6D, 0xBB, 0x5E, 0x50, 0xB1
        };

        private byte[] GetDigest() => new byte[]
        {
            0x67, 0x51, 0x64, 0x55, 0x15, 0x7F, 0xDC, 0xDB, 0x59, 0x54, 0x0E, 0x24, 0x43, 0xEA, 0xBD, 0xED, 0x62, 0x11,
            0xEC,
            0x2C, 0x06, 0x70, 0xB5, 0xDD, 0x95, 0x69, 0x6A, 0x8B, 0x66, 0x5E, 0xDB, 0x2E
        };

        private byte[] GetSignature() => new byte[]
        {
            0x30, 0x44, 0x02, 0x20, 0x0A, 0x5E, 0x2D, 0xB0, 0xD0, 0xF6, 0x64, 0x17, 0xCD, 0xBC, 0xDE, 0x89, 0x73, 0x1C,
            0x0E,
            0x0A, 0x68, 0x5D, 0x60, 0x69, 0x06, 0x90, 0x64, 0xEB, 0x8D, 0x06, 0xBF, 0xE1, 0x1F, 0xAD, 0x91, 0x55, 0x02,
            0x20,
            0x1E, 0x4D, 0xDB, 0xA7, 0x8B, 0x4E, 0x25, 0x03, 0x7D, 0xD6, 0xF0, 0x63, 0x6C, 0x84, 0x1A, 0xD3, 0x16, 0x1A,
            0x83,
            0x7F, 0x9D, 0x03, 0x02, 0x2A, 0x3D, 0x28, 0xE1, 0x5E, 0x59, 0x9F, 0xCD, 0xD6
        };

        private byte[] GetCert() => new byte[]
        {
            0x30, 0x82, 0x01, 0x47, 0x30, 0x81, 0xEF, 0xA0, 0x03, 0x02, 0x01, 0x02, 0x02, 0x09, 0x00, 0xF3, 0xCE, 0xD0,
            0xD8,
            0xE5, 0x2D, 0x30, 0x8E, 0x30, 0x0A, 0x06, 0x08, 0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x04, 0x03, 0x02, 0x30, 0x29,
            0x31,
            0x27, 0x30, 0x25, 0x06, 0x03, 0x55, 0x04, 0x03, 0x13, 0x1E, 0x53, 0x65, 0x6C, 0x66, 0x20, 0x67, 0x65, 0x6E,
            0x65,
            0x72, 0x61, 0x74, 0x65, 0x64, 0x20, 0x45, 0x43, 0x43, 0x20, 0x43, 0x65, 0x72, 0x74, 0x69, 0x66, 0x69, 0x63,
            0x61,
            0x74, 0x65, 0x30, 0x20, 0x17, 0x0D, 0x32, 0x34, 0x30, 0x34, 0x32, 0x35, 0x31, 0x37, 0x32, 0x33, 0x30, 0x31,
            0x5A,
            0x18, 0x0F, 0x39, 0x39, 0x39, 0x39, 0x31, 0x32, 0x33, 0x31, 0x32, 0x33, 0x35, 0x39, 0x35, 0x39, 0x5A, 0x30,
            0x29,
            0x31, 0x27, 0x30, 0x25, 0x06, 0x03, 0x55, 0x04, 0x03, 0x13, 0x1E, 0x53, 0x65, 0x6C, 0x66, 0x20, 0x67, 0x65,
            0x6E,
            0x65, 0x72, 0x61, 0x74, 0x65, 0x64, 0x20, 0x45, 0x43, 0x43, 0x20, 0x43, 0x65, 0x72, 0x74, 0x69, 0x66, 0x69,
            0x63,
            0x61, 0x74, 0x65, 0x30, 0x59, 0x30, 0x13, 0x06, 0x07, 0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x02, 0x01, 0x06, 0x08,
            0x2A,
            0x86, 0x48, 0xCE, 0x3D, 0x03, 0x01, 0x07, 0x03, 0x42, 0x00, 0x04, 0x3C, 0x51, 0xD3, 0x50, 0x87, 0x45, 0xA8,
            0xCB,
            0x8D, 0x64, 0x9D, 0xFF, 0x81, 0xE7, 0x6A, 0xA6, 0x68, 0xEC, 0xA5, 0xA4, 0xF7, 0x4B, 0xAE, 0x3B, 0xB9, 0x01,
            0xB2,
            0xCF, 0x1E, 0x8E, 0xD2, 0x80, 0x30, 0x44, 0xDD, 0x3C, 0x3E, 0x28, 0x5F, 0xA4, 0x90, 0x6B, 0xF7, 0xA0, 0x24,
            0x1A,
            0x7C, 0x1A, 0xA2, 0xF4, 0x22, 0x90, 0x24, 0x09, 0xE8, 0xAE, 0xAE, 0x14, 0x5E, 0x6D, 0xBB, 0x5E, 0x50, 0xB1,
            0x30,
            0x0A, 0x06, 0x08, 0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x04, 0x03, 0x02, 0x03, 0x47, 0x00, 0x30, 0x44, 0x02, 0x20,
            0x7E,
            0x23, 0xF1, 0x04, 0xAC, 0x4D, 0x1B, 0xC0, 0x39, 0xC7, 0xED, 0x95, 0xEE, 0x3A, 0x4B, 0x5E, 0x52, 0x03, 0x4A,
            0xFB,
            0xC9, 0xCA, 0xA3, 0xC3, 0x0D, 0xF9, 0x96, 0xD7, 0x11, 0x25, 0xF8, 0x19, 0x02, 0x20, 0x0D, 0x41, 0xEA, 0x93,
            0x29,
            0x1B, 0xC4, 0x28, 0x91, 0x2E, 0x24, 0x04, 0x07, 0x3D, 0x19, 0xEF, 0xB8, 0xC7, 0x29, 0x2A, 0x3C, 0x35, 0x9F,
            0xF5,
            0xFB, 0xEE, 0xC0, 0x7C, 0x11, 0xC1, 0xDF, 0x99
        };
    }
}
