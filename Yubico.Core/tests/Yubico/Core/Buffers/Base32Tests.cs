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
    // Test cases derived from RFC 4681
    // (https://datatracker.ietf.org/doc/html/rfc4648)

    public class Base32Tests
    {
        [Fact]
        public void TestEncodeEmpty()
        {
            var encoded = Base32.EncodeBytes(Array.Empty<byte>());
            Assert.True(encoded == string.Empty);
        }

        [Fact]
        public void TestDecodeEmpty()
        {
            var decoded = Base32.DecodeText(string.Empty);
            Assert.True(decoded.Length == 0);
        }

        [Theory]
        [InlineData("f", "MY======")]
        [InlineData("fo", "MZXQ====")]
        [InlineData("foo", "MZXW6===")]
        [InlineData("foob", "MZXW6YQ=")]
        [InlineData("fooba", "MZXW6YTB")]
        [InlineData("foobar", "MZXW6YTBOI======")]
        public void TestEncodeData(string source, string expected)
        {
            var data = source.Select(b => (byte)b).ToArray();
            var encoded = Base32.EncodeBytes(data);
            Assert.Equal(expected, encoded);
        }

        [Theory]
        [InlineData("f", "MY======")]
        [InlineData("fo", "MZXQ====")]
        [InlineData("foo", "MZXW6===")]
        [InlineData("foob", "MZXW6YQ=")]
        [InlineData("fooba", "MZXW6YTB")]
        [InlineData("foobar", "MZXW6YTBOI======")]
        public void TestDecodeString(string expectedString, string source)
        {
            var data = Base32.DecodeText(source);
            var expected = expectedString.Select(c => (byte)c).ToArray();
            Assert.Equal(expected, data);
        }

        [Theory]
        [InlineData(new byte[] { 0x9c }, "TT")]
        [InlineData(new byte[] { 0x00, 0x45 }, "ABCZ")]
        [InlineData(new byte[] { 0xd6, 0xf9 }, "2347")]
        [InlineData(new byte[] { 0x00, 0x45 }, "aBCz")]
        public void TestSuccessfulBoundries(byte[] expected, string encoded)
        {
            var decoded = Base32.DecodeText(encoded);
            Assert.Equal(expected, decoded);
        }

        [Theory]
        [InlineData("@ABC")]
        [InlineData("XYZ[")]
        [InlineData("1234")]
        [InlineData("5678")]
        [InlineData("`abc")]
        [InlineData("xyz{")]
        public void TestUnsuccessfulBoundries(string encoded)
        {
            _ = Assert.Throws<ArgumentException>(() => Base32.DecodeText(encoded));
        }

        [Fact]
        public void TestThrowsNullArgumentException()
        {
            _ = Assert.Throws<ArgumentNullException>(() => Base32.DecodeText(null!));
        }
    }
}
