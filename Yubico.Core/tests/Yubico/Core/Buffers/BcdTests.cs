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
using Xunit;

namespace Yubico.Core.Buffers
{
    public class BcdTests
    {
        [Fact]
        public void TestDecodeBcd()
        {
            var bytes = Bcd.DecodeText("01234567");
            byte[] expected = { 0x01, 0x23, 0x45, 0x67 };
            Assert.True(expected.SequenceEqual(bytes));
        }

        [Fact]
        public void TestEncodeBcd()
        {
            var bcd = Bcd.EncodeBytes(new byte[] { 0x01, 0x23, 0x45, 0x67 });
            var expected = "01234567";
            Assert.Equal(expected, bcd);
        }

        [Fact]
        public void TestThrowsNullArgumentException()
        {
            _ = Assert.Throws<ArgumentNullException>(() => Bcd.DecodeText(null!));
        }

        [Theory]
        [InlineData("0000f000")]
        [InlineData(" 000000")]
        [InlineData("I Am Legend!")]
        public void TestInvalidCharacters(string encoded)
        {
            _ = Assert.Throws<ArgumentException>(() => Bcd.DecodeText(encoded));
        }

        [Theory]
        [InlineData("0")]
        [InlineData("000")]
        [InlineData("00000")]
        [InlineData("000000000")]
        [InlineData("000000000000000")]
        [InlineData("00000000000000000")]
        public void TestUnevenStrings(string encoded)
        {
            _ = Assert.Throws<ArgumentException>(() => Bcd.DecodeText(encoded));
        }

        [Theory]
        [InlineData(new byte[] { 0x0f })]
        [InlineData(new byte[] { 0xa0 })]
        [InlineData(new byte[] { 0x00, 0x0a })]
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xa0 })]
        public void TestInvalidBytes(byte[] data)
        {
            _ = Assert.Throws<ArgumentException>(() => Bcd.EncodeBytes(data));
        }
    }
}
