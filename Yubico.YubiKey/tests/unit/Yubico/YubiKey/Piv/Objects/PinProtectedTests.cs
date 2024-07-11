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
using Xunit;

namespace Yubico.YubiKey.Piv.Objects
{
    public class PinProtectedTests
    {
        [Fact]
        public void Constructor_IsEmpty_Correct()
        {
            using var pinProtect = new PinProtectedData();

            Assert.True(pinProtect.IsEmpty);
        }

        [Fact]
        public void Constructor_DataTag_Correct()
        {
            using var pinProtect = new PinProtectedData();

            Assert.Equal(expected: 0x005FC109, pinProtect.DataTag);
        }

        [Fact]
        public void Constructor_DefinedDataTag_Correct()
        {
            using var pinProtect = new PinProtectedData();

            var definedTag = pinProtect.GetDefinedDataTag();
            Assert.Equal(expected: 0x005FC109, definedTag);
        }

        [Fact]
        public void SetDataTag_Throws()
        {
            using var pinProtect = new PinProtectedData();

            _ = Assert.Throws<ArgumentException>(() => pinProtect.DataTag = 0x005F0B01);
        }

        [Fact]
        public void SetTag_ToDefinedDataTag_Correct()
        {
            using var pinProtect = new PinProtectedData();
            pinProtect.DataTag = 0x005FC109;

            var definedTag = pinProtect.GetDefinedDataTag();
            Assert.Equal(expected: 0x005FC109, definedTag);
        }

        [Fact]
        public void SetNullMgmtKey_NotEmpty()
        {
            using var pinProtect = new PinProtectedData();
            pinProtect.SetManagementKey(ReadOnlyMemory<byte>.Empty);

            Assert.False(pinProtect.IsEmpty);
        }

        [Fact]
        public void SetMgmtKey_NotEmpty()
        {
            var mgmtKey = GetArbitraryMgmtKey();

            using var pinProtect = new PinProtectedData();
            pinProtect.SetManagementKey(mgmtKey);

            Assert.False(pinProtect.IsEmpty);
        }

        [Fact]
        public void SetMgmtKey_DataSame()
        {
            var mgmtKey = GetArbitraryMgmtKey();

            using var pinProtect = new PinProtectedData();
            pinProtect.SetManagementKey(mgmtKey);

            _ = Assert.NotNull(pinProtect.ManagementKey);
            if (!(pinProtect.ManagementKey is null))
            {
                var getData = (ReadOnlyMemory<byte>)pinProtect.ManagementKey;
                var isValid = mgmtKey.Span.SequenceEqual(getData.Span);
                Assert.True(isValid);
            }
        }

        [Fact]
        public void SetMgmtKey_Invalid_Throws()
        {
            var mgmtKey = GetArbitraryMgmtKey();
            mgmtKey = mgmtKey.Slice(start: 0, length: 23);

            using var pinProtect = new PinProtectedData();

            _ = Assert.Throws<ArgumentException>(() => pinProtect.SetManagementKey(mgmtKey));
        }

        [Fact]
        public void Encode_Empty_Correct()
        {
            var expected = new Span<byte>(new byte[] { 0x53, 0x00 });
            using var pinProtect = new PinProtectedData();

            var encoding = pinProtect.Encode();
            var isValid = expected.SequenceEqual(encoding);
            Assert.True(isValid);
        }

        [Fact]
        public void Encode_NoKey_Correct()
        {
            var expected = new Span<byte>(new byte[]
            {
                0x53, 0x02, 0x88, 0x00
            });

            using var pinProtect = new PinProtectedData();
            pinProtect.SetManagementKey(ReadOnlyMemory<byte>.Empty);

            var encoded = pinProtect.Encode();

            var isValid = expected.SequenceEqual(encoded);
            Assert.True(isValid);
        }

        [Fact]
        public void Encode_WithKey_Correct()
        {
            var expected = new Span<byte>(new byte[]
            {
                0x53, 0x1C, 0x88, 0x1A, 0x89, 0x18,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68
            });

            using var pinProtect = new PinProtectedData();
            pinProtect.SetManagementKey(GetArbitraryMgmtKey());

            var encoded = pinProtect.Encode();

            var isValid = expected.SequenceEqual(encoded);
            Assert.True(isValid);
        }

        [Fact]
        public void TryDecode_NoKey_ReturnsTrue()
        {
            var encodedData = new Memory<byte>(new byte[]
            {
                0x53, 0x02, 0x88, 0x00
            });

            using var pinProtect = new PinProtectedData();
            var isValid = pinProtect.TryDecode(encodedData);

            Assert.True(isValid);
        }

        [Fact]
        public void Decode_NoKey_IsEmptyFalse()
        {
            var encodedData = new Memory<byte>(new byte[]
            {
                0x53, 0x02, 0x88, 0x00
            });

            using var pinProtect = new PinProtectedData();
            pinProtect.Decode(encodedData);

            Assert.False(pinProtect.IsEmpty);
        }

        [Fact]
        public void Decode_NoKey_CorrectMgmtKey()
        {
            var encodedData = new Memory<byte>(new byte[]
            {
                0x53, 0x02, 0x88, 0x00
            });

            using var pinProtect = new PinProtectedData();
            pinProtect.Decode(encodedData);

            Assert.Null(pinProtect.ManagementKey);
        }

        [Fact]
        public void TryDecode_Full_ReturnsTrue()
        {
            var encodedData = new Memory<byte>(new byte[]
            {
                0x53, 0x1C, 0x88, 0x1A, 0x89, 0x18,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68
            });

            using var pinProtect = new PinProtectedData();
            var isValid = pinProtect.TryDecode(encodedData);

            Assert.True(isValid);
        }

        [Fact]
        public void Decode_Full_IsEmptyFalse()
        {
            var encodedData = new Memory<byte>(new byte[]
            {
                0x53, 0x1C, 0x88, 0x1A, 0x89, 0x18,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68
            });

            using var pinProtect = new PinProtectedData();
            pinProtect.Decode(encodedData);

            Assert.False(pinProtect.IsEmpty);
        }

        [Fact]
        public void Decode_Full_MgmtKeyCorrect()
        {
            var encodedData = new Memory<byte>(new byte[]
            {
                0x53, 0x1C, 0x88, 0x1A, 0x89, 0x18,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68
            });

            using var pinProtect = new PinProtectedData();
            pinProtect.Decode(encodedData);

            _ = Assert.NotNull(pinProtect.ManagementKey);
            if (!(pinProtect.ManagementKey is null))
            {
                var getData = (ReadOnlyMemory<byte>)pinProtect.ManagementKey;
                var isValid = encodedData[6..].Span.SequenceEqual(getData.Span);
                Assert.True(isValid);
            }
        }

        private Memory<byte> GetArbitraryMgmtKey()
        {
            var keyData = new byte[]
            {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68
            };

            return new Memory<byte>(keyData);
        }
    }
}
