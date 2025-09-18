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
using System.Text;
using Xunit;

namespace Yubico.Core.Buffers;

public class MultiStringTests
{
    [Fact]
    public void GetStrings_EmptyASCII_ReturnsZeroLengthArray()
    {
        var asciiBytes = Array.Empty<byte>();
        var expectedStrings = Array.Empty<string>();

        var actualStrings = MultiString.GetStrings(asciiBytes, Encoding.ASCII);

        Assert.Equal(expectedStrings, actualStrings);
    }

    [Fact]
    public void GetStrings_SingleASCIIStringDoubleNull_ReturnsSingleElementArray()
    {
        var asciiBytes = Encoding.ASCII.GetBytes("Test string1\0\0");
        string[] expectedStrings = { "Test string1" };

        var actualStrings = MultiString.GetStrings(asciiBytes, Encoding.ASCII);

        Assert.Equal(expectedStrings, actualStrings);
    }

    [Fact]
    public void GetStrings_MultipleASCIIString_ReturnsArray()
    {
        var asciiBytes = Encoding.ASCII.GetBytes("Test string1\0Test string2\0Test string3\0\0");
        string[] expectedStrings = { "Test string1", "Test string2", "Test string3" };
        var actualStrings = MultiString.GetStrings(asciiBytes, Encoding.ASCII);

        Assert.Equal(expectedStrings, actualStrings);
    }

    [Fact]
    public void GetBytes_EmptyArray_ReturnsEmptyArray()
    {
        var array = MultiString.GetBytes(Array.Empty<string>(), Encoding.ASCII);

        Assert.Equal(Array.Empty<byte>(), array);
    }

    [Fact]
    public void GetBytes_OneString_ReturnsArrayWithDoubleNullEnding()
    {
        var strings = new[] { "a" };
        var expectedBytes = Encoding.ASCII.GetBytes("a\0\0");
        var actualBytes = MultiString.GetBytes(strings, Encoding.ASCII);

        Assert.Equal(expectedBytes, actualBytes);
    }

    [Fact]
    public void GetBytes_MultipleStrings_SeparatedByNullAndEndsInDoubleNull()
    {
        var strings = new[] { "a", "b", "c" };
        var expectedBytes = Encoding.ASCII.GetBytes("a\0b\0c\0\0");
        var actualBytes = MultiString.GetBytes(strings, Encoding.ASCII);

        Assert.Equal(expectedBytes, actualBytes);
    }
}
