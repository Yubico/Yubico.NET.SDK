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

namespace Yubico.YubiKey.Scp03
{
    public class PaddingTests
    {
        private static byte[] Get1BytePayload() => Hex.HexToBytes("FF");
        private static byte[] GetPadded1BytePayload() => Hex.HexToBytes("FF800000000000000000000000000000");
        private static byte[] Get8BytePayload() => Hex.HexToBytes("0001020380050607");
        private static byte[] GetPadded8BytePayload() => Hex.HexToBytes("00010203800506078000000000000000");
        private static byte[] Get16BytePayload() => Hex.HexToBytes("000102038005060708090A0B0C0D0E0F");
        private static byte[] GetPadded16BytePayload() => Hex.HexToBytes("000102038005060708090A0B0C0D0E0F80000000000000000000000000000000");

        [Fact]
        public void PadToBlockSize_GivenNullPayload_ThrowsArgumentNullException()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => Padding.PadToBlockSize(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void RemovePadding_GivenNullPayload_ThrowsArgumentNullException()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => Padding.RemovePadding(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void PadToBlockSize_Given1BytePayload_ReturnsCorrectlyPaddedBlock()
        {
            // Arrange
            byte[] payload = Get1BytePayload();

            // Act
            byte[] paddedPayload = Padding.PadToBlockSize(payload);

            // Assert
            Assert.Equal(paddedPayload, GetPadded1BytePayload());
        }

        [Fact]
        public void PadToBlockSize_Given8BytePayload_ReturnsCorrectlyPaddedBlock()
        {
            // Arrange
            byte[] payload = Get8BytePayload();

            // Act
            byte[] paddedPayload = Padding.PadToBlockSize(payload);

            // Assert
            Assert.Equal(paddedPayload, GetPadded8BytePayload());
        }

        [Fact]
        public void PadToBlockSize_Given16BytePayload_ReturnsCorrectlyPaddedBlock()
        {
            // Arrange
            byte[] payload = Get16BytePayload();

            // Act
            byte[] paddedPayload = Padding.PadToBlockSize(payload);

            // Assert
            Assert.Equal(paddedPayload, GetPadded16BytePayload());
        }

        [Fact]
        public void RemovePadding_GivenPadded1BytePayload_ReturnsPayloadWithPaddingRemoved()
        {
            // Arrange
            byte[] paddedPayload = GetPadded1BytePayload();

            // Act
            byte[] payload = Padding.RemovePadding(paddedPayload);

            // Assert
            Assert.Equal(payload, Get1BytePayload());
        }

        [Fact]
        public void RemovePadding_GivenPadded8BytePayload_ReturnsPayloadWithPaddingRemoved()
        {
            // Arrange
            byte[] paddedPayload = GetPadded8BytePayload();

            // Act
            byte[] payload = Padding.RemovePadding(paddedPayload);

            // Assert
            Assert.Equal(payload, Get8BytePayload());
        }

        [Fact]
        public void RemovePadding_GivenPadded16BytePayload_ReturnsPayloadWithPaddingRemoved()
        {
            // Arrange
            byte[] paddedPayload = GetPadded16BytePayload();

            // Act
            byte[] payload = Padding.RemovePadding(paddedPayload);

            // Assert
            Assert.Equal(payload, Get16BytePayload());
        }
    }
}
