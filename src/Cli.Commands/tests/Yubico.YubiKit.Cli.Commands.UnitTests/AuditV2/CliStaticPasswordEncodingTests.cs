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

using Yubico.YubiKit.Cli.Commands.Otp;
using Yubico.YubiKit.Core.Transports.Hid.Keyboard;
using Yubico.YubiKit.YubiOtp;

namespace Yubico.YubiKit.Cli.Commands.UnitTests.AuditV2;

/// <summary>
///     Audit N3, defect 1: the static command must program HID scan codes for the chosen keyboard
///     layout, not the UTF-8 bytes of the text. ModHex 'c' is HID 0x06, not ASCII 0x63.
/// </summary>
public class CliStaticPasswordEncodingTests
{
    [Fact]
    [Trait("Audit", "N3")]
    public void N3_StaticPassword_IsEncodedAsScanCodesForLayout()
    {
        using var config = OtpHelpers.CreateStaticPasswordConfiguration("cbdefghijklnrtuv", KeyboardLayout.ModHex);
        using var expected = new StaticPasswordSlotConfiguration("cbdefghijklnrtuv", KeyboardLayout.ModHex);

        var bytes = config.GetConfig();
        Assert.Equal(expected.GetConfig(), bytes);
        Assert.Equal(new byte[] { 0x06, 0x05, 0x07 }, bytes[..3]);
    }

    [Fact]
    [Trait("Audit", "N3")]
    public void N3_StaticPassword_CharacterOutsideLayout_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => OtpHelpers.CreateStaticPasswordConfiguration("c!", KeyboardLayout.ModHex));
        using var usConfig = OtpHelpers.CreateStaticPasswordConfiguration("c!", KeyboardLayout.en_US);
        Assert.NotNull(usConfig);
    }

    [Theory]
    [Trait("Audit", "N3")]
    [InlineData(null, KeyboardLayout.ModHex)]
    [InlineData("modhex", KeyboardLayout.ModHex)]
    [InlineData("en_US", KeyboardLayout.en_US)]
    [InlineData("EN_us", KeyboardLayout.en_US)]
    [InlineData("de_DE", KeyboardLayout.de_DE)]
    public void N3_ParseKeyboardLayout_DefaultsToModHexAndIgnoresCase(string? text, KeyboardLayout expected) =>
        Assert.Equal(expected, OtpHelpers.ParseKeyboardLayout(text));

    [Fact]
    [Trait("Audit", "N3")]
    public void N3_ParseKeyboardLayout_Unknown_Throws() =>
        Assert.Throws<ArgumentException>(() => OtpHelpers.ParseKeyboardLayout("dvorak"));

    [Theory]
    [Trait("Audit", "N3")]
    [InlineData(KeyboardLayout.ModHex)]
    [InlineData(KeyboardLayout.en_US)]
    [InlineData(KeyboardLayout.de_DE)]
    public void N3_GeneratedPassword_UsesOnlyLayoutCharacters_NoWhitespace(KeyboardLayout layout)
    {
        var allowed = HidCodeTranslator.GetInstance(layout).SupportedCharacters.ToHashSet();

        var password = OtpHelpers.GenerateStaticPassword(38, layout);

        Assert.Equal(38, password.Length);
        Assert.All(password, c =>
        {
            Assert.Contains(c, allowed);
            Assert.False(char.IsWhiteSpace(c));
        });
        using var config = OtpHelpers.CreateStaticPasswordConfiguration(password, layout);
    }
}
