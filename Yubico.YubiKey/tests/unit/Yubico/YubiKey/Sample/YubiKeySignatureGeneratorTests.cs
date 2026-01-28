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
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Sample
{
    /// <summary>
    /// Unit tests for YubiKeySignatureGenerator.DigestData logic.
    /// These tests verify the fix for the regression introduced in commit 01d2a667
    /// where _algorithm.GetKeySizeBytes() was incorrectly used instead of the hash digest size.
    /// 
    /// Since YubiKeySignatureGenerator is in an example project that cannot be referenced
    /// from unit tests (strong naming issues), these tests verify the digest computation
    /// logic directly using the same approach the fixed code uses.
    /// </summary>
    public class YubiKeySignatureGeneratorDigestDataTests
    {
        private static readonly byte[] TestData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

        /// <summary>
        /// Computes a digest using the same logic as the fixed YubiKeySignatureGenerator.DigestData method.
        /// For RSA: returns the raw digest.
        /// For ECC: pads the digest to key size with leading zeros if needed.
        /// </summary>
        private static byte[] ComputeDigestData(byte[] data, HashAlgorithmName hashAlgorithm, KeyType keyType)
        {
            byte[] digest = ComputeMessageDigest(data, hashAlgorithm);

            // For RSA, return the raw digest - PadRsa handles the signature padding
            if (keyType.IsRSA())
            {
                return digest;
            }

            // For ECC, the digest must match the key size (e.g., 32 bytes for P-256)
            // Pad with leading zeros if necessary
            int keySizeBytes = keyType.GetKeySizeBytes();

            if (digest.Length == keySizeBytes)
            {
                return digest;
            }

            if (digest.Length > keySizeBytes)
            {
                throw new ArgumentException("Digest is larger than key size");
            }

            // Pad with leading zeros
            byte[] paddedDigest = new byte[keySizeBytes];
            int offset = keySizeBytes - digest.Length;
            Array.Copy(digest, 0, paddedDigest, offset, digest.Length);

            return paddedDigest;
        }

        /// <summary>
        /// Computes a message digest using the same logic as MessageDigestOperations.ComputeMessageDigest.
        /// </summary>
        private static byte[] ComputeMessageDigest(byte[] dataToDigest, HashAlgorithmName hashAlgorithm)
        {
            using HashAlgorithm digester = hashAlgorithm.Name switch
            {
                "SHA1" => CryptographyProviders.Sha1Creator(),
                "SHA256" => CryptographyProviders.Sha256Creator(),
                "SHA384" => CryptographyProviders.Sha384Creator(),
                "SHA512" => CryptographyProviders.Sha512Creator(),
                _ => throw new ArgumentException("Unsupported algorithm"),
            };

            byte[] digest = new byte[digester.HashSize / 8];

            _ = digester.TransformFinalBlock(dataToDigest, 0, dataToDigest.Length);
            Array.Copy(digester.Hash!, 0, digest, 0, digest.Length);

            return digest;
        }

        /// <summary>
        /// Computes a digest using the OLD BUGGY logic that was in YubiKeySignatureGenerator.DigestData.
        /// This is used to verify that the bug would cause failures.
        /// </summary>
        private static byte[] ComputeDigestDataBuggy(byte[] data, HashAlgorithmName hashAlgorithm, KeyType keyType)
        {
            using HashAlgorithm digester = hashAlgorithm.Name switch
            {
                "SHA1" => CryptographyProviders.Sha1Creator(),
                "SHA256" => CryptographyProviders.Sha256Creator(),
                "SHA384" => CryptographyProviders.Sha384Creator(),
                "SHA512" => CryptographyProviders.Sha512Creator(),
                _ => throw new ArgumentException("Unsupported algorithm"),
            };

            // BUG: This uses key size (256 bytes for RSA2048) instead of digest size (32 bytes for SHA256)
            int bufferSize = keyType.GetKeySizeBytes();

            byte[] digest = new byte[bufferSize];
            int offset = bufferSize - (digester.HashSize / 8);

            if (offset < 0)
            {
                throw new ArgumentException("Digest too big");
            }

            _ = digester.TransformFinalBlock(data, 0, data.Length);
            // BUG: This tries to copy digest.Length (256) bytes from a 32-byte Hash array
            Array.Copy(digester.Hash!, 0, digest, offset, digest.Length);

            return digest;
        }

        [Theory]
        [InlineData(KeyType.RSA2048, "SHA256", 32)]
        [InlineData(KeyType.RSA2048, "SHA384", 48)]
        [InlineData(KeyType.RSA2048, "SHA512", 64)]
        [InlineData(KeyType.RSA1024, "SHA256", 32)]
        [InlineData(KeyType.RSA3072, "SHA256", 32)]
        [InlineData(KeyType.RSA4096, "SHA256", 32)]
        public void DigestData_RSA_ReturnsCorrectDigestSize(KeyType keyType, string hashName, int expectedSize)
        {
            // Arrange
            var hashAlgorithm = new HashAlgorithmName(hashName);

            // Act
            byte[] digest = ComputeDigestData(TestData, hashAlgorithm, keyType);

            // Assert
            Assert.Equal(expectedSize, digest.Length);
        }

        [Fact]
        public void DigestData_RSA2048_SHA256_FixedVersion_DoesNotThrow()
        {
            // This is the specific scenario from the bug report:
            // RSA2048 with SHA256 - the fixed version should not throw
            var exception = Record.Exception(() => 
                ComputeDigestData(TestData, HashAlgorithmName.SHA256, KeyType.RSA2048));

            Assert.Null(exception);
        }

        [Fact]
        public void DigestData_RSA2048_SHA256_BuggyVersion_Throws()
        {
            // This demonstrates the bug: RSA2048 with SHA256 was throwing because
            // it tried to copy 256 bytes (key size) from a 32-byte array (hash size)
            Assert.Throws<ArgumentException>(() => 
                ComputeDigestDataBuggy(TestData, HashAlgorithmName.SHA256, KeyType.RSA2048));
        }

        [Theory]
        [InlineData(KeyType.ECP256, "SHA256", 32)]  // Digest matches key size
        [InlineData(KeyType.ECP384, "SHA256", 48)]  // Digest (32) padded to key size (48)
        [InlineData(KeyType.ECP384, "SHA384", 48)]  // Digest matches key size
        [InlineData(KeyType.ECP521, "SHA256", 66)]  // Digest (32) padded to key size (66)
        [InlineData(KeyType.ECP521, "SHA384", 66)]  // Digest (48) padded to key size (66)
        [InlineData(KeyType.ECP521, "SHA512", 66)]  // Digest (64) padded to key size (66)
        public void DigestData_ECC_ReturnsCorrectDigestSize(KeyType keyType, string hashName, int expectedSize)
        {
            // Arrange
            var hashAlgorithm = new HashAlgorithmName(hashName);

            // Act
            byte[] digest = ComputeDigestData(TestData, hashAlgorithm, keyType);

            // Assert
            Assert.Equal(expectedSize, digest.Length);
        }

        [Theory]
        [InlineData(KeyType.ECP256, "SHA384")]  // SHA384 (48 bytes) > P-256 key size (32 bytes)
        [InlineData(KeyType.ECP256, "SHA512")]  // SHA512 (64 bytes) > P-256 key size (32 bytes)
        public void DigestData_ECC_ThrowsWhenDigestLargerThanKeySize(KeyType keyType, string hashName)
        {
            // Arrange
            var hashAlgorithm = new HashAlgorithmName(hashName);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ComputeDigestData(TestData, hashAlgorithm, keyType));
        }

        [Theory]
        [InlineData(KeyType.ECP384, "SHA256", 16)]  // P-384 (48) - SHA256 (32) = 16 bytes padding
        [InlineData(KeyType.ECP521, "SHA256", 34)]  // P-521 (66) - SHA256 (32) = 34 bytes padding
        [InlineData(KeyType.ECP521, "SHA384", 18)]  // P-521 (66) - SHA384 (48) = 18 bytes padding
        public void DigestData_ECC_PadsWithLeadingZeros(KeyType keyType, string hashName, int expectedPadding)
        {
            // Arrange
            var hashAlgorithm = new HashAlgorithmName(hashName);

            // Act
            byte[] digest = ComputeDigestData(TestData, hashAlgorithm, keyType);

            // Assert - first bytes should be zeros (padding)
            for (int i = 0; i < expectedPadding; i++)
            {
                Assert.Equal(0, digest[i]);
            }

            // The non-zero hash data should start after the padding
            bool hasNonZeroData = false;
            for (int i = expectedPadding; i < digest.Length; i++)
            {
                if (digest[i] != 0)
                {
                    hasNonZeroData = true;
                    break;
                }
            }
            Assert.True(hasNonZeroData, "Expected non-zero hash data after padding");
        }
    }
}
