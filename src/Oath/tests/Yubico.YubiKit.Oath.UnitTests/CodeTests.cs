// Copyright 2026 Yubico AB
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

namespace Yubico.YubiKit.Oath.UnitTests;

public class CodeTests
{
    [Fact]
    public void FormatCode_Totp6Digits_ReturnsZeroPaddedCode()
    {
        var credential = new Credential(
            deviceId: "testdevice",
            id: "GitHub:user"u8.ToArray(),
            issuer: "GitHub",
            name: "user",
            oathType: OathType.Totp,
            period: 30,
            touchRequired: false);

        // Truncated response: [digits=6, then 4 bytes of truncated HMAC]
        // Code value: (0x00_04_93_e0 & 0x7FFFFFFF) % 10^6 = 300000
        byte[] truncated = [0x06, 0x00, 0x04, 0x93, 0xe0];
        long timestamp = 1700000000L; // Unix timestamp

        var code = Code.FormatCode(credential, timestamp, truncated);

        Assert.Equal("300000", code.Value);
        Assert.Equal(6, code.Value.Length);
    }

    [Fact]
    public void FormatCode_Totp6Digits_CalculatesValidityWindow()
    {
        var credential = new Credential(
            deviceId: "testdevice",
            id: "Test:user"u8.ToArray(),
            issuer: "Test",
            name: "user",
            oathType: OathType.Totp,
            period: 30,
            touchRequired: false);

        byte[] truncated = [0x06, 0x00, 0x00, 0x00, 0x01];
        long timestamp = 1700000010L; // 10 seconds into a 30s window

        var code = Code.FormatCode(credential, timestamp, truncated);

        // time_step = 1700000010 / 30 = 56666667
        // valid_from = 56666667 * 30 = 1700000010
        long expectedTimeStep = timestamp / 30;
        Assert.Equal(expectedTimeStep * 30, code.ValidFrom);
        Assert.Equal((expectedTimeStep + 1) * 30, code.ValidTo);
    }

    [Fact]
    public void FormatCode_Hotp_UsesMaxValidTo()
    {
        var credential = new Credential(
            deviceId: "testdevice",
            id: "user"u8.ToArray(),
            issuer: null,
            name: "user",
            oathType: OathType.Hotp,
            period: 0,
            touchRequired: false);

        byte[] truncated = [0x06, 0x00, 0x00, 0x00, 0x01];
        long timestamp = 1700000000L;

        var code = Code.FormatCode(credential, timestamp, truncated);

        Assert.Equal(timestamp, code.ValidFrom);
        Assert.Equal(long.MaxValue, code.ValidTo);
    }

    [Fact]
    public void FormatCode_SmallValue_ZeroPadsToDigitCount()
    {
        var credential = new Credential(
            deviceId: "testdevice",
            id: "user"u8.ToArray(),
            issuer: null,
            name: "user",
            oathType: OathType.Totp,
            period: 30,
            touchRequired: false);

        // (0x00_00_00_01 & 0x7FFFFFFF) % 10^6 = 1 → "000001"
        byte[] truncated = [0x06, 0x00, 0x00, 0x00, 0x01];

        var code = Code.FormatCode(credential, 1700000000L, truncated);

        Assert.Equal("000001", code.Value);
    }

    [Fact]
    public void FormatCode_8Digits_FormatsCorrectly()
    {
        var credential = new Credential(
            deviceId: "testdevice",
            id: "user"u8.ToArray(),
            issuer: null,
            name: "user",
            oathType: OathType.Totp,
            period: 30,
            touchRequired: false);

        // (0x00_00_00_2A & 0x7FFFFFFF) % 10^8 = 42 → "00000042"
        byte[] truncated = [0x08, 0x00, 0x00, 0x00, 0x2A];

        var code = Code.FormatCode(credential, 1700000000L, truncated);

        Assert.Equal("00000042", code.Value);
        Assert.Equal(8, code.Value.Length);
    }

    [Fact]
    public void FormatCode_HighBitSet_MasksCorrectly()
    {
        var credential = new Credential(
            deviceId: "testdevice",
            id: "user"u8.ToArray(),
            issuer: null,
            name: "user",
            oathType: OathType.Totp,
            period: 30,
            touchRequired: false);

        // High bit set: 0x80_00_00_01 & 0x7FFFFFFF = 1 → "000001"
        byte[] truncated = [0x06, 0x80, 0x00, 0x00, 0x01];

        var code = Code.FormatCode(credential, 1700000000L, truncated);

        Assert.Equal("000001", code.Value);
    }

    [Fact]
    public void FormatCode_Totp60sPeriod_CalculatesCorrectWindow()
    {
        var credential = new Credential(
            deviceId: "testdevice",
            id: "60/Test:user"u8.ToArray(),
            issuer: "Test",
            name: "user",
            oathType: OathType.Totp,
            period: 60,
            touchRequired: false);

        byte[] truncated = [0x06, 0x00, 0x00, 0x00, 0x01];
        long timestamp = 1700000010L;

        var code = Code.FormatCode(credential, timestamp, truncated);

        long expectedTimeStep = timestamp / 60;
        Assert.Equal(expectedTimeStep * 60, code.ValidFrom);
        Assert.Equal((expectedTimeStep + 1) * 60, code.ValidTo);
    }
}