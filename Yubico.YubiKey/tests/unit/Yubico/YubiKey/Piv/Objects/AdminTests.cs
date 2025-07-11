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

namespace Yubico.YubiKey.Piv.Objects
{
    public class AdminTests
    {
        [Fact]
        public void Constructor_IsEmpty_Correct()
        {
            using var admin = new AdminData();

            Assert.True(admin.IsEmpty);
        }

        [Fact]
        public void Constructor_PukBlocked_Correct()
        {
            using var admin = new AdminData();

            Assert.False(admin.PukBlocked);
        }

        [Fact]
        public void Constructor_PinProtected_Correct()
        {
            using var admin = new AdminData();

            Assert.False(admin.PinProtected);
        }

        [Fact]
        public void Constructor_DataTag_Correct()
        {
            using var admin = new AdminData();

            Assert.Equal(0x005FFF00, admin.DataTag);
        }

        [Fact]
        public void Constructor_DefinedDataTag_Correct()
        {
            using var admin = new AdminData();

            int definedTag = admin.GetDefinedDataTag();
            Assert.Equal(0x005FFF00, definedTag);
        }

        [Fact]
        public void SetTag_DataTag_Correct()
        {
            using var admin = new AdminData();
            admin.DataTag = 0x005F0A01;

            Assert.Equal(0x005F0A01, admin.DataTag);
        }

        [Fact]
        public void SetTag_DefinedDataTag_Correct()
        {
            using var admin = new AdminData();
            admin.DataTag = 0x005F0A01;

            int definedTag = admin.GetDefinedDataTag();
            Assert.Equal(0x005FFF00, definedTag);
        }

        [Theory]
        [InlineData(0x015FFF10)]
        [InlineData(0x0000007E)]
        [InlineData(0x00007F61)]
        [InlineData(0x005FC101)]
        [InlineData(0x005FC104)]
        [InlineData(0x005FC105)]
        [InlineData(0x005FC10A)]
        [InlineData(0x005FC10B)]
        [InlineData(0x005FC10D)]
        [InlineData(0x005FC120)]
        [InlineData(0x005FFF01)]
        public void SetTag_InvalidTag_Throws(int newTag)
        {
            using var admin = new AdminData();

            _ = Assert.Throws<ArgumentException>(() => admin.DataTag = newTag);
        }

        [Fact]
        public void SetSalt_Correct()
        {
            byte[] fixedBytes = GetFixedBytes();
            Array.Resize<byte>(ref fixedBytes, 16);

            using var admin = new AdminData();
            admin.SetSalt(fixedBytes);

            _ = Assert.NotNull(admin.Salt);
            if (!(admin.Salt is null))
            {
                var salt = (ReadOnlyMemory<byte>)admin.Salt;
                bool isValid = MemoryExtensions.SequenceEqual<byte>(fixedBytes, salt.Span);
                Assert.True(isValid);
            }
        }

        [Fact]
        public void SetSalt_Null_NotEmpty()
        {
            using var admin = new AdminData();
            admin.SetSalt(null);

            Assert.False(admin.IsEmpty);
        }

        [Fact]
        public void SetSalt_LengthZero_NotEmpty()
        {
            using var admin = new AdminData();
            admin.SetSalt(ReadOnlyMemory<byte>.Empty);

            Assert.False(admin.IsEmpty);
        }

        [Fact]
        public void SetSalt_ThenNull_Correct()
        {
            byte[] fixedBytes = GetFixedBytes();
            Array.Resize<byte>(ref fixedBytes, 16);

            using var admin = new AdminData();
            admin.SetSalt(fixedBytes);
            _ = Assert.NotNull(admin.Salt);

            admin.SetSalt(ReadOnlyMemory<byte>.Empty);
            Assert.Null(admin.Salt);
        }

        [Fact]
        public void SetTime_Null_NotEmpty()
        {
            using var admin = new AdminData();
            admin.PinLastUpdated = null;

            Assert.False(admin.IsEmpty);
        }

        [Fact]
        public void SetTime_NotCurrent_Null()
        {
            using var admin = new AdminData();
            admin.PinLastUpdated = null;

            Assert.Null(admin.PinLastUpdated);
        }

        [Fact]
        public void SetTime_Current_NotEmpty()
        {
            using var admin = new AdminData();
            admin.PinLastUpdated = DateTime.UtcNow;

            Assert.False(admin.IsEmpty);
        }

        [Fact]
        public void SetTime_Current_NotNull()
        {
            using var admin = new AdminData();
            admin.PinLastUpdated = DateTime.UtcNow;

            _ = Assert.NotNull(admin.PinLastUpdated);
        }

        [Fact]
        public void Encode_Empty_Correct()
        {
            var expected = new Span<byte>(new byte[] { 0x53, 0x00 });
            using var adminData = new AdminData();

            byte[] encoding = adminData.Encode();
            bool isValid = MemoryExtensions.SequenceEqual(expected, encoding);
            Assert.True(isValid);
        }

        [Fact]
        public void BitFieldZero_Encode_Correct()
        {
            var expected = new Span<byte>(new byte[] {
                0x53, 0x05, 0x80, 0x03, 0x81, 0x01, 0x00
            });
            using var admin = new AdminData();
            admin.PukBlocked = false;

            byte[] encoded = admin.Encode();

            bool isValid = MemoryExtensions.SequenceEqual<byte>(expected, encoded);
            Assert.True(isValid);
        }

        [Fact]
        public void BitFieldPinProtected_Encode_Correct()
        {
            var expected = new Span<byte>(new byte[] {
                0x53, 0x05, 0x80, 0x03, 0x81, 0x01, 0x02
            });
            using var admin = new AdminData();
            admin.PinProtected = true;

            byte[] encoded = admin.Encode();

            bool isValid = MemoryExtensions.SequenceEqual<byte>(expected, encoded);
            Assert.True(isValid);
        }

        [Fact]
        public void Salt_Encode_Correct()
        {
            var expected = new Span<byte>(new byte[] {
                0x53, 0x17,
                0x80, 0x15, 0x81, 0x01, 0x00, 0x82, 0x10,
                0xA4, 0xC4, 0xD9, 0x23, 0x74, 0x59, 0x7F, 0x64,
                0xA6, 0xD3, 0xCB, 0x2C, 0x10, 0xF0, 0xCD, 0x2D
            });

            byte[] fixedBytes = GetFixedBytes();
            Array.Resize<byte>(ref fixedBytes, 16);

            using var admin = new AdminData();
            admin.SetSalt(fixedBytes);

            byte[] encoded = admin.Encode();

            bool isValid = MemoryExtensions.SequenceEqual<byte>(expected, encoded);
            Assert.True(isValid);
        }

        [Fact]
        public void FullObject_Encode_Correct()
        {
            var expected = new Span<byte>(new byte[] {
                0x53, 0x1D,
                0x80, 0x1B, 0x81, 0x01, 0x03, 0x82, 0x10,
                0xA4, 0xC4, 0xD9, 0x23, 0x74, 0x59, 0x7F, 0x64,
                0xA6, 0xD3, 0xCB, 0x2C, 0x10, 0xF0, 0xCD, 0x2D,
                0x83, 0x04, 0x00, 0x00, 0x00, 0x00
            });

            byte[] fixedBytes = GetFixedBytes();
            Array.Resize<byte>(ref fixedBytes, 16);

            using var admin = new AdminData();
            admin.PukBlocked = true;
            admin.PinProtected = true;
            admin.SetSalt(fixedBytes);
            admin.PinLastUpdated = DateTime.UtcNow;
            if (!(admin.PinLastUpdated is null))
            {
                long unixTimeSeconds = new DateTimeOffset((DateTime)admin.PinLastUpdated).ToUnixTimeSeconds();
                expected[30] = (byte)(unixTimeSeconds >> 24);
                expected[29] = (byte)(unixTimeSeconds >> 16);
                expected[28] = (byte)(unixTimeSeconds >> 8);
                expected[27] = (byte)unixTimeSeconds;
            }

            byte[] encoded = admin.Encode();

            bool isValid = MemoryExtensions.SequenceEqual<byte>(expected, encoded);
            Assert.True(isValid);
        }

        [Fact]
        public void FullDecode_NotEmpty()
        {
            var encoding = new Memory<byte>(new byte[] {
                0x53, 0x1D,
                0x80, 0x1B, 0x81, 0x01, 0x03, 0x82, 0x10,
                0xA4, 0xC4, 0xD9, 0x23, 0x74, 0x59, 0x7F, 0x64,
                0xA6, 0xD3, 0xCB, 0x2C, 0x10, 0xF0, 0xCD, 0x2D,
                0x83, 0x04, 0x71, 0xB8, 0xE1, 0x61
            });

            using var admin = new AdminData();
            bool isValid = admin.TryDecode(encoding);
            Assert.True(isValid);
        }

        [Fact]
        public void FullDecode_PukBlockedCorrect()
        {
            var encoding = new Memory<byte>(new byte[] {
                0x53, 0x1D,
                0x80, 0x1B, 0x81, 0x01, 0x03, 0x82, 0x10,
                0xA4, 0xC4, 0xD9, 0x23, 0x74, 0x59, 0x7F, 0x64,
                0xA6, 0xD3, 0xCB, 0x2C, 0x10, 0xF0, 0xCD, 0x2D,
                0x83, 0x04, 0x71, 0xB8, 0xE1, 0x61
            });

            using var admin = new AdminData();
            bool isValid = admin.TryDecode(encoding);
            Assert.True(admin.PukBlocked);
        }

        [Fact]
        public void FullDecode_PinProtectedCorrect()
        {
            var encoding = new Memory<byte>(new byte[] {
                0x53, 0x1D,
                0x80, 0x1B, 0x81, 0x01, 0x03, 0x82, 0x10,
                0xA4, 0xC4, 0xD9, 0x23, 0x74, 0x59, 0x7F, 0x64,
                0xA6, 0xD3, 0xCB, 0x2C, 0x10, 0xF0, 0xCD, 0x2D,
                0x83, 0x04, 0x71, 0xB8, 0xE1, 0x61
            });

            using var admin = new AdminData();
            bool isValid = admin.TryDecode(encoding);
            Assert.True(admin.PinProtected);
        }

        [Fact]
        public void FullDecode_SaltCorrect()
        {
            var encoding = new Memory<byte>(new byte[] {
                0x53, 0x1D,
                0x80, 0x1B, 0x81, 0x01, 0x03, 0x82, 0x10,
                0xA4, 0xC4, 0xD9, 0x23, 0x74, 0x59, 0x7F, 0x64,
                0xA6, 0xD3, 0xCB, 0x2C, 0x10, 0xF0, 0xCD, 0x2D,
                0x83, 0x04, 0x71, 0xB8, 0xE1, 0x61
            });
            Memory<byte> expected = encoding.Slice(9, 16);

            using var admin = new AdminData();
            bool isValid = admin.TryDecode(encoding);
            if (!(admin.Salt is null))
            {
                var salt = (ReadOnlyMemory<byte>)admin.Salt;
                isValid = MemoryExtensions.SequenceEqual<byte>(expected.Span, salt.Span);
                Assert.True(isValid);
            }
        }

        [Fact]
        public void FullDecode_DateCorrect()
        {
            var encoding = new Memory<byte>(new byte[] {
                0x53, 0x1D,
                0x80, 0x1B, 0x81, 0x01, 0x03, 0x82, 0x10,
                0xA4, 0xC4, 0xD9, 0x23, 0x74, 0x59, 0x7F, 0x64,
                0xA6, 0xD3, 0xCB, 0x2C, 0x10, 0xF0, 0xCD, 0x2D,
                0x83, 0x04, 0x71, 0xB8, 0xE1, 0x61
            });
            long unixTimeSeconds = ((long)encoding.Span[30] & 255) << 24;
            unixTimeSeconds += ((long)encoding.Span[29] & 255) << 16;
            unixTimeSeconds += ((long)encoding.Span[28] & 255) << 8;
            unixTimeSeconds += (long)encoding.Span[27] & 255;
            var expectedOffset = DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds);
            DateTime expected = expectedOffset.UtcDateTime;

            using var admin = new AdminData();
            bool isValid = admin.TryDecode(encoding);
            Assert.True(isValid);
            if (!(admin.PinLastUpdated is null))
            {
                isValid = admin.PinLastUpdated == expected;
                Assert.True(isValid);
            }
        }

        [Fact]
        public void Decode_TwoBitFields_ReturnsFalse()
        {
            var encoding = new Memory<byte>(new byte[] {
                0x53, 0x20,
                0x80, 0x1E,
                0x81, 0x01, 0x03,
                0x82, 0x10,
                    0xA4, 0xC4, 0xD9, 0x23, 0x74, 0x59, 0x7F, 0x64,
                    0xA6, 0xD3, 0xCB, 0x2C, 0x10, 0xF0, 0xCD, 0x2D,
                0x83, 0x04, 0x71, 0xB8, 0xE1, 0x61,
                0x81, 0x01, 0x03
            });

            using var admin = new AdminData();
            bool isValid = admin.TryDecode(encoding);
            Assert.False(isValid);
        }

        [Fact]
        public void Decode_TwoSalts_ReturnsFalse()
        {
            var encoding = new Memory<byte>(new byte[] {
                0x53, 0x2F,
                0x80, 0x2D,
                    0x81, 0x01, 0x03,
                    0x82, 0x10,
                    0xA4, 0xC4, 0xD9, 0x23, 0x74, 0x59, 0x7F, 0x64,
                    0xA6, 0xD3, 0xCB, 0x2C, 0x10, 0xF0, 0xCD, 0x2D,
                    0x83, 0x04, 0x71, 0xB8, 0xE1, 0x61,
                    0x82, 0x10,
                    0xA6, 0xD3, 0xCB, 0x2C, 0x10, 0xF0, 0xCD, 0x2D,
                    0xA4, 0xC4, 0xD9, 0x23, 0x74, 0x59, 0x7F, 0x64,
            });

            using var admin = new AdminData();
            bool isValid = admin.TryDecode(encoding);
            Assert.False(isValid);
        }

        [Fact]
        public void Decode_TwoDates_ReturnsFalse()
        {
            var encoding = new Memory<byte>(new byte[] {
                0x53, 0x23,
                    0x80, 0x21,
                    0x81, 0x01, 0x03,
                    0x83, 0x04, 0x81, 0xB8, 0xE1, 0x61,
                    0x82, 0x10,
                    0xA4, 0xC4, 0xD9, 0x23, 0x74, 0x59, 0x7F, 0x64,
                    0xA6, 0xD3, 0xCB, 0x2C, 0x10, 0xF0, 0xCD, 0x2D,
                    0x83, 0x04, 0x71, 0xB8, 0xE1, 0x61
            });

            using var admin = new AdminData();
            bool isValid = admin.TryDecode(encoding);
            Assert.False(isValid);
        }

        private static byte[] GetFixedBytes()
        {
            // This is 256 bytes so that other tests pass.
            // Because of threading and race conditions, it is possible other
            // tests will use a random object built with these bytes.
            // Currently, setting to 256 seems to prevent problems when the
            // threading race goes bad.
            return new byte[256] {
                0xA4, 0xC4, 0xD9, 0x23, 0x74, 0x59, 0x7F, 0x64,
                0xA6, 0xD3, 0xCB, 0x2C, 0x10, 0xF0, 0xCD, 0x2D,
                0x57, 0xE9, 0x9F, 0x58, 0xC8, 0x57, 0x10, 0x6E,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0xA4, 0xC4, 0xD9, 0x23, 0x74, 0x59, 0x7F, 0x64,
                0xA6, 0xD3, 0xCB, 0x2C, 0x10, 0xF0, 0xCD, 0x2D,
                0x57, 0xE9, 0x9F, 0x58, 0xC8, 0x57, 0x10, 0x6E,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0xA4, 0xC4, 0xD9, 0x23, 0x74, 0x59, 0x7F, 0x64,
                0xA6, 0xD3, 0xCB, 0x2C, 0x10, 0xF0, 0xCD, 0x2D,
                0x57, 0xE9, 0x9F, 0x58, 0xC8, 0x57, 0x10, 0x6E,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0xA4, 0xC4, 0xD9, 0x23, 0x74, 0x59, 0x7F, 0x64,
                0xA6, 0xD3, 0xCB, 0x2C, 0x10, 0xF0, 0xCD, 0x2D,
                0x57, 0xE9, 0x9F, 0x58, 0xC8, 0x57, 0x10, 0x6E,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88
            };
        }
    }
}
