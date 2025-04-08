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
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    public class PivPublicKeyTests
    {
        [Theory]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.Ed25519)]
        public void Create_UsingTestKeys_ReturnsPivPublicKey(
            KeyType keyType)
        {
            var testKey = TestKeys.GetTestPublicKey(keyType);
            var tlvPublicKey = testKey.AsPivPublicKey().PivEncodedPublicKey;

            var keyObject = PivPublicKey.Create(tlvPublicKey, keyType.GetPivAlgorithm());

            Assert.True(keyObject is PivPublicKey);
        }


        [Theory]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        public void Create_ReturnsPivPublicKey(
            KeyType keyType)
        {
            ReadOnlyMemory<byte> keyData = GetKeyData(keyType);

            var keyObject = PivPublicKey.Create(keyData);

            Assert.True(keyObject is PivPublicKey);
        }

        [Theory]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        public void Create_SetsAlgorithmCorrectly(
            KeyType keyType)
        {
            ReadOnlyMemory<byte> keyData = GetKeyData(keyType);

            var keyObject = PivPublicKey.Create(keyData);

            Assert.NotNull(keyObject);
            Assert.Equal(keyType.GetPivAlgorithm(), keyObject.Algorithm);
        }

        [Theory]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        public void Create_SetsEncodedCorrectly(
            KeyType keyType)
        {
            ReadOnlyMemory<byte> keyData = GetKeyData(keyType);
            ReadOnlyMemory<byte> encoding = GetCorrectEncoding(keyType);

            var keyObject = PivPublicKey.Create(keyData);
            Assert.NotNull(keyObject);

            ReadOnlyMemory<byte> getKeyData = keyObject.PivEncodedPublicKey;
            bool compareResult = encoding.Span.SequenceEqual(getKeyData.Span);

            Assert.True(compareResult);
        }

        [Theory]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        public void Create_SetsMetadataEncodedCorrectly(
            KeyType keyType)
        {
            ReadOnlyMemory<byte> keyData = GetKeyData(keyType);
            ReadOnlyMemory<byte> encoding = GetCorrectMetadataEncoding(keyType);

            var keyObject = PivPublicKey.Create(keyData);
            Assert.NotNull(keyObject);

            ReadOnlyMemory<byte> getKeyData = keyObject.YubiKeyEncodedPublicKey;

            bool compareResult = encoding.Span.SequenceEqual(getKeyData.Span);

            Assert.True(compareResult);
        }

        [Theory]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        public void CreateRsa_SetsModulusCorrectly(
            KeyType keyType)
        {
            ReadOnlyMemory<byte> keyData = GetKeyData(keyType);
            ReadOnlyMemory<byte> modulus = GetModulus(keyType);

            var keyObject = PivPublicKey.Create(keyData);

            Assert.NotNull(keyObject);
            Assert.True(keyObject is PivRsaPublicKey);

            var rsaObject = (PivRsaPublicKey)keyObject;

            ReadOnlySpan<byte> getModulus = rsaObject.Modulus;

            bool compareResult = modulus.Span.SequenceEqual(getModulus);

            Assert.True(compareResult);
        }

        [Theory]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        [InlineData(KeyType.RSA3072)]
        [InlineData(KeyType.RSA4096)]
        public void CreateRsa_SetsExponentCorrectly(
            KeyType keyType)
        {
            ReadOnlyMemory<byte> keyData = SampleKeyPairs.GetPivPublicKey(keyType).PivEncodedPublicKey;
            ReadOnlyMemory<byte> exponent = GetExponent();

            var keyObject = PivPublicKey.Create(keyData);

            Assert.NotNull(keyObject);
            Assert.True(keyObject is PivRsaPublicKey);

            var rsaObject = (PivRsaPublicKey)keyObject;

            ReadOnlySpan<byte> getExponent = rsaObject.PublicExponent;

            bool compareResult = exponent.Span.SequenceEqual(getExponent);

            Assert.True(compareResult);
        }

        [Theory]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        public void CreateEcc_SetsPublicPointCorrectly(
            KeyType keyType)
        {
            ReadOnlyMemory<byte> keyData = GetKeyData(keyType);
            ReadOnlyMemory<byte> publicPoint = GetPoint(keyType);

            var keyObject = PivPublicKey.Create(keyData);

            Assert.NotNull(keyObject);
            Assert.True(keyObject is PivEccPublicKey);

            var eccObject = (PivEccPublicKey)keyObject;

            ReadOnlySpan<byte> getPoint = eccObject.PublicPoint;

            bool compareResult = publicPoint.Span.SequenceEqual(getPoint);

            Assert.True(compareResult);
        }

        [Theory]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        [InlineData(KeyType.RSA3072)]
        public void RsaConstructor_Components_BuildsEncoding(
            KeyType keyType)
        {
            ReadOnlyMemory<byte> modulus = GetModulus(keyType);
            ReadOnlyMemory<byte> exponent = GetExponent();
            ReadOnlyMemory<byte> encoding = GetCorrectEncoding(keyType);

            var rsaPublic = new PivRsaPublicKey(modulus.Span, exponent.Span);

            ReadOnlyMemory<byte> getEncoding = rsaPublic.PivEncodedPublicKey;

            bool compareResult = encoding.Span.SequenceEqual(getEncoding.Span);

            Assert.True(compareResult);
        }

        [Theory]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        [Obsolete("Obsolete")]
        public void EccConstructor_Components_BuildsEncoding(
            KeyType keyType)
        {
            ReadOnlyMemory<byte> point = GetPoint(keyType);
            ReadOnlyMemory<byte> encoding = GetKeyData(keyType);

            var eccPublic = new PivEccPublicKey(point.Span);

            ReadOnlyMemory<byte> getEncoding = eccPublic.PivEncodedPublicKey;

            bool compareResult = encoding.Span.SequenceEqual(getEncoding.Span);

            Assert.True(compareResult);
        }

        [Fact]
        public void Create_NullData_ThrowsException()
        {
            _ = Assert.Throws<ArgumentException>(() => PivPublicKey.Create(null, PivAlgorithm.EccP256));
        }

        [Fact]
        public void Create_BadTag_ThrowsExcpetion()
        {
            Memory<byte> keyData = GetKeyData(KeyType.ECP256);
            keyData.Span[0] = 0x84;
            _ = Assert.Throws<ArgumentException>(() => PivPublicKey.Create(keyData, KeyType.ECP256.GetPivAlgorithm()));
        }

        [Fact]
        public void Rsa_NoExpo_ThrowsExcpetion()
        {
            Memory<byte> keyData = GetKeyData(KeyType.RSA1024);
            Memory<byte> badData = keyData.Slice(keyData.Length - 6);
            _ = Assert.Throws<ArgumentException>(() => PivPublicKey.Create(badData, PivAlgorithm.Rsa1024));
        }

        [Theory]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        public void RsaConstructor_BadMod_ThrowsExcpetion(
            KeyType keyType)
        {
            Memory<byte> keyData = GetBadEncoding(keyType);
            _ = Assert.Throws<ArgumentException>(() => PivPublicKey.Create(keyData));
        }

        [Fact]
        public void RsaConstructorComponents_BadExpo_ThrowsExcpetion()
        {
            Memory<byte> modulus = GetModulus(KeyType.RSA1024);
            var exponent = new Memory<byte>(new byte[] { 0x01, 0x01, 0x00 });
            _ = Assert.Throws<ArgumentException>(() => new PivRsaPublicKey(modulus.Span, exponent.Span));
        }

        [Fact]
        [Obsolete("Obsolete")]
        public void EccConstructor_NullData_ThrowsExcpetion()
        {
            _ = Assert.Throws<ArgumentException>(() => new PivEccPublicKey(null));
        }

        [Theory]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        public void EccConstructor_BadPoint_ThrowsExcpetion(
            KeyType keyType)
        {
            Memory<byte> keyData = GetBadEncoding(keyType);
            _ = Assert.Throws<ArgumentException>(() => PivPublicKey.Create(keyData));
        }

        private static Memory<byte> GetModulus(
            KeyType keyType)
        {
            Memory<byte> keyData = GetCorrectEncoding(keyType);

            int start = 7;
            int count = 128;

            if (keyType == KeyType.RSA2048)
            {
                start = 9;
                count = 256;
            }

            if (keyType == KeyType.RSA3072)
            {
                start = 9;
                count = 384;
            }

            return keyData.Slice(start, count);
        }

        private static byte[] GetExponent()
        {
            return new byte[] { 0x01, 0x00, 0x01 };
        }

        private static Memory<byte> GetPoint(
            KeyType keyType)
        {
            Memory<byte> keyData = GetKeyData(keyType);
            Memory<byte> point = keyData.Slice(5);
            return point;
        }

        private static Memory<byte> GetKeyData(
            KeyType keyType) => keyType switch
        {
            KeyType.RSA1024 => new Memory<byte>(new byte[]
            {
                0x7f, 0x49, 0x81, 0x89,
                0x81, 0x81, 0x80,
                0xd3, 0xbd, 0x4b, 0xca, 0xa9, 0x8b, 0x92, 0x35, 0x0e, 0x9c, 0x8c, 0xb7, 0xc0, 0xf1, 0xf9, 0x35,
                0x56, 0x40, 0x82, 0x17, 0xa3, 0xa6, 0x57, 0xd7, 0xff, 0x0a, 0x15, 0x29, 0x8a, 0x0f, 0xe2, 0xd5,
                0x87, 0x46, 0xc9, 0xcd, 0x27, 0x7d, 0xa4, 0xac, 0x2f, 0xc7, 0xc0, 0x2e, 0x9c, 0xdd, 0x08, 0x42,
                0x05, 0x15, 0x62, 0x9e, 0x98, 0x4a, 0x79, 0x73, 0x1d, 0x83, 0xaf, 0xbe, 0xe9, 0x0e, 0x86, 0x45,
                0xae, 0xe7, 0xd0, 0x31, 0x36, 0xf8, 0x73, 0x34, 0x35, 0xae, 0x21, 0xc9, 0xd4, 0xbf, 0x9c, 0x4f,
                0x3f, 0x38, 0x4e, 0x13, 0xf0, 0x3e, 0xf1, 0x53, 0x12, 0xe2, 0x85, 0x91, 0xbe, 0x9b, 0xe0, 0xbc,
                0x2d, 0x55, 0x20, 0x3a, 0x8a, 0x3a, 0xec, 0x99, 0xdd, 0x15, 0x47, 0x99, 0x8c, 0xdd, 0x61, 0x5c,
                0x6e, 0x17, 0x4c, 0x78, 0x3c, 0xf3, 0xd8, 0x8a, 0x55, 0xd2, 0xb2, 0xa3, 0x88, 0x2d, 0x08, 0x27,
                0x82, 0x04,
                0x00, 0x01, 0x00, 0x01
            }),

            KeyType.RSA2048 => new Memory<byte>(new byte[]
            {
                0x7f, 0x49, 0x82, 0x01, 0x09,
                0x82, 0x03,
                0x01, 0x00, 0x01,
                0x81, 0x82, 0x01, 0x00,
                0xF1, 0x50, 0xBE, 0xFB, 0xB0, 0x9C, 0xAD, 0xFE, 0xF8, 0x0A, 0x3D, 0x10, 0x8C, 0x36, 0x92, 0xDC,
                0x34, 0xB7, 0x09, 0x86, 0x42, 0xC9, 0xCD, 0x00, 0x55, 0xD1, 0xA4, 0xA0, 0x40, 0x61, 0x5A, 0x2A,
                0x8A, 0xB4, 0x7D, 0xAC, 0xA1, 0x34, 0xA2, 0x2F, 0x0A, 0x36, 0xD2, 0x34, 0xB7, 0xD8, 0x72, 0x58,
                0x20, 0xD6, 0x04, 0x66, 0x80, 0x7A, 0x7A, 0x0A, 0xD1, 0x03, 0x32, 0xA2, 0xD0, 0xC9, 0x92, 0x7E,
                0x59, 0xB8, 0x63, 0xF8, 0xFD, 0xA3, 0x0F, 0xD0, 0xF1, 0xA1, 0x48, 0x50, 0xDF, 0x82, 0xDC, 0x4F,
                0x9F, 0x7C, 0x18, 0x02, 0x29, 0x35, 0x72, 0xDD, 0x10, 0x54, 0x80, 0x12, 0x68, 0x89, 0x8F, 0x05,
                0xCA, 0xA0, 0xEB, 0xD4, 0xF0, 0x82, 0x85, 0xB8, 0x67, 0xAD, 0xF3, 0xF7, 0x86, 0x2E, 0xD3, 0x6E,
                0xC8, 0xE0, 0x46, 0xC4, 0x6C, 0x67, 0x57, 0x53, 0x47, 0xC7, 0x38, 0x84, 0xAC, 0xF4, 0xF4, 0x44,
                0x81, 0xAB, 0xDB, 0x64, 0xEE, 0x53, 0xB5, 0x35, 0xAE, 0x92, 0xFF, 0x8E, 0xFE, 0x00, 0xA7, 0xA8,
                0xB2, 0x86, 0x3B, 0x66, 0xDB, 0x8E, 0xA7, 0x07, 0xFF, 0x13, 0x28, 0x49, 0xE5, 0x9B, 0xD1, 0xC8,
                0xD2, 0x2C, 0xF9, 0x84, 0xD5, 0x8A, 0xFF, 0x00, 0x3E, 0x88, 0xFB, 0xC1, 0xE1, 0xF8, 0x37, 0x8E,
                0x9D, 0xDB, 0x5D, 0x45, 0x61, 0x1B, 0x29, 0x29, 0xA5, 0xB7, 0xC3, 0xE7, 0x38, 0xE9, 0x1A, 0x15,
                0xF3, 0x58, 0xDD, 0xCA, 0xE2, 0xE1, 0x3D, 0x86, 0xBA, 0xBC, 0x63, 0xE2, 0xCD, 0xA4, 0x75, 0x3A,
                0xF9, 0x9C, 0xD8, 0x23, 0x0F, 0xD8, 0x18, 0x59, 0xF8, 0x12, 0x29, 0x62, 0xAB, 0xDC, 0xBE, 0xA5,
                0x01, 0xC5, 0x28, 0xC3, 0xE8, 0xA1, 0x65, 0xCF, 0x39, 0x30, 0x66, 0x18, 0x6A, 0xE5, 0xAD, 0xFA,
                0xEC, 0x48, 0xCC, 0xE7, 0xBA, 0x8B, 0xF7, 0x56, 0x6B, 0xDD, 0x7B, 0x56, 0x2A, 0x3B, 0xE7, 0xE9,
                0x00
            }),

            KeyType.ECP256 => new Memory<byte>(new byte[]
            {
                0x7f, 0x49, 0x43,
                0x86, 0x41,
                0x04,
                0xC4, 0x17, 0x7F, 0x2B, 0x96, 0x8F, 0x9C, 0x00, 0x0C, 0x4F, 0x3D, 0x2B, 0x88, 0xB0, 0xAB, 0x5B,
                0x0C, 0x3B, 0x19, 0x42, 0x63, 0x20, 0x8C, 0xA1, 0x2F, 0xEE, 0x1C, 0xB4, 0xD8, 0x81, 0x96, 0x9F,
                0xD8, 0xC8, 0xD0, 0x8D, 0xD1, 0xBB, 0x66, 0x58, 0x00, 0x26, 0x7D, 0x05, 0x34, 0xA8, 0xA3, 0x30,
                0xD1, 0x59, 0xDE, 0x66, 0x01, 0x0E, 0x3F, 0x21, 0x13, 0x29, 0xC5, 0x98, 0x56, 0x07, 0xB5, 0x26
            }),

            _ => new Memory<byte>(new byte[]
            {
                0x7f, 0x49, 0x63,
                0x86, 0x61,
                0x04,
                0xd0, 0x49, 0x7d, 0x23, 0x4e, 0xc5, 0x1b, 0x9b, 0x5a, 0xa9, 0xb6, 0x7b, 0x93, 0xf4, 0x4a, 0xe3,
                0x1d, 0xd5, 0x83, 0x91, 0xec, 0x9f, 0x89, 0xb1, 0x93, 0x31, 0x85, 0x4d, 0x46, 0xed, 0xf9, 0x24,
                0x92, 0x3b, 0x25, 0x94, 0x75, 0xf3, 0xf6, 0xf6, 0x3f, 0xaf, 0xb5, 0xbe, 0xdf, 0xe3, 0x79, 0x4b,
                0x30, 0xcf, 0x91, 0x85, 0x85, 0x84, 0x9c, 0x9a, 0x46, 0x69, 0x00, 0xc4, 0x3b, 0x07, 0x8b, 0xa3,
                0x07, 0x8a, 0x47, 0x6e, 0x0b, 0x17, 0x4d, 0x46, 0xba, 0x3a, 0xb8, 0x38, 0xc4, 0x3c, 0x0f, 0xc3,
                0xa4, 0x41, 0x18, 0xdb, 0xd1, 0x0a, 0x4b, 0x39, 0x93, 0xa1, 0x8b, 0x54, 0xf0, 0xd0, 0x97, 0x83
            }),
        };

        private static Memory<byte> GetCorrectMetadataEncoding(
            KeyType keyType)
        {
            int sliceIndex = keyType switch
            {
                KeyType.RSA1024 => 4,
                KeyType.RSA2048 => 5,
                _ => 3,
            };

            return GetCorrectEncoding(keyType).Slice(sliceIndex);
        }

        private static Memory<byte> GetCorrectEncoding(
            KeyType keyType) => keyType switch
        {
            KeyType.RSA1024 => new Memory<byte>(new byte[]
            {
                // Tag{2}   Length{2}
                // Public Key Tag
                0x7F, 0x49, 0x81, 0x88, // 136 Bytes of payload data
                // Tag{1} Length{2}
                // Modulus Tag 
                0x81, 0x81, 0x80, // 3 bytes
                // Data, 128 Bytes
                0xd3, 0xbd, 0x4b, 0xca, 0xa9, 0x8b, 0x92, 0x35, 0x0e, 0x9c, 0x8c, 0xb7, 0xc0, 0xf1, 0xf9, 0x35,
                0x56, 0x40, 0x82, 0x17, 0xa3, 0xa6, 0x57, 0xd7, 0xff, 0x0a, 0x15, 0x29, 0x8a, 0x0f, 0xe2, 0xd5,
                0x87, 0x46, 0xc9, 0xcd, 0x27, 0x7d, 0xa4, 0xac, 0x2f, 0xc7, 0xc0, 0x2e, 0x9c, 0xdd, 0x08, 0x42,
                0x05, 0x15, 0x62, 0x9e, 0x98, 0x4a, 0x79, 0x73, 0x1d, 0x83, 0xaf, 0xbe, 0xe9, 0x0e, 0x86, 0x45,
                0xae, 0xe7, 0xd0, 0x31, 0x36, 0xf8, 0x73, 0x34, 0x35, 0xae, 0x21, 0xc9, 0xd4, 0xbf, 0x9c, 0x4f,
                0x3f, 0x38, 0x4e, 0x13, 0xf0, 0x3e, 0xf1, 0x53, 0x12, 0xe2, 0x85, 0x91, 0xbe, 0x9b, 0xe0, 0xbc,
                0x2d, 0x55, 0x20, 0x3a, 0x8a, 0x3a, 0xec, 0x99, 0xdd, 0x15, 0x47, 0x99, 0x8c, 0xdd, 0x61, 0x5c,
                0x6e, 0x17, 0x4c, 0x78, 0x3c, 0xf3, 0xd8, 0x8a, 0x55, 0xd2, 0xb2, 0xa3, 0x88, 0x2d, 0x08, 0x27,
                // Tag{1} Length{1}
                // Exponent Tag
                0x82, 0x03, // 2 Bytes
                // Data, 3 Bytes
                0x01, 0x00, 0x01
            }),

            KeyType.RSA2048 => new Memory<byte>(new byte[]
            {
                0x7F, 0x49, 0x82, 0x01, 0x09,
                0x81, 0x82, 0x01, 0x00,
                0xF1, 0x50, 0xBE, 0xFB, 0xB0, 0x9C, 0xAD, 0xFE, 0xF8, 0x0A, 0x3D, 0x10, 0x8C, 0x36, 0x92, 0xDC,
                0x34, 0xB7, 0x09, 0x86, 0x42, 0xC9, 0xCD, 0x00, 0x55, 0xD1, 0xA4, 0xA0, 0x40, 0x61, 0x5A, 0x2A,
                0x8A, 0xB4, 0x7D, 0xAC, 0xA1, 0x34, 0xA2, 0x2F, 0x0A, 0x36, 0xD2, 0x34, 0xB7, 0xD8, 0x72, 0x58,
                0x20, 0xD6, 0x04, 0x66, 0x80, 0x7A, 0x7A, 0x0A, 0xD1, 0x03, 0x32, 0xA2, 0xD0, 0xC9, 0x92, 0x7E,
                0x59, 0xB8, 0x63, 0xF8, 0xFD, 0xA3, 0x0F, 0xD0, 0xF1, 0xA1, 0x48, 0x50, 0xDF, 0x82, 0xDC, 0x4F,
                0x9F, 0x7C, 0x18, 0x02, 0x29, 0x35, 0x72, 0xDD, 0x10, 0x54, 0x80, 0x12, 0x68, 0x89, 0x8F, 0x05,
                0xCA, 0xA0, 0xEB, 0xD4, 0xF0, 0x82, 0x85, 0xB8, 0x67, 0xAD, 0xF3, 0xF7, 0x86, 0x2E, 0xD3, 0x6E,
                0xC8, 0xE0, 0x46, 0xC4, 0x6C, 0x67, 0x57, 0x53, 0x47, 0xC7, 0x38, 0x84, 0xAC, 0xF4, 0xF4, 0x44,
                0x81, 0xAB, 0xDB, 0x64, 0xEE, 0x53, 0xB5, 0x35, 0xAE, 0x92, 0xFF, 0x8E, 0xFE, 0x00, 0xA7, 0xA8,
                0xB2, 0x86, 0x3B, 0x66, 0xDB, 0x8E, 0xA7, 0x07, 0xFF, 0x13, 0x28, 0x49, 0xE5, 0x9B, 0xD1, 0xC8,
                0xD2, 0x2C, 0xF9, 0x84, 0xD5, 0x8A, 0xFF, 0x00, 0x3E, 0x88, 0xFB, 0xC1, 0xE1, 0xF8, 0x37, 0x8E,
                0x9D, 0xDB, 0x5D, 0x45, 0x61, 0x1B, 0x29, 0x29, 0xA5, 0xB7, 0xC3, 0xE7, 0x38, 0xE9, 0x1A, 0x15,
                0xF3, 0x58, 0xDD, 0xCA, 0xE2, 0xE1, 0x3D, 0x86, 0xBA, 0xBC, 0x63, 0xE2, 0xCD, 0xA4, 0x75, 0x3A,
                0xF9, 0x9C, 0xD8, 0x23, 0x0F, 0xD8, 0x18, 0x59, 0xF8, 0x12, 0x29, 0x62, 0xAB, 0xDC, 0xBE, 0xA5,
                0x01, 0xC5, 0x28, 0xC3, 0xE8, 0xA1, 0x65, 0xCF, 0x39, 0x30, 0x66, 0x18, 0x6A, 0xE5, 0xAD, 0xFA,
                0xEC, 0x48, 0xCC, 0xE7, 0xBA, 0x8B, 0xF7, 0x56, 0x6B, 0xDD, 0x7B, 0x56, 0x2A, 0x3B, 0xE7, 0xE9,
                0x82, 0x03,
                0x01, 0x00, 0x01
            }),

            KeyType.RSA3072 => new Memory<byte>(new byte[]
            {
                0x7F, 0x49, 0x82, 0x01, 0x89,
                0x81, 0x82, 0x01, 0x80,
                0xF1, 0x50, 0xBE, 0xFB, 0xB0, 0x9C, 0xAD, 0xFE, 0xF8, 0x0A, 0x3D, 0x10, 0x8C, 0x36, 0x92, 0xDC,
                0x34, 0xB7, 0x09, 0x86, 0x42, 0xC9, 0xCD, 0x00, 0x55, 0xD1, 0xA4, 0xA0, 0x40, 0x61, 0x5A, 0x2A,
                0x8A, 0xB4, 0x7D, 0xAC, 0xA1, 0x34, 0xA2, 0x2F, 0x0A, 0x36, 0xD2, 0x34, 0xB7, 0xD8, 0x72, 0x58,
                0x20, 0xD6, 0x04, 0x66, 0x80, 0x7A, 0x7A, 0x0A, 0xD1, 0x03, 0x32, 0xA2, 0xD0, 0xC9, 0x92, 0x7E,
                0x59, 0xB8, 0x63, 0xF8, 0xFD, 0xA3, 0x0F, 0xD0, 0xF1, 0xA1, 0x48, 0x50, 0xDF, 0x82, 0xDC, 0x4F,
                0x9F, 0x7C, 0x18, 0x02, 0x29, 0x35, 0x72, 0xDD, 0x10, 0x54, 0x80, 0x12, 0x68, 0x89, 0x8F, 0x05,
                0xCA, 0xA0, 0xEB, 0xD4, 0xF0, 0x82, 0x85, 0xB8, 0x67, 0xAD, 0xF3, 0xF7, 0x86, 0x2E, 0xD3, 0x6E,
                0xC8, 0xE0, 0x46, 0xC4, 0x6C, 0x67, 0x57, 0x53, 0x47, 0xC7, 0x38, 0x84, 0xAC, 0xF4, 0xF4, 0x44,
                0xF1, 0x50, 0xBE, 0xFB, 0xB0, 0x9C, 0xAD, 0xFE, 0xF8, 0x0A, 0x3D, 0x10, 0x8C, 0x36, 0x92, 0xDC,
                0x34, 0xB7, 0x09, 0x86, 0x42, 0xC9, 0xCD, 0x00, 0x55, 0xD1, 0xA4, 0xA0, 0x40, 0x61, 0x5A, 0x2A,
                0x8A, 0xB4, 0x7D, 0xAC, 0xA1, 0x34, 0xA2, 0x2F, 0x0A, 0x36, 0xD2, 0x34, 0xB7, 0xD8, 0x72, 0x58,
                0x20, 0xD6, 0x04, 0x66, 0x80, 0x7A, 0x7A, 0x0A, 0xD1, 0x03, 0x32, 0xA2, 0xD0, 0xC9, 0x92, 0x7E,
                0x59, 0xB8, 0x63, 0xF8, 0xFD, 0xA3, 0x0F, 0xD0, 0xF1, 0xA1, 0x48, 0x50, 0xDF, 0x82, 0xDC, 0x4F,
                0x9F, 0x7C, 0x18, 0x02, 0x29, 0x35, 0x72, 0xDD, 0x10, 0x54, 0x80, 0x12, 0x68, 0x89, 0x8F, 0x05,
                0xCA, 0xA0, 0xEB, 0xD4, 0xF0, 0x82, 0x85, 0xB8, 0x67, 0xAD, 0xF3, 0xF7, 0x86, 0x2E, 0xD3, 0x6E,
                0xC8, 0xE0, 0x46, 0xC4, 0x6C, 0x67, 0x57, 0x53, 0x47, 0xC7, 0x38, 0x84, 0xAC, 0xF4, 0xF4, 0x44,
                0x81, 0xAB, 0xDB, 0x64, 0xEE, 0x53, 0xB5, 0x35, 0xAE, 0x92, 0xFF, 0x8E, 0xFE, 0x00, 0xA7, 0xA8,
                0xB2, 0x86, 0x3B, 0x66, 0xDB, 0x8E, 0xA7, 0x07, 0xFF, 0x13, 0x28, 0x49, 0xE5, 0x9B, 0xD1, 0xC8,
                0xD2, 0x2C, 0xF9, 0x84, 0xD5, 0x8A, 0xFF, 0x00, 0x3E, 0x88, 0xFB, 0xC1, 0xE1, 0xF8, 0x37, 0x8E,
                0x9D, 0xDB, 0x5D, 0x45, 0x61, 0x1B, 0x29, 0x29, 0xA5, 0xB7, 0xC3, 0xE7, 0x38, 0xE9, 0x1A, 0x15,
                0xF3, 0x58, 0xDD, 0xCA, 0xE2, 0xE1, 0x3D, 0x86, 0xBA, 0xBC, 0x63, 0xE2, 0xCD, 0xA4, 0x75, 0x3A,
                0xF9, 0x9C, 0xD8, 0x23, 0x0F, 0xD8, 0x18, 0x59, 0xF8, 0x12, 0x29, 0x62, 0xAB, 0xDC, 0xBE, 0xA5,
                0x01, 0xC5, 0x28, 0xC3, 0xE8, 0xA1, 0x65, 0xCF, 0x39, 0x30, 0x66, 0x18, 0x6A, 0xE5, 0xAD, 0xFA,
                0xEC, 0x48, 0xCC, 0xE7, 0xBA, 0x8B, 0xF7, 0x56, 0x6B, 0xDD, 0x7B, 0x56, 0x2A, 0x3B, 0xE7, 0xE9,
                0x82, 0x03,
                0x01, 0x00, 0x01
            }),

            KeyType.ECP256 => new Memory<byte>(new byte[]
            {
                0x7F, 0x49, 0x43,
                0x86, 0x41, 0x04,
                0xC4, 0x17, 0x7F, 0x2B, 0x96, 0x8F, 0x9C, 0x00, 0x0C, 0x4F, 0x3D, 0x2B, 0x88, 0xB0, 0xAB, 0x5B,
                0x0C, 0x3B, 0x19, 0x42, 0x63, 0x20, 0x8C, 0xA1, 0x2F, 0xEE, 0x1C, 0xB4, 0xD8, 0x81, 0x96, 0x9F,
                0xD8, 0xC8, 0xD0, 0x8D, 0xD1, 0xBB, 0x66, 0x58, 0x00, 0x26, 0x7D, 0x05, 0x34, 0xA8, 0xA3, 0x30,
                0xD1, 0x59, 0xDE, 0x66, 0x01, 0x0E, 0x3F, 0x21, 0x13, 0x29, 0xC5, 0x98, 0x56, 0x07, 0xB5, 0x26
            }),

            _ => new Memory<byte>(new byte[]
            {
                0x7F, 0x49, 0x63,
                0x86, 0x61, 0x04,
                0xd0, 0x49, 0x7d, 0x23, 0x4e, 0xc5, 0x1b, 0x9b, 0x5a, 0xa9, 0xb6, 0x7b, 0x93, 0xf4, 0x4a, 0xe3,
                0x1d, 0xd5, 0x83, 0x91, 0xec, 0x9f, 0x89, 0xb1, 0x93, 0x31, 0x85, 0x4d, 0x46, 0xed, 0xf9, 0x24,
                0x92, 0x3b, 0x25, 0x94, 0x75, 0xf3, 0xf6, 0xf6, 0x3f, 0xaf, 0xb5, 0xbe, 0xdf, 0xe3, 0x79, 0x4b,
                0x30, 0xcf, 0x91, 0x85, 0x85, 0x84, 0x9c, 0x9a, 0x46, 0x69, 0x00, 0xc4, 0x3b, 0x07, 0x8b, 0xa3,
                0x07, 0x8a, 0x47, 0x6e, 0x0b, 0x17, 0x4d, 0x46, 0xba, 0x3a, 0xb8, 0x38, 0xc4, 0x3c, 0x0f, 0xc3,
                0xa4, 0x41, 0x18, 0xdb, 0xd1, 0x0a, 0x4b, 0x39, 0x93, 0xa1, 0x8b, 0x54, 0xf0, 0xd0, 0x97, 0x83
            }),
        };

        private static Memory<byte> GetBadEncoding(
            KeyType keyType) => keyType switch
        {
            KeyType.RSA1024 => new Memory<byte>(new byte[]
            {
                0x7f, 0x49, 0x81, 0x89,
                0x81, 0x81, 0x81,
                0xd3, 0xbd, 0x4b, 0xca, 0xa9, 0x8b, 0x92, 0x35, 0x0e, 0x9c, 0x8c, 0xb7, 0xc0, 0xf1, 0xf9, 0x35,
                0x56, 0x40, 0x82, 0x17, 0xa3, 0xa6, 0x57, 0xd7, 0xff, 0x0a, 0x15, 0x29, 0x8a, 0x0f, 0xe2, 0xd5,
                0x87, 0x46, 0xc9, 0xcd, 0x27, 0x7d, 0xa4, 0xac, 0x2f, 0xc7, 0xc0, 0x2e, 0x9c, 0xdd, 0x08, 0x42,
                0x05, 0x15, 0x62, 0x9e, 0x98, 0x4a, 0x79, 0x73, 0x1d, 0x83, 0xaf, 0xbe, 0xe9, 0x0e, 0x86, 0x45,
                0xae, 0xe7, 0xd0, 0x31, 0x36, 0xf8, 0x73, 0x34, 0x35, 0xae, 0x21, 0xc9, 0xd4, 0xbf, 0x9c, 0x4f,
                0x3f, 0x38, 0x4e, 0x13, 0xf0, 0x3e, 0xf1, 0x53, 0x12, 0xe2, 0x85, 0x91, 0xbe, 0x9b, 0xe0, 0xbc,
                0x2d, 0x55, 0x20, 0x3a, 0x8a, 0x3a, 0xec, 0x99, 0xdd, 0x15, 0x47, 0x99, 0x8c, 0xdd, 0x61, 0x5c,
                0x6e, 0x17, 0x4c, 0x78, 0x3c, 0xf3, 0xd8, 0x8a, 0x55, 0xd2, 0xb2, 0xa3, 0x88, 0x2d, 0x08, 0x27,
                0x99,
                0x82, 0x03,
                0x01, 0x00, 0x01
            }),

            KeyType.RSA2048 => new Memory<byte>(new byte[]
            {
                0x7f, 0x49, 0x82, 0x01, 0x0A,
                0x81, 0x82, 0x01, 0x01,
                0xF1, 0x50, 0xBE, 0xFB, 0xB0, 0x9C, 0xAD, 0xFE, 0xF8, 0x0A, 0x3D, 0x10, 0x8C, 0x36, 0x92, 0xDC,
                0x34, 0xB7, 0x09, 0x86, 0x42, 0xC9, 0xCD, 0x00, 0x55, 0xD1, 0xA4, 0xA0, 0x40, 0x61, 0x5A, 0x2A,
                0x8A, 0xB4, 0x7D, 0xAC, 0xA1, 0x34, 0xA2, 0x2F, 0x0A, 0x36, 0xD2, 0x34, 0xB7, 0xD8, 0x72, 0x58,
                0x20, 0xD6, 0x04, 0x66, 0x80, 0x7A, 0x7A, 0x0A, 0xD1, 0x03, 0x32, 0xA2, 0xD0, 0xC9, 0x92, 0x7E,
                0x59, 0xB8, 0x63, 0xF8, 0xFD, 0xA3, 0x0F, 0xD0, 0xF1, 0xA1, 0x48, 0x50, 0xDF, 0x82, 0xDC, 0x4F,
                0x9F, 0x7C, 0x18, 0x02, 0x29, 0x35, 0x72, 0xDD, 0x10, 0x54, 0x80, 0x12, 0x68, 0x89, 0x8F, 0x05,
                0xCA, 0xA0, 0xEB, 0xD4, 0xF0, 0x82, 0x85, 0xB8, 0x67, 0xAD, 0xF3, 0xF7, 0x86, 0x2E, 0xD3, 0x6E,
                0xC8, 0xE0, 0x46, 0xC4, 0x6C, 0x67, 0x57, 0x53, 0x47, 0xC7, 0x38, 0x84, 0xAC, 0xF4, 0xF4, 0x44,
                0x81, 0xAB, 0xDB, 0x64, 0xEE, 0x53, 0xB5, 0x35, 0xAE, 0x92, 0xFF, 0x8E, 0xFE, 0x00, 0xA7, 0xA8,
                0xB2, 0x86, 0x3B, 0x66, 0xDB, 0x8E, 0xA7, 0x07, 0xFF, 0x13, 0x28, 0x49, 0xE5, 0x9B, 0xD1, 0xC8,
                0xD2, 0x2C, 0xF9, 0x84, 0xD5, 0x8A, 0xFF, 0x00, 0x3E, 0x88, 0xFB, 0xC1, 0xE1, 0xF8, 0x37, 0x8E,
                0x9D, 0xDB, 0x5D, 0x45, 0x61, 0x1B, 0x29, 0x29, 0xA5, 0xB7, 0xC3, 0xE7, 0x38, 0xE9, 0x1A, 0x15,
                0xF3, 0x58, 0xDD, 0xCA, 0xE2, 0xE1, 0x3D, 0x86, 0xBA, 0xBC, 0x63, 0xE2, 0xCD, 0xA4, 0x75, 0x3A,
                0xF9, 0x9C, 0xD8, 0x23, 0x0F, 0xD8, 0x18, 0x59, 0xF8, 0x12, 0x29, 0x62, 0xAB, 0xDC, 0xBE, 0xA5,
                0x01, 0xC5, 0x28, 0xC3, 0xE8, 0xA1, 0x65, 0xCF, 0x39, 0x30, 0x66, 0x18, 0x6A, 0xE5, 0xAD, 0xFA,
                0xEC, 0x48, 0xCC, 0xE7, 0xBA, 0x8B, 0xF7, 0x56, 0x6B, 0xDD, 0x7B, 0x56, 0x2A, 0x3B, 0xE7, 0xE9,
                0x99,
                0x82, 0x03,
                0x01, 0x00, 0x01
            }),

            KeyType.ECP256 => new Memory<byte>(new byte[]
            {
                0x7f, 0x49, 0x44,
                0x86, 0x42,
                0x04, 0x99,
                0xC4, 0x17, 0x7F, 0x2B, 0x96, 0x8F, 0x9C, 0x00, 0x0C, 0x4F, 0x3D, 0x2B, 0x88, 0xB0, 0xAB, 0x5B,
                0x0C, 0x3B, 0x19, 0x42, 0x63, 0x20, 0x8C, 0xA1, 0x2F, 0xEE, 0x1C, 0xB4, 0xD8, 0x81, 0x96, 0x9F,
                0xD8, 0xC8, 0xD0, 0x8D, 0xD1, 0xBB, 0x66, 0x58, 0x00, 0x26, 0x7D, 0x05, 0x34, 0xA8, 0xA3, 0x30,
                0xD1, 0x59, 0xDE, 0x66, 0x01, 0x0E, 0x3F, 0x21, 0x13, 0x29, 0xC5, 0x98, 0x56, 0x07, 0xB5, 0x26
            }),

            _ => new Memory<byte>(new byte[]
            {
                0x7f, 0x49, 0x64,
                0x86, 0x62,
                0x04, 0x99,
                0xd0, 0x49, 0x7d, 0x23, 0x4e, 0xc5, 0x1b, 0x9b, 0x5a, 0xa9, 0xb6, 0x7b, 0x93, 0xf4, 0x4a, 0xe3,
                0x1d, 0xd5, 0x83, 0x91, 0xec, 0x9f, 0x89, 0xb1, 0x93, 0x31, 0x85, 0x4d, 0x46, 0xed, 0xf9, 0x24,
                0x92, 0x3b, 0x25, 0x94, 0x75, 0xf3, 0xf6, 0xf6, 0x3f, 0xaf, 0xb5, 0xbe, 0xdf, 0xe3, 0x79, 0x4b,
                0x30, 0xcf, 0x91, 0x85, 0x85, 0x84, 0x9c, 0x9a, 0x46, 0x69, 0x00, 0xc4, 0x3b, 0x07, 0x8b, 0xa3,
                0x07, 0x8a, 0x47, 0x6e, 0x0b, 0x17, 0x4d, 0x46, 0xba, 0x3a, 0xb8, 0x38, 0xc4, 0x3c, 0x0f, 0xc3,
                0xa4, 0x41, 0x18, 0xdb, 0xd1, 0x0a, 0x4b, 0x39, 0x93, 0xa1, 0x8b, 0x54, 0xf0, 0xd0, 0x97, 0x83
            }),
        };

        private static byte[] GetSubjectPublicKeyInfo(
            KeyType keyType,
            IPublicKey publicKey)
        {
            byte[] subjectPublicKeyInfo;
            switch (keyType)
            {
                case KeyType.RSA1024:
                case KeyType.RSA2048:
                case KeyType.RSA3072:
                case KeyType.RSA4096:
                    Assert.IsAssignableFrom<RSAPublicKey>(publicKey);
                    var rsa = RSA.Create(((RSAPublicKey)publicKey).Parameters);
                    subjectPublicKeyInfo = rsa.ExportSubjectPublicKeyInfo();
                    break;
                case KeyType.ECP256:
                case KeyType.ECP384:
                case KeyType.ECP521:
                    Assert.IsAssignableFrom<ECPublicKey>(publicKey);
                    var ecDsa = ECDsa.Create(((ECPublicKey)publicKey).Parameters);
                    subjectPublicKeyInfo = ecDsa.ExportSubjectPublicKeyInfo();
                    break;
                case KeyType.Ed25519:
                case KeyType.X25519:
                    Assert.IsAssignableFrom<Curve25519PublicKey>(publicKey);
                    subjectPublicKeyInfo =
                        ((Curve25519PublicKey)publicKey).ExportSubjectPublicKeyInfo();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null);
            }

            return subjectPublicKeyInfo;
        }
    }
}
