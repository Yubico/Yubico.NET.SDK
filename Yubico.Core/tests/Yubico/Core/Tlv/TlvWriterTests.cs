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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Yubico.Core.Tlv.UnitTests;

public class TlvWriterTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TlvWriterTests(
        ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData(0x9B)]
    [InlineData(0x5F11)]
    [InlineData(0x01)]
    [InlineData(0x7A)]
    public void VerifyTag_Valid_Returns(
        int tag)
    {
        void action()
        {
            TlvEncoder.VerifyTag(tag);
        }

        var ex = Record.Exception(action);
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(0x5F1101)]
    [InlineData(0x010000)]
    [InlineData(-1)]
    public void VerifyTag_Invalid_ThrowsException(
        int tag)
    {
#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
        var actual = () => TlvEncoder.VerifyTag(tag);
        Assert.Throws<TlvException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
    }

    [Theory]
    [InlineData(0x00)]
    [InlineData(0x01)]
    [InlineData(0x7F)]
    [InlineData(0x80)]
    [InlineData(0x0100)]
    [InlineData(0xFFFFFF)]
    public void VerifyLength_Valid_Returns(
        int length)
    {
        void action()
        {
            TlvEncoder.VerifyLength(length);
        }

        var ex = Record.Exception(action);
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(0x01000000)]
    [InlineData(-2)]
    public void VerifyLength_Invalid_ThrowsException(
        int length)
    {
#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
        var actual = () => TlvEncoder.VerifyLength(length);
        Assert.Throws<TlvException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
    }

    [Theory]
    [InlineData(0x01, 0x01)]
    [InlineData(0x5F11, 0x7F)]
    [InlineData(0x01, 0x80)]
    [InlineData(0x7A, 0xFF)]
    [InlineData(0x11, 0x0100)]
    [InlineData(0x22, 0xFFFF)]
    [InlineData(0x33, 0x010000)]
    [InlineData(0x44, 0xFFFFFF)]
    public void ComputTagLength_Valid_ReturnsCorrect(
        int tag,
        int length)
    {
        var tagArray1 = new[] { (byte)tag };
        var tagArray2 = new[] { (byte)(tag >> 8), (byte)tag };
        var tagArray = tagArray2;
        if (tag < 0x0100)
        {
            tagArray = tagArray1;
        }

        byte[] lengthArray;
        if (length < 0x80)
        {
            lengthArray = new[] { (byte)length };
        }
        else if (length < 0x0100)
        {
            lengthArray = new byte[] { 0x81, (byte)length };
        }
        else if (length < 0x010000)
        {
            lengthArray = new byte[] { 0x82, (byte)(length >> 8), (byte)length };
        }
        else
        {
            lengthArray = new byte[] { 0x83, (byte)(length >> 16), (byte)(length >> 8), (byte)length };
        }

        var concatenation = tagArray.Concat(lengthArray);
        var expected = concatenation.ToArray();

        var encoding = TlvEncoder.BuildTagAndLength(tag, length);

        var compareResult = encoding.SequenceEqual(expected);

        Assert.True(compareResult);
    }

    [Fact]
    public void AddByte_Encode_ReturnsCorrectEncoding()
    {
        var expected = new byte[] { 0x81, 0x01, 0x11 };
        var writer = new TlvWriter();
        writer.WriteByte(0x81, 0x11);

        var encoding = writer.Encode();
        writer.Clear();

        var compareResult = encoding.SequenceEqual(expected);

        Assert.True(compareResult);
    }

    [Fact]
    public void AddValueEncoded_Encode_ReturnsCorrectEncoding()
    {
        var expected = new byte[] { 0x78, 0x02 };
        var writer = new TlvWriter();
        writer.WriteEncoded(new byte[] { 0x78, 0x02 });

        var encoding = writer.Encode();
        writer.Clear();

        var compareResult = encoding.SequenceEqual(expected);

        Assert.True(compareResult);
    }

    [Fact]
    public void AddInt16_TryEncode_ReturnsCorrect()
    {
        var expected = new byte[]
        {
            0x81, 0x02, 0xF1, 0xF2
        };
        var writer = new TlvWriter();
        writer.WriteInt16(0x81, unchecked((short)0xF1F2));

        var encoding = new byte[4];
        var isWritten = writer.TryEncode(encoding, out var encodingLen);
        writer.Clear();

        var compareResult = encoding.SequenceEqual(expected);

        Assert.True(isWritten);
        Assert.Equal(4, encodingLen);
        Assert.True(compareResult);
    }

    [Fact]
    public void AddInt16Endian_Encode_ReturnsCorrect()
    {
        var expected = new byte[]
        {
            0x7A, 0x02, 0x82, 0x00
        };
        var writer = new TlvWriter();
        writer.WriteInt16(0x7A, 0x0082, false);

        var encoding = writer.Encode();

        var compareResult = encoding.SequenceEqual(expected);

        Assert.True(compareResult);
    }

    [Fact]
    public void AddUInt16_TryEncode_ReturnsCorrect()
    {
        var expected = new byte[]
        {
            0x81, 0x02, 0xF1, 0xF2
        };
        var writer = new TlvWriter();
        writer.WriteUInt16(0x81, 0xF1F2);

        var encoding = new byte[4];
        var isWritten = writer.TryEncode(encoding, out var encodingLen);

        var compareResult = encoding.SequenceEqual(expected);

        Assert.True(isWritten);
        Assert.Equal(4, encodingLen);
        Assert.True(compareResult);
    }

    [Fact]
    public void AddUInt16Endian_Encode_ReturnsCorrect()
    {
        var expected = new byte[]
        {
            0x7A, 0x02, 0x82, 0x00
        };
        var writer = new TlvWriter();
        writer.WriteUInt16(0x7A, 0x0082, false);

        var encoding = writer.Encode();

        var compareResult = encoding.SequenceEqual(expected);

        Assert.True(compareResult);
    }

    [Fact]
    public void AddInt32_TryEncode_ReturnsCorrect()
    {
        var expected = new byte[]
        {
            0x11, 0x04, 0xF1, 0xF2, 0xF3, 0xF4
        };
        var writer = new TlvWriter();
        writer.WriteInt32(0x11, unchecked((int)0xF1F2F3F4));

        var encoding = new byte[6];
        var isWritten = writer.TryEncode(encoding, out var encodingLen);
        writer.Clear();

        var compareResult = encoding.SequenceEqual(expected);

        Assert.True(isWritten);
        Assert.Equal(6, encodingLen);
        Assert.True(compareResult);
    }

    [Fact]
    public void AddInt32Endian_Encode_ReturnsCorrect()
    {
        var expected = new byte[]
        {
            0x22, 0x04, 0xFF, 0x00, 0x00, 0x00
        };
        var writer = new TlvWriter();
        writer.WriteInt32(0x22, unchecked(0x000000FF), false);

        var encoding = writer.Encode();

        var compareResult = encoding.SequenceEqual(expected);

        Assert.True(compareResult);
    }

    [Fact]
    public void ASCIIString_Encode_ReturnsCorrect()
    {
        var expected = new byte[]
        {
            0xA2, 0x04, 0x41, 0x42, 0x43, 0x44
        };
        var value = "ABCD";

        var writer = new TlvWriter();
        writer.WriteString(0xA2, value, Encoding.ASCII);

        var encoding = writer.Encode();

        var compareResult = encoding.SequenceEqual(expected);

        Assert.True(compareResult);
    }

    [Fact]
    public void UTF8String_Encode_ReturnsCorrect()
    {
        var expected = new byte[]
        {
            0xA3, 0x04, 0x41, 0xC2, 0xB1, 0x42
        };
        var value = "A\u00B1B";

        var writer = new TlvWriter();
        writer.WriteString(0xA3, value, Encoding.UTF8);

        var encoding = writer.Encode();

        var compareResult = encoding.SequenceEqual(expected);

        Assert.True(compareResult);
    }

    [Fact]
    public void SimpleNested_Encode_ReturnsCorrect()
    {
        var expected = new byte[]
        {
            0xA1, 0x0C, 0x11, 0x05, 0x01, 0x02, 0x03, 0x04, 0x05, 0x22, 0x03, 0x90, 0xC4, 0x5B
        };

        var writer = new TlvWriter();
        using (var tlvScope = writer.WriteNestedTlv(0xA1))
        {
            var element1 = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            writer.WriteValue(0x11, element1);
            var element2 = new byte[] { 0x90, 0xC4, 0x5B };
            writer.WriteValue(0x22, element2);
            tlvScope.Dispose();
        }

        var encoding = writer.Encode();

        var compareResult = encoding.SequenceEqual(expected);

        Assert.True(compareResult);
    }

    [Fact]
    public void NestedInNested_TryEncode_ReturnsCorrect()
    {
        var expected = new byte[]
        {
            0x04, 0x1A,
            0x5F, 0x21, 0x03, 0x01, 0x02, 0x03,
            0x02, 0x0C,
            0x30, 0x04, 0x11, 0x22, 0x33, 0x44,
            0x31, 0x00,
            0x32, 0x02, 0x7f, 0xff,
            0x5F, 0x22, 0x03, 0x04, 0x05, 0x06
        };

        var writer = new TlvWriter();
        using (writer.WriteNestedTlv(0x04))
        {
            var element1 = new byte[] { 0x01, 0x02, 0x03 };
            writer.WriteValue(0x5F21, element1);
            using (writer.WriteNestedTlv(0x02))
            {
                writer.WriteInt32(0x30, 0x11223344);
                writer.WriteValue(0x31, null);
                writer.WriteInt16(0x32, 0x7FFF);
            }

            var element2 = new byte[] { 0x04, 0x05, 0x06 };
            writer.WriteValue(0x5F22, element2);
        }

        var encoding = new byte[writer.GetEncodedLength()];
        var isWritten = writer.TryEncode(encoding, out var encodingLen);
        writer.Clear();

        var compareResult = encoding.SequenceEqual(expected);

        Assert.True(isWritten);
        Assert.Equal(28, encodingLen);
        Assert.True(compareResult);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public void TryEncode_BufferTooSmall_ReturnsFalse(
        int decrement)
    {
        var writer = new TlvWriter();
        using (writer.WriteNestedTlv(0xA1))
        {
            var element1 = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            writer.WriteValue(0x11, element1);
            var element2 = new byte[] { 0x90, 0xC4, 0x5B };
            writer.WriteValue(0x22, element2);
        }

        var encodingLength = writer.GetEncodedLength();
        byte[] encoding;
        if (decrement == encodingLength)
        {
            encoding = Array.Empty<byte>();
        }
        else if (decrement > encodingLength)
        {
            return;
        }
        else
        {
            encoding = new byte[encodingLength - decrement];
        }

        var isWritten = writer.TryEncode(encoding, out encodingLength);
        writer.Clear();

        Assert.False(isWritten);
        Assert.Equal(0, encodingLength);
    }

    [Fact]
    public void WriteString_NullEncoding_ThrowsExcpetion()
    {
        var value = "ABCD";

        var writer = new TlvWriter();
        writer.WriteString(0xA2, value, Encoding.ASCII);

#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
        var actual = () => writer.WriteString(0x71, value, null);
        Assert.Throws<ArgumentNullException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
    }

    [Fact]
    public void GetEncodedLength_IncompleteSchema_ThrowsException()
    {
        var writer = new TlvWriter();
        using (writer.WriteNestedTlv(0xA1))
        {
            var element1 = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            writer.WriteValue(0x11, element1);
            var element2 = new byte[] { 0x90, 0xC4, 0x5B };
            writer.WriteValue(0x22, element2);
#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
            Action actual = () => writer.GetEncodedLength();
            Assert.Throws<TlvException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
        }
    }

    [Fact]
    public void Encode_IncompleteSchema_ThrowsException()
    {
        var writer = new TlvWriter();
        using (writer.WriteNestedTlv(0xA1))
        {
            var element1 = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            writer.WriteValue(0x11, element1);
            var element2 = new byte[] { 0x90, 0xC4, 0x5B };
            writer.WriteValue(0x22, element2);
#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
            Action actual = () => writer.Encode();
            Assert.Throws<TlvException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
        }
    }

    [Fact]
    public void EncodeTag_Has_Correct_LengthAndOffset()
    {
        _testOutputHelper.WriteLine("Output: ");
        var bytesSize = 128;
        while (bytesSize < 150000)
        {
            var writer = new TlvWriter();
            var data = new byte[bytesSize];

            // My visible data
            data[0] = 0xFF;

            writer.WriteValue(0x1, data);
            var encoding = writer.Encode();
            var reader = new TlvReader(encoding);
            Assert.Equal(bytesSize, reader.PeekLength());

            var bytesAsHex = encoding[..9].Select(b => b.ToString("X2"));
            PrettyPrint(bytesSize, bytesAsHex);

            // Increase for next round
            bytesSize *= 2;
        }
    }

    private void PrettyPrint(
        int number,
        IEnumerable<string> bytesAsHex)
    {
        // Print the contents 
        var numDigits = number.ToString().Length;
        var spacesNeeded = Math.Max(5 - numDigits, 0);
        var padding = new string(' ', spacesNeeded);
        _testOutputHelper.WriteLine(
            $"{padding + number}: {string.Join(",", bytesAsHex)}"
        );
    }
}
