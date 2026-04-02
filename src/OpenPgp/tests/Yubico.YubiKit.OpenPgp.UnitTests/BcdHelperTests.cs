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

namespace Yubico.YubiKit.OpenPgp.UnitTests;

public class BcdHelperTests
{
    [Theory]
    [InlineData(0x00, 0)]
    [InlineData(0x01, 1)]
    [InlineData(0x09, 9)]
    [InlineData(0x10, 10)]
    [InlineData(0x57, 57)]
    [InlineData(0x99, 99)]
    public void DecodeByte_ValidBcd_ReturnsDecimalValue(byte input, int expected)
    {
        Assert.Equal(expected, BcdHelper.DecodeByte(input));
    }

    [Fact]
    public void TryDecodeSerial_ValidBcd_ReturnsTrueAndValue()
    {
        byte[] bcd = [0x12, 0x34, 0x56, 0x78];
        Assert.True(BcdHelper.TryDecodeSerial(bcd, out var result));
        Assert.Equal(12345678, result);
    }

    [Fact]
    public void TryDecodeSerial_AllZeros_ReturnsTrueAndZero()
    {
        byte[] bcd = [0x00, 0x00, 0x00, 0x00];
        Assert.True(BcdHelper.TryDecodeSerial(bcd, out var result));
        Assert.Equal(0, result);
    }

    [Fact]
    public void TryDecodeSerial_InvalidHighNibble_ReturnsFalse()
    {
        byte[] bcd = [0xA0, 0x00, 0x00, 0x00];
        Assert.False(BcdHelper.TryDecodeSerial(bcd, out _));
    }

    [Fact]
    public void TryDecodeSerial_InvalidLowNibble_ReturnsFalse()
    {
        byte[] bcd = [0x0A, 0x00, 0x00, 0x00];
        Assert.False(BcdHelper.TryDecodeSerial(bcd, out _));
    }

    [Fact]
    public void TryDecodeSerial_SingleByte_Works()
    {
        byte[] bcd = [0x42];
        Assert.True(BcdHelper.TryDecodeSerial(bcd, out var result));
        Assert.Equal(42, result);
    }
}
