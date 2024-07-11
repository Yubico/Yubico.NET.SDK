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
using System.Collections.Generic;
using Xunit;

namespace Yubico.Core.Iso7816.UnitTests
{
    public class CommandApduTests
    {
        private const int _MaximumSizeShortEncoding = 256;
        private const int _MaximumSizeExtendedEncoding = 65536;

        private static readonly byte[] _header = new byte[] { 0xBA, 0xDF, 0x00, 0xD };
        private static byte _cla => _header[0];
        private static byte _ins => _header[1];
        private static byte _p1 => _header[2];
        private static byte _p2 => _header[3];

        //
        // Private utility functions
        //
        private static byte[] GenerateRandBytes(int length)
        {
            byte[] randBytes = new byte[length];
            System.Security.Cryptography.RandomNumberGenerator.Fill(randBytes);
            return randBytes;
        }

        //
        // Set/Get fields via properties
        //
        [Fact]
        public void Cla_InitialValue_IsZero()
        {
            var commandApdu = new CommandApdu();

            Assert.Equal(0, commandApdu.Cla);
        }

        [Fact]
        public void Cla_GetAfterSet_ReturnsSameValue()
        {
            const byte cla = 0x23;

            var commandApdu = new CommandApdu
            {
                Cla = cla
            };

            Assert.Equal(cla, commandApdu.Cla);
        }

        [Fact]
        public void Ins_InitialValue_IsZero()
        {
            var commandApdu = new CommandApdu();

            Assert.Equal(0, commandApdu.Ins);
        }

        [Fact]
        public void Ins_GetAfterSet_ReturnsSameValue()
        {
            const byte ins = 0xF5;

            var commandApdu = new CommandApdu
            {
                Ins = ins
            };

            Assert.Equal(ins, commandApdu.Ins);
        }

        [Fact]
        public void P1_InitialValue_IsZero()
        {
            var commandApdu = new CommandApdu();

            Assert.Equal(0, commandApdu.P1);
        }

        [Fact]
        public void P1_GetAfterSet_ReturnsSameValue()
        {
            const byte p1 = 0x4D;

            var commandApdu = new CommandApdu
            {
                P1 = p1
            };

            Assert.Equal(p1, commandApdu.P1);
        }

        [Fact]
        public void P2_InitialValue_IsZero()
        {
            var commandApdu = new CommandApdu();

            Assert.Equal(0, commandApdu.P2);
        }

        [Fact]
        public void P2_GetAfterSet_ReturnsSameValue()
        {
            const byte p2 = 0x52;

            var commandApdu = new CommandApdu
            {
                P2 = p2
            };

            Assert.Equal(p2, commandApdu.P2);
        }

        [Fact]
        public void Data_InitialValue_IsEmpty()
        {
            var commandApdu = new CommandApdu();

            Assert.True(commandApdu.Data.IsEmpty);
        }

        [Fact]
        public void Data_GetAfterEmptySet_ReturnsSameListValues()
        {
            byte[] data = Array.Empty<byte>();

            var commandApdu = new CommandApdu()
            {
                Data = data
            };

            Assert.Equal(data, commandApdu.Data);
        }

        [Fact]
        public void Data_GetAfterSet_ReturnsSameListValues()
        {
            byte[] data = GenerateRandBytes(4);

            var commandApdu = new CommandApdu()
            {
                Data = data
            };

            Assert.Equal(data, commandApdu.Data);
        }

        [Fact]
        public void Nc_ApduWithNoData_IsZero()
        {
            var commandApdu = new CommandApdu();

            Assert.Equal(0, commandApdu.Nc);
        }

        [Fact]
        public void Nc_ApduWithEmptyData_IsZero()
        {
            var commandApdu = new CommandApdu
            {
                Data = Array.Empty<byte>(),
            };

            Assert.Equal(0, commandApdu.Nc);
        }

        [Fact]
        public void Nc_ApduWithNonZeroData_ReturnsLengthOfData()
        {
            int expectedNc = 3;
            byte[] data = GenerateRandBytes(expectedNc);

            var commandApdu = new CommandApdu()
            {
                Data = data
            };

            Assert.Equal(expectedNc, commandApdu.Nc);
        }

        [Fact]
        public void Ne_InitialValue_IsZero()
        {
            var commandApdu = new CommandApdu();

            Assert.Equal(0, commandApdu.Ne);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0x1234)]
        [InlineData(int.MaxValue)]
        public void Ne_GetAfterSet_ReturnsSameValue(int ne)
        {
            var commandApdu = new CommandApdu
            {
                Ne = ne
            };

            Assert.Equal(ne, commandApdu.Ne);
        }

        [Fact]
        public void Ne_SetNegative_ThrowsArgumentOutOfRangeException()
        {
            const int ne = -1;

            static CommandApdu actual() => new CommandApdu
            {
                Ne = ne
            };

            _ = Assert.Throws<ArgumentOutOfRangeException>(actual);
        }

        //
        // Automatic encoded byte array - using AsByteArray()
        //

        // Case 1, APDU = [Header]
        [Fact]
        public void AsByteArray_DefaultApdu_EmitsFourByteHeader()
        {
            var commandApdu = new CommandApdu();

            byte[] byteArray = commandApdu.AsByteArray();

            Assert.Equal(4, byteArray.Length);
        }

        [Fact]
        public void AsByteArray_Data0Length_EmitsFourByteHeader()
        {
            byte[] data = Array.Empty<byte>();

            var commandApdu = new CommandApdu
            {
                Data = data,
            };

            byte[] byteArray = commandApdu.AsByteArray();

            Assert.Equal(4, byteArray.Length);
        }

        [Fact]
        public void AsByteArray_ClaInsP1P2Set_EmitsCorrectFourByteHeader()
        {
            var commandApdu = new CommandApdu
            {
                Cla = _cla,
                Ins = _ins,
                P1 = _p1,
                P2 = _p2,
            };

            byte[] actualByteArray = commandApdu.AsByteArray();

            Assert.Equal(_header, actualByteArray);
        }

        // Case 2S, APDU = [Header][short Le]
        [Theory]
        [InlineData(1, new byte[] { 0x01 })]
        [InlineData(0x12, new byte[] { 0x12 })]
        [InlineData(_MaximumSizeShortEncoding, new byte[] { 0x00 })]
        public void AsByteArray_HeaderSetNeSet_EmitsCorrectApduShort(int ne, byte[] expectedLe)
        {
            var expectedByteArray = new List<byte>();
            expectedByteArray.AddRange(_header);
            expectedByteArray.AddRange(expectedLe);

            var commandApdu = new CommandApdu
            {
                Cla = _cla,
                Ins = _ins,
                P1 = _p1,
                P2 = _p2,
                Ne = ne,
            };

            byte[] actualByteArray = commandApdu.AsByteArray();

            Assert.Equal(expectedByteArray, actualByteArray);
        }

        // Case 2E, APDU = [Header][extended (3 byte) Le]
        [Theory]
        [InlineData(_MaximumSizeShortEncoding + 1, new byte[] { 0x00, 0x01, 0x01 })]
        [InlineData(0x1234, new byte[] { 0x00, 0x12, 0x34 })]
        [InlineData(_MaximumSizeExtendedEncoding, new byte[] { 0x00, 0x00, 0x00 })]
        public void AsByteArray_HeaderSetNeSet_EmitsCorrectApduExtended(int ne, byte[] expectedLe)
        {
            var expectedByteArray = new List<byte>();
            expectedByteArray.AddRange(_header);
            expectedByteArray.AddRange(expectedLe);

            var commandApdu = new CommandApdu
            {
                Cla = _cla,
                Ins = _ins,
                P1 = _p1,
                P2 = _p2,
                Ne = ne,
            };

            byte[] actualByteArray = commandApdu.AsByteArray();

            Assert.Equal(expectedByteArray, actualByteArray);
        }

        // Case 3S, APDU = [Header][short Lc][Data]
        [Theory]
        [InlineData(1, new byte[] { 0x01 })]
        [InlineData(0x12, new byte[] { 0x12 })]
        [InlineData(_MaximumSizeShortEncoding, new byte[] { 0x00 })]
        public void AsByteArray_HeaderSetDataSet_EmitsCorrectApduShort(int nc, byte[] expectedLc)
        {
            byte[] data = GenerateRandBytes(nc);

            var expectedByteArray = new List<byte>();
            expectedByteArray.AddRange(_header);
            expectedByteArray.AddRange(expectedLc);
            expectedByteArray.AddRange(data);

            var commandApdu = new CommandApdu
            {
                Cla = _cla,
                Ins = _ins,
                P1 = _p1,
                P2 = _p2,
                Data = data,
            };

            byte[] actualByteArray = commandApdu.AsByteArray();

            Assert.Equal(expectedByteArray, actualByteArray);
        }

        // Case 3E, APDU = [Header][extended Lc][Data]
        [Theory]
        [InlineData(_MaximumSizeShortEncoding + 1, new byte[] { 0x00, 0x01, 0x01 })]
        [InlineData(0x1234, new byte[] { 0x00, 0x12, 0x34 })]
        [InlineData(_MaximumSizeExtendedEncoding, new byte[] { 0x00, 0x00, 0x00 })]
        public void AsByteArray_HeaderSetDataSet_EmitsCorrectApduExtended(int nc, byte[] expectedLc)
        {
            byte[] data = GenerateRandBytes(nc);

            var expectedByteArray = new List<byte>();
            expectedByteArray.AddRange(_header);
            expectedByteArray.AddRange(expectedLc);
            expectedByteArray.AddRange(data);

            var commandApdu = new CommandApdu
            {
                Cla = _cla,
                Ins = _ins,
                P1 = _p1,
                P2 = _p2,
                Data = data,
            };

            byte[] actualByteArray = commandApdu.AsByteArray();

            Assert.Equal(expectedByteArray, actualByteArray);
        }

        // Case 4S, APDU = [Header][short Lc][Data][short Le]
        [Theory]
        [InlineData(1, new byte[] { 0x01 }, 1, new byte[] { 0x01 })]
        [InlineData(0x12, new byte[] { 0x12 }, 0x12, new byte[] { 0x12 })]
        [InlineData(_MaximumSizeShortEncoding, new byte[] { 0x00 }, _MaximumSizeShortEncoding, new byte[] { 0x00 })]
        public void AsByteArray_HeaderSetDataSetNeSet_EmitsCorrectApduShort(
            int nc, byte[] expectedLc, int ne, byte[] expectedLe)
        {
            byte[] data = GenerateRandBytes(nc);

            var expectedByteArray = new List<byte>();
            expectedByteArray.AddRange(_header);
            expectedByteArray.AddRange(expectedLc);
            expectedByteArray.AddRange(data);
            expectedByteArray.AddRange(expectedLe);

            var commandApdu = new CommandApdu
            {
                Cla = _cla,
                Ins = _ins,
                P1 = _p1,
                P2 = _p2,
                Data = data,
                Ne = ne,
            };

            byte[] actualByteArray = commandApdu.AsByteArray();

            Assert.Equal(expectedByteArray, actualByteArray);
        }

        // Case 4E - matching, APDU = [Header][extended Lc][Data][extended (2 byte) Le]
        // Case 4E - mixed, APDU = [Header][extended Lc][Data][extended (2 byte) Le]
        [Theory]
        [InlineData(_MaximumSizeShortEncoding + 1, new byte[] { 0x00, 0x01, 0x01 }, _MaximumSizeShortEncoding + 1,
            new byte[] { 0x01, 0x01 })]
        [InlineData(0x1234, new byte[] { 0x00, 0x12, 0x34 }, 0x1234, new byte[] { 0x12, 0x34 })]
        [InlineData(_MaximumSizeExtendedEncoding, new byte[] { 0x00, 0x00, 0x00 }, _MaximumSizeExtendedEncoding,
            new byte[] { 0x00, 0x00 })]
        [InlineData(1, new byte[] { 0x00, 0x00, 0x01 }, _MaximumSizeShortEncoding + 1, new byte[] { 0x01, 0x01 })]
        [InlineData(_MaximumSizeShortEncoding + 1, new byte[] { 0x00, 0x01, 0x01 }, 1, new byte[] { 0x00, 0x01 })]
        public void AsByteArray_HeaderSetDataSetNeSet_EmitsCorrectApduExtended(
            int nc, byte[] expectedLc, int ne, byte[] expectedLe)
        {
            byte[] data = GenerateRandBytes(nc);

            var expectedByteArray = new List<byte>();
            expectedByteArray.AddRange(_header);
            expectedByteArray.AddRange(expectedLc);
            expectedByteArray.AddRange(data);
            expectedByteArray.AddRange(expectedLe);

            var commandApdu = new CommandApdu
            {
                Cla = _cla,
                Ins = _ins,
                P1 = _p1,
                P2 = _p2,
                Data = data,
                Ne = ne,
            };

            byte[] actualByteArray = commandApdu.AsByteArray();

            Assert.Equal(expectedByteArray, actualByteArray);
        }

        // EXCEPTION - cannot find suitable encoding
        [Fact]
        public void AsByteArray_NcLargerThanExtendedMax_ThrowsInvalidOperationException()
        {
            byte[] data = GenerateRandBytes(_MaximumSizeExtendedEncoding + 1);

            var commandApdu = new CommandApdu()
            {
                Data = data,
            };

            void encodeCommand() => commandApdu.AsByteArray();
            _ = Assert.Throws<InvalidOperationException>(encodeCommand);
        }

        // EXCEPTION - cannot find suitable encoding
        [Fact]
        public void AsByteArray_NeLargerThanExtendedMax_ThrowsInvalidOperationException()
        {
            int ne = _MaximumSizeExtendedEncoding + 1;

            var commandApdu = new CommandApdu()
            {
                Ne = ne,
            };

            void encodeCommand() => commandApdu.AsByteArray();
            _ = Assert.Throws<InvalidOperationException>(encodeCommand);
        }


        //
        // Short encoded byte array
        //
        [Theory]
        [InlineData(_MaximumSizeExtendedEncoding, 0)]
        [InlineData(0, _MaximumSizeExtendedEncoding)]
        [InlineData(_MaximumSizeExtendedEncoding, _MaximumSizeShortEncoding)]
        [InlineData(_MaximumSizeShortEncoding, _MaximumSizeExtendedEncoding)]
        [InlineData(_MaximumSizeExtendedEncoding, _MaximumSizeExtendedEncoding)]
        public void AsShortByteArray_NcAndOrNeInvalidSize_ThrowsInvalidOperationException(int nc, int ne)
        {
            byte[] data = new byte[nc];

            var commandApdu = new CommandApdu
            {
                Data = data,
                Ne = ne,
            };

            void encodeCommand() => commandApdu.AsByteArray(ApduEncoding.ShortLength);
            _ = Assert.Throws<InvalidOperationException>(encodeCommand);
        }


        //
        // Extended encoded byte array
        //

        // Case 4E - matching, APDU = [Header][extended Lc][Data][extended (2 byte) Le]
        [Fact]
        public void AsExtendedByteArray_DataAndNeShortSize_EmitsCorrectApduExtended()
        {
            int nc = 1;
            byte[] expectedLc = new byte[] { 0x00, 0x00, 0x01 };
            int ne = 1;
            byte[] expectedLe = new byte[] { 0x00, 0x01 };
            byte[] data = GenerateRandBytes(nc);

            var expectedByteArray = new List<byte>();
            expectedByteArray.AddRange(_header);
            expectedByteArray.AddRange(expectedLc);
            expectedByteArray.AddRange(data);
            expectedByteArray.AddRange(expectedLe);

            var commandApdu = new CommandApdu
            {
                Cla = _cla,
                Ins = _ins,
                P1 = _p1,
                P2 = _p2,
                Data = data,
                Ne = ne,
            };

            byte[] actualByteArray = commandApdu.AsByteArray(ApduEncoding.ExtendedLength);

            Assert.Equal(expectedByteArray, actualByteArray);
        }

        [Theory]
        [InlineData(_MaximumSizeExtendedEncoding + 1, 0)]
        [InlineData(0, _MaximumSizeExtendedEncoding + 1)]
        [InlineData(_MaximumSizeExtendedEncoding + 1, _MaximumSizeShortEncoding)]
        [InlineData(_MaximumSizeShortEncoding, _MaximumSizeExtendedEncoding + 1)]
        [InlineData(_MaximumSizeExtendedEncoding + 1, _MaximumSizeExtendedEncoding + 1)]
        public void AsExtendedByteArray_NcAndOrNeInvalidSize_ThrowsInvalidOperationException(int nc, int ne)
        {
            byte[] data = new byte[nc];

            var commandApdu = new CommandApdu
            {
                Data = data,
                Ne = ne,
            };

            void encodeCommand() => commandApdu.AsByteArray(ApduEncoding.ExtendedLength);
            _ = Assert.Throws<InvalidOperationException>(encodeCommand);
        }


        //
        // Check that it catches non-valid ApduEncoding
        //
        [Fact]
        public void AsEncodingByteArray_InvalidApduEncoding_ThrowsArgumentOutOfRangeException()
        {
            var commandApdu = new CommandApdu();

            void encodedCommand() => commandApdu.AsByteArray((ApduEncoding)(-1));
            _ = Assert.Throws<ArgumentOutOfRangeException>(encodedCommand);
        }


        //
        // Encodes Ne correctly when set to int.MaxValue
        //
        [Theory]
        [InlineData(ApduEncoding.ShortLength)]
        [InlineData(ApduEncoding.ExtendedLength)]
        public void AsEncodingByteArray_NeSetIntMaxValue_EmitsCorrectApduMaxNe(ApduEncoding apduEncoding)
        {
            int ne = int.MaxValue;
            byte[] expectedLe = apduEncoding switch
            {
                ApduEncoding.ShortLength => new byte[] { 0x00 },
                ApduEncoding.ExtendedLength => new byte[] { 0x00, 0x00, 0x00 },
                _ => Array.Empty<byte>(), // Shouldn't be reached
            };

            var expectedByteArray = new List<byte>();
            expectedByteArray.AddRange(_header);
            expectedByteArray.AddRange(expectedLe);

            var commandApdu = new CommandApdu()
            {
                Cla = _cla,
                Ins = _ins,
                P1 = _p1,
                P2 = _p2,
                Ne = ne,
            };

            byte[] actualByteArray = commandApdu.AsByteArray(apduEncoding);

            Assert.Equal(expectedByteArray, actualByteArray);
        }
    }
}
