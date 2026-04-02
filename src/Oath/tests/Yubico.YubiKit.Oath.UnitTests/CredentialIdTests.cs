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

using System.Text;

namespace Yubico.YubiKit.Oath.UnitTests;

public class CredentialIdTests
{
    // --- FormatCredentialId tests ---

    [Fact]
    public void FormatCredentialId_TotpWithIssuerDefaultPeriod_ReturnsIssuerColonName()
    {
        byte[] id = Credential.FormatCredentialId("GitHub", "user@example.com", OathType.Totp);

        Assert.Equal("GitHub:user@example.com", Encoding.UTF8.GetString(id));
    }

    [Fact]
    public void FormatCredentialId_TotpWithoutIssuerDefaultPeriod_ReturnsNameOnly()
    {
        byte[] id = Credential.FormatCredentialId(null, "user@example.com", OathType.Totp);

        Assert.Equal("user@example.com", Encoding.UTF8.GetString(id));
    }

    [Fact]
    public void FormatCredentialId_TotpWithIssuerNonDefaultPeriod_ReturnsPeriodPrefixed()
    {
        byte[] id = Credential.FormatCredentialId("GitHub", "user@example.com", OathType.Totp, 60);

        Assert.Equal("60/GitHub:user@example.com", Encoding.UTF8.GetString(id));
    }

    [Fact]
    public void FormatCredentialId_TotpWithoutIssuerNonDefaultPeriod_ReturnsPeriodSlashName()
    {
        byte[] id = Credential.FormatCredentialId(null, "user@example.com", OathType.Totp, 60);

        Assert.Equal("60/user@example.com", Encoding.UTF8.GetString(id));
    }

    [Fact]
    public void FormatCredentialId_HotpWithIssuer_ReturnsIssuerColonNameNoPeriod()
    {
        byte[] id = Credential.FormatCredentialId("GitHub", "user@example.com", OathType.Hotp);

        Assert.Equal("GitHub:user@example.com", Encoding.UTF8.GetString(id));
    }

    [Fact]
    public void FormatCredentialId_HotpWithoutIssuer_ReturnsNameOnly()
    {
        byte[] id = Credential.FormatCredentialId(null, "user@example.com", OathType.Hotp);

        Assert.Equal("user@example.com", Encoding.UTF8.GetString(id));
    }

    [Fact]
    public void FormatCredentialId_HotpWithNonDefaultPeriod_DoesNotPrefixPeriod()
    {
        // HOTP never includes period prefix, even if a non-default period is passed
        byte[] id = Credential.FormatCredentialId("GitHub", "user@example.com", OathType.Hotp, 60);

        Assert.Equal("GitHub:user@example.com", Encoding.UTF8.GetString(id));
    }

    // --- ParseCredentialId tests ---

    [Fact]
    public void ParseCredentialId_TotpWithIssuerDefaultPeriod_ParsesCorrectly()
    {
        byte[] id = "GitHub:user@example.com"u8.ToArray();

        var (issuer, name, period) = Credential.ParseCredentialId(id, OathType.Totp);

        Assert.Equal("GitHub", issuer);
        Assert.Equal("user@example.com", name);
        Assert.Equal(30, period);
    }

    [Fact]
    public void ParseCredentialId_TotpWithoutIssuer_ParsesCorrectly()
    {
        byte[] id = "user@example.com"u8.ToArray();

        var (issuer, name, period) = Credential.ParseCredentialId(id, OathType.Totp);

        Assert.Null(issuer);
        Assert.Equal("user@example.com", name);
        Assert.Equal(30, period);
    }

    [Fact]
    public void ParseCredentialId_TotpWithIssuerNonDefaultPeriod_ParsesCorrectly()
    {
        byte[] id = "60/GitHub:user@example.com"u8.ToArray();

        var (issuer, name, period) = Credential.ParseCredentialId(id, OathType.Totp);

        Assert.Equal("GitHub", issuer);
        Assert.Equal("user@example.com", name);
        Assert.Equal(60, period);
    }

    [Fact]
    public void ParseCredentialId_TotpWithoutIssuerNonDefaultPeriod_ParsesCorrectly()
    {
        byte[] id = "60/user@example.com"u8.ToArray();

        var (issuer, name, period) = Credential.ParseCredentialId(id, OathType.Totp);

        Assert.Null(issuer);
        Assert.Equal("user@example.com", name);
        Assert.Equal(60, period);
    }

    [Fact]
    public void ParseCredentialId_HotpWithIssuer_ParsesCorrectly()
    {
        byte[] id = "GitHub:user@example.com"u8.ToArray();

        var (issuer, name, period) = Credential.ParseCredentialId(id, OathType.Hotp);

        Assert.Equal("GitHub", issuer);
        Assert.Equal("user@example.com", name);
        Assert.Equal(0, period);
    }

    [Fact]
    public void ParseCredentialId_HotpWithoutIssuer_ParsesCorrectly()
    {
        byte[] id = "user@example.com"u8.ToArray();

        var (issuer, name, period) = Credential.ParseCredentialId(id, OathType.Hotp);

        Assert.Null(issuer);
        Assert.Equal("user@example.com", name);
        Assert.Equal(0, period);
    }

    // --- Round-trip tests ---

    [Theory]
    [InlineData("GitHub", "user@example.com", OathType.Totp, 30)]
    [InlineData("GitHub", "user@example.com", OathType.Totp, 60)]
    [InlineData(null, "user@example.com", OathType.Totp, 30)]
    [InlineData(null, "user@example.com", OathType.Totp, 60)]
    [InlineData("GitHub", "user@example.com", OathType.Hotp, 30)]
    [InlineData(null, "user@example.com", OathType.Hotp, 30)]
    public void CredentialId_RoundTrips(string? issuer, string name, OathType oathType, int period)
    {
        byte[] id = Credential.FormatCredentialId(issuer, name, oathType, period);
        var (parsedIssuer, parsedName, parsedPeriod) = Credential.ParseCredentialId(id, oathType);

        Assert.Equal(issuer, parsedIssuer);
        Assert.Equal(name, parsedName);

        if (oathType == OathType.Totp)
        {
            Assert.Equal(period, parsedPeriod);
        }
        else
        {
            Assert.Equal(0, parsedPeriod);
        }
    }

    [Fact]
    public void ParseCredentialId_HotpStartingWithColon_TreatsAsName()
    {
        // Edge case: colon at start means no issuer (matches Python behavior)
        byte[] id = ":user@example.com"u8.ToArray();

        var (issuer, name, period) = Credential.ParseCredentialId(id, OathType.Hotp);

        Assert.Null(issuer);
        Assert.Equal(":user@example.com", name);
        Assert.Equal(0, period);
    }
}