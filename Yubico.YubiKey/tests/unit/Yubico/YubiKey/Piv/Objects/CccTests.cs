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
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv.Objects
{
    public class CccTests
    {
        [Fact]
        public void Constructor_IsEmpty_Correct()
        {
            using var ccc = new CardCapabilityContainer();

            Assert.True(ccc.IsEmpty);
        }

        [Fact]
        public void Constructor_DataTag_Correct()
        {
            using var ccc = new CardCapabilityContainer();

            Assert.Equal(0x005FC107, ccc.DataTag);
        }

        [Fact]
        public void Constructor_DefinedDataTag_Correct()
        {
            using var ccc = new CardCapabilityContainer();

            int definedTag = ccc.GetDefinedDataTag();
            Assert.Equal(0x005FC107, definedTag);
        }

        [Fact]
        public void SetTag_DataTag_Correct()
        {
            using var ccc = new CardCapabilityContainer();
            ccc.DataTag = 0x005F0010;

            Assert.Equal(0x005F0010, ccc.DataTag);
        }

        [Fact]
        public void SetTag_DefinedDataTag_Correct()
        {
            using var ccc = new CardCapabilityContainer();
            ccc.DataTag = 0x005F0010;

            int definedTag = ccc.GetDefinedDataTag();
            Assert.Equal(0x005FC107, definedTag);
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
            using var ccc = new CardCapabilityContainer();

            _ = Assert.Throws<ArgumentException>(() => ccc.DataTag = newTag);
        }

        [Fact]
        public void Constructor_ManId_Correct()
        {
            using var ccc = new CardCapabilityContainer();

            Assert.Equal(0xFF, ccc.ManufacturerId);
        }

        [Fact]
        public void Constructor_CardType_Correct()
        {
            using var ccc = new CardCapabilityContainer();

            Assert.Equal(0x02, ccc.CardType);
        }

        [Fact]
        public void Constructor_ContainerVersionNumber_Correct()
        {
            using var ccc = new CardCapabilityContainer();

            Assert.Equal(0x21, ccc.ContainerVersionNumber);
        }

        [Fact]
        public void Constructor_GrammarVersionNumber_Correct()
        {
            using var ccc = new CardCapabilityContainer();

            Assert.Equal(0x21, ccc.GrammarVersionNumber);
        }

        [Fact]
        public void Constructor_P15_Correct()
        {
            using var ccc = new CardCapabilityContainer();

            Assert.Equal(0, ccc.Pkcs15Version);
        }

        [Fact]
        public void Constructor_DataModel_Correct()
        {
            using var ccc = new CardCapabilityContainer();

            Assert.Equal(0x10, ccc.DataModelNumber);
        }

        [Fact]
        public void SetCardId_Valid_NotEmpty()
        {
            RandomNumberGenerator random = RandomObjectUtility.GetRandomObject(null);
            byte[] newCardId = new byte[14];
            random.GetBytes(newCardId);

            using var ccc = new CardCapabilityContainer();
            ccc.SetCardId(newCardId);

            Assert.False(ccc.IsEmpty);
        }

        [Fact]
        public void SetCardId_CompareProperty_Correct()
        {
            RandomNumberGenerator random = RandomObjectUtility.GetRandomObject(null);
            byte[] newCardId = new byte[14];
            random.GetBytes(newCardId);

            using var ccc = new CardCapabilityContainer();
            ccc.SetCardId(newCardId);

            bool isValid = MemoryExtensions.SequenceEqual<byte>(newCardId, ccc.CardIdentifier.Span);
            Assert.True(isValid);
        }

        [Fact]
        public void SetRandomCardId_Valid_NotEmpty()
        {
            using var ccc = new CardCapabilityContainer();
            ccc.SetRandomCardId();

            Assert.False(ccc.IsEmpty);
        }

        [Fact]
        public void Encode_Empty_Correct()
        {
            var expected = new Span<byte>(new byte[] { 0x53, 0x00 });
            using var ccc = new CardCapabilityContainer();

            byte[] encoding = ccc.Encode();
            bool isValid = MemoryExtensions.SequenceEqual(expected, encoding);
            Assert.True(isValid);
        }

        [Fact]
        public void Encode_Valid_CorrectData()
        {
            var expectedValue = new Span<byte>(new byte[] {
                0x53, 0x33, 0xF0, 0x15, 0xA0, 0x00, 0x00, 0x01, 0x16, 0xFF, 0x02, 0x01, 0x02, 0x03, 0x04, 0x05,
                0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0xF1, 0x01, 0x21, 0xF2, 0x01, 0x21, 0xF3,
                0x00, 0xF4, 0x01, 0x00, 0xF5, 0x01, 0x10, 0xF6, 0x00, 0xF7, 0x00, 0xFA, 0x00, 0xFB, 0x00, 0xFC,
                0x00, 0xFD, 0x00, 0xFE, 0x00,
            });
            byte[] newCardId = GetFixedCardIdBytes();

            using var ccc = new CardCapabilityContainer();
            ccc.SetCardId(newCardId);

            byte[] encodedCcc = ccc.Encode();

            bool isValid = MemoryExtensions.SequenceEqual<byte>(expectedValue, encodedCcc);
            Assert.True(isValid);
        }

        [Fact]
        public void Decode_NoData_Empty()
        {
            var encoding = new Memory<byte>();

            using var ccc = new CardCapabilityContainer();

            ccc.Decode(encoding);
            Assert.True(ccc.IsEmpty);
        }

        [Fact]
        public void Decode_Valid_CorrectCardId()
        {
            var encodedValue = new Memory<byte>(new byte[] {
                0x53, 0x33, 0xF0, 0x15, 0xA0, 0x00, 0x00, 0x01, 0x16, 0xFF, 0x02, 0x01, 0x02, 0x03, 0x04, 0x05,
                0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0xF1, 0x01, 0x21, 0xF2, 0x01, 0x21, 0xF3,
                0x00, 0xF4, 0x01, 0x00, 0xF5, 0x01, 0x10, 0xF6, 0x00, 0xF7, 0x00, 0xFA, 0x00, 0xFB, 0x00, 0xFC,
                0x00, 0xFD, 0x00, 0xFE, 0x00,
            });
            byte[] expectedCardId = GetFixedCardIdBytes();

            using var ccc = new CardCapabilityContainer();
            ccc.Decode(encodedValue);

            bool isValid = MemoryExtensions.SequenceEqual<byte>(expectedCardId, ccc.CardIdentifier.Span);
            Assert.True(isValid);
        }

        [Fact]
        public void Decode_InvalidUniqueId_Throws()
        {
            var encodedValue = new Memory<byte>(new byte[] {
                0x53, 0x33, 0xF0, 0x15, 0xA0, 0x01, 0x00, 0x01, 0x16, 0xFF, 0x02, 0x01, 0x02, 0x03, 0x04, 0x05,
                0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0xF1, 0x01, 0x21, 0xF2, 0x01, 0x21, 0xF3,
                0x00, 0xF4, 0x01, 0x00, 0xF5, 0x01, 0x10, 0xF6, 0x00, 0xF7, 0x00, 0xFA, 0x00, 0xFB, 0x00, 0xFC,
                0x00, 0xFD, 0x00, 0xFE, 0x00,
            });

            using var ccc = new CardCapabilityContainer();
            _ = Assert.Throws<ArgumentException>(() => ccc.Decode(encodedValue));
        }

        [Fact]
        public void Decode_InvalidUniqueIdLength_Throws()
        {
            var encodedValue = new Memory<byte>(new byte[] {
                0x53, 0x32, 0xF0, 0x14, 0xA0, 0x00, 0x00, 0x01, 0x16, 0xFF, 0x02, 0x01, 0x02, 0x03, 0x04, 0x05,
                0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0xF1, 0x01, 0x21, 0xF2, 0x01, 0x21, 0xF3,
                0x00, 0xF4, 0x01, 0x00, 0xF5, 0x01, 0x10, 0xF6, 0x00, 0xF7, 0x00, 0xFA, 0x00, 0xFB, 0x00, 0xFC,
                0x00, 0xFD, 0x00, 0xFE, 0x00,
            });

            using var ccc = new CardCapabilityContainer();
            _ = Assert.Throws<ArgumentException>(() => ccc.Decode(encodedValue));
        }

        [Theory]
        [InlineData(27, 0x31)]
        [InlineData(30, 0x31)]
        [InlineData(35, 0x21)]
        [InlineData(38, 0x02)]
        public void Decode_InvalidFixedValue_Throws(int offset, byte alternateValue)
        {
            var encodedValue = new Memory<byte>(new byte[] {
                0x53, 0x33, 0xF0, 0x15, 0xA0, 0x00, 0x00, 0x01, 0x16, 0xFF, 0x02, 0x01, 0x02, 0x03, 0x04, 0x05,
                0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0xF1, 0x01, 0x21, 0xF2, 0x01, 0x21, 0xF3,
                0x00, 0xF4, 0x01, 0x00, 0xF5, 0x01, 0x10, 0xF6, 0x00, 0xF7, 0x00, 0xFA, 0x00, 0xFB, 0x00, 0xFC,
                0x00, 0xFD, 0x00, 0xFE, 0x00,
            });

            encodedValue.Span[offset] = alternateValue;

            using var ccc = new CardCapabilityContainer();
            _ = Assert.Throws<ArgumentException>(() => ccc.Decode(encodedValue));
        }

        [Theory]
        [InlineData(32)]
        [InlineData(40)]
        [InlineData(42)]
        [InlineData(44)]
        [InlineData(46)]
        [InlineData(48)]
        [InlineData(50)]
        [InlineData(52)]
        public void Decode_InvalidUnused_Throws(int offset)
        {
            var encodedValue = new Memory<byte>(new byte[] {
                0x53, 0x33, 0xF0, 0x15, 0xA0, 0x00, 0x00, 0x01, 0x16, 0xFF, 0x02, 0x01, 0x02, 0x03, 0x04, 0x05,
                0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0xF1, 0x01, 0x21, 0xF2, 0x01, 0x21, 0xF3,
                0x00, 0xF4, 0x01, 0x00, 0xF5, 0x01, 0x10, 0xF6, 0x00, 0xF7, 0x00, 0xFA, 0x00, 0xFB, 0x00, 0xFC,
                0x00, 0xFD, 0x00, 0xFE, 0x00,
            });

            var newEncoded = new Memory<byte>(new byte[encodedValue.Length + 1]);
            encodedValue.Slice(0, offset).CopyTo(newEncoded);
            newEncoded.Span[1] = 0x34;
            newEncoded.Span[offset] = 0x01;
            newEncoded.Span[offset + 1] = 0x01;
            encodedValue[(offset + 1)..].CopyTo(newEncoded[(offset + 2)..]);

            using var ccc = new CardCapabilityContainer();
            _ = Assert.Throws<ArgumentException>(() => ccc.Decode(newEncoded));
        }

        private byte[] GetFixedCardIdBytes() =>
            new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E
        };
    }
}
