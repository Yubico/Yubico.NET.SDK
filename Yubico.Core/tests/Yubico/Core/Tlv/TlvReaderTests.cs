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
using System.Text;
using Xunit;

namespace Yubico.Core.Tlv.UnitTests
{
    public class TlvReaderTests
    {
        [Theory]
        [InlineData(0x9B, 0x00, 1, 0x9B)]
        [InlineData(0x5F, 0x11, 2, 0x5F11)]
        [InlineData(0xFF, 0xFF, 2, 0xFFFF)]
        [InlineData(0xFF, 0xFE, 2, 0xFFFE)]
        [InlineData(0x01, 0x00, 1, 0x01)]
        [InlineData(0x7A, 0x00, 1, 0x7A)]
        public void PeekTag_ReturnsCorrect(byte encoding0, byte encoding1, int tagLength, int tag)
        {
            byte[] encoding = { encoding0, encoding1, 0x00 };

            var reader = new TlvReader(encoding);

            // Call PeekTag twice to verify that the second call returns the same
            // tag. The first call should store it locally, did it?
            int getTag;
            int getTagRepeat;
            if (tagLength == 1)
            {
                getTag = reader.PeekTag();
                getTagRepeat = reader.PeekTag();
            }
            else
            {
                getTag = reader.PeekTag(tagLength);
                getTagRepeat = reader.PeekTag(tagLength);
            }

            Assert.Equal(tag, getTag);
            Assert.Equal(tag, getTagRepeat);
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(2, 0x7F)]
        [InlineData(1, 0x80)]
        [InlineData(2, 0x81)]
        [InlineData(1, 0x0100)]
        [InlineData(2, 0x1122)]
        [InlineData(1, 0xFFFF)]
        [InlineData(2, 0x010000)]
        [InlineData(1, 0x313233)]
        [InlineData(2, 0xFFFFFF)]
        public void PeekLength_ReturnsCorrect(int tagLength, int length)
        {
            byte[] encoding = { 0x5F, 0x01, 0x83, 0x82, 0x81, (byte)length };
            var offset = 4 - (tagLength - 1);
            if (length > 0x7F)
            {
                offset--;
                if (length > 0xFF)
                {
                    encoding[4] = (byte)(length >> 8);
                    offset--;
                    if (length > 0xFFFF)
                    {
                        encoding[3] = (byte)(length >> 16);
                        offset--;
                    }
                }
            }

            encoding = encoding.Skip(offset).ToArray();
            var reader = new TlvReader(encoding);

            // Call PeekLength twice to verify that the second call returns the same
            // length. The first call should store it locally, did it?
            int getLength;
            int getLengthRepeat;
            if (tagLength == 1)
            {
                getLength = reader.PeekLength();
                getLengthRepeat = reader.PeekLength();
            }
            else
            {
                getLength = reader.PeekLength(tagLength);
                getLengthRepeat = reader.PeekLength(tagLength);
            }

            Assert.Equal(length, getLength);
            Assert.Equal(length, getLengthRepeat);
        }

        [Fact]
        public void ReadValue_Simple_ReturnsCorrect()
        {
            byte[] encoding =
            {
                0x01, 0x02, 0x11, 0x22
            };
            byte[] expected = { 0x11, 0x22 };

            var reader = new TlvReader(encoding);

            var value = reader.ReadValue(expectedTag: 0x01);

            var compareResult = value.Span.SequenceEqual(expected);

            Assert.True(compareResult);
        }

        [Fact]
        public void ReadValue_Multiple_ReturnsCorrect()
        {
            byte[] encoding =
            {
                0x01, 0x02, 0x11, 0x22, 0x02, 0x03, 0x31, 0x32, 0x33, 0x03, 0x00
            };
            byte[] expected = { 0x31, 0x32, 0x33 };

            var reader = new TlvReader(encoding);

            var value = reader.ReadValue(expectedTag: 0x01);
            Assert.NotEmpty(value.Span.ToArray());
            value = reader.ReadValue(expectedTag: 0x02);

            var compareResult = value.Span.SequenceEqual(expected);

            Assert.True(compareResult);
        }

        [Fact]
        public void ReadNestedTlv_Simple_ReturnsCorrect()
        {
            byte[] encoding =
            {
                0x5F, 0x7C, 0x09, 0x01, 0x02, 0x11, 0x22, 0x02, 0x03, 0x31, 0x32, 0x33
            };
            byte[] expected = { 0x11, 0x22 };

            var reader = new TlvReader(encoding);
            var nestedReader = reader.ReadNestedTlv(expectedTag: 0x5F7C);

            var value = nestedReader.ReadValue(expectedTag: 0x01);

            var compareResult = value.Span.SequenceEqual(expected);

            Assert.True(compareResult);
        }

        [Fact]
        public void ReadNestedTlv_Complex_ReturnsCorrect()
        {
            byte[] encoding =
            {
                0x7C, 0x14, 0x01, 0x02, 0x05, 0x05, 0x7A, 0x09,
                0x51, 0x02, 0x23, 0x24, 0x5F, 0x52, 0x02, 0x33,
                0x34, 0x02, 0x01, 0x00, 0x03, 0x00
            };
            byte[] expected1 = { 0x05, 0x05 };
            byte[] expected2 = { 0x00 };
            var expected3 = Array.Empty<byte>();
            byte[] expected51 = { 0x23, 0x24 };
            byte[] expected52 = { 0x33, 0x34 };

            var reader = new TlvReader(encoding);
            var nestedReader = reader.ReadNestedTlv(expectedTag: 0x7C);

            var value1 = nestedReader.ReadValue(expectedTag: 0x01);
            var innerReader = nestedReader.ReadNestedTlv(expectedTag: 0x7A);
            var value2 = nestedReader.ReadValue(expectedTag: 0x02);
            var value3 = nestedReader.ReadValue(expectedTag: 0x03);

            var value51 = innerReader.ReadValue(expectedTag: 0x51);
            var value52 = innerReader.ReadValue(expectedTag: 0x5F52);

            var compareResult = value1.Span.SequenceEqual(expected1);
            Assert.True(compareResult);

            compareResult = value2.Span.SequenceEqual(expected2);
            Assert.True(compareResult);

            compareResult = value3.Span.SequenceEqual(expected3);
            Assert.True(compareResult);

            compareResult = value51.Span.SequenceEqual(expected51);
            Assert.True(compareResult);

            compareResult = value52.Span.SequenceEqual(expected52);
            Assert.True(compareResult);
        }

        [Theory]
        [InlineData(0x01)]
        [InlineData(0x7F)]
        [InlineData(0x80)]
        [InlineData(0x81)]
        [InlineData(0xFF)]
        public void ReadByte_ReturnsCorrect(byte value)
        {
            byte[] encoding = { 0x01, 0x01, value };

            var reader = new TlvReader(encoding);

            var getValue = reader.ReadByte(expectedTag: 0x01);

            Assert.Equal(value, getValue);
        }

        [Theory]
        [InlineData(0x0000, true)]
        [InlineData(0x0000, false)]
        [InlineData(0x0001, true)]
        [InlineData(0x0001, false)]
        [InlineData(0x007F, true)]
        [InlineData(0x007F, false)]
        [InlineData(unchecked((short)0x8000), true)]
        [InlineData(unchecked((short)0x8000), false)]
        [InlineData(unchecked((short)0x81FF), true)]
        [InlineData(unchecked((short)0x81FF), false)]
        [InlineData(unchecked((short)0xFFFF), true)]
        [InlineData(unchecked((short)0xFFFF), false)]
        public void ReadInt16_ReturnsCorrect(short value, bool bigEndian)
        {
            var value0 = (byte)(value >> 8);
            var value1 = (byte)value;
            byte[] encoding = { 0x01, 0x02, value0, value1 };
            if (bigEndian == false)
            {
                encoding[2] = value1;
                encoding[3] = value0;
            }

            var reader = new TlvReader(encoding);

            var getValue = reader.ReadInt16(expectedTag: 0x01, bigEndian);

            Assert.Equal(value, getValue);
        }

        [Theory]
        [InlineData(0x0000, true)]
        [InlineData(0x0000, false)]
        [InlineData(0x0001, true)]
        [InlineData(0x0001, false)]
        [InlineData(0x007F, true)]
        [InlineData(0x007F, false)]
        [InlineData(0x8000, true)]
        [InlineData(0x8000, false)]
        [InlineData(0x81FF, true)]
        [InlineData(0x81FF, false)]
        [InlineData(0xFFFF, true)]
        [InlineData(0xFFFF, false)]
        public void ReadUInt16_ReturnsCorrect(ushort value, bool bigEndian)
        {
            var value0 = (byte)(value >> 8);
            var value1 = (byte)value;
            byte[] encoding = { 0x01, 0x02, value0, value1 };
            if (bigEndian == false)
            {
                encoding[2] = value1;
                encoding[3] = value0;
            }

            var reader = new TlvReader(encoding);

            var getValue = reader.ReadUInt16(expectedTag: 0x01, bigEndian);

            Assert.Equal(value, getValue);
        }

        [Theory]
        [InlineData(0x00000000, true)]
        [InlineData(0x00000000, false)]
        [InlineData(0x00000001, true)]
        [InlineData(0x00000001, false)]
        [InlineData(0x7FEDCBA9, true)]
        [InlineData(0x7FEDCBA9, false)]
        [InlineData(unchecked((short)0x80000000), true)]
        [InlineData(unchecked((short)0x80000000), false)]
        [InlineData(unchecked((short)0x81FF82FE), true)]
        [InlineData(unchecked((short)0x81FF82FE), false)]
        [InlineData(unchecked((short)0xFFFFFFFF), true)]
        [InlineData(unchecked((short)0xFFFFFFFF), false)]
        public void ReadInt32_ReturnsCorrect(int value, bool bigEndian)
        {
            var value0 = (byte)(value >> 24);
            var value1 = (byte)(value >> 16);
            var value2 = (byte)(value >> 8);
            var value3 = (byte)value;
            byte[] encoding = { 0x01, 0x04, value0, value1, value2, value3 };
            if (bigEndian == false)
            {
                encoding[2] = value3;
                encoding[3] = value2;
                encoding[4] = value1;
                encoding[5] = value0;
            }

            var reader = new TlvReader(encoding);

            var getValue = reader.ReadInt32(expectedTag: 0x01, bigEndian);

            Assert.Equal(value, getValue);
        }

        [Fact]
        public void ReadASCII_ReturnsCorrect()
        {
            byte[] encoding =
            {
                0xA2, 0x04, 0x41, 0x42, 0x43, 0x44
            };
            var expectedValue = "ABCD";

            var reader = new TlvReader(encoding);

            var getValue = reader.ReadString(expectedTag: 0xA2, Encoding.ASCII);

            Assert.Equal(expectedValue, getValue);
        }

        [Fact]
        public void ReadUTF8_ReturnsCorrect()
        {
            byte[] encoding =
            {
                0xA3, 0x04, 0x41, 0xC2, 0xB1, 0x42
            };
            var expectedValue = "A\u00B1B";

            var reader = new TlvReader(encoding);

            var getValue = reader.ReadString(expectedTag: 0xA3, Encoding.UTF8);

            Assert.Equal(expectedValue, getValue);
        }

        [Fact]
        public void HasData_ReturnsCorrect()
        {
            byte[] encoding =
            {
                0x81, 0x02, 0x11, 0x22, 0x82, 0x03, 0x31, 0x32, 0x33
            };

            var reader = new TlvReader(encoding);
            Assert.True(reader.HasData);

            var value = reader.ReadValue(expectedTag: 0x81);
            Assert.NotEmpty(value.ToArray());
            Assert.True(reader.HasData);

            value = reader.ReadValue(expectedTag: 0x82);
            Assert.NotEmpty(value.ToArray());
            Assert.False(reader.HasData);
        }

        [Fact]
        public void ReadEncoded_Simple_ReturnsCorrect()
        {
            byte[] encoding =
            {
                0x01, 0x02, 0x11, 0x22
            };
            byte[] expected =
            {
                0x01, 0x02, 0x11, 0x22
            };

            var reader = new TlvReader(encoding);

            var encoded = reader.ReadEncoded(expectedTag: 0x01);

            var compareResult = encoded.Span.SequenceEqual(expected);

            Assert.True(compareResult);
        }

        [Fact]
        public void ReadEncoded_Multiple_ReturnsCorrect()
        {
            byte[] encoding =
            {
                0x01, 0x02, 0x11, 0x22, 0x02, 0x03, 0x31, 0x32, 0x33, 0x03, 0x00
            };
            byte[] expected = { 0x02, 0x03, 0x31, 0x32, 0x33 };

            var reader = new TlvReader(encoding);

            var encoded = reader.ReadValue(expectedTag: 0x01);
            Assert.NotEmpty(encoded.Span.ToArray());
            encoded = reader.ReadEncoded(expectedTag: 0x02);

            var compareResult = encoded.Span.SequenceEqual(expected);

            Assert.True(compareResult);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public void ReadByte_InvalidLength_ThrowsException(int length)
        {
            byte[] encoding = { 0x89, (byte)length, 0x11, 0x02, 0x01, 0x00 };

            var reader = new TlvReader(encoding);

#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
            Action actual = () => reader.ReadByte(expectedTag: 0x89);
            Assert.Throws<TlvException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void ReadByte_InvalidLength_RestoresOffset()
        {
            byte[] encoding = { 0x89, 0x02, 0x11, 0x02, 0x01, 0x00 };

            var reader = new TlvReader(encoding);

#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
            Action actual = () => reader.ReadByte(expectedTag: 0x89);
            Assert.Throws<TlvException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.

            var getValue = reader.ReadInt16(expectedTag: 0x89);

            Assert.Equal(expected: 0x1102, getValue);
        }

        [Fact]
        public void ReadInt16_InvalidLength_RestoresOffset()
        {
            byte[] encoding = { 0x89, 0x04, 0x11, 0x22, 0x33, 0x44, 0x01, 0x00 };

            var reader = new TlvReader(encoding);

#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
            Action actual = () => reader.ReadInt16(expectedTag: 0x89);
            Assert.Throws<TlvException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.

            var getValue = reader.ReadInt32(expectedTag: 0x89, bigEndian: false);

            Assert.Equal(expected: 0x44332211, getValue);
        }

        [Fact]
        public void ReadUInt16_InvalidLength_RestoresOffset()
        {
            byte[] encoding = { 0x89, 0x04, 0x11, 0x22, 0x33, 0x44, 0x01, 0x00 };

            var reader = new TlvReader(encoding);

            void actual()
            {
                reader.ReadUInt16(expectedTag: 0x89);
            }

            _ = Assert.Throws<TlvException>(actual);

            var getValue = reader.ReadInt32(expectedTag: 0x89, bigEndian: false);

            Assert.Equal(expected: 0x44332211, getValue);
        }

        [Fact]
        public void ReadInt32_InvalidLength_RestoresOffset()
        {
            byte[] encoding = { 0x89, 0x02, 0x11, 0x22, 0x01, 0x00 };

            var reader = new TlvReader(encoding);

#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
            Action actual = () => reader.ReadInt32(expectedTag: 0x89);
            Assert.Throws<TlvException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.

            var getValue = reader.ReadInt16(expectedTag: 0x89);

            Assert.Equal(expected: 0x1122, getValue);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        public void ReadInt16_InvalidLength_ThrowsException(int length)
        {
            byte[] encoding =
            {
                0x90, (byte)length, 0x04, 0x03, 0x02, 0x01, 0x00
            };

            var reader = new TlvReader(encoding);

#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
            Action actual = () => reader.ReadInt16(expectedTag: 0x90);
            Assert.Throws<TlvException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        public void ReadUInt16_InvalidLength_ThrowsException(int length)
        {
            byte[] encoding =
            {
                0x90, (byte)length, 0x04, 0x03, 0x02, 0x01, 0x00
            };

            var reader = new TlvReader(encoding);

            void actual()
            {
                reader.ReadUInt16(expectedTag: 0x90);
            }

            _ = Assert.Throws<TlvException>(actual);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        public void ReadInt32_InvalidLength_ThrowsException(int length)
        {
            byte[] encoding =
            {
                0x91, (byte)length, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01, 0x00
            };

            var reader = new TlvReader(encoding);

#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
            Action actual = () => reader.ReadInt32(expectedTag: 0x91);
            Assert.Throws<TlvException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void ReadValue_NotEnoughData_ThrowsException()
        {
            byte[] encoding =
            {
                0x71, 0x04, 0x01, 0x02, 0x03
            };

            var reader = new TlvReader(encoding);

#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
            Action actual = () => reader.ReadValue(expectedTag: 0x71);
            Assert.Throws<TlvException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void ReadEncoded_NotEnoughData_ThrowsException()
        {
            byte[] encoding =
            {
                0x71, 0x04, 0x01, 0x02, 0x03
            };

            var reader = new TlvReader(encoding);

#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
            Action actual = () => reader.ReadEncoded(expectedTag: 0x71);
            Assert.Throws<TlvException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void ReadEncoded_InvalidLength_ThrowsException()
        {
            byte[] encoding =
            {
                0x30, 0x0a, 0x71, 0x80, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };

            var reader = new TlvReader(encoding);
            var nestedReader = reader.ReadNestedTlv(expectedTag: 0x30);

#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
            Action actual = () => nestedReader.ReadEncoded(expectedTag: 0x71);
            Assert.Throws<TlvException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void ReadString_NullEncoding_ThrowsExcpetion()
        {
            byte[] encoding =
            {
                0x71, 0x04, 0x01, 0x02, 0x03
            };

            var reader = new TlvReader(encoding);

#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
            Action actual = () => reader.ReadString(expectedTag: 0x71, encoding: null);
            Assert.Throws<ArgumentNullException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(-1)]
        public void PeekTag_InvalidTagLength_ThrowsException(int tagLength)
        {
            byte[] encoding =
            {
                0x90, 0x05, 0x04, 0x03, 0x02, 0x01, 0x00
            };

            var reader = new TlvReader(encoding);

#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
            Action actual = () => reader.PeekTag(tagLength);
            Assert.Throws<TlvException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void PeekTag_NotEnoughData_ThrowsException(int tagLength)
        {
            byte[] encoding = { 0x03, 0x02, 0x00, 0x01 };

            var tag = 0x0302;
            if (tagLength == 1)
            {
                tag = 0x03;
            }

            var reader = new TlvReader(encoding);
            var value = reader.ReadValue(tag);

#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
            Action actual = () => reader.PeekTag(tagLength);
            Assert.Throws<TlvException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(-1)]
        public void PeekLength_InvalidTagLength_ThrowsException(int tagLength)
        {
            byte[] encoding =
            {
                0x90, 0x05, 0x04, 0x03, 0x02, 0x01, 0x00
            };

            var reader = new TlvReader(encoding);

#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
            Action actual = () => reader.PeekLength(tagLength);
            Assert.Throws<TlvException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
        }

        [Theory]
        [InlineData(0x84)]
        [InlineData(0x80)]
        [InlineData(0x90)]
        [InlineData(0x91)]
        [InlineData(0xA2)]
        [InlineData(0xB3)]
        [InlineData(0xD1)]
        [InlineData(0xE2)]
        [InlineData(0xF3)]
        public void PeekLength_InvalidFirstByte_ThrowsException(byte firstByte)
        {
            byte[] encoding =
            {
                0x91, firstByte, 0x04, 0x02, 0x02, 0x01
            };

            var reader = new TlvReader(encoding);

#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
            Action actual = () => reader.PeekLength();
            Assert.Throws<TlvException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void PeekLength_TagOnly_ThrowsException()
        {
            byte[] encoding = { 0x94 };

            var reader = new TlvReader(encoding);

#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
            Action actual = () => reader.PeekLength();
            Assert.Throws<TlvException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
        }

        [Theory]
        [InlineData(3)]
        [InlineData(2)]
        [InlineData(1)]
        public void PeekLength_NotEnoughData_ThrowsException(int count)
        {
            var firstByte = (byte)(0x80 + count);
            var encoding = new byte[count + 1];
            encoding[0] = 0x91;
            encoding[1] = firstByte;

            var reader = new TlvReader(encoding);

#pragma warning disable CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
            Action actual = () => reader.PeekLength();
            Assert.Throws<TlvException>(actual);
#pragma warning restore CS8625, CA1806, IDE0039, IDE0058 // Cannot convert null literal to non-nullable reference type.
        }
    }
}
