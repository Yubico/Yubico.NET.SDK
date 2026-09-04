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

using System.Security.Cryptography;
using System.Text;

namespace Yubico.YubiKit.YubiHsm.UnitTests;

/// <summary>
///     Verifies that credential passwords accept at most 16 UTF-8 bytes and are returned as a
///     separate, null-padded 16-byte buffer.
/// </summary>
public class CredentialPasswordTests
{
    [Fact]
    public void ParseCredentialPassword_ShortInput_PaddedWithNullBytes()
    {
        // "abc" = 3 UTF-8 bytes, should be padded to 16 with zeros
        var result = HsmAuthSession.ParseCredentialPassword("abc"u8);

        Assert.Equal(16, result.Length);
        Assert.Equal((byte)'a', result[0]);
        Assert.Equal((byte)'b', result[1]);
        Assert.Equal((byte)'c', result[2]);

        // Remaining bytes should be zero
        for (var i = 3; i < 16; i++)
            Assert.Equal(0, result[i]);
    }

    [Fact]
    public void ParseCredentialPassword_Exact16Bytes_NoPadNeeded()
    {
        var password = "0123456789ABCDEF"; // Exactly 16 ASCII bytes
        var result = HsmAuthSession.ParseCredentialPassword(Encoding.UTF8.GetBytes(password));

        Assert.Equal(16, result.Length);
        Assert.Equal(Encoding.UTF8.GetBytes(password), result);
    }

    [Fact]
    public void ParseCredentialPassword_Empty_Returns16ZeroBytes()
    {
        var result = HsmAuthSession.ParseCredentialPassword(ReadOnlySpan<byte>.Empty);

        Assert.Equal(16, result.Length);
        Assert.True(result.All(b => b == 0));
    }

    [Fact]
    public void ParseCredentialPassword_DoesNotAliasCallerBuffer()
    {
        // The caller owns and zeros its input; the padded copy must survive that.
        var input = "abc"u8.ToArray();
        var result = HsmAuthSession.ParseCredentialPassword(input);

        CryptographicOperations.ZeroMemory(input);

        Assert.Equal((byte)'a', result[0]);
        Assert.Equal((byte)'b', result[1]);
        Assert.Equal((byte)'c', result[2]);
    }

    [Fact]
    public void ParseCredentialPassword_TooLong_ThrowsArgumentException()
    {
        // 17 ASCII characters = 17 UTF-8 bytes, exceeds 16
        var password = "12345678901234567"u8.ToArray();

        Assert.Throws<ArgumentException>(() => HsmAuthSession.ParseCredentialPassword(password));
    }

    [Fact]
    public void ParseCredentialPassword_MultiByteUtf8_CountsByteLength()
    {
        // "aaaa" + 3 * 4-byte char = 4 + 12 = 16 UTF-8 bytes, exactly fits
        var password = Encoding.UTF8.GetBytes("aaaa\U0001F600\U0001F601\U0001F602");
        Assert.Equal(16, password.Length);

        var result = HsmAuthSession.ParseCredentialPassword(password);

        Assert.Equal(16, result.Length);
        Assert.Equal(password, result);
    }

    [Fact]
    public void ParseCredentialPassword_MultiByteExceedsLimit_ThrowsArgumentException()
    {
        // 5 emojis at 4 bytes each = 20 bytes, exceeds 16
        var password = Encoding.UTF8.GetBytes("\U0001F600\U0001F601\U0001F602\U0001F603\U0001F604");

        Assert.Throws<ArgumentException>(() => HsmAuthSession.ParseCredentialPassword(password));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(15)]
    [InlineData(16)]
    public void ValidateCredentialPassword_AtMost16Bytes_DoesNotThrow(int length)
    {
        // Padding semantics: anything up to 16 bytes is accepted and later null-padded.
        HsmAuthSession.ValidateCredentialPassword(new byte[length]);
    }

    [Theory]
    [InlineData(17)]
    [InlineData(32)]
    public void ValidateCredentialPassword_TooLong_ThrowsArgumentException(int length)
    {
        var password = new byte[length];

        Assert.Throws<ArgumentException>(() => HsmAuthSession.ValidateCredentialPassword(password));
    }
}