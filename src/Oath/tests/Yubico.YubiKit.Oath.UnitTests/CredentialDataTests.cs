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

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Yubico.YubiKit.Oath.UnitTests;

public class CredentialDataTests
{
    [Fact]
    public void Secret_InputMutationAndDisposal_DoNotChangeOtherOwner()
    {
        byte[] input = [0xA5, 0x5A, 0x31];
        using var data = new CredentialData
        {
            Name = "seed",
            OathType = OathType.Totp,
            HashAlgorithm = OathHashAlgorithm.Sha1,
            Secret = input
        };
        try
        {
            input[0] = 0x11;
            ReadOnlyMemory<byte> borrowed = data.Secret;
            Assert.Equal(0xA5, borrowed.Span[0]);
            CryptographicOperations.ZeroMemory(input);
            Assert.Equal(0xA5, borrowed.Span[0]);
            Assert.Equal(0x5A, borrowed.Span[1]);
            input[0] = 0x11;
            input[1] = 0x5A;
            data.Dispose();
            Assert.Equal(0x11, input[0]);
            Assert.Equal(0x5A, input[1]);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(input);
        }
    }

    [Fact]
    public void Secret_ReadsBorrowSameStorage_DisposalZeroesExistingViewsButNotCopies()
    {
        using var data = CredentialData.ParseUri("otpauth://totp/seed?secret=JBSWY3DPEHPK3PXP");
        ReadOnlyMemory<byte> first = data.Secret;
        ReadOnlyMemory<byte> second = data.Secret;
        byte[] copy = first.ToArray();
        try
        {
            Assert.True(MemoryMarshal.TryGetArray(first, out var firstStorage));
            Assert.True(MemoryMarshal.TryGetArray(second, out var secondStorage));
            Assert.Same(firstStorage.Array, secondStorage.Array);
            Assert.Equal(typeof(ReadOnlyMemory<byte>), typeof(CredentialData).GetProperty("Secret")?.PropertyType);
            data.Dispose();
            data.Dispose();
            Assert.True(first.Span.IndexOfAnyExcept((byte)0) < 0);
            Assert.True(second.Span.IndexOfAnyExcept((byte)0) < 0);
            Assert.True(copy.AsSpan().IndexOfAnyExcept((byte)0) >= 0);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(copy);
        }
    }

    [Theory]
    [InlineData(OathHashAlgorithm.Sha1, 13, 14)]
    [InlineData(OathHashAlgorithm.Sha1, 14, 14)]
    [InlineData(OathHashAlgorithm.Sha1, 64, 64)]
    [InlineData(OathHashAlgorithm.Sha1, 65, 20)]
    [InlineData(OathHashAlgorithm.Sha256, 64, 64)]
    [InlineData(OathHashAlgorithm.Sha256, 65, 32)]
    [InlineData(OathHashAlgorithm.Sha512, 128, 128)]
    [InlineData(OathHashAlgorithm.Sha512, 129, 64)]
    public void GetProcessedSecret_Boundaries_ReturnIndependentStorageWithExpectedBytes(
        OathHashAlgorithm algorithm, int seedLength, int processedLength)
    {
        byte[] input = new byte[seedLength];
        Array.Fill(input, (byte)0xA5);
        using var data = new CredentialData
        {
            Name = "seed",
            OathType = OathType.Totp,
            HashAlgorithm = algorithm,
            Secret = input
        };
        byte[] processed = data.GetProcessedSecret();
        byte[] expected = new byte[processedLength];
        try
        {
            if (seedLength > processedLength)
            {
                _ = algorithm switch
                {
                    OathHashAlgorithm.Sha1 => SHA1.HashData(input, expected),
                    OathHashAlgorithm.Sha256 => SHA256.HashData(input, expected),
                    OathHashAlgorithm.Sha512 => SHA512.HashData(input, expected),
                    _ => throw new InvalidOperationException()
                };
            }
            else
            {
                input.CopyTo(expected.AsSpan());
            }

            Assert.Equal(processedLength, processed.Length);
            Assert.True(CryptographicOperations.FixedTimeEquals(expected, processed));
            CryptographicOperations.ZeroMemory(processed);
            ReadOnlyMemory<byte> borrowed = data.Secret;
            Assert.True(CryptographicOperations.FixedTimeEquals(input, borrowed.Span));
            Assert.All(input, value => Assert.Equal(0xA5, value));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(input);
            CryptographicOperations.ZeroMemory(processed);
            CryptographicOperations.ZeroMemory(expected);
        }
    }

    // --- ParseUri: valid TOTP ---

    [Fact]
    public void ParseUri_ValidTotpWithIssuer_ParsesAllFields()
    {
        const string uri = "otpauth://totp/GitHub:user@example.com?secret=JBSWY3DPEHPK3PXP&issuer=GitHub&algorithm=SHA1&digits=6&period=30";

        using var data = CredentialData.ParseUri(uri);

        Assert.Equal("user@example.com", data.Name);
        Assert.Equal(OathType.Totp, data.OathType);
        Assert.Equal(OathHashAlgorithm.Sha1, data.HashAlgorithm);
        Assert.Equal(6, data.Digits);
        Assert.Equal(30, data.Period);
        Assert.Equal(0, data.Counter);
        Assert.Equal("GitHub", data.Issuer);
        Assert.False(data.Secret.IsEmpty);
    }

    [Fact]
    public void ParseUri_TotpWithDefaults_UsesDefaultValues()
    {
        const string uri = "otpauth://totp/user@example.com?secret=JBSWY3DPEHPK3PXP";

        using var data = CredentialData.ParseUri(uri);

        Assert.Equal("user@example.com", data.Name);
        Assert.Equal(OathType.Totp, data.OathType);
        Assert.Equal(OathHashAlgorithm.Sha1, data.HashAlgorithm);
        Assert.Equal(6, data.Digits);
        Assert.Equal(30, data.Period);
        Assert.Null(data.Issuer);
    }

    [Fact]
    public void ParseUri_WithTouchPolicy_PreservesParsedCredentialAndOwnsOneSecret()
    {
        const string uri = "otpauth://totp/GitHub:user@example.com?secret=JBSWY3DPEHPK3PXP&issuer=GitHub";

        using var data = CredentialData.ParseUri(uri, requireTouch: true);

        Assert.True(data.RequireTouch);
        Assert.Equal("user@example.com", data.Name);
        Assert.Equal("GitHub", data.Issuer);
        Assert.False(data.Secret.IsEmpty);
    }

    [Fact]
    public void ParseUri_WithTouchPolicy_DisposeZeroesOwnedSecret()
    {
        const string uri = "otpauth://totp/user@example.com?secret=JBSWY3DPEHPK3PXP";
        using var data = CredentialData.ParseUri(uri, requireTouch: true);
        ReadOnlyMemory<byte> ownedSecret = data.Secret;

        data.Dispose();

        Assert.True(ownedSecret.Span.IndexOfAnyExcept((byte)0) < 0);
    }

    [Fact]
    public void ParseUri_TotpWithSha256_ParsesAlgorithm()
    {
        const string uri = "otpauth://totp/Test:user?secret=JBSWY3DPEHPK3PXP&algorithm=SHA256";

        using var data = CredentialData.ParseUri(uri);

        Assert.Equal(OathHashAlgorithm.Sha256, data.HashAlgorithm);
    }

    [Fact]
    public void ParseUri_TotpWithSha512_ParsesAlgorithm()
    {
        const string uri = "otpauth://totp/Test:user?secret=JBSWY3DPEHPK3PXP&algorithm=SHA512";

        using var data = CredentialData.ParseUri(uri);

        Assert.Equal(OathHashAlgorithm.Sha512, data.HashAlgorithm);
    }

    [Fact]
    public void ParseUri_TotpWith8Digits_ParsesDigits()
    {
        const string uri = "otpauth://totp/Test:user?secret=JBSWY3DPEHPK3PXP&digits=8";

        using var data = CredentialData.ParseUri(uri);

        Assert.Equal(8, data.Digits);
    }

    [Fact]
    public void ParseUri_TotpWith60sPeriod_ParsesPeriod()
    {
        const string uri = "otpauth://totp/Test:user?secret=JBSWY3DPEHPK3PXP&period=60";

        using var data = CredentialData.ParseUri(uri);

        Assert.Equal(60, data.Period);
    }

    [Fact]
    public void ParseUri_IssuerInPathAndQuery_QueryTakesPrecedence()
    {
        const string uri = "otpauth://totp/PathIssuer:user?secret=JBSWY3DPEHPK3PXP&issuer=QueryIssuer";

        using var data = CredentialData.ParseUri(uri);

        Assert.Equal("QueryIssuer", data.Issuer);
        Assert.Equal("user", data.Name);
    }

    [Fact]
    public void ParseUri_IssuerOnlyInPath_UsesPathIssuer()
    {
        const string uri = "otpauth://totp/GitHub:user?secret=JBSWY3DPEHPK3PXP";

        using var data = CredentialData.ParseUri(uri);

        Assert.Equal("GitHub", data.Issuer);
    }

    // --- ParseUri: valid HOTP ---

    [Fact]
    public void ParseUri_ValidHotp_ParsesCorrectly()
    {
        const string uri = "otpauth://hotp/Service:user?secret=JBSWY3DPEHPK3PXP&counter=42";

        using var data = CredentialData.ParseUri(uri);

        Assert.Equal("user", data.Name);
        Assert.Equal(OathType.Hotp, data.OathType);
        Assert.Equal("Service", data.Issuer);
        Assert.Equal(42, data.Counter);
    }

    [Fact]
    public void ParseUri_HotpWithoutCounter_DefaultsToZero()
    {
        const string uri = "otpauth://hotp/user?secret=JBSWY3DPEHPK3PXP";

        using var data = CredentialData.ParseUri(uri);

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

        using var data = CredentialData.ParseUri(uri);

        Assert.False(data.Secret.IsEmpty);
    }

    [Fact]
    public void ParseUri_SecretWithSpaces_ParsesSuccessfully()
    {
        const string uri = "otpauth://totp/Test:user?secret=JBSW Y3DP EHPK 3PXP";

        using var data = CredentialData.ParseUri(uri);

        Assert.False(data.Secret.IsEmpty);
    }

    [Fact]
    public void ParseUri_CaseInsensitiveType_ParsesTotp()
    {
        const string uri = "otpauth://TOTP/Test:user?secret=JBSWY3DPEHPK3PXP";

        using var data = CredentialData.ParseUri(uri);

        Assert.Equal(OathType.Totp, data.OathType);
    }

    // --- GetId ---

    [Fact]
    public void GetId_TotpWithIssuerDefaultPeriod_MatchesFormatCredentialId()
    {
        using var data = new CredentialData
        {
            Name = "user@example.com",
            OathType = OathType.Totp,
            HashAlgorithm = OathHashAlgorithm.Sha1,
            Secret = ReadOnlyMemory<byte>.Empty,
            Issuer = "GitHub"
        };

        byte[] id = data.GetId();
        byte[] expected = Credential.FormatCredentialId("GitHub", "user@example.com", OathType.Totp);

        Assert.Equal(expected, id);
    }

    [Fact]
    public void GetId_TotpNonDefaultPeriod_IncludesPeriodPrefix()
    {
        using var data = new CredentialData
        {
            Name = "user",
            OathType = OathType.Totp,
            HashAlgorithm = OathHashAlgorithm.Sha1,
            Secret = ReadOnlyMemory<byte>.Empty,
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

        try
        {
            Assert.True(CryptographicOperations.FixedTimeEquals(key, result));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(result);
        }
    }

    [Fact]
    public void HmacShortenKey_Sha1KeyExceedsBlockSize_ReturnsHashedKey()
    {
        byte[] key = new byte[65]; // 1 byte over SHA-1 block size
        Array.Fill(key, (byte)0xAA);

        byte[] result = CredentialData.HmacShortenKey(key, OathHashAlgorithm.Sha1);

        try
        {
            Assert.Equal(20, result.Length); // SHA-1 digest size
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(result);
        }
    }

    [Fact]
    public void HmacShortenKey_Sha256KeyExceedsBlockSize_ReturnsHashedKey()
    {
        byte[] key = new byte[65];
        Array.Fill(key, (byte)0xBB);

        byte[] result = CredentialData.HmacShortenKey(key, OathHashAlgorithm.Sha256);

        try
        {
            Assert.Equal(32, result.Length); // SHA-256 digest size
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(result);
        }
    }

    [Fact]
    public void HmacShortenKey_Sha512KeyExceedsBlockSize_ReturnsHashedKey()
    {
        byte[] key = new byte[129]; // SHA-512 block size is 128
        Array.Fill(key, (byte)0xCC);

        byte[] result = CredentialData.HmacShortenKey(key, OathHashAlgorithm.Sha512);

        try
        {
            Assert.Equal(64, result.Length); // SHA-512 digest size
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(result);
        }
    }

    [Fact]
    public void HmacShortenKey_Sha512KeyWithinBlockSize_ReturnsUnchanged()
    {
        byte[] key = new byte[128]; // Exactly SHA-512 block size
        Array.Fill(key, (byte)0xCC);

        byte[] result = CredentialData.HmacShortenKey(key, OathHashAlgorithm.Sha512);

        try
        {
            Assert.True(CryptographicOperations.FixedTimeEquals(key, result));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(result);
        }
    }

    [Fact]
    public void HmacShortenKey_ShortKey_ReturnsUnchanged()
    {
        byte[] key = [0x01, 0x02, 0x03];

        byte[] result = CredentialData.HmacShortenKey(key, OathHashAlgorithm.Sha1);

        try
        {
            Assert.True(CryptographicOperations.FixedTimeEquals(key, result));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(result);
        }
    }

    // --- Secret padding ---

    [Fact]
    public void PadSecret_ShortSecret_PadsToMinimumSize()
    {
        byte[] secret = [0x01, 0x02, 0x03];

        byte[] result = CredentialData.PadSecret(secret);

        try
        {
            Assert.Equal(OathConstants.HmacMinimumKeySize, result.Length);
            Assert.Equal(0x01, result[0]);
            Assert.Equal(0x02, result[1]);
            Assert.Equal(0x03, result[2]);
            Assert.True(result.AsSpan(3).IndexOfAnyExcept((byte)0) < 0);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
            CryptographicOperations.ZeroMemory(result);
        }
    }

    [Fact]
    public void PadSecret_ExactlyMinimumSize_ReturnsUnchanged()
    {
        byte[] secret = new byte[OathConstants.HmacMinimumKeySize];
        Array.Fill(secret, (byte)0xAA);

        byte[] result = CredentialData.PadSecret(secret);

        try
        {
            Assert.True(CryptographicOperations.FixedTimeEquals(secret, result));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
            CryptographicOperations.ZeroMemory(result);
        }
    }

    [Fact]
    public void PadSecret_LargerThanMinimum_ReturnsUnchanged()
    {
        byte[] secret = new byte[32];
        Array.Fill(secret, (byte)0xBB);

        byte[] result = CredentialData.PadSecret(secret);

        try
        {
            Assert.True(CryptographicOperations.FixedTimeEquals(secret, result));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
            CryptographicOperations.ZeroMemory(result);
        }
    }

    // --- GetProcessedSecret (integration of shorten + pad) ---

    [Fact]
    public void GetProcessedSecret_ShortKey_ShortensThenPads()
    {
        byte[] input = [0x01, 0x02, 0x03];
        using var data = new CredentialData
        {
            Name = "user",
            OathType = OathType.Totp,
            HashAlgorithm = OathHashAlgorithm.Sha1,
            Secret = input // Short key: no shortening, but needs padding
        };

        byte[] result = data.GetProcessedSecret();

        try
        {
            Assert.Equal(OathConstants.HmacMinimumKeySize, result.Length);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(input);
            CryptographicOperations.ZeroMemory(result);
        }
    }

    [Fact]
    public void GetProcessedSecret_OversizeKey_HashesFirst()
    {
        byte[] longKey = new byte[100]; // > 64 byte block size for SHA-1
        Array.Fill(longKey, (byte)0xFF);

        using var data = new CredentialData
        {
            Name = "user",
            OathType = OathType.Totp,
            HashAlgorithm = OathHashAlgorithm.Sha1,
            Secret = longKey
        };

        byte[] result = data.GetProcessedSecret();

        // SHA-1 hash is 20 bytes, which is >= 14 minimum, so no padding needed
        try
        {
            Assert.Equal(20, result.Length);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(longKey);
            CryptographicOperations.ZeroMemory(result);
        }
    }

    // --- Base32 decoding ---

    [Fact]
    public void ParseBase32Key_StandardPadded_DecodesCorrectly()
    {
        // "JBSWY3DPEHPK3PXP" encodes "Hello!0xDE0xAD0xBE0xEF"
        byte[] result = CredentialData.ParseBase32Key("JBSWY3DPEHPK3PXP");

        try
        {
            Assert.NotEmpty(result);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(result);
        }
    }

    [Fact]
    public void ParseBase32Key_LowercaseInput_DecodesCorrectly()
    {
        byte[] upper = CredentialData.ParseBase32Key("JBSWY3DPEHPK3PXP");
        byte[] lower = CredentialData.ParseBase32Key("jbswy3dpehpk3pxp");

        try
        {
            Assert.True(CryptographicOperations.FixedTimeEquals(upper, lower));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(upper);
            CryptographicOperations.ZeroMemory(lower);
        }
    }

    [Fact]
    public void ParseBase32Key_WithSpaces_DecodesCorrectly()
    {
        byte[] withSpaces = CredentialData.ParseBase32Key("JBSW Y3DP EHPK 3PXP");
        byte[] without = CredentialData.ParseBase32Key("JBSWY3DPEHPK3PXP");

        try
        {
            Assert.True(CryptographicOperations.FixedTimeEquals(without, withSpaces));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(without);
            CryptographicOperations.ZeroMemory(withSpaces);
        }
    }

    [Fact]
    public void ParseBase32Key_EmptyString_ReturnsEmptyArray()
    {
        byte[] result = CredentialData.ParseBase32Key("");

        Assert.Empty(result);
    }
}