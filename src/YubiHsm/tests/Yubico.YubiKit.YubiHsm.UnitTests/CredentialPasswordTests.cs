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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Tests.Shared;

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
        var result = HsmAuthSession.ParseCredentialPassword("abc"u8, "credentialPassword");

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
        var result = HsmAuthSession.ParseCredentialPassword(Encoding.UTF8.GetBytes(password), "credentialPassword");

        Assert.Equal(16, result.Length);
        Assert.Equal(Encoding.UTF8.GetBytes(password), result);
    }

    [Fact]
    public void ParseCredentialPassword_Empty_Returns16ZeroBytes()
    {
        var result = HsmAuthSession.ParseCredentialPassword(ReadOnlySpan<byte>.Empty, "credentialPassword");

        Assert.Equal(16, result.Length);
        Assert.True(result.All(b => b == 0));
    }

    [Fact]
    public void ParseCredentialPassword_DoesNotAliasCallerBuffer()
    {
        // The caller owns and zeros its input; the padded copy must survive that.
        var input = "abc"u8.ToArray();
        var result = HsmAuthSession.ParseCredentialPassword(input, "credentialPassword");

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

        Assert.Throws<ArgumentException>(
            () => HsmAuthSession.ParseCredentialPassword(password, "credentialPassword"));
    }

    [Fact]
    public void ParseCredentialPassword_MultiByteUtf8_CountsByteLength()
    {
        // "aaaa" + 3 * 4-byte char = 4 + 12 = 16 UTF-8 bytes, exactly fits
        var password = Encoding.UTF8.GetBytes("aaaa\U0001F600\U0001F601\U0001F602");
        Assert.Equal(16, password.Length);

        var result = HsmAuthSession.ParseCredentialPassword(password, "credentialPassword");

        Assert.Equal(16, result.Length);
        Assert.Equal(password, result);
    }

    [Fact]
    public void ParseCredentialPassword_MultiByteExceedsLimit_ThrowsArgumentException()
    {
        // 5 emojis at 4 bytes each = 20 bytes, exceeds 16
        var password = Encoding.UTF8.GetBytes("\U0001F600\U0001F601\U0001F602\U0001F603\U0001F604");

        Assert.Throws<ArgumentException>(
            () => HsmAuthSession.ParseCredentialPassword(password, "credentialPassword"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(15)]
    [InlineData(16)]
    public void ValidateCredentialPassword_AtMost16Bytes_DoesNotThrow(int length)
    {
        // Padding semantics: anything up to 16 bytes is accepted and later null-padded.
        HsmAuthSession.ValidateCredentialPassword(new byte[length], "credentialPassword");
    }

    [Theory]
    [InlineData(17)]
    [InlineData(32)]
    public void ValidateCredentialPassword_TooLong_ThrowsArgumentException(int length)
    {
        var password = new byte[length];

        Assert.Throws<ArgumentException>(
            () => HsmAuthSession.ValidateCredentialPassword(password, "credentialPassword"));
    }

    // ─── ParamName accuracy at the public entry points ─────────────────────────
    //
    // The length check lives in a shared internal helper, but a caller filtering on
    // ArgumentException.ParamName only ever sees the public parameter it actually passed. Each
    // public entry point therefore has to name its own parameter, and the three names below are
    // genuinely different, so a single hard-coded helper name cannot satisfy all of them.

    [Fact]
    public async Task PutCredentialSymmetricAsync_PasswordTooLong_ParamNameIsCredentialPassword()
    {
        await using var session = await CreateSessionAsync();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            session.PutCredentialSymmetricAsync(
                new byte[16],
                "cred",
                new byte[16],
                new byte[16],
                new byte[17],
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("credentialPassword", exception.ParamName);
    }

    [Fact]
    public async Task ChangeCredentialPasswordAsync_CurrentPasswordTooLong_ParamNameIsCurrentPassword()
    {
        await using var session = await CreateSessionAsync();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            session.ChangeCredentialPasswordAsync(
                "cred",
                new byte[17],
                new byte[16],
                TestContext.Current.CancellationToken));

        Assert.Equal("currentPassword", exception.ParamName);
    }

    [Fact]
    public async Task ChangeCredentialPasswordAsync_NewPasswordTooLong_ParamNameIsNewPassword()
    {
        await using var session = await CreateSessionAsync();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            session.ChangeCredentialPasswordAsync(
                "cred",
                new byte[16],
                new byte[17],
                TestContext.Current.CancellationToken));

        Assert.Equal("newPassword", exception.ParamName);
    }

    [Fact]
    public async Task GetChallengeAsync_PasswordTooLong_ParamNameIsCredentialPassword()
    {
        await using var session = await CreateSessionAsync();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            session.GetChallengeAsync(
                "cred",
                new byte[17],
                TestContext.Current.CancellationToken));

        Assert.Equal("credentialPassword", exception.ParamName);
    }

    /// <summary>
    ///     A session on a recorded connection reporting firmware new enough for every password-taking
    ///     command. All the tests above throw before any APDU is transmitted, so one OK response for
    ///     the applet SELECT is all the connection needs to supply.
    /// </summary>
    private static Task<HsmAuthSession> CreateSessionAsync() =>
        HsmAuthSession.CreateAsync(
            new RecordingSmartCardConnection([0x90, 0x00]),
            firmwareVersion: new FirmwareVersion(5, 8, 0),
            cancellationToken: TestContext.Current.CancellationToken);
}