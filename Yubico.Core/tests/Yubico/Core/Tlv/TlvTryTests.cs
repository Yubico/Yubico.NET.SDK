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
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Yubico.Core.Tlv.UnitTests;

public class TlvTryTests
{
    [Fact]
    public void Tlv_TryReadValue()
    {
        var encoding = new byte[]
        {
            0x02, 0x05, 0x31, 0x32, 0x33, 0x34, 0x35
        };

        var reader = new TlvReader(encoding);
        var validRead = reader.TryReadValue(out var value, 0x02);

        Assert.True(validRead);
        Assert.Equal(5, value.Length);
    }

    [Fact]
    public void Tlv_TryReadValue_ReturnsCorrectValue()
    {
        var encoding = new byte[200];
        FillWithRandomBytes(encoding);
        encoding[0] = 0x12;
        encoding[1] = 0x81;
        encoding[2] = 0x83;
        var expected = encoding.AsSpan(3, 131);

        var reader = new TlvReader(encoding);
        var validRead = reader.TryReadValue(out var value, 0x12);

        Assert.True(validRead);

        var compareResult = expected.SequenceEqual(value.Span);

        Assert.True(compareResult);
    }

    [Fact]
    public void Tlv_TryReadNested()
    {
        var encoding = new byte[]
        {
            0x72, 0x61, 0x0A,
            0x01, 0x02, 0x41, 0x42,
            0x02, 0x04, 0x31, 0x32, 0x33, 0x34
        };

        var reader = new TlvReader(encoding);
        var validRead = reader.TryReadNestedTlv(out var nested, 0x7261);
        if (validRead)
        {
            validRead = nested.TryReadValue(out var value, 0x01);
            if (validRead)
            {
                Assert.Equal(2, value.Length);
                validRead = nested.TryReadValue(out value, 0x02);
                Assert.Equal(4, value.Length);
            }
        }

        Assert.True(validRead);
    }

    [Fact]
    public void Tlv_TryReadByte()
    {
        var encoding = new byte[] { 0xFF, 0x01, 0x11, 0xFE, 0x02, 0x11, 0x22 };

        var reader = new TlvReader(encoding);
        var validRead = reader.TryReadByte(out var value, 0xFF);

        Assert.True(validRead);
        Assert.Equal(0x11, value);
    }

    [Fact]
    public void Tlv_TryReadInt16()
    {
        var encoding = new byte[] { 0xFF, 0x02, 0x11, 0x22, 0xFE, 0x02, 0x33, 0x44 };

        var reader = new TlvReader(encoding);
        var validRead = reader.TryReadInt16(out var value, 0xFF);

        Assert.True(validRead);
        Assert.Equal(0x1122, value);
    }

    [Fact]
    public void Tlv_TryReadInt16_LittleEndian()
    {
        var encoding = new byte[] { 0xFF, 0x02, 0x11, 0x22, 0xFE, 0x02, 0x33, 0x44 };

        var reader = new TlvReader(encoding);
        var validRead = reader.TryReadInt16(out var value, 0xFF);
        if (validRead)
        {
            validRead = reader.TryReadInt16(out value, 0xFE, false);
        }

        Assert.True(validRead);
        Assert.Equal(0x4433, value);
    }

    [Fact]
    public void Tlv_TryReadUInt16()
    {
        var encoding = new byte[] { 0xFF, 0x02, 0xFF, 0x22, 0xFE, 0x02, 0x33, 0xFF };

        var reader = new TlvReader(encoding);
        var validRead = reader.TryReadUInt16(out var value, 0xFF);

        Assert.True(validRead);
        Assert.Equal(0xFF22, value);
    }

    [Fact]
    public void Tlv_TryReadUInt16_LittleEndian()
    {
        var encoding = new byte[] { 0xFF, 0x02, 0xFF, 0x22, 0xFE, 0x02, 0x33, 0xFF };

        var reader = new TlvReader(encoding);
        var validRead = reader.TryReadUInt16(out var value, 0xFF);
        if (validRead)
        {
            validRead = reader.TryReadUInt16(out value, 0xFE, false);
        }

        Assert.True(validRead);
        Assert.Equal(0xFF33, value);
    }

    [Fact]
    public void Tlv_TryReadInt32()
    {
        var encoding = new byte[] { 0x81, 0x04, 0x11, 0x22, 0x33, 0x44 };

        var reader = new TlvReader(encoding);
        var validRead = reader.TryReadInt32(out var value, 0x81);

        Assert.True(validRead);
        Assert.Equal(0x11223344, value);
    }

    [Fact]
    public void Tlv_TryReadInt32_LittleEndian()
    {
        var encoding = new byte[] { 0x82, 0x04, 0x11, 0x22, 0x33, 0x44 };

        var reader = new TlvReader(encoding);
        var validRead = reader.TryReadInt32(out var value, 0x82, false);

        Assert.True(validRead);
        Assert.Equal(0x44332211, value);
    }

    [Fact]
    public void Tlv_TryReadString()
    {
        var expectedValue = "12345";
        var encoding = new byte[]
        {
            0x02, 0x05, 0x31, 0x32, 0x33, 0x34, 0x35
        };

        var reader = new TlvReader(encoding);
        var validRead = reader.TryReadString(out var value, 0x02, Encoding.ASCII);

        Assert.True(validRead);
        Assert.Equal(expectedValue, value);
    }

    [Fact]
    public void TlvTryRead_MultipleValues_Correct()
    {
        var encoding = new byte[]
        {
            0x72, 0x61, 0x0A,
            0x01, 0x02, 0x41, 0x42,
            0x02, 0x04, 0x31, 0x32, 0x33, 0x34
        };

        var reader = new TlvReader(encoding);
        var validRead = reader.TryReadNestedTlv(out var nested, 0x7261);
        if (validRead)
        {
            validRead = nested.TryReadValue(out _, 0x91);
            Assert.False(validRead);
            validRead = nested.TryReadValue(out var value, 0x01);
            if (validRead)
            {
                Assert.Equal(2, value.Length);

                validRead = nested.TryReadValue(out _, 0x92);
                Assert.False(validRead);

                validRead = nested.TryReadValue(out value, 0x02);
                Assert.Equal(4, value.Length);
            }
        }

        Assert.True(validRead);
    }

    [Fact]
    public void TryReadValue_TwoByteLength()
    {
        var encoding = new byte[265];
        FillWithRandomBytes(encoding);
        encoding[0] = 0x7F;
        encoding[1] = 0x11;
        encoding[2] = 0x82;
        encoding[3] = 0x01;
        encoding[4] = 0x04;
        var expected = encoding.AsSpan(5, 260);

        var reader = new TlvReader(encoding);
        var validRead = reader.TryReadValue(out var value, 0x7F11);

        Assert.True(validRead);
        Assert.Equal(260, value.Length);

        var compareResult = expected.SequenceEqual(value.Span);

        Assert.True(compareResult);
    }

    [Fact]
    public void TryReadNested_WrongTag_ReturnsFalse()
    {
        var encoding = new byte[]
        {
            0x81, 0x13,
            0x01, 0x02, 0x31, 0x32,
            0x82, 0x0B,
            0x03, 0x02, 0x41, 0x42,
            0x04, 0x05, 0x61, 0x62, 0x63, 0x64, 0x65,
            0x91, 0x00
        };

        var reader = new TlvReader(encoding);

        var validRead = reader.TryReadNestedTlv(out var nested1, 0x81);
        Assert.True(validRead);
        validRead = nested1.TryReadInt16(out var value1, 0x01);
        Assert.True(validRead);
        Assert.Equal(0x3132, value1);

        validRead = nested1.TryReadNestedTlv(out var nested2, 0xA1);

        Assert.NotNull(nested2);
        Assert.False(validRead);
    }

    [Fact]
    public void TryReadValue_WrongTag_ReturnsFalse()
    {
        var encoding = new byte[]
        {
            0x81, 0x13,
            0x11, 0x08, 0x31, 0x32, 0x82, 0x0B, 0x03, 0x02, 0x41, 0x42,
            0x04, 0x05, 0x61, 0x62, 0x63, 0x64, 0x65,
            0x91, 0x00
        };

        var reader = new TlvReader(encoding);

        var validRead = reader.TryReadNestedTlv(out var nested1, 0x81);
        Assert.True(validRead);
        validRead = nested1.TryReadValue(out var value1, 0x01);

        Assert.Equal(0, value1.Length);
        Assert.False(validRead);
    }

    [Fact]
    public void TryReadValue_InvalidLength_ReturnsFalse()
    {
        var encoding = new byte[]
        {
            0x81, 0x13,
            0x11, 0x80, 0x31, 0x32, 0x82, 0x0B, 0x03, 0x02, 0x41, 0x42,
            0x04, 0x05, 0x61, 0x62, 0x63, 0x64, 0x65,
            0x91, 0x00
        };

        var reader = new TlvReader(encoding);

        var validRead = reader.TryReadNestedTlv(out var nested1, 0x81);
        Assert.True(validRead);
        validRead = nested1.TryReadValue(out var value1, 0x11);

        Assert.Equal(0, value1.Length);
        Assert.False(validRead);
    }

    [Fact]
    public void TryReadValue_NotEnoughData_ReturnsFalse()
    {
        var encoding = new byte[]
        {
            0x81, 0x13,
            0x11, 0x12, 0x31, 0x32, 0x82, 0x0B, 0x03, 0x02, 0x41, 0x42,
            0x04, 0x05, 0x61, 0x62, 0x63, 0x64, 0x65,
            0x91, 0x00
        };

        var reader = new TlvReader(encoding);

        var validRead = reader.TryReadNestedTlv(out var nested1, 0x81);
        Assert.True(validRead);
        validRead = nested1.TryReadValue(out var value1, 0x11);

        Assert.Equal(0, value1.Length);
        Assert.False(validRead);
    }

    [Fact]
    public void TryReadByte_LengthZero_ReturnsFalse()
    {
        var encoding = new byte[]
        {
            0x81, 0x13,
            0x91, 0x00,
            0x11, 0x08, 0x31, 0x32, 0x82, 0x0B, 0x03, 0x02, 0x41, 0x42,
            0x04, 0x05, 0x61, 0x62, 0x63, 0x64, 0x65
        };

        var reader = new TlvReader(encoding);

        var validRead = reader.TryReadNestedTlv(out var nested1, 0x81);
        Assert.True(validRead);
        validRead = nested1.TryReadByte(out var value1, 0x91);

        Assert.Equal(0, value1);
        Assert.False(validRead);
    }

    [Fact]
    public void TryReadByte_LengthTwo_ReturnsFalse()
    {
        var encoding = new byte[]
        {
            0x81, 0x15,
            0x91, 0x02, 0x41, 0x42,
            0x11, 0x08, 0x31, 0x32, 0x82, 0x0B, 0x03, 0x02, 0x41, 0x42,
            0x04, 0x05, 0x61, 0x62, 0x63, 0x64, 0x65
        };

        var reader = new TlvReader(encoding);

        var validRead = reader.TryReadNestedTlv(out var nested1, 0x81);
        Assert.True(validRead);
        validRead = nested1.TryReadByte(out var value1, 0x91);

        Assert.Equal(0, value1);
        Assert.False(validRead);
    }

    [Fact]
    public void TryReadInt16_LengthZero_ReturnsFalse()
    {
        var encoding = new byte[]
        {
            0x81, 0x14,
            0x91, 0xFF, 0x00,
            0x11, 0x08, 0x31, 0x32, 0x82, 0x0B, 0x03, 0x02, 0x41, 0x42,
            0x04, 0x05, 0x61, 0x62, 0x63, 0x64, 0x65
        };

        var reader = new TlvReader(encoding);

        var validRead = reader.TryReadNestedTlv(out var nested1, 0x81);
        Assert.True(validRead);
        validRead = nested1.TryReadInt16(out var value1, 0x91FF);

        Assert.Equal(0, value1);
        Assert.False(validRead);
    }

    [Fact]
    public void TryReadInt16_LengthOne_ReturnsFalse()
    {
        var encoding = new byte[]
        {
            0x81, 0x15,
            0x91, 0xFF, 0x01, 0x41,
            0x11, 0x08, 0x31, 0x32, 0x82, 0x0B, 0x03, 0x02, 0x41, 0x42,
            0x04, 0x05, 0x61, 0x62, 0x63, 0x64, 0x65
        };

        var reader = new TlvReader(encoding);

        var validRead = reader.TryReadNestedTlv(out var nested1, 0x81);
        Assert.True(validRead);
        validRead = nested1.TryReadInt16(out var value1, 0x91FF);

        Assert.Equal(0, value1);
        Assert.False(validRead);
    }

    [Fact]
    public void TryReadUInt16_LengthZero_ReturnsFalse()
    {
        var encoding = new byte[]
        {
            0x81, 0x14,
            0x91, 0xFF, 0x00,
            0x11, 0x08, 0x31, 0x32, 0x82, 0x0B, 0x03, 0x02, 0x41, 0x42,
            0x04, 0x05, 0x61, 0x62, 0x63, 0x64, 0x65
        };

        var reader = new TlvReader(encoding);

        var validRead = reader.TryReadNestedTlv(out var nested1, 0x81);
        Assert.True(validRead);
        validRead = nested1.TryReadUInt16(out var value1, 0x91FF);

        Assert.Equal(0, value1);
        Assert.False(validRead);
    }

    [Fact]
    public void TryReadUInt16_LengthOne_ReturnsFalse()
    {
        var encoding = new byte[]
        {
            0x81, 0x15,
            0x91, 0xFF, 0x01, 0x41,
            0x11, 0x08, 0x31, 0x32, 0x82, 0x0B, 0x03, 0x02, 0x41, 0x42,
            0x04, 0x05, 0x61, 0x62, 0x63, 0x64, 0x65
        };

        var reader = new TlvReader(encoding);

        var validRead = reader.TryReadNestedTlv(out var nested1, 0x81);
        Assert.True(validRead);
        validRead = nested1.TryReadUInt16(out var value1, 0x91FF);

        Assert.Equal(0, value1);
        Assert.False(validRead);
    }

    [Fact]
    public void TryReadInt32_LengthZero_ReturnsFalse()
    {
        var encoding = new byte[256];
        FillWithRandomBytes(encoding);
        encoding[0] = 0x48;
        encoding[1] = 0x81;
        encoding[2] = 0xFD;
        encoding[3] = 0x49;
        encoding[4] = 0x81;
        encoding[5] = 0xF8;
        encoding[254] = 0x4A;
        encoding[255] = 0x00;

        var reader = new TlvReader(encoding);

        var validRead = reader.TryReadNestedTlv(out var nested1, 0x48);
        Assert.True(validRead);
        validRead = nested1.TryReadValue(out var value1, 0x49);
        Assert.True(validRead);
        Assert.Equal(0xF8, value1.Length);

        validRead = nested1.TryReadInt32(out var value2, 0x4A);

        Assert.Equal(0, value2);
        Assert.False(validRead);
    }

    [Fact]
    public void TryReadInt32_LengthOne_ReturnsFalse()
    {
        var encoding = new byte[256];
        FillWithRandomBytes(encoding);
        encoding[0] = 0x48;
        encoding[1] = 0x81;
        encoding[2] = 0xFD;
        encoding[3] = 0x49;
        encoding[4] = 0x81;
        encoding[5] = 0xF7;
        encoding[253] = 0x4A;
        encoding[254] = 0x01;
        encoding[255] = 0x00;

        var reader = new TlvReader(encoding);

        var validRead = reader.TryReadNestedTlv(out var nested1, 0x48);
        Assert.True(validRead);
        validRead = nested1.TryReadValue(out var value1, 0x49);
        Assert.True(validRead);
        Assert.Equal(0xF7, value1.Length);

        validRead = nested1.TryReadInt32(out var value2, 0x4A);

        Assert.Equal(0, value2);
        Assert.False(validRead);
    }

    [Fact]
    public void TryReadInt32_LengthTwo_ReturnsFalse()
    {
        var encoding = new byte[256];
        FillWithRandomBytes(encoding);
        encoding[0] = 0x48;
        encoding[1] = 0x81;
        encoding[2] = 0xFD;
        encoding[3] = 0x49;
        encoding[4] = 0x81;
        encoding[5] = 0xF6;
        encoding[252] = 0x4A;
        encoding[253] = 0x02;
        encoding[254] = 0x7F;
        encoding[255] = 0xFF;

        var reader = new TlvReader(encoding);

        var validRead = reader.TryReadNestedTlv(out var nested1, 0x48);
        Assert.True(validRead);
        validRead = nested1.TryReadValue(out var value1, 0x49);
        Assert.True(validRead);
        Assert.Equal(0xF6, value1.Length);

        validRead = nested1.TryReadInt32(out var value2, 0x4A);

        Assert.Equal(0, value2);
        Assert.False(validRead);
    }

    [Fact]
    public void TryReadInt32_LengthThree_ReturnsFalse()
    {
        var encoding = new byte[256];
        FillWithRandomBytes(encoding);
        encoding[0] = 0x48;
        encoding[1] = 0x81;
        encoding[2] = 0xFD;
        encoding[3] = 0x49;
        encoding[4] = 0x81;
        encoding[5] = 0xF5;
        encoding[251] = 0x4A;
        encoding[252] = 0x03;
        encoding[253] = 0x7F;
        encoding[254] = 0xFF;
        encoding[255] = 0xFF;

        var reader = new TlvReader(encoding);

        var validRead = reader.TryReadNestedTlv(out var nested1, 0x48);
        Assert.True(validRead);
        validRead = nested1.TryReadValue(out var value1, 0x49);
        Assert.True(validRead);
        Assert.Equal(0xF5, value1.Length);

        validRead = nested1.TryReadInt32(out var value2, 0x4A);

        Assert.Equal(0, value2);
        Assert.False(validRead);
    }

    [Fact]
    public void TryReadInt32_LengthFive_ReturnsFalse()
    {
        var encoding = new byte[256];
        FillWithRandomBytes(encoding);
        encoding[0] = 0x48;
        encoding[1] = 0x81;
        encoding[2] = 0xFD;
        encoding[3] = 0x49;
        encoding[4] = 0x81;
        encoding[5] = 0xF3;
        encoding[249] = 0x4A;
        encoding[250] = 0x05;
        encoding[251] = 0x00;
        encoding[252] = 0x7F;
        encoding[253] = 0xFF;
        encoding[254] = 0xFF;
        encoding[255] = 0xFF;

        var reader = new TlvReader(encoding);

        var validRead = reader.TryReadNestedTlv(out var nested1, 0x48);
        Assert.True(validRead);
        validRead = nested1.TryReadValue(out var value1, 0x49);
        Assert.True(validRead);
        Assert.Equal(0xF3, value1.Length);

        validRead = nested1.TryReadInt32(out var value2, 0x4A);

        Assert.Equal(0, value2);
        Assert.False(validRead);
    }

    [Fact]
    public void TryReadInt32_LengthFive_RewindsCorrect()
    {
        var encoding = new byte[256];
        FillWithRandomBytes(encoding);
        encoding[0] = 0x48;
        encoding[1] = 0x81;
        encoding[2] = 0xFD;
        encoding[3] = 0x49;
        encoding[4] = 0x81;
        encoding[5] = 0xF3;
        encoding[249] = 0x4A;
        encoding[250] = 0x05;
        encoding[251] = 0x00;
        encoding[252] = 0x7F;
        encoding[253] = 0xFF;
        encoding[254] = 0xFF;
        encoding[255] = 0xFF;
        var expected = encoding.AsSpan(251, 5);

        var reader = new TlvReader(encoding);

        var validRead = reader.TryReadNestedTlv(out var nested1, 0x48);
        Assert.True(validRead);
        validRead = nested1.TryReadValue(out var value1, 0x49);
        Assert.True(validRead);
        Assert.Equal(0xF3, value1.Length);

        validRead = nested1.TryReadInt32(out var value2, 0x4A);

        Assert.Equal(0, value2);
        Assert.False(validRead);

        validRead = nested1.TryReadValue(out var value3, 0x4A);

        Assert.True(validRead);
        Assert.Equal(5, value3.Length);

        var compareResult = expected.SequenceEqual(value3.Span);

        Assert.True(compareResult);
    }

    private static void FillWithRandomBytes(
        byte[] buffer)
    {
        using var random = RandomNumberGenerator.Create();
        random.GetBytes(buffer);
    }
}
