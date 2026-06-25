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

public class OpenPgpAidTests
{
    [Fact]
    public void Parse_ValidAid_DecodesVersionAndManufacturer()
    {
        // RID(6 bytes) + version 3.4 (BCD) + manufacturer 0x0006 (Yubico) + serial 12345678 (BCD)
        byte[] aid =
        [
            0xD2, 0x76, 0x00, 0x01, 0x24, 0x01, // RID + PIX prefix
            0x03, 0x04, // version 3.4 (BCD)
            0x00, 0x06, // manufacturer = 6 (Yubico)
            0x12, 0x34, 0x56, 0x78, // serial = 12345678 (BCD)
        ];

        var parsed = OpenPgpAid.Parse(aid);

        Assert.Equal(3, parsed.Version.Major);
        Assert.Equal(4, parsed.Version.Minor);
        Assert.Equal(6, parsed.Manufacturer);
        Assert.Equal(12345678, parsed.Serial);
    }

    [Fact]
    public void Parse_ValidBcdSerial_ReturnsPositiveSerial()
    {
        byte[] aid =
        [
            0xD2, 0x76, 0x00, 0x01, 0x24, 0x01,
            0x02, 0x00, // version 2.0
            0x00, 0x06, // manufacturer
            0x00, 0x00, 0x01, 0x23, // serial = 123
        ];

        var parsed = OpenPgpAid.Parse(aid);

        Assert.Equal(123, parsed.Serial);
    }

    [Fact]
    public void Parse_InvalidBcdSerial_ReturnsNegativeRawValue()
    {
        // Serial bytes contain hex digits A-F which are invalid BCD
        byte[] aid =
        [
            0xD2, 0x76, 0x00, 0x01, 0x24, 0x01,
            0x03, 0x04,
            0x00, 0x06,
            0x00, 0x0A, 0xBB, 0xCC, // invalid BCD (contains A, B, C)
        ];

        var parsed = OpenPgpAid.Parse(aid);

        // Raw uint32 = 0x000ABBCC = 703420, negated
        Assert.True(parsed.Serial < 0);
        Assert.Equal(-(int)0x000ABBCC, parsed.Serial);
    }

    [Fact]
    public void Parse_TooShort_Throws()
    {
        byte[] shortAid = [0xD2, 0x76, 0x00];
        Assert.Throws<ArgumentException>(() => OpenPgpAid.Parse(shortAid));
    }

    [Fact]
    public void Parse_PreservesRawBytes()
    {
        byte[] aid =
        [
            0xD2, 0x76, 0x00, 0x01, 0x24, 0x01,
            0x03, 0x04,
            0x00, 0x06,
            0x12, 0x34, 0x56, 0x78,
        ];

        var parsed = OpenPgpAid.Parse(aid);

        Assert.Equal(aid, parsed.Raw.ToArray());
    }

    [Fact]
    public void Parse_ZeroSerial_DecodesCorrectly()
    {
        byte[] aid =
        [
            0xD2, 0x76, 0x00, 0x01, 0x24, 0x01,
            0x03, 0x04,
            0x00, 0x06,
            0x00, 0x00, 0x00, 0x00, // serial = 0
        ];

        var parsed = OpenPgpAid.Parse(aid);
        Assert.Equal(0, parsed.Serial);
    }
}
