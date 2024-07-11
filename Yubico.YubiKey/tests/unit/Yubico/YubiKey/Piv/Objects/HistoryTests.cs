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

namespace Yubico.YubiKey.Piv.Objects
{
    public class HistoryTests
    {
        [Fact]
        public void Constructor_IsEmpty_Correct()
        {
            using var history = new KeyHistory();

            Assert.True(history.IsEmpty);
        }

        [Fact]
        public void Constructor_DataTag_Correct()
        {
            using var history = new KeyHistory();

            Assert.Equal(0x005FC10C, history.DataTag);
        }

        [Fact]
        public void Constructor_DefinedDataTag_Correct()
        {
            using var history = new KeyHistory();

            int definedTag = history.GetDefinedDataTag();
            Assert.Equal(0x005FC10C, definedTag);
        }

        [Fact]
        public void SetTag_DataTag_Correct()
        {
            using var history = new KeyHistory();
            history.DataTag = 0x005F2210;

            Assert.Equal(0x005F2210, history.DataTag);
        }

        [Fact]
        public void SetTag_DefinedDataTag_Correct()
        {
            using var history = new KeyHistory();
            history.DataTag = 0x005F2210;

            int definedTag = history.GetDefinedDataTag();
            Assert.Equal(0x005FC10C, definedTag);
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
            using var history = new KeyHistory();

            _ = Assert.Throws<ArgumentException>(() => history.DataTag = newTag);
        }

        [Fact]
        public void Constructor_OnCardCerts_Correct()
        {
            using var history = new KeyHistory();

            Assert.Equal(0, history.OnCardCertificates);
        }

        [Fact]
        public void Constructor_OffCardCerts_Correct()
        {
            using var history = new KeyHistory();

            Assert.Equal(0, history.OffCardCertificates);
        }

        [Fact]
        public void Constructor_Url_Correct()
        {
            using var history = new KeyHistory();

            Assert.Null(history.OffCardCertificateUrl);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(255)]
        [InlineData(0)]
        public void SetOnCardCerts_Valid_NotEmpty(byte certCount)
        {
            using var history = new KeyHistory();
            history.OnCardCertificates = certCount;

            Assert.False(history.IsEmpty);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(33)]
        [InlineData(0)]
        public void SetOffCardCerts_Valid_NotEmpty(byte certCount)
        {
            using var history = new KeyHistory();
            history.OffCardCertificates = certCount;

            Assert.False(history.IsEmpty);
        }

        [Fact]
        public void SetOffCardAndUrl_Valid_NotEmpty()
        {
            using var history = new KeyHistory();
            history.OffCardCertificates = 2;
            history.OffCardCertificateUrl = new Uri("file://user/certs");

            Assert.False(history.IsEmpty);
        }

        [Fact]
        public void SetOnCardAndUrl_Valid_NotEmpty()
        {
            using var history = new KeyHistory();
            history.OnCardCertificates = 4;
            history.OffCardCertificateUrl = new Uri("file://user/certs");

            Assert.False(history.IsEmpty);
        }

        [Fact]
        public void SetOffCardAndUrl_SetZero_UrlNull()
        {
            using var history = new KeyHistory();
            history.OffCardCertificates = 4;
            history.OffCardCertificateUrl = new Uri("file://user/certs");
            history.OffCardCertificates = 0;

            Assert.Null(history.OffCardCertificateUrl);
        }

        [Fact]
        public void ZeroCerts_SetUrl_Encode_Throws()
        {
            using var history = new KeyHistory();
            history.OffCardCertificateUrl = new Uri("file://user/certs");
            _ = Assert.Throws<InvalidOperationException>(() => history.Encode());
        }

        [Fact]
        public void LongUrl_Throws()
        {
            string someName = "file://users/certs/somelongname/evenlonger/reallylongname/needevenmore";
            someName += "/stillmoreneeded/118charactersisactuallyquitelong";
            using var history = new KeyHistory();
            history.OffCardCertificates = 4;
            _ = Assert.Throws<InvalidOperationException>(() => history.OffCardCertificateUrl = new Uri(someName));
        }

        [Fact]
        public void Encode_Empty_Correct()
        {
            var expected = new Span<byte>(new byte[] { 0x53, 0x00 });
            using var history = new KeyHistory();

            byte[] encoding = history.Encode();
            bool isValid = MemoryExtensions.SequenceEqual(expected, encoding);
            Assert.True(isValid);
        }

        [Fact]
        public void Encode_ZeroOffCard_Correct()
        {
            var expectedValue = new Span<byte>(new byte[]
            {
                0x53, 0x0A, 0xC1, 0x01, 0x00, 0xC2, 0x01, 0x00, 0xF3, 0x00, 0xFE, 0x00
            });

            using var history = new KeyHistory();
            history.OffCardCertificates = 0;

            byte[] encodedHistory = history.Encode();

            bool isValid = MemoryExtensions.SequenceEqual<byte>(expectedValue, encodedHistory);
            Assert.True(isValid);
        }

        [Fact]
        public void Encode_ZeroOnCard_Correct()
        {
            var expectedValue = new Span<byte>(new byte[]
            {
                0x53, 0x0A, 0xC1, 0x01, 0x00, 0xC2, 0x01, 0x00, 0xF3, 0x00, 0xFE, 0x00
            });

            using var history = new KeyHistory();
            history.OnCardCertificates = 0;

            byte[] encodedHistory = history.Encode();

            bool isValid = MemoryExtensions.SequenceEqual<byte>(expectedValue, encodedHistory);
            Assert.True(isValid);
        }

        [Fact]
        public void Encode_SetUrl_Correct()
        {
            var expectedValue = new Span<byte>(new byte[]
            {
                0x53, 0x1B,
                0xC1, 0x01, 0x01, 0xC2, 0x01, 0x01,
                0xF3, 0x11,
                0x66, 0x69, 0x6c, 0x65, 0x3a, 0x2f, 0x2f, 0x75, 0x73, 0x65, 0x72, 0x2f, 0x63, 0x65, 0x72, 0x74, 0x73,
                0xFE, 0x00
            });

            using var history = new KeyHistory();
            history.OnCardCertificates = 1;
            history.OffCardCertificates = 1;
            history.OffCardCertificateUrl = new Uri("file://user/certs");

            byte[] encodedHistory = history.Encode();

            bool isValid = MemoryExtensions.SequenceEqual<byte>(expectedValue, encodedHistory);
            Assert.True(isValid);
        }

        [Fact]
        public void Decode_OnCard_Correct()
        {
            var dataToDecode = new ReadOnlyMemory<byte>(new byte[]
            {
                0x53, 0x1B,
                0xC1, 0x01, 0x01, 0xC2, 0x01, 0x02,
                0xF3, 0x11,
                0x66, 0x69, 0x6c, 0x65, 0x3a, 0x2f, 0x2f, 0x75, 0x73, 0x65, 0x72, 0x2f, 0x63, 0x65, 0x72, 0x74, 0x73,
                0xFE, 0x00
            });

            using var history = new KeyHistory();
            bool isValid = history.TryDecode(dataToDecode);
            Assert.True(isValid);
            Assert.Equal(1, history.OnCardCertificates);
        }

        [Fact]
        public void Decode_OffCard_Correct()
        {
            var dataToDecode = new ReadOnlyMemory<byte>(new byte[]
            {
                0x53, 0x1B,
                0xC1, 0x01, 0x01, 0xC2, 0x01, 0x02,
                0xF3, 0x11,
                0x66, 0x69, 0x6c, 0x65, 0x3a, 0x2f, 0x2f, 0x75, 0x73, 0x65, 0x72, 0x2f, 0x63, 0x65, 0x72, 0x74, 0x73,
                0xFE, 0x00
            });

            using var history = new KeyHistory();
            bool isValid = history.TryDecode(dataToDecode);
            Assert.True(isValid);
            Assert.Equal(2, history.OffCardCertificates);
        }

        [Fact]
        public void Decode_WithUrl_CorrectUrl()
        {
            var dataToDecode = new ReadOnlyMemory<byte>(new byte[]
            {
                0x53, 0x1B,
                0xC1, 0x01, 0x01, 0xC2, 0x01, 0x01,
                0xF3, 0x11,
                0x66, 0x69, 0x6c, 0x65, 0x3a, 0x2f, 0x2f, 0x75, 0x73, 0x65, 0x72, 0x2f, 0x63, 0x65, 0x72, 0x74, 0x73,
                0xFE, 0x00
            });

            ReadOnlySpan<byte> expectedValue = dataToDecode.Span.Slice(10, 17);

            using var history = new KeyHistory();
            bool isValid = history.TryDecode(dataToDecode);
            Assert.True(isValid);
            Assert.NotNull(history.OffCardCertificateUrl);

            if (!(history.OffCardCertificateUrl is null))
            {
                byte[] getUrl = Encoding.UTF8.GetBytes(history.OffCardCertificateUrl.AbsoluteUri);

                isValid = MemoryExtensions.SequenceEqual<byte>(expectedValue, getUrl);
                Assert.True(isValid);
            }
        }

        [Fact]
        public void Decode_ZeroAndNoUrl_CorrectOnCard()
        {
            var dataToDecode = new ReadOnlyMemory<byte>(new byte[]
            {
                0x53, 0x0A, 0xC1, 0x01, 0x00, 0xC2, 0x01, 0x00, 0xF3, 0x00, 0xFE, 0x00
            });

            using var history = new KeyHistory();
            bool isValid = history.TryDecode(dataToDecode);
            Assert.True(isValid);
            Assert.Equal(0, history.OnCardCertificates);
        }

        [Fact]
        public void Decode_ZeroAndNoUrl_CorrectOffCard()
        {
            var dataToDecode = new ReadOnlyMemory<byte>(new byte[]
            {
                0x53, 0x0A, 0xC1, 0x01, 0x00, 0xC2, 0x01, 0x00, 0xF3, 0x00, 0xFE, 0x00
            });

            using var history = new KeyHistory();
            bool isValid = history.TryDecode(dataToDecode);
            Assert.True(isValid);
            Assert.Equal(0, history.OffCardCertificates);
        }

        [Fact]
        public void Decode_ZeroAndNoUrl_NullUrl()
        {
            var dataToDecode = new ReadOnlyMemory<byte>(new byte[]
            {
                0x53, 0x0A, 0xC1, 0x01, 0x00, 0xC2, 0x01, 0x00, 0xF3, 0x00, 0xFE, 0x00
            });

            using var history = new KeyHistory();
            bool isValid = history.TryDecode(dataToDecode);
            Assert.True(isValid);
            Assert.Null(history.OffCardCertificateUrl);
        }
    }
}
