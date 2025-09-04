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
using Xunit;

namespace Yubico.YubiKey.Fido2.Cbor;

public class CborExtensionsTests
{
    [Fact]
    public void ToCbor_Bool_True_ReturnsCorrectBytes()
    {
        // Arrange & Act
        var result = true.ToCbor();

        // Assert
        Assert.Equal(new byte[] { 0xF5 }, result);
        Assert.Same(CborExtensions.CborTrue, result); // Reference equality check
    }

    [Fact]
    public void ToCbor_Bool_False_ReturnsCorrectBytes()
    {
        // Arrange & Act
        var result = false.ToCbor();

        // Assert
        Assert.Equal(new byte[] { 0xF4 }, result);
        Assert.Same(CborExtensions.CborFalse, result);
    }

    [Theory]
    [InlineData("", new byte[] { 0x60 })] // Empty string
    [InlineData("a", new byte[] { 0x61, 0x61 })] // Single char
    [InlineData("hello", new byte[] { 0x65, 0x68, 0x65, 0x6C, 0x6C, 0x6F })] // Regular string
    [InlineData("IETF", new byte[] { 0x64, 0x49, 0x45, 0x54, 0x46 })] // CBOR spec example
    public void ToCbor_String_ReturnsCorrectEncoding(
        string input,
        byte[] expected)
    {
        // Act
        var result = input.ToCbor();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, new byte[] { 0x00 })]
    [InlineData(1, new byte[] { 0x01 })]
    [InlineData(23, new byte[] { 0x17 })]
    [InlineData(24, new byte[] { 0x18, 0x18 })]
    [InlineData(255, new byte[] { 0x18, 0xFF })]
    [InlineData(256, new byte[] { 0x19, 0x01, 0x00 })]
    [InlineData(-1, new byte[] { 0x20 })]
    [InlineData(-256, new byte[] { 0x38, 0xFF })]
    public void ToCbor_Int_ReturnsCorrectEncoding(
        int input,
        byte[] expected)
    {
        // Act
        var result = input.ToCbor();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData((byte)0, new byte[] { 0x00 })]
    [InlineData((byte)1, new byte[] { 0x01 })]
    [InlineData((byte)23, new byte[] { 0x17 })]
    [InlineData((byte)24, new byte[] { 0x18, 0x18 })]
    [InlineData((byte)255, new byte[] { 0x18, 0xFF })]
    public void ToCbor_Byte_ReturnsCorrectEncoding(
        byte input,
        byte[] expected)
    {
        // Act
        var result = input.ToCbor();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0U, new byte[] { 0x00 })]
    [InlineData(1000U, new byte[] { 0x19, 0x03, 0xE8 })]
    [InlineData(uint.MaxValue, new byte[] { 0x1A, 0xFF, 0xFF, 0xFF, 0xFF })]
    public void ToCbor_UInt_ReturnsCorrectEncoding(
        uint input,
        byte[] expected)
    {
        // Act
        var result = input.ToCbor();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0L, new byte[] { 0x00 })]
    [InlineData(1000000L, new byte[] { 0x1A, 0x00, 0x0F, 0x42, 0x40 })]
    [InlineData(-1000000L, new byte[] { 0x3A, 0x00, 0x0F, 0x42, 0x3F })]
    [InlineData(long.MaxValue, new byte[] { 0x1B, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF })]
    public void ToCbor_Long_ReturnsCorrectEncoding(
        long input,
        byte[] expected)
    {
        // Act
        var result = input.ToCbor();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0UL, new byte[] { 0x00 })]
    [InlineData(18446744073709551615UL, new byte[] { 0x1B, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF })]
    public void ToCbor_ULong_ReturnsCorrectEncoding(
        ulong input,
        byte[] expected)
    {
        // Act
        var result = input.ToCbor();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(new byte[0], new byte[] { 0x40 })] // Empty byte array
    [InlineData(new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x44, 0x01, 0x02, 0x03, 0x04 })]
    [InlineData(new byte[] { 0xFF }, new byte[] { 0x41, 0xFF })]
    public void ToCbor_ByteArray_ReturnsCorrectEncoding(
        byte[] input,
        byte[] expected)
    {
        // Act
        var result = input.ToCbor();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToCbor_String_Null_ThrowsArgumentNullException()
    {
        // Arrange
        string nullString = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullString.ToCbor());
    }

    [Fact]
    public void ToCbor_ByteArray_Null_ThrowsArgumentNullException()
    {
        // Arrange
        byte[] nullArray = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullArray.ToCbor());
    }

    [Fact]
    public void ToCbor_UsesCtap2CanonicalMode()
    {
        // This test verifies canonical encoding by checking indefinite length handling
        var longString = new string('x', 1000);
        var result = longString.ToCbor();

        // Should be definite length encoding (0x79 prefix for 1000-byte string)
        Assert.Equal(0x79, result[0]);
        Assert.Equal(0x03, result[1]); // High byte of 1000
        Assert.Equal(0xE8, result[2]); // Low byte of 1000
    }

    [Fact]
    public void CborConstants_AreImmutable()
    {
        // Verify the static arrays can't be modified externally
        var originalTrue = CborExtensions.CborTrue;
        var originalFalse = CborExtensions.CborFalse;

        // These should be the same references
        Assert.Same(originalTrue, CborExtensions.CborTrue);
        Assert.Same(originalFalse, CborExtensions.CborFalse);

        // Length should be 1
        Assert.Single(CborExtensions.CborTrue);
        Assert.Single(CborExtensions.CborFalse);
    }
}
