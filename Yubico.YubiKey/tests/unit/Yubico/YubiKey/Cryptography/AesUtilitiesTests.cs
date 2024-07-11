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
using Xunit;
using Yubico.Core.Buffers;

namespace Yubico.YubiKey.Cryptography
{
    public class AesUtilitiesTests
    {
        private static byte[] GetKey()
        {
            return Hex.HexToBytes("01020304050607080102030405060708");
        }

        private static byte[] GetIV()
        {
            return Hex.HexToBytes("deadbeefdeadbeefdeadbeefdeadbeef");
        }

        private static byte[] GetPlaintext()
        {
            return Hex.HexToBytes("01010101010101010101010101010101");
        }

        private static byte[] GetCiphertext()
        {
            return Hex.HexToBytes("01010101010101010101010101010101");
        }

        [Fact]
        public void AesBlockCipher_GivenNullKey_ThrowsArgumentNullException()
        {
            var plaintext = GetPlaintext();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => AesUtilities.BlockCipher(key: null, plaintext));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void AesBlockCipher_GivenKeyWrongLength_ThrowsArgumentException()
        {
            var plaintext = GetPlaintext();
            _ = Assert.Throws<ArgumentException>(() => AesUtilities.BlockCipher(new byte[9], plaintext));
        }

        [Fact]
        public void AesBlockCipher_GivenPlaintextWrongLength_ThrowsArgumentException()
        {
            var key = GetKey();
            _ = Assert.Throws<ArgumentException>(() => AesUtilities.BlockCipher(key, new byte[9]));
        }

        [Fact]
        public void AesBlockCipher_GivenKeyPlaintext_EncryptsCorrectly()
        {
            // Arrange
            var key = GetKey();
            var plaintext = GetPlaintext();

            // Act
            var result = AesUtilities.BlockCipher(key, plaintext);

            // Assert
            Assert.Equal(result, Hex.HexToBytes("dcc0c378ec111cb23048486ef9d9a6b7"));
        }

        [Fact]
        public void AesCbcEncrypt_GivenNullKey_ThrowsArgumentNullException()
        {
            var plaintext = GetPlaintext();
            var iv = GetIV();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => AesUtilities.AesCbcEncrypt(key: null, iv, plaintext));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void AesCbcEncrypt_GivenKeyWrongLength_ThrowsArgumentException()
        {
            var plaintext = GetPlaintext();
            var iv = GetIV();
            _ = Assert.Throws<ArgumentException>(() => AesUtilities.AesCbcEncrypt(new byte[9], iv, plaintext));
        }


        [Fact]
        public void AesCbcEncrypt_GivenNullIV_ThrowsArgumentNullException()
        {
            var key = GetKey();
            var plaintext = GetPlaintext();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => AesUtilities.AesCbcEncrypt(key, iv: null, plaintext));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void AesCbcEncrypt_GivenIVWrongLength_ThrowsArgumentException()
        {
            var key = GetKey();
            var plaintext = GetPlaintext();
            _ = Assert.Throws<ArgumentException>(() => AesUtilities.AesCbcEncrypt(key, new byte[9], plaintext));
        }

        [Fact]
        public void AesCbcEncrypt_GivenPlaintextWrongLength_ThrowsArgumentException()
        {
            var key = GetKey();
            var iv = GetIV();
            _ = Assert.Throws<ArgumentException>(() => AesUtilities.AesCbcEncrypt(key, iv, new byte[9]));
        }

        [Fact]
        public void AesCbcEncrypt_GivenKeyIVPlaintext_EncryptsCorrectly()
        {
            // Arrange
            var key = GetKey();
            var plaintext = GetPlaintext();
            var iv = GetIV();

            // Act
            var result = AesUtilities.AesCbcEncrypt(key, iv, plaintext);

            // Assert
            Assert.Equal(result, Hex.HexToBytes("da19df061b1bcba151d692a4a9e63901"));
        }

        [Fact]
        public void AesCbcDecrypt_GivenNullKey_ThrowsArgumentNullException()
        {
            var ciphertext = GetCiphertext();
            var iv = GetIV();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => AesUtilities.AesCbcDecrypt(key: null, iv, ciphertext));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void AesCbcDecrypt_GivenKeyWrongLength_ThrowsArgumentException()
        {
            var ciphertext = GetCiphertext();
            var iv = GetIV();
            _ = Assert.Throws<ArgumentException>(() => AesUtilities.AesCbcDecrypt(new byte[9], iv, ciphertext));
        }

        [Fact]
        public void AesCbcDecrypt_GivenNullIV_ThrowsArgumentNullException()
        {
            var key = GetKey();
            var ciphertext = GetCiphertext();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => AesUtilities.AesCbcDecrypt(key, iv: null, ciphertext));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void AesCbcDecrypt_GivenIVWrongLength_ThrowsArgumentException()
        {
            var key = GetKey();
            var ciphertext = GetCiphertext();
            _ = Assert.Throws<ArgumentException>(() => AesUtilities.AesCbcDecrypt(key, new byte[9], ciphertext));
        }

        [Fact]
        public void AesCbcDecrypt_GivenCiphertextWrongLength_ThrowsArgumentException()
        {
            var key = GetKey();
            var iv = GetIV();
            _ = Assert.Throws<ArgumentException>(() => AesUtilities.AesCbcDecrypt(key, iv, new byte[9]));
        }

        [Fact]
        public void AesCbcDecrypt_GivenKeyIVCiphertext_EncryptsCorrectly()
        {
            // Arrange
            var key = GetKey();
            var ciphertext = GetCiphertext();
            var iv = GetIV();

            // Act
            var result = AesUtilities.AesCbcDecrypt(key, iv, ciphertext);

            // Assert
            Assert.Equal(result, Hex.HexToBytes("d1af13631ea24595793ddf5bf6f9c42c"));
        }
    }
}
