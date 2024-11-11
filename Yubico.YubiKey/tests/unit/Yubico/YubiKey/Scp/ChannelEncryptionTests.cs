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

namespace Yubico.YubiKey.Scp
{
    public class ChannelEncryptionTests
    {
        private static byte[] GetKey() => Hex.HexToBytes("404142434445464748494A4B4C4D4E4F");
        private static byte[] GetPayload() => Hex.HexToBytes("000102030405060708090A0B0C0D0E0F010203");
        private static byte[] GetCorrectEncryptOutput() => Hex.HexToBytes("CB85E66F9DD7CD73F98810F393DB27825434DCE62EB12625D74018388DF2C6D0");

        private static byte[] GetKeyForDecrypt() => Hex.HexToBytes("7A3F4BB6F7081D7E25437674CCA306CB");
        private static byte[] GetBadlyPaddedCiphertext() => Hex.HexToBytes("000102030405060708090A0B0C0D0E0F000102030405060708090A0B0C0D0E0F");
        private static byte[] GetCiphertext() => Hex.HexToBytes("5F67E9E059DF3C52809DC9F6DDFBEF3E");
        private static byte[] GetCorrectDecryptedOutput() => Hex.HexToBytes("050301");
        private static byte[] GetBadKey() => Hex.HexToBytes("40414243");
        private static int GetEncryptionCounter() => 94;

        [Fact]
        public void EncryptData_GivenBadKey_ThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => ChannelEncryption.EncryptData(GetPayload(), GetBadKey(), GetEncryptionCounter()));
        }

        [Fact]
        public void EncryptData_GivenCorrectKeyPayload_ReturnsCorrectly()
        {
            // Arrange
            byte[] payload = GetPayload();
            byte[] key = GetKey();
            int ec = GetEncryptionCounter();

            // Act
            ReadOnlyMemory<byte> output = ChannelEncryption.EncryptData(payload, key, ec);

            // Assert
            Assert.Equal(GetCorrectEncryptOutput(), output);
        }

        [Fact]
        public void DecryptData_GivenBadKey_ThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => ChannelEncryption.DecryptData(GetPayload(), GetBadKey(), GetEncryptionCounter()));
        }

        [Fact]
        public void DecryptData_GivenBadCiphertext_ThrowsSecureChannelException()
        {
            byte[] payload = GetBadlyPaddedCiphertext();
            byte[] key = GetKey();
            int ec = GetEncryptionCounter();

            _ = Assert.Throws<SecureChannelException>(() => ChannelEncryption.DecryptData(payload, key, ec));
        }

        [Fact]
        public void DecryptData_GivenCorrectKeyPayload_ReturnsCorrectly()
        {
            // Arrange
            byte[] payload = GetCiphertext();
            byte[] key = GetKeyForDecrypt();
            int ec = 1;

            // Act
            ReadOnlyMemory<byte> output = ChannelEncryption.DecryptData(payload, key, ec);

            // Assert
            Assert.Equal(GetCorrectDecryptedOutput(), output);
        }
    }
}
