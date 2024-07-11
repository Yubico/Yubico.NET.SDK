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

namespace Yubico.Core.Iso7816.UnitTests
{
    public class ResponseApduTests
    {
        [Fact]
        public void SW_GivenResponseApdu_ReturnsCorrectStatusWord()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x69, 0x81 });

            Assert.Equal(SWConstants.IncompatibleCommand, responseApdu.SW);
        }

        [Fact]
        public void SW1_GivenResponseApdu_ReturnsUpperStatusByte()
        {
            const byte SW1 = 0x12;
            const byte SW2 = 0x34;

            var responseApdu = new ResponseApdu(new byte[] { SW1, SW2 });

            Assert.Equal(SW1, responseApdu.SW1);
        }

        [Fact]
        public void SW1_GivenResponseApdu_MatchesUpperByteOfSW()
        {
            const byte SW1 = 0x12;
            const byte SW2 = 0x34;

            var responseApdu = new ResponseApdu(new byte[] { SW1, SW2 });

            Assert.Equal(SW1, responseApdu.SW >> 8);
        }

        [Fact]
        public void SW2_GivenResponseApdu_ReturnsLowerStatusByte()
        {
            const byte SW1 = 0x12;
            const byte SW2 = 0x34;

            var responseApdu = new ResponseApdu(new byte[] { SW1, SW2 });

            Assert.Equal(SW2, responseApdu.SW2);
        }

        [Fact]
        public void SW2_GivenResponseApdu_MatchesLowerByteOfSW()
        {
            const byte SW1 = 0x12;
            const byte SW2 = 0x34;

            var responseApdu = new ResponseApdu(new byte[] { SW1, SW2 });

            Assert.Equal(SW2, responseApdu.SW & 0xFF);
        }

        [Fact]
        public void DataConstructor_GivenNullResponseData_ThrowsArgumentNullException()
        {
            // Suppressing nullable warning as we want to explicitly test this case.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => new ResponseApdu(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void DataConstructor_GivenSmallResponse_ThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => new ResponseApdu(Array.Empty<byte>()));
        }

        [Fact]
        public void DataSWConstructor_GivenNullResponseData_ThrowsArgumentNullException()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => new ResponseApdu(null, 0x0000));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void DataSWConstructor_GivenZeroLengthData_Succeeds()
        {
            var responseApdu = new ResponseApdu(Array.Empty<byte>(), 0);

            Assert.NotNull(responseApdu);
        }

        [Fact]
        public void DataSWConstructor_GivenData_ReflectedInDataProperty()
        {
            byte[] expectedData = new byte[] { 1, 2, 3, 4 };
            var responseApdu = new ResponseApdu(expectedData, 0);

            Assert.Equal(expectedData, responseApdu.Data.ToArray());
        }

        [Fact]
        public void DataSWConstructor_GivenSW_ReflectedInSWProperty()
        {
            var responseApdu = new ResponseApdu(Array.Empty<byte>(), SWConstants.Success);

            Assert.Equal(SWConstants.Success, responseApdu.SW);
        }

        [Fact]
        public void Data_GivenTwoByteResponse_IsEmptyArray()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0, 0 });

            Assert.Empty(responseApdu.Data.ToArray());
        }

        [Fact]
        public void Data_GivenLargerThanTwoByteResponse_ReturnsArrayDataExceptStatusWord()
        {
            var responseApdu = new ResponseApdu(new byte[] { 1, 2, 3, 4, 0x90, 0x00 });

            Assert.Equal(new byte[] { 1, 2, 3, 4 }, responseApdu.Data.ToArray());
        }
    }
}
