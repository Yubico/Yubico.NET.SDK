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

public class CredentialDataTests
{
    // --- ParseUri: valid TOTP ---

    [Fact]
    public void ParseUri_ValidTotpWithIssuer_ParsesAllFields()
    {
        const string uri = "otpauth://totp/GitHub:user@example.com?secret=JBSWY3DPEHPK3PXP&issuer=GitHub&algorithm=SHA1&digits=6&period=30";

        var data = CredentialData.ParseUri(uri);

        Assert.Equal("user@example.com", data.Name);
        Assert.Equal(OathType.Totp, data.OathType);
        Assert.Equal(OathHashAlgorithm.Sha1, data.HashAlgorithm);
        Assert.Equal(6, data.Digits);
        Assert.Equal(30, data.Period);
        Assert.Equal(0, data.Counter);
        Assert.Equal("GitHub", data.Issuer);
        Assert.NotEmpty(data.Secret);
    }

    [Fact]
    public void ParseUri_TotpWithDefaults_UsesDefaultValues()
    {
        const string uri = "otpauth://totp/user@example.com?secret=JBSWY3DPEHPK3PXP";

        var data = CredentialData.ParseUri(uri);

        Assert.Equal("user@example.com", data.Name);
        Assert.Equal(OathType.Totp, data.OathType);
        Assert.Equal(OathHashAlgorithm.Sha1, data.HashAlgorithm);
        Assert.Equal(6, data.Digits);
        Assert.Equal(30, data.Period);
        Assert.Null(data.Issuer);
    }

    [Fact]
    public void ParseUri_TotpWithSha256_ParsesAlgorithm()
    {
        const string uri = "otpauth://totp/Test:user?secret=JBSWY3DPEHPK3PXP&algorithm=SHA256";

        var data = CredentialData.ParseUri(uri);

        Assert.Equal(OathHashAlgorithm.Sha256, data.HashAlgorithm);
    }

    [Fact]
    public void ParseUri_TotpWithSha512_ParsesAlgorithm()
    {
        const string uri = "otpauth://totp/Test:user?secret=JBSWY3DPEHPK3PXP&algorithm=SHA512";

        var data = CredentialData.ParseUri(uri);

        Assert.Equal(OathHashAlgorithm.Sha512, data.HashAlgorithm);
    }

    [Fact]
    public void ParseUri_TotpWith8Digits_ParsesDigits()
    {
        const string uri = "otpauth://totp/Test:user?secret=JBSWY3DPEHPK3PXP&digits=8";

        var data = CredentialData.ParseUri(uri);

        Assert.Equal(8, data.Digits);
    }

    [Fact]
    public void ParseUri_TotpWith60sPeriod_ParsesPeriod()
    {
        const string uri = "otpauth://totp/Test:user?secret=JBSWY3DPEHPK3PXP&period=60";

        var data = CredentialData.ParseUri(uri);

        Assert.Equal(60, data.Period);
    }

    [Fact]
    public void ParseUri_IssuerInPathAndQuery_QueryTakesPrecedence()
    {
        const string uri = "otpauth://totp/PathIssuer:user?secret=JBSWY3DPEHPK3PXP&issuer=QueryIssuer";

        var data = CredentialData.ParseUri(uri);

        Assert.Equal("QueryIssuer", data.Issuer);
        Assert.Equal("user", data.Name);
    }

    [Fact]
    public void ParseUri_IssuerOnlyInPath_UsesPathIssuer()
    {
        const string uri = "otpauth://totp/GitHub:user?secret=JBSWY3DPEHPK3PXP";

        var data = CredentialData.ParseUri(uri);

        Assert.Equal("GitHub", data.Issuer);
    }

    // --- ParseUri: valid HOTP ---

    [Fact]
    public void ParseUri_ValidHotp_ParsesCorrectly()
    {
        const string uri = "otpauth://hotp/Service:user?secret=JBSWY3DPEHPK3PXP&counter=42";

        var data = CredentialData.ParseUri(uri);

        Assert.Equal("user", data.Name);
        Assert.Equal(OathType.Hotp, data.OathType);
        Assert.Equal("Service", data.Issuer);
        Assert.Equal(42, data.Counter);
    }

    [Fact]
    public void ParseUri_HotpWithoutCounter_DefaultsToZero()
    {
        const string uri = "otpauth://hotp/user?secret=JBSWY3DPEHPK3PXP";

        var data = CredentialData.ParseUri(uri);

        Assert.Equal(OathType.Hotp, data.OathType);
        Assert.Equal(0, data.Counter);
    }

    // --- ParseUri: invalid inputs ---

    [Fact]
    public void ParseUri_InvalidScheme_ThrowsArgumentException()
    {
        const string uri = "https://totp/user?secret=JBSWY3DPEHPK3PXP";

        Assert.Throws<ArgumentException>(() => CredentialData.ParseUri(uri));
    }

    [Fact]
    public void ParseUri_MissingSecret_ThrowsArgumentException()
    {
        const string uri = "otpauth://totp/user?issuer=Test";

        Assert.Throws<ArgumentException>(() => CredentialData.ParseUri(uri));
    }

    [Fact]
    public void ParseUri_InvalidOathType_ThrowsArgumentException()
    {
        const string uri = "otpauth://invalid/user?secret=JBSWY3DPEHPK3PXP";

        Assert.Throws<ArgumentException>(() => CredentialData.ParseUri(uri));
    }

    [Fact]
    public void ParseUri_UnpaddedBase32Secret_ParsesSuccessfully()
    {
        // "JBSWY3DPEHPK3PXP" is already valid, but test with a shorter unpadded secret
        const string uri = "otpauth://totp/Test:user?secret=JBSWY3DP";

        var data = CredentialData.ParseUri(uri);

        Assert.NotEmpty(data.Secret);
    }

    [Fact]
    public void ParseUri_SecretWithSpaces_ParsesSuccessfully()
    {
        const string uri = "otpauth://totp/Test:user?secret=JBSW Y3DP EHPK 3PXP";

        var data = CredentialData.ParseUri(uri);

        Assert.NotEmpty(data.Secret);
    }

    [Fact]
    public void ParseUri_CaseInsensitiveType_ParsesTotp()
    {
        const string uri = "otpauth://TOTP/Test:user?secret=JBSWY3DPEHPK3PXP";

        var data = CredentialData.ParseUri(uri);

        Assert.Equal(OathType.Totp, data.OathType);
    }

    // --- GetId ---

    [Fact]
    public void GetId_TotpWithIssuerDefaultPeriod_MatchesFormatCredentialId()
    {
        var data = new CredentialData
        {
            Name = "user@example.com",
            OathType = OathType.Totp,
            HashAlgorithm = OathHashAlgorithm.Sha1,
            Secret = [0x01, 0x02],
            Issuer = "GitHub"
        };

        byte[] id = data.GetId();
        byte[] expected = Credential.FormatCredentialId("GitHub", "user@example.com", OathType.Totp);

        Assert.Equal(expected, id);
    }

    [Fact]
    public void GetId_TotpNonDefaultPeriod_IncludesPeriodPrefix()
    {
        var data = new CredentialData
        {
            Name = "user",
            OathType = OathType.Totp,
            HashAlgorithm = OathHashAlgorithm.Sha1,
            Secret = [0x01, 0x02],
            Issuer = "Test",
            Period = 60
        };

        byte[] id = data.GetId();

        Assert.Equal("60/Test:user", Encoding.UTF8.GetString(id));
    }

    // --- Key shortening ---

    [Fact]
    public void HmacShortenKey_Sha1KeyWithinBlockSize_ReturnsUnchanged()
    {
        byte[] key = new byte[64]; // Exactly SHA-1 block size
        Array.Fill(key, (byte)0xAA);

        byte[] result = CredentialData.HmacShortenKey(key, OathHashAlgorithm.Sha1);

        Assert.Equal(key, result);
    }

    [Fact]
    public void HmacShortenKey_Sha1KeyExceedsBlockSize_ReturnsHashedKey()
    {
        byte[] key = new byte[65]; // 1 byte over SHA-1 block size
        Array.Fill(key, (byte)0xAA);

        byte[] result = CredentialData.HmacShortenKey(key, OathHashAlgorithm.Sha1);

        Assert.Equal(20, result.Length); // SHA-1 digest size
    }

    [Fact]
    public void HmacShortenKey_Sha256KeyExceedsBlockSize_ReturnsHashedKey()
    {
        byte[] key = new byte[65];
        Array.Fill(key, (byte)0xBB);

        byte[] result = CredentialData.HmacShortenKey(key, OathHashAlgorithm.Sha256);

        Assert.Equal(32, result.Length); // SHA-256 digest size
    }

    [Fact]
    public void HmacShortenKey_Sha512KeyExceedsBlockSize_ReturnsHashedKey()
    {
        byte[] key = new byte[129]; // SHA-512 block size is 128
        Array.Fill(key, (byte)0xCC);

        byte[] result = CredentialData.HmacShortenKey(key, OathHashAlgorithm.Sha512);

        Assert.Equal(64, result.Length); // SHA-512 digest size
    }

    [Fact]
    public void HmacShortenKey_Sha512KeyWithinBlockSize_ReturnsUnchanged()
    {
        byte[] key = new byte[128]; // Exactly SHA-512 block size
        Array.Fill(key, (byte)0xCC);

        byte[] result = CredentialData.HmacShortenKey(key, OathHashAlgorithm.Sha512);

        Assert.Equal(key, result);
    }

    [Fact]
    public void HmacShortenKey_ShortKey_ReturnsUnchanged()
    {
        byte[] key = [0x01, 0x02, 0x03];

        byte[] result = CredentialData.HmacShortenKey(key, OathHashAlgorithm.Sha1);

        Assert.Equal(key, result);
    }

    // --- Secret padding ---

    [Fact]
    public void PadSecret_ShortSecret_PadsToMinimumSize()
    {
        byte[] secret = [0x01, 0x02, 0x03];

        byte[] result = CredentialData.PadSecret(secret);

        Assert.Equal(OathConstants.HmacMinimumKeySize, result.Length);
        Assert.Equal(0x01, result[0]);
        Assert.Equal(0x02, result[1]);
        Assert.Equal(0x03, result[2]);
        // Remaining bytes should be zero
        for (int i = 3; i < result.Length; i++)
        {
            Assert.Equal(0x00, result[i]);
        }
    }

    [Fact]
    public void PadSecret_ExactlyMinimumSize_ReturnsUnchanged()
    {
        byte[] secret = new byte[OathConstants.HmacMinimumKeySize];
        Array.Fill(secret, (byte)0xAA);

        byte[] result = CredentialData.PadSecret(secret);

        Assert.Equal(secret, result);
    }

    [Fact]
    public void PadSecret_LargerThanMinimum_ReturnsUnchanged()
    {
        byte[] secret = new byte[32];
        Array.Fill(secret, (byte)0xBB);

        byte[] result = CredentialData.PadSecret(secret);

        Assert.Equal(secret, result);
    }

    // --- GetProcessedSecret (integration of shorten + pad) ---

    [Fact]
    public void GetProcessedSecret_ShortKey_ShortensThenPads()
    {
        var data = new CredentialData
        {
            Name = "user",
            OathType = OathType.Totp,
            HashAlgorithm = OathHashAlgorithm.Sha1,
            Secret = [0x01, 0x02, 0x03] // Short key: no shortening, but needs padding
        };

        byte[] result = data.GetProcessedSecret();

        Assert.Equal(OathConstants.HmacMinimumKeySize, result.Length);
    }

    [Fact]
    public void GetProcessedSecret_OversizeKey_HashesFirst()
    {
        byte[] longKey = new byte[100]; // > 64 byte block size for SHA-1
        Array.Fill(longKey, (byte)0xFF);

        var data = new CredentialData
        {
            Name = "user",
            OathType = OathType.Totp,
            HashAlgorithm = OathHashAlgorithm.Sha1,
            Secret = longKey
        };

        byte[] result = data.GetProcessedSecret();

        // SHA-1 hash is 20 bytes, which is >= 14 minimum, so no padding needed
        Assert.Equal(20, result.Length);
    }

    // --- Base32 decoding ---

    [Fact]
    public void ParseBase32Key_StandardPadded_DecodesCorrectly()
    {
        // "JBSWY3DPEHPK3PXP" encodes "Hello!0xDE0xAD0xBE0xEF"
        byte[] result = CredentialData.ParseBase32Key("JBSWY3DPEHPK3PXP");

        Assert.NotEmpty(result);
    }

    [Fact]
    public void ParseBase32Key_LowercaseInput_DecodesCorrectly()
    {
        byte[] upper = CredentialData.ParseBase32Key("JBSWY3DPEHPK3PXP");
        byte[] lower = CredentialData.ParseBase32Key("jbswy3dpehpk3pxp");

        Assert.Equal(upper, lower);
    }

    [Fact]
    public void ParseBase32Key_WithSpaces_DecodesCorrectly()
    {
        byte[] withSpaces = CredentialData.ParseBase32Key("JBSW Y3DP EHPK 3PXP");
        byte[] without = CredentialData.ParseBase32Key("JBSWY3DPEHPK3PXP");

        Assert.Equal(without, withSpaces);
    }

    [Fact]
    public void ParseBase32Key_EmptyString_ReturnsEmptyArray()
    {
        byte[] result = CredentialData.ParseBase32Key("");

        Assert.Empty(result);
    }
}