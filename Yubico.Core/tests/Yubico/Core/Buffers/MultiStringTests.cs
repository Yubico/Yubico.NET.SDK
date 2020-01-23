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
using System.Text;
using Xunit;

namespace Yubico.Core.Buffers
{
    public class MultiStringTests
    {
        [Fact]
        public void GetStrings_EmptyASCII_ReturnsZeroLengthArray()
        {
            byte[] asciiBytes = Array.Empty<byte>();
            string[] expectedStrings = Array.Empty<string>();

            string[] actualStrings = MultiString.GetStrings(asciiBytes, Encoding.ASCII);

            Assert.Equal(expectedStrings, actualStrings);
        }

        [Fact]
        public void GetStrings_SingleASCIIStringDoubleNull_ReturnsSingleElementArray()
        {
            byte[] asciiBytes = Encoding.ASCII.GetBytes("Test string1\0\0");
            string[] expectedStrings = { "Test string1" };

            string[] actualStrings = MultiString.GetStrings(asciiBytes, Encoding.ASCII);

            Assert.Equal(expectedStrings, actualStrings);
        }

        [Fact]
        public void GetStrings_MultipleASCIIString_ReturnsArray()
        {
            byte[] asciiBytes = Encoding.ASCII.GetBytes("Test string1\0Test string2\0Test string3\0\0");
            string[] expectedStrings = { "Test string1", "Test string2", "Test string3" };
            string[] actualStrings = MultiString.GetStrings(asciiBytes, Encoding.ASCII);

            Assert.Equal(expectedStrings, actualStrings);
        }

        [Fact]
        public void GetBytes_EmptyArray_ReturnsEmptyArray()
        {
            byte[] array = MultiString.GetBytes(Array.Empty<string>(), Encoding.ASCII);

            Assert.Equal(Array.Empty<byte>(), array);
        }

        [Fact]
        public void GetBytes_OneString_ReturnsArrayWithDoubleNullEnding()
        {
            string[] strings = new string[] { "a" };
            byte[] expectedBytes = Encoding.ASCII.GetBytes("a\0\0");
            byte[] actualBytes = MultiString.GetBytes(strings, Encoding.ASCII);

            Assert.Equal(expectedBytes, actualBytes);
        }

        [Fact]
        public void GetBytes_MultipleStrings_SeparatedByNullAndEndsInDoubleNull()
        {
            string[] strings = new string[] { "a", "b", "c" };
            byte[] expectedBytes = Encoding.ASCII.GetBytes("a\0b\0c\0\0");
            byte[] actualBytes = MultiString.GetBytes(strings, Encoding.ASCII);

            Assert.Equal(expectedBytes, actualBytes);
        }
    }
}
