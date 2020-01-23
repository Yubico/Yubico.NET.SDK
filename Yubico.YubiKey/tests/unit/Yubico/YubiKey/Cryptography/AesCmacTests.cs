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
    public class AesCmacTests
    {
        private static byte[] GetKey() => Hex.HexToBytes("01020304050607080102030405060708");
        private static byte[] GetInput() => Hex.HexToBytes("01010101010101010101010101010101");
        private static byte[] GetShortInput() => Hex.HexToBytes("AAFA4DAC615236");
        private static byte[] GetLongInput() => Hex.HexToBytes("00000000000000000000000600008001360CB43F4301B894CAAFA4DAC615236A");

        [Fact]
        public void AesCmac_GivenNullKey_ThrowsArgumentNullException()
        {
            byte[] input = GetInput();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => AesUtilities.BlockCipher(null, input));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void AesCmac_GivenKeyInput_CalculatesCorrectly()
        {
            // Arrange
            byte[] key = GetKey();
            byte[] input = GetInput();

            // Act
            byte[] result = Cmac.AesCmac(key, input);

            // Assert
            Assert.Equal(result, Hex.HexToBytes("165e74e305641b1365d1422a65e792ad"));
        }

        [Fact]
        public void AesCmac_GivenKeyShortInput_CalculatesCorrectly()
        {
            // Arrange
            byte[] key = GetKey();
            byte[] input = GetShortInput();

            // Act
            byte[] result = Cmac.AesCmac(key, input);

            // Assert
            Assert.Equal(result, Hex.HexToBytes("84438d43c2671c1322be492c768ce18e"));
        }

        [Fact]
        public void AesCmac_GivenKeyLongInput_CalculatesCorrectly()
        {
            // Arrange
            byte[] key = GetKey();
            byte[] input = GetLongInput();

            // Act
            byte[] result = Cmac.AesCmac(key, input);

            // Assert
            Assert.Equal(result, Hex.HexToBytes("72604ec81740e6ce8782da291c747cee"));
        }
    }
}
