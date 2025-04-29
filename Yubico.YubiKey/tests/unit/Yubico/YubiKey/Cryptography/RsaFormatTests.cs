// Copyright 2021 Yubico AB
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
using System.Linq;
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Cryptography
{
    public class RsaFormatTests
    {
        [Theory]
        [InlineData(1, RsaFormat.Sha1, 1024)]
        [InlineData(1, RsaFormat.Sha256, 1024)]
        [InlineData(1, RsaFormat.Sha384, 1024)]
        [InlineData(1, RsaFormat.Sha512, 1024)]
        [InlineData(1, RsaFormat.Sha1, 2048)]
        [InlineData(1, RsaFormat.Sha256, 2048)]
        [InlineData(1, RsaFormat.Sha384, 2048)]
        [InlineData(1, RsaFormat.Sha512, 2048)]
        [InlineData(2, RsaFormat.Sha1, 1024)]
        [InlineData(2, RsaFormat.Sha256, 1024)]
        [InlineData(2, RsaFormat.Sha384, 1024)]
        [InlineData(2, RsaFormat.Sha1, 2048)]
        [InlineData(2, RsaFormat.Sha256, 2048)]
        [InlineData(2, RsaFormat.Sha384, 2048)]
        [InlineData(2, RsaFormat.Sha512, 2048)]
        public void Format_Sign_CorrectLength(
            int format,
            int digestAlgorithm,
            int keySize)
        {
            byte[] digest =
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20,
                0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F, 0x30,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F, 0x40
            };

            var newSize = digestAlgorithm switch
            {
                RsaFormat.Sha1 => 20,
                RsaFormat.Sha256 => 32,
                RsaFormat.Sha384 => 48,
                _ => 64,
            };

            Array.Resize(ref digest, newSize);

            byte[] formattedData;
            if (format == 1)
            {
                formattedData = RsaFormat.FormatPkcs1Sign(digest, digestAlgorithm, keySize);
            }
            else
            {
                formattedData = RsaFormat.FormatPkcs1Pss(digest, digestAlgorithm, keySize);
            }

            Assert.Equal(keySize / 8, formattedData.Length);
        }

        [Theory]
        [InlineData(1, RsaFormat.Sha1, 1024)]
        [InlineData(1, RsaFormat.Sha256, 1024)]
        [InlineData(1, RsaFormat.Sha384, 1024)]
        [InlineData(1, RsaFormat.Sha512, 1024)]
        [InlineData(1, RsaFormat.Sha1, 2048)]
        [InlineData(1, RsaFormat.Sha256, 2048)]
        [InlineData(1, RsaFormat.Sha384, 2048)]
        [InlineData(1, RsaFormat.Sha512, 2048)]
        [InlineData(2, RsaFormat.Sha1, 1024)]
        [InlineData(2, RsaFormat.Sha256, 1024)]
        [InlineData(2, RsaFormat.Sha384, 1024)]
        [InlineData(2, RsaFormat.Sha1, 2048)]
        [InlineData(2, RsaFormat.Sha256, 2048)]
        [InlineData(2, RsaFormat.Sha384, 2048)]
        [InlineData(2, RsaFormat.Sha512, 2048)]
        public void Format_Sign_CorrectParse(
            int format,
            int digestAlgorithm,
            int keySize)
        {
            byte[] digest =
            {
                0x01, 0xFF, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20,
                0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F, 0x30,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F, 0x40
            };

            var newSize = digestAlgorithm switch
            {
                RsaFormat.Sha1 => 20,
                RsaFormat.Sha256 => 32,
                RsaFormat.Sha384 => 48,
                _ => 64,
            };

            Array.Resize(ref digest, newSize);

            if (format == 1)
            {
                var formattedData = RsaFormat.FormatPkcs1Sign(digest, digestAlgorithm, keySize);
                var isValid = RsaFormat.TryParsePkcs1Verify(
                    formattedData,
                    out var algorithm,
                    out var messageDigest);

                Assert.True(isValid);
                Assert.Equal(digestAlgorithm, algorithm);
                isValid = messageDigest.SequenceEqual(digest);
                Assert.True(isValid);
            }
            else
            {
                var formattedData = RsaFormat.FormatPkcs1Pss(digest, digestAlgorithm, keySize);
                var isValid = RsaFormat.TryParsePkcs1Pss(
                    formattedData,
                    digest,
                    digestAlgorithm,
                    out var mPrimeAndH,
                    out var isVerified);

                Assert.True(isValid);
                Assert.True(isVerified);

                using HashAlgorithm digester = digestAlgorithm switch
                {
                    RsaFormat.Sha1 => CryptographyProviders.Sha1Creator(),
                    RsaFormat.Sha256 => CryptographyProviders.Sha256Creator(),
                    RsaFormat.Sha384 => CryptographyProviders.Sha384Creator(),
                    _ => CryptographyProviders.Sha512Creator(),
                };
                _ = digester.TransformFinalBlock(mPrimeAndH, 0, (2 * digest.Length) + 8);
                var messageDigest = new byte[digester.Hash!.Length];
                Array.Copy(digester.Hash, messageDigest, digester.Hash.Length);

                isValid = messageDigest.SequenceEqual(digest);
            }
        }

        [Theory]
        [InlineData(1, RsaFormat.Sha1, 1024)]
        [InlineData(1, RsaFormat.Sha1, 2048)]
        [InlineData(2, RsaFormat.Sha1, 1024)]
        [InlineData(2, RsaFormat.Sha256, 1024)]
        [InlineData(2, RsaFormat.Sha384, 1024)]
        [InlineData(2, RsaFormat.Sha1, 2048)]
        [InlineData(2, RsaFormat.Sha256, 2048)]
        [InlineData(2, RsaFormat.Sha384, 2048)]
        [InlineData(2, RsaFormat.Sha512, 2048)]
        public void Format_Encrypt_CorrectLength(
            int format,
            int digestAlgorithm,
            int keySize)
        {
            byte[] dataToEncrypt =
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10
            };

            byte[] formattedData;
            if (format == 1)
            {
                formattedData = RsaFormat.FormatPkcs1Encrypt(dataToEncrypt, keySize);
            }
            else
            {
                formattedData = RsaFormat.FormatPkcs1Oaep(dataToEncrypt, digestAlgorithm, keySize);
            }

            Assert.Equal(keySize / 8, formattedData.Length);
        }

        [Theory]
        [InlineData(1, RsaFormat.Sha1, 1024)]
        [InlineData(1, RsaFormat.Sha1, 2048)]
        [InlineData(2, RsaFormat.Sha1, 1024)]
        [InlineData(2, RsaFormat.Sha256, 1024)]
        [InlineData(2, RsaFormat.Sha384, 1024)]
        [InlineData(2, RsaFormat.Sha1, 2048)]
        [InlineData(2, RsaFormat.Sha256, 2048)]
        [InlineData(2, RsaFormat.Sha384, 2048)]
        [InlineData(2, RsaFormat.Sha512, 2048)]
        public void Format_Encrypt_CorrectParse(
            int format,
            int digestAlgorithm,
            int keySize)
        {
            byte[] dataToEncrypt =
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10
            };

            bool isValid;
            byte[] outputData;
            if (format == 1)
            {
                var formattedData = RsaFormat.FormatPkcs1Encrypt(dataToEncrypt, keySize);
                isValid = RsaFormat.TryParsePkcs1Decrypt(formattedData, out outputData);
                Assert.True(isValid);
                isValid = outputData.SequenceEqual(dataToEncrypt);
                Assert.True(isValid);
            }
            else
            {
                var formattedData = RsaFormat.FormatPkcs1Oaep(dataToEncrypt, digestAlgorithm, keySize);
                isValid = RsaFormat.TryParsePkcs1Oaep(formattedData, digestAlgorithm, out outputData);
            }

            Assert.True(isValid);
            isValid = outputData.SequenceEqual(dataToEncrypt);
            Assert.True(isValid);
        }

        [Theory]
        [InlineData(1, RsaFormat.Sha1, 1024)]
        [InlineData(1, RsaFormat.Sha256, 1024)]
        [InlineData(1, RsaFormat.Sha384, 1024)]
        [InlineData(1, RsaFormat.Sha512, 1024)]
        [InlineData(1, RsaFormat.Sha1, 2048)]
        [InlineData(1, RsaFormat.Sha256, 2048)]
        [InlineData(1, RsaFormat.Sha384, 2048)]
        [InlineData(1, RsaFormat.Sha512, 2048)]
        [InlineData(2, RsaFormat.Sha1, 1024)]
        [InlineData(2, RsaFormat.Sha256, 1024)]
        [InlineData(2, RsaFormat.Sha384, 1024)]
        [InlineData(2, RsaFormat.Sha1, 2048)]
        [InlineData(2, RsaFormat.Sha256, 2048)]
        [InlineData(2, RsaFormat.Sha384, 2048)]
        [InlineData(2, RsaFormat.Sha512, 2048)]
        public void Format_Sign_MatchesCSharp(
            int format,
            int digestAlgorithm,
            int keySize)
        {
            byte[] dataToSign =
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20,
            };

            KeyConverter? publicKey = null;
            KeyConverter? privateKey = null;

            using HashAlgorithm digester = digestAlgorithm switch
            {
                RsaFormat.Sha1 => CryptographyProviders.Sha1Creator(),
                RsaFormat.Sha256 => CryptographyProviders.Sha256Creator(),
                RsaFormat.Sha384 => CryptographyProviders.Sha384Creator(),
                _ => CryptographyProviders.Sha512Creator(),
            };

            var keyType = KeyDefinitions.GetByRSALength(keySize).KeyType;
            var hashAlg = digestAlgorithm switch
            {
                RsaFormat.Sha1 => HashAlgorithmName.SHA1,
                RsaFormat.Sha256 => HashAlgorithmName.SHA256,
                RsaFormat.Sha384 => HashAlgorithmName.SHA384,
                _ => HashAlgorithmName.SHA512,
            };

            var padding = RSASignaturePadding.Pkcs1;
            if (format != 1)
            {
                padding = RSASignaturePadding.Pss;
            }

            var (testPublicKey, testPrivateKey) = TestKeys.GetKeyPair(keyType);
            try
            {
                _ = digester.TransformFinalBlock(dataToSign, 0, dataToSign.Length);
                var formattedData = format == 1
                    ? RsaFormat.FormatPkcs1Sign(digester.Hash, digestAlgorithm, keySize)
                    : RsaFormat.FormatPkcs1Pss(digester.Hash, digestAlgorithm, keySize);

                var isValid =
                    CryptoSupport.CSharpRawRsaPrivate(testPrivateKey, formattedData, out var signature);
                Assert.True(isValid);
                Assert.Equal(keySize / 8, formattedData.Length);

                using var rsaPublic = testPublicKey.AsRSA();
                isValid = rsaPublic.VerifyData(dataToSign, signature, hashAlg, padding);
                Assert.True(isValid);
            }
            finally
            {
                publicKey?.Clear();
                privateKey?.Clear();
            }
        }

        [Theory]
        [InlineData(1, RsaFormat.Sha1, 1024)]
        [InlineData(1, RsaFormat.Sha256, 1024)]
        [InlineData(1, RsaFormat.Sha384, 1024)]
        [InlineData(1, RsaFormat.Sha512, 1024)]
        [InlineData(1, RsaFormat.Sha1, 2048)]
        [InlineData(1, RsaFormat.Sha256, 2048)]
        [InlineData(1, RsaFormat.Sha384, 2048)]
        [InlineData(1, RsaFormat.Sha512, 2048)]
        [InlineData(2, RsaFormat.Sha1, 1024)]
        [InlineData(2, RsaFormat.Sha256, 1024)]
        [InlineData(2, RsaFormat.Sha384, 1024)]
        [InlineData(2, RsaFormat.Sha1, 2048)]
        [InlineData(2, RsaFormat.Sha256, 2048)]
        [InlineData(2, RsaFormat.Sha384, 2048)]
        [InlineData(2, RsaFormat.Sha512, 2048)]
        public void Parse_Sign_MatchesCSharp(
            int format,
            int digestAlgorithm,
            int keySize)
        {
            byte[] dataToSign =
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20,
            };

            KeyConverter? publicKey = null;
            KeyConverter? privateKey = null;

            using HashAlgorithm digester = digestAlgorithm switch
            {
                RsaFormat.Sha1 => CryptographyProviders.Sha1Creator(),
                RsaFormat.Sha256 => CryptographyProviders.Sha256Creator(),
                RsaFormat.Sha384 => CryptographyProviders.Sha384Creator(),
                _ => CryptographyProviders.Sha512Creator(),
            };

            var keyType = KeyDefinitions.GetByRSALength(keySize).KeyType;
            var hashAlg = digestAlgorithm switch
            {
                RsaFormat.Sha1 => HashAlgorithmName.SHA1,
                RsaFormat.Sha256 => HashAlgorithmName.SHA256,
                RsaFormat.Sha384 => HashAlgorithmName.SHA384,
                _ => HashAlgorithmName.SHA512,
            };

            var padding = RSASignaturePadding.Pkcs1;
            if (format != 1)
            {
                padding = RSASignaturePadding.Pss;
            }

            var (testPublicKey, testPrivateKey) = TestKeys.GetKeyPair(keyType);
            try
            {
                using var rsaPrivate = testPrivateKey.AsRSA();
                var signature = rsaPrivate.SignData(dataToSign, hashAlg, padding);
                Assert.Equal(keySize / 8, signature.Length);

                var isValid =
                    CryptoSupport.CSharpRawRsaPublic(testPublicKey, signature, out var formattedData);
                Assert.True(isValid);
                Assert.Equal(keySize / 8, formattedData.Length);

                _ = digester.TransformFinalBlock(dataToSign, 0, dataToSign.Length);

                if (format == 1)
                {
                    isValid = RsaFormat.TryParsePkcs1Verify(formattedData, out var digestAlg, out var digest);

                    Assert.True(isValid);
                    Assert.Equal(digestAlgorithm, digestAlg);
                    isValid = digest.SequenceEqual(digester.Hash!);
                    Assert.True(isValid);
                }
                else
                {
                    isValid = RsaFormat.TryParsePkcs1Pss(
                        formattedData,
                        digester.Hash,
                        digestAlgorithm,
                        out var mPrimePlusH,
                        out var isVerified);

                    Assert.True(isValid);
                    Assert.True(isVerified);
                }
            }
            finally
            {
                publicKey?.Clear();
                privateKey?.Clear();
            }
        }

        [Theory]
        [InlineData(1, RsaFormat.Sha1, 1024)]
        [InlineData(1, RsaFormat.Sha1, 2048)]
        [InlineData(2, RsaFormat.Sha1, 1024)]
        [InlineData(2, RsaFormat.Sha256, 1024)]
        [InlineData(2, RsaFormat.Sha384, 1024)]
        [InlineData(2, RsaFormat.Sha1, 2048)]
        [InlineData(2, RsaFormat.Sha256, 2048)]
        [InlineData(2, RsaFormat.Sha384, 2048)]
        [InlineData(2, RsaFormat.Sha512, 2048)]
        public void Format_Encrypt_MatchesCSharp(
            int format,
            int digestAlgorithm,
            int keySize)
        {
            byte[] dataToEncrypt =
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10
            };

            var keyType = KeyDefinitions.GetByRSALength(keySize).KeyType;
            var padding = RSAEncryptionPadding.Pkcs1;
            if (format != 1)
            {
                padding = digestAlgorithm switch
                {
                    RsaFormat.Sha1 => RSAEncryptionPadding.OaepSHA1,
                    RsaFormat.Sha256 => RSAEncryptionPadding.OaepSHA256,
                    RsaFormat.Sha384 => RSAEncryptionPadding.OaepSHA384,
                    _ => RSAEncryptionPadding.OaepSHA512,
                };
            }

            var (testPublicKey, testPrivateKey) = TestKeys.GetKeyPair(keyType);
            var formattedData = format == 1
                ? RsaFormat.FormatPkcs1Encrypt(dataToEncrypt, keySize)
                : RsaFormat.FormatPkcs1Oaep(dataToEncrypt, digestAlgorithm, keySize);
            Assert.Equal(keySize / 8, formattedData.Length);

            var isValid =
                CryptoSupport.CSharpRawRsaPublic(testPublicKey, formattedData, out var encryptedData);
            Assert.True(isValid);

            using var rsaPrivate = testPrivateKey.AsRSA();
            var decryptedData = rsaPrivate.Decrypt(encryptedData, padding);
            Assert.Equal(keySize / 8, encryptedData.Length);
            Assert.True(decryptedData.SequenceEqual(dataToEncrypt)); // Should I have done this?
        }

        [Theory]
        [InlineData(1, RsaFormat.Sha1, 1024)]
        [InlineData(1, RsaFormat.Sha1, 2048)]
        [InlineData(2, RsaFormat.Sha1, 1024)]
        [InlineData(2, RsaFormat.Sha256, 1024)]
        [InlineData(2, RsaFormat.Sha384, 1024)]
        [InlineData(2, RsaFormat.Sha1, 2048)]
        [InlineData(2, RsaFormat.Sha256, 2048)]
        [InlineData(2, RsaFormat.Sha384, 2048)]
        [InlineData(2, RsaFormat.Sha512, 2048)]
        public void Parse_Encrypt_MatchesCSharp(
            int format,
            int digestAlgorithm,
            int keySize)
        {
            byte[] dataToEncrypt =
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10
            };

            var keyType = KeyDefinitions.GetByRSALength(keySize).KeyType;
            var padding = RSAEncryptionPadding.Pkcs1;
            if (format != 1)
            {
                padding = digestAlgorithm switch
                {
                    RsaFormat.Sha1 => RSAEncryptionPadding.OaepSHA1,
                    RsaFormat.Sha256 => RSAEncryptionPadding.OaepSHA256,
                    RsaFormat.Sha384 => RSAEncryptionPadding.OaepSHA384,
                    _ => RSAEncryptionPadding.OaepSHA512,
                };
            }

            var (testPublicKey, testPrivateKey) = TestKeys.GetKeyPair(keyType);
            using var rsaPublic = testPublicKey.AsRSA();

            var encryptedData = rsaPublic.Encrypt(dataToEncrypt, padding);
            Assert.Equal(keySize / 8, encryptedData.Length);

            var isValid = CryptoSupport.CSharpRawRsaPrivate(testPrivateKey, encryptedData, out var formattedData);
            Assert.True(isValid);
            Assert.Equal(keySize / 8, formattedData.Length);
            if (format == 1)
            {
                Assert.True(
                    RsaFormat.TryParsePkcs1Decrypt(formattedData, out var output));
                Assert.True(
                    output.SequenceEqual(dataToEncrypt));
            }
            else
            {
                Assert.True(
                    RsaFormat.TryParsePkcs1Oaep(formattedData, digestAlgorithm, out var output));
                Assert.True(
                    output.SequenceEqual(dataToEncrypt));
            }
        }
    }
}
