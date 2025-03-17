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
using Xunit;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.TestUtilities
{
    public class TestKeysTests
    {
        // [Theory]
        // [InlineData(KeyDefinitions.KeyType.RSA1024)]
        // [InlineData(KeyDefinitions.KeyType.RSA2048)]
        // [InlineData(KeyDefinitions.KeyType.RSA3072)]
        // [InlineData(KeyDefinitions.KeyType.RSA4096)]
        // public void TestKey_GetModulusAndExponent_ReturnsCorrectValues(
        //     KeyDefinitions.KeyType keyType)
        // {
        //     var key = TestKeys.GetTestPublicKey(keyType);
        //     Assert.NotNull(key);
        //
        //     var modulus = key.GetModulus();
        //     var exponent = key.GetExponent();
        //     Assert.NotNull(modulus);
        //     Assert.NotNull(exponent);
        //
        //
        //     byte[] expectedModulus, expectedExponent;
        //     switch (keyType)
        //     {
        //         // TODO supply actual public key data, this is theprivate 
        //         case KeyDefinitions.KeyType.RSA1024:
        //             expectedModulus =
        //             [
        //             ];
        //             expectedExponent =
        //             [
        //             ];
        //             break;
        //         case KeyDefinitions.KeyType.RSA2048:
        //             expectedModulus =
        //             [
        //             ];
        //             expectedExponent =
        //             [
        //             ];
        //             break;
        //         case KeyDefinitions.KeyType.RSA3072:
        //             expectedModulus =
        //             [
        //             ];
        //             expectedExponent =
        //             [
        //             ];
        //             break;
        //         case KeyDefinitions.KeyType.RSA4096:
        //             expectedModulus =
        //             [
        //             ];
        //             expectedExponent =
        //             [
        //             ];
        //             break;
        //
        //         default:
        //             throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null);
        //     }
        // }

        [Theory]
        // [InlineData(KeyDefinitions.KeyType.RSA1024)]
        // [InlineData(KeyDefinitions.KeyType.RSA2048)]
        // [InlineData(KeyDefinitions.KeyType.RSA3072)]
        // [InlineData(KeyDefinitions.KeyType.RSA4096)]
        [InlineData(KeyDefinitions.KeyType.P256)]
        [InlineData(KeyDefinitions.KeyType.P384)]
        [InlineData(KeyDefinitions.KeyType.P521)]
        [InlineData(KeyDefinitions.KeyType.Ed25519)]
        [InlineData(KeyDefinitions.KeyType.X25519)]
        public void TestKey_GetPublicKey_ReturnsCorrectPublicKey(
            KeyDefinitions.KeyType keyType)
        {
            var key = TestKeys.GetTestPublicKey(keyType);
            Assert.NotNull(key);

            var privateKey = key.GetPublicPoint();
            Assert.NotNull(privateKey);

            byte[] expectedPublicKey;
            switch (keyType)
            {
                // TODO supply actual public key data, this is theprivate 
                case KeyDefinitions.KeyType.P256:
                    expectedPublicKey =
                    [
                        0x54, 0x9D, 0x2A, 0x8A, 0x03, 0xE6, 0x2D, 0xC8, 0x29, 0xAD, 0xE4, 0xD6, 0x85, 0x0D, 0xB9, 0x56,
                        0x84, 0x75, 0x14, 0x7C, 0x59, 0xEF, 0x23, 0x8F, 0x12, 0x2A, 0x08, 0xCF, 0x55, 0x7C, 0xDB, 0x91
                    ];
                    break;
                case KeyDefinitions.KeyType.P384:
                    expectedPublicKey =
                    [
                        0xC7, 0x5D, 0x6B, 0x6A, 0xD3, 0xD1, 0xBC, 0x59, 0x25, 0xEB, 0xE5, 0x89, 0x7C, 0x3D, 0x63, 0xC7,
                        0xFB, 0x6B, 0x0A,
                        0x1D, 0x85, 0xA8, 0x5F, 0x04, 0xB3, 0x48, 0x54, 0x48, 0xF6, 0xC5, 0x9D, 0x97, 0xB8, 0xD6, 0x36,
                        0x06, 0xEE, 0xC0,
                        0xBD, 0x3F, 0xF2, 0x48, 0xB5, 0x7E, 0x92, 0xB9, 0xEA, 0x1B
                    ];
                    break;
                case KeyDefinitions.KeyType.P521:
                    expectedPublicKey =
                    [
                        0x01, 0x5D, 0xDA, 0x2E, 0x0B, 0x56, 0x65, 0x38, 0xB9, 0x2D, 0x45, 0xDC, 0xAE, 0x1F, 0xC8, 0xB4,
                        0xA4, 0x7A, 0x7C,
                        0x69, 0x92, 0xBD, 0x60, 0x9C, 0x54, 0xED, 0x0C, 0x6D, 0xD2, 0xC3, 0x2F, 0x89, 0xE0, 0x7D, 0xC6,
                        0x8E, 0x9B, 0x08,
                        0xB3, 0xE5, 0x72, 0xE2, 0x19, 0x76, 0x29, 0x5A, 0x74, 0x6E, 0xA6, 0x0E, 0x70, 0xA2, 0xFD, 0x2A,
                        0x17, 0x95, 0x73,
                        0x00, 0xDA, 0xE0, 0xCA, 0xEB, 0xA0, 0x5E, 0x4B, 0xDB
                    ];
                    break;
                case KeyDefinitions.KeyType.X25519:
                    expectedPublicKey =
                    [
                        0x60, 0x82, 0xB9, 0xFA, 0x5E, 0x9B, 0xEA, 0x4C, 0xAE, 0x11, 0xDC, 0x43, 0x05, 0x2F, 0xAD, 0x4C,
                        0x61, 0xD0, 0xA4, 0x3D, 0xCE, 0xB3, 0x63, 0xB8, 0x05, 0x71, 0x33, 0xF1, 0x38, 0x77, 0x98, 0x4D
                    ];
                    break;
                case KeyDefinitions.KeyType.Ed25519:
                    expectedPublicKey =
                    [
                        0x3B, 0x8B, 0x15, 0x1C, 0x62, 0xAD, 0x64, 0x85, 0xAB, 0x28, 0x8C, 0x4C, 0xF0, 0x7A, 0xE4, 0xCE,
                        0x2C, 0x58, 0x8F, 0xAC, 0x99, 0x17, 0x02, 0x77, 0x23, 0x04, 0x2F, 0xC4, 0x6D, 0x37, 0xB8, 0x9B
                    ];
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null);
            }

            Assert.Equal(expectedPublicKey, privateKey);
        }

        [Theory]
        // [InlineData(KeyDefinitions.KeyType.RSA1024)]
        // [InlineData(KeyDefinitions.KeyType.RSA2048)]
        // [InlineData(KeyDefinitions.KeyType.RSA3072)]
        // [InlineData(KeyDefinitions.KeyType.RSA4096)]
        [InlineData(KeyDefinitions.KeyType.P256)]
        [InlineData(KeyDefinitions.KeyType.P384)]
        [InlineData(KeyDefinitions.KeyType.P521)]
        [InlineData(KeyDefinitions.KeyType.Ed25519)]
        [InlineData(KeyDefinitions.KeyType.X25519)]
        public void TestKey_GetPrivateKey_ReturnsCorrectPrivateKey(
            KeyDefinitions.KeyType keyType)
        {
            var key = TestKeys.GetTestPrivateKey(keyType);
            Assert.NotNull(key);

            var privateKey = key.GetPrivateKey();
            Assert.NotNull(privateKey);

            byte[] expectedPrivateKey;
            switch (keyType)
            {
                case KeyDefinitions.KeyType.P256:
                    expectedPrivateKey =
                    [
                        0x54, 0x9D, 0x2A, 0x8A, 0x03, 0xE6, 0x2D, 0xC8, 0x29, 0xAD, 0xE4, 0xD6, 0x85, 0x0D, 0xB9, 0x56,
                        0x84, 0x75, 0x14, 0x7C, 0x59, 0xEF, 0x23, 0x8F, 0x12, 0x2A, 0x08, 0xCF, 0x55, 0x7C, 0xDB, 0x91
                    ];
                    break;
                case KeyDefinitions.KeyType.P384:
                    expectedPrivateKey =
                    [
                        0xC7, 0x5D, 0x6B, 0x6A, 0xD3, 0xD1, 0xBC, 0x59, 0x25, 0xEB, 0xE5, 0x89, 0x7C, 0x3D, 0x63, 0xC7,
                        0xFB, 0x6B, 0x0A,
                        0x1D, 0x85, 0xA8, 0x5F, 0x04, 0xB3, 0x48, 0x54, 0x48, 0xF6, 0xC5, 0x9D, 0x97, 0xB8, 0xD6, 0x36,
                        0x06, 0xEE, 0xC0,
                        0xBD, 0x3F, 0xF2, 0x48, 0xB5, 0x7E, 0x92, 0xB9, 0xEA, 0x1B
                    ];
                    break;
                case KeyDefinitions.KeyType.P521:
                    expectedPrivateKey =
                    [
                        0x01, 0x5D, 0xDA, 0x2E, 0x0B, 0x56, 0x65, 0x38, 0xB9, 0x2D, 0x45, 0xDC, 0xAE, 0x1F, 0xC8, 0xB4,
                        0xA4, 0x7A, 0x7C,
                        0x69, 0x92, 0xBD, 0x60, 0x9C, 0x54, 0xED, 0x0C, 0x6D, 0xD2, 0xC3, 0x2F, 0x89, 0xE0, 0x7D, 0xC6,
                        0x8E, 0x9B, 0x08,
                        0xB3, 0xE5, 0x72, 0xE2, 0x19, 0x76, 0x29, 0x5A, 0x74, 0x6E, 0xA6, 0x0E, 0x70, 0xA2, 0xFD, 0x2A,
                        0x17, 0x95, 0x73,
                        0x00, 0xDA, 0xE0, 0xCA, 0xEB, 0xA0, 0x5E, 0x4B, 0xDB
                    ];
                    break;
                case KeyDefinitions.KeyType.X25519:
                    expectedPrivateKey =
                    [
                        0x60, 0x82, 0xB9, 0xFA, 0x5E, 0x9B, 0xEA, 0x4C, 0xAE, 0x11, 0xDC, 0x43, 0x05, 0x2F, 0xAD, 0x4C,
                        0x61, 0xD0, 0xA4, 0x3D, 0xCE, 0xB3, 0x63, 0xB8, 0x05, 0x71, 0x33, 0xF1, 0x38, 0x77, 0x98, 0x4D
                    ];
                    break;
                case KeyDefinitions.KeyType.Ed25519:
                    expectedPrivateKey =
                    [
                        0x3B, 0x8B, 0x15, 0x1C, 0x62, 0xAD, 0x64, 0x85, 0xAB, 0x28, 0x8C, 0x4C, 0xF0, 0x7A, 0xE4, 0xCE,
                        0x2C, 0x58, 0x8F, 0xAC, 0x99, 0x17, 0x02, 0x77, 0x23, 0x04, 0x2F, 0xC4, 0x6D, 0x37, 0xB8, 0x9B
                    ];
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null);
            }

            Assert.Equal(expectedPrivateKey, privateKey);
        }

        [Theory]
        [InlineData(KeyDefinitions.KeyType.RSA1024)]
        [InlineData(KeyDefinitions.KeyType.RSA2048)]
        [InlineData(KeyDefinitions.KeyType.RSA3072)]
        [InlineData(KeyDefinitions.KeyType.RSA4096)]
        [InlineData(KeyDefinitions.KeyType.P256)]
        [InlineData(KeyDefinitions.KeyType.P384)]
        [InlineData(KeyDefinitions.KeyType.P521)]
        [InlineData(KeyDefinitions.KeyType.Ed25519)]
        [InlineData(KeyDefinitions.KeyType.X25519)]
        public void TestKey_GetKeyDefinition_ReturnsCorrectDefintion(
            KeyDefinitions.KeyType keyType)
        {
            var key = TestKeys.GetTestPublicKey(keyType);
            Assert.NotNull(key);

            var keyDef = key.GetKeyDefinition();
            Assert.Equal(keyType, keyDef.KeyType);
        }

        [Fact]
        public void TestKey_LoadRSA_CanReadPublicKey()
        {
            var key = TestKeys.GetTestPublicKey("rsa4096");

            var rsaKey = key.AsRSA();
            Assert.NotNull(rsaKey);
            Assert.Equal(4096, rsaKey.KeySize);
        }

        [Fact]
        public void TestKey_LoadRSA_CanReadPrivateKey()
        {
            var key = TestKeys.GetTestPrivateKey("rsa4096");

            var rsaKey = key.AsRSA();
            Assert.NotNull(rsaKey);
            Assert.Equal(4096, rsaKey.KeySize);
        }

        [Fact]
        public void TestKey_LoadECDsa_CanReadPublicKey()
        {
            var key = TestKeys.GetTestPublicKey("p384");

            var ecKey = key.AsECDsa();
            Assert.NotNull(ecKey);
        }

        [Fact]
        public void TestKey_LoadECDsa_ThrowsOnRSAKey()
        {
            var key = TestKeys.GetTestPublicKey("rsa4096");

            Assert.Throws<InvalidOperationException>(() => key.AsECDsa());
        }

        [Fact]
        public void TestKey_LoadRSA_ThrowsOnECKey()
        {
            var key = TestKeys.GetTestPublicKey("p384");

            Assert.Throws<InvalidOperationException>(() => key.AsRSA());
        }

        [Fact]
        public void TestKey_AsBase64_StripsHeaders()
        {
            var key = TestKeys.GetTestPublicKey("rsa4096");

            string base64 = key.AsBase64String();
            Assert.DoesNotContain("-----BEGIN", base64);
            Assert.DoesNotContain("-----END", base64);
            Assert.DoesNotContain("\n", base64);
        }

        [Fact]
        public void TestKey_AsPemBase64_PreservesFormat()
        {
            var key = TestKeys.GetTestPublicKey("rsa4096");

            string pem = key.AsPemString();
            Assert.Contains("-----BEGIN PUBLIC KEY-----", pem);
            Assert.Contains("-----END PUBLIC KEY-----", pem);
        }

        [Fact]
        public void TestCertificate_Load_CanReadCertificate()
        {
            var cert = TestKeys.GetTestCertificate("rsa4096");

            var x509 = cert.AsX509Certificate2();
            Assert.NotNull(x509);
        }

        [Fact]
        public void TestCertificate_Load_CanReadAttestationCertificate()
        {
            var cert = TestKeys.GetTestCertificate("rsa4096", true);
            Assert.True(cert.IsAttestation);

            var x509 = cert.AsX509Certificate2();
            Assert.NotNull(x509);
        }

        [Fact]
        public void TestKey_Load_ThrowsOnMissingFile()
        {
            Assert.Throws<FileNotFoundException>(() => TestKeys.GetTestPublicKey("invalid"));
        }

        [Fact]
        public void TestCertificate_Load_ThrowsOnMissingFile()
        {
            Assert.Throws<FileNotFoundException>(() => TestKeys.GetTestCertificate("invalid"));
        }
    }
}
